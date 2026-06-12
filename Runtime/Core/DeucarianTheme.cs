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

        /// <summary>Stable theme identifier.</summary>
        public string ThemeId => themeId;

        /// <summary>Human-readable theme name.</summary>
        public string DisplayName => displayName;

        /// <summary>Palette used by this theme.</summary>
        public DeucarianColorPalette ColorPalette => colorPalette;

        /// <summary>Configures theme metadata and palette reference.</summary>
        public void Configure(string id, string name, DeucarianColorPalette palette)
        {
            themeId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            colorPalette = palette;
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
            if (colorPalette != null)
            {
                return colorPalette.GetColorById(roleId);
            }

            return DeucarianColorPalette.MissingColor;
        }

        private void OnValidate()
        {
            themeId = DeucarianColorRole.NormalizeId(themeId);
            displayName = displayName ?? string.Empty;
        }
    }
}
