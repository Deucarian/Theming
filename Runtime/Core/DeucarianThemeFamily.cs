using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Paired light and dark variants that represent one logical theme family.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme Family", menuName = "Deucarian/Theming/Theme Family")]
    public sealed class DeucarianThemeFamily : ScriptableObject
    {
        [SerializeField] private string familyId = "deucarian.theme-family.default";
        [SerializeField] private string displayName = "Default";
        [SerializeField] private DeucarianTheme lightTheme;
        [SerializeField] private DeucarianTheme darkTheme;

        /// <summary>Stable theme-family identifier.</summary>
        public string FamilyId => familyId;

        /// <summary>Human-readable theme-family name.</summary>
        public string DisplayName => displayName;

        /// <summary>Theme used when the family is resolved in light mode.</summary>
        public DeucarianTheme LightTheme => lightTheme;

        /// <summary>Theme used when the family is resolved in dark mode.</summary>
        public DeucarianTheme DarkTheme => darkTheme;

        /// <summary>Whether both required variants are assigned.</summary>
        public bool IsComplete => lightTheme != null && darkTheme != null;

        /// <summary>Configures theme-family metadata and both required variants.</summary>
        public void Configure(
            string id,
            string name,
            DeucarianTheme light,
            DeucarianTheme dark)
        {
            familyId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            lightTheme = light;
            darkTheme = dark;
            NotifyChanged();
        }

        /// <summary>Returns the exact variant assigned for the requested mode.</summary>
        public DeucarianTheme GetTheme(DeucarianThemeMode mode)
        {
            return mode == DeucarianThemeMode.Light ? lightTheme : darkTheme;
        }

        /// <summary>Attempts to return the exact variant assigned for the requested mode.</summary>
        public bool TryGetTheme(DeucarianThemeMode mode, out DeucarianTheme theme)
        {
            theme = GetTheme(mode);
            return theme != null;
        }

        /// <summary>
        /// Returns the requested variant, falling back to the available opposite variant when incomplete.
        /// </summary>
        public DeucarianTheme ResolveTheme(DeucarianThemeMode mode)
        {
            DeucarianTheme theme = GetTheme(mode);
            if (theme != null)
            {
                return theme;
            }

            return mode == DeucarianThemeMode.Light ? darkTheme : lightTheme;
        }

        /// <summary>Sets one variant without changing the family metadata or opposite variant.</summary>
        public void SetTheme(DeucarianThemeMode mode, DeucarianTheme theme)
        {
            if (mode == DeucarianThemeMode.Light)
            {
                if (lightTheme == theme)
                {
                    return;
                }

                lightTheme = theme;
            }
            else
            {
                if (darkTheme == theme)
                {
                    return;
                }

                darkTheme = theme;
            }

            NotifyChanged();
        }

        /// <summary>
        /// Assigns one visual style to both available variants and publishes one family-level change notification.
        /// </summary>
        public bool SetSharedVisualStyle(DeucarianThemeStyle style)
        {
            bool lightChanged = lightTheme != null && lightTheme.VisualStyle != style;
            bool darkChanged = darkTheme != null && darkTheme.VisualStyle != style;
            if (!lightChanged && !darkChanged)
            {
                return false;
            }

            using (DeucarianThemeAssetChangeBus.BeginBatch(this))
            {
                if (lightChanged)
                {
                    lightTheme.SetVisualStyle(style);
                }

                if (darkChanged)
                {
                    darkTheme.SetVisualStyle(style);
                }
            }

            return true;
        }

        private void OnValidate()
        {
            familyId = DeucarianColorRole.NormalizeId(familyId);
            displayName = displayName ?? string.Empty;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
