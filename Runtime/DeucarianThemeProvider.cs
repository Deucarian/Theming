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
        [SerializeField] private bool applyToChildrenOnEnable = true;
        [SerializeField] private bool includeInactiveChildren = true;

        /// <summary>
        /// Last enabled provider. This is a convenience fallback, not a required singleton.
        /// </summary>
        public static DeucarianThemeProvider Active { get; private set; }

        /// <summary>Currently active theme for this provider.</summary>
        public DeucarianTheme CurrentTheme => currentTheme;

        /// <summary>Raised after this provider changes theme and reapplies child targets.</summary>
        public event Action<DeucarianTheme> ThemeChanged;

        /// <summary>Sets the active theme and reapplies it to child theme targets.</summary>
        public void SetTheme(DeucarianTheme theme)
        {
            if (currentTheme == theme)
            {
                return;
            }

            currentTheme = theme;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            ApplyThemeToChildren();
            ThemeChanged?.Invoke(currentTheme);
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
