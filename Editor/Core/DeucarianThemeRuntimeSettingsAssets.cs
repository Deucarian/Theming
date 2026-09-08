using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeRuntimeSettingsAssets
    {
        internal static bool TryValidateRuntimeSettingsCandidate(
            DeucarianThemeRuntimeSettings candidate,
            out string message)
        {
            if (candidate == null)
            {
                message = "Select an existing runtime settings asset.";
                return false;
            }

            string path = AssetDatabase.GetAssetPath(candidate);
            if (!IsRuntimeSettingsResourcePath(path))
            {
                message = "The asset must use the exact DeucarianThemeRuntimeSettings.asset filename inside a Resources folder.";
                return false;
            }

            IReadOnlyList<DeucarianThemeRuntimeSettings> resources =
                FindRuntimeSettingsResourceAssets();
            if (resources.Count != 1)
            {
                message = resources.Count == 0
                    ? "Unity cannot find this settings asset as a runtime resource."
                    : $"Found {resources.Count} runtime settings resources. Keep exactly one and remove or rename the duplicates.";
                return false;
            }

            if (resources[0] != candidate
                || DeucarianThemingMenuActions.ResolveProjectRuntimeSettings() != candidate)
            {
                message = "Unity resolves a different runtime settings asset. Keep one exact resource and select it here.";
                return false;
            }

            message = "Runtime settings resource is unique and resolvable.";
            return true;
        }

        internal static IReadOnlyList<DeucarianThemeRuntimeSettings> FindRuntimeSettingsResourceAssets()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:" + nameof(DeucarianThemeRuntimeSettings));
            var settings = new List<DeucarianThemeRuntimeSettings>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!IsRuntimeSettingsResourcePath(path))
                {
                    continue;
                }

                DeucarianThemeRuntimeSettings asset =
                    AssetDatabase.LoadAssetAtPath<DeucarianThemeRuntimeSettings>(path);
                if (asset != null)
                {
                    settings.Add(asset);
                }
            }

            return settings;
        }

        internal static DeucarianThemeRuntimeSettings CreateRuntimeSettingsAtPath(string assetPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ThemingLog.Editor.Warning("Exit Play Mode before creating runtime settings.");
                return null;
            }

            string normalizedPath = DeucarianThemingEditorSettings.NormalizeAssetPath(assetPath);
            if (!IsRuntimeSettingsResourcePath(normalizedPath)
                || AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null
                || FindRuntimeSettingsResourceAssets().Count > 0)
            {
                return null;
            }

            int slash = normalizedPath.LastIndexOf('/');
            if (slash > 0)
            {
                DeucarianThemingMenuActions.EnsureAssetFolder(normalizedPath.Substring(0, slash));
            }

            DeucarianThemeRuntimeSettings settings =
                ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>();
            AssetDatabase.CreateAsset(settings, normalizedPath);
            AssetDatabase.SaveAssetIfDirty(settings);
            AssetDatabase.Refresh();
            return settings;
        }

        internal static bool IsRuntimeSettingsResourcePath(string assetPath)
        {
            string normalizedPath = DeucarianThemingEditorSettings.NormalizeAssetPath(assetPath);
            if (string.IsNullOrWhiteSpace(normalizedPath)
                || !normalizedPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return false;
            }

            string expectedFile = "/" + DeucarianThemeRuntimeSettings.ResourceName + ".asset";
            return normalizedPath.EndsWith(expectedFile, StringComparison.OrdinalIgnoreCase)
                   && normalizedPath.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool CanCreateRuntimeSettings(
            int existingRuntimeSettingsCount,
            bool isPlaying)
        {
            return existingRuntimeSettingsCount == 0 && !isPlaying;
        }
    }
}
