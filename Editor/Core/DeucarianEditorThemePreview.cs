using UnityEditor;

namespace Deucarian.Theming.Editor
{
    /// <summary>Read-only selection for package-owned sandbox previews. Never applies project settings.</summary>
    public readonly struct DeucarianEditorThemePreview
    {
        private DeucarianEditorThemePreview(DeucarianTheme theme, DeucarianThemeStyle style,
            DeucarianThemeMode mode, bool draft, string label)
        { Theme = theme; Style = style; Mode = mode; IsDraft = draft; Label = label; }

        public DeucarianTheme Theme { get; }
        public DeucarianThemeStyle Style { get; }
        public DeucarianThemeMode Mode { get; }
        public bool IsDraft { get; }
        public string Label { get; }

        /// <summary>Uses Visual palettes' selected mode and composer draft, falling back to the project default.</summary>
        public static DeucarianEditorThemePreview Capture()
        {
            var selection = DeucarianThemePreviewCoordinator.SelectedPreview;
            var applied = DeucarianThemeRuntimeResolver.ResolveDefaultTheme();
            var theme = selection.ResolvedTheme != null ? selection.ResolvedTheme : applied;
            var style = selection.ResolvedTheme != null ? selection.Style : theme != null ? theme.VisualStyle : null;
            var mode = selection.ResolvedTheme != null ? selection.Mode : DeucarianThemeRuntimeResolver.ResolveDefaultThemeMode();
            bool draft = theme != applied || style != (applied != null ? applied.VisualStyle : null)
                || DeucarianThemePreviewCoordinator.HasComposerPreview || IsDirty(theme) || IsDirty(style)
                || (theme != null && IsDirty(theme.ColorPalette))
                || (style != null && (IsDirty(style.SurfaceProfile) || IsDirty(style.ShapeProfile)
                    || IsDirty(style.StrokeProfile) || IsDirty(style.TypographyProfile)));
            string label = (selection.Family != null ? selection.Family.DisplayName : theme != null ? theme.DisplayName : "No theme")
                + " · " + mode + (style != null ? " · " + style.DisplayName : "");
            return new DeucarianEditorThemePreview(theme, style, mode, draft, label);
        }

        private static bool IsDirty(UnityEngine.Object asset) => asset != null && EditorUtility.IsDirty(asset);
    }
}
