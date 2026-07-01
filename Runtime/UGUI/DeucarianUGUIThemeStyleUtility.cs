using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Theming
{
    /// <summary>
    /// Helpers for applying Deucarian visual styles to uGUI surfaces.
    /// </summary>
    public static class DeucarianUGUIThemeStyleUtility
    {
        /// <summary>
        /// Applies a panel-like visual style to a Graphic using a theme role as the base surface color.
        /// </summary>
        public static bool ApplyPanel(
            Graphic graphic,
            DeucarianTheme theme,
            DeucarianThemeStyle style = null,
            string surfaceRoleId = DeucarianBuiltinColorRoleIds.Core.SurfaceRaised)
        {
            Color baseColor = ResolveBaseColor(theme, surfaceRoleId);
            return ApplyPanel(graphic, baseColor, ResolveStyle(theme, style));
        }

        /// <summary>Applies a panel-like visual style to a Graphic using an explicit base color.</summary>
        public static bool ApplyPanel(Graphic graphic, Color baseColor, DeucarianThemeStyle style)
        {
            if (graphic == null || style == null)
            {
                return false;
            }

            Color surface = style.ResolveSurfaceColor(baseColor);
            graphic.color = surface;
            return true;
        }

        /// <summary>Applies the style border as a uGUI outline/rim.</summary>
        public static bool ApplyOutline(Outline outline, Color surfaceColor, DeucarianThemeStyle style)
        {
            if (outline == null || style == null)
            {
                return false;
            }

            outline.effectColor = style.ResolveBorderColor(surfaceColor);
            outline.effectDistance = new Vector2(style.BorderWidth, -style.BorderWidth);
            outline.useGraphicAlpha = false;
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
