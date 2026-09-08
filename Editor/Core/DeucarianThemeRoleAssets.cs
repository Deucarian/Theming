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
    internal static class DeucarianThemeRoleAssets
    {
        internal static void ConfigureRole(DeucarianColorRole role, BuiltinRoleDefinition definition)
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

        internal static void ConfigurePairedRole(
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
                lightColor = DeucarianThemePaletteEntryLookup.IsPackageMissingColor(role.LightDefaultColor)
                    ? lightDefinition.DefaultColor
                    : role.LightDefaultColor;
                darkColor = DeucarianThemePaletteEntryLookup.IsPackageMissingColor(role.DarkDefaultColor)
                    ? darkDefinition.DefaultColor
                    : role.DarkDefaultColor;
            }
            else
            {
                lightColor = lightDefinition.DefaultColor;
                darkColor = DeucarianThemePaletteEntryLookup.IsPackageMissingColor(role.DefaultColor)
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

        internal static DeucarianColorRole CreateRoleAsset(
            string rolesFolder,
            BuiltinRoleDefinition definition,
            bool overwriteExisting)
        {
            string rolePath = DeucarianThemeAssetNaming.CombineAssetPath(rolesFolder, DeucarianThemeAssetNaming.SafeAssetName(definition.DisplayName) + ".asset");
            DeucarianColorRole role = DeucarianThemeAssetStore.LoadOrCreateAsset(
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

        internal static DeucarianColorRole FindRoleInLibrary(DeucarianColorRoleLibrary library, string roleId)
        {
            if (library != null && library.TryGetRoleById(roleId, out DeucarianColorRole role))
            {
                return role;
            }

            return null;
        }
    }
}
