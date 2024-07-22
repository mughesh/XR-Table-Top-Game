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
using UnityEngine;
using System.Collections.Generic;
using Meta.XR.Util;

namespace Meta.XR.MRUtilityKit
{
    [Feature(Feature.Scene)]
    public static class Triangulator
    {
        static bool IsConvex(Vector2 prevPoint, Vector2 currPoint, Vector2 nextPoint)
        {
            Vector2 edge1 = prevPoint - currPoint;
            Vector2 edge2 = nextPoint - currPoint;

            float crossProductZ = edge1.x * edge2.y - edge1.y * edge2.x;

            return crossProductZ <= 0;
        }

        static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            Vector2 ab = b - a;
            Vector2 bc = c - b;
            Vector2 ca = a - c;

            Vector2 ap = p - a;
            Vector2 bp = p - b;
            Vector2 cp = p - c;

            float crossProductZ1 = ab.x * ap.y - ab.y * ap.x;
            float crossProductZ2 = bc.x * bp.y - bc.y * bp.x;
            float crossProductZ3 = ca.x * cp.y - ca.y * cp.x;

            return (crossProductZ1 >= 0 && crossProductZ2 >= 0 && crossProductZ3 >= 0) ||
                   (crossProductZ1 <= 0 && crossProductZ2 <= 0 && crossProductZ3 <= 0);
        }

