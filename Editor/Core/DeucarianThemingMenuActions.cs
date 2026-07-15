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
                IReadOnlyList<DeucarianThemeFamily> themeFamilies,
                IReadOnlyList<DeucarianTheme> themes,
                IReadOnlyList<DeucarianColorPalette> palettes,
                IReadOnlyList<DeucarianColorRoleLibrary> roleLibraries,
                IReadOnlyList<DeucarianThemeStyle> styles)
            {
                ThemeFamilies = themeFamilies;
                Themes = themes;
                Palettes = palettes;
                RoleLibraries = roleLibraries;
                Styles = styles;
            }

            public IReadOnlyList<DeucarianThemeFamily> ThemeFamilies { get; }
            public IReadOnlyList<DeucarianTheme> Themes { get; }
            public IReadOnlyList<DeucarianColorPalette> Palettes { get; }
            public IReadOnlyList<DeucarianColorRoleLibrary> RoleLibraries { get; }
            public IReadOnlyList<DeucarianThemeStyle> Styles { get; }
        }

        public static AssetSearchResult FindExistingAssets(string[] searchFolders = null, bool autoSelectSingleAssets = false)
        {
            AssetSearchResult result = new AssetSearchResult(
                FindAssets<DeucarianThemeFamily>(searchFolders),
                FindAssets<DeucarianTheme>(searchFolders),
                FindAssets<DeucarianColorPalette>(searchFolders),
                FindAssets<DeucarianColorRoleLibrary>(searchFolders),
                FindAssets<DeucarianThemeStyle>(searchFolders));

            if (autoSelectSingleAssets)
            {
                AutoSelectSingleAsset(
                    result.ThemeFamilies,
                    DeucarianThemingEditorSettings.ActiveThemeFamily,
                    family => SetActiveThemeFamilySelection(family, DeucarianThemingEditorSettings.ActiveThemeMode));
                AutoSelectSingleAsset(result.Themes, DeucarianThemingEditorSettings.ActiveTheme, theme => DeucarianThemingEditorSettings.ActiveTheme = theme);
                AutoSelectSingleAsset(result.Palettes, DeucarianThemingEditorSettings.ActivePalette, palette => DeucarianThemingEditorSettings.ActivePalette = palette);
                AutoSelectSingleAsset(result.RoleLibraries, DeucarianThemingEditorSettings.ActiveRoleLibrary, library => DeucarianThemingEditorSettings.ActiveRoleLibrary = library);
                AutoSelectSingleAsset(result.Styles, DeucarianThemingEditorSettings.ActiveStyle, style => DeucarianThemingEditorSettings.ActiveStyle = style);
            }

            return result;
        }

        /// <summary>
        /// Loads the source-controlled runtime settings used as the project theme default.
        /// </summary>
        public static DeucarianThemeRuntimeSettings ResolveProjectRuntimeSettings()
        {
            return DeucarianThemeRuntimeResolver.LoadSettings();
        }

        /// <summary>
        /// Populates an empty or invalid local preview selection from the project runtime default.
        /// Existing valid family selections remain local editor overrides.
        /// </summary>
        public static bool TryHydrateActiveAssetsFromProjectDefault()
        {
            return TryHydrateActiveAssetsFromProjectDefault(ResolveProjectRuntimeSettings());
        }

        /// <summary>
        /// Populates an empty or invalid local preview selection from the supplied runtime settings.
        /// </summary>
        public static bool TryHydrateActiveAssetsFromProjectDefault(
            DeucarianThemeRuntimeSettings settings)
        {
            if (DeucarianThemingEditorSettings.ActiveThemeFamily != null
                || settings == null
                || settings.DefaultThemeFamily == null)
            {
                return false;
            }

            SetActiveThemeFamilySelection(settings.DefaultThemeFamily, settings.DefaultThemeMode);
            return true;
        }

        /// <summary>
        /// Writes the active preview family and mode to the source-controlled runtime settings asset.
        /// </summary>
        public static bool SetActiveThemeFamilyAsProjectDefault()
        {
            return SetActiveThemeFamilyAsProjectDefault(ResolveProjectRuntimeSettings());
        }

        /// <summary>
        /// Writes the active preview family and mode to the supplied runtime settings asset.
        /// </summary>
        public static bool SetActiveThemeFamilyAsProjectDefault(
            DeucarianThemeRuntimeSettings settings)
        {
            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (settings == null)
            {
                ThemingLog.Editor.Warning(
                    "No Deucarian runtime theme settings were found. Create an asset named '"
                    + DeucarianThemeRuntimeSettings.ResourceName
                    + ".asset' in a Resources folder before setting the project default.");
                return false;
            }

            if (family == null)
            {
                ThemingLog.Editor.Warning("Choose an active Deucarian theme family before setting the project default.");
                return false;
            }

            Undo.RecordObject(settings, "Set Deucarian Project Theme Default");
            settings.Configure(family, DeucarianThemingEditorSettings.ActiveThemeMode);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            ThemingLog.Editor.Info(
                $"Set '{family.name}' ({DeucarianThemingEditorSettings.ActiveThemeMode}) as the Deucarian project theme default.",
                settings);
            return true;
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
            ThemingLog.Editor.Info($"Deucarian default theme assets are ready in {assetFolder}.", assets.Theme);
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
            ThemingLog.Editor.Info($"Deucarian game theme assets are ready in {assetFolder}.", assets.Theme);
            return assets;
        }

        public static IReadOnlyList<DeucarianThemeStyle> CreateBuiltinThemeStyleAssets()
        {
            return CreateBuiltinThemeStyleAssets(
                CombineAssetPath(
                    DeucarianThemingEditorSettings.DefaultAssetFolder,
                    DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName));
        }

        public static IReadOnlyList<DeucarianThemeStyle> CreateBuiltinThemeStyleAssets(string folder)
        {
            string assetFolder = string.IsNullOrWhiteSpace(folder)
                ? CombineAssetPath(
                    DeucarianThemingEditorSettings.DefaultAssetFolder,
                    DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName)
                : DeucarianThemingEditorSettings.NormalizeAssetPath(folder);

            IReadOnlyList<DeucarianThemeStyle> styles =
                DeucarianDefaultThemeAssetFactory.CreateBuiltinThemeStyleAssets(assetFolder);
            DeucarianThemeStyle defaultStyle = FindStyleById(styles, DeucarianThemeStyleIds.FrostedGlass)
                ?? (styles.Count > 0 ? styles[0] : null);
            if (defaultStyle != null)
            {
                DeucarianThemingEditorSettings.ActiveStyle = defaultStyle;
            }

            ThemingLog.Editor.Info($"Deucarian built-in theme styles are ready in {assetFolder}.", defaultStyle);
            return styles;
        }

        public static DeucarianDefaultThemeAssets CreateThemeFamily()
        {
            string folder = EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string familyPath = CombineAssetPath(folder, DeucarianDefaultThemeAssetFactory.ThemeFamilyFileName);
            return CreateThemeFamily(familyPath);
        }

        public static DeucarianDefaultThemeAssets CreateThemeFamily(string familyPath)
        {
            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateThemeFamily(familyPath);
            StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info(
                $"Deucarian theme family is ready at {AssetDatabase.GetAssetPath(assets.ThemeFamily)}.",
                assets.ThemeFamily);
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateThemeFamilyFromSavePanel()
        {
            string folder = EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string familyPath = EditorUtility.SaveFilePanelInProject(
                "Create Theme Family",
                PathWithoutExtension(DeucarianDefaultThemeAssetFactory.ThemeFamilyFileName),
                "asset",
                "Choose where to create the paired light and dark Deucarian theme family.",
                folder);

            if (string.IsNullOrEmpty(familyPath))
            {
                return null;
            }

            DeucarianDefaultThemeAssets assets = CreateThemeFamily(familyPath);
            SelectAndPing(assets.ThemeFamily);
            return assets;
        }

        public static DeucarianDefaultThemeAssets RepairActiveThemeFamilySetup()
        {
            DeucarianThemeFamily family = ResolveOrCreateActiveThemeFamily();
            if (family == null)
            {
                ThemingLog.Editor.Warning("No active Deucarian theme family is selected.");
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(family);
            StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info($"Repaired Deucarian theme family '{family.name}'.", family);
            return assets;
        }

        public static DeucarianDefaultThemeAssets WrapActiveThemeInFamily(
            DeucarianThemeMode existingThemeMode,
            string familyPath)
        {
            if (DeucarianThemingEditorSettings.ActiveThemeFamily != null)
            {
                ThemingLog.Editor.Warning(
                    "The active theme already belongs to a theme family. Select a standalone legacy theme before migration.");
                return null;
            }

            DeucarianTheme theme = DeucarianThemingEditorSettings.ActiveTheme;
            if (theme == null)
            {
                ThemingLog.Editor.Warning("Choose an active standalone theme before wrapping it in a family.");
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.WrapExistingThemeInFamily(
                theme,
                existingThemeMode,
                familyPath);
            DeucarianThemingEditorSettings.ActiveThemeMode = existingThemeMode;
            StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info(
                $"Wrapped theme '{theme.name}' as the {existingThemeMode} variant of '{assets.ThemeFamily.name}'.",
                assets.ThemeFamily);
            return assets;
        }

        public static DeucarianDefaultThemeAssets WrapActiveThemeInFamilyFromSavePanel(
            DeucarianThemeMode existingThemeMode)
        {
            if (DeucarianThemingEditorSettings.ActiveThemeFamily != null)
            {
                ThemingLog.Editor.Warning(
                    "The active theme already belongs to a theme family. Select a standalone legacy theme before migration.");
                return null;
            }

            DeucarianTheme theme = DeucarianThemingEditorSettings.ActiveTheme;
            if (theme == null)
            {
                ThemingLog.Editor.Warning("Choose an active standalone theme before wrapping it in a family.");
                return null;
            }

            string folder = EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string familyPath = EditorUtility.SaveFilePanelInProject(
                $"Wrap Theme As {existingThemeMode}",
                theme.name + "Family",
                "asset",
                $"Choose the family asset that will reference '{theme.name}' as its {existingThemeMode} variant.",
                folder);
            return string.IsNullOrEmpty(familyPath)
                ? null
                : WrapActiveThemeInFamily(existingThemeMode, familyPath);
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
            ThemingLog.Editor.Info($"Deucarian minimal palette is ready at {AssetDatabase.GetAssetPath(assets.Palette)}.", assets.Palette);
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
                ThemingLog.Editor.Warning("No active Deucarian palette is selected. Choose one palette or create a minimal palette first.");
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.RepairPaletteSetup(palette);
            StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info($"Repaired Deucarian palette setup for '{palette.name}'.", palette);
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

            ThemingLog.Editor.Info($"Created Deucarian palette '{palette.name}' from theme '{theme.name}'.", palette);
            return palette;
        }

        public static DeucarianColorPalette CreatePaletteFromActiveThemeFromSavePanel()
        {
            DeucarianTheme theme = ResolveOrCreateActiveTheme();
            if (theme == null || theme.ColorPalette == null)
            {
                ThemingLog.Editor.Warning("No active Deucarian theme with a palette is selected.");
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
                ThemingLog.Editor.Info("No Deucarian generated asset folders were found to repair.");
                return 0;
            }

            int repaired = 0;
            repaired += RepairAssetNames<DeucarianThemeFamily>(folders);
            repaired += RepairAssetNames<DeucarianColorRole>(folders);
            repaired += RepairAssetNames<DeucarianColorRoleLibrary>(folders);
            repaired += RepairAssetNames<DeucarianColorPalette>(folders);
            repaired += RepairAssetNames<DeucarianTheme>(folders);
            repaired += RepairAssetNames<DeucarianThemeStyle>(folders);

            if (repaired > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            ThemingLog.Editor.Info($"Repaired {repaired} Deucarian generated asset name(s).");
            return repaired;
        }

        public static DeucarianThemeFamily ResolveOrCreateActiveThemeFamily(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
        {
            DeucarianThemeFamily activeFamily = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (activeFamily != null)
            {
                return activeFamily;
            }

            IReadOnlyList<DeucarianThemeFamily> families = FindAssets<DeucarianThemeFamily>(searchFolders);
            if (families.Count == 1)
            {
                SetActiveThemeFamilySelection(families[0], DeucarianThemingEditorSettings.ActiveThemeMode);
                return families[0];
            }

            if (families.Count == 0)
            {
                string folder = string.IsNullOrWhiteSpace(createFolder)
                    ? DeucarianThemingEditorSettings.DefaultAssetFolder
                    : EnsureAssetFolder(createFolder);
                string familyPath = CombineAssetPath(folder, "DefaultThemeFamily.asset");
                DeucarianDefaultThemeAssets assets = CreateThemeFamily(familyPath);
                return assets.ThemeFamily;
            }

            if (openManagerForMultiple)
            {
                DeucarianThemeManagerWindow.OpenWindow();
            }

            return null;
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

        public static DeucarianThemeStyle ResolveOrCreateActiveStyle(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
        {
            DeucarianThemeStyle activeStyle = DeucarianThemingEditorSettings.ActiveStyle;
            if (activeStyle != null)
            {
                return activeStyle;
            }

            IReadOnlyList<DeucarianThemeStyle> foundStyles = FindAssets<DeucarianThemeStyle>(searchFolders);
            if (foundStyles.Count == 1)
            {
                DeucarianThemingEditorSettings.ActiveStyle = foundStyles[0];
                return foundStyles[0];
            }

            if (foundStyles.Count == 0)
            {
                string folder = string.IsNullOrWhiteSpace(createFolder)
                    ? CombineAssetPath(
                        DeucarianThemingEditorSettings.DefaultAssetFolder,
                        DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName)
                    : createFolder;
                IReadOnlyList<DeucarianThemeStyle> createdStyles = CreateBuiltinThemeStyleAssets(folder);
                return FindStyleById(createdStyles, DeucarianThemeStyleIds.FrostedGlass)
                    ?? (createdStyles.Count > 0 ? createdStyles[0] : null);
            }

            if (openManagerForMultiple)
            {
                DeucarianThemeManagerWindow.OpenWindow();
            }

            return null;
        }

        public static DeucarianTheme SelectActiveTheme()
        {
            DeucarianTheme theme = ResolveOrCreateActiveTheme();
            SelectAndPing(theme);
            return theme;
        }

        public static DeucarianThemeFamily SelectActiveThemeFamily()
        {
            DeucarianThemeFamily family = ResolveOrCreateActiveThemeFamily();
            SelectAndPing(family);
            return family;
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

        public static DeucarianThemeStyle SelectActiveStyle()
        {
            DeucarianThemeStyle style = ResolveOrCreateActiveStyle();
            SelectAndPing(style);
            return style;
        }

        public static int SetActiveThemeFamilyAndApply(DeucarianThemeFamily family)
        {
            SetActiveThemeFamilySelection(family, DeucarianThemingEditorSettings.ActiveThemeMode);
            if (family == null)
            {
                return 0;
            }

            return ApplyThemeFamilyToOpenScene(
                family,
                DeucarianThemingEditorSettings.ActiveThemeMode,
                false,
                false);
        }

        public static int SetActiveThemeModeAndApply(DeucarianThemeMode mode)
        {
            DeucarianThemingEditorSettings.ActiveThemeMode = mode;
            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (family == null)
            {
                return 0;
            }

            SetActiveThemeFamilySelection(family, mode);
            return ApplyThemeFamilyToOpenScene(family, mode, false, false);
        }

        public static int SetActiveThemeAndApply(DeucarianTheme theme)
        {
            DeucarianThemingEditorSettings.ActiveThemeFamily = null;
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            if (theme == null)
            {
                return 0;
            }

            return ApplyThemeToOpenScene(theme, false, false);
        }

        public static bool SetActivePaletteAndApply(DeucarianColorPalette palette)
        {
            DeucarianThemingEditorSettings.ActivePalette = palette;
            if (palette == null)
            {
                return false;
            }

            DeucarianTheme theme = DeucarianThemingEditorSettings.ActiveTheme
                ?? ResolveOrCreateActiveTheme(false);
            if (theme == null)
            {
                return false;
            }

            Undo.RecordObject(theme, "Assign Deucarian Theme Palette");
            theme.SetColorPalette(palette);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            RefreshOpenSceneProvidersUsingAsset(theme);
            return true;
        }

        public static bool SetActiveRoleLibraryAndApply(DeucarianColorRoleLibrary roleLibrary)
        {
            DeucarianThemingEditorSettings.ActiveRoleLibrary = roleLibrary;
            if (roleLibrary == null)
            {
                return false;
            }

            DeucarianColorPalette palette = DeucarianThemingEditorSettings.ActivePalette
                ?? ResolveOrCreateActivePalette(false);
            if (palette == null)
            {
                return false;
            }

            Undo.RecordObject(palette, "Assign Deucarian Palette Role Library");
            palette.SetRoleLibrary(roleLibrary);
            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            RefreshOpenSceneProvidersUsingAsset(palette);
            return true;
        }

        public static bool SetActiveStyleAndApply(DeucarianThemeStyle style)
        {
            DeucarianThemingEditorSettings.ActiveStyle = style;
            if (style == null)
            {
                return false;
            }

            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (family != null)
            {
                return AssignStyleToThemeFamily(family, style);
            }

            DeucarianTheme theme = DeucarianThemingEditorSettings.ActiveTheme
                ?? ResolveOrCreateActiveTheme(false);
            if (theme == null)
            {
                return false;
            }

            Undo.RecordObject(theme, "Assign Deucarian Theme Style");
            theme.SetVisualStyle(style);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            RefreshOpenSceneProvidersUsingAsset(theme);
            return true;
        }

        public static bool AssignActiveStyleToActiveTheme()
        {
            DeucarianTheme theme = ResolveOrCreateActiveTheme();
            DeucarianThemeStyle style = ResolveOrCreateActiveStyle();
            if (theme == null || style == null)
            {
                ThemingLog.Editor.Warning("Assigning a Deucarian style requires both an active theme and an active style.");
                return false;
            }

            Undo.RecordObject(theme, "Assign Deucarian Theme Style");
            theme.SetVisualStyle(style);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            int refreshed = RefreshOpenSceneProvidersUsingAsset(theme);
            string providerNote = refreshed > 0 ? $" Refreshed {refreshed} open scene provider(s)." : string.Empty;
            ThemingLog.Editor.Info(
                $"Assigned Deucarian style '{style.name}' to theme '{theme.name}'.{providerNote}",
                theme);
            return true;
        }

        public static bool AssignActiveStyleToActiveThemeFamily()
        {
            DeucarianThemeFamily family = ResolveOrCreateActiveThemeFamily();
            DeucarianThemeStyle style = ResolveOrCreateActiveStyle();
            if (family == null || style == null)
            {
                ThemingLog.Editor.Warning("Assigning a shared Deucarian style requires an active theme family and style.");
                return false;
            }

            return AssignStyleToThemeFamily(family, style);
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

            ThemingLog.Editor.Info($"Deucarian theme assets folder: {folder}");
        }

        public static int ApplyActiveThemeToOpenScene(bool createProviderIfMissing = true, bool askBeforeCreate = true)
        {
            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (family != null)
            {
                return ApplyThemeFamilyToOpenScene(
                    family,
                    DeucarianThemingEditorSettings.ActiveThemeMode,
                    createProviderIfMissing,
                    askBeforeCreate);
            }

            DeucarianTheme theme = ResolveOrCreateActiveTheme();
            if (theme == null)
            {
                ThemingLog.Editor.Warning("No active Deucarian theme is selected. Open the Theme Manager and choose one.");
                return 0;
            }

            return ApplyThemeToOpenScene(theme, createProviderIfMissing, askBeforeCreate);
        }

        public static int ApplyThemeFamilyToOpenScene(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            bool createProviderIfMissing = true,
            bool askBeforeCreate = true)
        {
            if (family == null)
            {
                ThemingLog.Editor.Warning("Cannot apply a null Deucarian theme family to the open scene.");
                return 0;
            }

            DeucarianTheme resolvedTheme = family.ResolveTheme(mode);
            if (resolvedTheme == null)
            {
                ThemingLog.Editor.Warning(
                    $"Cannot apply theme family '{family.name}' because neither variant is assigned.",
                    family);
                return 0;
            }

            DeucarianThemeProvider[] providers = FindThemeProvidersInOpenScenes();
            if (providers.Length == 0)
            {
                if (!createProviderIfMissing || !ShouldCreateThemeProvider(askBeforeCreate))
                {
                    ThemingLog.Editor.Warning("No DeucarianThemeProvider was found in the open scenes.");
                    return 0;
                }

                DeucarianThemeProvider createdProvider = CreateThemeFamilyProvider();
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

                Undo.RecordObject(provider, "Apply Deucarian Theme Family");
                provider.SetThemeFamily(family, mode);
                EditorUtility.SetDirty(provider);
                EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
                applied++;
            }

            if (applied > 0)
            {
                ThemingLog.Editor.Info(
                    $"Applied Deucarian theme family '{family.name}' in {mode} mode to {applied} theme provider(s).",
                    family);
            }

            return applied;
        }

        public static int ApplyThemeToOpenScene(
            DeucarianTheme theme,
            bool createProviderIfMissing = true,
            bool askBeforeCreate = true)
        {
            if (theme == null)
            {
                ThemingLog.Editor.Warning("Cannot apply a null Deucarian theme to the open scene.");
                return 0;
            }

            DeucarianThemeProvider[] providers = FindThemeProvidersInOpenScenes();
            if (providers.Length == 0)
            {
                if (!createProviderIfMissing || !ShouldCreateThemeProvider(askBeforeCreate))
                {
                    ThemingLog.Editor.Warning("No DeucarianThemeProvider was found in the open scenes.");
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
                ThemingLog.Editor.Info($"Applied Deucarian theme '{theme.name}' to {applied} theme provider(s).", theme);
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

            if (assets.ThemeFamily != null)
            {
                SetActiveThemeFamilySelection(
                    assets.ThemeFamily,
                    DeucarianThemingEditorSettings.ActiveThemeMode);
            }
            else
            {
                DeucarianThemingEditorSettings.ActiveThemeFamily = null;
            }

            if (assets.ThemeFamily == null && assets.Theme != null)
            {
                DeucarianThemingEditorSettings.ActiveTheme = assets.Theme;
            }

            if (assets.ThemeFamily == null && assets.Palette != null)
            {
                DeucarianThemingEditorSettings.ActivePalette = assets.Palette;
            }

            if (assets.RoleLibrary != null)
            {
                DeucarianThemingEditorSettings.ActiveRoleLibrary = assets.RoleLibrary;
            }

            if (assets.DefaultStyle != null)
            {
                DeucarianThemingEditorSettings.ActiveStyle = assets.DefaultStyle;
            }
            else if (assets.Styles.Count > 0)
            {
                DeucarianThemingEditorSettings.ActiveStyle = assets.Styles[0];
            }
        }

        private static void SetActiveThemeFamilySelection(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode)
        {
            DeucarianThemingEditorSettings.ActiveThemeFamily = family;
            DeucarianThemingEditorSettings.ActiveThemeMode = mode;
            if (family == null)
            {
                return;
            }

            DeucarianTheme theme = family.ResolveTheme(mode);
            DeucarianColorPalette palette = theme != null ? theme.ColorPalette : null;
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            DeucarianThemingEditorSettings.ActivePalette = palette;
            DeucarianThemingEditorSettings.ActiveRoleLibrary = palette != null ? palette.RoleLibrary : null;
            if (theme != null && theme.VisualStyle != null)
            {
                DeucarianThemingEditorSettings.ActiveStyle = theme.VisualStyle;
            }
        }

        private static bool AssignStyleToThemeFamily(
            DeucarianThemeFamily family,
            DeucarianThemeStyle style)
        {
            if (family == null || style == null)
            {
                return false;
            }

            DeucarianTheme lightTheme = family.LightTheme;
            DeucarianTheme darkTheme = family.DarkTheme;
            if (lightTheme != null)
            {
                Undo.RecordObject(lightTheme, "Assign Shared Deucarian Theme Style");
            }

            if (darkTheme != null && darkTheme != lightTheme)
            {
                Undo.RecordObject(darkTheme, "Assign Shared Deucarian Theme Style");
            }

            bool changed = family.SetSharedVisualStyle(style);

            if (changed)
            {
                if (lightTheme != null)
                {
                    EditorUtility.SetDirty(lightTheme);
                }

                if (darkTheme != null)
                {
                    EditorUtility.SetDirty(darkTheme);
                }

                AssetDatabase.SaveAssets();
                RefreshOpenSceneProvidersUsingAsset(family);
            }

            ThemingLog.Editor.Info(
                $"Assigned Deucarian style '{style.name}' to both variants of family '{family.name}'.",
                family);
            return true;
        }

        private static DeucarianThemeStyle FindStyleById(IReadOnlyList<DeucarianThemeStyle> styles, string styleId)
        {
            string normalizedId = DeucarianColorRole.NormalizeId(styleId);
            for (int i = 0; i < styles.Count; i++)
            {
                DeucarianThemeStyle style = styles[i];
                if (style != null && string.Equals(style.StyleId, normalizedId, StringComparison.Ordinal))
                {
                    return style;
                }
            }

            return null;
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

        public static int RefreshOpenSceneProvidersUsingAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return 0;
            }

            DeucarianThemeProvider[] providers = FindThemeProvidersInOpenScenes();
            int refreshed = 0;
            for (int i = 0; i < providers.Length; i++)
            {
                DeucarianThemeProvider provider = providers[i];
                if (provider == null
                    || !provider.UsesThemeAsset(asset)
                    || !provider.gameObject.scene.IsValid())
                {
                    continue;
                }

                Undo.RecordObject(provider, "Refresh Deucarian Theme");
                provider.RefreshThemeGraph();
                EditorUtility.SetDirty(provider);
                EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
                refreshed++;
            }

            return refreshed;
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

        private static DeucarianThemeProvider CreateThemeFamilyProvider()
        {
            GameObject gameObject = new GameObject("Deucarian Theme Provider");
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(gameObject, activeScene);
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Deucarian Theme Provider");
            DeucarianThemeProvider provider = gameObject.AddComponent<DeucarianThemeProvider>();
            EditorUtility.SetDirty(provider);
            if (provider.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
            }

            return provider;
        }
    }
}
