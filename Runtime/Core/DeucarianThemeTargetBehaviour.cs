using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Base behaviour for components that resolve, apply, and respond to Deucarian themes.
    /// </summary>
    public abstract class DeucarianThemeTargetBehaviour : MonoBehaviour, IDeucarianThemeTarget
    {
        [SerializeField] private DeucarianTheme themeOverride;
        [SerializeField] private bool applyOnEnable = true;

        private DeucarianThemeProvider subscribedProvider;
        private bool warnedMissingTheme;

        /// <summary>Optional theme override used before provider lookup.</summary>
        public DeucarianTheme ThemeOverride
        {
            get => themeOverride;
            set
            {
                if (themeOverride == value)
                {
                    return;
                }

                themeOverride = value;
                if (isActiveAndEnabled)
                {
                    RefreshProviderSubscription();
                    ApplyTheme();
                }
                else
                {
                    UnsubscribeFromProvider();
                }
            }
        }

        /// <summary>Whether this target applies its theme when enabled.</summary>
        public bool ApplyOnEnable
        {
            get => applyOnEnable;
            set => applyOnEnable = value;
        }

        /// <summary>Applies the currently resolved theme.</summary>
        public void ApplyTheme()
        {
            ApplyTheme(null);
        }

        /// <inheritdoc />
        public void ApplyTheme(DeucarianTheme theme)
        {
            DeucarianTheme resolvedTheme = ResolveTheme(theme);
            if (resolvedTheme == null)
            {
                WarnOnce(ref warnedMissingTheme, "No theme found for theme target.");
                return;
            }

            warnedMissingTheme = false;
            ApplyResolvedTheme(resolvedTheme);
        }

        /// <summary>Applies an already resolved non-null theme to the concrete target.</summary>
        protected abstract void ApplyResolvedTheme(DeucarianTheme theme);

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

            DeucarianThemeProvider provider = subscribedProvider != null
                ? subscribedProvider
                : GetComponentInParent<DeucarianThemeProvider>();

            if (provider != null && provider.CurrentTheme != null)
            {
                return provider.CurrentTheme;
            }

            return DeucarianThemeProvider.Active != null ? DeucarianThemeProvider.Active.CurrentTheme : null;
        }

        /// <summary>Logs a warning once until the warning condition clears.</summary>
        protected void WarnOnce(ref bool flag, string message)
        {
            if (flag)
            {
                return;
            }

            flag = true;
            Debug.LogWarning(message, this);
        }

        /// <summary>Finds the provider this target should listen to for theme changes.</summary>
        protected virtual DeucarianThemeProvider FindProvider()
        {
            DeucarianThemeProvider parentProvider = GetComponentInParent<DeucarianThemeProvider>();
            return parentProvider != null ? parentProvider : DeucarianThemeProvider.Active;
        }

        protected virtual void OnEnable()
        {
            RefreshProviderSubscription();

            if (applyOnEnable)
            {
                ApplyTheme();
            }
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromProvider();
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromProvider();
        }

        protected virtual void OnTransformParentChanged()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            RefreshProviderSubscription();

            if (applyOnEnable)
            {
                ApplyTheme();
            }
        }

        private void RefreshProviderSubscription()
        {
            DeucarianThemeProvider provider = themeOverride == null ? FindProvider() : null;
            if (provider == subscribedProvider)
            {
                return;
            }

            UnsubscribeFromProvider();

            subscribedProvider = provider;
            if (subscribedProvider != null)
            {
                subscribedProvider.ThemeChanged += OnProviderThemeChanged;
            }
        }

        private void UnsubscribeFromProvider()
        {
            if (subscribedProvider == null)
            {
                return;
            }

            subscribedProvider.ThemeChanged -= OnProviderThemeChanged;
            subscribedProvider = null;
        }

        private void OnProviderThemeChanged(DeucarianTheme theme)
        {
            if (themeOverride != null)
            {
                return;
            }

            ApplyTheme(theme);
        }
    }
}
