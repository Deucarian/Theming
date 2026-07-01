using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Theme asset that owns the active palette for a visual style.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme", menuName = "Deucarian/Theming/Theme")]
    public sealed class DeucarianTheme : ScriptableObject
    {
        [SerializeField] private string themeId = "deucarian.theme.default";
        [SerializeField] private string displayName = "Default";
        [SerializeField] private DeucarianColorPalette colorPalette;
        [SerializeField] private DeucarianThemeStyle visualStyle;

        /// <summary>Stable theme identifier.</summary>
        public string ThemeId => themeId;

        /// <summary>Human-readable theme name.</summary>
        public string DisplayName => displayName;

        /// <summary>Palette used by this theme.</summary>
        public DeucarianColorPalette ColorPalette => colorPalette;

        /// <summary>Optional visual style used for themed chrome and surface treatment.</summary>
        public DeucarianThemeStyle VisualStyle => visualStyle;

        /// <summary>Configures theme metadata and palette reference.</summary>
        public void Configure(string id, string name, DeucarianColorPalette palette)
        {
            Configure(id, name, palette, visualStyle);
        }

        /// <summary>Configures theme metadata, palette reference, and optional visual style.</summary>
        public void Configure(string id, string name, DeucarianColorPalette palette, DeucarianThemeStyle style)
        {
            themeId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            colorPalette = palette;
            visualStyle = style;
            NotifyChanged();
        }

        /// <summary>Sets the optional visual style used by this theme.</summary>
        public void SetVisualStyle(DeucarianThemeStyle style)
        {
            if (visualStyle == style)
            {
                return;
            }

            visualStyle = style;
            NotifyChanged();
        }

        /// <summary>Sets the palette used by this theme.</summary>
        public void SetColorPalette(DeucarianColorPalette palette)
        {
            if (colorPalette == palette)
            {
                return;
            }

            colorPalette = palette;
            NotifyChanged();
        }

        /// <summary>Returns the palette color, role default color, or magenta when unresolved.</summary>
        public Color GetColor(DeucarianColorRole role)
        {
            return TryGetColor(role, out Color color) ? color : DeucarianColorPalette.MissingColor;
        }

        /// <summary>Delegates color lookup to the assigned palette.</summary>
        public bool TryGetColor(DeucarianColorRole role, out Color color)
        {
            if (colorPalette != null)
            {
                return colorPalette.TryGetColor(role, out color);
            }

            color = DeucarianColorPalette.MissingColor;
            return false;
        }

        /// <summary>Returns the palette color by role ID or magenta when unresolved.</summary>
        public Color GetColorById(string roleId)
        {
            return TryGetColorById(roleId, out Color color) ? color : DeucarianColorPalette.MissingColor;
        }

        /// <summary>Delegates color lookup by role ID to the assigned palette.</summary>
        public bool TryGetColorById(string roleId, out Color color)
        {
            if (colorPalette != null)
            {
                return colorPalette.TryGetColorById(roleId, out color);
            }

            color = DeucarianColorPalette.MissingColor;
            return false;
        }

        private void OnValidate()
        {
            themeId = DeucarianColorRole.NormalizeId(themeId);
            displayName = displayName ?? string.Empty;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
