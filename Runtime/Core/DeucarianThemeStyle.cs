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

        /// <summary>Broad surface treatment used by this style.</summary>
        public DeucarianThemeStyleSurfaceTreatment SurfaceTreatment => surfaceTreatment;

        /// <summary>Tint used when the source color is visually dark.</summary>
        public Color DarkSurfaceTint => darkSurfaceTint;

        /// <summary>Tint used when the source color is visually light.</summary>
        public Color LightSurfaceTint => lightSurfaceTint;

        /// <summary>Blend factor between source color and the selected style tint.</summary>
        public float SurfaceTintStrength => surfaceTintStrength;

        /// <summary>Multiplier applied to source alpha before clamping.</summary>
        public float SurfaceAlphaMultiplier => surfaceAlphaMultiplier;

        /// <summary>Minimum resolved surface alpha.</summary>
        public float MinimumSurfaceAlpha => minimumSurfaceAlpha;

        /// <summary>Maximum resolved surface alpha.</summary>
        public float MaximumSurfaceAlpha => maximumSurfaceAlpha;

        /// <summary>Tint used when resolving borders.</summary>
        public Color BorderTint => borderTint;

        /// <summary>Blend factor between surface color and border tint.</summary>
        public float BorderTintStrength => borderTintStrength;

        /// <summary>Resolved border alpha.</summary>
        public float BorderAlpha => borderAlpha;

        /// <summary>Recommended border width for panel-like surfaces.</summary>
        public float BorderWidth => borderWidth;

        /// <summary>Recommended corner radius for panel-like surfaces.</summary>
        public float CornerRadius => cornerRadius;

        /// <summary>Whether utilities should add the generated fine texture.</summary>
        public bool UseGeneratedNoiseTexture => useGeneratedNoiseTexture;

        /// <summary>Tint applied to the generated texture when one is used.</summary>
        public Color TextureTint => textureTint;

        /// <summary>Generated texture size in pixels.</summary>
        public int GeneratedTextureSize => generatedTextureSize;

        /// <summary>Blur radius used by the generated frosted texture.</summary>
        public int GeneratedTextureBlurRadius => generatedTextureBlurRadius;

        /// <summary>Blend strength between raw grain and blurred frosted texture.</summary>
        public float GeneratedTextureBlurStrength => generatedTextureBlurStrength;

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

        /// <summary>Resolves a panel-like surface color from a palette/role color.</summary>
        public Color ResolveSurfaceColor(Color baseColor)
        {
            Color tint = ResolveSurfaceTint(baseColor);
            Color surface = Color.Lerp(baseColor, tint, surfaceTintStrength);
            surface.a = Mathf.Clamp(baseColor.a * surfaceAlphaMultiplier, minimumSurfaceAlpha, maximumSurfaceAlpha);
            return surface;
        }

        /// <summary>Resolves a border color from a surface color.</summary>
        public Color ResolveBorderColor(Color surfaceColor)
        {
            Color border = Color.Lerp(surfaceColor, borderTint, borderTintStrength);
            border.a = borderAlpha;
            return border;
        }

        /// <summary>Returns the generated frosted style texture, or null when this style does not use one.</summary>
        public Texture2D GetGeneratedTexture()
        {
            if (!useGeneratedNoiseTexture)
            {
                return null;
            }

            if (generatedNoiseTexture != null)
            {
                return generatedNoiseTexture;
            }

            int size = Mathf.Clamp(generatedTextureSize, 8, 128);
            float[] rawValues = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hash = (x * 73856093) ^ (y * 19349663);
                    rawValues[y * size + x] = (hash & 255) / 255f;
                }
            }

            float[] blurredValues = BlurWrapped(rawValues, size, generatedTextureBlurRadius);
            float blurStrength = Mathf.Clamp01(generatedTextureBlurStrength);
            Color32[] pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                float value = Mathf.Lerp(rawValues[i], blurredValues[i], blurStrength);
                byte channel = (byte)Mathf.Clamp(Mathf.RoundToInt(220f + value * 35f), 0, 255);
                pixels[i] = new Color32(channel, channel, channel, 255);
            }

            generatedNoiseTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = displayName + " Texture",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            generatedNoiseTexture.SetPixels32(pixels);
            generatedNoiseTexture.Apply(false, false);
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
        }

        private static float[] BlurWrapped(float[] source, int size, int radius)
        {
            if (source == null || source.Length == 0 || radius <= 0)
            {
                return source;
            }

            float[] horizontal = new float[source.Length];
            float[] result = new float[source.Length];
            int sampleCount = radius * 2 + 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int wrappedX = WrapIndex(x + offset, size);
                        sum += source[y * size + wrappedX];
                    }

                    horizontal[y * size + x] = sum / sampleCount;
                }
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int wrappedY = WrapIndex(y + offset, size);
                        sum += horizontal[wrappedY * size + x];
                    }

                    result[y * size + x] = sum / sampleCount;
                }
            }

            return result;
        }

        private static int WrapIndex(int value, int size)
        {
            int wrapped = value % size;
            return wrapped < 0 ? wrapped + size : wrapped;
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
