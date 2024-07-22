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

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// A struct that can filter certain labels. The default is
    /// to allow all labels.
    /// </summary>
    public struct LabelFilter
    {
        private MRUKAnchor.SceneLabels? _included;
        private MRUKAnchor.SceneLabels _excluded;

        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public static LabelFilter Included(List<string> included) => Included(Utilities.StringLabelsToEnum(included));

        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public static LabelFilter Excluded(List<string> excluded) => Excluded(Utilities.StringLabelsToEnum(excluded));

        /// <see cref="OVRSemanticLabels.DeprecationMessage"/>
        [Obsolete("Use '" + nameof(Included) + "()' instead.")]
        public static LabelFilter FromEnum(MRUKAnchor.SceneLabels labels) => Included(labels);

        [Obsolete(OVRSemanticLabels.DeprecationMessage)]
        public bool PassesFilter(List<string> labels) => PassesFilter(Utilities.StringLabelsToEnum(labels));

        public static LabelFilter Included(MRUKAnchor.SceneLabels labelFlags) => new LabelFilter { _included = labelFlags };

        public static LabelFilter Excluded(MRUKAnchor.SceneLabels labelFlags) => new LabelFilter { _excluded = labelFlags };

        public bool PassesFilter(MRUKAnchor.SceneLabels labelFlags)
        {
            if ((_excluded & labelFlags) != 0)
                return false;
            if (_included.HasValue)
                return (_included.Value & labelFlags) != 0;
            return true;
        }
    }
}
