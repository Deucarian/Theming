using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.UIToolkit
{
    /// <summary>
    /// Adapts typography from a Deucarian visual style to UI Toolkit's Unity font property.
    /// </summary>
    public static class DeucarianUIToolkitThemeTypography
    {
        /// <summary>
        /// Applies the resolved typography font to an element. A nearby theme provider's
        /// style override takes precedence over the supplied theme's visual style.
        /// </summary>
        public static bool Apply(
            VisualElement element,
            DeucarianTheme theme,
            Component context = null)
        {
            DeucarianThemeProvider provider = context != null
                ? DeucarianThemeRuntimeResolver.FindProvider(context)
                : null;
            DeucarianThemeStyle style = provider != null && provider.CurrentStyle != null
                ? provider.CurrentStyle
                : theme != null
                    ? theme.VisualStyle
                    : null;
            return ApplyStyle(element, style);
        }

        /// <summary>Applies a visual style's resolved typography font to an element.</summary>
        public static bool ApplyStyle(
            VisualElement element,
            DeucarianThemeStyle style)
        {
            Font font = ResolveFont(style);
            if (element == null || font == null)
            {
                return false;
            }

            element.style.unityFont = new StyleFont(font);
            return true;
        }

        /// <summary>
        /// Resolves the legacy Unity font backing a style's TextMesh Pro typography asset.
        /// </summary>
        public static Font ResolveFont(DeucarianThemeStyle style)
        {
            DeucarianThemeTypographyProfile typography = style != null
                ? style.TypographyProfile
                : null;
            TMP_FontAsset fontAsset = typography != null
                ? typography.ResolvedFontAsset
                : null;
            return fontAsset != null ? fontAsset.sourceFontFile : null;
        }
    }
}
