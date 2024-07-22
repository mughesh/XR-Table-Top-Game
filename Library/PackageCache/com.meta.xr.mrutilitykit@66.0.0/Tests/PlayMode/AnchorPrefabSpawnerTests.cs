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
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class AnchorPrefabSpawnerTests : MonoBehaviour
    {
        private const int DefaultTimeoutMs = 10000;
        private MRUKRoom _currentRoom;

        private static readonly int Room1WallCount = 7;
        private static readonly int Room1FloorCount = 1;
        private static readonly int Room1CeilingCount = 1;
        private static readonly int Room1TableCount = 1;
        private static readonly int Room1OtherCount = 2;

        private static readonly int Room3WallCount = 7;
        private static readonly int Room3FloorCount = 1;
        private static readonly int Room3CeilingCount = 1;
        private static readonly int Room3TableCount = 1;
        private static readonly int Room3OtherCount = 2;

        private static readonly string SceneWithRoom1 =
            @"{""CoordinateSystem"":""Unity"",""Rooms"":[{""UUID"":""26621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""24A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""91C4D6D7094448EEA20403AE86EE6EC1"",""33800E8EB9EE4C138FDE577C993EA90B"",""A6282AA1CE7B446388350D3D92A48848"",""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""F822A765695745529594E95CA5B92E8C"",""4D96ABFBDD3744EDB7BF1417081005FD"",""799EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""24A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""91C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""33800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""A6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""F822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""4D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""799EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]},{""UUID"":""6853E03AE54E4DE9A82D8AC151BF00AC"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[0.95,1.0874939,1.63],""Rotation"":[270,5.721004,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.398986936,-0.432495117,-1.08197021],""Max"":[0.398986936,0.432495117,0]}},{""UUID"":""D767E0E46C1742A88813BFA49B8B28F0"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[-1.22,1.27151489,-1.72],""Rotation"":[270,96.27296,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.199005172,-0.401000977,-1.26599121],""Max"":[0.199005172,0.401000977,0]}},{""UUID"":""CAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]}]}]}";
        private static readonly string SceneWithRoom3Room1 =
            @"{""CoordinateSystem"":""Unity"",""Rooms"":[{""UUID"":""36621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""C36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""34A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""01C4D6D7094448EEA20403AE86EE6EC1"",""43800E8EB9EE4C138FDE577C993EA90B"",""B6282AA1CE7B446388350D3D92A48848"",""FE8EAAA0CEC14186B1FF44A9DF52A2B1"",""A822A765695745529594E95CA5B92E8C"",""5D96ABFBDD3744EDB7BF1417081005FD"",""899EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""C36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""34A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""01C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""43800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""B6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""FE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""A822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""5D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""899EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]},{""UUID"":""7853E03AE54E4DE9A82D8AC151BF00AC"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[0.95,1.0874939,1.63],""Rotation"":[270,5.721004,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.398986936,-0.432495117,-1.08197021],""Max"":[0.398986936,0.432495117,0]}},{""UUID"":""E767E0E46C1742A88813BFA49B8B28F0"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[-1.22,1.27151489,-1.72],""Rotation"":[270,96.27296,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.199005172,-0.401000977,-1.26599121],""Max"":[0.199005172,0.401000977,0]}},{""UUID"":""DAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]}]},{""UUID"":""26621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""24A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""91C4D6D7094448EEA20403AE86EE6EC1"",""33800E8EB9EE4C138FDE577C993EA90B"",""A6282AA1CE7B446388350D3D92A48848"",""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""F822A765695745529594E95CA5B92E8C"",""4D96ABFBDD3744EDB7BF1417081005FD"",""799EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""24A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""91C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""33800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""A6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""F822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""4D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""799EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]},{""UUID"":""6853E03AE54E4DE9A82D8AC151BF00AC"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[0.95,1.0874939,1.63],""Rotation"":[270,5.721004,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.398986936,-0.432495117,-1.08197021],""Max"":[0.398986936,0.432495117,0]}},{""UUID"":""D767E0E46C1742A88813BFA49B8B28F0"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[-1.22,1.27151489,-1.72],""Rotation"":[270,96.27296,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.199005172,-0.401000977,-1.26599121],""Max"":[0.199005172,0.401000977,0]}},{""UUID"":""CAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]}]}]}";
        private static readonly string SceneWithRoom1MoreAnchors =
            @"{""CoordinateSystem"":""Unity"",""Rooms"":[{""UUID"":""26621F6BC6E04FC09FF7F37167AD92B7"",""RoomLayout"":{""FloorUuid"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""CeilingUuid"":""24A8F474833744DFB02C488A9BA111E8"",""WallsUUid"":[""91C4D6D7094448EEA20403AE86EE6EC1"",""33800E8EB9EE4C138FDE577C993EA90B"",""A6282AA1CE7B446388350D3D92A48848"",""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""F822A765695745529594E95CA5B92E8C"",""4D96ABFBDD3744EDB7BF1417081005FD"",""799EB9126BD94EFDA93418DDBEB28B99""]},""Anchors"":[{""UUID"":""B36F3D4FDD3C4E05A9591402F93F61E5"",""SemanticClassifications"":[""FLOOR""],""Transform"":{""Translation"":[-1.06952024,0,1.2340889],""Rotation"":[270,273.148438,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-0.614610434,3.14264679],[-3.1610744,3.185142],[-3.16107464,-0.628885269],[0.0159805864,-0.3973337],[-0.00542993844,-3.17527843],[3.121027,-3.18514252],[3.16107488,-0.4239784],[3.05573153,2.90878654]]},{""UUID"":""24A8F474833744DFB02C488A9BA111E8"",""SemanticClassifications"":[""CEILING""],""Transform"":{""Translation"":[-1.06952024,3,1.2340889],""Rotation"":[90,93.14846,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-3.16107464,-3.18514252],""Max"":[3.16107464,3.18514252]},""PlaneBoundary2D"":[[-3.05573153,2.90878654],[-3.16107488,-0.423978329],[-3.121027,-3.18514252],[0.00542993844,-3.17527819],[-0.0159805864,-0.397333622],[3.16107464,-0.628885269],[3.1610744,3.18514228],[0.614610434,3.14264679]]},{""UUID"":""91C4D6D7094448EEA20403AE86EE6EC1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.71035528,1.5,1.268393],""Rotation"":[0,248.437622,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.8823391,-1.5],""Max"":[0.8823391,1.5]},""PlaneBoundary2D"":[[-0.8823391,-1.5],[0.8823391,-1.5],[0.8823391,1.5],[-0.8823391,1.5]]},{""UUID"":""33800E8EB9EE4C138FDE577C993EA90B"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.3914528,1.5,2.15365982],""Rotation"":[0,183.720367,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.9967317,-1.5],""Max"":[0.9967317,1.5]},""PlaneBoundary2D"":[[-0.9967317,-1.5],[0.9967317,-1.5],[0.9967317,1.5],[-0.9967317,1.5]]},{""UUID"":""A6282AA1CE7B446388350D3D92A48848"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-0.9457869,1.5,1.72746456],""Rotation"":[0,124.913544,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.5986103,-1.5],""Max"":[0.5986103,1.5]},""PlaneBoundary2D"":[[-0.5986103,-1.5],[0.5986103,-1.5],[0.5986103,1.5],[-0.5986103,1.5]]},{""UUID"":""EE8EAAA0CEC14186B1FF44A9DF52A2B1"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.49773419,1.5,-0.343039751],""Rotation"":[0,97.54905,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.59344471,-1.5],""Max"":[1.59344471,1.5]},""PlaneBoundary2D"":[[-1.59344471,-1.5],[1.59344471,-1.5],[1.59344471,1.5],[-1.59344471,1.5]]},{""UUID"":""F822A765695745529594E95CA5B92E8C"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[-1.22031093,1.5,-1.98077047],""Rotation"":[0,6.806239,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.4902168,-1.5],""Max"":[0.4902168,1.5]},""PlaneBoundary2D"":[[-0.4902168,-1.5],[0.4902168,-1.5],[0.4902168,1.5],[-0.4902168,1.5]]},{""UUID"":""4D96ABFBDD3744EDB7BF1417081005FD"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[0.6066754,1.5,-2.05464935],""Rotation"":[0,0.674668849,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.34031725,-1.5],""Max"":[1.34031725,1.5]},""PlaneBoundary2D"":[[-1.34031725,-1.5],[1.34031725,-1.5],[1.34031725,1.5],[-1.34031725,1.5]]},{""UUID"":""799EB9126BD94EFDA93418DDBEB28B99"",""SemanticClassifications"":[""WALL_FACE""],""Transform"":{""Translation"":[1.99076307,1.5,-0.8113152],""Rotation"":[0,271.995178,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-1.25988054,-1.5],""Max"":[1.25988054,1.5]},""PlaneBoundary2D"":[[-1.25988054,-1.5],[1.25988054,-1.5],[1.25988054,1.5],[-1.25988054,1.5]]},{""UUID"":""6853E03AE54E4DE9A82D8AC151BF00AC"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[0.95,1.0874939,1.63],""Rotation"":[270,5.721004,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.398986936,-0.432495117,-1.08197021],""Max"":[0.398986936,0.432495117,0]}},{""UUID"":""D767E0E46C1742A88813BFA49B8B28F0"",""SemanticClassifications"":[""OTHER""],""Transform"":{""Translation"":[-1.22,1.27151489,-1.72],""Rotation"":[270,96.27296,0],""Scale"":[1,1,1]},""VolumeBounds"":{""Min"":[-0.199005172,-0.401000977,-1.26599121],""Max"":[0.199005172,0.401000977,0]}},{""UUID"":""CAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]},{""UUID"":""DAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]},{""UUID"":""EAE852DB8847498F8A5812EA04F6C1F6"",""SemanticClassifications"":[""TABLE""],""Transform"":{""Translation"":[1.555448,1.06253052,-0.8596737],""Rotation"":[270,1.2390064,0],""Scale"":[1,1,1]},""PlaneBounds"":{""Min"":[-0.400512785,-0.9039919],""Max"":[0.400512785,0.9039919]},""VolumeBounds"":{""Min"":[-0.400512785,-0.9039919,-1.05700684],""Max"":[0.400512785,0.9039919,0]},""PlaneBoundary2D"":[[-0.400512785,-0.9039919],[0.400512785,-0.9039919],[0.400512785,0.9039919],[-0.400512785,0.9039919]]}]}]}";


        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Packages\\com.meta.xr.mrutilitykit\\Tests\\AnchorPrefabSpawnerTests.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return DestroyAnchors();
            for (int i = SceneManager.sceneCount - 1; i >= 1; i--)
            {
                var asyncOperation =
                    SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i).name); // Clear/reset scene
                yield return new WaitUntil(() => asyncOperation.isDone);
            }
        }

        private (int, int, int, int, int) CountSpawnedChildrenInRoom(MRUKRoom room)
        {
            int createdWalls = 0;
            int createdFloor = 0;
            int createdCeiling = 0;
            int createdTable = 0;
            int createdOther = 0;

            foreach (var anchor in room.Anchors)
            {
                switch (anchor.Label)
                {
                    case MRUKAnchor.SceneLabels.FLOOR:
                        if(HasSpawnedChild(anchor)) createdFloor++;
                        break;
                    case MRUKAnchor.SceneLabels.CEILING:
                        if(HasSpawnedChild(anchor)) createdCeiling++;
                        break;
                    case MRUKAnchor.SceneLabels.WALL_FACE:
                        if(HasSpawnedChild(anchor)) createdWalls++;
                        break;
                    case MRUKAnchor.SceneLabels.TABLE:
                        createdTable++;
                        break;
                    case MRUKAnchor.SceneLabels.COUCH:
                    case MRUKAnchor.SceneLabels.DOOR_FRAME:
                    case MRUKAnchor.SceneLabels.WINDOW_FRAME:
                    case MRUKAnchor.SceneLabels.STORAGE:
                    case MRUKAnchor.SceneLabels.BED:
                    case MRUKAnchor.SceneLabels.SCREEN:
                    case MRUKAnchor.SceneLabels.LAMP:
                    case MRUKAnchor.SceneLabels.PLANT:
                    case MRUKAnchor.SceneLabels.WALL_ART:
                    case MRUKAnchor.SceneLabels.GLOBAL_MESH:
                    case MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE:
                    case MRUKAnchor.SceneLabels.OTHER:
                        if(HasSpawnedChild(anchor)) createdOther++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (createdWalls, createdFloor, createdCeiling, createdTable, createdOther);
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountDifferentLabelsForSpawnedPrefab()
        {
            SetupAnchorPrefabSpawner();
            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.GetCurrentRoom());


            Assert.AreEqual(createdWalls, Room1WallCount);
            Assert.AreEqual(createdFloor, Room1FloorCount);
            Assert.AreEqual(createdCeiling, Room1CeilingCount);
            Assert.AreEqual(createdTable, Room1TableCount);
            Assert.AreEqual(createdOther, Room1OtherCount);

            yield return null;
        }


        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountSpawnedItemsRoom3Room1()
        {
            SetupAnchorPrefabSpawner();
            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom3Room1);
            yield return null;

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(createdWalls, Room1WallCount);
            Assert.AreEqual(createdFloor, Room1FloorCount);
            Assert.AreEqual(createdCeiling, Room1CeilingCount);
            Assert.AreEqual(createdTable, Room1TableCount);
            Assert.AreEqual(createdOther, Room1OtherCount);

            (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[1]);

            Assert.AreEqual(createdWalls, Room3WallCount);
            Assert.AreEqual(createdFloor, Room3FloorCount);
            Assert.AreEqual(createdCeiling, Room3CeilingCount);
            Assert.AreEqual(createdTable, Room3TableCount);
            Assert.AreEqual(createdOther, Room3OtherCount);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountSpawnedItemsRoom1WallsOnly()
        {
            var anchorPrefabSpawner = SetupAnchorPrefabSpawner();
            string[] searchResults = AssetDatabase.FindAssets("WALL",new[] { "Packages\\com.meta.xr.mrutilitykit\\Core\\Prefabs\\" });
            string prefabPath = AssetDatabase.GUIDToAssetPath(searchResults[0]);

            anchorPrefabSpawner.PrefabsToSpawn = new List<AnchorPrefabSpawner.AnchorPrefabGroup>()
            {
                new()
                {
                    Labels = MRUKAnchor.SceneLabels.WALL_FACE,
                    Prefabs = new List<GameObject>()
                    {
                        AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)
                    }
                }
            };

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(createdWalls, Room1WallCount);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountSpawnedItemsRoom1ThenAddRoom3()
        {
            var anchorPrefabSpawner = SetupAnchorPrefabSpawner();
            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(createdWalls, Room1WallCount);
            Assert.AreEqual(createdFloor, Room1FloorCount);
            Assert.AreEqual(createdCeiling, Room1CeilingCount);
            Assert.AreEqual(createdTable, Room1TableCount);
            Assert.AreEqual(createdOther, Room1OtherCount);


            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom3Room1);
            yield return null;

            (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[1]);

            Assert.AreEqual(createdWalls, Room3WallCount);
            Assert.AreEqual(createdFloor, Room3FloorCount);
            Assert.AreEqual(createdCeiling, Room3CeilingCount);
            Assert.AreEqual(createdTable, Room3TableCount);
            Assert.AreEqual(createdOther, Room3OtherCount);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountSpawnedItemsRoom1AddMoreAnchors()
        {
            SetupAnchorPrefabSpawner();

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(createdWalls, Room1WallCount);
            Assert.AreEqual(createdFloor, Room1FloorCount);
            Assert.AreEqual(createdCeiling, Room1CeilingCount);
            Assert.AreEqual(createdTable, Room1TableCount);
            Assert.AreEqual(createdOther, Room1OtherCount);

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1MoreAnchors);
            yield return null;

           (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(createdWalls, Room1WallCount);
            Assert.AreEqual(createdFloor, Room1FloorCount);
            Assert.AreEqual(createdCeiling, Room1CeilingCount);
            Assert.AreEqual(createdTable, Room1TableCount + 2);
            Assert.AreEqual(createdOther, Room1OtherCount);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator AllAnchorsHaveSpawnedPrefab()
        {
            var anchorPrefabSpawner = SetupAnchorPrefabSpawner();

            MRUK.Instance.LoadSceneFromJsonString(SceneWithRoom1);
            yield return null;

            foreach (var anchor in MRUK.Instance.GetCurrentRoom().Anchors)
            {
                Assert.AreEqual(true,HasSpawnedChild(anchor));
            }

            yield return null;
        }


        private bool HasSpawnedChild(MRUKAnchor anchorParent)
        {
            return anchorParent.transform.Cast<Transform>().Any(child => child.name.Contains("(PrefabSpawner Clone)"));
        }

        private AnchorPrefabSpawner SetupAnchorPrefabSpawner()
        {
            var anchorPrefabSpawner = FindObjectOfType<AnchorPrefabSpawner>();
            if(anchorPrefabSpawner == null)
            {
                Assert.Fail();
            }
            anchorPrefabSpawner.TrackUpdates = true;
            return anchorPrefabSpawner;
        }

        private IEnumerator DestroyAnchors()
        {
            var allObjects = (MRUKAnchor[]) GameObject.FindObjectsOfType(typeof(MRUKAnchor));
            foreach (var anchor in allObjects)
            {
                DestroyImmediate(anchor);
            }
            yield return null;
        }
    }

}
