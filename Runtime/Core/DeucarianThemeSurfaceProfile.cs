using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Reusable surface tint, transparency, and generated texture treatment.</summary>
    [CreateAssetMenu(fileName = "Theme Surface Profile", menuName = "Deucarian/Theming/Theme Surface Profile")]
    public sealed class DeucarianThemeSurfaceProfile : ScriptableObject
    {
        [SerializeField] private string profileId = DeucarianThemePresentationProfileIds.Surface.FrostedGlass;
        [SerializeField] private string displayName = "Frosted Glass";
        [SerializeField] private string description = "Translucent frosted surface treatment.";
        [SerializeField] private DeucarianThemeStyleSurfaceTreatment surfaceTreatment =
            DeucarianThemeStyleSurfaceTreatment.FrostedGlass;
        [SerializeField] private Color darkSurfaceTint = new Color(0.78f, 0.9f, 1f, 1f);
        [SerializeField] private Color lightSurfaceTint = new Color(0.86f, 0.94f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float surfaceTintStrength = 0.24f;
        [SerializeField, Range(0f, 2f)] private float surfaceAlphaMultiplier = 0.62f;
        [SerializeField, Range(0f, 1f)] private float minimumSurfaceAlpha = 0.48f;
        [SerializeField, Range(0f, 1f)] private float maximumSurfaceAlpha = 0.68f;
        [SerializeField] private bool useGeneratedNoiseTexture = true;
        [SerializeField] private Color textureTint = new Color(1f, 1f, 1f, 0.08f);
        [SerializeField, Range(8, 128)] private int generatedTextureSize = 48;
        [SerializeField, Range(0, 12)] private int generatedTextureBlurRadius = 4;
        [SerializeField, Range(0f, 1f)] private float generatedTextureBlurStrength = 0.92f;

        [System.NonSerialized] private Texture2D generatedNoiseTexture;

        public string ProfileId => profileId;
        public string DisplayName => displayName;
        public string Description => description;
        public DeucarianThemeStyleSurfaceTreatment SurfaceTreatment => surfaceTreatment;
        public Color DarkSurfaceTint => darkSurfaceTint;
        public Color LightSurfaceTint => lightSurfaceTint;
        public float SurfaceTintStrength => surfaceTintStrength;
        public float SurfaceAlphaMultiplier => surfaceAlphaMultiplier;
        public float MinimumSurfaceAlpha => minimumSurfaceAlpha;
        public float MaximumSurfaceAlpha => maximumSurfaceAlpha;
        public bool UseGeneratedNoiseTexture => useGeneratedNoiseTexture;
        public Color TextureTint => textureTint;
        public int GeneratedTextureSize => generatedTextureSize;
        public int GeneratedTextureBlurRadius => generatedTextureBlurRadius;
        public float GeneratedTextureBlurStrength => generatedTextureBlurStrength;

        public void Configure(
            string id,
            string name,
            string profileDescription,
            DeucarianThemeStyleSurfaceTreatment treatment,
            Color darkTint,
            Color lightTint,
            float tintStrength,
            float alphaMultiplier,
            float minAlpha,
            float maxAlpha,
            bool useNoiseTexture,
            Color resolvedTextureTint,
            int textureSize,
            int textureBlurRadius,
            float textureBlurStrength)
        {
            profileId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            description = profileDescription ?? string.Empty;
            surfaceTreatment = treatment;
            darkSurfaceTint = darkTint;
            lightSurfaceTint = lightTint;
            surfaceTintStrength = tintStrength;
            surfaceAlphaMultiplier = alphaMultiplier;
            minimumSurfaceAlpha = minAlpha;
            maximumSurfaceAlpha = maxAlpha;
            useGeneratedNoiseTexture = useNoiseTexture;
            textureTint = resolvedTextureTint;
            generatedTextureSize = textureSize;
            generatedTextureBlurRadius = textureBlurRadius;
            generatedTextureBlurStrength = textureBlurStrength;
            generatedNoiseTexture = null;
            Sanitize();
            NotifyChanged();
        }

        public Color ResolveSurfaceColor(Color baseColor)
        {
            float luminance = baseColor.r * 0.2126f + baseColor.g * 0.7152f + baseColor.b * 0.0722f;
            Color tint = luminance > 0.5f ? lightSurfaceTint : darkSurfaceTint;
            Color surface = Color.Lerp(baseColor, tint, surfaceTintStrength);
            surface.a = Mathf.Clamp(baseColor.a * surfaceAlphaMultiplier, minimumSurfaceAlpha, maximumSurfaceAlpha);
            return surface;
        }

        public Texture2D GetGeneratedTexture()
        {
            if (!useGeneratedNoiseTexture)
            {
                return null;
            }

            if (generatedNoiseTexture == null)
            {
                generatedNoiseTexture = CreateGeneratedTexture(
                    displayName,
                    generatedTextureSize,
                    generatedTextureBlurRadius,
                    generatedTextureBlurStrength);
            }

            return generatedNoiseTexture;
        }

        internal static Texture2D CreateGeneratedTexture(string name, int requestedSize, int blurRadius, float blurStrength)
        {
            int size = Mathf.Clamp(requestedSize, 8, 128);
            float[] rawValues = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hash = (x * 73856093) ^ (y * 19349663);
                    rawValues[y * size + x] = (hash & 255) / 255f;
                }
            }

            float[] blurredValues = BlurWrapped(rawValues, size, blurRadius);
            float safeBlurStrength = Mathf.Clamp01(blurStrength);
            Color32[] pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                float value = Mathf.Lerp(rawValues[i], blurredValues[i], safeBlurStrength);
                byte channel = (byte)Mathf.Clamp(Mathf.RoundToInt(220f + value * 35f), 0, 255);
                pixels[i] = new Color32(channel, channel, channel, 255);
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = (name ?? string.Empty) + " Texture",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private void OnValidate()
        {
            Sanitize();
            generatedNoiseTexture = null;
            NotifyChanged();
        }

        private void Sanitize()
        {
            profileId = DeucarianColorRole.NormalizeId(profileId);
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

            int safeRadius = Mathf.Clamp(radius, 0, 12);
            float[] horizontal = new float[source.Length];
            float[] result = new float[source.Length];
            int sampleCount = safeRadius * 2 + 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int offset = -safeRadius; offset <= safeRadius; offset++)
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
                    for (int offset = -safeRadius; offset <= safeRadius; offset++)
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
