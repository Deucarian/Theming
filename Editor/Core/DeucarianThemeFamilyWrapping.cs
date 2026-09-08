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
    internal static class DeucarianThemeFamilyWrapping
    {
        internal static DeucarianDefaultThemeAssets CreateThemeFamily(
            string familyPath,
            bool overwriteExisting = false)
        {
            string normalizedFamilyPath = DeucarianThemeAssetNaming.NormalizeAssetPath(familyPath);
            DeucarianThemeAssetNaming.ValidateAssetPath(normalizedFamilyPath, nameof(familyPath));

            string rootFolder = DeucarianThemeAssetNaming.GetAssetFolder(normalizedFamilyPath);
            string familyAssetName = Path.GetFileNameWithoutExtension(normalizedFamilyPath);
            string baseName = DeucarianThemeAssetNaming.DeriveThemeFamilyBaseName(familyAssetName);
            string supportFolderName = baseName + " Theme Support";
            string displayName = DeucarianThemeAssetNaming.HumanizeAssetName(baseName);
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
                DeucarianThemeAssetNaming.BuildStableId("deucarian.palette", baseName + " light"),
                displayName + " Light Palette",
                DeucarianThemeAssetNaming.BuildStableId("deucarian.palette", baseName + " dark"),
                displayName + " Dark Palette",
                DeucarianThemeAssetNaming.BuildStableId("deucarian.theme", baseName + " light"),
                displayName + " Light",
                DeucarianThemeAssetNaming.BuildStableId("deucarian.theme", baseName + " dark"),
                displayName + " Dark",
                DeucarianThemeAssetNaming.BuildStableId("deucarian.theme-family", baseName),
                familyDisplayName,
                supportFolderName + "/Roles");

            DeucarianDefaultThemeAssets result = DeucarianThemeFamilyAssetCreation.CreateThemeFamilyAssets(
                rootFolder,
                overwriteExisting,
                DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Light),
                DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Dark),
                preset);

            IReadOnlyList<DeucarianThemeStyle> styles = DeucarianBuiltinThemeStyleAssets.CreateBuiltinThemeStyleAssets(
                DeucarianThemeAssetNaming.CombineAssetPath(rootFolder, supportFolderName + "/Styles"),
                overwriteExisting);
            for (int i = 0; i < styles.Count; i++)
            {
                result.AddStyle(styles[i]);
            }

            result.DefaultStyle = DeucarianBuiltinThemeStyleAssets.FindStyleById(styles, DeucarianThemeStyleIds.FrostedGlass);
            DeucarianBuiltinThemeStyleAssets.AssignStyleIfMissing(result.LightTheme, result.DefaultStyle, overwriteExisting);
            DeucarianBuiltinThemeStyleAssets.AssignStyleIfMissing(result.DarkTheme, result.DefaultStyle, overwriteExisting);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        internal static DeucarianDefaultThemeAssets RepairThemeFamilySetup(
            DeucarianThemeFamily family,
            bool overwriteExisting = false)
        {
            if (family == null)
            {
                throw new ArgumentNullException(nameof(family));
            }

            string familyPath = DeucarianThemeAssetNaming.NormalizeAssetPath(AssetDatabase.GetAssetPath(family));
            if (string.IsNullOrEmpty(familyPath))
            {
                throw new ArgumentException("Theme family repair requires a family asset saved in the project.", nameof(family));
            }

            return CreateThemeFamily(familyPath, overwriteExisting);
        }

        internal static DeucarianDefaultThemeAssets WrapExistingThemeInFamily(
            DeucarianTheme theme,
            DeucarianThemeMode existingThemeMode,
            string familyPath,
            bool overwriteExisting = false)
        {
            if (theme == null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            string normalizedFamilyPath = DeucarianThemeAssetNaming.NormalizeAssetPath(familyPath);
            DeucarianThemeAssetNaming.ValidateAssetPath(normalizedFamilyPath, nameof(familyPath));
            DeucarianThemeAssetStore.EnsureFolder(DeucarianThemeAssetNaming.GetAssetFolder(normalizedFamilyPath));

            DeucarianThemeFamily family = DeucarianThemeAssetStore.LoadOrCreateAsset(
                normalizedFamilyPath,
                () => ScriptableObject.CreateInstance<DeucarianThemeFamily>(),
                overwriteExisting,
                out bool familyCreated);

            string familyAssetName = Path.GetFileNameWithoutExtension(normalizedFamilyPath);
            string baseName = DeucarianThemeAssetNaming.DeriveThemeFamilyBaseName(familyAssetName);
            string familyId = familyCreated || overwriteExisting || string.IsNullOrWhiteSpace(family.FamilyId)
                ? DeucarianThemeAssetNaming.BuildStableId("deucarian.theme-family", baseName)
                : family.FamilyId;
            string familyDisplayName = familyCreated || overwriteExisting || string.IsNullOrWhiteSpace(family.DisplayName)
                ? DeucarianThemeAssetNaming.BuildThemeFamilyDisplayName(baseName)
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
    }
}
