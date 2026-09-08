using System;
using System.Collections.Generic;
using Deucarian.Editor;
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
            => DeucarianThemeSelectionActions.FindExistingAssets(searchFolders, autoSelectSingleAssets);

        /// <summary>
        /// Loads the source-controlled runtime settings used as the project theme default.
        /// </summary>
        public static DeucarianThemeRuntimeSettings ResolveProjectRuntimeSettings()
            => DeucarianThemeSelectionCommands.ResolveProjectRuntimeSettings();

        /// <summary>
        /// Populates an empty or invalid local preview selection from the project runtime default.
        /// Existing valid family selections remain local editor overrides.
        /// </summary>
        public static bool TryHydrateActiveAssetsFromProjectDefault()
            => DeucarianThemeSelectionCommands.TryHydrateActiveAssetsFromProjectDefault();

        /// <summary>
        /// Populates an empty or invalid local preview selection from the supplied runtime settings.
        /// </summary>
        public static bool TryHydrateActiveAssetsFromProjectDefault(
            DeucarianThemeRuntimeSettings settings)
            => DeucarianThemeSelectionCommands.TryHydrateActiveAssetsFromProjectDefault(settings);

        /// <summary>
        /// Writes the active preview family and mode to the source-controlled runtime settings asset.
        /// </summary>
        public static bool SetActiveThemeFamilyAsProjectDefault()
            => DeucarianThemeSelectionCommands.SetActiveThemeFamilyAsProjectDefault();

        /// <summary>
        /// Writes the active preview family and mode to the supplied runtime settings asset.
        /// </summary>
        public static bool SetActiveThemeFamilyAsProjectDefault(
            DeucarianThemeRuntimeSettings settings)
            => DeucarianThemeSelectionCommands.SetActiveThemeFamilyAsProjectDefault(settings);

        public static IReadOnlyList<T> FindAssets<T>(string[] searchFolders = null)
            where T : UnityEngine.Object
            => DeucarianThemeAssetCatalog.FindAssets<T>(searchFolders);

        public static DeucarianDefaultThemeAssets CreateMissingDefaultThemeAssets()
            => DeucarianThemeAssetSetup.CreateMissingDefaultThemeAssets();

        public static DeucarianDefaultThemeAssets CreateMissingDefaultThemeAssets(string folder)
            => DeucarianThemeAssetSetup.CreateMissingDefaultThemeAssets(folder);

        public static DeucarianDefaultThemeAssets CreateGameThemeAssets()
            => DeucarianThemeAssetSetup.CreateGameThemeAssets();

        public static DeucarianDefaultThemeAssets CreateGameThemeAssets(string folder)
            => DeucarianThemeAssetSetup.CreateGameThemeAssets(folder);

        public static IReadOnlyList<DeucarianThemeStyle> CreateBuiltinThemeStyleAssets()
            => DeucarianThemeAssetSetup.CreateBuiltinThemeStyleAssets();

        public static IReadOnlyList<DeucarianThemeStyle> CreateBuiltinThemeStyleAssets(string folder)
            => DeucarianThemeAssetSetup.CreateBuiltinThemeStyleAssets(folder);

        public static DeucarianDefaultThemeAssets CreateThemeFamily()
            => DeucarianThemeAssetSetup.CreateThemeFamily();

        public static DeucarianDefaultThemeAssets CreateThemeFamily(string familyPath)
            => DeucarianThemeAssetSetup.CreateThemeFamily(familyPath);

        public static DeucarianDefaultThemeAssets CreateThemeFamilyFromSavePanel()
            => DeucarianThemeAssetDialogs.CreateThemeFamilyFromSavePanel();

        public static DeucarianDefaultThemeAssets RepairActiveThemeFamilySetup()
            => DeucarianThemeAssetSetup.RepairActiveThemeFamilySetup();

        public static DeucarianDefaultThemeAssets WrapActiveThemeInFamily(
            DeucarianThemeMode existingThemeMode,
            string familyPath)
            => DeucarianThemeAssetSetup.WrapActiveThemeInFamily(existingThemeMode, familyPath);

        public static DeucarianDefaultThemeAssets WrapActiveThemeInFamilyFromSavePanel(
            DeucarianThemeMode existingThemeMode)
            => DeucarianThemeAssetDialogs.WrapActiveThemeInFamilyFromSavePanel(existingThemeMode);

        public static DeucarianDefaultThemeAssets CreateMinimalPalette()
            => DeucarianThemeAssetSetup.CreateMinimalPalette();

        public static DeucarianDefaultThemeAssets CreateMinimalPalette(string palettePath)
            => DeucarianThemeAssetSetup.CreateMinimalPalette(palettePath);

        public static DeucarianDefaultThemeAssets CreateMinimalPaletteFromSavePanel()
            => DeucarianThemeAssetDialogs.CreateMinimalPaletteFromSavePanel();

        public static DeucarianDefaultThemeAssets CreateThemeFromActivePalette()
            => DeucarianThemeAssetSetup.CreateThemeFromActivePalette();

        public static DeucarianDefaultThemeAssets RepairActivePaletteSetup()
            => DeucarianThemeAssetSetup.RepairActivePaletteSetup();

        public static DeucarianColorPalette CreatePaletteFromTheme(DeucarianTheme theme, string palettePath)
            => DeucarianThemeAssetSetup.CreatePaletteFromTheme(theme, palettePath);

        public static DeucarianColorPalette CreatePaletteFromActiveThemeFromSavePanel()
            => DeucarianThemeAssetDialogs.CreatePaletteFromActiveThemeFromSavePanel();

        public static int RepairGeneratedAssetNames(string[] searchFolders = null)
            => DeucarianThemeAssetCatalog.RepairGeneratedAssetNames(searchFolders);

        public static DeucarianThemeFamily ResolveOrCreateActiveThemeFamily(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
            => DeucarianThemeSelectionActions.ResolveOrCreateActiveThemeFamily(openManagerForMultiple, searchFolders, createFolder);

        public static DeucarianTheme ResolveOrCreateActiveTheme(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
            => DeucarianThemeSelectionActions.ResolveOrCreateActiveTheme(openManagerForMultiple, searchFolders, createFolder);

        public static DeucarianColorPalette ResolveOrCreateActivePaletteFirst(
            bool openManagerForMultiple = true,
            string[] searchFolders = null)
            => DeucarianThemeSelectionActions.ResolveOrCreateActivePaletteFirst(openManagerForMultiple, searchFolders);

        public static DeucarianColorPalette ResolveOrCreateActivePalette(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
            => DeucarianThemeSelectionActions.ResolveOrCreateActivePalette(openManagerForMultiple, searchFolders, createFolder);

        public static DeucarianColorRoleLibrary ResolveOrCreateActiveRoleLibrary(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
            => DeucarianThemeSelectionActions.ResolveOrCreateActiveRoleLibrary(openManagerForMultiple, searchFolders, createFolder);

        public static DeucarianThemeStyle ResolveOrCreateActiveStyle(
            bool openManagerForMultiple = true,
            string[] searchFolders = null,
            string createFolder = null)
            => DeucarianThemeSelectionActions.ResolveOrCreateActiveStyle(openManagerForMultiple, searchFolders, createFolder);

        public static DeucarianTheme SelectActiveTheme()
            => DeucarianThemeSelectionActions.SelectActiveTheme();

        public static DeucarianThemeFamily SelectActiveThemeFamily()
            => DeucarianThemeSelectionActions.SelectActiveThemeFamily();

        public static DeucarianColorPalette SelectActivePalette()
            => DeucarianThemeSelectionActions.SelectActivePalette();

        public static DeucarianColorRoleLibrary SelectActiveRoleLibrary()
            => DeucarianThemeSelectionActions.SelectActiveRoleLibrary();

        public static DeucarianThemeStyle SelectActiveStyle()
            => DeucarianThemeSelectionActions.SelectActiveStyle();

        public static int SetActiveThemeFamilyAndApply(DeucarianThemeFamily family)
            => DeucarianThemeSelectionCommands.SetActiveThemeFamilyAndApply(family);

        public static int SetActiveThemeModeAndApply(DeucarianThemeMode mode)
            => DeucarianThemeSelectionCommands.SetActiveThemeModeAndApply(mode);

        public static int SetActiveThemeAndApply(DeucarianTheme theme)
            => DeucarianThemeSelectionCommands.SetActiveThemeAndApply(theme);

        public static bool SetActivePaletteAndApply(DeucarianColorPalette palette)
            => DeucarianThemeSelectionCommands.SetActivePaletteAndApply(palette);

        public static bool SetActiveRoleLibraryAndApply(DeucarianColorRoleLibrary roleLibrary)
            => DeucarianThemeSelectionCommands.SetActiveRoleLibraryAndApply(roleLibrary);

        public static bool SetActiveStyleAndApply(DeucarianThemeStyle style)
            => DeucarianThemeSelectionCommands.SetActiveStyleAndApply(style);

        /// <summary>
        /// Opens a project save panel and creates a reusable custom style from the active composition.
        /// No asset is created until the user confirms the save location.
        /// </summary>
        public static DeucarianThemeStyle CreateStyleVariantFromActiveFromSavePanel()
            => DeucarianThemeAssetDialogs.CreateStyleVariantFromActiveFromSavePanel();

        /// <summary>Legacy API that creates and activates a source-controlled custom style.</summary>
        public static DeucarianThemeStyle CreateStyleVariant(
            DeucarianThemeStyle source,
            string assetPath)
            => DeucarianThemeStyleAssets.CreateStyleVariant(source, assetPath);

        /// <summary>
        /// Creates a complete project-authored custom style without assigning it to themes or providers.
        /// The Theme Manager keeps that assignment staged until its explicit Activate action.
        /// </summary>
        public static DeucarianThemeStyle CreateCustomStyle(
            DeucarianThemeStyle source,
            string assetPath,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size)
            => DeucarianThemeStyleAssets.CreateCustomStyle(source, assetPath, surface, corners, border, size);

        /// <summary>Creates a complete custom style with an optional TMP typography profile.</summary>
        public static DeucarianThemeStyle CreateCustomStyle(
            DeucarianThemeStyle source,
            string assetPath,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size,
            DeucarianThemeTypographyProfile typography)
            => DeucarianThemeStyleAssets.CreateCustomStyle(source, assetPath, surface, corners, border, size, typography);

        /// <summary>Legacy API that updates a project-authored custom style and refreshes active providers.</summary>
        public static bool UpdateStyleVariantComposition(
            DeucarianThemeStyle style,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile shape,
            DeucarianThemeStrokeProfile stroke,
            DeucarianThemeDensity density)
            => DeucarianThemeStyleAssets.UpdateStyleVariantComposition(style, surface, shape, stroke, density);

        public static bool AssignActiveStyleToActiveTheme()
            => DeucarianThemeSelectionCommands.AssignActiveStyleToActiveTheme();

        public static bool AssignActiveStyleToActiveThemeFamily()
            => DeucarianThemeSelectionCommands.AssignActiveStyleToActiveThemeFamily();

        public static void SelectAndPing(UnityEngine.Object asset)
            => DeucarianThemeAssetDialogs.SelectAndPing(asset);

        public static void OpenThemeAssetsFolder()
            => DeucarianThemeAssetDialogs.OpenThemeAssetsFolder();

        public static int ApplyActiveThemeToOpenScene(bool createProviderIfMissing = true, bool askBeforeCreate = true)
            => DeucarianThemeSceneCommands.ApplyActiveThemeToOpenScene(createProviderIfMissing, askBeforeCreate);

        public static int ApplyThemeFamilyToOpenScene(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            bool createProviderIfMissing = true,
            bool askBeforeCreate = true)
            => DeucarianThemeSceneCommands.ApplyThemeFamilyToOpenScene(family, mode, createProviderIfMissing, askBeforeCreate);

        public static int ApplyThemeToOpenScene(
            DeucarianTheme theme,
            bool createProviderIfMissing = true,
            bool askBeforeCreate = true)
            => DeucarianThemeSceneCommands.ApplyThemeToOpenScene(theme, createProviderIfMissing, askBeforeCreate);

        public static string EnsureAssetFolder(string folder)
            => DeucarianThemeAssetCatalog.EnsureAssetFolder(folder);

        public static int RefreshOpenSceneProvidersUsingAsset(UnityEngine.Object asset)
            => DeucarianThemeSceneCommands.RefreshOpenSceneProvidersUsingAsset(asset);

    }
}
