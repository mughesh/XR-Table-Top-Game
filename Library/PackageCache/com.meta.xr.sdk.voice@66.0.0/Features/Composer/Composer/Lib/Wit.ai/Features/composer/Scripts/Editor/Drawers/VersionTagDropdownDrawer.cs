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
using Meta.WitAi.Composer.Attributes;

namespace Meta.WitAi.Composer.Drawers
{
    using UnityEditor;
    using UnityEngine;
    using System.Linq;

    [CustomPropertyDrawer(typeof(VersionTagDropdownAttribute))]
    public class VersionDropdownDrawer : PropertyDrawer
    {
        private ComposerService _composerService;
        private string[] _versionTagNames;

        private void SetupTagVersionDropDown()
        {
            if (!_composerService ||
                _composerService.VoiceService == null ||
                _composerService.VoiceService.WitConfiguration == null) return;

            var versionTags = _composerService.VoiceService.WitConfiguration.GetApplicationInfo().versionTags;
            var names = null != versionTags ? versionTags.Select(instance => instance.name).ToList() : new List<string>();
            names.Insert(0, "Current");
            _versionTagNames = names.ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _composerService = (ComposerService) property.serializedObject.targetObject;
            EnsureSetup();
            if (!_composerService || !_composerService.VoiceService || !_composerService.VoiceService.WitConfiguration)
            {
                EditorGUILayout.LabelField("Version Tag", "No wit configuration.");
                return;
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                var lastIndex = Array.IndexOf(_versionTagNames,property.stringValue);
                lastIndex = lastIndex < 1 ? 0 : lastIndex;
                int selectedIndex = EditorGUI.Popup(position, label.text, lastIndex, _versionTagNames);
                property.stringValue = selectedIndex<1?string.Empty:_versionTagNames[selectedIndex];
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }

        private void EnsureSetup()
        {
            if (_versionTagNames == null)
            {
                SetupTagVersionDropDown();
            }
        }
    }
}
