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
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Meta.XR.MRUtilityKit
{
    // tool for swapping scene prefabs with standardized unity objects
    [Feature(Feature.Scene)]
    public class AnchorPrefabSpawner : MonoBehaviour
    {
        public enum ScalingMode
        {
            /// Stretch each axis to exactly match the size of the Plane/Volume.
            Stretch,
            /// Scale each axis by the same amount to maintain the correct aspect ratio.
            UniformScaling,
            /// Scale the X and Z axes uniformly but the Y scale can be different.
            UniformXZScale,
            /// Don't perform any scaling.
            NoScaling
        }

        public enum AlignMode
        {
            /// For volumes align to the base, for planes align to the center.
            Automatic,
            /// Align the bottom of the prefab with the bottom of the volume or plane
            Bottom,
            /// Align the center of the prefab with the center of the volume or plane
            Center,
            /// Don't add any local offset to the prefab.
            NoAlignment
        }

        public enum SelectionMode
        {
            // Randomly choose a prefab from the list every time
            Random,

            // Chose the prefab the has the smallest difference between its size with the one of the anchor
            ClosestSize
        }

        [Serializable]
        public struct AnchorPrefabGroup
        {
            [FormerlySerializedAs("_include")]
            [SerializeField, Tooltip("Anchors to include.")]
            public MRUKAnchor.SceneLabels Labels;
            [SerializeField, Tooltip("Prefab(s) to spawn (randomly chosen from list.)")]
            public List<GameObject> Prefabs;

            [SerializeField]
            [Tooltip("The logic that determines what prefab to chose when spawning the relative labels' game objects")]
            public SelectionMode PrefabSelection;
            [SerializeField, Tooltip("When enabled, the prefab will be rotated to try and match the aspect ratio of the volume as closely as possible. This is most useful for long and thin volumes, keep this disabled for objects with an aspect ratio close to 1:1. Only applies to volumes.")]
            public bool MatchAspectRatio;
            [SerializeField, Tooltip("When calculate facing direction is enabled the prefab will be rotated to face away from the closest wall. If match aspect ratio is also enabled then that will take precedence and it will be constrained to a choice between 2 directions only.Only applies to volumes.")]
            public bool CalculateFacingDirection;
            [SerializeField, Tooltip("Set what scaling mode to apply to the prefab. By default the prefab will be stretched to fit the size of the plane/volume. But in some cases this may not be desirable and can be customized here.")]
            public ScalingMode Scaling;
            [SerializeField, Tooltip("Spawn new object at the center, top or bottom of the anchor.")]
            public AlignMode Alignment;
            [SerializeField, Tooltip("Don't analyze prefab, just assume a default scale of 1.")]
            public bool IgnorePrefabSize;
        }

        [Tooltip("When the scene data is loaded, this controls what room(s) the prefabs will spawn in.")]
        public MRUK.RoomFilter SpawnOnStart = MRUK.RoomFilter.CurrentRoomOnly;

        [Tooltip("If enabled, updates on scene elements such as rooms and anchors will be handled by this class")]
        public bool TrackUpdates = true;

        public List<AnchorPrefabGroup> PrefabsToSpawn;

        [Tooltip("Specify a seed value for consistent prefab selection (0 = Random).")]
        public int SeedValue;

        [Obsolete("Use AnchorPrefabSpawnerObjects property instead. This property is inefficient because it will generate a new list each time it is accessed")]
        public List<GameObject> SpawnedPrefabs => new(AnchorPrefabSpawnerObjects.Values);

        private MRUK.SceneTrackingSettings SceneTrackingSettings;

        private static readonly string Suffix = "(PrefabSpawner Clone)";
        private Random _random;

        public Dictionary<MRUKAnchor, GameObject> AnchorPrefabSpawnerObjects { get; private set; } = new();

        [Obsolete("Event onPrefabSpawned will be deprecated in a future version")]
        public UnityEvent onPrefabSpawned;
        private void Start()
        {
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadAnchorPrefabSpawner).Send();
#endif
            if (MRUK.Instance is null) return;
            SceneTrackingSettings.UnTrackedRooms = new();
            SceneTrackingSettings.UnTrackedAnchors = new();

            MRUK.Instance.RegisterSceneLoadedCallback(() =>
            {
                if (SpawnOnStart == MRUK.RoomFilter.None) return;

                switch (SpawnOnStart)
                {
                    case MRUK.RoomFilter.CurrentRoomOnly:
                        SpawnPrefabs(MRUK.Instance.GetCurrentRoom());
                        break;
                    case MRUK.RoomFilter.AllRooms:
                        SpawnPrefabs();
                        break;
                }
            });

            if (!TrackUpdates) return;
        }

        private void OnEnable()
        {
            if (MRUK.Instance)
            {
                MRUK.Instance.RoomCreatedEvent.AddListener(ReceiveCreatedRoom);
                MRUK.Instance.RoomRemovedEvent.AddListener(ReceiveRemovedRoom);
            }
        }

        private void OnDisable()
        {
            if (MRUK.Instance)
            {
                MRUK.Instance.RoomCreatedEvent.RemoveListener(ReceiveCreatedRoom);
                MRUK.Instance.RoomRemovedEvent.RemoveListener(ReceiveRemovedRoom);
            }
        }

        private void ReceiveRemovedRoom(MRUKRoom room)
        {
            // there is no check on ```SceneTrackingSettings.TrackUpdates``` when removing a room.
            ClearPrefabs(room);
            UnRegisterAnchorUpdates(room);
        }

        private void UnRegisterAnchorUpdates(MRUKRoom room)
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

        private void ReceiveAnchorUpdatedCallback(MRUKAnchor anchorInfo)
        {
            // only update the anchor when we track updates
            // &
            // only create when the anchor or parent room is tracked
            if (SceneTrackingSettings.UnTrackedRooms.Contains(anchorInfo.Room) ||
                SceneTrackingSettings.UnTrackedAnchors.Contains(anchorInfo) ||
                !TrackUpdates)
            {
                return;
            }
            ClearPrefab(anchorInfo);
            SpawnPrefab(anchorInfo);
        }

        private void ReceiveAnchorRemovedCallback(MRUKAnchor anchorInfo)
        {
            // there is no check on ```SceneTrackingSettings.TrackUpdates``` when removing an anchor.
            ClearPrefab(anchorInfo);
        }

        private void ReceiveAnchorCreatedEvent(MRUKAnchor anchorInfo)
        {
            // only create the anchor when we track updates
            // &
            // only create when the parent room is tracked
            if (SceneTrackingSettings.UnTrackedRooms.Contains(anchorInfo.Room) ||
                !TrackUpdates)
            {
                return;
            }
            SpawnPrefab(anchorInfo);
        }

        private void ReceiveCreatedRoom(MRUKRoom room)
        {
            //only create the room when we track room updates
            if (TrackUpdates &&
                SpawnOnStart == MRUK.RoomFilter.AllRooms)
            {
                SpawnPrefabs(room);
                RegisterAnchorUpdates(room);
            }

        }

        /// <summary>
        /// Clears all the spawned gameobjects from this AnchorPrefabSpawner in the given room
        /// </summary>
        /// <param name="room">The room from where to remove all the spawned objects</param>
        public void ClearPrefabs(MRUKRoom room)
        {
            List<MRUKAnchor> anchorsToRemove = new();
            foreach (var kv in AnchorPrefabSpawnerObjects)
            {
                if (kv.Key.Room != room)
                {
                    continue;
                }
                ClearPrefab(kv.Value);
                anchorsToRemove.Add(kv.Key);
            }

            foreach (var anchor in anchorsToRemove)
            {
                AnchorPrefabSpawnerObjects.Remove(anchor);
            }
            SceneTrackingSettings.UnTrackedRooms.Add(room);
        }

        private void ClearPrefab(GameObject go)
        {
            Destroy(go);
        }

        /// <summary>
        /// Clears the gameobject associated with the anchor. Useful when receiving an event that a
        /// specific anchor has been removed
        /// </summary>
        /// <param name="anchorInfo">The anchor reference</param>
        public void ClearPrefab(MRUKAnchor anchorInfo)
        {
            if (!AnchorPrefabSpawnerObjects.ContainsKey(anchorInfo))
            {
                return;
            }
            ClearPrefab(AnchorPrefabSpawnerObjects[anchorInfo]);
            AnchorPrefabSpawnerObjects.Remove(anchorInfo);

            SceneTrackingSettings.UnTrackedAnchors.Add(anchorInfo);
        }

        /// <summary>
        /// Clears all the gameobjects created with the PrefabSpawner
        /// </summary>
        public void ClearPrefabs()
        {
            foreach (var kv in AnchorPrefabSpawnerObjects)
            {
                ClearPrefab(kv.Value);
            }
            AnchorPrefabSpawnerObjects.Clear();
        }

        private Bounds RotateVolumeBounds(Bounds bounds, int rotation)
        {
            var center = bounds.center;
            var size = bounds.size;
            switch (rotation)
            {
                default:
                    return bounds;
                case 1:
                    return new Bounds(new Vector3(-center.y, center.x, center.z), new Vector3(size.y, size.x, size.z));
                case 2:
                    return new Bounds(new Vector3(-center.x, -center.x, center.z), size);
                case 3:
                    return new Bounds(new Vector3(center.y, -center.x, center.z), new Vector3(size.y, size.x, size.z));
            }
        }


        /// <summary>
        /// Spawns prefabs according to the settings
        /// </summary>
        /// <param name="clearPrefabs">Clear already existing prefabs before.</param>
        private void SpawnPrefabs(bool clearPrefabs = true)
        {
            // Perform a cleanup if necessary
            if (clearPrefabs)
            {
                ClearPrefabs();
            }

            foreach (var room in MRUK.Instance.Rooms)
            {
                SpawnPrefabsInternal(room);
            }
#pragma warning disable CS0618 // Type or member is obsolete
            onPrefabSpawned?.Invoke();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Creates gameobjects for the given room.
        /// </summary>
        /// <param name="room">The room reference</param>
        /// <param name="clearPrefabs">clear all before adding them again</param>
        public void SpawnPrefabs(MRUKRoom room, bool clearPrefabs = true)
        {
            // Perform a cleanup if necessary
            if (clearPrefabs)
            {
                ClearPrefabs();
            }
            SpawnPrefabsInternal(room);
#pragma warning disable CS0618 // Type or member is obsolete
            onPrefabSpawned?.Invoke();
#pragma warning restore CS0618 // Type or member is obsolete
        }


        private void SpawnPrefab(MRUKAnchor anchorInfo)
        {
            var prefabToCreate = LabelToPrefab(anchorInfo.Label, anchorInfo, out var prefabGroup);
            if (prefabToCreate == null)
            {
                return;
            }

            if (AnchorPrefabSpawnerObjects.ContainsKey(anchorInfo))
            {
                Debug.LogWarning("Anchor already associated with a gameobject spawned from this AnchorPrefabSpawner");
                return;
            }

            Bounds? prefabBounds = prefabGroup.IgnorePrefabSize ? null : Utilities.GetPrefabBounds(prefabToCreate);

            // Create a new instance of the prefab
            // We will translate location and scale differently depending on the label.
            var prefab = Instantiate(prefabToCreate, anchorInfo.transform);
            prefab.name = prefabToCreate.name + Suffix;
            prefab.transform.parent = anchorInfo.transform;

            GridSliceResizer resizer = prefab.GetComponentInChildren<GridSliceResizer>(true);
            if (!prefabBounds.HasValue && resizer)
            {
                prefabBounds = resizer.OriginalMesh.bounds;
            }
            Vector3 prefabSize = prefabBounds?.size ?? Vector3.one;

            if (anchorInfo.VolumeBounds.HasValue)
            {
                int cardinalAxisIndex = 0;
                if (prefabGroup.CalculateFacingDirection && !prefabGroup.MatchAspectRatio)
                {
                    anchorInfo.Room.GetDirectionAwayFromClosestWall(anchorInfo, out cardinalAxisIndex);
                }
                Bounds volumeBounds = RotateVolumeBounds(anchorInfo.VolumeBounds.Value, cardinalAxisIndex);

                Vector3 volumeSize = volumeBounds.size;
                Vector3 scale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y, volumeSize.y / prefabSize.z);  // flipped z and y to correct orientation
                if (prefabGroup.MatchAspectRatio)
                {
                    Vector3 prefabSizeRotated = new Vector3(prefabSize.z, prefabSize.y, prefabSize.x);
                    Vector3 scaleRotated = new Vector3(volumeSize.x / prefabSizeRotated.x, volumeSize.z / prefabSizeRotated.y, volumeSize.y / prefabSizeRotated.z);

                    float distortion = Mathf.Max(scale.x, scale.z) / Mathf.Min(scale.x, scale.z);
                    float distortionRotated = Mathf.Max(scaleRotated.x, scaleRotated.z) / Mathf.Min(scaleRotated.x, scaleRotated.z);

                    bool rotateToMatchAspectRatio = distortion > distortionRotated;
                    if (rotateToMatchAspectRatio)
                    {
                        cardinalAxisIndex = 1;
                    }
                    if (prefabGroup.CalculateFacingDirection)
                    {
                        anchorInfo.Room.GetDirectionAwayFromClosestWall(anchorInfo, out cardinalAxisIndex, rotateToMatchAspectRatio ? new List<int> { 0, 2 } : new List<int> { 1, 3 });
                    }
                    if (cardinalAxisIndex != 0)
                    {
                        // Update the volume bounds if necessary
                        volumeBounds = RotateVolumeBounds(anchorInfo.VolumeBounds.Value, cardinalAxisIndex);
                        volumeSize = volumeBounds.size;
                        scale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y, volumeSize.y / prefabSize.z);  // flipped z and y to correct orientation
                    }
                }

                switch (prefabGroup.Scaling)
                {
                    case ScalingMode.UniformScaling:
                        scale.x = scale.y = scale.z = Mathf.Min(scale.x, scale.y, scale.z);
                        break;
                    case ScalingMode.UniformXZScale:
                        scale.x = scale.z = Mathf.Min(scale.x, scale.z);
                        break;
                    case ScalingMode.NoScaling:
                        scale = Vector3.one;
                        break;
                }

                Vector3 prefabPivot = new();
                Vector3 volumePivot = new();

                switch (prefabGroup.Alignment)
                {
                    case AlignMode.Automatic:
                    case AlignMode.Bottom:
                        if (prefabBounds.HasValue)
                        {
                            var center = prefabBounds.Value.center;
                            var min = prefabBounds.Value.min;
                            prefabPivot = new Vector3(center.x, center.z, min.y);
                        }
                        volumePivot = volumeBounds.center;
                        volumePivot.z = volumeBounds.min.z;
                        break;
                    case AlignMode.Center:
                        if (prefabBounds.HasValue)
                        {
                            var center = prefabBounds.Value.center;
                            prefabPivot = new Vector3(center.x, center.z, center.y);
                        }
                        volumePivot = volumeBounds.center;
                        break;
                    case AlignMode.NoAlignment:
                        break;
                }
                prefabPivot.x *= scale.x;
                prefabPivot.y *= scale.z;
                prefabPivot.z *= scale.y;
                prefab.transform.localPosition = volumePivot - prefabPivot;
                prefab.transform.localRotation = Quaternion.Euler((cardinalAxisIndex - 1) * 90, -90, -90);// scene geometry is unusual, we need to swap Y/Z for a more standard prefab structure
                prefab.transform.localScale = scale;
            }
            else if (anchorInfo.PlaneRect.HasValue)
            {
                Vector2 planeSize = anchorInfo.PlaneRect.Value.size;
                Vector2 scale = new Vector2(planeSize.x / prefabSize.x, planeSize.y / prefabSize.y);

                switch (prefabGroup.Scaling)
                {
                    case ScalingMode.UniformScaling:
                    case ScalingMode.UniformXZScale:
                        scale.x = scale.y = Mathf.Min(scale.x, scale.y);
                        break;
                    case ScalingMode.NoScaling:
                        scale = Vector2.one;
                        break;
                }

                Vector2 planePivot = new();
                Vector2 prefabPivot = new();
                switch (prefabGroup.Alignment)
                {
                    case AlignMode.Automatic:
                    case AlignMode.Center:
                        prefabPivot = prefabBounds?.center ?? Vector3.zero;
                        planePivot = anchorInfo.PlaneRect.Value.center;
                        break;
                    case AlignMode.Bottom:
                        if (prefabBounds.HasValue)
                        {
                            var center = prefabBounds.Value.center;
                            var min = prefabBounds.Value.min;
                            prefabPivot = new Vector3(center.x, min.y);
                        }
                        planePivot = anchorInfo.PlaneRect.Value.center;
                        planePivot.y = anchorInfo.PlaneRect.Value.min.y;
                        break;
                    case AlignMode.NoAlignment:
                        break;
                }
                prefabPivot.Scale(scale);
                prefab.transform.localPosition = new Vector3(planePivot.x - prefabPivot.x, planePivot.y - prefabPivot.y, 0);
                prefab.transform.localRotation = Quaternion.identity;
                prefab.transform.localScale = new Vector3(scale.x, scale.y, 0.5f * (scale.x + scale.y));
            }

            AnchorPrefabSpawnerObjects.Add(anchorInfo, prefab);
        }

        private void SpawnPrefabsInternal(MRUKRoom room)
        {
            InitializeRandom();
            foreach (var anchor in room.Anchors)
            {
                SpawnPrefab(anchor);
            }
        }

        private GameObject LabelToPrefab(MRUKAnchor.SceneLabels labels, MRUKAnchor anchor,
            out AnchorPrefabGroup prefabGroup)
        {
            foreach (AnchorPrefabGroup item in PrefabsToSpawn)
            {
                if ((item.Labels & labels) == 0 || item.Prefabs == null || item.Prefabs.Count == 0)
                    continue;

                GameObject prefabObjectToSpawn = null;
                var randomIndex = 0;
                switch (item.PrefabSelection)
                {
                    case SelectionMode.Random:
                        randomIndex = _random.Next(0, item.Prefabs.Count);
                        break;
                    case SelectionMode.ClosestSize:
                        if (anchor.VolumeBounds.HasValue)
                        {
                            var anchorVolume = anchor.VolumeBounds.Value.size.x * anchor.VolumeBounds.Value.size.y *
                                               anchor.VolumeBounds.Value.size.z;
                            var anchorAverageSide = MathF.Pow(anchorVolume, 1f / 3.0f); // cubic root
                            var closestSizeDifference = Mathf.Infinity;
                            for (var i = 0; i < item.Prefabs.Count; i++)
                            {
                                var bounds = Utilities.GetPrefabBounds(item.Prefabs[i]);
                                if (!bounds.HasValue)
                                    continue;
                                var prefabVolume = bounds.Value.size.x * bounds.Value.size.y * bounds.Value.size.z;
                                var prefabAverageSide = Mathf.Pow(prefabVolume, 1.0f / 3.0f); // cubic root
                                var sizeDifference = Mathf.Abs(anchorAverageSide - prefabAverageSide);
                                if (sizeDifference >= closestSizeDifference)
                                    continue;
                                closestSizeDifference = sizeDifference;
                                randomIndex = i;
                            }
                        }

                        break;
                }

                prefabObjectToSpawn = item.Prefabs[randomIndex];
                prefabGroup = item;


                return prefabObjectToSpawn;
            }
            prefabGroup = new();
            return null;
        }

        private void InitializeRandom()
        {
            if (SeedValue == 0) SeedValue = Environment.TickCount;

            _random = new Random(SeedValue);
        }

        private void OnDestroy()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            onPrefabSpawned.RemoveAllListeners();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
