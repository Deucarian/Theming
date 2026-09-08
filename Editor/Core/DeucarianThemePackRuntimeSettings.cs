using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;


namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemePackRuntimeSettings
    {
        internal static DeucarianThemeRuntimeSettings CreateOrRepairRuntimeSettings(
            string resourcesFolder,
            DeucarianTheme defaultTheme,
            bool overwriteExisting = false)
        {
            string normalizedFolder = DeucarianThemeAssetNaming.NormalizeAssetPath(resourcesFolder);
            DeucarianThemePackAssetPolicy.ValidateAssetFolder(normalizedFolder, nameof(resourcesFolder));
            DeucarianThemeAssetStore.EnsureFolder(normalizedFolder);

            string settingsPath = DeucarianThemeAssetNaming.CombineAssetPath(
                normalizedFolder,
                DeucarianThemeRuntimeSettings.ResourceName + ".asset");
            DeucarianThemeRuntimeSettings settings = DeucarianThemeAssetStore.LoadOrCreateAsset(
                settingsPath,
                () => ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>(),
                overwriteExisting,
                out bool settingsCreated);

            if (settingsCreated
                || overwriteExisting
                || settings.DefaultThemeFamily != null
                || settings.LegacyDefaultTheme != defaultTheme)
            {
                settings.Configure(defaultTheme);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return settings;
        }

        internal static DeucarianThemeRuntimeSettings CreateOrRepairRuntimeSettings(
            string resourcesFolder,
            DeucarianThemeFamily defaultThemeFamily,
            DeucarianThemeMode defaultThemeMode,
            bool overwriteExisting = false)
        {
            string normalizedFolder = DeucarianThemeAssetNaming.NormalizeAssetPath(resourcesFolder);
            DeucarianThemePackAssetPolicy.ValidateAssetFolder(normalizedFolder, nameof(resourcesFolder));
            DeucarianThemeAssetStore.EnsureFolder(normalizedFolder);

            string settingsPath = DeucarianThemeAssetNaming.CombineAssetPath(
                normalizedFolder,
                DeucarianThemeRuntimeSettings.ResourceName + ".asset");
            DeucarianThemeRuntimeSettings settings = DeucarianThemeAssetStore.LoadOrCreateAsset(
                settingsPath,
                () => ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>(),
                overwriteExisting,
                out bool settingsCreated);

            if (settingsCreated
                || overwriteExisting
                || settings.DefaultThemeFamily != defaultThemeFamily
                || settings.LegacyDefaultTheme != null
                || settings.DefaultThemeMode != defaultThemeMode)
            {
                settings.Configure(defaultThemeFamily, defaultThemeMode);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return settings;
        }
    }
}
