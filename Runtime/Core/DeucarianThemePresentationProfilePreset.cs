using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Immutable built-in surface profile definition.</summary>
    public readonly struct DeucarianThemeSurfaceProfilePreset
    {
        public DeucarianThemeSurfaceProfilePreset(
            string fileName,
            string id,
            string displayName,
            string description,
            DeucarianThemeStyleSurfaceTreatment treatment,
            Color darkTint,
            Color lightTint,
            float tintStrength,
            float alphaMultiplier,
            float minimumAlpha,
            float maximumAlpha,
            bool useTexture,
            Color textureTint,
            int textureSize,
            int blurRadius,
            float blurStrength)
        {
            FileName = fileName;
            Id = id;
            DisplayName = displayName;
            Description = description;
            Treatment = treatment;
            DarkTint = darkTint;
            LightTint = lightTint;
            TintStrength = tintStrength;
            AlphaMultiplier = alphaMultiplier;
            MinimumAlpha = minimumAlpha;
            MaximumAlpha = maximumAlpha;
            UseTexture = useTexture;
            TextureTint = textureTint;
            TextureSize = textureSize;
            BlurRadius = blurRadius;
            BlurStrength = blurStrength;
        }

        public string FileName { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public DeucarianThemeStyleSurfaceTreatment Treatment { get; }
        public Color DarkTint { get; }
        public Color LightTint { get; }
        public float TintStrength { get; }
        public float AlphaMultiplier { get; }
        public float MinimumAlpha { get; }
        public float MaximumAlpha { get; }
        public bool UseTexture { get; }
        public Color TextureTint { get; }
        public int TextureSize { get; }
        public int BlurRadius { get; }
        public float BlurStrength { get; }

        public void Configure(DeucarianThemeSurfaceProfile profile)
        {
            profile.Configure(
                Id,
                DisplayName,
                Description,
                Treatment,
                DarkTint,
                LightTint,
                TintStrength,
                AlphaMultiplier,
                MinimumAlpha,
                MaximumAlpha,
                UseTexture,
                TextureTint,
                TextureSize,
                BlurRadius,
                BlurStrength);
        }
    }

    /// <summary>Immutable built-in shape profile definition.</summary>
    public readonly struct DeucarianThemeShapeProfilePreset
    {
        public DeucarianThemeShapeProfilePreset(
            string fileName,
            string id,
            string displayName,
            string description,
            float cornerRadius)
        {
            FileName = fileName;
            Id = id;
            DisplayName = displayName;
            Description = description;
            CornerRadius = cornerRadius;
        }

        public string FileName { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public float CornerRadius { get; }

        public void Configure(DeucarianThemeShapeProfile profile)
        {
            profile.Configure(Id, DisplayName, Description, CornerRadius);
        }
    }

    /// <summary>Immutable built-in stroke profile definition.</summary>
    public readonly struct DeucarianThemeStrokeProfilePreset
    {
        public DeucarianThemeStrokeProfilePreset(
            string fileName,
            string id,
            string displayName,
            string description,
            Color tint,
            float tintStrength,
            float alpha,
            float width)
        {
            FileName = fileName;
            Id = id;
            DisplayName = displayName;
            Description = description;
            Tint = tint;
            TintStrength = tintStrength;
            Alpha = alpha;
            Width = width;
        }

        public string FileName { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Color Tint { get; }
        public float TintStrength { get; }
        public float Alpha { get; }
        public float Width { get; }

        public void Configure(DeucarianThemeStrokeProfile profile)
        {
            profile.Configure(Id, DisplayName, Description, Tint, TintStrength, Alpha, Width);
        }
    }

    /// <summary>Built-in reusable components used by the curated visual style presets.</summary>
    public static class DeucarianThemePresentationProfilePresets
    {
        private static readonly DeucarianThemeSurfaceProfilePreset[] Surfaces =
        {
            new DeucarianThemeSurfaceProfilePreset(
                "FrostedGlassSurface.asset",
                DeucarianThemePresentationProfileIds.Surface.FrostedGlass,
                "Frosted Glass",
                "Translucent glass surface with cool tinting and fine blurred texture.",
                DeucarianThemeStyleSurfaceTreatment.FrostedGlass,
                new Color(0.78f, 0.9f, 1f, 1f),
                new Color(0.86f, 0.94f, 1f, 1f),
                0.24f,
                0.62f,
                0.48f,
                0.68f,
                true,
                new Color(1f, 1f, 1f, 0.08f),
                48,
                4,
                0.92f),
            new DeucarianThemeSurfaceProfilePreset(
                "FluentAcrylicSurface.asset",
                DeucarianThemePresentationProfileIds.Surface.FluentAcrylic,
                "Fluent Acrylic",
                "Acrylic-inspired translucent surface with subtle tint and texture.",
                DeucarianThemeStyleSurfaceTreatment.FluentAcrylic,
                Hex("#D7E8FF"),
                Hex("#F7FBFF"),
                0.18f,
                0.72f,
                0.52f,
                0.78f,
                true,
                new Color(1f, 1f, 1f, 0.06f),
                48,
                3,
                0.86f),
            new DeucarianThemeSurfaceProfilePreset(
                "MaterialSurface.asset",
                DeucarianThemePresentationProfileIds.Surface.Material,
                "Material",
                "Solid opaque layered material surface.",
                DeucarianThemeStyleSurfaceTreatment.Material,
                Hex("#18202A"),
                Hex("#F7FAFC"),
                0.04f,
                1f,
                1f,
                1f,
                false,
                Color.clear,
                32,
                0,
                0f)
        };

        private static readonly DeucarianThemeShapeProfilePreset[] Shapes =
        {
            new DeucarianThemeShapeProfilePreset(
                "RoundedShape.asset",
                DeucarianThemePresentationProfileIds.Shape.Rounded,
                "Rounded",
                "Large rounded panel corners.",
                16f),
            new DeucarianThemeShapeProfilePreset(
                "SoftShape.asset",
                DeucarianThemePresentationProfileIds.Shape.Soft,
                "Soft",
                "Medium soft panel corners.",
                8f),
            new DeucarianThemeShapeProfilePreset(
                "TightShape.asset",
                DeucarianThemePresentationProfileIds.Shape.Tight,
                "Tight",
                "Restrained panel corners.",
                4f),
            new DeucarianThemeShapeProfilePreset(
                "SquareShape.asset",
                DeucarianThemePresentationProfileIds.Shape.Square,
                "Square",
                "Square panel and nested control corners.",
                0f)
        };

        private static readonly DeucarianThemeStrokeProfilePreset[] Strokes =
        {
            new DeucarianThemeStrokeProfilePreset(
                "FrostedStroke.asset",
                DeucarianThemePresentationProfileIds.Stroke.Frosted,
                "Frosted Highlight",
                "Soft highlighted glass border.",
                Color.white,
                0.58f,
                0.48f,
                1f),
            new DeucarianThemeStrokeProfilePreset(
                "AcrylicStroke.asset",
                DeucarianThemePresentationProfileIds.Stroke.Acrylic,
                "Acrylic Subtle",
                "Subtle acrylic edge highlight.",
                Color.white,
                0.42f,
                0.36f,
                1f),
            new DeucarianThemeStrokeProfilePreset(
                "MaterialStroke.asset",
                DeucarianThemePresentationProfileIds.Stroke.Material,
                "Material Crisp",
                "Restrained crisp material border.",
                Color.white,
                0.1f,
                0.18f,
                1f),
            new DeucarianThemeStrokeProfilePreset(
                "BorderlessStroke.asset",
                DeucarianThemePresentationProfileIds.Stroke.Borderless,
                "Borderless",
                "No visible panel border.",
                Color.clear,
                0f,
                0f,
                0f)
        };

        public static IReadOnlyList<DeucarianThemeSurfaceProfilePreset> BuiltinSurfaces => Surfaces;
        public static IReadOnlyList<DeucarianThemeShapeProfilePreset> BuiltinShapes => Shapes;
        public static IReadOnlyList<DeucarianThemeStrokeProfilePreset> BuiltinStrokes => Strokes;

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
