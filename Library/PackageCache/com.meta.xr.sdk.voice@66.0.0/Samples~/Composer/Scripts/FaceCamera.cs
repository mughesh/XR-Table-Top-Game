/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;

namespace Meta.WitAi.Composer.Samples
{
    /// <summary>
    /// Class for rotating object to face main camera on update
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        // Main camera
        private static Camera _mainCamera;
        public bool OnlyY = true;

        // Update is called once per frame
        void Update()
        {
            // Obtain main camera
            if (_mainCamera == null)
            {
                // Obtain main camera
                _mainCamera = Camera.main;

                // Not found, log & destroy script
                if (_mainCamera == null)
                {
                    Debug.LogError($"{GetType().Name} - No Main Camera Found");
                    Destroy(this);
                    return;
                }
            }

            // Look at camera
            Vector3 direction = transform.position - _mainCamera.transform.position;
            Quaternion rotation = Quaternion.LookRotation(direction.normalized);
            if (OnlyY)
            {
                rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            }
            transform.rotation = rotation;
        }
    }
}
