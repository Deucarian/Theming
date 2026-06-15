using System;
using System.Collections.Generic;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Shared implementation for Deucarian theming menu items, window buttons, and editor tests.
    /// </summary>
    public static class DeucarianThemingMenuActions
    {
        public sealed class AssetSearchResult
        {
            internal AssetSearchResult(
                IReadOnlyList<DeucarianTheme> themes,
                IReadOnlyList<DeucarianColorPalette> palettes,
                IReadOnlyList<DeucarianColorRoleLibrary> roleLibraries)
            {
                Themes = themes;
                Palettes = palettes;
                RoleLibraries = roleLibraries;
            }

            public IReadOnlyList<DeucarianTheme> Themes { get; }
            public IReadOnlyList<DeucarianColorPalette> Palettes { get; }
            public IReadOnlyList<DeucarianColorRoleLibrary> RoleLibraries { get; }
        }

        public static AssetSearchResult FindExistingAssets(string[] searchFolders = null, bool autoSelectSingleAssets = false)
        {
            AssetSearchResult result = new AssetSearchResult(
                FindAssets<DeucarianTheme>(searchFolders),
                FindAssets<DeucarianColorPalette>(searchFolders),
                FindAssets<DeucarianColorRoleLibrary>(searchFolders));

            if (autoSelectSingleAssets)
            {
                AutoSelectSingleAsset(result.Themes, DeucarianThemingEditorSettings.ActiveTheme, theme => DeucarianThemingEditorSettings.ActiveTheme = theme);
                AutoSelectSingleAsset(result.Palettes, DeucarianThemingEditorSettings.ActivePalette, palette => DeucarianThemingEditorSettings.ActivePalette = palette);
                AutoSelectSingleAsset(result.RoleLibraries, DeucarianThemingEditorSettings.ActiveRoleLibrary, library => DeucarianThemingEditorSettings.ActiveRoleLibrary = library);
            }

            return result;
        }

        public static IReadOnlyList<T> FindAssets<T>(string[] searchFolders = null)
            where T : UnityEngine.Object
        {
            string[] normalizedFolders = NormalizeSearchFolders(searchFolders);
            if (searchFolders != null && normalizedFolders.Length == 0)
            {
                return Array.Empty<T>();
            }

            string filter = "t:" + typeof(T).Name;
            string[] guids = normalizedFolders == null
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, normalizedFolders);

            List<T> assets = new List<T>();
            HashSet<string> seenGuids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < guids.Length; i++)
            {
                if (!seenGuids.Add(guids[i]))
                {
                    continue;
                }

                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            assets.Sort((left, right) =>
                string.Compare(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right), StringComparison.OrdinalIgnoreCase));
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateMissingDefaultThemeAssets()
        {
            return CreateMissingDefaultThemeAssets(DeucarianThemingEditorSettings.DefaultAssetFolder);
        }

        public static DeucarianDefaultThemeAssets CreateMissingDefaultThemeAssets(string folder)
        {
            string assetFolder = string.IsNullOrWhiteSpace(folder)
                ? DeucarianThemingEditorSettings.DefaultThemeAssetFolder
                : DeucarianThemingEditorSettings.NormalizeAssetPath(folder);

            DeucarianThemingEditorSettings.DefaultAssetFolder = assetFolder;
            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(assetFolder);
            StoreDefaultAssetSelections(assets);
            Debug.Log($"Deucarian default theme assets are ready in {assetFolder}.", assets.Theme);
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateGameThemeAssets()
        {
            return CreateGameThemeAssets(DeucarianDefaultThemeAssetFactory.GameRootFolder);
        }

        public static DeucarianDefaultThemeAssets CreateGameThemeAssets(string folder)
        {
            string assetFolder = string.IsNullOrWhiteSpace(folder)
                ? DeucarianDefaultThemeAssetFactory.GameRootFolder
                : DeucarianThemingEditorSettings.NormalizeAssetPath(folder);

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateGameThemeAssets(assetFolder);
            StoreDefaultAssetSelections(assets);
            Debug.Log($"Deucarian game theme assets are ready in {assetFolder}.", assets.Theme);
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateMinimalPalette()
        {
            string folder = EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string palettePath = CombineAssetPath(folder, DeucarianDefaultThemeAssetFactory.MinimalPaletteFileName);
            return CreateMinimalPalette(palettePath);
        }

        public static DeucarianDefaultThemeAssets CreateMinimalPalette(string palettePath)
        {
            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(palettePath);
            StoreDefaultAssetSelections(assets);
            Debug.Log($"Deucarian minimal palette is ready at {AssetDatabase.GetAssetPath(assets.Palette)}.", assets.Palette);
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateMinimalPaletteFromSavePanel()
        {
            string folder = EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string palettePath = EditorUtility.SaveFilePanelInProject(
                "Create Minimal Palette",
                PathWithoutExtension(DeucarianDefaultThemeAssetFactory.MinimalPaletteFileName),
                "asset",
                "Choose where to create the minimal Deucarian palette.",
                folder);

            if (string.IsNullOrEmpty(palettePath))
            {
                return null;
            }

            DeucarianDefaultThemeAssets assets = CreateMinimalPalette(palettePath);
            SelectAndPing(assets.Palette);
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateThemeFromActivePalette()
        {
            return RepairActivePaletteSetup();
        }

        public static DeucarianDefaultThemeAssets RepairActivePaletteSetup()
        {
            DeucarianColorPalette palette = ResolveOrCreateActivePaletteFirst();
            if (palette == null)
            {
                Debug.LogWarning("No active Deucarian palette is selected. Choose one palette or create a minimal palette first.");
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.RepairPaletteSetup(palette);
            StoreDefaultAssetSelections(assets);
            Debug.Log($"Repaired Deucarian palette setup for '{palette.name}'.", palette);
            return assets;
        }

        public static DeucarianColorPalette CreatePaletteFromTheme(DeucarianTheme theme, string palettePath)
        {
            DeucarianColorPalette palette = DeucarianDefaultThemeAssetFactory.CreatePaletteFromTheme(theme, palettePath);
            DeucarianThemingEditorSettings.ActivePalette = palette;
            if (palette != null && palette.RoleLibrary != null)
            {
                DeucarianThemingEditorSettings.ActiveRoleLibrary = palette.RoleLibrary;
            }

            Debug.Log($"Created Deucarian palette '{palette.name}' from theme '{theme.name}'.", palette);
            return palette;
        }

        public static DeucarianColorPalette CreatePaletteFromActiveThemeFromSavePanel()
        {
            DeucarianTheme theme = ResolveOrCreateActiveTheme();
            if (theme == null || theme.ColorPalette == null)
            {
                Debug.LogWarning("No active Deucarian theme with a palette is selected.");
                return null;
            }

            string folder = EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string palettePath = EditorUtility.SaveFilePanelInProject(
                "Create Palette From Active Theme",
                theme.name + "Palette",
                "asset",
                "Choose where to create or update the palette copy.",
                folder);

            if (string.IsNullOrEmpty(palettePath))
            {
                return null;
            }

            DeucarianColorPalette palette = CreatePaletteFromTheme(theme, palettePath);
            SelectAndPing(palette);
            return palette;
        }

        public static int RepairGeneratedAssetNames(string[] searchFolders = null)
        {
            string[] folders = searchFolders == null
                ? NormalizeSearchFolders(new[] { DeucarianThemingEditorSettings.DefaultProjectFolder })
                : NormalizeSearchFolders(searchFolders);
            if (folders == null || folders.Length == 0)
            {
                Debug.Log("No Deucarian generated asset folders were found to repair.");
                return 0;
            }

            int repaired = 0;
            repaired += RepairAssetNames<DeucarianColorRole>(folders);
            repaired += RepairAssetNames<DeucarianColorRoleLibrary>(folders);
            repaired += RepairAssetNames<DeucarianColorPalette>(folders);
            repaired += RepairAssetNames<DeucarianTheme>(folders);

            if (repaired > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Repaired {repaired} Deucarian generated asset name(s).");
            return repaired;
        }

        public static DeucarianTheme ResolveOrCreateActiveTheme(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
        {
            return ResolveOrCreateActiveAsset(
                DeucarianThemingEditorSettings.ActiveTheme,
                theme => DeucarianThemingEditorSettings.ActiveTheme = theme,
                FindAssets<DeucarianTheme>(searchFolders),
                assets => assets.Theme,
                openManagerForMultiple,
                createFolder);
        }

        public static DeucarianColorPalette ResolveOrCreateActivePaletteFirst(
            bool openManagerForMultiple = true,
            string[] searchFolders = null)
        {
            DeucarianColorPalette activePalette = DeucarianThemingEditorSettings.ActivePalette;
            if (activePalette != null)
            {
                return activePalette;
            }

            IReadOnlyList<DeucarianColorPalette> palettes = FindAssets<DeucarianColorPalette>(searchFolders);
            if (palettes.Count == 1)
            {
                DeucarianThemingEditorSettings.ActivePalette = palettes[0];
                return palettes[0];
            }

            if (palettes.Count == 0)
            {
                DeucarianDefaultThemeAssets assets = CreateMinimalPalette();
                return assets.Palette;
            }

            if (openManagerForMultiple)
            {
                DeucarianThemeManagerWindow.OpenWindow();
            }

            return null;
        }

        public static DeucarianColorPalette ResolveOrCreateActivePalette(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
        {
            return ResolveOrCreateActiveAsset(
                DeucarianThemingEditorSettings.ActivePalette,
                palette => DeucarianThemingEditorSettings.ActivePalette = palette,
                FindAssets<DeucarianColorPalette>(searchFolders),
                assets => assets.Palette,
                openManagerForMultiple,
                createFolder);
        }

        public static DeucarianColorRoleLibrary ResolveOrCreateActiveRoleLibrary(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
        {
            return ResolveOrCreateActiveAsset(
                DeucarianThemingEditorSettings.ActiveRoleLibrary,
                library => DeucarianThemingEditorSettings.ActiveRoleLibrary = library,
                FindAssets<DeucarianColorRoleLibrary>(searchFolders),
                assets => assets.RoleLibrary,
                openManagerForMultiple,
                createFolder);
        }

        public static DeucarianTheme SelectActiveTheme()
        {
            DeucarianTheme theme = ResolveOrCreateActiveTheme();
            SelectAndPing(theme);
            return theme;
        }

        public static DeucarianColorPalette SelectActivePalette()
        {
            DeucarianColorPalette palette = ResolveOrCreateActivePalette();
            SelectAndPing(palette);
            return palette;
        }

        public static DeucarianColorRoleLibrary SelectActiveRoleLibrary()
        {
            DeucarianColorRoleLibrary library = ResolveOrCreateActiveRoleLibrary();
            SelectAndPing(library);
            return library;
        }

        public static void SelectAndPing(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        public static void OpenThemeAssetsFolder()
        {
            string folder = EnsureAssetFolder(DeucarianThemingEditorSettings.DefaultAssetFolder);
            UnityEngine.Object folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
            if (folderAsset != null)
            {
                SelectAndPing(folderAsset);
                return;
            }

            Debug.Log($"Deucarian theme assets folder: {folder}");
        }

        public static int ApplyActiveThemeToOpenScene(bool createProviderIfMissing = true, bool askBeforeCreate = true)
        {
            DeucarianTheme theme = ResolveOrCreateActiveTheme();
            if (theme == null)
            {
                Debug.LogWarning("No active Deucarian theme is selected. Open the Theme Manager and choose one.");
                return 0;
            }

            return ApplyThemeToOpenScene(theme, createProviderIfMissing, askBeforeCreate);
        }

        public static int ApplyThemeToOpenScene(
            DeucarianTheme theme,
            bool createProviderIfMissing = true,
            bool askBeforeCreate = true)
        {
            if (theme == null)
            {
                Debug.LogWarning("Cannot apply a null Deucarian theme to the open scene.");
                return 0;
            }

            DeucarianThemeProvider[] providers = FindThemeProvidersInOpenScenes();
            if (providers.Length == 0)
            {
                if (!createProviderIfMissing || !ShouldCreateThemeProvider(askBeforeCreate))
                {
                    Debug.LogWarning("No DeucarianThemeProvider was found in the open scenes.");
                    return 0;
                }

                DeucarianThemeProvider createdProvider = CreateThemeProvider(theme);
                providers = new[] { createdProvider };
                Selection.activeObject = createdProvider.gameObject;
            }

            int applied = 0;
            for (int i = 0; i < providers.Length; i++)
            {
                DeucarianThemeProvider provider = providers[i];
                if (provider == null || !provider.gameObject.scene.IsValid())
                {
                    continue;
                }

                Undo.RecordObject(provider, "Apply Deucarian Theme");
                provider.SetTheme(theme);
                provider.ApplyThemeToChildren(true);
                EditorUtility.SetDirty(provider);
                EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
                applied++;
            }

            if (applied > 0)
            {
                Debug.Log($"Applied Deucarian theme '{theme.name}' to {applied} theme provider(s).", theme);
            }

            return applied;
        }

        public static string EnsureAssetFolder(string folder)
        {
            string normalized = DeucarianThemingEditorSettings.NormalizeAssetPath(folder);
            if (string.IsNullOrEmpty(normalized))
            {
                normalized = DeucarianThemingEditorSettings.DefaultThemeAssetFolder;
            }

            if (normalized != "Assets" && !normalized.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Theme asset folders must be under the Assets folder.", nameof(folder));
            }

            if (AssetDatabase.IsValidFolder(normalized))
            {
                return normalized;
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

            AssetDatabase.Refresh();
            return normalized;
        }

        private static int RepairAssetNames<T>(string[] searchFolders)
            where T : UnityEngine.Object
        {
            string[] guids = searchFolders == null
                ? AssetDatabase.FindAssets("t:" + typeof(T).Name)
                : AssetDatabase.FindAssets("t:" + typeof(T).Name, searchFolders);

            int repaired = 0;
            HashSet<string> seenGuids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                if (!seenGuids.Add(guids[i]))
                {
                    continue;
                }

                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                string expectedName = PathWithoutExtension(path);
                if (!string.Equals(asset.name, expectedName, StringComparison.Ordinal))
                {
                    asset.name = expectedName;
                    EditorUtility.SetDirty(asset);
                    repaired++;
                }
            }

            return repaired;
        }

        private static string CombineAssetPath(string left, string right)
        {
            return DeucarianThemingEditorSettings.NormalizeAssetPath(left.TrimEnd('/') + "/" + right.TrimStart('/'));
        }

        private static string PathWithoutExtension(string path)
        {
            string fileName = path;
            int slashIndex = fileName.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                fileName = fileName.Substring(slashIndex + 1);
            }

            return fileName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - ".asset".Length)
                : fileName;
        }

        private static T ResolveOrCreateActiveAsset<T>(
            T activeAsset,
            Action<T> setActiveAsset,
            IReadOnlyList<T> foundAssets,
            Func<DeucarianDefaultThemeAssets, T> getCreatedAsset,
            bool openManagerForMultiple,
            string createFolder)
            where T : UnityEngine.Object
        {
            if (activeAsset != null)
            {
                return activeAsset;
            }

            if (foundAssets.Count == 1)
            {
                T onlyAsset = foundAssets[0];
                setActiveAsset(onlyAsset);
                return onlyAsset;
            }

            if (foundAssets.Count == 0)
            {
                DeucarianDefaultThemeAssets assets = CreateMissingDefaultThemeAssets(
                    string.IsNullOrWhiteSpace(createFolder) ? DeucarianThemingEditorSettings.DefaultAssetFolder : createFolder);
                T createdAsset = getCreatedAsset(assets);
                setActiveAsset(createdAsset);
                return createdAsset;
            }

            if (openManagerForMultiple)
            {
                DeucarianThemeManagerWindow.OpenWindow();
            }

            return null;
        }

        private static void StoreDefaultAssetSelections(DeucarianDefaultThemeAssets assets)
        {
            if (assets == null)
            {
                return;
            }

            if (assets.Theme != null)
            {
                DeucarianThemingEditorSettings.ActiveTheme = assets.Theme;
            }

            if (assets.Palette != null)
            {
                DeucarianThemingEditorSettings.ActivePalette = assets.Palette;
            }

            if (assets.RoleLibrary != null)
            {
                DeucarianThemingEditorSettings.ActiveRoleLibrary = assets.RoleLibrary;
            }
        }

        private static void AutoSelectSingleAsset<T>(IReadOnlyList<T> assets, T activeAsset, Action<T> setActiveAsset)
            where T : UnityEngine.Object
        {
            if (activeAsset == null && assets.Count == 1)
            {
                setActiveAsset(assets[0]);
            }
        }

        private static string[] NormalizeSearchFolders(string[] searchFolders)
        {
            if (searchFolders == null)
            {
                return null;
            }

            List<string> validFolders = new List<string>();
            for (int i = 0; i < searchFolders.Length; i++)
            {
                string folder = DeucarianThemingEditorSettings.NormalizeAssetPath(searchFolders[i]);
                if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                {
                    validFolders.Add(folder);
                }
            }

            return validFolders.ToArray();
        }

        private static bool ShouldCreateThemeProvider(bool askBeforeCreate)
        {
            return !askBeforeCreate || EditorUtility.DisplayDialog(
                "Create Deucarian Theme Provider?",
                "No DeucarianThemeProvider exists in the open scenes. Create one named 'Deucarian Theme Provider' and assign the active theme?",
                "Create",
                "Cancel");
        }

        private static DeucarianThemeProvider[] FindThemeProvidersInOpenScenes()
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<DeucarianThemeProvider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            return UnityEngine.Object.FindObjectsOfType<DeucarianThemeProvider>(true);
#pragma warning restore CS0618
#endif
        }

        private static DeucarianThemeProvider CreateThemeProvider(DeucarianTheme theme)
        {
            GameObject gameObject = new GameObject("Deucarian Theme Provider");
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(gameObject, activeScene);
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Deucarian Theme Provider");
            DeucarianThemeProvider provider = gameObject.AddComponent<DeucarianThemeProvider>();
            provider.SetTheme(theme);
            EditorUtility.SetDirty(provider);
            if (provider.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
            }

            return provider;
        }
    }
}
