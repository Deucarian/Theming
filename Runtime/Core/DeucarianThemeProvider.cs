using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Scene component that owns the current theme for child theme targets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeucarianThemeProvider : MonoBehaviour
    {
        private static readonly List<DeucarianThemeProvider> EnabledProviders = new List<DeucarianThemeProvider>();

        [SerializeField] private DeucarianTheme currentTheme;
        [SerializeField] private DeucarianThemeStyle styleOverride;
        [SerializeField] private bool applyToChildrenOnEnable = true;
        [SerializeField] private bool includeInactiveChildren = true;

        /// <summary>
        /// Last enabled provider. This is a convenience fallback, not a required singleton.
        /// </summary>
        public static DeucarianThemeProvider Active { get; private set; }

        /// <summary>Currently active theme for this provider.</summary>
        public DeucarianTheme CurrentTheme => currentTheme;

        /// <summary>Explicit style override. When null, the current theme's visual style is used.</summary>
        public DeucarianThemeStyle StyleOverride => styleOverride;

        /// <summary>Resolved active visual style for this provider.</summary>
        public DeucarianThemeStyle CurrentStyle => styleOverride != null
            ? styleOverride
            : currentTheme != null ? currentTheme.VisualStyle : null;

        /// <summary>Raised after this provider changes theme and reapplies child targets.</summary>
        public event Action<DeucarianTheme> ThemeChanged;

        /// <summary>Raised after this provider changes resolved style and reapplies child style targets.</summary>
        public event Action<DeucarianThemeStyle> StyleChanged;

        /// <summary>Sets the active theme and reapplies it to child theme targets.</summary>
        public void SetTheme(DeucarianTheme theme)
        {
            if (currentTheme == theme)
            {
                return;
            }

            DeucarianThemeStyle previousStyle = CurrentStyle;
            currentTheme = theme;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            ApplyThemeToChildren();
            ThemeChanged?.Invoke(currentTheme);

            if (previousStyle != CurrentStyle)
            {
                ApplyStyleToChildren();
                StyleChanged?.Invoke(CurrentStyle);
            }
        }

        /// <summary>Sets the provider style override and reapplies it to child style targets.</summary>
        public void SetStyle(DeucarianThemeStyle style)
        {
            if (styleOverride == style)
            {
                return;
            }

            styleOverride = style;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            ApplyStyleToChildren();
            StyleChanged?.Invoke(CurrentStyle);
        }

        /// <summary>Clears the style override so the current theme's visual style is used.</summary>
        public void ClearStyleOverride()
        {
            SetStyle(null);
        }

        /// <summary>Applies the current theme to child components implementing <see cref="IDeucarianThemeTarget"/>.</summary>
        public void ApplyThemeToChildren()
        {
            ApplyThemeToChildren(includeInactiveChildren);
        }

        /// <summary>Applies the current theme to child components implementing <see cref="IDeucarianThemeTarget"/>.</summary>
        public void ApplyThemeToChildren(bool includeInactive)
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(includeInactive);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is IDeucarianThemeTarget target)
                {
                    target.ApplyTheme(currentTheme);
                }
            }
        }

        /// <summary>Applies the current resolved style to child components implementing <see cref="IDeucarianThemeStyleTarget"/>.</summary>
        public void ApplyStyleToChildren()
        {
            ApplyStyleToChildren(includeInactiveChildren);
        }

        /// <summary>Applies the current resolved style to child components implementing <see cref="IDeucarianThemeStyleTarget"/>.</summary>
        public void ApplyStyleToChildren(bool includeInactive)
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(includeInactive);
            DeucarianThemeStyle style = CurrentStyle;
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is IDeucarianThemeStyleTarget target)
                {
                    target.ApplyStyle(style);
                }
            }
        }

        private void OnEnable()
        {
            if (!EnabledProviders.Contains(this))
            {
                EnabledProviders.Add(this);
            }

            Active = this;

            if (applyToChildrenOnEnable)
            {
                ApplyThemeToChildren();
                ApplyStyleToChildren();
            }
        }

        private void OnDisable()
        {
            EnabledProviders.Remove(this);

            if (Active == this)
            {
                Active = EnabledProviders.Count > 0 ? EnabledProviders[EnabledProviders.Count - 1] : null;
            }
        }
    }
}
