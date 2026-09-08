using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Creates project assets from package-provided theme pack definitions.
    /// </summary>
    public static class DeucarianThemePackAssetFactory
    {
        internal const string RolesFolderName = "Roles";

        public static DeucarianDefaultThemeAssets CreateOrRepairThemePackAssets(
            DeucarianThemePack themePack,
            string rootFolder,
            bool overwriteExisting = false)
        {
            if (themePack == null)
            {
                throw new ArgumentNullException(nameof(themePack));
            }

            return themePack.SupportsThemeFamily
                ? DeucarianThemeFamilyPackAssets.CreateOrRepairThemeFamilyPackAssets(themePack, rootFolder, overwriteExisting)
                : DeucarianLegacyThemePackAssets.CreateOrRepairLegacyThemePackAssets(themePack, rootFolder, overwriteExisting);
        }

        public static DeucarianThemeRuntimeSettings CreateOrRepairRuntimeSettings(
            string resourcesFolder,
            DeucarianTheme defaultTheme,
            bool overwriteExisting = false)
        {
            return DeucarianThemePackRuntimeSettings.CreateOrRepairRuntimeSettings(resourcesFolder, defaultTheme, overwriteExisting);
        }

        public static DeucarianThemeRuntimeSettings CreateOrRepairRuntimeSettings(
            string resourcesFolder,
            DeucarianThemeFamily defaultThemeFamily,
            DeucarianThemeMode defaultThemeMode,
            bool overwriteExisting = false)
        {
            return DeucarianThemePackRuntimeSettings.CreateOrRepairRuntimeSettings(resourcesFolder, defaultThemeFamily, defaultThemeMode, overwriteExisting);
        }

    }
}
