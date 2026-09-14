using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Foreground policy for sRGB UI colours. Callers supply the actual opaque surface.</summary>
    public static class DeucarianForegroundContrast
    {
        public const float TextMinimum = 4.5f;
        public const float IconMinimum = 3f;

        public static Color Composite(Color foreground, Color background)
        {
            float alpha = Mathf.Clamp01(foreground.a);
            return new Color(
                foreground.r * alpha + background.r * (1f - alpha),
                foreground.g * alpha + background.g * (1f - alpha),
                foreground.b * alpha + background.b * (1f - alpha), 1f);
        }

        public static float Ratio(Color foreground, Color surface)
        {
            float first = Luminance(Composite(foreground, surface));
            float second = Luminance(surface);
            return (Mathf.Max(first, second) + 0.05f) / (Mathf.Min(first, second) + 0.05f);
        }

        public static Color Resolve(Color preferred, Color surface, float minimum = TextMinimum)
        {
            if (Ratio(preferred, surface) >= minimum) return preferred;
            return Ratio(Color.black, surface) >= Ratio(Color.white, surface)
                ? Color.black : Color.white;
        }

        private static float Luminance(Color color) =>
            Linear(color.r) * 0.2126f + Linear(color.g) * 0.7152f + Linear(color.b) * 0.0722f;

        private static float Linear(float channel)
        {
            float value = Mathf.Clamp01(channel);
            return value <= 0.04045f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }
    }
}
