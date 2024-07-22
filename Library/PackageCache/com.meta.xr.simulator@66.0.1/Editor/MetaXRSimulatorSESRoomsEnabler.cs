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

#if USING_XR_MANAGEMENT && (USING_XR_SDK_OCULUS || USING_XR_SDK_OPENXR)
#define USING_XR_SDK
#endif

#if UNITY_2020_1_OR_NEWER
#define REQUIRES_XR_SDK
#endif

using System.IO;

using UnityEditor;

public static partial class MetaXRSimulatorEnabler
{

    private static readonly string FullPath =
        Path.GetFullPath(PackagePath + "/MetaXRSimulator/json_synthenv_server~/json_server.exe");

    [MenuItem(kSynthEnvServerMenu + "/JSON Server")]
    public static void LaunchJSONServer()
    {
        var jsonPath = EditorUtility.OpenFilePanel("Open File", "", "json");
        LaunchEnvironment(jsonPath, FullPath);
        LaunchLocalSharingServer();
    }

    [MenuItem(kSynthEnvServerMenu + "/More Rooms/Room with Staircase")]
    public static void LaunchXRoom1_1()
    {
        LaunchSESRoom("XRoom1_1");
        LaunchLocalSharingServer();
    }

    [MenuItem(kSynthEnvServerMenu + "/More Rooms/Office")]
    public static void LaunchXRoom1_2()
    {
        LaunchSESRoom("XRoom1_2");
        LaunchLocalSharingServer();
    }

    [MenuItem(kSynthEnvServerMenu + "/More Rooms/Trapezoidal Room")]
    public static void LaunchXRoom1_4()
    {
        LaunchSESRoom("XRoom1_4");
        LaunchLocalSharingServer();
    }

    [MenuItem(kSynthEnvServerMenu + "/More Rooms/Corridor")]
    public static void LaunchXRoom2_3()
    {
        LaunchSESRoom("XRoom2_3");
        LaunchLocalSharingServer();
    }

    [MenuItem(kSynthEnvServerMenu + "/More Rooms/Furniture-filled Room")]
    public static void LaunchXRoom2_4()
    {
        LaunchSESRoom("XRoom2_4");
        LaunchLocalSharingServer();
    }

    [MenuItem(kSynthEnvServerMenu + "/More Rooms/Living Room with Multiple Spaces")]
    public static void LaunchXRoom3_1()
    {
        LaunchSESRoom("XRoom3_1");
        LaunchLocalSharingServer();
    }

    [MenuItem(kSynthEnvServerMenu + "/More Rooms/L-shape Room")]
    public static void LaunchXRoom5_1()
    {
        LaunchSESRoom("XRoom5_1");
        LaunchLocalSharingServer();
    }

    [MenuItem(kSynthEnvServerMenu + "/More Rooms/High-ceiling Room")]
    public static void LaunchXRoom5_5()
    {
        LaunchSESRoom("XRoom5_6");
        LaunchLocalSharingServer();
    }

    private static void LaunchSESRoom(string environmentName)
    {
        LaunchEnvironment(environmentName, GetSESRoomPath());
    }

    private static string GetSESRoomPath()
    {
        return Path.GetFullPath(PackagePath + "/MetaXRSimulator/ses_rooms~/synth_env_server_extra_rooms.exe");
    }
}
