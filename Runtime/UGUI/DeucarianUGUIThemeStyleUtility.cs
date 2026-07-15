using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Theming
{
    /// <summary>
    /// Helpers for applying Deucarian visual styles to uGUI surfaces.
    /// </summary>
    public static class DeucarianUGUIThemeStyleUtility
    {
        private static readonly Dictionary<Texture2D, Sprite> GeneratedTextureSprites = new Dictionary<Texture2D, Sprite>();
        private static readonly HashSet<Sprite> GeneratedSprites = new HashSet<Sprite>();

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

        /// <summary>
        /// Applies a panel-like visual style and generated frosted texture to a uGUI Image.
        /// </summary>
        public static bool ApplyPanelImage(
            Image image,
            DeucarianTheme theme,
            DeucarianThemeStyle style = null,
            string surfaceRoleId = DeucarianBuiltinColorRoleIds.Core.SurfaceRaised)
        {
            Color baseColor = ResolveBaseColor(theme, surfaceRoleId);
            return ApplyPanelImage(image, baseColor, ResolveStyle(theme, style));
        }

        /// <summary>Applies a panel-like visual style and generated texture to a uGUI Image.</summary>
        public static bool ApplyPanelImage(Image image, Color baseColor, DeucarianThemeStyle style)
        {
            if (image == null || style == null || !ApplyPanel(image, baseColor, style))
            {
                return false;
            }

            Texture2D texture = style.GetGeneratedTexture();
            if (texture == null)
            {
                if (image.sprite != null && GeneratedSprites.Contains(image.sprite))
                {
                    image.sprite = null;
                }

                return true;
            }

            image.sprite = GetOrCreateGeneratedSprite(texture);
            image.type = Image.Type.Tiled;
            image.preserveAspect = false;
            return true;
        }

        /// <summary>Applies the style border as a uGUI outline/rim.</summary>
        public static bool ApplyOutline(Outline outline, Color surfaceColor, DeucarianThemeStyle style)
        {
            if (outline == null || style == null)
            {
                return false;
            }

            float borderWidth = Mathf.Max(0f, style.BorderWidth);
            if (borderWidth <= Mathf.Epsilon)
            {
                // An enabled uGUI Outline still redraws and can tint its Graphic when its
                // distance is zero. Borderless must clear the effect, not merely collapse it.
                // Preserve enabled so applying a style never overrides caller-owned component state.
                outline.effectColor = Color.clear;
                outline.effectDistance = Vector2.zero;
                outline.useGraphicAlpha = false;
                return true;
            }

            outline.effectColor = style.ResolveBorderColor(surfaceColor);
            outline.effectDistance = new Vector2(borderWidth, -borderWidth);
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

        private static Sprite GetOrCreateGeneratedSprite(Texture2D texture)
        {
            if (GeneratedTextureSprites.TryGetValue(texture, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = texture.name + " Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            GeneratedTextureSprites[texture] = sprite;
            GeneratedSprites.Add(sprite);
            return sprite;
        }
    }
}
