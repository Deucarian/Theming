using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using AssetSearchResult = Deucarian.Theming.Editor.DeucarianThemingMenuActions.AssetSearchResult;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeStyleAssets
    {
        public static DeucarianThemeStyle CreateStyleVariant(
            DeucarianThemeStyle source,
            string assetPath)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a style variant"))
            {
                return null;
            }

            string normalizedPath = DeucarianThemingEditorSettings.NormalizeAssetPath(assetPath);
            if (source == null
                || string.IsNullOrWhiteSpace(normalizedPath)
                || !normalizedPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !normalizedPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                ThemingLog.Editor.Warning("A custom style requires an active source style and an .asset path under Assets.");
                return null;
            }

            if (AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null)
            {
                ThemingLog.Editor.Warning($"A Deucarian asset already exists at '{normalizedPath}'.");
                return null;
            }

            string fileName = DeucarianThemeAssetCatalog.PathWithoutExtension(normalizedPath);
            string variantId = DeucarianThemeAssetCatalog.BuildVariantStyleId(fileName);
            int slashIndex = normalizedPath.LastIndexOf('/');
            if (slashIndex > 0)
            {
                DeucarianThemeAssetCatalog.EnsureAssetFolder(normalizedPath.Substring(0, slashIndex));
            }

            DeucarianThemeStyle variant = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            variant.name = fileName;
            variant.Configure(
                variantId,
                fileName,
                $"Reusable custom presentation style created from {source.DisplayName}.",
                source.SurfaceTreatment,
                source.DarkSurfaceTint,
                source.LightSurfaceTint,
                source.SurfaceTintStrength,
                source.SurfaceAlphaMultiplier,
                source.MinimumSurfaceAlpha,
                source.MaximumSurfaceAlpha,
                source.BorderTint,
                source.BorderTintStrength,
                source.BorderAlpha,
                source.BorderWidth,
                source.CornerRadius,
                source.UseGeneratedNoiseTexture,
                source.TextureTint,
                source.GeneratedTextureSize,
                source.GeneratedTextureBlurRadius,
                source.GeneratedTextureBlurStrength);
            variant.SetVariantMetadata(
                variantId,
                fileName,
                $"Reusable custom presentation style created from {source.DisplayName}.");
            variant.SetComposition(
                source.SurfaceProfile,
                source.ShapeProfile,
                source.StrokeProfile,
                source.Density,
                source.TypographyProfile,
                true);

            AssetDatabase.CreateAsset(variant, normalizedPath);
            AssetDatabase.SaveAssets();
            DeucarianThemingEditorSettings.ActiveStyle = variant;
            DeucarianThemeSelectionCommands.SetActiveStyleAndApply(variant);
            DeucarianEditorSelection.SelectAndPing(variant);
            return variant;
        }

        public static DeucarianThemeStyle CreateCustomStyle(
            DeucarianThemeStyle source,
            string assetPath,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size)
        {
            return CreateCustomStyle(
                source,
                assetPath,
                surface,
                corners,
                border,
                size,
                source != null ? source.TypographyProfile : null);
        }

        public static DeucarianThemeStyle CreateCustomStyle(
            DeucarianThemeStyle source,
            string assetPath,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size,
            DeucarianThemeTypographyProfile typography)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a custom style"))
            {
                return null;
            }

            string normalizedPath = DeucarianThemingEditorSettings.NormalizeAssetPath(assetPath);
            if (source == null
                || surface == null
                || corners == null
                || border == null
                || size == DeucarianThemeDensity.Unspecified
                || string.IsNullOrWhiteSpace(normalizedPath)
                || !normalizedPath.StartsWith("Assets/", StringComparison.Ordinal)
                || !normalizedPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                ThemingLog.Editor.Warning(
                    "A custom style requires a source, Surface, Corners, Border, Size, and an .asset path under Assets.");
                return null;
            }

            if (AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null)
            {
                ThemingLog.Editor.Warning($"A Deucarian asset already exists at '{normalizedPath}'.");
                return null;
            }

            string fileName = DeucarianThemeAssetCatalog.PathWithoutExtension(normalizedPath);
            int slashIndex = normalizedPath.LastIndexOf('/');
            if (slashIndex > 0)
            {
                DeucarianThemeAssetCatalog.EnsureAssetFolder(normalizedPath.Substring(0, slashIndex));
            }

            string customId = "deucarian.style.custom." + Guid.NewGuid().ToString("N");
            DeucarianThemeStyle customStyle = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            customStyle.name = fileName;
            customStyle.Configure(
                customId,
                fileName,
                $"Custom presentation style created from {source.DisplayName}.",
                source.SurfaceTreatment,
                source.DarkSurfaceTint,
                source.LightSurfaceTint,
                source.SurfaceTintStrength,
                source.SurfaceAlphaMultiplier,
                source.MinimumSurfaceAlpha,
                source.MaximumSurfaceAlpha,
                source.BorderTint,
                source.BorderTintStrength,
                source.BorderAlpha,
                source.BorderWidth,
                source.CornerRadius,
                source.UseGeneratedNoiseTexture,
                source.TextureTint,
                source.GeneratedTextureSize,
                source.GeneratedTextureBlurRadius,
                source.GeneratedTextureBlurStrength);
            customStyle.SetCustomStyleMetadata(
                customId,
                fileName,
                $"Custom presentation style created from {source.DisplayName}.");
            customStyle.SetComposition(surface, corners, border, size, typography, true);

            AssetDatabase.CreateAsset(customStyle, normalizedPath);
            EditorUtility.SetDirty(customStyle);
            AssetDatabase.SaveAssetIfDirty(customStyle);
            DeucarianThemingEditorSettings.ActiveStyle = customStyle;
            return customStyle;
        }

        public static bool UpdateStyleVariantComposition(
            DeucarianThemeStyle style,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile shape,
            DeucarianThemeStrokeProfile stroke,
            DeucarianThemeDensity density)
        {
            if (style == null || !style.IsVariant)
            {
                ThemingLog.Editor.Warning("Only project-authored Deucarian custom styles can be edited as compositions.");
                return false;
            }

            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("editing a custom style"))
            {
                return false;
            }

            Undo.RecordObject(style, "Edit Deucarian Custom Style");
            style.SetComposition(surface, shape, stroke, density, true);
            EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            DeucarianThemingEditorSettings.ActiveStyle = style;
            DeucarianThemeSceneCommands.RefreshOpenSceneProvidersUsingAsset(style);
            return true;
        }
    }
}
