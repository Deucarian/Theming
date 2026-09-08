using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using AssetSearchResult = Deucarian.Theming.Editor.DeucarianThemingMenuActions.AssetSearchResult;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeSelectionActions
    {
        public static AssetSearchResult FindExistingAssets(string[] searchFolders = null, bool autoSelectSingleAssets = false)
        {
            AssetSearchResult result = new AssetSearchResult(
                DeucarianThemeAssetCatalog.FindAssets<DeucarianThemeFamily>(searchFolders),
                DeucarianThemeAssetCatalog.FindAssets<DeucarianTheme>(searchFolders),
                DeucarianThemeAssetCatalog.FindAssets<DeucarianColorPalette>(searchFolders),
                DeucarianThemeAssetCatalog.FindAssets<DeucarianColorRoleLibrary>(searchFolders),
                DeucarianThemeAssetCatalog.FindAssets<DeucarianThemeStyle>(searchFolders));

            if (autoSelectSingleAssets)
            {
                AutoSelectSingleAsset(
                    result.ThemeFamilies,
                    DeucarianThemingEditorSettings.ActiveThemeFamily,
                    family => DeucarianThemeSelectionCommands.SetActiveThemeFamilySelection(family, DeucarianThemingEditorSettings.ActiveThemeMode));
                AutoSelectSingleAsset(result.Themes, DeucarianThemingEditorSettings.ActiveTheme, theme => DeucarianThemingEditorSettings.ActiveTheme = theme);
                AutoSelectSingleAsset(result.Palettes, DeucarianThemingEditorSettings.ActivePalette, palette => DeucarianThemingEditorSettings.ActivePalette = palette);
                AutoSelectSingleAsset(result.RoleLibraries, DeucarianThemingEditorSettings.ActiveRoleLibrary, library => DeucarianThemingEditorSettings.ActiveRoleLibrary = library);
                AutoSelectSingleAsset(result.Styles, DeucarianThemingEditorSettings.ActiveStyle, style => DeucarianThemingEditorSettings.ActiveStyle = style);
            }

            return result;
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

            IReadOnlyList<DeucarianThemeFamily> families = DeucarianThemeAssetCatalog.FindAssets<DeucarianThemeFamily>(searchFolders);
            if (families.Count == 1)
            {
                DeucarianThemeSelectionCommands.SetActiveThemeFamilySelection(families[0], DeucarianThemingEditorSettings.ActiveThemeMode);
                return families[0];
            }

            if (families.Count == 0)
            {
                if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a theme family"))
                {
                    return null;
                }

                string folder = string.IsNullOrWhiteSpace(createFolder)
                    ? DeucarianThemingEditorSettings.DefaultAssetFolder
                    : DeucarianThemeAssetCatalog.EnsureAssetFolder(createFolder);
                string familyPath = DeucarianThemeAssetCatalog.CombineAssetPath(folder, "DefaultThemeFamily.asset");
                DeucarianDefaultThemeAssets assets = DeucarianThemeAssetSetup.CreateThemeFamily(familyPath);
                return assets != null ? assets.ThemeFamily : null;
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
                DeucarianThemeAssetCatalog.FindAssets<DeucarianTheme>(searchFolders),
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

            IReadOnlyList<DeucarianColorPalette> palettes = DeucarianThemeAssetCatalog.FindAssets<DeucarianColorPalette>(searchFolders);
            if (palettes.Count == 1)
            {
                DeucarianThemingEditorSettings.ActivePalette = palettes[0];
                return palettes[0];
            }

            if (palettes.Count == 0)
            {
                DeucarianDefaultThemeAssets assets = DeucarianThemeAssetSetup.CreateMinimalPalette();
                return assets != null ? assets.Palette : null;
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
                DeucarianThemeAssetCatalog.FindAssets<DeucarianColorPalette>(searchFolders),
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
                DeucarianThemeAssetCatalog.FindAssets<DeucarianColorRoleLibrary>(searchFolders),
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

            IReadOnlyList<DeucarianThemeStyle> foundStyles = DeucarianThemeAssetCatalog.FindAssets<DeucarianThemeStyle>(searchFolders);
            if (foundStyles.Count == 1)
            {
                DeucarianThemingEditorSettings.ActiveStyle = foundStyles[0];
                return foundStyles[0];
            }

            if (foundStyles.Count == 0)
            {
                string folder = string.IsNullOrWhiteSpace(createFolder)
                    ? DeucarianThemeAssetCatalog.CombineAssetPath(
                        DeucarianThemingEditorSettings.DefaultAssetFolder,
                        DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName)
                    : createFolder;
                IReadOnlyList<DeucarianThemeStyle> createdStyles = DeucarianThemeAssetSetup.CreateBuiltinThemeStyleAssets(folder);
                return DeucarianThemeAssetCatalog.FindStyleById(createdStyles, DeucarianThemeStyleIds.FrostedGlass)
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
            DeucarianEditorSelection.SelectAndPing(theme);
            return theme;
        }

        public static DeucarianThemeFamily SelectActiveThemeFamily()
        {
            DeucarianThemeFamily family = ResolveOrCreateActiveThemeFamily();
            DeucarianEditorSelection.SelectAndPing(family);
            return family;
        }

        public static DeucarianColorPalette SelectActivePalette()
        {
            DeucarianColorPalette palette = ResolveOrCreateActivePalette();
            DeucarianEditorSelection.SelectAndPing(palette);
            return palette;
        }

        public static DeucarianColorRoleLibrary SelectActiveRoleLibrary()
        {
            DeucarianColorRoleLibrary library = ResolveOrCreateActiveRoleLibrary();
            DeucarianEditorSelection.SelectAndPing(library);
            return library;
        }

        public static DeucarianThemeStyle SelectActiveStyle()
        {
            DeucarianThemeStyle style = ResolveOrCreateActiveStyle();
            DeucarianEditorSelection.SelectAndPing(style);
            return style;
        }

        internal static T ResolveOrCreateActiveAsset<T>(
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
                DeucarianDefaultThemeAssets assets = DeucarianThemeAssetSetup.CreateMissingDefaultThemeAssets(
                    string.IsNullOrWhiteSpace(createFolder) ? DeucarianThemingEditorSettings.DefaultAssetFolder : createFolder);
                if (assets == null)
                {
                    return null;
                }

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

        internal static void AutoSelectSingleAsset<T>(IReadOnlyList<T> assets, T activeAsset, Action<T> setActiveAsset)
            where T : UnityEngine.Object
        {
            if (activeAsset == null && assets.Count == 1)
            {
                setActiveAsset(assets[0]);
            }
        }
    }
}
