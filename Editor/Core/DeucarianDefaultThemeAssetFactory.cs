using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Result object returned by theme preset asset creation.
    /// </summary>
    public sealed class DeucarianDefaultThemeAssets
    {
        private readonly List<DeucarianColorRole> roles = new List<DeucarianColorRole>();
        private readonly List<DeucarianThemeStyle> styles = new List<DeucarianThemeStyle>();

        public DeucarianColorRoleLibrary RoleLibrary { get; internal set; }
        public DeucarianThemeFamily ThemeFamily { get; internal set; }
        public DeucarianColorPalette LightPalette { get; internal set; }
        public DeucarianColorPalette DarkPalette { get; internal set; }
        public DeucarianTheme LightTheme { get; internal set; }
        public DeucarianTheme DarkTheme { get; internal set; }

        /// <summary>
        /// Backward-compatible primary palette. Paired workflows expose the dark palette here.
        /// </summary>
        public DeucarianColorPalette Palette { get; internal set; }

        /// <summary>
        /// Backward-compatible primary theme. Paired workflows expose the dark theme here.
        /// </summary>
        public DeucarianTheme Theme { get; internal set; }
        public DeucarianThemeStyle DefaultStyle { get; internal set; }
        public IReadOnlyList<DeucarianColorRole> Roles => roles;
        public IReadOnlyList<DeucarianThemeStyle> Styles => styles;

        internal void AddRole(DeucarianColorRole role)
        {
            if (role != null)
            {
                roles.Add(role);
            }
        }

        internal void AddStyle(DeucarianThemeStyle style)
        {
            if (style != null)
            {
                styles.Add(style);
            }
        }
    }

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
        public const string MinimalPaletteRootFolder = "Assets/Deucarian/Theming";
        public const string MinimalPaletteFileName = "DeucarianMinimalPalette.asset";
        public const string MinimalThemeFileName = "DeucarianMinimalTheme.asset";
        public const string ThemeFamilyFileName = "DeucarianThemeFamily.asset";

        public static void CreateDefaultThemeAssetsFromMenu()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets(DefaultRootFolder);
            if (assets.ThemeFamily != null)
            {
                DeucarianThemingMenuActions.SelectAndPing(assets.ThemeFamily);
            }
        }

        public static void CreateGameThemeAssetsFromMenu()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets(GameRootFolder);
            if (assets.Theme != null)
            {
                DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
            }
        }

        /// <summary>
        /// Creates minimal generic role, library, palette, and theme assets under the requested Assets folder.
        /// Existing assets are reused, with missing role references and palette entries filled in.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateDefaultThemeAssets(string rootFolder, bool overwriteExisting = false)
        {
            DeucarianDefaultThemeAssets assets = CreateThemeFamilyAssets(
                rootFolder,
                overwriteExisting,
                CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Light),
                CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Dark),
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

            IReadOnlyList<DeucarianThemeStyle> styles = CreateBuiltinThemeStyleAssets(
                CombineAssetPath(NormalizeAssetPath(rootFolder), BuiltinStylesFolderName),
                overwriteExisting);
            for (int i = 0; i < styles.Count; i++)
            {
                assets.AddStyle(styles[i]);
            }

            assets.DefaultStyle = FindStyleById(styles, DeucarianThemeStyleIds.FrostedGlass);
            if (assets.DefaultStyle != null)
            {
                AssignStyleIfMissing(assets.LightTheme, assets.DefaultStyle, overwriteExisting);
                AssignStyleIfMissing(assets.DarkTheme, assets.DefaultStyle, overwriteExisting);
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
            return CreateThemeAssets(
                rootFolder,
                overwriteExisting,
                CreateGameRoleDefinitions(),
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
            string normalizedRoot = NormalizeAssetPath(rootFolder);
            if (string.IsNullOrEmpty(normalizedRoot)
                || (normalizedRoot != "Assets" && !normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme style assets must be created under the Assets folder.", nameof(rootFolder));
            }

            EnsureFolder(normalizedRoot);
            IReadOnlyList<DeucarianThemeSurfaceProfile> surfaces = CreateBuiltinSurfaceProfiles(
                CombineAssetPath(
                    CombineAssetPath(normalizedRoot, BuiltinStyleComponentsFolderName),
                    BuiltinSurfaceProfilesFolderName),
                overwriteExisting);
            IReadOnlyList<DeucarianThemeShapeProfile> shapes = CreateBuiltinShapeProfiles(
                CombineAssetPath(
                    CombineAssetPath(normalizedRoot, BuiltinStyleComponentsFolderName),
                    BuiltinShapeProfilesFolderName),
                overwriteExisting);
            IReadOnlyList<DeucarianThemeStrokeProfile> strokes = CreateBuiltinStrokeProfiles(
                CombineAssetPath(
                    CombineAssetPath(normalizedRoot, BuiltinStyleComponentsFolderName),
                    BuiltinStrokeProfilesFolderName),
                overwriteExisting);
            IReadOnlyList<DeucarianThemeStylePreset> definitions = DeucarianThemeStylePresets.BuiltinStyles;
            List<DeucarianThemeStyle> styles = new List<DeucarianThemeStyle>();

            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemeStylePreset definition = definitions[i];
                string stylePath = CombineAssetPath(normalizedRoot, definition.FileName);
                DeucarianThemeStyle style = LoadOrCreateAsset(
                    stylePath,
                    () => ScriptableObject.CreateInstance<DeucarianThemeStyle>(),
                    overwriteExisting,
                    out bool styleCreated);

                DeucarianThemeSurfaceProfile surface = FindSurfaceProfile(surfaces, definition.SurfaceProfileId);
                DeucarianThemeShapeProfile shape = FindShapeProfile(shapes, definition.ShapeProfileId);
                DeucarianThemeStrokeProfile stroke = FindStrokeProfile(strokes, definition.StrokeProfileId);
                if (styleCreated || overwriteExisting || ShouldRepairGeneratedStyle(style, definition))
                {
                    definition.Configure(style);
                    style.SetComposition(surface, shape, stroke, definition.Density);
                    EditorUtility.SetDirty(style);
                }

                styles.Add(style);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return styles;
        }

        private static IReadOnlyList<DeucarianThemeSurfaceProfile> CreateBuiltinSurfaceProfiles(
            string folder,
            bool overwriteExisting)
        {
            EnsureFolder(folder);
            IReadOnlyList<DeucarianThemeSurfaceProfilePreset> definitions =
                DeucarianThemePresentationProfilePresets.BuiltinSurfaces;
            List<DeucarianThemeSurfaceProfile> profiles = new List<DeucarianThemeSurfaceProfile>();
            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemeSurfaceProfilePreset definition = definitions[i];
                DeucarianThemeSurfaceProfile profile = LoadOrCreateAsset(
                    CombineAssetPath(folder, definition.FileName),
                    () => ScriptableObject.CreateInstance<DeucarianThemeSurfaceProfile>(),
                    overwriteExisting,
                    out bool created);
                if (created || overwriteExisting || ShouldRepairSurfaceProfile(profile, definition))
                {
                    definition.Configure(profile);
                    EditorUtility.SetDirty(profile);
                }

                profiles.Add(profile);
            }

            return profiles;
        }

        private static IReadOnlyList<DeucarianThemeShapeProfile> CreateBuiltinShapeProfiles(
            string folder,
            bool overwriteExisting)
        {
            EnsureFolder(folder);
            IReadOnlyList<DeucarianThemeShapeProfilePreset> definitions =
                DeucarianThemePresentationProfilePresets.BuiltinShapes;
            List<DeucarianThemeShapeProfile> profiles = new List<DeucarianThemeShapeProfile>();
            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemeShapeProfilePreset definition = definitions[i];
                DeucarianThemeShapeProfile profile = LoadOrCreateAsset(
                    CombineAssetPath(folder, definition.FileName),
                    () => ScriptableObject.CreateInstance<DeucarianThemeShapeProfile>(),
                    overwriteExisting,
                    out bool created);
                if (created || overwriteExisting || ShouldRepairShapeProfile(profile, definition))
                {
                    definition.Configure(profile);
                    EditorUtility.SetDirty(profile);
                }

                profiles.Add(profile);
            }

            return profiles;
        }

        private static IReadOnlyList<DeucarianThemeStrokeProfile> CreateBuiltinStrokeProfiles(
            string folder,
            bool overwriteExisting)
        {
            EnsureFolder(folder);
            IReadOnlyList<DeucarianThemeStrokeProfilePreset> definitions =
                DeucarianThemePresentationProfilePresets.BuiltinStrokes;
            List<DeucarianThemeStrokeProfile> profiles = new List<DeucarianThemeStrokeProfile>();
            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemeStrokeProfilePreset definition = definitions[i];
                DeucarianThemeStrokeProfile profile = LoadOrCreateAsset(
                    CombineAssetPath(folder, definition.FileName),
                    () => ScriptableObject.CreateInstance<DeucarianThemeStrokeProfile>(),
                    overwriteExisting,
                    out bool created);
                if (created || overwriteExisting || ShouldRepairStrokeProfile(profile, definition))
                {
                    definition.Configure(profile);
                    EditorUtility.SetDirty(profile);
                }

                profiles.Add(profile);
            }

            return profiles;
        }

        /// <summary>
        /// Creates a complete light/dark theme family at the requested family asset path.
        /// The two palettes are independently editable while roles, the role library, and the initial visual style are shared.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateThemeFamily(
            string familyPath,
            bool overwriteExisting = false)
        {
            string normalizedFamilyPath = NormalizeAssetPath(familyPath);
            ValidateAssetPath(normalizedFamilyPath, nameof(familyPath));

            string rootFolder = GetAssetFolder(normalizedFamilyPath);
            string familyAssetName = Path.GetFileNameWithoutExtension(normalizedFamilyPath);
            string baseName = DeriveThemeFamilyBaseName(familyAssetName);
            string supportFolderName = baseName + " Theme Support";
            string displayName = HumanizeAssetName(baseName);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Deucarian";
            }
            string familyDisplayName = displayName.EndsWith(" Theme", StringComparison.OrdinalIgnoreCase)
                ? displayName
                : displayName + " Theme";

            ThemeFamilyPresetDefinition preset = new ThemeFamilyPresetDefinition(
                supportFolderName + "/" + baseName + "ColorRoleLibrary.asset",
                baseName + "LightColorPalette.asset",
                baseName + "DarkColorPalette.asset",
                baseName + "LightTheme.asset",
                baseName + "DarkTheme.asset",
                Path.GetFileName(normalizedFamilyPath),
                BuildStableId("deucarian.palette", baseName + " light"),
                displayName + " Light Palette",
                BuildStableId("deucarian.palette", baseName + " dark"),
                displayName + " Dark Palette",
                BuildStableId("deucarian.theme", baseName + " light"),
                displayName + " Light",
                BuildStableId("deucarian.theme", baseName + " dark"),
                displayName + " Dark",
                BuildStableId("deucarian.theme-family", baseName),
                familyDisplayName,
                supportFolderName + "/Roles");

            DeucarianDefaultThemeAssets result = CreateThemeFamilyAssets(
                rootFolder,
                overwriteExisting,
                CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Light),
                CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Dark),
                preset);

            IReadOnlyList<DeucarianThemeStyle> styles = CreateBuiltinThemeStyleAssets(
                CombineAssetPath(rootFolder, supportFolderName + "/Styles"),
                overwriteExisting);
            for (int i = 0; i < styles.Count; i++)
            {
                result.AddStyle(styles[i]);
            }

            result.DefaultStyle = FindStyleById(styles, DeucarianThemeStyleIds.FrostedGlass);
            AssignStyleIfMissing(result.LightTheme, result.DefaultStyle, overwriteExisting);
            AssignStyleIfMissing(result.DarkTheme, result.DefaultStyle, overwriteExisting);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        /// <summary>
        /// Repairs a saved theme family using the paired asset conventions without replacing user-authored colors or styles.
        /// </summary>
        public static DeucarianDefaultThemeAssets RepairThemeFamilySetup(
            DeucarianThemeFamily family,
            bool overwriteExisting = false)
        {
            if (family == null)
            {
                throw new ArgumentNullException(nameof(family));
            }

            string familyPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(family));
            if (string.IsNullOrEmpty(familyPath))
            {
                throw new ArgumentException("Theme family repair requires a family asset saved in the project.", nameof(family));
            }

            return CreateThemeFamily(familyPath, overwriteExisting);
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
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            string normalizedFamilyPath = NormalizeAssetPath(familyPath);
            ValidateAssetPath(normalizedFamilyPath, nameof(familyPath));
            EnsureFolder(GetAssetFolder(normalizedFamilyPath));

            DeucarianThemeFamily family = LoadOrCreateAsset(
                normalizedFamilyPath,
                () => ScriptableObject.CreateInstance<DeucarianThemeFamily>(),
                overwriteExisting,
                out bool familyCreated);

            string familyAssetName = Path.GetFileNameWithoutExtension(normalizedFamilyPath);
            string baseName = DeriveThemeFamilyBaseName(familyAssetName);
            string familyId = familyCreated || overwriteExisting || string.IsNullOrWhiteSpace(family.FamilyId)
                ? BuildStableId("deucarian.theme-family", baseName)
                : family.FamilyId;
            string familyDisplayName = familyCreated || overwriteExisting || string.IsNullOrWhiteSpace(family.DisplayName)
                ? BuildThemeFamilyDisplayName(baseName)
                : family.DisplayName;

            DeucarianTheme lightTheme = overwriteExisting ? null : family.LightTheme;
            DeucarianTheme darkTheme = overwriteExisting ? null : family.DarkTheme;
            if (existingThemeMode == DeucarianThemeMode.Light)
            {
                lightTheme = theme;
            }
            else
            {
                darkTheme = theme;
            }

            family.Configure(familyId, familyDisplayName, lightTheme, darkTheme);
            EditorUtility.SetDirty(family);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new DeucarianDefaultThemeAssets
            {
                ThemeFamily = family,
                LightTheme = lightTheme,
                DarkTheme = darkTheme,
                LightPalette = lightTheme != null ? lightTheme.ColorPalette : null,
                DarkPalette = darkTheme != null ? darkTheme.ColorPalette : null,
                Theme = theme,
                Palette = theme.ColorPalette,
                RoleLibrary = theme.ColorPalette != null ? theme.ColorPalette.RoleLibrary : null,
                DefaultStyle = theme.VisualStyle
            };
        }

        /// <summary>
        /// Creates a palette-first minimal setup. The palette is the main editable asset; support roles and library
        /// are generated next to it, and a theme is linked to the palette.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateMinimalPalette(string palettePath, bool overwriteExisting = false)
        {
            string normalizedPalettePath = NormalizeAssetPath(palettePath);
            ValidateAssetPath(normalizedPalettePath, nameof(palettePath));

            string paletteFolder = GetAssetFolder(normalizedPalettePath);
            string paletteAssetName = Path.GetFileNameWithoutExtension(normalizedPalettePath);
            string themeAssetName = DeriveThemeAssetName(paletteAssetName);
            string themePath = CombineAssetPath(paletteFolder, themeAssetName + ".asset");
            EnsureFolder(paletteFolder);

            DeucarianColorPalette palette = LoadOrCreateAsset(
                normalizedPalettePath,
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            if (paletteCreated || overwriteExisting || string.IsNullOrWhiteSpace(palette.PaletteId))
            {
                palette.Configure(
                    BuildStableId("deucarian.palette", paletteAssetName),
                    HumanizeAssetName(paletteAssetName),
                    palette.RoleLibrary);
            }

            DeucarianDefaultThemeAssets result = RepairPaletteSetup(palette, themePath, overwriteExisting);
            if (result.Palette == null)
            {
                result.Palette = palette;
            }

            return result;
        }

        /// <summary>
        /// Repairs the support assets needed for a palette-first workflow without overwriting user colors.
        /// </summary>
        public static DeucarianDefaultThemeAssets RepairPaletteSetup(
            DeucarianColorPalette palette,
            string themePath = null,
            bool overwriteExisting = false)
        {
            if (palette == null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            string palettePath = NormalizeAssetPath(AssetDatabase.GetAssetPath(palette));
            if (string.IsNullOrEmpty(palettePath))
            {
                throw new ArgumentException("Palette setup repair requires a palette asset saved in the project.", nameof(palette));
            }

            ValidateAssetPath(palettePath, nameof(palette));

            string paletteFolder = GetAssetFolder(palettePath);
            string paletteAssetName = Path.GetFileNameWithoutExtension(palettePath);
            string supportFolder = CombineAssetPath(paletteFolder, paletteAssetName + " Support");
            string rolesFolder = CombineAssetPath(supportFolder, "Roles");
            string libraryPath = CombineAssetPath(supportFolder, paletteAssetName + "RoleLibrary.asset");
            EnsureFolder(rolesFolder);

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets
            {
                Palette = palette
            };

            DeucarianColorRoleLibrary library = palette.RoleLibrary != null
                ? palette.RoleLibrary
                : LoadOrCreateAsset(
                    libraryPath,
                    () => ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>(),
                    overwriteExisting,
                    out _);

            IReadOnlyList<BuiltinRoleDefinition> definitions = CreateMinimalDefaultRoleDefinitions();
            bool libraryChanged = library.RemoveNullRoles() > 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                BuiltinRoleDefinition definition = definitions[i];
                DeucarianColorRole role = FindRoleInLibrary(library, definition.Id)
                    ?? FindRoleAssetById(definition.Id)
                    ?? CreateRoleAsset(rolesFolder, definition, overwriteExisting);

                if (ShouldRepairGeneratedRole(role, definition))
                {
                    role.Configure(
                        definition.Id,
                        definition.DisplayName,
                        definition.Category,
                        definition.Description,
                        definition.DefaultColor,
                        true);
                    EditorUtility.SetDirty(role);
                }

                libraryChanged |= library.AddRole(role);
                result.AddRole(role);
            }

            if (libraryChanged || overwriteExisting)
            {
                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }

            if (palette.RoleLibrary != library)
            {
                palette.SetRoleLibrary(library);
                EditorUtility.SetDirty(palette);
            }

            bool paletteChanged = palette.RemoveNullEntries() > 0;
            for (int i = 0; i < result.Roles.Count; i++)
            {
                DeucarianColorRole role = result.Roles[i];
                if (role == null)
                {
                    continue;
                }

                if (!TryGetPaletteEntry(palette, role, out DeucarianColorEntry entry)
                    || entry == null
                    || entry.Role == null
                    || IsPackageMissingColor(entry.Color))
                {
                    palette.SetColor(role, role.DefaultColor, role.Description);
                    paletteChanged = true;
                }
            }

            if (paletteChanged || overwriteExisting)
            {
                EditorUtility.SetDirty(palette);
            }

            string resolvedThemePath = NormalizeAssetPath(themePath);
            if (string.IsNullOrEmpty(resolvedThemePath))
            {
                resolvedThemePath = CombineAssetPath(paletteFolder, DeriveThemeAssetName(paletteAssetName) + ".asset");
            }

            ValidateAssetPath(resolvedThemePath, nameof(themePath));
            DeucarianTheme theme = LoadOrCreateAsset(
                resolvedThemePath,
                () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                overwriteExisting,
                out bool themeCreated);

            if (themeCreated || overwriteExisting || theme.ColorPalette != palette || string.IsNullOrWhiteSpace(theme.ThemeId))
            {
                string themeId = themeCreated || overwriteExisting || string.IsNullOrWhiteSpace(theme.ThemeId)
                    ? BuildStableId("deucarian.theme", Path.GetFileNameWithoutExtension(resolvedThemePath))
                    : theme.ThemeId;
                string themeDisplayName = themeCreated || overwriteExisting || string.IsNullOrWhiteSpace(theme.DisplayName)
                    ? HumanizeAssetName(Path.GetFileNameWithoutExtension(resolvedThemePath))
                    : theme.DisplayName;
                theme.Configure(themeId, themeDisplayName, palette);
                EditorUtility.SetDirty(theme);
            }

            result.RoleLibrary = library;
            result.Theme = theme;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        /// <summary>
        /// Creates or updates a palette asset using the entries and role library from an existing theme.
        /// </summary>
        public static DeucarianColorPalette CreatePaletteFromTheme(
            DeucarianTheme theme,
            string palettePath,
            bool overwriteExisting = false)
        {
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            if (theme.ColorPalette == null)
            {
                throw new ArgumentException("Theme has no color palette to copy.", nameof(theme));
            }

            string normalizedPalettePath = NormalizeAssetPath(palettePath);
            ValidateAssetPath(normalizedPalettePath, nameof(palettePath));
            EnsureFolder(GetAssetFolder(normalizedPalettePath));

            DeucarianColorPalette palette = LoadOrCreateAsset(
                normalizedPalettePath,
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            string paletteAssetName = Path.GetFileNameWithoutExtension(normalizedPalettePath);
            if (paletteCreated || overwriteExisting || string.IsNullOrWhiteSpace(palette.PaletteId))
            {
                palette.Configure(
                    BuildStableId("deucarian.palette", paletteAssetName),
                    HumanizeAssetName(paletteAssetName),
                    theme.ColorPalette.RoleLibrary);
            }
            else if (palette.RoleLibrary != theme.ColorPalette.RoleLibrary)
            {
                palette.SetRoleLibrary(theme.ColorPalette.RoleLibrary);
            }

            if (paletteCreated || overwriteExisting)
            {
                palette.ClearEntries();
            }

            IReadOnlyList<DeucarianColorEntry> entries = theme.ColorPalette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                palette.SetColor(entry.Role, entry.Color, entry.Note);
            }

            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return palette;
        }

        private static DeucarianDefaultThemeAssets CreateThemeFamilyAssets(
            string rootFolder,
            bool overwriteExisting,
            IReadOnlyList<BuiltinRoleDefinition> lightDefinitions,
            IReadOnlyList<BuiltinRoleDefinition> darkDefinitions,
            ThemeFamilyPresetDefinition preset)
        {
            string normalizedRoot = NormalizeAssetPath(rootFolder);
            if (string.IsNullOrEmpty(normalizedRoot)
                || (normalizedRoot != "Assets" && !normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme assets must be created under the Assets folder.", nameof(rootFolder));
            }

            ValidatePairedDefinitions(lightDefinitions, darkDefinitions);
            EnsureFolder(normalizedRoot);

            string rolesFolder = CombineAssetPath(normalizedRoot, preset.RolesFolderName);
            string libraryPath = CombineAssetPath(normalizedRoot, preset.RoleLibraryFileName);
            string lightPalettePath = CombineAssetPath(normalizedRoot, preset.LightPaletteFileName);
            string darkPalettePath = CombineAssetPath(normalizedRoot, preset.DarkPaletteFileName);
            string lightThemePath = CombineAssetPath(normalizedRoot, preset.LightThemeFileName);
            string darkThemePath = CombineAssetPath(normalizedRoot, preset.DarkThemeFileName);
            string familyPath = CombineAssetPath(normalizedRoot, preset.FamilyFileName);
            EnsureFolder(rolesFolder);
            EnsureFolder(GetAssetFolder(libraryPath));

            DeucarianThemeFamily family = LoadOrCreateAsset(
                familyPath,
                () => ScriptableObject.CreateInstance<DeucarianThemeFamily>(),
                overwriteExisting,
                out bool familyCreated);

            DeucarianTheme lightTheme;
            bool lightThemeCreated;
            if (!overwriteExisting && family.LightTheme != null)
            {
                lightTheme = family.LightTheme;
                lightThemeCreated = false;
            }
            else
            {
                lightTheme = LoadOrCreateAsset(
                    lightThemePath,
                    () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                    overwriteExisting,
                    out lightThemeCreated);
            }

            DeucarianTheme darkTheme;
            bool darkThemeCreated;
            if (!overwriteExisting && family.DarkTheme != null)
            {
                darkTheme = family.DarkTheme;
                darkThemeCreated = false;
            }
            else
            {
                darkTheme = LoadOrCreateAsset(
                    darkThemePath,
                    () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                    overwriteExisting,
                    out darkThemeCreated);
            }

            DeucarianColorPalette lightPalette;
            bool lightPaletteCreated;
            if (!overwriteExisting && lightTheme.ColorPalette != null)
            {
                lightPalette = lightTheme.ColorPalette;
                lightPaletteCreated = false;
            }
            else
            {
                lightPalette = LoadOrCreateAsset(
                    lightPalettePath,
                    () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                    overwriteExisting,
                    out lightPaletteCreated);
            }

            DeucarianColorPalette darkPalette;
            bool darkPaletteCreated;
            if (!overwriteExisting && darkTheme.ColorPalette != null)
            {
                darkPalette = darkTheme.ColorPalette;
                darkPaletteCreated = false;
            }
            else
            {
                darkPalette = LoadOrCreateAsset(
                    darkPalettePath,
                    () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                    overwriteExisting,
                    out darkPaletteCreated);
            }

            DeucarianColorRoleLibrary existingLibrary = !overwriteExisting
                ? lightPalette.RoleLibrary ?? darkPalette.RoleLibrary
                : null;
            DeucarianColorRoleLibrary library;
            bool libraryCreated;
            if (existingLibrary != null)
            {
                library = existingLibrary;
                libraryCreated = false;
            }
            else
            {
                library = LoadOrCreateAsset(
                    libraryPath,
                    () => ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>(),
                    overwriteExisting,
                    out libraryCreated);
            }

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets();
            bool libraryChanged = library.RemoveNullRoles() > 0;
            for (int i = 0; i < darkDefinitions.Count; i++)
            {
                BuiltinRoleDefinition definition = darkDefinitions[i];
                DeucarianColorRole role = FindRoleInLibrary(library, definition.Id);
                bool roleCreated = false;
                if (role == null)
                {
                    string rolePath = CombineAssetPath(rolesFolder, SafeAssetName(definition.DisplayName) + ".asset");
                    role = LoadOrCreateAsset(
                        rolePath,
                        () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                        overwriteExisting,
                        out roleCreated);
                }

                if (roleCreated
                    || overwriteExisting
                    || ShouldRepairGeneratedPairedRole(role, lightDefinitions[i], definition))
                {
                    ConfigurePairedRole(
                        role,
                        lightDefinitions[i],
                        definition,
                        roleCreated || overwriteExisting);
                }

                libraryChanged |= library.AddRole(role);
                result.AddRole(role);
            }

            if (libraryChanged || libraryCreated || overwriteExisting)
            {
                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }

            RepairFamilyPalette(
                lightPalette,
                lightPaletteCreated,
                overwriteExisting,
                preset.LightPaletteId,
                preset.LightPaletteDisplayName,
                DeucarianThemeMode.Light,
                library,
                result.Roles,
                lightDefinitions);
            RepairFamilyPalette(
                darkPalette,
                darkPaletteCreated,
                overwriteExisting,
                preset.DarkPaletteId,
                preset.DarkPaletteDisplayName,
                DeucarianThemeMode.Dark,
                library,
                result.Roles,
                darkDefinitions);

            RepairFamilyTheme(
                lightTheme,
                lightThemeCreated,
                overwriteExisting,
                preset.LightThemeId,
                preset.LightThemeDisplayName,
                lightPalette);
            RepairFamilyTheme(
                darkTheme,
                darkThemeCreated,
                overwriteExisting,
                preset.DarkThemeId,
                preset.DarkThemeDisplayName,
                darkPalette);

            if (familyCreated
                || overwriteExisting
                || family.LightTheme != lightTheme
                || family.DarkTheme != darkTheme
                || string.IsNullOrWhiteSpace(family.FamilyId)
                || string.IsNullOrWhiteSpace(family.DisplayName))
            {
                string familyId = familyCreated || overwriteExisting || string.IsNullOrWhiteSpace(family.FamilyId)
                    ? preset.FamilyId
                    : family.FamilyId;
                string familyDisplayName = familyCreated || overwriteExisting || string.IsNullOrWhiteSpace(family.DisplayName)
                    ? preset.FamilyDisplayName
                    : family.DisplayName;
                family.Configure(familyId, familyDisplayName, lightTheme, darkTheme);
                EditorUtility.SetDirty(family);
            }

            result.RoleLibrary = library;
            result.ThemeFamily = family;
            result.LightPalette = lightPalette;
            result.DarkPalette = darkPalette;
            result.LightTheme = lightTheme;
            result.DarkTheme = darkTheme;
            result.Palette = darkPalette;
            result.Theme = darkTheme;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        private static void RepairFamilyPalette(
            DeucarianColorPalette palette,
            bool paletteCreated,
            bool overwriteExisting,
            string paletteId,
            string paletteDisplayName,
            DeucarianThemeMode themeMode,
            DeucarianColorRoleLibrary library,
            IReadOnlyList<DeucarianColorRole> roles,
            IReadOnlyList<BuiltinRoleDefinition> definitions)
        {
            bool paletteChanged = paletteCreated || overwriteExisting;
            if (paletteCreated || overwriteExisting)
            {
                palette.Configure(paletteId, paletteDisplayName, library, themeMode);
                palette.ClearEntries();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(palette.PaletteId)
                    || string.IsNullOrWhiteSpace(palette.DisplayName))
                {
                    palette.Configure(
                        string.IsNullOrWhiteSpace(palette.PaletteId) ? paletteId : palette.PaletteId,
                        string.IsNullOrWhiteSpace(palette.DisplayName) ? paletteDisplayName : palette.DisplayName,
                        library,
                        themeMode);
                    paletteChanged = true;
                }
                else if (palette.RoleLibrary != library)
                {
                    palette.SetRoleLibrary(library);
                    paletteChanged = true;
                }

                if (!palette.HasThemeMode || palette.ThemeMode != themeMode)
                {
                    palette.SetThemeMode(themeMode);
                    paletteChanged = true;
                }

                paletteChanged |= palette.RemoveNullEntries() > 0;
            }

            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianColorRole role = roles[i];
                if (role == null)
                {
                    continue;
                }

                if (overwriteExisting
                    || !TryGetPaletteEntry(palette, role, out DeucarianColorEntry entry)
                    || entry == null
                    || entry.Role == null
                    || IsPackageMissingColor(entry.Color))
                {
                    palette.SetColor(role, definitions[i].DefaultColor, definitions[i].Description);
                    paletteChanged = true;
                }
                else if (entry.Role != role)
                {
                    palette.SetColor(role, entry.Color, entry.Note);
                    paletteChanged = true;
                }
            }

            if (library != null)
            {
                paletteChanged |= palette.AddMissingRolesFromLibrary() > 0;
            }

            if (paletteChanged)
            {
                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }
        }

        private static void RepairFamilyTheme(
            DeucarianTheme theme,
            bool themeCreated,
            bool overwriteExisting,
            string themeId,
            string themeDisplayName,
            DeucarianColorPalette palette)
        {
            if (!themeCreated
                && !overwriteExisting
                && theme.ColorPalette == palette
                && !string.IsNullOrWhiteSpace(theme.ThemeId)
                && !string.IsNullOrWhiteSpace(theme.DisplayName))
            {
                return;
            }

            theme.Configure(
                themeCreated || overwriteExisting || string.IsNullOrWhiteSpace(theme.ThemeId) ? themeId : theme.ThemeId,
                themeCreated || overwriteExisting || string.IsNullOrWhiteSpace(theme.DisplayName)
                    ? themeDisplayName
                    : theme.DisplayName,
                palette,
                theme.VisualStyle);
            EditorUtility.SetDirty(theme);
        }

        private static void ValidatePairedDefinitions(
            IReadOnlyList<BuiltinRoleDefinition> lightDefinitions,
            IReadOnlyList<BuiltinRoleDefinition> darkDefinitions)
        {
            if (lightDefinitions == null || darkDefinitions == null || lightDefinitions.Count != darkDefinitions.Count)
            {
                throw new ArgumentException("Light and dark theme definitions must contain the same roles.");
            }

            for (int i = 0; i < lightDefinitions.Count; i++)
            {
                if (!string.Equals(lightDefinitions[i].Id, darkDefinitions[i].Id, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Light and dark theme definitions must use the same role order and IDs.");
                }
            }
        }

        private static void ConfigureRole(DeucarianColorRole role, BuiltinRoleDefinition definition)
        {
            role.Configure(
                definition.Id,
                definition.DisplayName,
                definition.Category,
                definition.Description,
                definition.DefaultColor,
                true);
            EditorUtility.SetDirty(role);
        }

        private static void ConfigurePairedRole(
            DeucarianColorRole role,
            BuiltinRoleDefinition lightDefinition,
            BuiltinRoleDefinition darkDefinition,
            bool resetToDefinitions)
        {
            if (resetToDefinitions)
            {
                role.Configure(
                    darkDefinition.Id,
                    darkDefinition.DisplayName,
                    darkDefinition.Category,
                    darkDefinition.Description,
                    lightDefinition.DefaultColor,
                    darkDefinition.DefaultColor,
                    true);
                EditorUtility.SetDirty(role);
                return;
            }

            Color lightColor;
            Color darkColor;
            if (role.HasPairedDefaultColors)
            {
                lightColor = IsPackageMissingColor(role.LightDefaultColor)
                    ? lightDefinition.DefaultColor
                    : role.LightDefaultColor;
                darkColor = IsPackageMissingColor(role.DarkDefaultColor)
                    ? darkDefinition.DefaultColor
                    : role.DarkDefaultColor;
            }
            else
            {
                lightColor = lightDefinition.DefaultColor;
                darkColor = IsPackageMissingColor(role.DefaultColor)
                    ? darkDefinition.DefaultColor
                    : role.DefaultColor;
            }

            role.Configure(
                string.IsNullOrWhiteSpace(role.Id) ? darkDefinition.Id : role.Id,
                string.IsNullOrWhiteSpace(role.DisplayName) ? darkDefinition.DisplayName : role.DisplayName,
                string.IsNullOrWhiteSpace(role.Category) ? darkDefinition.Category : role.Category,
                role.Description,
                lightColor,
                darkColor,
                role.IsCoreRole);
            EditorUtility.SetDirty(role);
        }

        private static void AssignStyleIfMissing(
            DeucarianTheme theme,
            DeucarianThemeStyle style,
            bool overwriteExisting)
        {
            if (theme == null || style == null || (!overwriteExisting && theme.VisualStyle != null))
            {
                return;
            }

            theme.SetVisualStyle(style);
            EditorUtility.SetDirty(theme);
        }

        private static DeucarianDefaultThemeAssets CreateThemeAssets(
            string rootFolder,
            bool overwriteExisting,
            IReadOnlyList<BuiltinRoleDefinition> definitions,
            ThemePresetDefinition preset)
        {
            string normalizedRoot = NormalizeAssetPath(rootFolder);
            if (string.IsNullOrEmpty(normalizedRoot) || (normalizedRoot != "Assets" && !normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme assets must be created under the Assets folder.", nameof(rootFolder));
            }

            string rolesFolder = CombineAssetPath(normalizedRoot, "Roles");
            EnsureFolder(rolesFolder);

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets();

            for (int i = 0; i < definitions.Count; i++)
            {
                BuiltinRoleDefinition definition = definitions[i];
                string rolePath = CombineAssetPath(rolesFolder, SafeAssetName(definition.DisplayName) + ".asset");
                DeucarianColorRole role = LoadOrCreateAsset(
                    rolePath,
                    () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                    overwriteExisting,
                    out bool roleCreated);

                if (roleCreated || overwriteExisting || ShouldRepairGeneratedRole(role, definition))
                {
                    role.Configure(
                        definition.Id,
                        definition.DisplayName,
                        definition.Category,
                        definition.Description,
                        definition.DefaultColor,
                        true);
                    EditorUtility.SetDirty(role);
                }

                result.AddRole(role);
            }

            string libraryPath = CombineAssetPath(normalizedRoot, preset.RoleLibraryFileName);
            DeucarianColorRoleLibrary library = LoadOrCreateAsset(
                libraryPath,
                () => ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>(),
                overwriteExisting,
                out bool libraryCreated);

            bool libraryChanged = library.RemoveNullRoles() > 0;
            for (int i = 0; i < result.Roles.Count; i++)
            {
                libraryChanged |= library.AddRole(result.Roles[i]);
            }

            if (libraryChanged || libraryCreated || overwriteExisting)
            {
                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }

            string palettePath = CombineAssetPath(normalizedRoot, preset.PaletteFileName);
            DeucarianColorPalette palette = LoadOrCreateAsset(
                palettePath,
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            bool paletteChanged = paletteCreated || overwriteExisting || palette.RoleLibrary != library;
            palette.Configure(preset.PaletteId, preset.PaletteDisplayName, library);
            if (paletteCreated || overwriteExisting)
            {
                palette.ClearEntries();
            }
            else
            {
                paletteChanged |= palette.RemoveNullEntries() > 0;
            }

            for (int i = 0; i < result.Roles.Count; i++)
            {
                DeucarianColorRole role = result.Roles[i];
                Color defaultColor = definitions[i].DefaultColor;
                if (role == null)
                {
                    continue;
                }

                if (paletteCreated
                    || overwriteExisting
                    || !PaletteHasEntryForRole(palette, role)
                    || PaletteRoleColorIsMissing(palette, role))
                {
                    palette.SetColor(role, defaultColor, definitions[i].Description);
                    paletteChanged = true;
                }
            }

            if (paletteChanged)
            {
                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }

            string themePath = CombineAssetPath(normalizedRoot, preset.ThemeFileName);
            DeucarianTheme theme = LoadOrCreateAsset(
                themePath,
                () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                overwriteExisting,
                out bool themeCreated);

            if (themeCreated || overwriteExisting || theme.ColorPalette != palette || string.IsNullOrWhiteSpace(theme.ThemeId))
            {
                theme.Configure(preset.ThemeId, preset.ThemeDisplayName, palette);
                EditorUtility.SetDirty(theme);
            }

            result.RoleLibrary = library;
            result.Palette = palette;
            result.Theme = theme;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return result;
        }

        private static T LoadOrCreateAsset<T>(string assetPath, Func<T> factory, bool overwriteExisting, out bool created)
            where T : ScriptableObject
        {
            created = false;
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (existing != null)
            {
                if (!overwriteExisting)
                {
                    T typedExisting = existing as T;
                    if (typedExisting != null)
                    {
                        EnsureAssetObjectNameMatchesPath(typedExisting, assetPath);
                        return typedExisting;
                    }

                    string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    ThemingLog.Editor.Warning($"Asset already exists at {assetPath} and is not a {typeof(T).Name}. Creating {uniquePath} instead.");
                    assetPath = uniquePath;
                }
                else
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            T asset = factory();
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            created = true;
            return asset;
        }

        private static DeucarianColorRole CreateRoleAsset(
            string rolesFolder,
            BuiltinRoleDefinition definition,
            bool overwriteExisting)
        {
            string rolePath = CombineAssetPath(rolesFolder, SafeAssetName(definition.DisplayName) + ".asset");
            DeucarianColorRole role = LoadOrCreateAsset(
                rolePath,
                () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                overwriteExisting,
                out _);

            role.Configure(
                definition.Id,
                definition.DisplayName,
                definition.Category,
                definition.Description,
                definition.DefaultColor,
                true);
            EditorUtility.SetDirty(role);
            return role;
        }

        private static DeucarianColorRole FindRoleInLibrary(DeucarianColorRoleLibrary library, string roleId)
        {
            if (library != null && library.TryGetRoleById(roleId, out DeucarianColorRole role))
            {
                return role;
            }

            return null;
        }

        private static DeucarianColorRole FindRoleAssetById(string roleId)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(DeucarianColorRole));
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                DeucarianColorRole role = AssetDatabase.LoadAssetAtPath<DeucarianColorRole>(path);
                if (role != null && string.Equals(role.Id, roleId, StringComparison.Ordinal))
                {
                    return role;
                }
            }

            return null;
        }

        private static bool TryGetPaletteEntry(
            DeucarianColorPalette palette,
            DeucarianColorRole role,
            out DeucarianColorEntry matchingEntry)
        {
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                if (entry.Role == role || string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal))
                {
                    matchingEntry = entry;
                    return true;
                }
            }

            matchingEntry = null;
            return false;
        }

        private static void ValidateAssetPath(string assetPath, string parameterName)
        {
            if (string.IsNullOrEmpty(assetPath)
                || !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                || (assetPath != "Assets" && !assetPath.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Asset paths must be .asset files under the Assets folder.", parameterName);
            }
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

        private static string GetAssetFolder(string assetPath)
        {
            string normalized = NormalizeAssetPath(assetPath);
            int slashIndex = normalized.LastIndexOf('/');
            return slashIndex <= 0 ? "Assets" : normalized.Substring(0, slashIndex);
        }

        private static string CombineAssetPath(string left, string right)
        {
            return NormalizeAssetPath(left.TrimEnd('/') + "/" + right.TrimStart('/'));
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').Trim().TrimEnd('/');
        }

        private static string SafeAssetName(string value)
        {
            string safeName = string.IsNullOrWhiteSpace(value) ? "Color Role" : value;
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                safeName = safeName.Replace(invalidCharacters[i], '-');
            }

            return safeName;
        }

        private static string DeriveThemeAssetName(string paletteAssetName)
        {
            if (string.IsNullOrWhiteSpace(paletteAssetName))
            {
                return "Theme";
            }

            if (paletteAssetName.EndsWith("Palette", StringComparison.Ordinal))
            {
                return paletteAssetName.Substring(0, paletteAssetName.Length - "Palette".Length) + "Theme";
            }

            return paletteAssetName + "Theme";
        }

        private static string DeriveThemeFamilyBaseName(string familyAssetName)
        {
            if (string.IsNullOrWhiteSpace(familyAssetName))
            {
                return "Deucarian";
            }

            if (familyAssetName.EndsWith("ThemeFamily", StringComparison.Ordinal))
            {
                string withoutSuffix = familyAssetName.Substring(
                    0,
                    familyAssetName.Length - "ThemeFamily".Length);
                return string.IsNullOrWhiteSpace(withoutSuffix) ? "Deucarian" : withoutSuffix;
            }

            if (familyAssetName.EndsWith("Family", StringComparison.Ordinal))
            {
                string withoutSuffix = familyAssetName.Substring(0, familyAssetName.Length - "Family".Length);
                return string.IsNullOrWhiteSpace(withoutSuffix) ? "Deucarian" : withoutSuffix;
            }

            return familyAssetName;
        }

        private static string BuildThemeFamilyDisplayName(string baseName)
        {
            string displayName = HumanizeAssetName(baseName);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Deucarian";
            }

            return displayName.EndsWith(" Theme", StringComparison.OrdinalIgnoreCase)
                ? displayName
                : displayName + " Theme";
        }

        private static string HumanizeAssetName(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return string.Empty;
            }

            List<char> characters = new List<char>();
            for (int i = 0; i < assetName.Length; i++)
            {
                char character = assetName[i];
                if (i > 0 && char.IsUpper(character) && !char.IsWhiteSpace(assetName[i - 1]))
                {
                    characters.Add(' ');
                }

                characters.Add(character);
            }

            return new string(characters.ToArray()).Trim();
        }

        private static string BuildStableId(string prefix, string assetName)
        {
            string normalizedName = string.IsNullOrWhiteSpace(assetName)
                ? "asset"
                : assetName.Trim();
            List<char> characters = new List<char>();
            bool previousWasSeparator = false;

            for (int i = 0; i < normalizedName.Length; i++)
            {
                char character = char.ToLowerInvariant(normalizedName[i]);
                if (char.IsLetterOrDigit(character))
                {
                    characters.Add(character);
                    previousWasSeparator = false;
                }
                else if (!previousWasSeparator)
                {
                    characters.Add('.');
                    previousWasSeparator = true;
                }
            }

            string suffix = new string(characters.ToArray()).Trim('.');
            return string.IsNullOrEmpty(suffix) ? prefix : prefix + "." + suffix;
        }

        internal static void EnsureAssetObjectNameMatchesPath(UnityEngine.Object asset, string assetPath)
        {
            if (asset == null || string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string expectedName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrEmpty(expectedName) || asset.name == expectedName)
            {
                return;
            }

            asset.name = expectedName;
            EditorUtility.SetDirty(asset);
        }

        private static bool ShouldRepairGeneratedRole(DeucarianColorRole role, BuiltinRoleDefinition definition)
        {
            if (role == null)
            {
                return false;
            }

            return !string.Equals(role.Id, definition.Id, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(role.DisplayName)
                || string.IsNullOrWhiteSpace(role.Category)
                || IsPackageMissingColor(role.DefaultColor);
        }

        private static bool ShouldRepairGeneratedPairedRole(
            DeucarianColorRole role,
            BuiltinRoleDefinition lightDefinition,
            BuiltinRoleDefinition darkDefinition)
        {
            return ShouldRepairGeneratedRole(role, darkDefinition)
                || !string.Equals(role.Id, lightDefinition.Id, StringComparison.Ordinal)
                || !role.HasPairedDefaultColors
                || IsPackageMissingColor(role.GetDefaultColor(DeucarianThemeMode.Light))
                || IsPackageMissingColor(role.GetDefaultColor(DeucarianThemeMode.Dark));
        }

        private static bool ShouldRepairGeneratedStyle(DeucarianThemeStyle style, DeucarianThemeStylePreset definition)
        {
            if (style == null)
            {
                return false;
            }

            return !string.Equals(style.StyleId, definition.Id, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(style.DisplayName)
                || string.IsNullOrWhiteSpace(style.Description)
                || style.SurfaceProfile == null
                || !string.Equals(style.SurfaceProfile.ProfileId, definition.SurfaceProfileId, StringComparison.Ordinal)
                || style.ShapeProfile == null
                || !string.Equals(style.ShapeProfile.ProfileId, definition.ShapeProfileId, StringComparison.Ordinal)
                || style.StrokeProfile == null
                || !string.Equals(style.StrokeProfile.ProfileId, definition.StrokeProfileId, StringComparison.Ordinal)
                || style.Density != definition.Density;
        }

        private static bool ShouldRepairSurfaceProfile(
            DeucarianThemeSurfaceProfile profile,
            DeucarianThemeSurfaceProfilePreset definition)
        {
            return profile != null
                && (!string.Equals(profile.ProfileId, definition.Id, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(profile.DisplayName)
                    || string.IsNullOrWhiteSpace(profile.Description));
        }

        private static bool ShouldRepairShapeProfile(
            DeucarianThemeShapeProfile profile,
            DeucarianThemeShapeProfilePreset definition)
        {
            return profile != null
                && (!string.Equals(profile.ProfileId, definition.Id, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(profile.DisplayName)
                    || string.IsNullOrWhiteSpace(profile.Description));
        }

        private static bool ShouldRepairStrokeProfile(
            DeucarianThemeStrokeProfile profile,
            DeucarianThemeStrokeProfilePreset definition)
        {
            return profile != null
                && (!string.Equals(profile.ProfileId, definition.Id, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(profile.DisplayName)
                    || string.IsNullOrWhiteSpace(profile.Description));
        }

        private static DeucarianThemeSurfaceProfile FindSurfaceProfile(
            IReadOnlyList<DeucarianThemeSurfaceProfile> profiles,
            string id)
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] != null && string.Equals(profiles[i].ProfileId, id, StringComparison.Ordinal))
                {
                    return profiles[i];
                }
            }

            return null;
        }

        private static DeucarianThemeShapeProfile FindShapeProfile(
            IReadOnlyList<DeucarianThemeShapeProfile> profiles,
            string id)
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] != null && string.Equals(profiles[i].ProfileId, id, StringComparison.Ordinal))
                {
                    return profiles[i];
                }
            }

            return null;
        }

        private static DeucarianThemeStrokeProfile FindStrokeProfile(
            IReadOnlyList<DeucarianThemeStrokeProfile> profiles,
            string id)
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] != null && string.Equals(profiles[i].ProfileId, id, StringComparison.Ordinal))
                {
                    return profiles[i];
                }
            }

            return null;
        }

        private static bool PaletteHasEntryForRole(DeucarianColorPalette palette, DeucarianColorRole role)
        {
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                if (entry.Role == role || string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PaletteRoleColorIsMissing(DeucarianColorPalette palette, DeucarianColorRole role)
        {
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                if ((entry.Role == role || string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal))
                    && IsPackageMissingColor(entry.Color))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPackageMissingColor(Color color)
        {
            const float tolerance = 0.0001f;
            return Mathf.Abs(color.r - Color.magenta.r) <= tolerance
                && Mathf.Abs(color.g - Color.magenta.g) <= tolerance
                && Mathf.Abs(color.b - Color.magenta.b) <= tolerance
                && Mathf.Abs(color.a - Color.magenta.a) <= tolerance;
        }

        private static IReadOnlyList<BuiltinRoleDefinition> CreateMinimalDefaultRoleDefinitions()
        {
            return CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Dark);
        }

        private static IReadOnlyList<BuiltinRoleDefinition> CreateMinimalDefaultRoleDefinitions(
            DeucarianThemeMode mode)
        {
            return new[]
            {
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Background, "Background", DeucarianColorRoleCategories.Semantic, "Main UI or scene background surface.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Background)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Surface, "Surface", DeucarianColorRoleCategories.Semantic, "Default panel or card surface.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Surface)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.SurfaceRaised, "Surface Raised", DeucarianColorRoleCategories.Semantic, "Elevated panel or overlay surface.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.SurfaceRaised)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Primary, "Primary", DeucarianColorRoleCategories.Semantic, "Primary action or brand emphasis.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Primary)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Secondary, "Secondary", DeucarianColorRoleCategories.Semantic, "Secondary action or brand emphasis.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Secondary)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Accent, "Accent", DeucarianColorRoleCategories.Semantic, "Subtle accent or supporting emphasis.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Accent)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Primary, "Text Primary", DeucarianColorRoleCategories.Text, "Primary readable text.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Text.Primary)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Secondary, "Text Secondary", DeucarianColorRoleCategories.Text, "Secondary readable text.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Text.Secondary)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Muted, "Text Muted", DeucarianColorRoleCategories.Text, "Muted helper or supporting text.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Text.Muted)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Disabled, "Text Disabled", DeucarianColorRoleCategories.Text, "Disabled or unavailable text.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Text.Disabled)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Success, "Success", DeucarianColorRoleCategories.Status, "Positive state or confirmation.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Status.Success)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Warning, "Warning", DeucarianColorRoleCategories.Status, "Warning state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Status.Warning)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Error, "Error", DeucarianColorRoleCategories.Status, "Error or destructive state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Status.Error)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Info, "Info", DeucarianColorRoleCategories.Status, "Informational state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Status.Info)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Normal, "UI Normal", DeucarianColorRoleCategories.UiState, "Default selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Normal)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Highlighted, "UI Highlighted", DeucarianColorRoleCategories.UiState, "Hovered or highlighted selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Highlighted)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Pressed, "UI Pressed", DeucarianColorRoleCategories.UiState, "Pressed selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Pressed)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Selected, "UI Selected", DeucarianColorRoleCategories.UiState, "Selected selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Selected)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Disabled, "UI Disabled", DeucarianColorRoleCategories.UiState, "Disabled selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Disabled)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Focused, "UI Focused", DeucarianColorRoleCategories.UiState, "Keyboard or controller focused UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Focused))
            };
        }

        private static Color BrandColor(DeucarianThemeMode mode, string roleId)
        {
            if (DeucarianBrandThemePreset.TryGetColor(mode, roleId, out Color color))
            {
                return color;
            }

            throw new InvalidOperationException(
                $"Brand theme preset {DeucarianBrandThemePreset.Version} has no {mode} value for role '{roleId}'.");
        }

        private static IReadOnlyList<BuiltinRoleDefinition> CreateGameRoleDefinitions()
        {
            return new[]
            {
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Health, "Health", DeucarianColorRoleCategories.Gameplay, "Health resource color.", new Color32(180, 67, 76, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Mana, "Mana", DeucarianColorRoleCategories.Gameplay, "Mana resource color.", Hex("#5A6FA0")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Stamina, "Stamina", DeucarianColorRoleCategories.Gameplay, "Stamina resource color.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Experience, "Experience", DeucarianColorRoleCategories.Gameplay, "Experience resource color.", new Color32(168, 121, 50, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Interactable, "Interactable", DeucarianColorRoleCategories.Gameplay, "Interactive game affordance color.", Hex("#276065")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Highlight, "Highlight", DeucarianColorRoleCategories.Gameplay, "Selected or highlighted game element color.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Faction.Ally, "Ally", DeucarianColorRoleCategories.Faction, "Friendly team or unit color.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Faction.Enemy, "Enemy", DeucarianColorRoleCategories.Faction, "Hostile team or unit color.", new Color32(180, 67, 76, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Faction.Neutral, "Neutral", DeucarianColorRoleCategories.Faction, "Neutral team or unit color.", Hex("#A8B0BA")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Common, "Item Common", DeucarianColorRoleCategories.ItemRarity, "Common item rarity color.", Hex("#C4CAD1")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Uncommon, "Item Uncommon", DeucarianColorRoleCategories.ItemRarity, "Uncommon item rarity color.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Rare, "Item Rare", DeucarianColorRoleCategories.ItemRarity, "Rare item rarity color.", Hex("#5A6FA0")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Epic, "Item Epic", DeucarianColorRoleCategories.ItemRarity, "Epic item rarity color.", new Color32(128, 103, 169, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Legendary, "Item Legendary", DeucarianColorRoleCategories.ItemRarity, "Legendary item rarity color.", new Color32(168, 121, 50, 255))
            };
        }

        private static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                throw new ArgumentException("Invalid color value: " + hex, nameof(hex));
            }

            return color;
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

        private readonly struct ThemeFamilyPresetDefinition
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

        private readonly struct ThemePresetDefinition
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

        private readonly struct BuiltinRoleDefinition
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
