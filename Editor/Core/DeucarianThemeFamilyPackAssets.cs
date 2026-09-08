using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;


namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeFamilyPackAssets
    {
        internal static DeucarianDefaultThemeAssets CreateOrRepairThemeFamilyPackAssets(
            DeucarianThemePack themePack,
            string rootFolder,
            bool overwriteExisting)
        {
            string normalizedRoot = DeucarianThemeAssetNaming.NormalizeAssetPath(rootFolder);
            DeucarianThemePackAssetPolicy.ValidateAssetFolder(normalizedRoot, nameof(rootFolder));
            DeucarianThemeAssetStore.EnsureFolder(normalizedRoot);

            string rolesFolder = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, DeucarianThemePackAssetFactory.RolesFolderName);
            DeucarianThemeAssetStore.EnsureFolder(rolesFolder);

            DeucarianThemeFamily family = DeucarianThemeAssetStore.LoadOrCreateAsset(
                DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.FamilyFileName),
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
                lightTheme = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.LightThemeFileName),
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
                darkTheme = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.DarkThemeFileName),
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
                lightPalette = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.LightPaletteFileName),
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
                darkPalette = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.DarkPaletteFileName),
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
                library = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.RoleLibraryFileName),
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

                DeucarianColorRole role = DeucarianThemePackRoleAssets.FindRoleInLibrary(library, definition.Id);
                bool roleCreated = false;
                if (role == null)
                {
                    string roleAssetName = string.IsNullOrWhiteSpace(definition.AssetName)
                        ? definition.DisplayName
                        : definition.AssetName;
                    role = DeucarianThemeAssetStore.LoadOrCreateAsset(
                        DeucarianThemeAssetNaming.CombineAssetPath(rolesFolder, DeucarianThemeAssetNaming.SafeAssetName(roleAssetName) + ".asset"),
                        () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                        overwriteExisting,
                        out roleCreated);
                }

                if (DeucarianThemePackRoleAssets.RepairPairedRole(role, definition, roleCreated, overwriteExisting))
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

            string stylesFolder = DeucarianThemeAssetNaming.CombineAssetPath(
                normalizedRoot,
                DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName);
            IReadOnlyList<DeucarianThemeStyle> styles =
                DeucarianDefaultThemeAssetFactory.CreateBuiltinThemeStyleAssets(stylesFolder, overwriteExisting);
            for (int i = 0; i < styles.Count; i++)
            {
                result.AddStyle(styles[i]);
            }

            DeucarianThemeStyle defaultStyle = DeucarianThemePackAssetPolicy.FindStyleById(styles, themePack.DefaultStyleId);
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

        internal static void RepairPairedPalette(
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
                if (DeucarianThemePackPaletteRepair.RepairPaletteEntry(
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

        internal static void RepairPairedTheme(
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
    }
}
