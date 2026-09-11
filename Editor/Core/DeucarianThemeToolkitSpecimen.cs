using Deucarian.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using Ui = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemeToolkitSpecimen
    {
        private readonly VisualElement sample;
        private readonly Label title, body;
        private readonly Button primary, secondary;
        private readonly DeucarianEditorControlSpecimen controls;
        private bool wantsControls;
        private bool hasPalette;
        internal VisualElement Root { get; }

        internal DeucarianThemeToolkitSpecimen()
        {
            Root = Ui.Panel("theme-native-preview", "Preview");
            Root.Add(Ui.Label("See your changes as you edit.", "dw-muted"));
            sample = Ui.Region("theme-specimen", "dw-specimen");
            title = Ui.Label("Sample title", "dw-section-title");
            body = Ui.Label("This is how your app's interface could look with the selected palette.", "dw-muted");
            primary = Ui.Button("Primary action", null, true);
            secondary = Ui.Button("Cancel", null);
            primary.tooltip = secondary.tooltip = "Preview control · no project action";
            sample.Add(title); sample.Add(body); sample.Add(Ui.Actions(primary, secondary)); Root.Add(sample);
            controls = new DeucarianEditorControlSpecimen(); Root.Add(controls);
            Ui.Show(controls, false);
        }

        internal void ShowControls(bool show)
        { wantsControls = show; Ui.Show(controls, wantsControls && hasPalette); }

        internal void Refresh(DeucarianThemeManagerSelection selection, DeucarianThemeStyleDraft composer = null)
        {
            var palette = selection.ResolvedPalette;
            hasPalette = palette != null;
            Ui.Show(controls, wantsControls && hasPalette);
            Ui.Show(sample, palette != null);
            if (palette == null) return;
            Color surface = palette.GetColorById(DeucarianBuiltinColorRoleIds.Surface);
            var style = selection.Style;
            var surfaceProfile = composer?.Surface ?? style?.SurfaceProfile;
            var shape = composer?.Corners ?? style?.ShapeProfile;
            var typography = composer?.Typography ?? style?.TypographyProfile;
            var stroke = composer?.Border ?? style?.StrokeProfile;
            var density = composer?.Size ?? style?.Density ?? DeucarianThemeDensity.Standard;
            Color resolvedSurface = surfaceProfile != null ? surfaceProfile.ResolveSurfaceColor(surface) : style != null ? style.ResolveSurfaceColor(surface) : surface;
            sample.style.backgroundColor = resolvedSurface;
            Color border = stroke != null ? stroke.ResolveBorderColor(resolvedSurface) : style != null ? style.ResolveBorderColor(resolvedSurface) : Color.clear;
            float borderWidth = stroke != null ? stroke.BorderWidth : style != null ? style.BorderWidth : 0;
            sample.style.borderTopColor = sample.style.borderBottomColor = sample.style.borderLeftColor = sample.style.borderRightColor = border;
            sample.style.borderTopWidth = sample.style.borderBottomWidth = sample.style.borderLeftWidth = sample.style.borderRightWidth = borderWidth;
            var texture = surfaceProfile != null ? surfaceProfile.GetGeneratedTexture() : style != null ? style.GetGeneratedTexture() : null;
            sample.style.backgroundImage = texture != null ? new StyleBackground(texture) : new StyleBackground(StyleKeyword.None);
            sample.style.unityBackgroundImageTintColor = surfaceProfile != null ? surfaceProfile.TextureTint : style != null ? style.TextureTint : Color.clear;
            title.style.color = palette.GetColorById(DeucarianBuiltinColorRoleIds.TextPrimary);
            body.style.color = palette.TryGetColorById(DeucarianBuiltinColorRoleIds.TextSecondary, out var text) ? text : title.style.color.value;
            primary.style.backgroundColor = palette.GetColorById(DeucarianBuiltinColorRoleIds.Primary);
            primary.style.color = Contrast(primary.style.backgroundColor.value);
            secondary.style.color = title.style.color;
            secondary.style.backgroundColor = surface;
            float radius = shape != null ? shape.CornerRadius : style != null ? style.CornerRadius : 0;
            sample.style.borderTopLeftRadius = sample.style.borderTopRightRadius = sample.style.borderBottomLeftRadius = sample.style.borderBottomRightRadius = radius;
            var font = typography?.ResolvedFontAsset?.sourceFontFile;
            title.style.unityFont = body.style.unityFont = font != null ? new StyleFont(font) : new StyleFont(StyleKeyword.Null);
            if (typography != null)
            {
                title.style.fontSize = typography.Title.FontSize * 2;
                body.style.fontSize = typography.Body.FontSize * 2;
                title.style.unityFontStyleAndWeight = DeucarianThemeSpecimenRenderer.ResolveUnityFontStyle(typography.Title.FontStyle, FontStyle.Bold);
                body.style.unityFontStyleAndWeight = DeucarianThemeSpecimenRenderer.ResolveUnityFontStyle(typography.Body.FontStyle, FontStyle.Normal);
            }
            else { title.style.fontSize = StyleKeyword.Null; body.style.fontSize = StyleKeyword.Null; title.style.unityFontStyleAndWeight = StyleKeyword.Null; body.style.unityFontStyleAndWeight = StyleKeyword.Null; }
            primary.style.height = secondary.style.height = DeucarianThemeSpecimenRenderer.ResolvePreviewControlHeight(density) * 2;
            controls.SetColors(resolvedSurface, primary.style.backgroundColor.value, title.style.color.value);
            sample.tooltip = "Magnified theme specimen · selected surface, border, shape, type and density";
        }

        private static Color Contrast(Color color) => color.grayscale > .48f ? Color.black : Color.white;
    }
}
