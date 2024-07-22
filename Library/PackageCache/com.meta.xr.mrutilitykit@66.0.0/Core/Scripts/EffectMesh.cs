/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Meta.XR.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Meta.XR.MRUtilityKit
{
    [Feature(Feature.Scene)]
    public class EffectMesh : MonoBehaviour
    {
        [Tooltip("When the scene data is loaded, this controls what room(s) the effect mesh is applied to.")]
        public MRUK.RoomFilter SpawnOnStart = MRUK.RoomFilter.CurrentRoomOnly;

        [Tooltip("If enabled, updates on scene elements such as rooms and anchors will be handled by this class")]
        public bool TrackUpdates = true;

        [Tooltip("The material applied to the generated mesh.")]
        [FormerlySerializedAs("_MeshMaterial")]
        public Material MeshMaterial;

        [Tooltip("The inset vertex spacing on each polygon.")]
        [FormerlySerializedAs("_borderSize")]
        public float BorderSize = 0.0f;

        [Tooltip("Generate a BoxCollider for each mesh component.")]
        [FormerlySerializedAs("addColliders")]
        public bool Colliders = false;

        [Tooltip("Cut holes in the mesh for door frames and/or window frames. NOTE: This does not apply if border size is non-zero.")]
        public MRUKAnchor.SceneLabels CutHoles;

        [Tooltip("Whether the effect mesh objects will cast a shadow.")]
        [SerializeField]
        private bool castShadows = true;

        [Tooltip("Hide the effect mesh.")]
        [SerializeField]
        private bool hideMesh = false;


        private MRUK.SceneTrackingSettings SceneTrackingSettings;

        public bool CastShadow
        {
            get => castShadows;
            set { ToggleShadowCasting(value); castShadows = value; }
        }

        public bool HideMesh
        {
            get => hideMesh;
            set { ToggleEffectMeshVisibility(!value); hideMesh = value; }
        }

        public bool ToggleColliders
        {
            get => Colliders;
            set { ToggleEffectMeshColliders(!value); Colliders = value;
            }
        }

        public enum WallTextureCoordinateModeU
        {
            METRIC,                         // The texture coordinates start at 0 and increase by 1 unit every meter.
            METRIC_SEAMLESS,                // The texture coordinates start at 0 and increase by 1 unit every meter but are adjusted to end on a whole number to avoid seams.
            MAINTAIN_ASPECT_RATIO,          // The texture coordinates are adjusted to the other dimensions to ensure the aspect ratio is maintained.
            MAINTAIN_ASPECT_RATIO_SEAMLESS, // The texture coordinates are adjusted to the other dimensions to ensure the aspect ratio is maintained but are adjusted to end on a whole number to avoid seams.
            STRETCH,                        // The texture coordinates range from 0 to 1.
            STRETCH_SECTION,                // The texture coordinates start at 0 and increase to 1 for each individual wall section.
        };
        public enum WallTextureCoordinateModeV
        {
            METRIC,                         // The texture coordinates start at 0 and increase by 1 unit every meter.
            MAINTAIN_ASPECT_RATIO,          // The texture coordinates are adjusted to the other dimensions to ensure the aspect ratio is maintained.
            STRETCH,                        // The texture coordinates range from 0 to 1.
        };
        public enum AnchorTextureCoordinateMode
        {
            METRIC,                         // The texture coordinates start at 0 and increase by 1 unit every meter.
            STRETCH,                        // The texture coordinates range from 0 to 1 across the anchor surface.
        };

        [Serializable]
        public class TextureCoordinateModes
        {
            [FormerlySerializedAs("U")]
            public WallTextureCoordinateModeU WallU = WallTextureCoordinateModeU.METRIC;
            [FormerlySerializedAs("V")]
            public WallTextureCoordinateModeV WallV = WallTextureCoordinateModeV.METRIC;
            public AnchorTextureCoordinateMode AnchorUV = AnchorTextureCoordinateMode.METRIC;
        };
        [Tooltip("Can not exceed 8.")]
        public TextureCoordinateModes[] textureCoordinateModes = new TextureCoordinateModes[1] { new TextureCoordinateModes() };

        [FormerlySerializedAs("_include")]
        public MRUKAnchor.SceneLabels Labels;

        private static readonly string Suffix = "_EffectMesh";

        Dictionary<MRUKAnchor, EffectMeshObject> effectMeshObjects = new ();


        public class EffectMeshObject
        {
            public GameObject effectMeshGO;
            public Mesh mesh;
            public Collider collider;
        }

        private void Start()
        {
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadEffectMesh).Send();
#endif
            if (MRUK.Instance is null) return;

            SceneTrackingSettings.UnTrackedRooms = new();
            SceneTrackingSettings.UnTrackedAnchors = new();

            MRUK.Instance.RegisterSceneLoadedCallback(() =>
            {
                if (SpawnOnStart == MRUK.RoomFilter.None) return;

                switch(SpawnOnStart)
                {
                    case MRUK.RoomFilter.CurrentRoomOnly:
                        CreateMesh(MRUK.Instance.GetCurrentRoom());
                        break;
                    case MRUK.RoomFilter.AllRooms:
                        CreateMesh();
                        break;
                }
            });

            if (!TrackUpdates) return;

            MRUK.Instance.RoomCreatedEvent.AddListener(ReceiveCreatedRoom);
            MRUK.Instance.RoomRemovedEvent.AddListener(ReceiveRemovedRoom);
        }

        private void ReceiveRemovedRoom(MRUKRoom room)
        {
            // there is no check on ```SceneTrackingSettings.TrackUpdates``` when removing a room.
            DestroyMesh(room);
            UnregisterAnchorUpdates(room);
        }

        private void UnregisterAnchorUpdates(MRUKRoom room)
        {
            room.AnchorCreatedEvent.RemoveListener(ReceiveAnchorCreatedEvent);
            room.AnchorRemovedEvent.RemoveListener(ReceiveAnchorRemovedCallback);
            room.AnchorUpdatedEvent.RemoveListener(ReceiveAnchorUpdatedCallback);
        }

        private void RegisterAnchorUpdates(MRUKRoom room)
        {
            room.AnchorCreatedEvent.AddListener(ReceiveAnchorCreatedEvent);
            room.AnchorRemovedEvent.AddListener(ReceiveAnchorRemovedCallback);
            room.AnchorUpdatedEvent.AddListener(ReceiveAnchorUpdatedCallback);
        }

        private void ReceiveAnchorUpdatedCallback(MRUKAnchor anchor)
        {
            // only update the anchor when we track updates
            // &
            // only create when the anchor or parent room is tracked
            if (SceneTrackingSettings.UnTrackedRooms.Contains(anchor.Room) ||
                SceneTrackingSettings.UnTrackedAnchors.Contains(anchor) ||
                !TrackUpdates)
            {
                return;
            }
            DestroyMesh(anchor);
            CreateEffectMesh(anchor);
        }

        private void ReceiveAnchorRemovedCallback(MRUKAnchor anchor)
        {
            // there is no check on ```SceneTrackingSettings.TrackUpdates``` when removing an anchor.
            DestroyMesh(anchor);
        }

        private void ReceiveAnchorCreatedEvent(MRUKAnchor anchor)
        {
            // only create the anchor when we track updates
            // &
            // only create when the parent room is tracked
            if (SceneTrackingSettings.UnTrackedRooms.Contains(anchor.Room) ||
                !TrackUpdates)
            {
                return;
            }
            CreateEffectMesh(anchor);
        }

        private void ReceiveCreatedRoom(MRUKRoom room)
        {
            //only create the room when we track room updates
            if (TrackUpdates &&
                SpawnOnStart == MRUK.RoomFilter.AllRooms)
            {
                CreateMesh(room);
                RegisterAnchorUpdates(room);
            }
        }

        /// <summary>
        /// For 2 walls defined by 3 corner points, get the inset position from the inside corner.
        /// Inset Position is "border" meters away from the corner, and relative to point2
        /// It will always point to the "inside" of the room
        /// </summary>
        public Vector3 GetInsetPositionOffset(Vector2 point1, Vector2 point2, Vector2 point3, float border)
        {
            Vector2 vec1 = (point2 - point1).normalized;
            Vector2 vec2 = (point3 - point2).normalized;
            Vector2 insetDir = (vec2 - vec1).normalized;
            insetDir.Normalize();
            if ((vec1.x * vec2.y - vec1.y * vec2.x) < 0)
            {
                insetDir = -insetDir;
            }
            if (insetDir.magnitude <= Mathf.Epsilon)
            {
                insetDir = Vector2.right;
            }
            // ensure that the border is the same width regardless of angle between walls
            float angle = Vector3.Angle(vec2, insetDir);
            float adjacent = border / Mathf.Tan(angle * Mathf.Deg2Rad);
            float adjustedBorderSize = Mathf.Sqrt(adjacent * adjacent + border * border);

            return (insetDir * adjustedBorderSize);
        }

        /// <summary>
        /// Given a clockwise set of points (outer then inner), set up triangle indices accordingly
        /// </summary>
        void CreateBorderPolygon(ref int[] indexArray, ref int indexCounter, int baseCount, int pointsInLoop)
        {
            for (int j = 0; j < pointsInLoop; j++)
            {
                int id1 = ((j + 1) % pointsInLoop);
                int id2 = pointsInLoop + j;

                indexArray[indexCounter++] = baseCount + j;
                indexArray[indexCounter++] = baseCount + id1;
                indexArray[indexCounter++] = baseCount + id2;

                indexArray[indexCounter++] = baseCount + pointsInLoop + ((j + 1) % pointsInLoop);
                indexArray[indexCounter++] = baseCount + id2;
                indexArray[indexCounter++] = baseCount + id1;
            }
        }

        /// <summary>
        /// Given a clockwise set of points, triangulate the interior
        /// </summary>
        void CreateInteriorPolygon(ref int[] indexArray, ref int indexCounter, int baseCount, List<Vector2> points)
        {
            List<int> indices = Triangulator.TriangulatePoints(points);
            int capTriCount = indices.Count / 3;
            for (int j = 0; j < capTriCount; j++)
            {
                int id0 = indices[j * 3];
                int id1 = indices[j * 3 + 1];
                int id2 = indices[j * 3 + 2];

                indexArray[indexCounter++] = baseCount + id0;
                indexArray[indexCounter++] = baseCount + id1;
                indexArray[indexCounter++] = baseCount + id2;
            }
        }

        /// <summary>
        /// Create a triangle fan given the number of points to triangulate
        /// </summary>
        void CreateInteriorTriangleFan(ref int[] indexArray, ref int indexCounter, int baseCount, int pointsInLoop)
        {
            int capTriCount = pointsInLoop - 2;
            for (int j = 0; j < capTriCount; j++)
            {
                int id1 = j + 1;
                int id2 = j + 2;
                indexArray[indexCounter++] = baseCount;
                indexArray[indexCounter++] = baseCount + id1;
                indexArray[indexCounter++] = baseCount + id2;
            }
        }

        /// <summary>
        /// Creates effect mesh for all elements in all rooms
        /// </summary>
        public void CreateMesh()
        {
            foreach (var room in MRUK.Instance.Rooms)
            {
                CreateMesh(room);
                RegisterAnchorUpdates(room);
            }
        }


        /// <summary>
        /// Destroys mesh the objects instantiated based on the provided label filter.
        /// </summary>
        /// <param name="label">The filter for mesh object destruction.
        /// If a mesh object's anchor labels pass this filter, the mesh object will be destroyed.
        /// Default value includes all labels.</param>
        public void DestroyMesh(LabelFilter label = new LabelFilter())
        {
            List<MRUKAnchor> itemsToRemove = new();
            foreach (var kv in effectMeshObjects)
            {
                bool filterByLabel = label.PassesFilter(kv.Key.Label);
                if (kv.Value.effectMeshGO && filterByLabel)
                {
                    DestroyImmediate(kv.Value.effectMeshGO);
                    itemsToRemove.Add(kv.Key);
                }
            }

            foreach (var itemToRemove in itemsToRemove)
            {
                effectMeshObjects.Remove(itemToRemove);
                SceneTrackingSettings.UnTrackedAnchors.Add(itemToRemove);
            }
        }

        /// <summary>
        /// Destroys all meshs in the given room and mark the room as not tracked anymore by this class
        /// </summary>
        /// <param name="room">MRUK Room</param>
        public void DestroyMesh(MRUKRoom room)
        {
            var anchors = room.Anchors;
            foreach (var anchor in anchors)
            {
                DestroyMesh(anchor);
            }
            SceneTrackingSettings.UnTrackedRooms.Add(room);
        }

        /// <summary>
        /// Destroys the meshs associated with the provided anchor and mark the anchor as not tracked anymore by this class
        /// </summary>
        /// <param name="anchor">MRUK Anchor</param>
        public void DestroyMesh(MRUKAnchor anchor)
        {
            if(effectMeshObjects.TryGetValue(anchor, out var eMO))
            {
                if (eMO.effectMeshGO)
                {
                    DestroyImmediate(eMO.effectMeshGO);
                    effectMeshObjects.Remove(anchor);
                    SceneTrackingSettings.UnTrackedAnchors.Add(anchor);
                }
            }
        }

        /// <summary>
        /// Adds colliders to the mesh objects instantiated based on the provided label filter.
        /// </summary>
        /// <param name="label">The filter to determine which mesh objects receive a collider.
        /// If a mesh object's anchor labels pass this filter and the mesh object does not already have a collider, a new collider is added.
        /// Default value includes all labels.</param>
        public void AddColliders(LabelFilter label = new LabelFilter())
        {
            foreach (var kv in effectMeshObjects)
            {
                bool filterByLabel = label.PassesFilter(kv.Key.Label);
                if (kv.Key && !kv.Value.collider && filterByLabel)
                {
                    kv.Value.collider = AddCollider(kv.Key, kv.Value);
                }
            }
        }

        /// <summary>
        /// Destroy the colliders of the instantiated mesh objects based on the provided label filter.
        /// </summary>
        /// <param name="label">The filter to determine which mesh objects receive a collider.
        /// If a mesh object's anchor labels pass this filter and the mesh object does not already have a collider, a new collider is added.
        /// Default value includes all labels.</param>
        public void DestroyColliders(LabelFilter label = new LabelFilter())
        {
            foreach (var kv in effectMeshObjects)
            {
                bool filterByLabel = label.PassesFilter(kv.Key.Label);
                if (kv.Value.collider && filterByLabel)
                {
                    DestroyImmediate(kv.Value.collider);
                }
            }
        }

        public void ToggleShadowCasting(bool shouldCast, LabelFilter label = new LabelFilter())
        {
            foreach (var kv in effectMeshObjects)
            {
                bool filterByLabel = label.PassesFilter(kv.Key.Label);
                if (kv.Value.effectMeshGO && filterByLabel)
                {
                    ShadowCastingMode castingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    kv.Value.effectMeshGO.GetComponent<MeshRenderer>().shadowCastingMode = castingMode;
                }
            }
        }

        /// <summary>
        /// Toggles the visibility of the effect mesh objects instantiated based on the provided label filter.
        /// </summary>
        /// <param name="shouldShow">Determines whether the effect mesh objects should be visible or not.</param>
        /// <param name="label">The filter to determine which effect mesh objects have their visibility toggled.
        /// If an effect mesh object's anchor labels pass this filter, its visibility is toggled according to the 'shouldShow' parameter.
        /// Default value includes all labels.</param>
        /// <param name="materialOverride">An optional material to apply to the effect mesh objects when their visibility is toggled.
        /// If not provided, the material of the mesh objects remains unchanged.</param>
        public void ToggleEffectMeshVisibility(bool shouldShow, LabelFilter label = new LabelFilter(), Material materialOverride = null)
        {
            foreach (var kv in effectMeshObjects)
            {
                bool filterByLabel = label.PassesFilter(kv.Key.Label);
                if (kv.Value.effectMeshGO && filterByLabel)
                {
                    kv.Value.effectMeshGO.GetComponent<MeshRenderer>().enabled = shouldShow;
                    if (materialOverride)
                    {
                        kv.Value.effectMeshGO.GetComponent<MeshRenderer>().material = materialOverride;
                    }
                }
            }
        }

        /// <summary>
        /// Toggles the colliders of the effect mesh objects. Creates one if not existing.
        /// </summary>
        /// <param name="doEnable">Determines whether the effect mesh objects should have an active collider or not.</param>
        /// <param name="label">The filter to determine which effect mesh objects have their visibility toggled.
        /// If an effect mesh object's anchor labels pass this filter, its visibility is toggled according to the 'shouldShow' parameter.
        /// Default value includes all labels.</param>
        public void ToggleEffectMeshColliders(bool doEnable, LabelFilter label = new LabelFilter())
        {
            foreach (var kv in effectMeshObjects)
            {
                var filterByLabel = label.PassesFilter(kv.Key.Label);
                if (!filterByLabel) continue;

                if (!kv.Value.collider)
                {
                    AddCollider(kv.Key, kv.Value);
                }

                kv.Value.collider.enabled = doEnable;
            }
        }
        /// <summary>
        /// Overrides the material of the effect mesh objects instantiated based on the provided label filter.
        /// </summary>
        /// <param name="newMaterial">The new material to apply to the effect mesh objects.</param>
        /// <param name="label">The filter to determine which effect mesh objects have their material overridden.
        /// If an effect mesh object's anchor labels pass this filter, its material is changed to the new material.
        /// Default value is a new instance of LabelFilter.</param>
        public void OverrideEffectMaterial(Material newMaterial, LabelFilter label = new LabelFilter())
        {
            foreach (var kv in effectMeshObjects)
            {
                bool filterByLabel = label.PassesFilter(kv.Key.Label);
                if (kv.Value.effectMeshGO && filterByLabel)
                {
                    kv.Value.effectMeshGO.GetComponent<MeshRenderer>().material = newMaterial;
                }
            }
        }

        private (List<MRUKAnchor>,float,float) GenerateData(MRUKRoom room)
        {
            var shortestWall = Mathf.Infinity;
            var totalWallLength = 0.0f;

            List<MRUKAnchor> walls = new List<MRUKAnchor>();
            foreach (var anchorInfo in room.Anchors)
            {
                if (anchorInfo.HasAnyLabel(MRUKAnchor.SceneLabels.WALL_FACE))
                {
                    Vector2 wallScale = anchorInfo.PlaneRect.Value.size;
                    shortestWall = Mathf.Min(Mathf.Min(wallScale.x, wallScale.y), shortestWall);
                    walls.Add(anchorInfo);
                }
            }
            var sortedWalls = GetOrderedWalls(walls, ref totalWallLength);
            var polyBorderSize = Mathf.Min(shortestWall * 0.5f, BorderSize);

            return (sortedWalls,totalWallLength, polyBorderSize);
        }

        /// <summary>
        /// Creates effect mesh for all objects in the given room
        /// </summary>
        /// <param name="room">The room to apply to</param>
        public void CreateMesh(MRUKRoom room)
        {
            // To get all the anchors in the space:
            var sceneAnchors = room.Anchors;

            var (sortedWalls, totalWallLength, polyBorderSize) = GenerateData(room);

            MRUKAnchor floor = null;
            MRUKAnchor ceiling = null;

            if (CutHoles != 0 && BorderSize > 0.0f)
            {
                Debug.LogWarning("CutHoles property does not have any effect when BorderSize is non-zero");
            }

            for (int i = 0; i < sceneAnchors.Count; i++)
            {
                MRUKAnchor anchorInfo = sceneAnchors[i];
                if (anchorInfo && anchorInfo.HasAnyLabel(Labels))
                {
                    if (anchorInfo.HasAnyLabel(MRUKAnchor.SceneLabels.WALL_FACE))
                    {
                        continue;
                    }
                    if (anchorInfo.HasAnyLabel(MRUKAnchor.SceneLabels.CEILING))
                    {
                        ceiling = anchorInfo;
                    }
                    else if (anchorInfo.HasAnyLabel(MRUKAnchor.SceneLabels.FLOOR))
                    {
                        floor = anchorInfo;
                    }
                    else if (anchorInfo.HasAnyLabel(MRUKAnchor.SceneLabels.GLOBAL_MESH))
                    {
                        CreateGlobalMeshObject(anchorInfo);
                    }
                    else
                    {
                        CreateEffectMesh(anchorInfo);
                    }
                }
            }

            float uSpacing = 0.0f;
            for (int i = 0; i < sortedWalls.Count; i++)
            {
                // sortedWalls contains ALL walls in the room, both WALL_FACE and INVISIBLE_WALL_FACE
                // however, we only want to create the mesh for walls in this EffectMesh's label list
                // this requires custom behavior because every INVISIBLE wall is also tagged as a WALL
                bool includeMesh = IncludesLabel(MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE) &&
                    sortedWalls[i].HasAnyLabel(MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE);
                includeMesh |= IncludesLabel(MRUKAnchor.SceneLabels.WALL_FACE) &&
                    !sortedWalls[i].HasAnyLabel(MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE);

                if (includeMesh)
                {
                    CreateEffectMeshWall(sortedWalls[i], totalWallLength, ref uSpacing, polyBorderSize);
                }
            }
            if (ceiling) CreateEffectMesh(ceiling, polyBorderSize);
            if (floor) CreateEffectMesh(floor, polyBorderSize);

            if (!TrackUpdates)
            {
                if (!SceneTrackingSettings.UnTrackedRooms.Contains(room))
                {
                    SceneTrackingSettings.UnTrackedRooms.Add(room);
                }
            }
        }

        private bool IncludesLabel(MRUKAnchor.SceneLabels label)
        {
            return (Labels & label) != 0;
        }

        List<MRUKAnchor> GetOrderedWalls(List<MRUKAnchor> randomWalls, ref float wallLength)
        {
            List<MRUKAnchor> orderedWalls = new List<MRUKAnchor>(randomWalls.Count);

            int seedId = 0;
            for (int i = 0; i < randomWalls.Count; i++)
            {
                Vector2 wallScale = randomWalls[i].PlaneRect.Value.size;
                float thisLength = wallScale.x;
                wallLength += thisLength;

                orderedWalls.Add(GetRightWall(ref seedId, randomWalls));
            }
            return orderedWalls;
        }

        MRUKAnchor GetRightWall(ref int thisID, List<MRUKAnchor> randomWalls)
        {
            Vector2 thisWallScale = randomWalls[thisID].PlaneRect.Value.size;

            Vector3 halfScale = thisWallScale * 0.5f;
            Vector3 bottomRight = randomWalls[thisID].transform.position - randomWalls[thisID].transform.up * halfScale.y - randomWalls[thisID].transform.right * halfScale.x;
            float closestCornerDistance = Mathf.Infinity;
            // When searching for a matching corner, the correct one should match positions. If they don't, assume there's a crack in the room.
            // This should be an impossible scenario and likely means broken data from Room Setup.
            int rightWallID = 0;
            for (int i = 0; i < randomWalls.Count; i++)
            {
                // compare to bottom left point of other walls
                if (i != thisID)
                {
                    Vector2 testWallHalfScale = randomWalls[i].PlaneRect.Value.size;
                    testWallHalfScale *= 0.5f;
                    Vector3 bottomLeft = randomWalls[i].transform.position - randomWalls[i].transform.up * testWallHalfScale.y + randomWalls[i].transform.right * testWallHalfScale.x;
                    float thisCornerDistance = Vector3.Distance(bottomLeft, bottomRight);
                    if (thisCornerDistance < closestCornerDistance)
                    {
                        closestCornerDistance = thisCornerDistance;
                        rightWallID = i;
                    }
                }
            }
            thisID = rightWallID;
            return randomWalls[thisID];
        }

        public EffectMeshObject CreateEffectMesh(MRUKAnchor anchorInfo)
        {
            return CreateEffectMesh(anchorInfo, BorderSize);
        }

        EffectMeshObject CreateEffectMesh(MRUKAnchor anchorInfo, float border)
        {
            if (effectMeshObjects.ContainsKey(anchorInfo))
            {
                //Anchor already has an EffectMeshComponent
                return null;
            }
            EffectMeshObject effectMeshObject = new EffectMeshObject();
            int totalVertices;
            int totalIndices;
            bool createBorder = border > 0.0f;
            if (anchorInfo.VolumeBounds.HasValue)
            {
                totalVertices = 24;
                totalIndices = 36;
                if (createBorder)
                {
                    totalVertices += 24;
                    totalIndices += 144;
                }
            }
            else if (anchorInfo.PlaneRect.HasValue)
            {
                totalVertices = anchorInfo.PlaneBoundary2D.Count;
                totalIndices = (anchorInfo.PlaneBoundary2D.Count - 2) * 3;
                if (createBorder)
                {
                    totalVertices += anchorInfo.PlaneBoundary2D.Count;
                    totalIndices += anchorInfo.PlaneBoundary2D.Count * 6;
                }
            }
            else
            {
                return effectMeshObject;
            }

            GameObject newGameObject = new GameObject(anchorInfo.name + Suffix);
            newGameObject.transform.SetParent(anchorInfo.transform, false);

            effectMeshObject.effectMeshGO = newGameObject;
            Mesh newMesh = new Mesh();
            var meshFilter = newGameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = newMesh;

            // only attach MeshRenderer if a material has been assigned
            if (MeshMaterial != null)
            {
                MeshRenderer newRenderer = newGameObject.AddComponent<MeshRenderer>();
                newRenderer.material = MeshMaterial;
                newRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                newRenderer.enabled = !hideMesh;
            }
            int UVChannelCount = Math.Min(8, textureCoordinateModes.Length);
            Vector2[][] MeshUVs = new Vector2[UVChannelCount][];
            for (int x = 0; x < UVChannelCount; x++)
            {
                MeshUVs[x] = new Vector2[totalVertices];
            }
            Vector3[] MeshVertices = new Vector3[totalVertices];
            Color32[] MeshColors = new Color32[totalVertices];
            Vector3[] MeshNormals = new Vector3[totalVertices];
            Vector4[] MeshTangents = new Vector4[totalVertices];

            int[] MeshTriangles = new int[totalIndices];

            int vertCounter = 0;
            int triCounter = 0;
            int baseVert = 0;

            if (anchorInfo.VolumeBounds.HasValue)
            {
                Vector3 dim = anchorInfo.VolumeBounds.Value.size;
                // if the object is thin, the border size needs to adjust
                border = Mathf.Min(border, dim.x * 0.5f, dim.y * 0.5f, dim.z * 0.5f);

                // each cube face gets an 8-vertex mesh
                for (int j = 0; j < 6; j++)
                {
                    Vector3 right, up, fwd;
                    Vector3 rotatedDim;
                    float UVxDim = dim.x;
                    float UVyDim = dim.y;
                    switch (j)
                    {
                        case 0:
                            rotatedDim = new Vector3(dim.x, dim.y, dim.z);
                            right = Vector3.right;
                            up = Vector3.up;
                            fwd = Vector3.forward;
                            break;
                        case 1:
                            rotatedDim = new Vector3(dim.x, dim.z, dim.y);
                            right = Vector3.right;
                            up = -Vector3.forward;
                            fwd = Vector3.up;
                            UVyDim = dim.z;
                            break;
                        case 2:
                            rotatedDim = new Vector3(dim.x, dim.y, dim.z);
                            right = Vector3.right;
                            up = -Vector3.up;
                            fwd = -Vector3.forward;
                            break;
                        case 3:
                            rotatedDim = new Vector3(dim.x, dim.z, dim.y);
                            right = Vector3.right;
                            up = Vector3.forward;
                            fwd = -Vector3.up;
                            UVyDim = dim.z;
                            break;
                        case 4:
                            rotatedDim = new Vector3(dim.z, dim.y, dim.x);
                            right = -Vector3.forward;
                            up = Vector3.up;
                            fwd = Vector3.right;
                            UVxDim = dim.z;
                            break;
                        case 5:
                            rotatedDim = new Vector3(dim.z, dim.y, dim.x);
                            right = Vector3.forward;
                            up = Vector3.up;
                            fwd = -Vector3.right;
                            UVxDim = dim.z;
                            break;
                        default:
                            throw new IndexOutOfRangeException("Index j is out of range");
                    }

                    // for each face of the cube, make a bordered quad
                    for (int k = 0; k < 4; k++)
                    {
                        float UVx = (k / 2 == 0) ? 0.0f : 1.0f;
                        float UVy = (k == 1 || k == 2) ? 1.0f : 0.0f;

                        float xDir = Mathf.Sign(UVx - 0.5f);
                        float yDir = Mathf.Sign(UVy - 0.5f);

                        Vector3 basePoint = fwd * rotatedDim.z * 0.5f + right * rotatedDim.x * 0.5f - up * rotatedDim.y * 0.5f;
                        basePoint += up * rotatedDim.y * UVy - right * rotatedDim.x * UVx;
                        Vector2 quadUV = new Vector2(UVx, UVy);

                        for (int x = 0; x < UVChannelCount; x++)
                        {
                            Vector2 uvScaleFactor = Vector2.one;
                            switch (textureCoordinateModes[x].AnchorUV)
                            {
                                case AnchorTextureCoordinateMode.METRIC:
                                    uvScaleFactor = new Vector2(UVxDim, UVyDim);
                                    break;
                            }
                            MeshUVs[x][vertCounter] = Vector2.Scale(quadUV, uvScaleFactor);
                            if (createBorder)
                            {
                                Vector2 UVoffset = new Vector2(-xDir * border / UVxDim, -yDir * border / UVyDim);
                                MeshUVs[x][vertCounter + 4] = Vector2.Scale(quadUV + UVoffset, uvScaleFactor);
                            }
                        }
                        MeshVertices[vertCounter] = basePoint - Vector3.forward * dim.z * 0.5f;
                        MeshColors[vertCounter] = createBorder ? Color.black : Color.white;
                        MeshNormals[vertCounter] = fwd;
                        MeshTangents[vertCounter] = new Vector4(-right.x, -right.y, -right.z, -1);

                        if (createBorder)
                        {
                            Vector3 offset = up * -yDir * border + right * xDir * border;

                            MeshVertices[vertCounter + 4] = MeshVertices[vertCounter] + offset;
                            MeshColors[vertCounter + 4] = Color.white;
                            MeshNormals[vertCounter + 4] = fwd;
                            MeshTangents[vertCounter + 4] = new Vector4(-right.x, -right.y, -right.z, -1);
                        }
                        vertCounter++;
                    }

                    if (createBorder)
                    {
                        vertCounter += 4;
                        CreateBorderPolygon(ref MeshTriangles, ref triCounter, baseVert, 4);
                        baseVert += 4;
                    }
                    CreateInteriorTriangleFan(ref MeshTriangles, ref triCounter, baseVert, 4);
                    baseVert += 4;
                }
            }
            else
            {
                Rect rect = anchorInfo.PlaneRect.Value;
                // if the object is thin, the border size needs to adjust
                border = Mathf.Min(border, rect.size.x * 0.5f, rect.size.y * 0.5f);

                List<Vector2> localPoints = anchorInfo.PlaneBoundary2D;
                List<Vector2> localInnerPoints = new List<Vector2>(localPoints.Count);

                for (int i = 0; i < localPoints.Count; i++)
                {
                    Vector2 thisCorner = localPoints[i];
                    Vector2 nextCorner = (i == localPoints.Count - 1) ? localPoints[0] : localPoints[i + 1];
                    Vector2 lastCorner = (i == 0) ? localPoints[localPoints.Count - 1] : localPoints[i - 1];
                    Vector2 insetPosOffset = GetInsetPositionOffset(lastCorner, thisCorner, nextCorner, border);
                    Vector2 innerVertex = thisCorner + insetPosOffset;
                    localInnerPoints.Add(innerVertex);

                    for (int x = 0; x < UVChannelCount; x++)
                    {
                        Vector2 uvScaleFactor = Vector2.one;
                        switch (textureCoordinateModes[x].AnchorUV)
                        {
                            case AnchorTextureCoordinateMode.STRETCH:
                                uvScaleFactor = new Vector2(1 / (rect.xMax - rect.xMin), 1 / (rect.yMax - rect.yMin));
                                break;
                        }

                        MeshUVs[x][vertCounter] = Vector2.Scale(new Vector2(rect.xMax - thisCorner.x, thisCorner.y - rect.yMin), uvScaleFactor);
                        if (createBorder)
                        {
                            MeshUVs[x][vertCounter + localPoints.Count] = Vector2.Scale(new Vector2(rect.xMax - innerVertex.x, innerVertex.y - rect.yMin), uvScaleFactor);
                        }
                    }

                    MeshVertices[vertCounter] = new Vector3(thisCorner.x, thisCorner.y, 0);
                    MeshColors[vertCounter] = createBorder ? Color.black : Color.white;
                    MeshNormals[vertCounter] = Vector3.forward;
                    MeshTangents[vertCounter] = new Vector4(1, 0, 0, 1);

                    if (createBorder)
                    {
                        MeshVertices[vertCounter + localPoints.Count] = new Vector3(innerVertex.x, innerVertex.y, 0);
                        MeshColors[vertCounter + localPoints.Count] = Color.white;
                        MeshNormals[vertCounter + localPoints.Count] = Vector3.forward;
                        MeshTangents[vertCounter + localPoints.Count] = new Vector4(1, 0, 0, 1);
                    }

                    vertCounter++;
                }

                if (createBorder)
                {
                    localPoints = localInnerPoints;
                    CreateBorderPolygon(ref MeshTriangles, ref triCounter, baseVert, localPoints.Count);
                    baseVert += localPoints.Count;
                }

                CreateInteriorPolygon(ref MeshTriangles, ref triCounter, baseVert, localPoints);
            }

            newMesh.Clear();
            newMesh.name = anchorInfo.name;
            newMesh.vertices = MeshVertices;
            for (int x = 0; x < UVChannelCount; x++)
            {
                switch (x)
                {
                    case 0:
                        newMesh.uv = MeshUVs[x];
                        break;
                    case 1:
                        newMesh.uv2 = MeshUVs[x];
                        break;
                    case 2:
                        newMesh.uv3 = MeshUVs[x];
                        break;
                    case 3:
                        newMesh.uv4 = MeshUVs[x];
                        break;
                    case 4:
                        newMesh.uv5 = MeshUVs[x];
                        break;
                    case 5:
                        newMesh.uv6 = MeshUVs[x];
                        break;
                    case 6:
                        newMesh.uv7 = MeshUVs[x];
                        break;
                    case 7:
                        newMesh.uv8 = MeshUVs[x];
                        break;
                }
            }
            newMesh.colors32 = MeshColors;
            newMesh.triangles = MeshTriangles;
            newMesh.normals = MeshNormals;
            newMesh.tangents = MeshTangents;

            effectMeshObject.mesh = newMesh;

            if (Colliders)
            {
                effectMeshObject.collider = AddCollider(anchorInfo, effectMeshObject);
            }
            effectMeshObjects.Add(anchorInfo, effectMeshObject);
            return effectMeshObject;
        }

        private Collider AddCollider(MRUKAnchor anchorInfo, EffectMeshObject effectMeshObject)
        {
            if (anchorInfo.VolumeBounds.HasValue)
            {
                var boxCollider = effectMeshObject.effectMeshGO.AddComponent<BoxCollider>();
                boxCollider.size = anchorInfo.VolumeBounds.Value.size;
                boxCollider.center = anchorInfo.VolumeBounds.Value.center;
                return boxCollider;
            }

            var meshCollider = effectMeshObject.effectMeshGO.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = effectMeshObject.mesh;
            meshCollider.convex = false;
            return meshCollider;
        }

        float GetSeamlessFactor(float totalWallLength, float stepSize)
        {
            float roundedTotalWallLength = Mathf.Round(totalWallLength / stepSize);
            roundedTotalWallLength = Mathf.Max(1, roundedTotalWallLength);
            return totalWallLength / roundedTotalWallLength;
        }

        EffectMeshObject CreateEffectMeshWall(MRUKAnchor anchorInfo, float totalWallLength, ref float uSpacing, float border)
        {
            if (effectMeshObjects.ContainsKey(anchorInfo))
            {
                //WallAnchor already has an EffectMeshComponent
                return null;
            }
            EffectMeshObject effectMeshObject = new();

            GameObject newGameObject = new GameObject(anchorInfo.name + Suffix);
            newGameObject.transform.SetParent(anchorInfo.transform, false);

            effectMeshObject.effectMeshGO = newGameObject;

            Mesh newMesh = new Mesh();
            var meshFilter = newGameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = newMesh;

            // only attach MeshRenderer if a material has been assigned
            if (MeshMaterial != null)
            {
                MeshRenderer newRenderer = newGameObject.AddComponent<MeshRenderer>();
                newRenderer.material = MeshMaterial;
                newRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                newRenderer.enabled = !hideMesh;
            }

            Vector2 wallScale = anchorInfo.PlaneRect.Value.size;
            float ceilingHeight = wallScale.y;

            bool createBorder = border > 0.0f;

            List<List<Vector2>> holes = new();

            Rect wallRect = anchorInfo.PlaneRect.Value;

            // Holes are not supported if a border is created, supporting this combination adds a lot of complexity
            // in dealing with the edge cases.
            if (!createBorder)
            {
                foreach (var child in anchorInfo.ChildAnchors)
                {
                    if (child.PlaneRect.HasValue)
                    {
                        bool shouldCutHoles = (child.Label & CutHoles) != 0;
                        if (!shouldCutHoles)
                        {
                            continue;
                        }
                        Vector2 relativePos = anchorInfo.transform.InverseTransformPoint(child.transform.position);

                        var childRect = child.PlaneRect.Value;
                        childRect.position += new Vector2(relativePos.x, relativePos.y);
                        List<Vector2> childOutline = new(child.PlaneBoundary2D.Count);
                        // Reverse the order for the holes, this is necessary for the hole cutting algorithm to work
                        // correctly.
                        for (int i = child.PlaneBoundary2D.Count - 1; i >= 0; i--)
                        {
                            childOutline.Add(child.PlaneBoundary2D[i] + relativePos);
                        }
                        holes.Add(childOutline);
                    }
                }
            }

            var outline = Triangulator.CreateOutline(anchorInfo.PlaneBoundary2D, holes);

            int totalVertices = outline.vertices.Count;
            if (createBorder)
            {
                totalVertices += 4;
            }

            int UVChannelCount = Math.Min(8, textureCoordinateModes.Length);

            Vector3[] MeshVertices = new Vector3[totalVertices];
            Vector2[][] MeshUVs = new Vector2[UVChannelCount][];
            for (int x = 0; x < UVChannelCount; x++)
            {
                MeshUVs[x] = new Vector2[totalVertices];
            }
            Color32[] MeshColors = new Color32[totalVertices];
            Vector3[] MeshNormals = new Vector3[totalVertices];
            Vector4[] MeshTangents = new Vector4[totalVertices];

            int vertCounter = 0;

            float seamlessScaleFactor = GetSeamlessFactor(totalWallLength, 1);

            // direction to points
            float thisSegmentLength = wallScale.x;

            Vector3 wallNorm = Vector3.forward;
            Vector4 wallTan = new Vector4(1, 0, 0, 1);

            var wallCenter = wallRect.center;

            for (int j = 0; j < outline.vertices.Count; j++)
            {
                var vert = outline.vertices[j];

                float u = vert.x - wallRect.xMin;
                float v = vert.y - wallRect.yMin;
                float innerU = vert.x < wallCenter.x ? u + border : u - border;
                float innerV = vert.y < wallCenter.y ? v + border : v - border;

                for (int x = 0; x < UVChannelCount; x++)
                {
                    float denominatorX;
                    float denominatorY;
                    float defaultSpacing = uSpacing;
                    // Determine the scaling in the V direction first, if this is set to maintain aspect
                    // ratio we need to come back to it after U scaling has been determined.
                    switch (textureCoordinateModes[x].WallV)
                    {
                        // Default to stretch in case maintain aspect ratio is set for both axes
                        default:
                        case WallTextureCoordinateModeV.STRETCH:
                            denominatorY = ceilingHeight;
                            break;
                        case WallTextureCoordinateModeV.METRIC:
                            denominatorY = 1;
                            break;
                    }
                    switch (textureCoordinateModes[x].WallU)
                    {
                        default:
                        case WallTextureCoordinateModeU.STRETCH:
                            denominatorX = totalWallLength;
                            break;
                        case WallTextureCoordinateModeU.METRIC:
                            denominatorX = 1;
                            break;
                        case WallTextureCoordinateModeU.METRIC_SEAMLESS:
                            denominatorX = seamlessScaleFactor;
                            break;
                        case WallTextureCoordinateModeU.MAINTAIN_ASPECT_RATIO:
                            denominatorX = denominatorY;
                            break;
                        case WallTextureCoordinateModeU.MAINTAIN_ASPECT_RATIO_SEAMLESS:
                            denominatorX = GetSeamlessFactor(totalWallLength, denominatorY);
                            break;
                        case WallTextureCoordinateModeU.STRETCH_SECTION:
                            denominatorX = thisSegmentLength;
                            defaultSpacing = 0;
                            break;
                    }
                    // Do another pass on V in case it has maintain aspect ratio set
                    if (textureCoordinateModes[x].WallV == WallTextureCoordinateModeV.MAINTAIN_ASPECT_RATIO)
                    {
                        denominatorY = denominatorX;
                    }

                    MeshUVs[x][vertCounter] = new Vector2((defaultSpacing + thisSegmentLength - u) / denominatorX, v / denominatorY);
                    if (createBorder)
                    {
                        MeshUVs[x][vertCounter + 4] = new Vector2((defaultSpacing + thisSegmentLength - innerU) / denominatorX, innerV / denominatorY);
                    }
                }

                MeshVertices[vertCounter] = new Vector3(u - thisSegmentLength / 2, v - ceilingHeight / 2, 0);
                MeshColors[vertCounter] = createBorder ? Color.black : Color.white;
                MeshNormals[vertCounter] = wallNorm;
                MeshTangents[vertCounter] = wallTan;

                if (createBorder)
                {
                    MeshVertices[vertCounter + 4] = new Vector3(innerU - thisSegmentLength / 2, innerV - ceilingHeight / 2, 0);
                    MeshColors[vertCounter + 4] = Color.white;
                    MeshNormals[vertCounter + 4] = wallNorm;
                    MeshTangents[vertCounter + 4] = wallTan;
                }
                vertCounter++;
            }

            uSpacing += thisSegmentLength;

            int[] MeshTriangles;
            if (createBorder)
            {
                MeshTriangles = new int[30];
                int triCounter = 0;
                CreateBorderPolygon(ref MeshTriangles, ref triCounter, 0, 4);
                CreateInteriorTriangleFan(ref MeshTriangles, ref triCounter, 4, 4);
            }
            else
            {
                var triangles = Triangulator.TriangulateMesh(outline);
                MeshTriangles = triangles.ToArray();
            }

            newMesh.Clear();
            newMesh.name = anchorInfo.name;
            newMesh.vertices = MeshVertices;
            for (int x = 0; x < UVChannelCount; x++)
            {
                switch (x)
                {
                    case 0:
                        newMesh.uv = MeshUVs[x];
                        break;
                    case 1:
                        newMesh.uv2 = MeshUVs[x];
                        break;
                    case 2:
                        newMesh.uv3 = MeshUVs[x];
                        break;
                    case 3:
                        newMesh.uv4 = MeshUVs[x];
                        break;
                    case 4:
                        newMesh.uv5 = MeshUVs[x];
                        break;
                    case 5:
                        newMesh.uv6 = MeshUVs[x];
                        break;
                    case 6:
                        newMesh.uv7 = MeshUVs[x];
                        break;
                    case 7:
                        newMesh.uv8 = MeshUVs[x];
                        break;
                }
            }
            newMesh.colors32 = MeshColors;
            newMesh.triangles = MeshTriangles;
            newMesh.normals = MeshNormals;
            newMesh.tangents = MeshTangents;

            effectMeshObject.mesh = newMesh;

            if (Colliders)
            {
                effectMeshObject.collider = AddCollider(anchorInfo, effectMeshObject);
            }
            effectMeshObjects.Add(anchorInfo, effectMeshObject);
            return effectMeshObject;
        }

        async void CreateGlobalMeshObject(MRUKAnchor globalMeshAnchor)
        {
            if (!globalMeshAnchor)
            {
                Debug.LogWarning("No global mesh was found in the current room");
                return;
            }
            if (effectMeshObjects.ContainsKey(globalMeshAnchor))
            {
                //Anchor already has an EffectMeshComponent
                return;
            }

            var effectMeshObject = new EffectMeshObject();

            var globalMeshGO = new GameObject(globalMeshAnchor.name + Suffix, typeof(MeshFilter), typeof(MeshRenderer));
            globalMeshGO.transform.SetParent(globalMeshAnchor.transform, false);
            effectMeshObject.effectMeshGO = globalMeshGO;

            if (globalMeshAnchor.GlobalMesh == null)
            {
                globalMeshAnchor.Anchor.TryGetComponent(out OVRLocatable locatable);
                await locatable.SetEnabledAsync(true);

                if (!locatable.TryGetSceneAnchorPose(out var pose))
                    return;

                var pos = pose.ComputeWorldPosition(Camera.main);
                var rot = pose.ComputeWorldRotation(Camera.main);

                globalMeshGO.transform.SetPositionAndRotation(pos.Value, rot.Value);
                globalMeshAnchor.GlobalMesh = globalMeshAnchor.LoadGlobalMeshTriangles();
            }

            globalMeshAnchor.GlobalMesh.RecalculateNormals();
            var trimesh = globalMeshAnchor.GlobalMesh;

            globalMeshGO.GetComponent<MeshFilter>().sharedMesh = trimesh;

            if (Colliders)
            {
                var meshCollider = globalMeshGO.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = trimesh;
                effectMeshObject.collider = meshCollider;
            }

            var renderer = globalMeshGO.GetComponent<MeshRenderer>();
            if (MeshMaterial != null)
            {
                renderer.material = MeshMaterial;
            }
            renderer.enabled = !hideMesh;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            effectMeshObject.mesh = trimesh;
            effectMeshObjects.Add(globalMeshAnchor, effectMeshObject);
        }

        public void SetEffectObjectsParent(Transform newParent)
        {
            foreach (var kv in effectMeshObjects)
            {
                kv.Value.effectMeshGO.transform.SetParent(newParent);
            }
        }
    }
}
