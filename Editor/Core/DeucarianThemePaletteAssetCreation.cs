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
    internal static class DeucarianThemePaletteAssetCreation
    {
        internal static DeucarianDefaultThemeAssets CreateMinimalPalette(string palettePath, bool overwriteExisting = false)
        {
            string normalizedPalettePath = DeucarianThemeAssetNaming.NormalizeAssetPath(palettePath);
            DeucarianThemeAssetNaming.ValidateAssetPath(normalizedPalettePath, nameof(palettePath));

            string paletteFolder = DeucarianThemeAssetNaming.GetAssetFolder(normalizedPalettePath);
            string paletteAssetName = Path.GetFileNameWithoutExtension(normalizedPalettePath);
            string themeAssetName = DeucarianThemeAssetNaming.DeriveThemeAssetName(paletteAssetName);
            string themePath = DeucarianThemeAssetNaming.CombineAssetPath(paletteFolder, themeAssetName + ".asset");
            DeucarianThemeAssetStore.EnsureFolder(paletteFolder);

            DeucarianColorPalette palette = DeucarianThemeAssetStore.LoadOrCreateAsset(
                normalizedPalettePath,
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            if (paletteCreated || overwriteExisting || string.IsNullOrWhiteSpace(palette.PaletteId))
            {
                palette.Configure(
                    DeucarianThemeAssetNaming.BuildStableId("deucarian.palette", paletteAssetName),
                    DeucarianThemeAssetNaming.HumanizeAssetName(paletteAssetName),
                    palette.RoleLibrary);
            }

            DeucarianDefaultThemeAssets result = RepairPaletteSetup(palette, themePath, overwriteExisting);
            if (result.Palette == null)
            {
                result.Palette = palette;
            }

            return result;
        }

        internal static DeucarianDefaultThemeAssets RepairPaletteSetup(
            DeucarianColorPalette palette,
            string themePath = null,
            bool overwriteExisting = false)
        {
            if (palette == null)
            {
                throw new ArgumentNullException(nameof(palette));
            }

            string palettePath = DeucarianThemeAssetNaming.NormalizeAssetPath(AssetDatabase.GetAssetPath(palette));
            if (string.IsNullOrEmpty(palettePath))
            {
                throw new ArgumentException("Palette setup repair requires a palette asset saved in the project.", nameof(palette));
            }

            DeucarianThemeAssetNaming.ValidateAssetPath(palettePath, nameof(palette));

            string paletteFolder = DeucarianThemeAssetNaming.GetAssetFolder(palettePath);
            string paletteAssetName = Path.GetFileNameWithoutExtension(palettePath);
            string supportFolder = DeucarianThemeAssetNaming.CombineAssetPath(paletteFolder, paletteAssetName + " Support");
            string rolesFolder = DeucarianThemeAssetNaming.CombineAssetPath(supportFolder, "Roles");
            string libraryPath = DeucarianThemeAssetNaming.CombineAssetPath(supportFolder, paletteAssetName + "RoleLibrary.asset");
            DeucarianThemeAssetStore.EnsureFolder(rolesFolder);

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets
            {
                Palette = palette
            };

            DeucarianColorRoleLibrary library = palette.RoleLibrary != null
                ? palette.RoleLibrary
                : DeucarianThemeAssetStore.LoadOrCreateAsset(
                    libraryPath,
                    () => ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>(),
                    overwriteExisting,
                    out _);

            IReadOnlyList<BuiltinRoleDefinition> definitions = DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions();
            bool libraryChanged = library.RemoveNullRoles() > 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                BuiltinRoleDefinition definition = definitions[i];
                DeucarianColorRole role = DeucarianThemeRoleAssets.FindRoleInLibrary(library, definition.Id)
                    ?? DeucarianThemeAssetStore.FindRoleAssetById(definition.Id)
                    ?? DeucarianThemeRoleAssets.CreateRoleAsset(rolesFolder, definition, overwriteExisting);

                if (DeucarianGeneratedThemeRepairPolicy.ShouldRepairGeneratedRole(role, definition))
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

                if (!DeucarianThemePaletteEntryLookup.TryGetPaletteEntry(palette, role, out DeucarianColorEntry entry)
                    || entry == null
                    || entry.Role == null
                    || DeucarianThemePaletteEntryLookup.IsPackageMissingColor(entry.Color))
                {
                    palette.SetColor(role, role.DefaultColor, role.Description);
                    paletteChanged = true;
                }
            }

            if (paletteChanged || overwriteExisting)
            {
                EditorUtility.SetDirty(palette);
            }

            string resolvedThemePath = DeucarianThemeAssetNaming.NormalizeAssetPath(themePath);
            if (string.IsNullOrEmpty(resolvedThemePath))
            {
                resolvedThemePath = DeucarianThemeAssetNaming.CombineAssetPath(paletteFolder, DeucarianThemeAssetNaming.DeriveThemeAssetName(paletteAssetName) + ".asset");
            }

            DeucarianThemeAssetNaming.ValidateAssetPath(resolvedThemePath, nameof(themePath));
            DeucarianTheme theme = DeucarianThemeAssetStore.LoadOrCreateAsset(
                resolvedThemePath,
                () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                overwriteExisting,
                out bool themeCreated);

            if (themeCreated || overwriteExisting || theme.ColorPalette != palette || string.IsNullOrWhiteSpace(theme.ThemeId))
            {
                string themeId = themeCreated || overwriteExisting || string.IsNullOrWhiteSpace(theme.ThemeId)
                    ? DeucarianThemeAssetNaming.BuildStableId("deucarian.theme", Path.GetFileNameWithoutExtension(resolvedThemePath))
                    : theme.ThemeId;
                string themeDisplayName = themeCreated || overwriteExisting || string.IsNullOrWhiteSpace(theme.DisplayName)
                    ? DeucarianThemeAssetNaming.HumanizeAssetName(Path.GetFileNameWithoutExtension(resolvedThemePath))
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

        internal static DeucarianColorPalette CreatePaletteFromTheme(
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

            string normalizedPalettePath = DeucarianThemeAssetNaming.NormalizeAssetPath(palettePath);
            DeucarianThemeAssetNaming.ValidateAssetPath(normalizedPalettePath, nameof(palettePath));
            DeucarianThemeAssetStore.EnsureFolder(DeucarianThemeAssetNaming.GetAssetFolder(normalizedPalettePath));

            DeucarianColorPalette palette = DeucarianThemeAssetStore.LoadOrCreateAsset(
                normalizedPalettePath,
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            string paletteAssetName = Path.GetFileNameWithoutExtension(normalizedPalettePath);
            if (paletteCreated || overwriteExisting || string.IsNullOrWhiteSpace(palette.PaletteId))
            {
                palette.Configure(
                    DeucarianThemeAssetNaming.BuildStableId("deucarian.palette", paletteAssetName),
                    DeucarianThemeAssetNaming.HumanizeAssetName(paletteAssetName),
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

        internal static DeucarianDefaultThemeAssets CreateThemeAssets(
            string rootFolder,
            bool overwriteExisting,
            IReadOnlyList<BuiltinRoleDefinition> definitions,
            ThemePresetDefinition preset)
        {
            string normalizedRoot = DeucarianThemeAssetNaming.NormalizeAssetPath(rootFolder);
            if (string.IsNullOrEmpty(normalizedRoot) || (normalizedRoot != "Assets" && !normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme assets must be created under the Assets folder.", nameof(rootFolder));
            }

            string rolesFolder = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, "Roles");
            DeucarianThemeAssetStore.EnsureFolder(rolesFolder);

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets();

            for (int i = 0; i < definitions.Count; i++)
            {
                BuiltinRoleDefinition definition = definitions[i];
                string rolePath = DeucarianThemeAssetNaming.CombineAssetPath(rolesFolder, DeucarianThemeAssetNaming.SafeAssetName(definition.DisplayName) + ".asset");
                DeucarianColorRole role = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    rolePath,
                    () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                    overwriteExisting,
                    out bool roleCreated);

                if (roleCreated || overwriteExisting || DeucarianGeneratedThemeRepairPolicy.ShouldRepairGeneratedRole(role, definition))
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

            string libraryPath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.RoleLibraryFileName);
            DeucarianColorRoleLibrary library = DeucarianThemeAssetStore.LoadOrCreateAsset(
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

            string palettePath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.PaletteFileName);
            DeucarianColorPalette palette = DeucarianThemeAssetStore.LoadOrCreateAsset(
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
                    || !DeucarianThemePaletteEntryLookup.PaletteHasEntryForRole(palette, role)
                    || DeucarianThemePaletteEntryLookup.PaletteRoleColorIsMissing(palette, role))
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

            string themePath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, preset.ThemeFileName);
            DeucarianTheme theme = DeucarianThemeAssetStore.LoadOrCreateAsset(
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
    }
}
