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

using JetBrains.Annotations;
using Meta.WitAi.Composer.Data.Info;
using Meta.WitAi;
using Meta.WitAi.Attributes;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Windows;

namespace Meta.Voice.Composer.Data
{
    /// <summary>
    /// Static class that provides a method for refreshing composer data canvases
    /// </summary>
    [UsedImplicitly]
    public static class WitComposerDataRefresher
    {
        /// <summary>
        /// Refreshes all WitComposerData within a configuration
        /// </summary>
        [WitConfigurationAssetRefresh]
        [UsedImplicitly]
        public static void RefreshComposerData(WitConfiguration configuration, WitComposerData composerData)
        {
            // Ignore without server access token
            if (string.IsNullOrEmpty(configuration.GetServerAccessToken()))
            {
                return;
            }

            // Instantiating an assembly walker is a non-trivial operation, so we pull a cached one.
            var conduitManifestGenerationManager = ConduitManifestGenerationManager.GetInstance(configuration);
            var assemblyWalker = conduitManifestGenerationManager.AssemblyWalker;

            // Update
            WitExportRetriever.GetExport(configuration,
                (exportZip, error) =>
                {
                    if (string.IsNullOrEmpty(error))
                    {
                        composerData.canvases = new ComposerParser(assemblyWalker).ExtractComposerInfo(exportZip);
                    }
                    else
                    {
                        VLog.E($"Could not retrieve Composer data for app {configuration.GetApplicationId()}\n{error}\n");
                    }
                });
        }
    }
}
