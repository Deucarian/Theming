using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Shared editor helpers for Deucarian theming menu items and manager windows.
    /// </summary>
    public static class DeucarianThemingEditorAssetUtility
    {
        public const string ActiveThemeGuidKey = "Deucarian.Theming.ActiveThemeGuid";
        public const string ActivePaletteGuidKey = "Deucarian.Theming.ActivePaletteGuid";
        public const string ActiveRoleLibraryGuidKey = "Deucarian.Theming.ActiveRoleLibraryGuid";

        [MenuItem("Deucarian/Theming/Select Active Theme")]
        public static void SelectActiveThemeFromMenu()
        {
            SelectSingleAssetOrOpenManager(
                "theme",
                GetActiveTheme,
                SetActiveTheme);
        }

        [MenuItem("Deucarian/Theming/Select Active Palette")]
        public static void SelectActivePaletteFromMenu()
        {
            SelectSingleAssetOrOpenManager(
                "palette",
                GetActivePalette,
                SetActivePalette);
        }

        [MenuItem("Deucarian/Theming/Select Role Library")]
        public static void SelectActiveRoleLibraryFromMenu()
        {
            SelectSingleAssetOrOpenManager(
                "role library",
                GetActiveRoleLibrary,
                SetActiveRoleLibrary);
        }

        [MenuItem("Deucarian/Theming/Open Theme Assets Folder")]
        public static void OpenThemeAssetsFolder()
        {
            EnsureFolder(DeucarianDefaultThemeAssetFactory.DefaultRootFolder);

            Object folder = AssetDatabase.LoadAssetAtPath<Object>(
                DeucarianDefaultThemeAssetFactory.DefaultRootFolder);
            SelectAndPing(folder);
            EditorUtility.FocusProjectWindow();
        }

        [MenuItem("Deucarian/Theming/Apply Active Theme To Open Scene")]
        public static void ApplyActiveThemeToOpenSceneFromMenu()
        {
            DeucarianTheme theme = ResolveActiveThemeForAction();
            if (theme == null)
            {
                return;
            }

            int providerCount = ApplyThemeToOpenScene(theme);
            Debug.Log(
                $"[Deucarian.Theming] Applied active theme '{theme.DisplayName}' to {providerCount} theme provider(s).",
                theme);
        }

        public static DeucarianDefaultThemeAssets CreateMissingDefaultThemeAssets(string rootFolder)
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(rootFolder);

            if (assets.Theme != null)
            {
                SetActiveTheme(assets.Theme);
            }

            if (assets.Palette != null)
            {
                SetActivePalette(assets.Palette);
            }

            if (assets.RoleLibrary != null)
            {
                SetActiveRoleLibrary(assets.RoleLibrary);
            }

            return assets;
        }

        public static DeucarianTheme GetActiveTheme()
        {
            return GetActiveAsset<DeucarianTheme>(ActiveThemeGuidKey);
        }

        public static void SetActiveTheme(DeucarianTheme theme)
        {
            SetActiveAsset(ActiveThemeGuidKey, theme);
        }

        public static DeucarianColorPalette GetActivePalette()
        {
            return GetActiveAsset<DeucarianColorPalette>(ActivePaletteGuidKey);
        }

        public static void SetActivePalette(DeucarianColorPalette palette)
        {
            SetActiveAsset(ActivePaletteGuidKey, palette);
        }

        public static DeucarianColorRoleLibrary GetActiveRoleLibrary()
        {
            return GetActiveAsset<DeucarianColorRoleLibrary>(ActiveRoleLibraryGuidKey);
        }

        public static void SetActiveRoleLibrary(DeucarianColorRoleLibrary roleLibrary)
        {
            SetActiveAsset(ActiveRoleLibraryGuidKey, roleLibrary);
        }

        public static IReadOnlyList<T> FindAssets<T>()
            where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            List<T> assets = new List<T>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            assets.Sort(CompareAssetPaths);
            return assets;
        }

        public static void SelectAndPing(Object asset)
        {
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        public static int ApplyThemeToOpenScene(DeucarianTheme theme)
        {
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            DeucarianThemeProvider[] providers = FindThemeProviders();
            List<DeucarianThemeProvider> sceneProviders = new List<DeucarianThemeProvider>(providers.Length);

            for (int i = 0; i < providers.Length; i++)
            {
                DeucarianThemeProvider provider = providers[i];
                if (provider != null && !EditorUtility.IsPersistent(provider))
                {
                    sceneProviders.Add(provider);
                }
            }

            if (sceneProviders.Count == 0)
            {
                GameObject providerObject = new GameObject("Deucarian Theme Provider");
                Undo.RegisterCreatedObjectUndo(providerObject, "Create Deucarian Theme Provider");
                sceneProviders.Add(providerObject.AddComponent<DeucarianThemeProvider>());
            }

            for (int i = 0; i < sceneProviders.Count; i++)
            {
                DeucarianThemeProvider provider = sceneProviders[i];
                Undo.RecordObject(provider, "Apply Deucarian Theme");
                provider.SetTheme(theme);
                EditorUtility.SetDirty(provider);
                MarkSceneDirty(provider.gameObject.scene);
            }

            return sceneProviders.Count;
        }

        private static T SelectSingleAssetOrOpenManager<T>(
            string assetLabel,
            Func<T> getActiveAsset,
            Action<T> setActiveAsset)
            where T : ScriptableObject
        {
            IReadOnlyList<T> assets = FindAssets<T>();
            if (assets.Count == 0)
            {
                CreateMissingDefaultThemeAssets(DeucarianDefaultThemeAssetFactory.DefaultRootFolder);
                assets = FindAssets<T>();
            }

            if (assets.Count == 1)
            {
                T asset = assets[0];
                setActiveAsset(asset);
                SelectAndPing(asset);
                Debug.Log($"[Deucarian.Theming] Selected active {assetLabel}: {asset.name}", asset);
                return asset;
            }

            if (assets.Count > 1)
            {
                DeucarianThemeManagerWindow.Open();
                Debug.Log(
                    $"[Deucarian.Theming] Multiple {assetLabel} assets found. Select the active asset in the Theme Manager.");
                return getActiveAsset();
            }

            Debug.LogWarning($"[Deucarian.Theming] No {assetLabel} assets were found or created.");
            return null;
        }

        private static DeucarianTheme ResolveActiveThemeForAction()
        {
            DeucarianTheme theme = GetActiveTheme();
            if (theme != null)
            {
                return theme;
            }

            IReadOnlyList<DeucarianTheme> themes = FindAssets<DeucarianTheme>();
            if (themes.Count == 0)
            {
                DeucarianDefaultThemeAssets assets =
                    CreateMissingDefaultThemeAssets(DeucarianDefaultThemeAssetFactory.DefaultRootFolder);
                return assets.Theme;
            }

            if (themes.Count == 1)
            {
                theme = themes[0];
                SetActiveTheme(theme);
                SelectAndPing(theme);
                return theme;
            }

            DeucarianThemeManagerWindow.Open();
            Debug.LogWarning(
                "[Deucarian.Theming] Multiple theme assets exist and no active theme is selected. " +
                "Choose an active theme in the Theme Manager, then apply it again.");
            return null;
        }

        private static T GetActiveAsset<T>(string key)
            where T : Object
        {
            string guid = EditorPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void SetActiveAsset(string key, Object asset)
        {
            if (asset == null)
            {
                EditorPrefs.DeleteKey(key);
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning(
                    $"[Deucarian.Theming] Active asset '{asset.name}' must be saved in the AssetDatabase.",
                    asset);
                return;
            }

            EditorPrefs.SetString(key, guid);
        }

        private static void EnsureFolder(string folder)
        {
            string normalized = NormalizeAssetPath(folder);
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string[] parts = normalized.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').Trim().TrimEnd('/');
        }

        private static int CompareAssetPaths<T>(T left, T right)
            where T : Object
        {
            return string.Compare(
                AssetDatabase.GetAssetPath(left),
                AssetDatabase.GetAssetPath(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private static void MarkSceneDirty(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static DeucarianThemeProvider[] FindThemeProviders()
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            return Object.FindObjectsByType<DeucarianThemeProvider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            return Object.FindObjectsOfType<DeucarianThemeProvider>(true);
#pragma warning restore CS0618
#endif
        }
    }
}
