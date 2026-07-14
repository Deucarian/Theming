using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Creates project assets from package-provided theme pack definitions.
    /// </summary>
    public static class DeucarianThemePackAssetFactory
    {
        private const string RolesFolderName = "Roles";

        public static DeucarianDefaultThemeAssets CreateOrRepairThemePackAssets(
            DeucarianThemePack themePack,
            string rootFolder,
            bool overwriteExisting = false)
        {
            if (themePack == null)
            {
                throw new ArgumentNullException(nameof(themePack));
            }

            return themePack.SupportsThemeFamily
                ? CreateOrRepairThemeFamilyPackAssets(themePack, rootFolder, overwriteExisting)
                : CreateOrRepairLegacyThemePackAssets(themePack, rootFolder, overwriteExisting);
        }

        private static DeucarianDefaultThemeAssets CreateOrRepairLegacyThemePackAssets(
            DeucarianThemePack themePack,
            string rootFolder,
            bool overwriteExisting = false)
        {
            if (themePack == null)
            {
                throw new ArgumentNullException(nameof(themePack));
            }

            string normalizedRoot = NormalizeAssetPath(rootFolder);
            ValidateAssetFolder(normalizedRoot, nameof(rootFolder));
            EnsureFolder(normalizedRoot);

            string rolesFolder = CombineAssetPath(normalizedRoot, RolesFolderName);
            EnsureFolder(rolesFolder);

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets();
            List<DeucarianThemePackRole> createdDefinitions = new List<DeucarianThemePackRole>();
            IReadOnlyList<DeucarianThemePackRole> roleDefinitions = themePack.Roles;
            for (int i = 0; i < roleDefinitions.Count; i++)
            {
                DeucarianThemePackRole definition = roleDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                DeucarianColorRole role = CreateOrRepairRole(rolesFolder, definition, overwriteExisting);
                result.AddRole(role);
                createdDefinitions.Add(definition);
            }

            DeucarianColorRoleLibrary library = LoadOrCreateAsset(
                CombineAssetPath(normalizedRoot, themePack.RoleLibraryFileName),
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

            DeucarianColorPalette palette = LoadOrCreateAsset(
                CombineAssetPath(normalizedRoot, themePack.PaletteFileName),
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            bool paletteChanged = paletteCreated || overwriteExisting || palette.RoleLibrary != library;
            palette.Configure(themePack.PaletteId, themePack.PaletteDisplayName, library);
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
                DeucarianThemePackRole definition = createdDefinitions[i];
                if (RepairPaletteEntry(palette, role, definition, overwriteExisting))
                {
                    paletteChanged = true;
                }
            }

            if (paletteChanged)
            {
                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }

            string stylesFolder = CombineAssetPath(
                normalizedRoot,
                DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName);
            IReadOnlyList<DeucarianThemeStyle> styles =
                DeucarianDefaultThemeAssetFactory.CreateBuiltinThemeStyleAssets(stylesFolder, overwriteExisting);
            for (int i = 0; i < styles.Count; i++)
            {
                result.AddStyle(styles[i]);
            }

            DeucarianThemeStyle defaultStyle = FindStyleById(styles, themePack.DefaultStyleId);
            DeucarianTheme theme = LoadOrCreateAsset(
                CombineAssetPath(normalizedRoot, themePack.ThemeFileName),
                () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                overwriteExisting,
                out bool themeCreated);

            if (themeCreated
                || overwriteExisting
                || theme.ColorPalette != palette
                || string.IsNullOrWhiteSpace(theme.ThemeId))
            {
                theme.Configure(themePack.ThemeId, themePack.ThemeDisplayName, palette, theme.VisualStyle);
                EditorUtility.SetDirty(theme);
            }

            if (defaultStyle != null && (overwriteExisting || theme.VisualStyle == null))
            {
                theme.SetVisualStyle(defaultStyle);
                EditorUtility.SetDirty(theme);
            }

            result.RoleLibrary = library;
            result.Palette = palette;
            result.Theme = theme;
            result.DefaultStyle = defaultStyle;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        private static DeucarianDefaultThemeAssets CreateOrRepairThemeFamilyPackAssets(
            DeucarianThemePack themePack,
            string rootFolder,
            bool overwriteExisting)
        {
            string normalizedRoot = NormalizeAssetPath(rootFolder);
            ValidateAssetFolder(normalizedRoot, nameof(rootFolder));
            EnsureFolder(normalizedRoot);

            string rolesFolder = CombineAssetPath(normalizedRoot, RolesFolderName);
            EnsureFolder(rolesFolder);

            DeucarianThemeFamily family = LoadOrCreateAsset(
                CombineAssetPath(normalizedRoot, themePack.FamilyFileName),
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
                    CombineAssetPath(normalizedRoot, themePack.LightThemeFileName),
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
                    CombineAssetPath(normalizedRoot, themePack.DarkThemeFileName),
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
                    CombineAssetPath(normalizedRoot, themePack.LightPaletteFileName),
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
                    CombineAssetPath(normalizedRoot, themePack.DarkPaletteFileName),
                    () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                    overwriteExisting,
                    out darkPaletteCreated);
            }

            DeucarianColorRoleLibrary library = !overwriteExisting
                ? lightPalette.RoleLibrary ?? darkPalette.RoleLibrary
                : null;
            bool libraryCreated = false;
            if (library == null)
            {
                library = LoadOrCreateAsset(
                    CombineAssetPath(normalizedRoot, themePack.RoleLibraryFileName),
                    () => ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>(),
                    overwriteExisting,
                    out libraryCreated);
            }

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets();
            IReadOnlyList<DeucarianThemePackRole> definitions = themePack.Roles;
            List<DeucarianThemePackRole> createdDefinitions = new List<DeucarianThemePackRole>();
            bool libraryChanged = library.RemoveNullRoles() > 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemePackRole definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                DeucarianColorRole role = FindRoleInLibrary(library, definition.Id);
                bool roleCreated = false;
                if (role == null)
                {
                    string roleAssetName = string.IsNullOrWhiteSpace(definition.AssetName)
                        ? definition.DisplayName
                        : definition.AssetName;
                    role = LoadOrCreateAsset(
                        CombineAssetPath(rolesFolder, SafeAssetName(roleAssetName) + ".asset"),
                        () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                        overwriteExisting,
                        out roleCreated);
                }

                if (RepairPairedRole(role, definition, roleCreated, overwriteExisting))
                {
                    EditorUtility.SetDirty(role);
                }

                libraryChanged |= library.AddRole(role);
                result.AddRole(role);
                createdDefinitions.Add(definition);
            }

            if (libraryChanged || libraryCreated || overwriteExisting)
            {
                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }

            RepairPairedPalette(
                lightPalette,
                lightPaletteCreated,
                overwriteExisting,
                themePack.LightPaletteId,
                themePack.LightPaletteDisplayName,
                DeucarianThemeMode.Light,
                library,
                result.Roles,
                createdDefinitions);
            RepairPairedPalette(
                darkPalette,
                darkPaletteCreated,
                overwriteExisting,
                themePack.DarkPaletteId,
                themePack.DarkPaletteDisplayName,
                DeucarianThemeMode.Dark,
                library,
                result.Roles,
                createdDefinitions);

            string stylesFolder = CombineAssetPath(
                normalizedRoot,
                DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName);
            IReadOnlyList<DeucarianThemeStyle> styles =
                DeucarianDefaultThemeAssetFactory.CreateBuiltinThemeStyleAssets(stylesFolder, overwriteExisting);
            for (int i = 0; i < styles.Count; i++)
            {
                result.AddStyle(styles[i]);
            }

            DeucarianThemeStyle defaultStyle = FindStyleById(styles, themePack.DefaultStyleId);
            RepairPairedTheme(
                lightTheme,
                lightThemeCreated,
                overwriteExisting,
                themePack.LightThemeId,
                themePack.LightThemeDisplayName,
                lightPalette,
                defaultStyle);
            RepairPairedTheme(
                darkTheme,
                darkThemeCreated,
                overwriteExisting,
                themePack.DarkThemeId,
                themePack.DarkThemeDisplayName,
                darkPalette,
                defaultStyle);

            if (familyCreated
                || overwriteExisting
                || family.LightTheme != lightTheme
                || family.DarkTheme != darkTheme
                || string.IsNullOrWhiteSpace(family.FamilyId)
                || string.IsNullOrWhiteSpace(family.DisplayName))
            {
                family.Configure(
                    familyCreated || overwriteExisting || string.IsNullOrWhiteSpace(family.FamilyId)
                        ? themePack.FamilyId
                        : family.FamilyId,
                    familyCreated || overwriteExisting || string.IsNullOrWhiteSpace(family.DisplayName)
                        ? themePack.FamilyDisplayName
                        : family.DisplayName,
                    lightTheme,
                    darkTheme);
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
            result.DefaultStyle = defaultStyle;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        public static DeucarianThemeRuntimeSettings CreateOrRepairRuntimeSettings(
            string resourcesFolder,
            DeucarianTheme defaultTheme,
            bool overwriteExisting = false)
        {
            string normalizedFolder = NormalizeAssetPath(resourcesFolder);
            ValidateAssetFolder(normalizedFolder, nameof(resourcesFolder));
            EnsureFolder(normalizedFolder);

            string settingsPath = CombineAssetPath(
                normalizedFolder,
                DeucarianThemeRuntimeSettings.ResourceName + ".asset");
            DeucarianThemeRuntimeSettings settings = LoadOrCreateAsset(
                settingsPath,
                () => ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>(),
                overwriteExisting,
                out bool settingsCreated);

            if (settingsCreated
                || overwriteExisting
                || settings.DefaultThemeFamily != null
                || settings.LegacyDefaultTheme != defaultTheme)
            {
                settings.Configure(defaultTheme);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return settings;
        }

        public static DeucarianThemeRuntimeSettings CreateOrRepairRuntimeSettings(
            string resourcesFolder,
            DeucarianThemeFamily defaultThemeFamily,
            DeucarianThemeMode defaultThemeMode,
            bool overwriteExisting = false)
        {
            string normalizedFolder = NormalizeAssetPath(resourcesFolder);
            ValidateAssetFolder(normalizedFolder, nameof(resourcesFolder));
            EnsureFolder(normalizedFolder);

            string settingsPath = CombineAssetPath(
                normalizedFolder,
                DeucarianThemeRuntimeSettings.ResourceName + ".asset");
            DeucarianThemeRuntimeSettings settings = LoadOrCreateAsset(
                settingsPath,
                () => ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>(),
                overwriteExisting,
                out bool settingsCreated);

            if (settingsCreated
                || overwriteExisting
                || settings.DefaultThemeFamily != defaultThemeFamily
                || settings.LegacyDefaultTheme != null
                || settings.DefaultThemeMode != defaultThemeMode)
            {
                settings.Configure(defaultThemeFamily, defaultThemeMode);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return settings;
        }

        private static DeucarianColorRole CreateOrRepairRole(
            string rolesFolder,
            DeucarianThemePackRole definition,
            bool overwriteExisting)
        {
            string roleAssetName = string.IsNullOrWhiteSpace(definition.AssetName)
                ? definition.DisplayName
                : definition.AssetName;
            string rolePath = CombineAssetPath(rolesFolder, SafeAssetName(roleAssetName) + ".asset");
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
                    definition.IsCoreRole);
                EditorUtility.SetDirty(role);
            }

            return role;
        }

        private static DeucarianColorRole FindRoleInLibrary(
            DeucarianColorRoleLibrary library,
            string roleId)
        {
            if (library != null && library.TryGetRoleById(roleId, out DeucarianColorRole role))
            {
                return role;
            }

            return null;
        }

        private static bool RepairPairedRole(
            DeucarianColorRole role,
            DeucarianThemePackRole definition,
            bool roleCreated,
            bool overwriteExisting)
        {
            if (role == null || definition == null)
            {
                return false;
            }

            if (roleCreated || overwriteExisting)
            {
                role.Configure(
                    definition.Id,
                    definition.DisplayName,
                    definition.Category,
                    definition.Description,
                    definition.LightColor,
                    definition.DarkColor,
                    definition.IsCoreRole);
                return true;
            }

            Color lightColor;
            Color darkColor;
            bool changed = false;
            if (role.HasPairedDefaultColors)
            {
                lightColor = role.LightDefaultColor;
                darkColor = role.DarkDefaultColor;
                if (IsPackageMissingColor(lightColor))
                {
                    lightColor = definition.LightColor;
                    changed = true;
                }

                if (IsPackageMissingColor(darkColor))
                {
                    darkColor = definition.DarkColor;
                    changed = true;
                }
            }
            else
            {
                lightColor = definition.LightColor;
                darkColor = IsPackageMissingColor(role.DefaultColor)
                    ? definition.DarkColor
                    : role.DefaultColor;
                changed = true;
            }

            string roleId = role.Id;
            if (!string.Equals(roleId, definition.Id, StringComparison.Ordinal))
            {
                roleId = definition.Id;
                changed = true;
            }

            string displayName = role.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = definition.DisplayName;
                changed = true;
            }

            string category = role.Category;
            if (string.IsNullOrWhiteSpace(category))
            {
                category = definition.Category;
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            role.Configure(
                roleId,
                displayName,
                category,
                role.Description,
                lightColor,
                darkColor,
                role.IsCoreRole);
            return true;
        }

        private static void RepairPairedPalette(
            DeucarianColorPalette palette,
            bool paletteCreated,
            bool overwriteExisting,
            string paletteId,
            string paletteDisplayName,
            DeucarianThemeMode mode,
            DeucarianColorRoleLibrary library,
            IReadOnlyList<DeucarianColorRole> roles,
            IReadOnlyList<DeucarianThemePackRole> definitions)
        {
            bool paletteChanged = paletteCreated || overwriteExisting;
            if (paletteCreated || overwriteExisting)
            {
                palette.Configure(paletteId, paletteDisplayName, library, mode);
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
                        mode);
                    paletteChanged = true;
                }
                else
                {
                    if (palette.RoleLibrary != library)
                    {
                        palette.SetRoleLibrary(library);
                        paletteChanged = true;
                    }

                    if (!palette.HasThemeMode || palette.ThemeMode != mode)
                    {
                        palette.SetThemeMode(mode);
                        paletteChanged = true;
                    }
                }

                paletteChanged |= palette.RemoveNullEntries() > 0;
            }

            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianThemePackRole definition = definitions[i];
                Color desiredColor = mode == DeucarianThemeMode.Light
                    ? definition.LightColor
                    : definition.DarkColor;
                if (RepairPaletteEntry(
                        palette,
                        roles[i],
                        definition,
                        desiredColor,
                        overwriteExisting))
                {
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

        private static void RepairPairedTheme(
            DeucarianTheme theme,
            bool themeCreated,
            bool overwriteExisting,
            string themeId,
            string themeDisplayName,
            DeucarianColorPalette palette,
            DeucarianThemeStyle defaultStyle)
        {
            if (themeCreated
                || overwriteExisting
                || theme.ColorPalette != palette
                || string.IsNullOrWhiteSpace(theme.ThemeId)
                || string.IsNullOrWhiteSpace(theme.DisplayName))
            {
                theme.Configure(
                    themeCreated || overwriteExisting || string.IsNullOrWhiteSpace(theme.ThemeId)
                        ? themeId
                        : theme.ThemeId,
                    themeCreated || overwriteExisting || string.IsNullOrWhiteSpace(theme.DisplayName)
                        ? themeDisplayName
                        : theme.DisplayName,
                    palette,
                    theme.VisualStyle);
                EditorUtility.SetDirty(theme);
            }

            if (defaultStyle != null && (overwriteExisting || theme.VisualStyle == null))
            {
                theme.SetVisualStyle(defaultStyle);
                EditorUtility.SetDirty(theme);
            }
        }

        private static bool RepairPaletteEntry(
            DeucarianColorPalette palette,
            DeucarianColorRole role,
            DeucarianThemePackRole definition,
            bool overwriteExisting)
        {
            return RepairPaletteEntry(
                palette,
                role,
                definition,
                definition != null ? definition.DefaultColor : default(Color),
                overwriteExisting);
        }

        private static bool RepairPaletteEntry(
            DeucarianColorPalette palette,
            DeucarianColorRole role,
            DeucarianThemePackRole definition,
            Color desiredColor,
            bool overwriteExisting)
        {
            if (palette == null || role == null || definition == null)
            {
                return false;
            }

            if (overwriteExisting
                || !TryGetPaletteEntry(palette, role.Id, out DeucarianColorRole entryRole, out Color entryColor)
                || entryRole == null
                || IsPackageMissingColor(entryColor))
            {
                palette.SetColor(role, desiredColor, definition.Description);
                return true;
            }

            if (entryRole != role)
            {
                palette.SetColor(role, entryColor, definition.Description);
                return true;
            }

            return false;
        }

        private static bool TryGetPaletteEntry(
            DeucarianColorPalette palette,
            string roleId,
            out DeucarianColorRole role,
            out Color color)
        {
            role = null;
            color = DeucarianColorPalette.MissingColor;
            if (palette == null)
            {
                return false;
            }

            string normalizedId = DeucarianColorRole.NormalizeId(roleId);
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                DeucarianColorRole entryRole = entry != null ? entry.Role : null;
                if (entryRole == null || !string.Equals(entryRole.Id, normalizedId, StringComparison.Ordinal))
                {
                    continue;
                }

                role = entryRole;
                color = entry.Color;
                return true;
            }

            return false;
        }

        private static T LoadOrCreateAsset<T>(
            string assetPath,
            Func<T> factory,
            bool overwriteExisting,
            out bool created)
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

        private static void EnsureAssetObjectNameMatchesPath(UnityEngine.Object asset, string assetPath)
        {
            if (asset == null || string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string expectedName = Path.GetFileNameWithoutExtension(assetPath);
            if (!string.IsNullOrEmpty(expectedName) && asset.name != expectedName)
            {
                asset.name = expectedName;
                EditorUtility.SetDirty(asset);
            }
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

        private static bool ShouldRepairGeneratedRole(DeucarianColorRole role, DeucarianThemePackRole definition)
        {
            if (role == null || definition == null)
            {
                return false;
            }

            return !string.Equals(role.Id, definition.Id, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(role.DisplayName)
                || string.IsNullOrWhiteSpace(role.Category)
                || IsPackageMissingColor(role.DefaultColor);
        }

        private static bool IsPackageMissingColor(Color color)
        {
            const float tolerance = 0.0001f;
            return Mathf.Abs(color.r - DeucarianColorPalette.MissingColor.r) <= tolerance
                && Mathf.Abs(color.g - DeucarianColorPalette.MissingColor.g) <= tolerance
                && Mathf.Abs(color.b - DeucarianColorPalette.MissingColor.b) <= tolerance
                && Mathf.Abs(color.a - DeucarianColorPalette.MissingColor.a) <= tolerance;
        }

        private static void ValidateAssetFolder(string folder, string parameterName)
        {
            if (string.IsNullOrEmpty(folder)
                || (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme pack assets must be created under the Assets folder.", parameterName);
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
    }
}
