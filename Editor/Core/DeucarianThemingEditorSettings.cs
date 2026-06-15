using System;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Stores project editor selections for the Deucarian theming workflow.
    /// </summary>
    public static class DeucarianThemingEditorSettings
    {
        public const string DefaultProjectFolder = "Assets/Deucarian/Theming";
        public const string DefaultThemeAssetFolder = DefaultProjectFolder + "/Defaults";

        private const string ActiveThemeGuidKey = "Deucarian.Theming.ActiveThemeGuid";
        private const string ActivePaletteGuidKey = "Deucarian.Theming.ActivePaletteGuid";
        private const string ActiveRoleLibraryGuidKey = "Deucarian.Theming.ActiveRoleLibraryGuid";
        private const string DefaultAssetFolderKey = "Deucarian.Theming.DefaultAssetFolder";

        public static string ActiveThemeGuid
        {
            get => EditorPrefs.GetString(ActiveThemeGuidKey, string.Empty);
            set => SetGuid(ActiveThemeGuidKey, value);
        }

        public static string ActivePaletteGuid
        {
            get => EditorPrefs.GetString(ActivePaletteGuidKey, string.Empty);
            set => SetGuid(ActivePaletteGuidKey, value);
        }

        public static string ActiveRoleLibraryGuid
        {
            get => EditorPrefs.GetString(ActiveRoleLibraryGuidKey, string.Empty);
            set => SetGuid(ActiveRoleLibraryGuidKey, value);
        }

        public static string DefaultAssetFolder
        {
            get
            {
                string folder = EditorPrefs.GetString(DefaultAssetFolderKey, DefaultThemeAssetFolder);
                return string.IsNullOrWhiteSpace(folder) ? DefaultThemeAssetFolder : NormalizeAssetPath(folder);
            }
            set
            {
                string folder = string.IsNullOrWhiteSpace(value) ? DefaultThemeAssetFolder : NormalizeAssetPath(value);
                EditorPrefs.SetString(DefaultAssetFolderKey, folder);
            }
        }

        public static DeucarianTheme ActiveTheme
        {
            get => LoadAssetByGuid<DeucarianTheme>(ActiveThemeGuid);
            set => ActiveThemeGuid = GetAssetGuid(value);
        }

        public static DeucarianColorPalette ActivePalette
        {
            get => LoadAssetByGuid<DeucarianColorPalette>(ActivePaletteGuid);
            set => ActivePaletteGuid = GetAssetGuid(value);
        }

        public static DeucarianColorRoleLibrary ActiveRoleLibrary
        {
            get => LoadAssetByGuid<DeucarianColorRoleLibrary>(ActiveRoleLibraryGuid);
            set => ActiveRoleLibraryGuid = GetAssetGuid(value);
        }

        public static string GetAssetGuid(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(assetPath) ? string.Empty : AssetDatabase.AssetPathToGUID(assetPath);
        }

        public static T LoadAssetByGuid<T>(string guid)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').Trim().TrimEnd('/');
        }

        public static void ClearActiveAssets()
        {
            ActiveThemeGuid = string.Empty;
            ActivePaletteGuid = string.Empty;
            ActiveRoleLibraryGuid = string.Empty;
        }

        public static void ResetToDefaults()
        {
            EditorPrefs.DeleteKey(ActiveThemeGuidKey);
            EditorPrefs.DeleteKey(ActivePaletteGuidKey);
            EditorPrefs.DeleteKey(ActiveRoleLibraryGuidKey);
            EditorPrefs.DeleteKey(DefaultAssetFolderKey);
        }

        private static void SetGuid(string key, string guid)
        {
            EditorPrefs.SetString(key, string.IsNullOrWhiteSpace(guid) ? string.Empty : guid.Trim());
        }
    }
}
