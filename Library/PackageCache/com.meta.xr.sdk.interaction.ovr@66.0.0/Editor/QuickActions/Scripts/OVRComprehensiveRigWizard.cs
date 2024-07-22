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

using UnityEditor;
using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.Editor.QuickActions;

namespace Oculus.Interaction.OVR.Editor.QuickActions
{
    internal class OVRComprehensiveRigWizard
    {
        private const string MENU_NAME_ADD_NEW_RIG = QuickActionsWizard.MENU_FOLDER +
            "Add OVR Interaction Rig";

        private const string MENU_NAME_ADD_TO_EXISTING_RIG = QuickActionsWizard.MENU_FOLDER +
            "Add Interaction to OVR Camera Rig";

        public static readonly Template OVRInteractionComprehensive =
            new Template(
                "OVRInteractionComprehensive",
                "314f6e1da20902f4ba50509fa6eb8fa8");

        public static readonly Template OVRCameraRigInteraction =
            new Template(
                "OVRCameraRigInteraction",
                "f54012181eacade4ea242c65f804720f");

        [MenuItem(MENU_NAME_ADD_NEW_RIG, priority = MenuOrder.COMPREHENSIVE_RIG_NEW)]
        public static GameObject CreateNewRig()
        {
            GameObject createdRig = Templates.CreateFromTemplate(
                null, OVRCameraRigInteraction, asPrefab: true);
            Selection.activeObject = createdRig;

            return createdRig;
        }

        [MenuItem(MENU_NAME_ADD_NEW_RIG, true)]
        public static bool ValidateCreateNewRig()
        {
            // If no Camera Rig exists, add the OVRCameraRigInteraction prefab
            return FindExistingCameraRig() == null;
        }

        [MenuItem(MENU_NAME_ADD_TO_EXISTING_RIG, priority = MenuOrder.COMPREHENSIVE_RIG_ADD)]
        public static void AddToExistingRig()
        {
            GameObject createdRig;

            OVRCameraRig existingRig = FindExistingCameraRig();
            if (existingRig)
            {
                createdRig = Templates.CreateFromTemplate(
                    existingRig.transform, OVRInteractionComprehensive, asPrefab: true);
                Selection.activeObject = createdRig;

                // Disable existing rig visuals
                foreach (OVRHand ovrCameraRigHand in existingRig.trackingSpace.GetComponentsInChildren<OVRHand>())
                    DisableDuplicateVisuals(ovrCameraRigHand);

                // Wire up  missing references
                AutoWire(existingRig);
            }
        }

        [MenuItem(MENU_NAME_ADD_TO_EXISTING_RIG, true)]
        public static bool ValidateAddToExistingRig()
        {
            // Check for existince of OVRInteraction
            // If Camera Rig exists, but not OVRInteraction, add OVRInteractionComprehensive
            // to the existing Camera Rig and set up
            OVRCameraRig existingRig = FindExistingCameraRig();
            if (existingRig == null || FindExistingInteractionRig() != null)
                return false;

            // Verify Camera Rig hierarchy and that either hands or controllers are present
            if (existingRig.trackingSpace.GetComponentsInChildren<OVRHand>().Length != 2 &&
                existingRig.trackingSpace.GetComponentsInChildren<OVRControllerHelper>().Length != 2)
                return false;

            return true;
        }

        public static OVRCameraRig FindExistingCameraRig()
        {
            return Object.FindObjectOfType<OVRCameraRig>();
        }

        public static OVRCameraRigRef FindExistingInteractionRig()
        {
            return Object.FindObjectOfType<OVRCameraRigRef>();
        }

        // Disable visuals in existing Camera Rig once ISDK visuals are present
        public static void DisableDuplicateVisuals(OVRHand hand)
        {
            if (hand.TryGetComponent<OVRSkeletonRenderer>(out var skeletonRenderer))
                skeletonRenderer.enabled = false;
            if (hand.TryGetComponent<OVRMesh>(out var mesh))
                mesh.enabled = false;
            if (hand.TryGetComponent<OVRMeshRenderer>(out var meshRenderer))
                meshRenderer.enabled = false;
            if (hand.TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
                skinnedMeshRenderer.enabled = false;
            Debug.Log("Duplicate hand visual components in OVRCameraRig disabled");
        }

        // Fill missing references between Camera Rig and Interaction prefabs
        private static void AutoWire(OVRCameraRig cameraRig)
        {
            // Set locomotion origin
            if (cameraRig.GetComponentInChildren<PlayerLocomotor>())
            {
                cameraRig.GetComponentInChildren<PlayerLocomotor>().InjectPlayerOrigin(cameraRig.transform);
                Debug.Log("Auto-wiring succeeded: PlayerLocomotor PlayerOrigin was linked to " + cameraRig.transform.name);
            }
        }
    }
}

