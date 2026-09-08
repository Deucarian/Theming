using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

using BuiltinRoleDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.BuiltinRoleDefinition;
using ThemeFamilyPresetDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.ThemeFamilyPresetDefinition;
using ThemePresetDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.ThemePresetDefinition;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeFamilyAssetCreation
    {
        internal static DeucarianDefaultThemeAssets CreateThemeFamilyAssets(
            string rootFolder,
            bool overwriteExisting,
            IReadOnlyList<BuiltinRoleDefinition> lightDefinitions,
            IReadOnlyList<BuiltinRoleDefinition> darkDefinitions,
            ThemeFamilyPresetDefinition preset)
        {
            string normalizedRoot = DeucarianThemeAssetNaming.NormalizeAssetPath(rootFolder);
            if (string.IsNullOrEmpty(normalizedRoot)
                || (normalizedRoot != "Assets" && !normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme assets must be created under the Assets folder.", nameof(rootFolder));
            }

            ValidatePairedDefinitions(lightDefinitions, darkDefinitions);
            DeucarianThemeAssetStore.EnsureFolder(normalizedRoot);

            string rolesFolder = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.RolesFolderName);
            string libraryPath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.RoleLibraryFileName);
            string lightPalettePath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.LightPaletteFileName);
            string darkPalettePath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.DarkPaletteFileName);
            string lightThemePath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.LightThemeFileName);
            string darkThemePath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.DarkThemeFileName);
            string familyPath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.FamilyFileName);
            DeucarianThemeAssetStore.EnsureFolder(rolesFolder);
            DeucarianThemeAssetStore.EnsureFolder(DeucarianThemeAssetNaming.GetAssetFolder(libraryPath));

            DeucarianThemeFamily family = DeucarianThemeAssetStore.LoadOrCreateAsset(
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
                lightTheme = DeucarianThemeAssetStore.LoadOrCreateAsset(
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
                darkTheme = DeucarianThemeAssetStore.LoadOrCreateAsset(
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
                lightPalette = DeucarianThemeAssetStore.LoadOrCreateAsset(
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
                darkPalette = DeucarianThemeAssetStore.LoadOrCreateAsset(
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
                library = DeucarianThemeAssetStore.LoadOrCreateAsset(
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
                DeucarianColorRole role = DeucarianThemeRoleAssets.FindRoleInLibrary(library, definition.Id);
                bool roleCreated = false;
                if (role == null)
                {
                    string rolePath = DeucarianThemeAssetNaming.CombineAssetPath(rolesFolder, DeucarianThemeAssetNaming.SafeAssetName(definition.DisplayName) + ".asset");
                    role = DeucarianThemeAssetStore.LoadOrCreateAsset(
                        rolePath,
                        () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                        overwriteExisting,
                        out roleCreated);
                }

                if (roleCreated
                    || overwriteExisting
                    || DeucarianGeneratedThemeRepairPolicy.ShouldRepairGeneratedPairedRole(role, lightDefinitions[i], definition))
                {
                    DeucarianThemeRoleAssets.ConfigurePairedRole(
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

        internal static void RepairFamilyPalette(
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
                    || !DeucarianThemePaletteEntryLookup.TryGetPaletteEntry(palette, role, out DeucarianColorEntry entry)
                    || entry == null
                    || entry.Role == null
                    || DeucarianThemePaletteEntryLookup.IsPackageMissingColor(entry.Color))
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

        internal static void RepairFamilyTheme(
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

        internal static void ValidatePairedDefinitions(
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
    }
}
