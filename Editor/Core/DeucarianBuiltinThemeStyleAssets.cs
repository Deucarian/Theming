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
    internal static class DeucarianBuiltinThemeStyleAssets
    {
        internal static IReadOnlyList<DeucarianThemeStyle> CreateBuiltinThemeStyleAssets(
            string rootFolder,
            bool overwriteExisting = false)
        {
            string normalizedRoot = DeucarianThemeAssetNaming.NormalizeAssetPath(rootFolder);
            if (string.IsNullOrEmpty(normalizedRoot)
                || (normalizedRoot != "Assets" && !normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme style assets must be created under the Assets folder.", nameof(rootFolder));
            }

            DeucarianThemeAssetStore.EnsureFolder(normalizedRoot);
            IReadOnlyList<DeucarianThemeSurfaceProfile> surfaces = CreateBuiltinSurfaceProfiles(
                DeucarianThemeAssetNaming.CombineAssetPath(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, DeucarianDefaultThemeAssetFactory.BuiltinStyleComponentsFolderName),
                    DeucarianDefaultThemeAssetFactory.BuiltinSurfaceProfilesFolderName),
                overwriteExisting);
            IReadOnlyList<DeucarianThemeShapeProfile> shapes = CreateBuiltinShapeProfiles(
                DeucarianThemeAssetNaming.CombineAssetPath(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, DeucarianDefaultThemeAssetFactory.BuiltinStyleComponentsFolderName),
                    DeucarianDefaultThemeAssetFactory.BuiltinShapeProfilesFolderName),
                overwriteExisting);
            IReadOnlyList<DeucarianThemeStrokeProfile> strokes = CreateBuiltinStrokeProfiles(
                DeucarianThemeAssetNaming.CombineAssetPath(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, DeucarianDefaultThemeAssetFactory.BuiltinStyleComponentsFolderName),
                    DeucarianDefaultThemeAssetFactory.BuiltinStrokeProfilesFolderName),
                overwriteExisting);
            DeucarianThemeTypographyProfile systemDefaultTypography = CreateSystemDefaultTypographyProfile(
                DeucarianThemeAssetNaming.CombineAssetPath(
                    DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, DeucarianDefaultThemeAssetFactory.BuiltinStyleComponentsFolderName),
                    DeucarianDefaultThemeAssetFactory.BuiltinTypographyProfilesFolderName),
                overwriteExisting);
            DeucarianThemeTypographyProfile typography =
                DeucarianBundledTypographyAssets.InterProfile ?? systemDefaultTypography;
            IReadOnlyList<DeucarianThemeStylePreset> definitions = DeucarianThemeStylePresets.BuiltinStyles;
            List<DeucarianThemeStyle> styles = new List<DeucarianThemeStyle>();

            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemeStylePreset definition = definitions[i];
                string stylePath = DeucarianThemeAssetNaming.CombineAssetPath(normalizedRoot, definition.FileName);
                DeucarianThemeStyle style = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    stylePath,
                    () => ScriptableObject.CreateInstance<DeucarianThemeStyle>(),
                    overwriteExisting,
                    out bool styleCreated);

                DeucarianThemeSurfaceProfile surface = FindSurfaceProfile(surfaces, definition.SurfaceProfileId);
                DeucarianThemeShapeProfile shape = FindShapeProfile(shapes, definition.ShapeProfileId);
                DeucarianThemeStrokeProfile stroke = FindStrokeProfile(strokes, definition.StrokeProfileId);
                if (styleCreated || overwriteExisting || DeucarianGeneratedThemeRepairPolicy.ShouldRepairGeneratedStyle(style, definition))
                {
                    DeucarianThemeTypographyProfile styleTypography =
                        styleCreated
                        || overwriteExisting
                        || style.TypographyProfile == null
                        || DeucarianGeneratedThemeRepairPolicy.ShouldMigrateLegacyDefaultTypography(style.TypographyProfile)
                            ? typography
                            : style.TypographyProfile;
                    definition.Configure(style);
                    style.SetComposition(surface, shape, stroke, definition.Density, styleTypography);
                    EditorUtility.SetDirty(style);
                }

                styles.Add(style);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return styles;
        }

        internal static IReadOnlyList<DeucarianThemeSurfaceProfile> CreateBuiltinSurfaceProfiles(
            string folder,
            bool overwriteExisting)
        {
            DeucarianThemeAssetStore.EnsureFolder(folder);
            IReadOnlyList<DeucarianThemeSurfaceProfilePreset> definitions =
                DeucarianThemePresentationProfilePresets.BuiltinSurfaces;
            List<DeucarianThemeSurfaceProfile> profiles = new List<DeucarianThemeSurfaceProfile>();
            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemeSurfaceProfilePreset definition = definitions[i];
                DeucarianThemeSurfaceProfile profile = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    DeucarianThemeAssetNaming.CombineAssetPath(folder, definition.FileName),
                    () => ScriptableObject.CreateInstance<DeucarianThemeSurfaceProfile>(),
                    overwriteExisting,
                    out bool created);
                if (created || overwriteExisting || DeucarianGeneratedThemeRepairPolicy.ShouldRepairSurfaceProfile(profile, definition))
                {
                    definition.Configure(profile);
                    EditorUtility.SetDirty(profile);
                }

                profiles.Add(profile);
            }

            return profiles;
        }

        internal static DeucarianThemeTypographyProfile CreateSystemDefaultTypographyProfile(
            string folder,
            bool overwriteExisting)
        {
            DeucarianThemeAssetStore.EnsureFolder(folder);
            DeucarianThemeTypographyProfile profile = DeucarianThemeAssetStore.LoadOrCreateAsset(
                DeucarianThemeAssetNaming.CombineAssetPath(folder, DeucarianDefaultThemeAssetFactory.SystemDefaultTypographyFileName),
                () => ScriptableObject.CreateInstance<DeucarianThemeTypographyProfile>(),
                overwriteExisting,
                out bool created);
            if (created || overwriteExisting || DeucarianGeneratedThemeRepairPolicy.ShouldRepairTypographyProfile(profile))
            {
                profile.Configure(
                    null,
                    DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Title),
                    DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Body),
                    DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Caption));
                EditorUtility.SetDirty(profile);
            }

            return profile;
        }

        internal static IReadOnlyList<DeucarianThemeShapeProfile> CreateBuiltinShapeProfiles(
            string folder,
            bool overwriteExisting)
        {
            DeucarianThemeAssetStore.EnsureFolder(folder);
            IReadOnlyList<DeucarianThemeShapeProfilePreset> definitions =
                DeucarianThemePresentationProfilePresets.BuiltinShapes;
            List<DeucarianThemeShapeProfile> profiles = new List<DeucarianThemeShapeProfile>();
            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemeShapeProfilePreset definition = definitions[i];
                DeucarianThemeShapeProfile profile = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    DeucarianThemeAssetNaming.CombineAssetPath(folder, definition.FileName),
                    () => ScriptableObject.CreateInstance<DeucarianThemeShapeProfile>(),
                    overwriteExisting,
                    out bool created);
                if (created || overwriteExisting || DeucarianGeneratedThemeRepairPolicy.ShouldRepairShapeProfile(profile, definition))
                {
                    definition.Configure(profile);
                    EditorUtility.SetDirty(profile);
                }

                profiles.Add(profile);
            }

            return profiles;
        }

        internal static IReadOnlyList<DeucarianThemeStrokeProfile> CreateBuiltinStrokeProfiles(
            string folder,
            bool overwriteExisting)
        {
            DeucarianThemeAssetStore.EnsureFolder(folder);
            IReadOnlyList<DeucarianThemeStrokeProfilePreset> definitions =
                DeucarianThemePresentationProfilePresets.BuiltinStrokes;
            List<DeucarianThemeStrokeProfile> profiles = new List<DeucarianThemeStrokeProfile>();
            for (int i = 0; i < definitions.Count; i++)
            {
                DeucarianThemeStrokeProfilePreset definition = definitions[i];
                DeucarianThemeStrokeProfile profile = DeucarianThemeAssetStore.LoadOrCreateAsset(
                    DeucarianThemeAssetNaming.CombineAssetPath(folder, definition.FileName),
                    () => ScriptableObject.CreateInstance<DeucarianThemeStrokeProfile>(),
                    overwriteExisting,
                    out bool created);
                if (created || overwriteExisting || DeucarianGeneratedThemeRepairPolicy.ShouldRepairStrokeProfile(profile, definition))
                {
                    definition.Configure(profile);
                    EditorUtility.SetDirty(profile);
                }

                profiles.Add(profile);
            }

            return profiles;
        }

        internal static void AssignStyleIfMissing(
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

        internal static DeucarianThemeSurfaceProfile FindSurfaceProfile(
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

        internal static DeucarianThemeShapeProfile FindShapeProfile(
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

        internal static DeucarianThemeStrokeProfile FindStrokeProfile(
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

        internal static DeucarianThemeStyle FindStyleById(IReadOnlyList<DeucarianThemeStyle> styles, string styleId)
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
    }
}
