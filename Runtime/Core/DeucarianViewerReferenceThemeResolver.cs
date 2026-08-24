using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Consumer-neutral lookup helpers for the canonical reference viewer theme.
    /// </summary>
    public static class DeucarianViewerReferenceThemeResolver
    {
        /// <summary>Finds the nearest provider, then the active provider.</summary>
        public static DeucarianThemeProvider FindProvider(Component context)
        {
            return DeucarianThemeRuntimeResolver.FindProvider(context);
        }

        /// <summary>
        /// Resolves the provider theme, falling back to the package-owned reference theme.
        /// </summary>
        public static DeucarianTheme ResolveTheme(Component context)
        {
            DeucarianThemeProvider provider = FindProvider(context);
            return provider != null && provider.CurrentTheme != null
                ? provider.CurrentTheme
                : DeucarianViewerReferenceThemePreset.Resolve().DefaultTheme;
        }

        /// <summary>
        /// Assigns the reference family when a provider has no usable theme.
        /// </summary>
        public static bool EnsureProviderHasTheme(
            DeucarianThemeProvider provider)
        {
            if (provider == null)
            {
                return false;
            }

            if (provider.CurrentTheme != null)
            {
                return true;
            }

            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();
            provider.SetThemeFamily(
                profile.ThemeFamily,
                DeucarianViewerReferenceThemePreset.DefaultMode);
            return provider.CurrentTheme != null;
        }

        /// <summary>
        /// Resolves a semantic color and uses the matching reference-mode color
        /// when a custom theme omits that role.
        /// </summary>
        public static Color ResolveColor(
            DeucarianTheme theme,
            string roleId)
        {
            if (theme != null &&
                theme.TryGetColorById(roleId, out Color color) &&
                !IsMissingColor(color))
            {
                return color;
            }

            DeucarianThemeMode fallbackMode = ResolveMode(theme);
            DeucarianTheme fallbackTheme =
                DeucarianViewerReferenceThemePreset.Resolve()
                    .ResolveTheme(fallbackMode);
            if (fallbackTheme != null &&
                fallbackTheme.TryGetColorById(roleId, out color) &&
                !IsMissingColor(color))
            {
                return color;
            }

            return DeucarianColorPalette.MissingColor;
        }

        /// <summary>Returns whether a color is the package missing-role sentinel.</summary>
        public static bool IsMissingColor(Color color)
        {
            Color missing = DeucarianColorPalette.MissingColor;
            return Mathf.Approximately(color.r, missing.r) &&
                   Mathf.Approximately(color.g, missing.g) &&
                   Mathf.Approximately(color.b, missing.b) &&
                   Mathf.Approximately(color.a, missing.a);
        }

        private static DeucarianThemeMode ResolveMode(DeucarianTheme theme)
        {
            DeucarianColorPalette palette =
                theme != null ? theme.ColorPalette : null;
            return palette != null &&
                   palette.HasThemeMode &&
                   palette.ThemeMode == DeucarianThemeMode.Light
                ? DeucarianThemeMode.Light
                : DeucarianViewerReferenceThemePreset.DefaultMode;
        }
    }
}
