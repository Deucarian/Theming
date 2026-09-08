using System;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    [Serializable]
    internal sealed class DeucarianThemeStyleDraft
    {
        public DeucarianThemeStyle Source;
        public DeucarianThemeStyle EditingStyle;
        public DeucarianThemeSurfaceProfile Surface;
        public DeucarianThemeShapeProfile Corners;
        public DeucarianThemeStrokeProfile Border;
        public DeucarianThemeDensity Size;
        public DeucarianThemeTypographyProfile Typography;

        public bool HasDraft => Source != null;
        public bool IsComplete => Surface != null && Corners != null && Border != null &&
                                  Size != DeucarianThemeDensity.Unspecified;
        public bool IsDirty => DeucarianThemeDraftPolicy.IsComposerDraftDirty(
            EditingStyle != null ? EditingStyle : Source, Surface, Corners, Border, Size, Typography);

        public void Reset(DeucarianThemeStyle style)
        {
            Source = style;
            EditingStyle = style != null && style.IsCustomStyle ? style : null;
            Surface = style != null ? style.SurfaceProfile : null;
            Corners = style != null ? style.ShapeProfile : null;
            Border = style != null ? style.StrokeProfile : null;
            Size = style != null ? style.Density : DeucarianThemeDensity.Unspecified;
            Typography = style != null ? style.TypographyProfile : null;
        }
    }
}
