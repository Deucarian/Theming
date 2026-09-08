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
    internal static class DeucarianThemeAssetSetup
    {
        public static DeucarianDefaultThemeAssets CreateMissingDefaultThemeAssets()
        {
            return CreateMissingDefaultThemeAssets(DeucarianThemingEditorSettings.DefaultAssetFolder);
        }

        public static DeucarianDefaultThemeAssets CreateMissingDefaultThemeAssets(string folder)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating default theme assets"))
            {
                return null;
            }

            string assetFolder = string.IsNullOrWhiteSpace(folder)
                ? DeucarianThemingEditorSettings.DefaultThemeAssetFolder
                : DeucarianThemingEditorSettings.NormalizeAssetPath(folder);

            DeucarianThemingEditorSettings.DefaultAssetFolder = assetFolder;
            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(assetFolder);
            DeucarianThemeSelectionCommands.StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info($"Deucarian default theme assets are ready in {assetFolder}.", assets.Theme);
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateGameThemeAssets()
        {
            return CreateGameThemeAssets(DeucarianDefaultThemeAssetFactory.GameRootFolder);
        }

        public static DeucarianDefaultThemeAssets CreateGameThemeAssets(string folder)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating game theme assets"))
            {
                return null;
            }

            string assetFolder = string.IsNullOrWhiteSpace(folder)
                ? DeucarianDefaultThemeAssetFactory.GameRootFolder
                : DeucarianThemingEditorSettings.NormalizeAssetPath(folder);

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateGameThemeAssets(assetFolder);
            DeucarianThemeSelectionCommands.StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info($"Deucarian game theme assets are ready in {assetFolder}.", assets.Theme);
            return assets;
        }

        public static IReadOnlyList<DeucarianThemeStyle> CreateBuiltinThemeStyleAssets()
        {
            return CreateBuiltinThemeStyleAssets(
                DeucarianThemeAssetCatalog.CombineAssetPath(
                    DeucarianThemingEditorSettings.DefaultAssetFolder,
                    DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName));
        }

        public static IReadOnlyList<DeucarianThemeStyle> CreateBuiltinThemeStyleAssets(string folder)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating built-in theme styles"))
            {
                return Array.Empty<DeucarianThemeStyle>();
            }

            string assetFolder = string.IsNullOrWhiteSpace(folder)
                ? DeucarianThemeAssetCatalog.CombineAssetPath(
                    DeucarianThemingEditorSettings.DefaultAssetFolder,
                    DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName)
                : DeucarianThemingEditorSettings.NormalizeAssetPath(folder);

            IReadOnlyList<DeucarianThemeStyle> styles =
                DeucarianDefaultThemeAssetFactory.CreateBuiltinThemeStyleAssets(assetFolder);
            DeucarianThemeStyle defaultStyle = DeucarianThemeAssetCatalog.FindStyleById(styles, DeucarianThemeStyleIds.FrostedGlass)
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
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a theme family"))
            {
                return null;
            }

            string folder = DeucarianThemeAssetCatalog.EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string familyPath = DeucarianThemeAssetCatalog.CombineAssetPath(folder, DeucarianDefaultThemeAssetFactory.ThemeFamilyFileName);
            return CreateThemeFamily(familyPath);
        }

        public static DeucarianDefaultThemeAssets CreateThemeFamily(string familyPath)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a theme family"))
            {
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateThemeFamily(familyPath);
            DeucarianThemeSelectionCommands.StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info(
                $"Deucarian theme family is ready at {AssetDatabase.GetAssetPath(assets.ThemeFamily)}.",
                assets.ThemeFamily);
            return assets;
        }

        public static DeucarianDefaultThemeAssets RepairActiveThemeFamilySetup()
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("repairing a theme family"))
            {
                return null;
            }

            DeucarianThemeFamily family = DeucarianThemeSelectionActions.ResolveOrCreateActiveThemeFamily();
            if (family == null)
            {
                ThemingLog.Editor.Warning("No active Deucarian theme family is selected.");
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(family);
            DeucarianThemeSelectionCommands.StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info($"Repaired Deucarian theme family '{family.name}'.", family);
            return assets;
        }

        public static DeucarianDefaultThemeAssets WrapActiveThemeInFamily(
            DeucarianThemeMode existingThemeMode,
            string familyPath)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("wrapping a theme in a family"))
            {
                return null;
            }

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
            DeucarianThemeSelectionCommands.StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info(
                $"Wrapped theme '{theme.name}' as the {existingThemeMode} variant of '{assets.ThemeFamily.name}'.",
                assets.ThemeFamily);
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateMinimalPalette()
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a minimal palette"))
            {
                return null;
            }

            string folder = DeucarianThemeAssetCatalog.EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string palettePath = DeucarianThemeAssetCatalog.CombineAssetPath(folder, DeucarianDefaultThemeAssetFactory.MinimalPaletteFileName);
            return CreateMinimalPalette(palettePath);
        }

        public static DeucarianDefaultThemeAssets CreateMinimalPalette(string palettePath)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a minimal palette"))
            {
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(palettePath);
            DeucarianThemeSelectionCommands.StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info($"Deucarian minimal palette is ready at {AssetDatabase.GetAssetPath(assets.Palette)}.", assets.Palette);
            return assets;
        }

        public static DeucarianDefaultThemeAssets CreateThemeFromActivePalette()
        {
            return RepairActivePaletteSetup();
        }

        public static DeucarianDefaultThemeAssets RepairActivePaletteSetup()
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("repairing a palette"))
            {
                return null;
            }

            DeucarianColorPalette palette = DeucarianThemeSelectionActions.ResolveOrCreateActivePaletteFirst();
            if (palette == null)
            {
                ThemingLog.Editor.Warning("No active Deucarian palette is selected. Choose one palette or create a minimal palette first.");
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.RepairPaletteSetup(palette);
            DeucarianThemeSelectionCommands.StoreDefaultAssetSelections(assets);
            ThemingLog.Editor.Info($"Repaired Deucarian palette setup for '{palette.name}'.", palette);
            return assets;
        }

        public static DeucarianColorPalette CreatePaletteFromTheme(DeucarianTheme theme, string palettePath)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a palette from a theme"))
            {
                return null;
            }

            DeucarianColorPalette palette = DeucarianDefaultThemeAssetFactory.CreatePaletteFromTheme(theme, palettePath);
            DeucarianThemingEditorSettings.ActivePalette = palette;
            if (palette != null && palette.RoleLibrary != null)
            {
                DeucarianThemingEditorSettings.ActiveRoleLibrary = palette.RoleLibrary;
            }

            ThemingLog.Editor.Info($"Created Deucarian palette '{palette.name}' from theme '{theme.name}'.", palette);
            return palette;
        }
    }
}
