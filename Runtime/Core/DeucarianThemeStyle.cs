using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Visual style asset for theme chrome such as surfaces, borders, corner radius, and optional texture tint.
    /// Color palettes still own semantic color meaning; styles describe how surfaces are treated.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme Style", menuName = "Deucarian/Theming/Theme Style")]
    public sealed class DeucarianThemeStyle : ScriptableObject
    {
        [SerializeField] private string styleId = DeucarianThemeStyleIds.FrostedGlass;
        [SerializeField] private string displayName = "Frosted Glass";
        [SerializeField] private string description = "Translucent glass-like surfaces with cool tinting, fine texture, and soft borders.";
        [SerializeField] private DeucarianThemeSurfaceProfile surfaceProfile;
        [SerializeField] private DeucarianThemeShapeProfile shapeProfile;
        [SerializeField] private DeucarianThemeStrokeProfile strokeProfile;
        [SerializeField] private DeucarianThemeTypographyProfile typographyProfile;
        [SerializeField] private DeucarianThemeDensity density = DeucarianThemeDensity.Unspecified;
        [SerializeField] private bool isVariant;
        [SerializeField] private DeucarianThemeStyleSurfaceTreatment surfaceTreatment =
            DeucarianThemeStyleSurfaceTreatment.FrostedGlass;
        [SerializeField] private Color darkSurfaceTint = new Color(0.78f, 0.9f, 1f, 1f);
        [SerializeField] private Color lightSurfaceTint = new Color(0.86f, 0.94f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float surfaceTintStrength = 0.24f;
        [SerializeField, Range(0f, 2f)] private float surfaceAlphaMultiplier = 0.62f;
        [SerializeField, Range(0f, 1f)] private float minimumSurfaceAlpha = 0.48f;
        [SerializeField, Range(0f, 1f)] private float maximumSurfaceAlpha = 0.68f;
        [SerializeField] private Color borderTint = Color.white;
        [SerializeField, Range(0f, 1f)] private float borderTintStrength = 0.58f;
        [SerializeField, Range(0f, 1f)] private float borderAlpha = 0.48f;
        [SerializeField, Min(0f)] private float borderWidth = 1f;
        [SerializeField, Min(0f)] private float cornerRadius = 16f;
        [SerializeField] private bool useGeneratedNoiseTexture = true;
        [SerializeField] private Color textureTint = new Color(1f, 1f, 1f, 0.08f);
        [SerializeField, Range(8, 128)] private int generatedTextureSize = 32;
        [SerializeField, Range(0, 12)] private int generatedTextureBlurRadius = 3;
        [SerializeField, Range(0f, 1f)] private float generatedTextureBlurStrength = 0.85f;

        [System.NonSerialized] private Texture2D generatedNoiseTexture;

        /// <summary>Stable style identifier.</summary>
        public string StyleId => styleId;

        /// <summary>Human-readable style name.</summary>
        public string DisplayName => displayName;

        /// <summary>Short description of the intended visual language.</summary>
        public string Description => description;

        /// <summary>Reusable surface component, or null for a legacy inline style.</summary>
        public DeucarianThemeSurfaceProfile SurfaceProfile => surfaceProfile;

        /// <summary>Reusable shape component, or null for a legacy inline style.</summary>
        public DeucarianThemeShapeProfile ShapeProfile => shapeProfile;

        /// <summary>Reusable stroke component, or null for a legacy inline style.</summary>
        public DeucarianThemeStrokeProfile StrokeProfile => strokeProfile;

        /// <summary>Optional TMP typography component. Null resolves through project TMP defaults.</summary>
        public DeucarianThemeTypographyProfile TypographyProfile => typographyProfile;

        /// <summary>Explicit semantic density, or Unspecified for legacy style-ID resolution.</summary>
        public DeucarianThemeDensity Density => density;

        /// <summary>True when the style was explicitly created as a reusable project custom style.</summary>
        public bool IsCustomStyle => isVariant;

        /// <summary>
        /// Backward-compatible alias for <see cref="IsCustomStyle"/>.
        /// Existing serialized assets continue to use the original isVariant field.
        /// </summary>
        public bool IsVariant => isVariant;

        /// <summary>True when all reusable presentation components and a density are assigned.</summary>
        public bool IsComposed => surfaceProfile != null
                                  && shapeProfile != null
                                  && strokeProfile != null
                                  && density != DeucarianThemeDensity.Unspecified;

        /// <summary>Identifies whether the style is legacy, complete, or only partially composed.</summary>
        public DeucarianThemeStyleCompositionKind CompositionKind
        {
            get
            {
                bool hasAnyCompositionValue = surfaceProfile != null
                                              || shapeProfile != null
                                              || strokeProfile != null
                                              || typographyProfile != null
                                              || density != DeucarianThemeDensity.Unspecified;
                if (!hasAnyCompositionValue)
                {
                    return DeucarianThemeStyleCompositionKind.LegacyInline;
                }

                if (!IsComposed)
                {
                    return DeucarianThemeStyleCompositionKind.Incomplete;
                }

                return isVariant
                    ? DeucarianThemeStyleCompositionKind.CompleteCustom
                    : DeucarianThemeStyleCompositionKind.CompletePreset;
            }
        }

        /// <summary>Broad surface treatment used by this style.</summary>
        public DeucarianThemeStyleSurfaceTreatment SurfaceTreatment =>
            surfaceProfile != null ? surfaceProfile.SurfaceTreatment : surfaceTreatment;

        /// <summary>Tint used when the source color is visually dark.</summary>
        public Color DarkSurfaceTint => surfaceProfile != null ? surfaceProfile.DarkSurfaceTint : darkSurfaceTint;

        /// <summary>Tint used when the source color is visually light.</summary>
        public Color LightSurfaceTint => surfaceProfile != null ? surfaceProfile.LightSurfaceTint : lightSurfaceTint;

        /// <summary>Blend factor between source color and the selected style tint.</summary>
        public float SurfaceTintStrength =>
            surfaceProfile != null ? surfaceProfile.SurfaceTintStrength : surfaceTintStrength;

        /// <summary>Multiplier applied to source alpha before clamping.</summary>
        public float SurfaceAlphaMultiplier =>
            surfaceProfile != null ? surfaceProfile.SurfaceAlphaMultiplier : surfaceAlphaMultiplier;

        /// <summary>Minimum resolved surface alpha.</summary>
        public float MinimumSurfaceAlpha =>
            surfaceProfile != null ? surfaceProfile.MinimumSurfaceAlpha : minimumSurfaceAlpha;

        /// <summary>Maximum resolved surface alpha.</summary>
        public float MaximumSurfaceAlpha =>
            surfaceProfile != null ? surfaceProfile.MaximumSurfaceAlpha : maximumSurfaceAlpha;

        /// <summary>Tint used when resolving borders.</summary>
        public Color BorderTint => strokeProfile != null ? strokeProfile.BorderTint : borderTint;

        /// <summary>Blend factor between surface color and border tint.</summary>
        public float BorderTintStrength =>
            strokeProfile != null ? strokeProfile.BorderTintStrength : borderTintStrength;

        /// <summary>Resolved border alpha.</summary>
        public float BorderAlpha => strokeProfile != null ? strokeProfile.BorderAlpha : borderAlpha;

        /// <summary>Recommended border width for panel-like surfaces.</summary>
        public float BorderWidth => strokeProfile != null ? strokeProfile.BorderWidth : borderWidth;

        /// <summary>Recommended corner radius for panel-like surfaces.</summary>
        public float CornerRadius => shapeProfile != null ? shapeProfile.CornerRadius : cornerRadius;

        /// <summary>Whether utilities should add the generated fine texture.</summary>
        public bool UseGeneratedNoiseTexture =>
            surfaceProfile != null ? surfaceProfile.UseGeneratedNoiseTexture : useGeneratedNoiseTexture;

        /// <summary>Tint applied to the generated texture when one is used.</summary>
        public Color TextureTint => surfaceProfile != null ? surfaceProfile.TextureTint : textureTint;

        /// <summary>Generated texture size in pixels.</summary>
        public int GeneratedTextureSize =>
            surfaceProfile != null ? surfaceProfile.GeneratedTextureSize : generatedTextureSize;

        /// <summary>Blur radius used by the generated frosted texture.</summary>
        public int GeneratedTextureBlurRadius =>
            surfaceProfile != null ? surfaceProfile.GeneratedTextureBlurRadius : generatedTextureBlurRadius;

        /// <summary>Blend strength between raw grain and blurred frosted texture.</summary>
        public float GeneratedTextureBlurStrength =>
            surfaceProfile != null ? surfaceProfile.GeneratedTextureBlurStrength : generatedTextureBlurStrength;

        /// <summary>Configures all style fields for editor preset creation and tests.</summary>
        public void Configure(
            string id,
            string name,
            string styleDescription,
            DeucarianThemeStyleSurfaceTreatment treatment,
            Color darkTint,
            Color lightTint,
            float tintStrength,
            float alphaMultiplier,
            float minAlpha,
            float maxAlpha,
            Color resolvedBorderTint,
            float resolvedBorderTintStrength,
            float resolvedBorderAlpha,
            float resolvedBorderWidth,
            float resolvedCornerRadius,
            bool useNoiseTexture,
            Color resolvedTextureTint,
            int textureSize)
        {
            Configure(
                id,
                name,
                styleDescription,
                treatment,
                darkTint,
                lightTint,
                tintStrength,
                alphaMultiplier,
                minAlpha,
                maxAlpha,
                resolvedBorderTint,
                resolvedBorderTintStrength,
                resolvedBorderAlpha,
                resolvedBorderWidth,
                resolvedCornerRadius,
                useNoiseTexture,
                resolvedTextureTint,
                textureSize,
                3,
                0.85f);
        }

        /// <summary>Configures all style fields, including generated frosted texture blur controls.</summary>
        public void Configure(
            string id,
            string name,
            string styleDescription,
            DeucarianThemeStyleSurfaceTreatment treatment,
            Color darkTint,
            Color lightTint,
            float tintStrength,
            float alphaMultiplier,
            float minAlpha,
            float maxAlpha,
            Color resolvedBorderTint,
            float resolvedBorderTintStrength,
            float resolvedBorderAlpha,
            float resolvedBorderWidth,
            float resolvedCornerRadius,
            bool useNoiseTexture,
            Color resolvedTextureTint,
            int textureSize,
            int textureBlurRadius,
            float textureBlurStrength)
        {
            styleId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            description = styleDescription ?? string.Empty;
            surfaceProfile = null;
            shapeProfile = null;
            strokeProfile = null;
            typographyProfile = null;
            density = DeucarianThemeDensity.Unspecified;
            isVariant = false;
            surfaceTreatment = treatment;
            darkSurfaceTint = darkTint;
            lightSurfaceTint = lightTint;
            surfaceTintStrength = tintStrength;
            surfaceAlphaMultiplier = alphaMultiplier;
            minimumSurfaceAlpha = minAlpha;
            maximumSurfaceAlpha = maxAlpha;
            borderTint = resolvedBorderTint;
            borderTintStrength = resolvedBorderTintStrength;
            borderAlpha = resolvedBorderAlpha;
            borderWidth = resolvedBorderWidth;
            cornerRadius = resolvedCornerRadius;
            useGeneratedNoiseTexture = useNoiseTexture;
            textureTint = resolvedTextureTint;
            generatedTextureSize = textureSize;
            generatedTextureBlurRadius = textureBlurRadius;
            generatedTextureBlurStrength = textureBlurStrength;
            generatedNoiseTexture = null;
            Sanitize();
            NotifyChanged();
        }

        /// <summary>
        /// Assigns reusable presentation components while retaining the inline values as a legacy fallback.
        /// </summary>
        public void SetComposition(
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile shape,
            DeucarianThemeStrokeProfile stroke,
            DeucarianThemeDensity resolvedDensity,
            bool variant = false)
        {
            SetComposition(
                surface,
                shape,
                stroke,
                resolvedDensity,
                typographyProfile,
                variant);
        }

        /// <summary>Assigns all reusable presentation components, including optional TMP typography.</summary>
        public void SetComposition(
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile shape,
            DeucarianThemeStrokeProfile stroke,
            DeucarianThemeDensity resolvedDensity,
            DeucarianThemeTypographyProfile typography,
            bool variant = false)
        {
            surfaceProfile = surface;
            shapeProfile = shape;
            strokeProfile = stroke;
            typographyProfile = typography;
            density = NormalizeDensity(resolvedDensity);
            isVariant = variant;
            generatedNoiseTexture = null;
            NotifyChanged();
        }

        /// <summary>Configures metadata for a project-authored custom style.</summary>
        public void SetCustomStyleMetadata(string id, string name, string styleDescription)
        {
            styleId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            description = styleDescription ?? string.Empty;
            isVariant = true;
            NotifyChanged();
        }

        /// <summary>Backward-compatible alias for <see cref="SetCustomStyleMetadata"/>.</summary>
        public void SetVariantMetadata(string id, string name, string styleDescription)
        {
            SetCustomStyleMetadata(id, name, styleDescription);
        }

        /// <summary>Returns true when this style directly references the supplied component asset.</summary>
        public bool UsesComponentAsset(Object asset)
        {
            return asset != null
                   && (asset == surfaceProfile
                       || asset == shapeProfile
                       || asset == strokeProfile
                       || asset == typographyProfile);
        }

        /// <summary>Resolves a panel-like surface color from a palette/role color.</summary>
        public Color ResolveSurfaceColor(Color baseColor)
        {
            if (surfaceProfile != null)
            {
                return surfaceProfile.ResolveSurfaceColor(baseColor);
            }

            Color tint = ResolveSurfaceTint(baseColor);
            Color surface = Color.Lerp(baseColor, tint, surfaceTintStrength);
            surface.a = Mathf.Clamp(baseColor.a * surfaceAlphaMultiplier, minimumSurfaceAlpha, maximumSurfaceAlpha);
            return surface;
        }

        /// <summary>Resolves a border color from a surface color.</summary>
        public Color ResolveBorderColor(Color surfaceColor)
        {
            if (strokeProfile != null)
            {
                return strokeProfile.ResolveBorderColor(surfaceColor);
            }

            Color border = Color.Lerp(surfaceColor, borderTint, borderTintStrength);
            border.a = borderAlpha;
            return border;
        }

        /// <summary>Returns the generated frosted style texture, or null when this style does not use one.</summary>
        public Texture2D GetGeneratedTexture()
        {
            if (surfaceProfile != null)
            {
                return surfaceProfile.GetGeneratedTexture();
            }

            if (!useGeneratedNoiseTexture)
            {
                return null;
            }

            if (generatedNoiseTexture != null)
            {
                return generatedNoiseTexture;
            }

            generatedNoiseTexture = DeucarianThemeSurfaceProfile.CreateGeneratedTexture(
                displayName,
                generatedTextureSize,
                generatedTextureBlurRadius,
                generatedTextureBlurStrength);
            return generatedNoiseTexture;
        }

        private Color ResolveSurfaceTint(Color baseColor)
        {
            float luminance = baseColor.r * 0.2126f + baseColor.g * 0.7152f + baseColor.b * 0.0722f;
            return luminance > 0.5f ? lightSurfaceTint : darkSurfaceTint;
        }

        private void OnValidate()
        {
            Sanitize();
            generatedNoiseTexture = null;
            NotifyChanged();
        }

        private void Sanitize()
        {
            styleId = DeucarianColorRole.NormalizeId(styleId);
            displayName = displayName ?? string.Empty;
            description = description ?? string.Empty;
            surfaceTintStrength = Mathf.Clamp01(surfaceTintStrength);
            surfaceAlphaMultiplier = Mathf.Clamp(surfaceAlphaMultiplier, 0f, 2f);
            minimumSurfaceAlpha = Mathf.Clamp01(minimumSurfaceAlpha);
            maximumSurfaceAlpha = Mathf.Clamp01(maximumSurfaceAlpha);
            if (maximumSurfaceAlpha < minimumSurfaceAlpha)
            {
                maximumSurfaceAlpha = minimumSurfaceAlpha;
            }

            borderTintStrength = Mathf.Clamp01(borderTintStrength);
            borderAlpha = Mathf.Clamp01(borderAlpha);
            borderWidth = Mathf.Max(0f, borderWidth);
            cornerRadius = Mathf.Max(0f, cornerRadius);
            generatedTextureSize = Mathf.Clamp(generatedTextureSize, 8, 128);
            generatedTextureBlurRadius = Mathf.Clamp(generatedTextureBlurRadius, 0, 12);
            generatedTextureBlurStrength = Mathf.Clamp01(generatedTextureBlurStrength);
            density = NormalizeDensity(density);
        }

        private static DeucarianThemeDensity NormalizeDensity(DeucarianThemeDensity value)
        {
            return value == DeucarianThemeDensity.Comfortable
                   || value == DeucarianThemeDensity.Standard
                   || value == DeucarianThemeDensity.Compact
                ? value
                : DeucarianThemeDensity.Unspecified;
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
