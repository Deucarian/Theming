using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Browser-neutral CSS projection of a viewer theme. Consumers choose the
    /// transport; Theming owns the semantic projection and JSON shape.
    /// </summary>
    [Serializable]
    public sealed class DeucarianViewerThemeSnapshot
    {
        private const float DefaultCornerRadius = 8f;
        private const float DefaultBorderWidth = 1f;

        private static readonly string[] RequiredRoleIds =
        {
            DeucarianBuiltinColorRoleIds.Background,
            DeucarianBuiltinColorRoleIds.Surface,
            DeucarianBuiltinColorRoleIds.SurfaceRaised,
            DeucarianBuiltinColorRoleIds.TextPrimary,
            DeucarianBuiltinColorRoleIds.TextSecondary,
            DeucarianBuiltinColorRoleIds.TextMuted,
            DeucarianBuiltinColorRoleIds.TextDisabled,
            DeucarianBuiltinColorRoleIds.Primary,
            DeucarianBuiltinColorRoleIds.Secondary,
            DeucarianBuiltinColorRoleIds.Accent,
            DeucarianBuiltinColorRoleIds.Success,
            DeucarianBuiltinColorRoleIds.Warning,
            DeucarianBuiltinColorRoleIds.Error,
            DeucarianBuiltinColorRoleIds.Info,
            DeucarianBuiltinColorRoleIds.UiNormal,
            DeucarianBuiltinColorRoleIds.UiHighlighted,
            DeucarianBuiltinColorRoleIds.UiPressed,
            DeucarianBuiltinColorRoleIds.UiSelected,
            DeucarianBuiltinColorRoleIds.UiDisabled,
            DeucarianBuiltinColorRoleIds.UiFocused
        };

        [SerializeField] private string themeId;
        [SerializeField] private string styleId;
        [SerializeField] private string surfaceTreatment;
        [SerializeField] private bool isDark;
        [SerializeField] private bool isValid;
        [SerializeField] private string[] missingRoles;
        [SerializeField] private string background;
        [SerializeField] private string surface;
        [SerializeField] private string surfaceRaised;
        [SerializeField] private string panel;
        [SerializeField] private string border;
        [SerializeField] private string textPrimary;
        [SerializeField] private string textSecondary;
        [SerializeField] private string textMuted;
        [SerializeField] private string textDisabled;
        [SerializeField] private string primary;
        [SerializeField] private string secondary;
        [SerializeField] private string accent;
        [SerializeField] private string success;
        [SerializeField] private string warning;
        [SerializeField] private string error;
        [SerializeField] private string info;
        [SerializeField] private string interactionNormal;
        [SerializeField] private string interactionHover;
        [SerializeField] private string interactionPressed;
        [SerializeField] private string interactionSelected;
        [SerializeField] private string interactionDisabled;
        [SerializeField] private string interactionFocused;
        [SerializeField] private string loading;
        [SerializeField] private string ready;
        [SerializeField] private string navigationActive;
        [SerializeField] private string navigationInactive;
        [SerializeField] private float radius;
        [SerializeField] private float borderWidth;
        [SerializeField] private float backdropBlur;
        [SerializeField] private float noiseOpacity;

        public string ThemeId => themeId;
        public string StyleId => styleId;
        public string SurfaceTreatment => surfaceTreatment;
        public bool IsDark => isDark;
        public bool IsValid => isValid;
        public string[] MissingRoles => missingRoles;
        public string Background => background;
        public string Surface => surface;
        public string SurfaceRaised => surfaceRaised;
        public string Panel => panel;
        public string Border => border;
        public string TextPrimary => textPrimary;
        public string TextSecondary => textSecondary;
        public string TextMuted => textMuted;
        public string TextDisabled => textDisabled;
        public string Primary => primary;
        public string Secondary => secondary;
        public string Accent => accent;
        public string Success => success;
        public string Warning => warning;
        public string Error => error;
        public string Info => info;
        public string InteractionNormal => interactionNormal;
        public string InteractionHover => interactionHover;
        public string InteractionPressed => interactionPressed;
        public string InteractionSelected => interactionSelected;
        public string InteractionDisabled => interactionDisabled;
        public string InteractionFocused => interactionFocused;
        public string Loading => loading;
        public string Ready => ready;
        public string NavigationActive => navigationActive;
        public string NavigationInactive => navigationInactive;
        public float Radius => radius;
        public float BorderWidth => borderWidth;
        public float BackdropBlur => backdropBlur;
        public float NoiseOpacity => noiseOpacity;

        /// <summary>Projects a theme and optional style into the canonical snapshot.</summary>
        public static DeucarianViewerThemeSnapshot FromTheme(
            DeucarianTheme theme,
            DeucarianThemeStyle style = null)
        {
            DeucarianTheme resolvedTheme = theme;
            DeucarianThemeStyle resolvedStyle = style ??
                resolvedTheme?.VisualStyle;
            Color backgroundColor = Resolve(
                resolvedTheme,
                DeucarianBuiltinColorRoleIds.Background);
            Color surfaceColor = Resolve(
                resolvedTheme,
                DeucarianBuiltinColorRoleIds.Surface);
            Color raisedColor = Resolve(
                resolvedTheme,
                DeucarianBuiltinColorRoleIds.SurfaceRaised);
            Color navigationActiveColor = Resolve(
                resolvedTheme,
                IsLightTheme(resolvedTheme)
                    ? DeucarianBuiltinColorRoleIds.UiSelected
                    : DeucarianBuiltinColorRoleIds.Accent);
            Color navigationInactiveColor = Resolve(
                resolvedTheme,
                IsLightTheme(resolvedTheme)
                    ? DeucarianBuiltinColorRoleIds.TextSecondary
                    : DeucarianBuiltinColorRoleIds.TextMuted);
            Color panelColor = resolvedStyle != null
                ? resolvedStyle.ResolveSurfaceColor(raisedColor)
                : raisedColor;
            Color borderColor = resolvedStyle != null
                ? resolvedStyle.ResolveBorderColor(panelColor)
                : navigationInactiveColor;
            string[] missing = GetMissingRequiredRoleIds(resolvedTheme);

            return new DeucarianViewerThemeSnapshot
            {
                themeId = resolvedTheme?.ThemeId ?? string.Empty,
                styleId = resolvedStyle?.StyleId ?? string.Empty,
                surfaceTreatment = GetSurfaceTreatmentName(resolvedStyle),
                isDark = IsDarkColor(backgroundColor),
                isValid = resolvedTheme != null &&
                          !string.IsNullOrWhiteSpace(resolvedTheme.ThemeId) &&
                          missing.Length == 0,
                missingRoles = missing,
                background = ToCssColor(backgroundColor),
                surface = ToCssColor(surfaceColor),
                surfaceRaised = ToCssColor(raisedColor),
                panel = ToCssColor(panelColor),
                border = ToCssColor(borderColor),
                textPrimary = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.TextPrimary),
                textSecondary = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.TextSecondary),
                textMuted = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.TextMuted),
                textDisabled = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.TextDisabled),
                primary = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Primary),
                secondary = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Secondary),
                accent = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Accent),
                success = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Success),
                warning = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Warning),
                error = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Error),
                info = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Info),
                interactionNormal = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.UiNormal),
                interactionHover = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.UiHighlighted),
                interactionPressed = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.UiPressed),
                interactionSelected = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.UiSelected),
                interactionDisabled = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.UiDisabled),
                interactionFocused = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.UiFocused),
                loading = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Info),
                ready = ResolveCss(resolvedTheme, DeucarianBuiltinColorRoleIds.Success),
                navigationActive = ToCssColor(navigationActiveColor),
                navigationInactive = ToCssColor(navigationInactiveColor),
                radius = resolvedStyle != null
                    ? Mathf.Max(0f, resolvedStyle.CornerRadius)
                    : DefaultCornerRadius,
                borderWidth = resolvedStyle != null
                    ? Mathf.Max(0f, resolvedStyle.BorderWidth)
                    : DefaultBorderWidth,
                backdropBlur = GetBackdropBlur(resolvedStyle),
                noiseOpacity = resolvedStyle != null &&
                               resolvedStyle.UseGeneratedNoiseTexture
                    ? Mathf.Clamp01(resolvedStyle.TextureTint.a)
                    : 0f
            };
        }

        /// <summary>Serializes the canonical camelCase snapshot shape.</summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        private static Color Resolve(DeucarianTheme theme, string roleId)
        {
            return DeucarianViewerReferenceThemeResolver.ResolveColor(
                theme,
                roleId);
        }

        private static string ResolveCss(DeucarianTheme theme, string roleId)
        {
            return ToCssColor(Resolve(theme, roleId));
        }

        private static string ToCssColor(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        private static bool IsDarkColor(Color color)
        {
            float luminance = color.r * 0.2126f +
                              color.g * 0.7152f +
                              color.b * 0.0722f;
            return luminance <= 0.5f;
        }

        private static bool IsLightTheme(DeucarianTheme theme)
        {
            DeucarianColorPalette palette = theme?.ColorPalette;
            return palette != null && palette.HasThemeMode &&
                   palette.ThemeMode == DeucarianThemeMode.Light;
        }

        private static string[] GetMissingRequiredRoleIds(
            DeucarianTheme theme)
        {
            var missing = new List<string>();
            DeucarianColorPalette palette = theme?.ColorPalette;
            for (int i = 0; i < RequiredRoleIds.Length; i++)
            {
                string roleId = RequiredRoleIds[i];
                if (palette == null ||
                    !palette.TryGetColorById(roleId, out Color color) ||
                    DeucarianViewerReferenceThemeResolver.IsMissingColor(color))
                {
                    missing.Add(roleId);
                }
            }

            return missing.ToArray();
        }

        private static float GetBackdropBlur(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return 0f;
            }

            switch (style.SurfaceTreatment)
            {
                case DeucarianThemeStyleSurfaceTreatment.FrostedGlass:
                    return 18f;
                case DeucarianThemeStyleSurfaceTreatment.FluentAcrylic:
                    return 12f;
                default:
                    return 0f;
            }
        }

        private static string GetSurfaceTreatmentName(
            DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return "solid";
            }

            switch (style.SurfaceTreatment)
            {
                case DeucarianThemeStyleSurfaceTreatment.FrostedGlass:
                    return "frosted-glass";
                case DeucarianThemeStyleSurfaceTreatment.Material:
                    return "material";
                case DeucarianThemeStyleSurfaceTreatment.FluentAcrylic:
                    return "fluent-acrylic";
                default:
                    return "solid";
            }
        }
    }
}