        static bool IsEar(List<Vector2> vertices, List<int> indices, int prevIndex, int currIndex, int nextIndex)
        {
            int numPoints = indices.Count;

            Vector2 prevPoint = vertices[prevIndex];
            Vector2 currPoint = vertices[currIndex];
            Vector2 nextPoint = vertices[nextIndex];

            for (int i = 0; i < numPoints; ++i)
            {
                int index = indices[i];
                if (index != prevIndex && index != currIndex && index != nextIndex)
                {
                    Vector2 testPoint = vertices[index];

                    if (PointInTriangle(prevPoint, currPoint, nextPoint, testPoint))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool FindLineIntersection(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2,
            out Vector2 intersection, out float u1, out float u2)
        {
            // Calculate direction vectors
            Vector2 dir1 = end1 - start1;
            Vector2 dir2 = end2 - start2;

            // Calculate normal vector
            Vector2 perp1 = Vector2.Perpendicular(dir1);
            Vector2 perp2 = Vector2.Perpendicular(dir2);

            // Calculate dot product
            float dot = Vector2.Dot(perp2, dir1);

            // Check if lines are parallel
            if (dot == 0)
            {
                u1 = default;
                u2 = default;
                intersection = default;
                return false; // No intersection
            }

            // Calculate distance to intersection point
            u1 = Vector2.Dot(perp2, start2 - start1) / dot;
            u2 = Vector2.Dot(perp1, start2 - start1) / dot;

            intersection = start1 + (end1 - start1) * u1;

            return u1 is >= 0f and <= 1f &&
                   u2 is >= 0f and <= 1f;
        }

        private struct MergedVertex
        {
            public Vector2 Position;
            public int NextVertex;  // Linked list to the next vertex (or -1 if none)
            public int NextHole;    // Linked list to the next hole (or -1 if none)
        }

        // Implements a simplified Weiler–Atherton clipping algorithm that does not deal with many edge cases
        internal static void ClipPolygon(List<Vector2> vertices, List<Vector2> hole)
        {
            var mergedVerts = new List<MergedVertex>(vertices.Count + hole.Count);

            int vertStart = 0;
            for (int i = 0; i < vertices.Count; ++i)
            {
                mergedVerts.Add(new MergedVertex()
                    { Position = vertices[i], NextVertex = (i + 1) % vertices.Count, NextHole = -1 });
            }

            int holeStart = vertices.Count;
            for (int i = 0; i < hole.Count; ++i)
            {
                for (int j = 0; j < vertices.Count; ++j)
                {
                    if (hole[i] == vertices[j])
                    {
                        throw new NotSupportedException("Overlapping vertices and holes are not supported");
                    }
                }

                mergedVerts.Add(new MergedVertex()
                    { Position = hole[i], NextVertex = -1, NextHole = holeStart + (i + 1) % hole.Count });
            }

            // Iterate through all the edges of the vertices polygon
            bool foundIntersection = false;
            int vertIndex = vertStart;
            while (true)
            {
                var vertMerged = mergedVerts[vertIndex];
                Vector2 o1 = mergedVerts[vertIndex].Position;
                Vector2 o2 = mergedVerts[vertMerged.NextVertex].Position;

                float closestU = Mathf.Infinity;
                Vector2 closestIntersect = new();
                int closestHoleIndex = -1;

                // Iterate through all the edges of the hole polygon and find the closest intersection to o1
                int holeIndex = holeStart;
                while (true)
                {
                    var holeMerged = mergedVerts[holeIndex];
                    Vector2 h1 = mergedVerts[holeIndex].Position;
                    Vector2 h2 = mergedVerts[holeMerged.NextHole].Position;

                    if (holeIndex != vertIndex && holeMerged.NextHole != vertIndex &&
                        holeIndex != vertMerged.NextVertex && holeMerged.NextHole != vertMerged.NextVertex)
                    {
                        if (FindLineIntersection(o1, o2, h1, h2, out var intersection, out var u1, out var u2))
                        {
                            if (u1 < closestU)
                            {
                                closestU = u1;
                                closestIntersect = intersection;
                                closestHoleIndex = holeIndex;
                            }
                        }
                    }

                    holeIndex = holeMerged.NextHole;
                    if (holeIndex == holeStart)
                    {
                        break;
                    }
                }

                // If an intersection was found then create a new vertex at the intersection point and update
                // the linked list pointers to insert it
                if (closestHoleIndex != -1)
                {
                    foundIntersection = true;
                    var holeMerged = mergedVerts[closestHoleIndex];
                    mergedVerts.Add(new MergedVertex()
                    {
                        Position = closestIntersect, NextVertex = vertMerged.NextVertex,
                        NextHole = holeMerged.NextHole
                    });

                    vertMerged.NextVertex = mergedVerts.Count - 1;
                    mergedVerts[vertIndex] = vertMerged;

                    holeMerged.NextHole = mergedVerts.Count - 1;
                    mergedVerts[closestHoleIndex] = holeMerged;
                }

                vertIndex = vertMerged.NextVertex;
                if (vertIndex == vertStart)
                {
                    break;
                }
            }

            if (foundIntersection)
            {
                // Find the first vertex that is not inside the hole
                vertIndex = vertStart;
                while (true)
                {
                    var vertMerged = mergedVerts[vertIndex];

                    if (!Utilities.IsPositionInPolygon(vertMerged.Position, hole))
                    {
                        break;
                    }

                    vertIndex = vertMerged.NextVertex;
                    if (vertIndex == vertStart)
                    {
                        throw new Exception("Unable to find a starting vertex outside of the hole");
                    }
                }

                // Clear the hole and vertices, we are going to merge them together into the vertices list
                hole.Clear();
                vertices.Clear();

                // Start at the first vertex not in the hole
                vertStart = vertIndex;
                bool followingVerts = true;
                while (true)
                {
                    var vertMerged = mergedVerts[vertIndex];
                    vertices.Add(vertMerged.Position);

                    if (vertMerged.NextHole != -1 && (followingVerts || vertMerged.NextVertex == -1))
                    {
                        vertIndex = vertMerged.NextHole;
                        followingVerts = false;
                    }
                    else if (vertMerged.NextVertex != -1)
                    {
                        vertIndex = vertMerged.NextVertex;
                        followingVerts = true;
                    }
                    else
                    {
                        throw new Exception("Unable to follow next vertex or hole, the node is invalid");
                    }

                    if (vertIndex == vertStart)
                    {
                        break;
                    }
                }
            }
            else if (Utilities.IsPositionInPolygon(vertices[0], hole))
            {
                throw new Exception("vertices are completely encapsulated within the hole");
            }
        }

        public struct Outline
        {
            public List<Vector2> vertices;
            public List<int> indices;
        }

        public static Outline CreateOutline(List<Vector2> vertices, List<List<Vector2>> holes)
        {
            for (int i = 0; i < holes.Count;)
            {
                ClipPolygon(vertices, holes[i]);
                if (holes[i].Count == 0)
                {
                    // Hole was merged or remove, so remove it from the list
                    holes.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }

            Outline outline = new();
            outline.vertices = new List<Vector2>(vertices);
            outline.indices = new();
            int totalVertices = vertices.Count;
            foreach (var hole in holes)
            {
                totalVertices += hole.Count;
            }

            outline.vertices.Capacity = totalVertices;
            outline.indices.Capacity = totalVertices + 2 * holes.Count;
            for (int i = 0; i < vertices.Count; ++i)
            {
                outline.indices.Add(i);
            }

            while (holes.Count > 0)
            {
                float maxX = Mathf.NegativeInfinity;
                int holeToMerge = -1;
                int vertexToMerge = -1;
                for (int i = 0; i < holes.Count; ++i)
                {
                    var hole = holes[i];
                    for (int j = 0; j < hole.Count; ++j)
                    {
                        var vertex = hole[j];
                        if (vertex.x > maxX)
                        {
                            maxX = vertex.x;
                            holeToMerge = i;
                            vertexToMerge = j;
                        }
                    }
                }

                Vector2 holePos = holes[holeToMerge][vertexToMerge];
                float closestXIntersection = Mathf.Infinity;
                int mergeWithIndex = -1;
                Vector2 mergeWith = new();
                for (int i = 0; i < outline.indices.Count; ++i)
                {
                    int i1 = outline.indices[i];
                    int i2 = outline.indices[(i + 1) % outline.indices.Count];
                    Vector2 p1 = outline.vertices[i1];
                    Vector2 p2 = outline.vertices[i2];
                    if ((p1.y != p2.y) && (p2.y >= holePos.y && p1.y <= holePos.y))
                    {
                        float frac = (holePos.y - p1.y) / (p2.y - p1.y);
                        float xIntersection = p1.x + frac * (p2.x - p1.x);
                        if (xIntersection >= holePos.x && xIntersection < closestXIntersection)
                        {
                            closestXIntersection = xIntersection;
                            mergeWithIndex = i;
                            mergeWith = p1;
                        }
                    }
                }

                if (mergeWithIndex != -1)
                {
                    Vector2 intersection = new Vector2(closestXIntersection, holePos.y);
                    int mergeWithVertexIndex = outline.indices[mergeWithIndex];
                    for (int i = 0; i < outline.indices.Count; ++i)
                    {
                        int prevVertexIndex = outline.indices[(i + outline.indices.Count - 1) % outline.indices.Count];
                        int vertexIndex = outline.indices[i];
                        int nextVertexIndex = outline.indices[(i + 1) % outline.indices.Count];
                        Vector2 prevVertex = outline.vertices[prevVertexIndex];
                        Vector2 candidateVertex = outline.vertices[vertexIndex];
                        Vector2 nextVertex = outline.vertices[nextVertexIndex];
                        if (mergeWithVertexIndex != vertexIndex && !IsConvex(prevVertex, candidateVertex, nextVertex))
                        {
                            if (candidateVertex.x < mergeWith.x && candidateVertex.x > holePos.x &&
                                PointInTriangle(holePos, mergeWith, intersection, candidateVertex))
                            {
                                mergeWith = candidateVertex;
                                mergeWithIndex = i;
                            }
                        }
                    }

                    int startIndex = outline.vertices.Count;
                    int holeVertexCount = holes[holeToMerge].Count;
                    List<int> insertedIndices = new();
                    insertedIndices.Capacity = holeVertexCount + 2;
                    outline.vertices.AddRange(holes[holeToMerge]);
                    for (int j = 0; j < holeVertexCount; ++j)
                    {
                        insertedIndices.Add(startIndex + (j + vertexToMerge) % holeVertexCount);
                    }

                    insertedIndices.Add(startIndex + vertexToMerge);
                    insertedIndices.Add(outline.indices[mergeWithIndex]);
                    outline.indices.InsertRange(mergeWithIndex + 1, insertedIndices);
                }

                holes.RemoveAt(holeToMerge);
            }

            return outline;
        }

        // Ear clipping algorithm to triangulate the boundary
        public static List<int> TriangulatePoints(List<Vector2> vertices)
        {
            Outline outline = new();
            outline.vertices = vertices;
            outline.indices = new();

            outline.indices.Capacity = vertices.Count;
            for (int i = 0; i < vertices.Count; ++i)
            {
                outline.indices.Add(i);
            }

            return TriangulateMesh(outline);
        }

        // Ear clipping algorithm to triangulate the boundary
        public static List<int> TriangulateMesh(Outline outline)
        {
            List<Vector2> vertices = outline.vertices;
            List<int> indices = outline.indices;
            List<int> triangles = new();
            int numTriangles = Mathf.Max(outline.indices.Count - 2, 0);
            triangles.Capacity = 3 * numTriangles;

            while (indices.Count > 3)
            {
                bool earFound = false;

                for (int i = 0; i < indices.Count; ++i)
                {
                    int prevIndex = indices[(i + indices.Count - 1) % indices.Count];
                    int currIndex = indices[i];
                    int nextIndex = indices[(i + 1) % indices.Count];

                    if (IsConvex(vertices[prevIndex], vertices[currIndex], vertices[nextIndex]) &&
                        IsEar(vertices, indices, prevIndex, currIndex, nextIndex))
                    {
                        triangles.Add(prevIndex);
                        triangles.Add(currIndex);
                        triangles.Add(nextIndex);

                        indices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }

                if (!earFound)
                {
                    Debug.LogError("Failed to triangulate points.");
                    break;
                }
            }

            if (indices.Count == 3)
            {
                triangles.AddRange(indices);
            }

            return triangles;
        }
    }
}
