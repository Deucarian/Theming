using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{

    /// <summary>
    /// Creates built-in Deucarian theme preset assets.
    /// </summary>
    public static class DeucarianDefaultThemeAssetFactory
    {
        public const string DefaultRootFolder = "Assets/Deucarian/Theming/Defaults";
        public const string GameRootFolder = "Assets/Deucarian/Theming/Game";
        public const string BuiltinStylesFolderName = "Styles";
        public const string BuiltinStyleComponentsFolderName = "Components";
        public const string BuiltinSurfaceProfilesFolderName = "Surfaces";
        public const string BuiltinShapeProfilesFolderName = "Shapes";
        public const string BuiltinStrokeProfilesFolderName = "Strokes";
        public const string BuiltinTypographyProfilesFolderName = "Typography";
        public const string SystemDefaultTypographyFileName = "SystemDefaultTypography.asset";
        public const string MinimalPaletteRootFolder = "Assets/Deucarian/Theming";
        public const string MinimalPaletteFileName = "DeucarianMinimalPalette.asset";
        public const string MinimalThemeFileName = "DeucarianMinimalTheme.asset";
        public const string ThemeFamilyFileName = "DeucarianThemeFamily.asset";

        public static void CreateDefaultThemeAssetsFromMenu()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets(DefaultRootFolder);
            if (assets != null && assets.ThemeFamily != null)
            {
                DeucarianEditorSelection.SelectAndPing(assets.ThemeFamily);
            }
        }

        public static void CreateGameThemeAssetsFromMenu()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets(GameRootFolder);
            if (assets != null && assets.Theme != null)
            {
                DeucarianEditorSelection.SelectAndPing(assets.Theme);
            }
        }

        /// <summary>
        /// Creates minimal generic role, library, palette, and theme assets under the requested Assets folder.
        /// Existing assets are reused, with missing role references and palette entries filled in.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateDefaultThemeAssets(string rootFolder, bool overwriteExisting = false)
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemeFamilyAssetCreation.CreateThemeFamilyAssets(
                rootFolder,
                overwriteExisting,
                DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Light),
                DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Dark),
                new ThemeFamilyPresetDefinition(
                    "DefaultColorRoleLibrary.asset",
                    "DefaultLightColorPalette.asset",
                    "DefaultDarkColorPalette.asset",
                    "DefaultLightTheme.asset",
                    "DefaultTheme.asset",
                    "DefaultThemeFamily.asset",
                    "deucarian.palette.default.light",
                    "Deucarian Default Light",
                    "deucarian.palette.default",
                    "Deucarian Default",
                    "deucarian.theme.default.light",
                    "Deucarian Default Light",
                    "deucarian.theme.default",
                    "Default",
                    "deucarian.theme-family.default",
                    "Deucarian Default"));

            IReadOnlyList<DeucarianThemeStyle> styles = DeucarianBuiltinThemeStyleAssets.CreateBuiltinThemeStyleAssets(
                DeucarianThemeAssetNaming.CombineAssetPath(DeucarianThemeAssetNaming.NormalizeAssetPath(rootFolder), BuiltinStylesFolderName),
                overwriteExisting);
            for (int i = 0; i < styles.Count; i++)
            {
                assets.AddStyle(styles[i]);
            }

            assets.DefaultStyle = DeucarianBuiltinThemeStyleAssets.FindStyleById(styles, DeucarianThemeStyleIds.FrostedGlass);
            if (assets.DefaultStyle != null)
            {
                DeucarianBuiltinThemeStyleAssets.AssignStyleIfMissing(assets.LightTheme, assets.DefaultStyle, overwriteExisting);
                DeucarianBuiltinThemeStyleAssets.AssignStyleIfMissing(assets.DarkTheme, assets.DefaultStyle, overwriteExisting);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return assets;
        }

        /// <summary>
        /// Creates optional gameplay, faction, and item rarity role assets under the requested Assets folder.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateGameThemeAssets(string rootFolder, bool overwriteExisting = false)
        {
            return DeucarianThemePaletteAssetCreation.CreateThemeAssets(
                rootFolder,
                overwriteExisting,
                DeucarianBuiltinThemeRolePresets.CreateGameRoleDefinitions(),
                new ThemePresetDefinition(
                    "GameColorRoleLibrary.asset",
                    "GameColorPalette.asset",
                    "GameTheme.asset",
                    "deucarian.palette.game",
                    "Game Default",
                    "deucarian.theme.game",
                    "Game Theme"));
        }

        /// <summary>
        /// Creates or repairs the built-in Deucarian visual style assets under the requested Assets folder.
        /// </summary>
        public static IReadOnlyList<DeucarianThemeStyle> CreateBuiltinThemeStyleAssets(
            string rootFolder,
            bool overwriteExisting = false)
        {
            return DeucarianBuiltinThemeStyleAssets.CreateBuiltinThemeStyleAssets(rootFolder, overwriteExisting);
        }

        /// <summary>
        /// Creates a complete light/dark theme family at the requested family asset path.
        /// The two palettes are independently editable while roles, the role library, and the initial visual style are shared.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateThemeFamily(
            string familyPath,
            bool overwriteExisting = false)
        {
            return DeucarianThemeFamilyWrapping.CreateThemeFamily(familyPath, overwriteExisting);
        }

        /// <summary>
        /// Repairs a saved theme family using the paired asset conventions without replacing user-authored colors or styles.
        /// </summary>
        public static DeucarianDefaultThemeAssets RepairThemeFamilySetup(
            DeucarianThemeFamily family,
            bool overwriteExisting = false)
        {
            return DeucarianThemeFamilyWrapping.RepairThemeFamilySetup(family, overwriteExisting);
        }

        /// <summary>
        /// Wraps an existing standalone theme in an explicitly selected family slot. The other slot is preserved or left empty.
        /// </summary>
        public static DeucarianDefaultThemeAssets WrapExistingThemeInFamily(
            DeucarianTheme theme,
            DeucarianThemeMode existingThemeMode,
            string familyPath,
            bool overwriteExisting = false)
        {
            return DeucarianThemeFamilyWrapping.WrapExistingThemeInFamily(theme, existingThemeMode, familyPath, overwriteExisting);
        }

        /// <summary>
        /// Creates a palette-first minimal setup. The palette is the main editable asset; support roles and library
        /// are generated next to it, and a theme is linked to the palette.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateMinimalPalette(string palettePath, bool overwriteExisting = false)
        {
            return DeucarianThemePaletteAssetCreation.CreateMinimalPalette(palettePath, overwriteExisting);
        }

        /// <summary>
        /// Repairs the support assets needed for a palette-first workflow without overwriting user colors.
        /// </summary>
        public static DeucarianDefaultThemeAssets RepairPaletteSetup(
            DeucarianColorPalette palette,
            string themePath = null,
            bool overwriteExisting = false)
        {
            return DeucarianThemePaletteAssetCreation.RepairPaletteSetup(palette, themePath, overwriteExisting);
        }

        /// <summary>
        /// Creates or updates a palette asset using the entries and role library from an existing theme.
        /// </summary>
        public static DeucarianColorPalette CreatePaletteFromTheme(
            DeucarianTheme theme,
            string palettePath,
            bool overwriteExisting = false)
        {
            return DeucarianThemePaletteAssetCreation.CreatePaletteFromTheme(theme, palettePath, overwriteExisting);
        }

        internal static void EnsureAssetObjectNameMatchesPath(UnityEngine.Object asset, string assetPath)
        {
            DeucarianThemeAssetStore.EnsureAssetObjectNameMatchesPath(asset, assetPath);
        }

        internal readonly struct ThemeFamilyPresetDefinition
        {
            public ThemeFamilyPresetDefinition(
                string roleLibraryFileName,
                string lightPaletteFileName,
                string darkPaletteFileName,
                string lightThemeFileName,
                string darkThemeFileName,
                string familyFileName,
                string lightPaletteId,
                string lightPaletteDisplayName,
                string darkPaletteId,
                string darkPaletteDisplayName,
                string lightThemeId,
                string lightThemeDisplayName,
                string darkThemeId,
                string darkThemeDisplayName,
                string familyId,
                string familyDisplayName,
                string rolesFolderName = "Roles")
            {
                RoleLibraryFileName = roleLibraryFileName;
                LightPaletteFileName = lightPaletteFileName;
                DarkPaletteFileName = darkPaletteFileName;
                LightThemeFileName = lightThemeFileName;
                DarkThemeFileName = darkThemeFileName;
                FamilyFileName = familyFileName;
                LightPaletteId = lightPaletteId;
                LightPaletteDisplayName = lightPaletteDisplayName;
                DarkPaletteId = darkPaletteId;
                DarkPaletteDisplayName = darkPaletteDisplayName;
                LightThemeId = lightThemeId;
                LightThemeDisplayName = lightThemeDisplayName;
                DarkThemeId = darkThemeId;
                DarkThemeDisplayName = darkThemeDisplayName;
                FamilyId = familyId;
                FamilyDisplayName = familyDisplayName;
                RolesFolderName = string.IsNullOrWhiteSpace(rolesFolderName) ? "Roles" : rolesFolderName;
            }

            public string RoleLibraryFileName { get; }
            public string LightPaletteFileName { get; }
            public string DarkPaletteFileName { get; }
            public string LightThemeFileName { get; }
            public string DarkThemeFileName { get; }
            public string FamilyFileName { get; }
            public string LightPaletteId { get; }
            public string LightPaletteDisplayName { get; }
            public string DarkPaletteId { get; }
            public string DarkPaletteDisplayName { get; }
            public string LightThemeId { get; }
            public string LightThemeDisplayName { get; }
            public string DarkThemeId { get; }
            public string DarkThemeDisplayName { get; }
            public string FamilyId { get; }
            public string FamilyDisplayName { get; }
            public string RolesFolderName { get; }
        }

        internal readonly struct ThemePresetDefinition
        {
            public ThemePresetDefinition(
                string roleLibraryFileName,
                string paletteFileName,
                string themeFileName,
                string paletteId,
                string paletteDisplayName,
                string themeId,
                string themeDisplayName)
            {
                RoleLibraryFileName = roleLibraryFileName;
                PaletteFileName = paletteFileName;
                ThemeFileName = themeFileName;
                PaletteId = paletteId;
                PaletteDisplayName = paletteDisplayName;
                ThemeId = themeId;
                ThemeDisplayName = themeDisplayName;
            }

            public string RoleLibraryFileName { get; }
            public string PaletteFileName { get; }
            public string ThemeFileName { get; }
            public string PaletteId { get; }
            public string PaletteDisplayName { get; }
            public string ThemeId { get; }
            public string ThemeDisplayName { get; }
        }

        internal readonly struct BuiltinRoleDefinition
        {
            public BuiltinRoleDefinition(string id, string displayName, string category, string description, Color defaultColor)
            {
                Id = id;
                DisplayName = displayName;
                Category = category;
                Description = description;
                DefaultColor = defaultColor;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Category { get; }
            public string Description { get; }
            public Color DefaultColor { get; }
        }

    }
}
