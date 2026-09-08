using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;


namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemePackRoleAssets
    {
        internal static DeucarianColorRole CreateOrRepairRole(
            string rolesFolder,
            DeucarianThemePackRole definition,
            bool overwriteExisting)
        {
            string roleAssetName = string.IsNullOrWhiteSpace(definition.AssetName)
                ? definition.DisplayName
                : definition.AssetName;
            string rolePath = DeucarianThemeAssetNaming.CombineAssetPath(rolesFolder, DeucarianThemeAssetNaming.SafeAssetName(roleAssetName) + ".asset");
            DeucarianColorRole role = DeucarianThemeAssetStore.LoadOrCreateAsset(
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

        internal static DeucarianColorRole FindRoleInLibrary(
            DeucarianColorRoleLibrary library,
            string roleId)
        {
            if (library != null && library.TryGetRoleById(roleId, out DeucarianColorRole role))
            {
                return role;
            }

            return null;
        }

        internal static bool RepairPairedRole(
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
                if (DeucarianThemePackPaletteRepair.IsPackageMissingColor(lightColor))
                {
                    lightColor = definition.LightColor;
                    changed = true;
                }

                if (DeucarianThemePackPaletteRepair.IsPackageMissingColor(darkColor))
                {
                    darkColor = definition.DarkColor;
                    changed = true;
                }
            }
            else
            {
                lightColor = definition.LightColor;
                darkColor = DeucarianThemePackPaletteRepair.IsPackageMissingColor(role.DefaultColor)
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

        internal static bool ShouldRepairGeneratedRole(DeucarianColorRole role, DeucarianThemePackRole definition)
        {
            if (role == null || definition == null)
            {
                return false;
            }

            return !string.Equals(role.Id, definition.Id, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(role.DisplayName)
                || string.IsNullOrWhiteSpace(role.Category)
                || DeucarianThemePackPaletteRepair.IsPackageMissingColor(role.DefaultColor);
        }
    }
}
