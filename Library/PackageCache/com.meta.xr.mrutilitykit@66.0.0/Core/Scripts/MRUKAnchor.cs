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
using Unity.Collections;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    [Feature(Feature.Scene)]
    public class MRUKAnchor : MonoBehaviour
    {
        [Flags]
        public enum SceneLabels
        {
            FLOOR = 1 << OVRSemanticLabels.Classification.Floor,
            CEILING = 1 << OVRSemanticLabels.Classification.Ceiling,
            WALL_FACE = 1 << OVRSemanticLabels.Classification.WallFace,
            TABLE = 1 << OVRSemanticLabels.Classification.Table,
            COUCH = 1 << OVRSemanticLabels.Classification.Couch,
            DOOR_FRAME = 1 << OVRSemanticLabels.Classification.DoorFrame,
            WINDOW_FRAME = 1 << OVRSemanticLabels.Classification.WindowFrame,
            OTHER = 1 << OVRSemanticLabels.Classification.Other,
            STORAGE = 1 << OVRSemanticLabels.Classification.Storage,
            BED = 1 << OVRSemanticLabels.Classification.Bed,
            SCREEN = 1 << OVRSemanticLabels.Classification.Screen,
            LAMP = 1 << OVRSemanticLabels.Classification.Lamp,
            PLANT = 1 << OVRSemanticLabels.Classification.Plant,
            WALL_ART = 1 << OVRSemanticLabels.Classification.WallArt,
            GLOBAL_MESH = 1 << OVRSemanticLabels.Classification.SceneMesh,
            INVISIBLE_WALL_FACE = 1 << OVRSemanticLabels.Classification.InvisibleWallFace,
        };

        /// <see cref="OVRSemanticLabels.DeprecationMessage"/>
        [Obsolete("Use '" + nameof(Label) + "' instead.")]
        public List<string> AnchorLabels => Utilities.SceneLabelsEnumToList(Label);

        public SceneLabels Label { get; internal set; }

        public Rect? PlaneRect;
        public Bounds? VolumeBounds;
        public List<Vector2> PlaneBoundary2D;

        // Link to the tracked object
        public OVRAnchor Anchor = OVRAnchor.Null;

        // Link to the parent room object. Gets set on construction.
        public MRUKRoom Room;

        // these are populated via MRUKRoom.CalculateHierarchyReferences
        // (shouldn't be manually set in Inspector)
        public MRUKAnchor ParentAnchor;
        public List<MRUKAnchor> ChildAnchors = new List<MRUKAnchor>();

        [Obsolete("Use PlaneRect.HasValue instead.")]
        public bool HasPlane => PlaneRect != null;
        [Obsolete("Use VolumeBounds.HasValue instead.")]
        public bool HasVolume => VolumeBounds != null;

        public Mesh GlobalMesh
        {
            get
            {
                if (!_globalMesh)
                {
                    _globalMesh = LoadGlobalMeshTriangles();
                }
                return _globalMesh;
            }
            set => _globalMesh = value;
        }
        Mesh _globalMesh;


        /// <summary>
        /// We prefer to avoid colliders and Physics.Raycast because: <br/>
        /// 1. It requires tags/layers to filter out Scene API object hits from general raycasts. This can be intrusive to a dev's pipeline by altering their project settings <br/>
        /// 2. It requires specific Plane & Volume prefabs that MRUK instantiates with colliders as children <br/>
        /// 3. It seems like overkill, since we already "know" where all the Scene API primitives are; no need to raycast everywhere to find them <br/>
        /// Instead, we use Plane.Raycast and other methods to see if the ray has hit the surface of the object <br/>
        /// </summary>
        public bool Raycast(Ray ray, float maxDist, out RaycastHit hitInfo)
        {
            var localOrigin = transform.InverseTransformPoint(ray.origin);
            var localDirection = transform.InverseTransformDirection(ray.direction);
            Ray localRay = new Ray(localOrigin, localDirection);
            bool hitPlane = RaycastPlane(localRay, maxDist, out RaycastHit hitInfoPlane);
            bool hitVolume = RaycastVolume(localRay, maxDist, out RaycastHit hitInfoVolume);
            if (hitPlane && hitVolume)
            {
                // If the ray hit both the plane and the volume then pick whichever is closest
                if (hitInfoPlane.distance < hitInfoVolume.distance)
                {
                    hitInfo = hitInfoPlane;
                    return true;
                }
                else
                {
                    hitInfo = hitInfoVolume;
                    return true;
                }
            }
            if (hitPlane)
            {
                hitInfo = hitInfoPlane;
                return true;
            }
            if (hitVolume)
            {
                hitInfo = hitInfoVolume;
                return true;
            }
            hitInfo = new();
            return false;
        }

        public bool IsPositionInBoundary(Vector2 position)
        {
            if (PlaneBoundary2D == null || PlaneBoundary2D.Count == 0)
            {
                return false;
            }

            return Utilities.IsPositionInPolygon(position, PlaneBoundary2D);
        }

        public void AddChildReference(MRUKAnchor childObj)
        {
            if (childObj != null)
            {
                ChildAnchors.Add(childObj);
            }
        }

        public void ClearChildReferences()
        {
            ChildAnchors.Clear();
        }

        public float GetDistanceToSurface(Vector3 position) =>
            GetClosestSurfacePosition(position, out _);

        public float GetClosestSurfacePosition(Vector3 testPosition, out Vector3 closestPosition) =>
            GetClosestSurfacePosition(testPosition, out closestPosition, out _);

        public float GetClosestSurfacePosition(Vector3 testPosition, out Vector3 closestPosition, out Vector3 normal)
        {
            float candidateDistance = Mathf.Infinity;
            closestPosition = Vector3.zero;
            normal = Vector3.zero;

            if (VolumeBounds.HasValue)
            {
                Vector3 localPosition = transform.InverseTransformPoint(testPosition);
                if (VolumeBounds.Value.Contains(localPosition))
                {
                    // in this case, we need custom math not provided by Bounds.ClosestPoint, which returns the original query position if inside
                    Vector3 halfScale = VolumeBounds.Value.size * 0.5f;
                    localPosition += Vector3.forward * halfScale.z;
                    float minXdist = halfScale.x - Mathf.Abs(localPosition.x);
                    float closestX = localPosition.x + minXdist * Mathf.Sign(localPosition.x);
                    float minYdist = halfScale.y - Mathf.Abs(localPosition.y);
                    float closestY = localPosition.y + minYdist * Mathf.Sign(localPosition.y);
                    float minZdist = halfScale.z - Mathf.Abs(localPosition.z);
                    float closestZ = localPosition.z + minZdist * Mathf.Sign(localPosition.z);
                    if (minXdist < minYdist)
                    {
                        closestPosition = minXdist < minZdist ? new Vector3(closestX, localPosition.y, localPosition.z) : new Vector3(localPosition.x, localPosition.y, closestZ);
                    }
                    else
                    {
                        closestPosition = minYdist < minZdist ? new Vector3(localPosition.x, closestY, localPosition.z) : new Vector3(localPosition.x, localPosition.y, closestZ);
                    }

                    closestPosition = transform.TransformPoint(closestPosition - Vector3.forward * halfScale.z);

                    candidateDistance = -Mathf.Min(Mathf.Min(minXdist, minYdist), minZdist);
                }
                else
                {
                    closestPosition = VolumeBounds.Value.ClosestPoint(localPosition);
                    closestPosition = transform.TransformPoint(closestPosition);
                    candidateDistance = Vector3.Distance(closestPosition, testPosition);
                }
            }
            else if (PlaneRect.HasValue)
            {
                Vector2 planeScale = PlaneRect.Value.size;

                Vector3 toPos = testPosition - transform.position;
                Vector3 localX = Vector3.Project(toPos, transform.right);
                Vector3 localY = Vector3.Project(toPos, transform.up);
                float x = Mathf.Min(0.5f * planeScale.x, localX.magnitude);
                float y = Mathf.Min(0.5f * planeScale.y, localY.magnitude);
                closestPosition = transform.position + localX.normalized * x + localY.normalized * y;
                candidateDistance = Vector3.Distance(closestPosition, testPosition);
            }

            return Mathf.Abs(candidateDistance);
        }

        public Vector3 GetAnchorCenter()
        {
            if (VolumeBounds.HasValue)
            {
                return transform.TransformPoint(VolumeBounds.Value.center);
            }
            return transform.position;
        }

        /// <summary>
        /// A convenience function to get a transform-friendly Vector3 size of an anchor.
        /// If you'd like the size of the quad or volume instead, use <seealso cref="MRUKAnchor.PlaneRect"/> or <seealso cref="MRUKAnchor.VolumeBounds"/>
        /// </summary>
        [Obsolete("Use PlaneRect and VolumeBounds properties instead")]
        public Vector3 GetAnchorSize()
        {
            Vector3 returnSize = Vector3.one;

            if (HasPlane)
            {
                returnSize = new Vector3(PlaneRect.Value.size.x, PlaneRect.Value.size.y, 1);
            }
            // prioritize the volume's size, since that is likely the desired value
            if (HasVolume)
            {
                returnSize = VolumeBounds.Value.size;
            }

            return returnSize;
        }

        bool RaycastPlane(Ray localRay, float maxDist, out RaycastHit hitInfo)
        {
            hitInfo = new RaycastHit();

            if (!PlaneRect.HasValue)
            {
                return false;
            }

            // Early rejection if surface isn't facing raycast
            if (localRay.direction.z >= 0)
            {
                return false;
            }
            Plane plane = new Plane(Vector3.forward, 0);
            if (plane.Raycast(localRay, out float entryDistance) && entryDistance < maxDist)
            {
                // A Unity Plane is infinite, so we still need to calculate if the impact point is within the dimensions
                Vector3 localImpactPos = localRay.GetPoint(entryDistance);
                if (IsPositionInBoundary(new Vector2(localImpactPos.x, localImpactPos.y)))
                {
                    // WARNING: the outgoing RaycastHit object does NOT have collider info, so don't query for it
                    // (since this raycast method doesn't involve colliders
                    hitInfo.point = transform.TransformPoint(localImpactPos);
                    hitInfo.normal = transform.forward;
                    hitInfo.distance = entryDistance;
                    return true;
                }
            }
            return false;
        }

        bool RaycastVolume(Ray localRay, float maxDist, out RaycastHit hitInfo)
        {
            // Use the slab method to determine if the ray intersects with the bounding box
            // https://education.siggraph.org/static/HyperGraph/raytrace/rtinter3.htm
            hitInfo = new RaycastHit();
            if (!VolumeBounds.HasValue)
            {
                return false;
            }
            int hitAxis = 0;
            float distFar = Mathf.Infinity;
            float distNear = -Mathf.Infinity;
            Bounds volume = VolumeBounds.Value;
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(localRay.direction[i]) > Mathf.Epsilon)
                {
                    float dist1 = (volume.min[i] - localRay.origin[i]) / localRay.direction[i];
                    float dist2 = (volume.max[i] - localRay.origin[i]) / localRay.direction[i];
                    if (dist1 > dist2)
                    {
                        // Swap as dist1 is the intersection with the plane
                        (dist1, dist2) = (dist2, dist1);
                    }
                    if (dist1 > distNear)
                    {
                        distNear = dist1;
                        hitAxis = i;
                    }
                    if (dist2 < distFar)
                    {
                        distFar = dist2;
                    }
                }
                else
                {
                    // The ray is parallel to the plane so no intersection
                    if (localRay.origin[i] < volume.min[i] || localRay.origin[i] > volume.max[i])
                    {
                        // No intersections
                        distNear = Mathf.Infinity;
                        break;
                    }
                }
            }
            if (distNear >= 0 && distNear <= distFar && distNear < maxDist)
            {
                Vector3 impactPos = localRay.GetPoint(distNear);
                Vector3 impactNormal = Vector3.zero;
                impactNormal[hitAxis] = localRay.direction[hitAxis] > 0 ? -1 : 1;
                // WARNING: the outgoing RaycastHit object does NOT have collider info, so don't query for it
                // (since this raycast method doesn't involve colliders
                // Transform the result back into world space
                hitInfo.point = transform.TransformPoint(impactPos);
                hitInfo.normal = transform.TransformDirection(impactNormal);
                hitInfo.distance = distNear;
                return true;
            }
            return false;
        }

        public Vector3[] GetBoundsFaceCenters()
        {
            if (VolumeBounds.HasValue)
            {
                Vector3[] centers = new Vector3[6];
                Vector3 scale = VolumeBounds.Value.size;
                // anchor transform.position is at top of volume
                Vector3 cubeCenter = transform.position - transform.forward * scale.z * 0.5f;

                centers[0] = transform.position;
                centers[1] = cubeCenter - transform.forward * scale.z * 0.5f;
                centers[2] = cubeCenter + transform.right * scale.x * 0.5f;
                centers[3] = cubeCenter - transform.right * scale.x * 0.5f;
                centers[4] = cubeCenter + transform.up * scale.y * 0.5f;
                centers[5] = cubeCenter - transform.up * scale.y * 0.5f;
                return centers;
            }

            if (PlaneRect.HasValue)
            {
                Vector3[] centers = new Vector3[1];
                centers[0] = transform.position;
                return centers;
            }

            return null;
        }

        /// <summary>
        /// Test if a position is inside of this volume (couch, desk, etc.)
        /// </summary>
        public bool IsPositionInVolume(Vector3 worldPosition, bool testVerticalBounds, float distanceBuffer = 0.0f)
        {
            if (!VolumeBounds.HasValue) return false;

            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            var bounds = VolumeBounds.Value;
            bounds.Expand(distanceBuffer);
            if (testVerticalBounds)
            {
                return bounds.Contains(localPosition);
            }
            else
            {
                return (localPosition.x >= bounds.min.x) && (localPosition.x <= bounds.max.x)
                && (localPosition.z >= bounds.min.z) && (localPosition.z <= bounds.max.z);
            }
        }

        public Mesh LoadGlobalMeshTriangles()
        {
            if (!HasAnyLabel(SceneLabels.GLOBAL_MESH))
                return null; // for now only global mesh is supported
            Anchor.TryGetComponent(out OVRTriangleMesh mesh);
            var trimesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            if (!mesh.TryGetCounts(out var vcount, out var tcount)) return trimesh;
            using var vs = new NativeArray<Vector3>(vcount, Allocator.Temp);
            using var ts = new NativeArray<int>(tcount * 3, Allocator.Temp);

            if (!mesh.TryGetMesh(vs, ts)) return trimesh;

            trimesh.SetVertices(vs);
            trimesh.SetIndices(ts, MeshTopology.Triangles, 0, true, 0);

            return trimesh;
        }

        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public bool HasLabel(string label) => HasAnyLabel(Utilities.StringLabelToEnum(label));

        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public bool HasAnyLabel(List<string> labels) => HasAnyLabel(Utilities.StringLabelsToEnum(labels));

        /// <summary>
        /// See if an anchor contains any of the SceneLabels passed as a parameter.
        /// </summary>
        public bool HasAnyLabel(SceneLabels labelFlags) => (Label & labelFlags) != 0;

        internal void UpdateAnchor(Data.AnchorData newData)
        {
            PlaneBoundary2D = newData.PlaneBoundary2D;
            PlaneRect = Utilities.GetPlaneRectFromAnchorData(newData);
            VolumeBounds = Utilities.GetVolumeBoundsFromAnchorData(newData);
            GlobalMesh = Utilities.GetGlobalMeshFromAnchorData(newData);
            Label = Utilities.StringLabelsToEnum(newData.SemanticClassifications);
        }

        /// <see cref="OVRSemanticLabels.DeprecationMessage"/>
        [Obsolete("Use '" + nameof(Label) + "' instead.")]
        public SceneLabels GetLabelsAsEnum() => Label;

        /// <summary>
        /// Compares the current MRUKAnchor object with another Data.AnchorData object for equality.
        /// </summary>
        /// <param name="anchorData">The Data.AnchorData object to compare with.</param>
        /// <returns>True if both objects are equal, false otherwise.</returns>
        public bool Equals(Data.AnchorData anchorData)
        {
            return Anchor == anchorData.Anchor &&
                   PlaneRect == Utilities.GetPlaneRectFromAnchorData(anchorData) &&
                   VolumeBounds == Utilities.GetVolumeBoundsFromAnchorData(anchorData) &&
                   Label == Utilities.StringLabelsToEnum(anchorData.SemanticClassifications) &&
                   PlaneBoundary2D.SequenceEqual(anchorData.PlaneBoundary2D);
        }
    }
}
