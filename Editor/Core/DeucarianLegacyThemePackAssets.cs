using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;


namespace Deucarian.Theming.Editor
{
    internal static class DeucarianLegacyThemePackAssets
    {
        internal static DeucarianDefaultThemeAssets CreateOrRepairLegacyThemePackAssets(
            DeucarianThemePack themePack,
            string rootFolder,
            bool overwriteExisting = false)
        {
            if (themePack == null)
            {
                throw new ArgumentNullException(nameof(themePack));
            }

            string normalizedRoot = DeucarianThemeAssetNaming.NormalizeAssetPath(rootFolder);
            DeucarianThemePackAssetPolicy.ValidateAssetFolder(normalizedRoot, nameof(rootFolder));
            DeucarianThemeAssetStore.EnsureFolder(normalizedRoot);

            string rolesFolder = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, DeucarianThemePackAssetFactory.RolesFolderName);
            DeucarianThemeAssetStore.EnsureFolder(rolesFolder);

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

                DeucarianColorRole role = DeucarianThemePackRoleAssets.CreateOrRepairRole(rolesFolder, definition, overwriteExisting);
                result.AddRole(role);
                createdDefinitions.Add(definition);
            }

            DeucarianColorRoleLibrary library = DeucarianThemeAssetStore.LoadOrCreateAsset(
                DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.RoleLibraryFileName),
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

            DeucarianColorPalette palette = DeucarianThemeAssetStore.LoadOrCreateAsset(
                DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.PaletteFileName),
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
                if (DeucarianThemePackPaletteRepair.RepairPaletteEntry(palette, role, definition, overwriteExisting))
                {
                    paletteChanged = true;
                }
            }

            if (paletteChanged)
            {
                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }

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
            DeucarianTheme theme = DeucarianThemeAssetStore.LoadOrCreateAsset(
                DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, themePack.ThemeFileName),
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
    }
}
