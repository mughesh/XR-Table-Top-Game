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
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using System.Collections.Generic;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class TriangulatorTests : MRUKTestBase
    {

        private float CalculateTriangleArea(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x * (p2.y - p3.y) + p2.x * (p3.y - p1.y) + p3.x * (p1.y - p2.y)) / 2.0f;
        }

        // Use the triangulated area as a proxy to ensure the triangulation worked as expected
        private float CalculateTriangulatedArea(List<Vector2> vertices, List<int> indices)
        {
            float area = 0f;
            for (int i = 0; i < indices.Count; i += 3)
            {
                var p1 = vertices[indices[i]];
                var p2 = vertices[indices[i + 1]];
                var p3 = vertices[indices[i + 2]];
                var triangleArea = CalculateTriangleArea(p1, p2, p3);
                Assert.GreaterOrEqual(triangleArea, 0f);
                area += triangleArea;
            }

            return area;
        }

        /// <summary>
        /// Tests that the triangulator is able to triangulate a simple quad
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TriangulateQuad()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f) };
            var indices = Triangulator.TriangulatePoints(vertices);
            yield return null;
            Assert.AreEqual(6, indices.Count);
            Assert.AreEqual(1.0f, CalculateTriangulatedArea(vertices, indices));
        }

        /// <summary>
        /// Tests that the triangulator is able to triangulate a 2x2 quad with a 1x1 quad hole in the center of it
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TriangulateQuadWithHole()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) };
            var holes = new List<List<Vector2>> { new List<Vector2> { new(0.5f, 0.5f), new(0.5f, 1.5f), new(1.5f, 1.5f), new(1.5f, 0.5f) } };
            var outline = Triangulator.CreateOutline(vertices, holes);
            var indices = Triangulator.TriangulateMesh(outline);
            yield return null;
            Assert.AreEqual(24, indices.Count);
            Assert.AreEqual(3.0f, CalculateTriangulatedArea(outline.vertices, indices));
        }

        /// <summary>
        /// Tests that the triangulator is able to triangulate a large quad with 2 large holes in it.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TriangulateQuadWith2HolesLarge()
        {
            var vertices = new List<Vector2> {
                new(101.985214f, 113.8258f), new(-101.985214f, 113.8258f), new(-101.985214f, -113.8258f), new(101.985214f, -113.8258f)
            };
            var holes = new List<List<Vector2>> {
                new List<Vector2> { new(18.395055731633885f, 9.0596833f), new(-72.518264268366110f, 9.0596833f), new(-72.518264268366110f, 67.2252527f), new(18.395055731633885f, 67.2252527f) },
                new List<Vector2> { new(18.395055731633885f, -53.4203167f), new(-72.518264268366110f, -53.4203167f), new(-72.518264268366110f, 4.7452569f), new(18.395055731633885f, 4.7452569f) },
            };
            var outline = Triangulator.CreateOutline(vertices, holes);
            var indices = Triangulator.TriangulateMesh(outline);
            yield return null;
            Assert.AreEqual(42, indices.Count);
            Assert.AreEqual(35858.1445f, CalculateTriangulatedArea(outline.vertices, indices));
        }

        /// <summary>
        /// Tests that the triangulator is able to triangulate a 4x4 quad with four 1x1 quad holes distributed in a grid pattern
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TriangulateQuadWith4Holes()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(4f, 0f), new(4f, 4f), new(0f, 4f) };
            var holes = new List<List<Vector2>>();
            for (int i = 0; i < 4; i++)
            {
                var offset = new Vector2(0.5f + 2f * (i / 2), 0.5f + 2f * (i % 2));
                holes.Add(new List<Vector2> { offset + new Vector2(0f, 0f), offset + new Vector2(0f, 1f), offset + new Vector2(1f, 1f), offset + new Vector2(1f, 0f) });
            }
            var outline = Triangulator.CreateOutline(vertices, holes);
            var indices = Triangulator.TriangulateMesh(outline);
            yield return null;
            Assert.AreEqual(78, indices.Count);
            Assert.AreEqual(12.0f, CalculateTriangulatedArea(outline.vertices, indices));
        }

        /// <summary>
        /// Tests that the triangulator is able to triangulate an L shape
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TriangulateLShape()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(1f, 2f), new(1f, 1f), new(0f, 1f) };
            var indices = Triangulator.TriangulatePoints(vertices);
            yield return null;
            Assert.AreEqual(12, indices.Count);
            Assert.AreEqual(3.0f, CalculateTriangulatedArea(vertices, indices));
        }

        /// <summary>
        /// Tests that the triangulator is able to triangulate a C shape
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TriangulateCShape()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 1f), new(1f, 1f), new(1f, 2f), new(2f, 2f), new(2f, 3f), new(0f, 3f) };
            var indices = Triangulator.TriangulatePoints(vertices);
            yield return null;
            Assert.AreEqual(18, indices.Count);
            Assert.AreEqual(5.0f, CalculateTriangulatedArea(vertices, indices));
        }

        /// <summary>
        /// Tests that FindLineIntersection works as expected
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator FindLineIntersection1()
        {
            var foundIntersection = Triangulator.FindLineIntersection(new(0f, 0f), new(0f, 1f), new(-1f, 1f), new(1f, 1f), out Vector2 intersection, out var u1, out var u2);
            yield return null;
            Assert.IsTrue(foundIntersection);
            Assert.AreEqual(new Vector2(0f, 1f), intersection);
            Assert.AreEqual(1f, u1);
            Assert.AreEqual(0.5f, u2);
        }

        /// <summary>
        /// Tests that FindLineIntersection works as expected
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator FindLineIntersection2()
        {
            var foundIntersection = Triangulator.FindLineIntersection(new(0f, 0f), new(2f, 2f), new(1f, 0f), new(0f, 1f), out Vector2 intersection, out var u1, out var u2);
            yield return null;
            Assert.IsTrue(foundIntersection);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), intersection);
            Assert.AreEqual(0.25f, u1);
            Assert.AreEqual(0.5f, u2);
        }

        /// <summary>
        /// Tests that MergePolygons works as expected with a hole cut out of a quad at the bottom middle section
        /// </summary>
        /// <remarks>
        /// __________
        /// |        |
        /// |  ____  |
        /// |__|  |__|
        /// </remarks>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator MergePolygonsBottomMiddle()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) };
            var hole = new List<Vector2> { new(0.5f, -0.5f), new(0.5f, 1.5f), new(1.5f, 1.5f), new(1.5f, -0.5f) };
            Triangulator.ClipPolygon(vertices, hole);
            yield return null;
            Assert.AreEqual(new List<Vector2> { new(0f, 0f), new(0.5f, 0f), new(0.5f, 1.5f), new(1.5f, 1.5f), new(1.5f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) }, vertices);
            Assert.AreEqual(0, hole.Count);
        }

        /// <summary>
        /// Tests that MergePolygons works as expected with a hole cut out of a quad at the bottom middle section and
        /// one of the hole edges exactly overlaps with one of the edges of the polygon
        /// </summary>
        /// <remarks>
        /// __________
        /// |        |
        /// |  ____  |
        /// |__|  |__|
        /// </remarks>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator MergePolygonsBottomMiddleOverlap()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) };
            var hole = new List<Vector2> { new(0.5f, 0f), new(0.5f, 1.5f), new(1.5f, 1.5f), new(1.5f, 0f) };
            Triangulator.ClipPolygon(vertices, hole);
            yield return null;
            Assert.AreEqual(new List<Vector2> { new(0f, 0f), new(0.5f, 0f), new(0.5f, 1.5f), new(1.5f, 1.5f), new(1.5f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) }, vertices);
            Assert.AreEqual(0, hole.Count);
        }

        /// <summary>
        /// Tests that MergePolygons works as expected with a hole cut out of a quad at the bottom left section
        /// </summary>
        /// <remarks>
        /// __________
        /// |        |
        /// |___     |
        ///    |_____|
        /// </remarks>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator MergePolygonsBottomLeftCorner()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) };
            var hole = new List<Vector2> { new(-0.5f, -0.5f), new(-0.5f, 1.5f), new(1.5f, 1.5f), new(1.5f, -0.5f) };
            Triangulator.ClipPolygon(vertices, hole);
            yield return null;
            Assert.AreEqual(new List<Vector2> { new(2f, 0f), new(2f, 2f), new(0f, 2f), new(0f, 1.5f), new(1.5f, 1.5f), new(1.5f, 0f) }, vertices);
            Assert.AreEqual(0, hole.Count);
        }

        /// <summary>
        /// Tests that MergePolygons works as expected with a hole cut out of a quad at the right middle section
        /// </summary>
        /// <remarks>
        /// __________
        /// |    ____|
        /// |    |___
        /// |________|
        /// </remarks>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator MergePolygonsRightMiddle()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) };
            var hole = new List<Vector2> { new(0.5f, 0.5f), new(0.5f, 1.5f), new(2.5f, 1.5f), new(2.5f, 0.5f) };
            Triangulator.ClipPolygon(vertices, hole);
            yield return null;
            Assert.AreEqual(new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2.0f, 0.5f), new(0.5f, 0.5f), new(0.5f, 1.5f), new(2.0f, 1.5f), new(2f, 2f), new(0f, 2f) }, vertices);
            Assert.AreEqual(0, hole.Count);
        }

        /// <summary>
        /// Tests that MergePolygons works as expected with a hole cut out of a quad encapsulating the top half of the quad
        /// </summary>
        /// <remarks>
        ///
        ///
        /// __________
        /// |________|
        /// </remarks>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator MergePolygonsTopHalf()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) };
            var hole = new List<Vector2> { new(-0.5f, 0.5f), new(-0.5f, 2.5f), new(2.5f, 2.5f), new(2.5f, 0.5f) };
            Triangulator.ClipPolygon(vertices, hole);
            yield return null;
            Assert.AreEqual(new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 0.5f), new(0f, 0.5f) }, vertices);
            Assert.AreEqual(0, hole.Count);
        }

        /// <summary>
        /// Tests that MergePolygons throws an exception if the hole completely encapsulates the polygon
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator MergePolygonsHoleEncapsulatesVertices()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) };
            var hole = new List<Vector2> { new(-0.5f, -0.5f), new(-0.5f, 2.5f), new(2.5f, 2.5f), new(2.5f, -0.5f) };
            Assert.That(() => Triangulator.ClipPolygon(vertices, hole),
                Throws.TypeOf<Exception>());
            yield return null;
        }

        /// <summary>
        /// Tests that MergePolygons works as expected with a hole vertex sharing the same position as a vertex
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator MergePolygonsOverlappingPoints()
        {
            var vertices = new List<Vector2> { new(0f, 0f), new(2f, 0f), new(2f, 2f), new(0f, 2f) };
            var hole = new List<Vector2> { new(-0.5f, -0.5f), new(-0.5f, 0f), new(0f, 0f), new(0f, -0.5f) };
            Assert.That(() => Triangulator.ClipPolygon(vertices, hole),
                Throws.TypeOf<NotSupportedException>());
            yield return null;
        }

    }
}

