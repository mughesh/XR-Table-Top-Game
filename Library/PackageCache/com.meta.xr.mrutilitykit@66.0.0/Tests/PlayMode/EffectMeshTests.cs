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
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class EffectMeshTests : MRUKTestBase
    {
        private MRUKRoom _currentRoom;

        private static int Room1VertCountWall = 4;
        private static int Room1VertCountFloor = 8;
        private static int Room1VertCountCeiling = 8;
        private static int Room1VertCountTable = 24;
        private static int Room1VertCountOther = 24;


        [UnitySetUp]
        public new IEnumerator SetUp()
        {
            SceneToLoad = @"Packages\com.meta.xr.mrutilitykit\Tests\EffectMeshTests.unity";
            yield return base.SetUp();
        }

        [UnityTearDown]
        public new IEnumerator TearDown()
        {
            DestroyAll();
            yield return base.TearDown();
        }

        private int GetRoom1Vertices()
        {
            return 7 * Room1VertCountWall
                + Room1VertCountFloor
                + Room1VertCountCeiling
                + Room1VertCountTable
                + 2 * Room1VertCountOther;
        }
        private int GetRoom1VerticesMoreAnchors()
        {
            return 7 * Room1VertCountWall
                   + Room1VertCountFloor
                   + Room1VertCountCeiling
                   + Room1VertCountTable
                   + 4 * Room1VertCountOther;
        }

        private int GetRoom1Room3Vertices()
        {
            return 7 * Room1VertCountWall //room1
                   + Room1VertCountFloor
                   + Room1VertCountCeiling
                   + Room1VertCountTable
                   + 2 * Room1VertCountOther
                   + 7 * Room1VertCountWall //room3
                   + Room1VertCountFloor
                   + Room1VertCountCeiling
                   + Room1VertCountTable
                   + 2 * Room1VertCountOther;
        }

        private int GetDefaultRoomVertices()
        {
            return 7 * Room1VertCountWall
                + Room1VertCountFloor
                + Room1VertCountCeiling;
        }

        /// <summary>
        /// This function tests the count of vertices for each anchor in a scene.
        /// It iterates over each room and anchor, and asserts that the count of vertices matches the expected count based on the anchor's label.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountVertsFromObjectsInSequenceWithChildren()
        {
            SetupEffectMesh();
            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom3Room1);
            yield return null;

            foreach (var room in MRUK.Instance.Rooms)
            {
                foreach (var anchor in room.Anchors)
                {
                    switch (anchor.Label)
                    {
                        case MRUKAnchor.SceneLabels.FLOOR:
                            Assert.AreEqual(Room1VertCountFloor,CountVertex(anchor));
                            break;
                        case MRUKAnchor.SceneLabels.CEILING:
                            Assert.AreEqual(Room1VertCountCeiling,CountVertex(anchor));
                            break;
                        case MRUKAnchor.SceneLabels.DOOR_FRAME:
                        case MRUKAnchor.SceneLabels.WINDOW_FRAME:
                        case MRUKAnchor.SceneLabels.SCREEN:
                        case MRUKAnchor.SceneLabels.WALL_ART:
                        case MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE:
                        case MRUKAnchor.SceneLabels.WALL_FACE:
                            Assert.AreEqual(Room1VertCountWall,CountVertex(anchor));
                            break;
                        case MRUKAnchor.SceneLabels.STORAGE:
                        case MRUKAnchor.SceneLabels.BED:
                        case MRUKAnchor.SceneLabels.TABLE:
                        case MRUKAnchor.SceneLabels.COUCH:
                        case MRUKAnchor.SceneLabels.PLANT:
                        case MRUKAnchor.SceneLabels.LAMP:
                        case MRUKAnchor.SceneLabels.OTHER:
                            Assert.AreEqual(Room1VertCountOther,CountVertex(anchor));
                            break;
                        case MRUKAnchor.SceneLabels.GLOBAL_MESH:
                            break;
                    }
                }
            }

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in a default room with no anchors.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesDefaultRoomNoAnchors()
        {
            SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(DefaultRoomNoAnchors);
            yield return null;

            var vertCount = CountVertex();

            var expectedVerts = GetDefaultRoomVertices();
            Assert.AreEqual(expectedVerts,vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in a default room with anchors.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1WithAnchors()
        {
            SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Vertices();

            Assert.AreEqual(expectedVerts,vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in a room1 after room3 got removed.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom3Removed()
        {
            var effectMesh = SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom3Room1);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Room3Vertices();

            Assert.AreEqual(expectedVerts,vertCount);

            //track room updates, we want just vertices of Room one for this test
            effectMesh.TrackUpdates = true;

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            vertCount = CountVertex();
            expectedVerts = GetRoom1Vertices();
            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in room1 after anchors got added.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1AddedRoomByUser()
        {
            var effectMesh = SetupEffectMesh();

            //we just track one room, and we simulate an added room by the user
            effectMesh.SpawnOnStart = MRUK.RoomFilter.CurrentRoomOnly;

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Vertices();
            Assert.AreEqual(expectedVerts,vertCount);

            effectMesh.gameObject.SetActive(false);
            effectMesh.TrackUpdates = false;
            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom3Room1);
            yield return null;

            effectMesh.gameObject.SetActive(true);

            _currentRoom = MRUK.Instance.GetCurrentRoom();
            List<MRUKRoom> manualCreateEffectMesh = new();

            foreach (var room in MRUK.Instance.Rooms)
            {
                bool foundRoom = false;
                foreach (var anchor in room.Anchors)
                {
                    if (!anchor.GetComponentInChildren<Renderer>())
                    {
                        manualCreateEffectMesh.Add(room);
                        foundRoom = true;
                        break;
                    }
                }
                if (foundRoom) break;
            }


            Assert.AreEqual(1, manualCreateEffectMesh.Count);

            foreach (var room in manualCreateEffectMesh)
            {
                effectMesh.CreateMesh(room);
                yield return null;
            }

            vertCount = CountVertex();
            expectedVerts = GetRoom1Room3Vertices();
            Assert.AreEqual(expectedVerts,vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in room1 after anchors got added manually.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1AddedAnchorsByUser()
        {
            var effectMesh = SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Vertices();

            Assert.AreEqual(expectedVerts,vertCount);

            effectMesh.gameObject.SetActive(false);
            effectMesh.TrackUpdates = false;
            effectMesh.SpawnOnStart = MRUK.RoomFilter.None;
            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1MoreAnchors);
            yield return null;

            effectMesh.gameObject.SetActive(true);

            _currentRoom = MRUK.Instance.GetCurrentRoom();
            List<MRUKAnchor> manualCreateEffectMesh = new();

            foreach (var anchor in _currentRoom.Anchors)
            {
                if (!anchor.GetComponentInChildren<Renderer>())
                {
                    manualCreateEffectMesh.Add(anchor);
                }
            }

            Assert.AreEqual(2, manualCreateEffectMesh.Count);

            foreach (var anchor in manualCreateEffectMesh)
            {
                effectMesh.CreateEffectMesh(anchor);
                yield return null;
            }

            vertCount = CountVertex();
            expectedVerts = GetRoom1VerticesMoreAnchors();
            Assert.AreEqual(expectedVerts,vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in room1 after anchors got added by an update.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1AddedAnchorsBySystem()
        {
            SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Vertices();

            Assert.AreEqual(expectedVerts,vertCount);

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1MoreAnchors);
            yield return null;

            vertCount = CountVertex();
            expectedVerts = GetRoom1VerticesMoreAnchors();

            Assert.AreEqual(expectedVerts,vertCount);

            yield return null;
        }

        /// <summary>
        /// This function tests the vertex count in room1 and room3 at startup.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1Room3Startup()
        {
            SetupEffectMesh();

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom3Room1);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = GetRoom1Room3Vertices();

            Assert.AreEqual(expectedVerts, vertCount);

            yield return null;
        }


        /// <summary>
        /// This function tests the vertex count in room1 with a different bordersize setting.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountTrianglesRoom1StartupWithBorder()
        {
            SetupEffectMesh(0.2f);

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var vertCount = CountVertex();
            var expectedVerts = 232;
            Assert.AreEqual(expectedVerts,vertCount);

            yield return null;
        }

        private int CountVertex()
        {
            var allObjects = (GameObject[]) FindObjectsOfType(typeof(GameObject));
            int vertCount = 0;
            foreach (GameObject obj in allObjects)
            {
                Renderer rend = obj.GetComponentInChildren<Renderer>();
                if (!rend) continue;
                MeshFilter mf = obj.GetComponent<MeshFilter>();
                if (!mf) continue;

                vertCount += mf.mesh.vertexCount;
            }
            return vertCount;
        }

        private int CountVertex(MRUKAnchor anchor)
        {
            int vertCount = 0;
            foreach (var rend in anchor.gameObject.GetComponentsInChildren<Renderer>())
            {
                MeshFilter mf = rend.gameObject.GetComponent<MeshFilter>();
                if (!mf) continue;

                vertCount += mf.mesh.vertexCount;

            }

            return vertCount;
        }

        private EffectMesh SetupEffectMesh(float bordersize = 0.0f)
        {
            var effectMesh = FindObjectOfType<EffectMesh>();
            if (effectMesh == null)
            {
                Assert.Fail();
            }

            effectMesh.BorderSize = bordersize;
            effectMesh.SpawnOnStart = MRUK.RoomFilter.AllRooms;
            effectMesh.TrackUpdates = true;
            return effectMesh;
        }

        private void DestroyAll()
        {
            DestroyAll<MeshRenderer>();
            DestroyAll<MeshFilter>();
            DestroyAll<EffectMesh>();
            DestroyAll<MRUKAnchor>();
            DestroyAll<MRUKRoom>();
        }
        private void DestroyAll<T>() where T : Component
        {
            var allObjects = (T[])GameObject.FindObjectsOfType(typeof(T));
            foreach (var obj in allObjects)
            {
                DestroyImmediate(obj.gameObject);
            }
        }

        private static string DefaultRoomNoAnchors =
            @"{""CoordinateSystem"":""Unity"",""Rooms"":[{""UUID"":""26621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""24A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""91C4D6D7094448EEA20403AE86EE6EC1"",""33800E8EB9EE4C138FDE577C993EA90B"",""A6282AA1CE7B446388350D3D92A48848"",""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""F822A765695745529594E95CA5B92E8C"",""4D96ABFBDD3744EDB7BF1417081005FD"",""799EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""24A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""91C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""33800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""A6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""F822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""4D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""799EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]}]}]}";
        private static string SceneWithRoom1 =
            @"{""CoordinateSystem"":""Unity"",""Rooms"":[{""UUID"":""26621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""24A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""91C4D6D7094448EEA20403AE86EE6EC1"",""33800E8EB9EE4C138FDE577C993EA90B"",""A6282AA1CE7B446388350D3D92A48848"",""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""F822A765695745529594E95CA5B92E8C"",""4D96ABFBDD3744EDB7BF1417081005FD"",""799EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""24A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""91C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""33800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""A6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""F822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""4D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""799EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]},{""UUID"":""6853E03AE54E4DE9A82D8AC151BF00AC"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[0.95,1.0874939,1.63],""Rotation"":[270,5.721004,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.398986936,-0.432495117,-1.08197021],""Max"":[0.398986936,0.432495117,0]}},{""UUID"":""D767E0E46C1742A88813BFA49B8B28F0"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[-1.22,1.27151489,-1.72],""Rotation"":[270,96.27296,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.199005172,-0.401000977,-1.26599121],""Max"":[0.199005172,0.401000977,0]}},{""UUID"":""CAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]}]}]}";
        private static string SceneWithRoom3Room1 =
            @"{""CoordinateSystem"":""Unity"",""Rooms"":[{""UUID"":""36621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""C36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""34A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""01C4D6D7094448EEA20403AE86EE6EC1"",""43800E8EB9EE4C138FDE577C993EA90B"",""B6282AA1CE7B446388350D3D92A48848"",""FE8EAAA0CEC14186B1FF44A9DF52A2B1"",""A822A765695745529594E95CA5B92E8C"",""5D96ABFBDD3744EDB7BF1417081005FD"",""899EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""C36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""34A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""01C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""43800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""B6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""FE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""A822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""5D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""899EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]},{""UUID"":""7853E03AE54E4DE9A82D8AC151BF00AC"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[0.95,1.0874939,1.63],""Rotation"":[270,5.721004,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.398986936,-0.432495117,-1.08197021],""Max"":[0.398986936,0.432495117,0]}},{""UUID"":""E767E0E46C1742A88813BFA49B8B28F0"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[-1.22,1.27151489,-1.72],""Rotation"":[270,96.27296,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.199005172,-0.401000977,-1.26599121],""Max"":[0.199005172,0.401000977,0]}},{""UUID"":""DAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]}]},{""UUID"":""26621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""24A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""91C4D6D7094448EEA20403AE86EE6EC1"",""33800E8EB9EE4C138FDE577C993EA90B"",""A6282AA1CE7B446388350D3D92A48848"",""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""F822A765695745529594E95CA5B92E8C"",""4D96ABFBDD3744EDB7BF1417081005FD"",""799EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""24A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""91C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""33800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""A6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""F822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""4D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""799EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]},{""UUID"":""6853E03AE54E4DE9A82D8AC151BF00AC"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[0.95,1.0874939,1.63],""Rotation"":[270,5.721004,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.398986936,-0.432495117,-1.08197021],""Max"":[0.398986936,0.432495117,0]}},{""UUID"":""D767E0E46C1742A88813BFA49B8B28F0"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[-1.22,1.27151489,-1.72],""Rotation"":[270,96.27296,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.199005172,-0.401000977,-1.26599121],""Max"":[0.199005172,0.401000977,0]}},{""UUID"":""CAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]}]}]}";
        private static string SceneWithRoom1MoreAnchors =
            @"{""CoordinateSystem"":""Unity"",""Rooms"":[{""UUID"":""26621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""24A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""91C4D6D7094448EEA20403AE86EE6EC1"",""33800E8EB9EE4C138FDE577C993EA90B"",""A6282AA1CE7B446388350D3D92A48848"",""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""F822A765695745529594E95CA5B92E8C"",""4D96ABFBDD3744EDB7BF1417081005FD"",""799EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""24A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""91C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""33800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""A6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""F822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""4D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""799EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]},{""UUID"":""6853E03AE54E4DE9A82D8AC151BF00AC"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[0.95,1.0874939,1.63],""Rotation"":[270,5.721004,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.398986936,-0.432495117,-1.08197021],""Max"":[0.398986936,0.432495117,0]}},{""UUID"":""D767E0E46C1742A88813BFA49B8B28F0"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[-1.22,1.27151489,-1.72],""Rotation"":[270,96.27296,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.199005172,-0.401000977,-1.26599121],""Max"":[0.199005172,0.401000977,0]}},{""UUID"":""CAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]},{""UUID"":""DAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]},{""UUID"":""EAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]}]}]}";


    }
}
