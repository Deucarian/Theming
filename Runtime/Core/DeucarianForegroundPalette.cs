using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Exact palette colours available when a preferred foreground needs more contrast.</summary>
    public readonly struct DeucarianForegroundPalette
    {
        public DeucarianForegroundPalette(Color first, Color second)
        {
            bool firstIsDark = DeucarianForegroundContrast.Luminance(first) <=
                               DeucarianForegroundContrast.Luminance(second);
            Dark = firstIsDark ? first : second;
            Light = firstIsDark ? second : first;
        }

        public Color Dark { get; }
        public Color Light { get; }

        /// <summary>
        /// Reuses the normal control surface and text colour. Optional roles in the same
        /// palette override either end; absent roles continue to follow the existing colours.
        /// </summary>
        public static DeucarianForegroundPalette FromTheme(
            DeucarianTheme theme, Color normalSurface, Color normalText)
        {
            var inherited = new DeucarianForegroundPalette(normalSurface, normalText);
            Color dark = theme != null && theme.TryGetColorById(
                DeucarianControlColorRoleIds.ForegroundDark, out Color darkOverride)
                ? darkOverride : inherited.Dark;
            Color light = theme != null && theme.TryGetColorById(
                DeucarianControlColorRoleIds.ForegroundLight, out Color lightOverride)
                ? lightOverride : inherited.Light;
            return new DeucarianForegroundPalette(dark, light);
        }
    }
}
