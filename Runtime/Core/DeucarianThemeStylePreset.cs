using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Immutable definition for a built-in visual style preset.
    /// </summary>
    public readonly struct DeucarianThemeStylePreset
    {
        /// <summary>Backward-compatible constructor for legacy inline style presets.</summary>
        public DeucarianThemeStylePreset(
            string fileName,
            string id,
            string displayName,
            string description,
            DeucarianThemeStyleSurfaceTreatment surfaceTreatment,
            Color darkSurfaceTint,
            Color lightSurfaceTint,
            float surfaceTintStrength,
            float surfaceAlphaMultiplier,
            float minimumSurfaceAlpha,
            float maximumSurfaceAlpha,
            Color borderTint,
            float borderTintStrength,
            float borderAlpha,
            float borderWidth,
            float cornerRadius,
            bool useGeneratedNoiseTexture,
            Color textureTint,
            int generatedTextureSize,
            int generatedTextureBlurRadius,
            float generatedTextureBlurStrength)
            : this(
                fileName,
                id,
                displayName,
                description,
                surfaceTreatment,
                null,
                null,
                null,
                DeucarianThemeDensity.Unspecified,
                darkSurfaceTint,
                lightSurfaceTint,
                surfaceTintStrength,
                surfaceAlphaMultiplier,
                minimumSurfaceAlpha,
                maximumSurfaceAlpha,
                borderTint,
                borderTintStrength,
                borderAlpha,
                borderWidth,
                cornerRadius,
                useGeneratedNoiseTexture,
                textureTint,
                generatedTextureSize,
                generatedTextureBlurRadius,
                generatedTextureBlurStrength)
        {
        }

        public DeucarianThemeStylePreset(
            string fileName,
            string id,
            string displayName,
            string description,
            DeucarianThemeStyleSurfaceTreatment surfaceTreatment,
            string surfaceProfileId,
            string shapeProfileId,
            string strokeProfileId,
            DeucarianThemeDensity density,
            Color darkSurfaceTint,
            Color lightSurfaceTint,
            float surfaceTintStrength,
            float surfaceAlphaMultiplier,
            float minimumSurfaceAlpha,
            float maximumSurfaceAlpha,
            Color borderTint,
            float borderTintStrength,
            float borderAlpha,
            float borderWidth,
            float cornerRadius,
            bool useGeneratedNoiseTexture,
            Color textureTint,
            int generatedTextureSize,
            int generatedTextureBlurRadius,
            float generatedTextureBlurStrength)
        {
            FileName = fileName;
            Id = id;
            DisplayName = displayName;
            Description = description;
            SurfaceTreatment = surfaceTreatment;
            SurfaceProfileId = surfaceProfileId;
            ShapeProfileId = shapeProfileId;
            StrokeProfileId = strokeProfileId;
            Density = density;
            DarkSurfaceTint = darkSurfaceTint;
            LightSurfaceTint = lightSurfaceTint;
            SurfaceTintStrength = surfaceTintStrength;
            SurfaceAlphaMultiplier = surfaceAlphaMultiplier;
            MinimumSurfaceAlpha = minimumSurfaceAlpha;
            MaximumSurfaceAlpha = maximumSurfaceAlpha;
            BorderTint = borderTint;
            BorderTintStrength = borderTintStrength;
            BorderAlpha = borderAlpha;
            BorderWidth = borderWidth;
            CornerRadius = cornerRadius;
            UseGeneratedNoiseTexture = useGeneratedNoiseTexture;
            TextureTint = textureTint;
            GeneratedTextureSize = generatedTextureSize;
            GeneratedTextureBlurRadius = generatedTextureBlurRadius;
            GeneratedTextureBlurStrength = generatedTextureBlurStrength;
        }

        public string FileName { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public DeucarianThemeStyleSurfaceTreatment SurfaceTreatment { get; }
        public string SurfaceProfileId { get; }
        public string ShapeProfileId { get; }
        public string StrokeProfileId { get; }
        public DeucarianThemeDensity Density { get; }
        public Color DarkSurfaceTint { get; }
        public Color LightSurfaceTint { get; }
        public float SurfaceTintStrength { get; }
        public float SurfaceAlphaMultiplier { get; }
        public float MinimumSurfaceAlpha { get; }
        public float MaximumSurfaceAlpha { get; }
        public Color BorderTint { get; }
        public float BorderTintStrength { get; }
        public float BorderAlpha { get; }
        public float BorderWidth { get; }
        public float CornerRadius { get; }
        public bool UseGeneratedNoiseTexture { get; }
        public Color TextureTint { get; }
        public int GeneratedTextureSize { get; }
        public int GeneratedTextureBlurRadius { get; }
        public float GeneratedTextureBlurStrength { get; }

        /// <summary>Applies this preset to an existing style asset or runtime instance.</summary>
        public void Configure(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                throw new ArgumentNullException(nameof(style));
            }

            style.Configure(
                Id,
                DisplayName,
                Description,
                SurfaceTreatment,
                DarkSurfaceTint,
                LightSurfaceTint,
                SurfaceTintStrength,
                SurfaceAlphaMultiplier,
                MinimumSurfaceAlpha,
                MaximumSurfaceAlpha,
                BorderTint,
                BorderTintStrength,
                BorderAlpha,
                BorderWidth,
                CornerRadius,
                UseGeneratedNoiseTexture,
                TextureTint,
                GeneratedTextureSize,
                GeneratedTextureBlurRadius,
                GeneratedTextureBlurStrength);
        }
    }

    /// <summary>
    /// Built-in Deucarian visual style presets.
    /// </summary>
    public static class DeucarianThemeStylePresets
    {
        private static readonly DeucarianThemeStylePreset[] BuiltinStylePresets =
        {
            new DeucarianThemeStylePreset(
                "FrostedGlassStyle.asset",
                DeucarianThemeStyleIds.FrostedGlass,
                "Frosted Glass",
                "Translucent glass-like surfaces with cool tinting, fine texture, and soft borders.",
                DeucarianThemeStyleSurfaceTreatment.FrostedGlass,
                DeucarianThemePresentationProfileIds.Surface.FrostedGlass,
                DeucarianThemePresentationProfileIds.Shape.Rounded,
                DeucarianThemePresentationProfileIds.Stroke.Frosted,
                DeucarianThemeDensity.Comfortable,
                new Color(0.78f, 0.9f, 1f, 1f),
                new Color(0.86f, 0.94f, 1f, 1f),
                0.24f,
                0.62f,
                0.48f,
                0.68f,
                Color.white,
                0.58f,
                0.48f,
                1f,
                16f,
                true,
                new Color(1f, 1f, 1f, 0.08f),
                48,
                4,
                0.92f),
            new DeucarianThemeStylePreset(
                "MaterialDarkStyle.asset",
                DeucarianThemeStyleIds.MaterialDark,
                "Material Dark",
                "Solid layered surfaces with restrained radius, crisp borders, and opaque dark chrome.",
                DeucarianThemeStyleSurfaceTreatment.Material,
                DeucarianThemePresentationProfileIds.Surface.Material,
                DeucarianThemePresentationProfileIds.Shape.Tight,
                DeucarianThemePresentationProfileIds.Stroke.Material,
                DeucarianThemeDensity.Compact,
                Hex("#18202A"),
                Hex("#F7FAFC"),
                0.04f,
                1f,
                1f,
                1f,
                Color.white,
                0.1f,
                0.18f,
                1f,
                4f,
                false,
                Color.clear,
                32,
                0,
                0f),
            new DeucarianThemeStylePreset(
                "FluentAcrylicStyle.asset",
                DeucarianThemeStyleIds.FluentAcrylic,
                "Fluent Acrylic",
                "Acrylic-inspired translucent surfaces with subtle tint, texture, and medium-radius chrome.",
                DeucarianThemeStyleSurfaceTreatment.FluentAcrylic,
                DeucarianThemePresentationProfileIds.Surface.FluentAcrylic,
                DeucarianThemePresentationProfileIds.Shape.Soft,
                DeucarianThemePresentationProfileIds.Stroke.Acrylic,
                DeucarianThemeDensity.Standard,
                Hex("#D7E8FF"),
                Hex("#F7FBFF"),
                0.18f,
                0.72f,
                0.52f,
                0.78f,
                Color.white,
                0.42f,
                0.36f,
                1f,
                8f,
                true,
                new Color(1f, 1f, 1f, 0.06f),
                48,
                3,
                0.86f)
        };

        /// <summary>All built-in styles shipped by the theming package.</summary>
        public static IReadOnlyList<DeucarianThemeStylePreset> BuiltinStyles => BuiltinStylePresets;

        /// <summary>Creates a HideAndDontSave runtime style instance for a built-in preset.</summary>
        public static DeucarianThemeStyle CreateRuntimeStyle(string styleId)
        {
            if (!TryGetBuiltinStyle(styleId, out DeucarianThemeStylePreset preset))
            {
                return null;
            }

            DeucarianThemeStyle style = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            style.name = preset.DisplayName;
            style.hideFlags = HideFlags.HideAndDontSave;
            preset.Configure(style);
            return style;
        }

        /// <summary>Finds a built-in style preset by stable style ID.</summary>
        public static bool TryGetBuiltinStyle(string styleId, out DeucarianThemeStylePreset preset)
        {
            string normalizedId = DeucarianColorRole.NormalizeId(styleId);
            for (int i = 0; i < BuiltinStylePresets.Length; i++)
            {
                DeucarianThemeStylePreset candidate = BuiltinStylePresets[i];
                if (string.Equals(candidate.Id, normalizedId, StringComparison.Ordinal))
                {
                    preset = candidate;
                    return true;
                }
            }

            preset = default(DeucarianThemeStylePreset);
            return false;
        }

        private static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                throw new ArgumentException("Invalid color value: " + hex, nameof(hex));
            }

            return color;
        }
    }
}
