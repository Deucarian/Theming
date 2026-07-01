using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

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
        [NonSerialized] private DeucarianThemeStyle lastAppliedStyle;

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
                RefreshThemeGraph();
                return;
            }

            currentTheme = theme;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            RefreshThemeGraph();
        }

        /// <summary>Sets the provider style override and reapplies it to child style targets.</summary>
        public void SetStyle(DeucarianThemeStyle style)
        {
            if (styleOverride == style)
            {
                RefreshStyle();
                return;
            }

            styleOverride = style;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            ApplyResolvedStyleAndNotify();
        }

        /// <summary>Clears the style override so the current theme's visual style is used.</summary>
        public void ClearStyleOverride()
        {
            SetStyle(null);
        }

        /// <summary>
        /// Reapplies and broadcasts the resolved style if it changed outside the provider API,
        /// such as when a theme asset's visual style is edited.
        /// </summary>
        public bool RefreshStyle()
        {
            DeucarianThemeStyle style = CurrentStyle;
            if (lastAppliedStyle == null && style == null)
            {
                return false;
            }

            ApplyResolvedStyleAndNotify();
            return true;
        }

        /// <summary>Reapplies the resolved theme and style through the provider's target APIs.</summary>
        public bool RefreshThemeGraph()
        {
            ApplyThemeToChildren();
            ThemeChanged?.Invoke(currentTheme);
            ApplyResolvedStyleAndNotify();
            return true;
        }

        /// <summary>Returns true when this provider depends on the supplied theme asset.</summary>
        public bool UsesThemeAsset(UnityObject asset)
        {
            if (asset == null)
            {
                return false;
            }

            DeucarianThemeStyle currentStyle = CurrentStyle;
            if (asset == currentTheme || asset == styleOverride || asset == currentStyle)
            {
                return true;
            }

            if (asset is DeucarianThemeRuntimeSettings)
            {
                return currentTheme == null;
            }

            DeucarianColorPalette palette = currentTheme != null ? currentTheme.ColorPalette : null;
            if (asset == palette)
            {
                return true;
            }

            if (palette == null)
            {
                return asset is DeucarianColorRole && currentTheme == null;
            }

            if (asset == palette.RoleLibrary)
            {
                return true;
            }

            if (asset is DeucarianColorRole)
            {
                return true;
            }

            return false;
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

            lastAppliedStyle = style;
        }

        private void OnEnable()
        {
            if (!EnabledProviders.Contains(this))
            {
                EnabledProviders.Add(this);
            }

            Active = this;
            DeucarianThemeAssetChangeBus.AssetChanged += OnThemeAssetChanged;

            if (applyToChildrenOnEnable)
            {
                ApplyThemeToChildren();
                ApplyStyleToChildren();
            }
            else
            {
                lastAppliedStyle = CurrentStyle;
            }
        }

        private void OnDisable()
        {
            DeucarianThemeAssetChangeBus.AssetChanged -= OnThemeAssetChanged;
            EnabledProviders.Remove(this);

            if (Active == this)
            {
                Active = EnabledProviders.Count > 0 ? EnabledProviders[EnabledProviders.Count - 1] : null;
            }
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                lastAppliedStyle = CurrentStyle;
                return;
            }

            RefreshThemeGraph();
        }

        private void ApplyResolvedStyleAndNotify()
        {
            ApplyStyleToChildren();
            StyleChanged?.Invoke(lastAppliedStyle);
        }

        private void OnThemeAssetChanged(UnityObject asset)
        {
            if (!isActiveAndEnabled || !UsesThemeAsset(asset))
            {
                return;
            }

            RefreshThemeGraph();
        }
    }
}
