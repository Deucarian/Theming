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
    internal static class DeucarianGeneratedThemeRepairPolicy
    {
        internal static bool ShouldRepairGeneratedRole(DeucarianColorRole role, BuiltinRoleDefinition definition)
        {
            if (role == null)
            {
                return false;
            }

            return !string.Equals(role.Id, definition.Id, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(role.DisplayName)
                || string.IsNullOrWhiteSpace(role.Category)
                || DeucarianThemePaletteEntryLookup.IsPackageMissingColor(role.DefaultColor);
        }

        internal static bool ShouldRepairGeneratedPairedRole(
            DeucarianColorRole role,
            BuiltinRoleDefinition lightDefinition,
            BuiltinRoleDefinition darkDefinition)
        {
            return ShouldRepairGeneratedRole(role, darkDefinition)
                || !string.Equals(role.Id, lightDefinition.Id, StringComparison.Ordinal)
                || !role.HasPairedDefaultColors
                || DeucarianThemePaletteEntryLookup.IsPackageMissingColor(role.GetDefaultColor(DeucarianThemeMode.Light))
                || DeucarianThemePaletteEntryLookup.IsPackageMissingColor(role.GetDefaultColor(DeucarianThemeMode.Dark));
        }

        internal static bool ShouldRepairGeneratedStyle(DeucarianThemeStyle style, DeucarianThemeStylePreset definition)
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
                || style.TypographyProfile == null
                || ShouldMigrateLegacyDefaultTypography(style.TypographyProfile)
                || style.Density != definition.Density;
        }

        internal static bool ShouldMigrateLegacyDefaultTypography(
            DeucarianThemeTypographyProfile profile)
        {
            if (profile == null || profile.FontAsset != null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrWhiteSpace(path)
                || !path.EndsWith("/" + DeucarianDefaultThemeAssetFactory.SystemDefaultTypographyFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return TextStyleMatchesDefault(profile.Title, DeucarianThemeTextRole.Title)
                   && TextStyleMatchesDefault(profile.Body, DeucarianThemeTextRole.Body)
                   && TextStyleMatchesDefault(profile.Caption, DeucarianThemeTextRole.Caption);
        }

        internal static bool TextStyleMatchesDefault(
            DeucarianThemeTextStyle style,
            DeucarianThemeTextRole role)
        {
            DeucarianThemeTextStyle expected = DeucarianThemeTextStyle.DefaultFor(role);
            return Mathf.Approximately(style.FontSize, expected.FontSize)
                   && style.FontStyle == expected.FontStyle
                   && Mathf.Approximately(style.CharacterSpacing, expected.CharacterSpacing)
                   && Mathf.Approximately(style.LineSpacing, expected.LineSpacing);
        }

        internal static bool ShouldRepairTypographyProfile(DeucarianThemeTypographyProfile profile)
        {
            return profile != null
                   && (string.IsNullOrWhiteSpace(profile.DisplayName)
                       || profile.Title.FontSize <= 0f
                       || profile.Body.FontSize <= 0f
                       || profile.Caption.FontSize <= 0f);
        }

        internal static bool ShouldRepairSurfaceProfile(
            DeucarianThemeSurfaceProfile profile,
            DeucarianThemeSurfaceProfilePreset definition)
        {
            return profile != null
                && (!string.Equals(profile.ProfileId, definition.Id, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(profile.DisplayName)
                    || string.IsNullOrWhiteSpace(profile.Description));
        }

        internal static bool ShouldRepairShapeProfile(
            DeucarianThemeShapeProfile profile,
            DeucarianThemeShapeProfilePreset definition)
        {
            return profile != null
                && (!string.Equals(profile.ProfileId, definition.Id, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(profile.DisplayName)
                    || string.IsNullOrWhiteSpace(profile.Description));
        }

        internal static bool ShouldRepairStrokeProfile(
            DeucarianThemeStrokeProfile profile,
            DeucarianThemeStrokeProfilePreset definition)
        {
            return profile != null
                && (!string.Equals(profile.ProfileId, definition.Id, StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(profile.DisplayName)
                    || string.IsNullOrWhiteSpace(profile.Description));
        }
    }
}
