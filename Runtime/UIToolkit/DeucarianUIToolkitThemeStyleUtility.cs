using Deucarian.Theming;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.UIToolkit
{
    /// <summary>
    /// Helpers for applying Deucarian visual styles to UI Toolkit elements.
    /// </summary>
    public static class DeucarianUIToolkitThemeStyleUtility
    {
        /// <summary>
        /// Applies a panel-like visual style to an element using a theme role as the base surface color.
        /// </summary>
        public static bool ApplyPanel(
            VisualElement element,
            DeucarianTheme theme,
            DeucarianThemeStyle style = null,
            string surfaceRoleId = DeucarianBuiltinColorRoleIds.Core.SurfaceRaised)
        {
            Color baseColor = ResolveBaseColor(theme, surfaceRoleId);
            return ApplyPanel(element, baseColor, ResolveStyle(theme, style));
        }

        /// <summary>Applies a panel-like visual style to an element using an explicit base color.</summary>
        public static bool ApplyPanel(VisualElement element, Color baseColor, DeucarianThemeStyle style)
        {
            if (element == null || style == null)
            {
                return false;
            }

            Color surface = style.ResolveSurfaceColor(baseColor);
            element.style.backgroundColor = surface;
            element.style.borderLeftWidth = style.BorderWidth;
            element.style.borderRightWidth = style.BorderWidth;
            element.style.borderTopWidth = style.BorderWidth;
            element.style.borderBottomWidth = style.BorderWidth;

            Color border = style.ResolveBorderColor(surface);
            element.style.borderLeftColor = border;
            element.style.borderRightColor = border;
            element.style.borderTopColor = border;
            element.style.borderBottomColor = border;
            element.style.borderTopLeftRadius = style.CornerRadius;
            element.style.borderTopRightRadius = style.CornerRadius;
            element.style.borderBottomLeftRadius = style.CornerRadius;
            element.style.borderBottomRightRadius = style.CornerRadius;

            Texture2D texture = style.GetGeneratedTexture();
            element.style.backgroundImage = texture != null ? new StyleBackground(texture) : StyleKeyword.Null;
            element.style.unityBackgroundImageTintColor = texture != null ? style.TextureTint : StyleKeyword.Null;
            return true;
        }

        /// <summary>Returns the supplied style, then theme style, then null.</summary>
        public static DeucarianThemeStyle ResolveStyle(DeucarianTheme theme, DeucarianThemeStyle style)
        {
            return style != null ? style : theme != null ? theme.VisualStyle : null;
        }

        private static Color ResolveBaseColor(DeucarianTheme theme, string surfaceRoleId)
        {
            if (theme != null && theme.TryGetColorById(surfaceRoleId, out Color color))
            {
                return color;
            }

            return new Color(0.12f, 0.16f, 0.21f, 1f);
        }
    }
}
