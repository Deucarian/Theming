using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Base class for components that apply a theme color role to a Unity component.
    /// </summary>
    public abstract class DeucarianThemeColorTarget : MonoBehaviour, IDeucarianThemeTarget
    {
        [SerializeField] private DeucarianColorRole colorRole;
        [SerializeField] private DeucarianTheme themeOverride;
        [SerializeField] private bool applyOnEnable = true;

        private bool warnedMissingRole;
        private bool warnedMissingTheme;

        /// <summary>Role whose color should be applied.</summary>
        public DeucarianColorRole ColorRole
        {
            get => colorRole;
            set
            {
                colorRole = value;
                ApplyTheme();
            }
        }

        /// <summary>Optional theme override used before provider lookup.</summary>
        public DeucarianTheme ThemeOverride
        {
            get => themeOverride;
            set
            {
                themeOverride = value;
                ApplyTheme();
            }
        }

        /// <summary>Whether this target applies its color when enabled.</summary>
        public bool ApplyOnEnable
        {
            get => applyOnEnable;
            set => applyOnEnable = value;
        }

        /// <summary>Applies the resolved theme using override, parent provider, or active provider lookup.</summary>
        public void ApplyTheme()
        {
            ApplyTheme(null);
        }

        /// <inheritdoc />
        public void ApplyTheme(DeucarianTheme theme)
        {
            CacheTarget();

            if (colorRole == null)
            {
                WarnOnce(ref warnedMissingRole, "Theme color target has no color role assigned.");
                return;
            }

            DeucarianTheme resolvedTheme = ResolveTheme(theme);
            if (resolvedTheme == null)
            {
                WarnOnce(ref warnedMissingTheme, $"No theme found for color role '{colorRole.Id}'.");
                return;
            }

            warnedMissingRole = false;
            warnedMissingTheme = false;

            Color color = resolvedTheme.GetColor(colorRole);
            ApplyColor(color);
        }

        /// <summary>Allows derived adapters to cache their target component.</summary>
        protected virtual void CacheTarget()
        {
        }

        /// <summary>Applies the resolved color to the concrete target component.</summary>
        protected abstract void ApplyColor(Color color);

        /// <summary>Resolves the theme from override, supplied provider theme, nearest provider, or active provider.</summary>
        protected DeucarianTheme ResolveTheme(DeucarianTheme suppliedTheme)
        {
            if (themeOverride != null)
            {
                return themeOverride;
            }

            if (suppliedTheme != null)
            {
                return suppliedTheme;
            }

            DeucarianThemeProvider provider = GetComponentInParent<DeucarianThemeProvider>();
            if (provider != null && provider.CurrentTheme != null)
            {
                return provider.CurrentTheme;
            }

            return DeucarianThemeProvider.Active != null ? DeucarianThemeProvider.Active.CurrentTheme : null;
        }

        protected virtual void Awake()
        {
            CacheTarget();
        }

        protected virtual void OnEnable()
        {
            if (applyOnEnable)
            {
                ApplyTheme();
            }
        }

        private void WarnOnce(ref bool flag, string message)
        {
            if (flag)
            {
                return;
            }

            flag = true;
            Debug.LogWarning(message, this);
        }
    }
}
