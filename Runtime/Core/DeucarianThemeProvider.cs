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
        [SerializeField] private DeucarianThemeFamily currentThemeFamily;
        [SerializeField] private DeucarianThemeMode themeMode = DeucarianThemeMode.Dark;
        [SerializeField] private DeucarianThemeStyle styleOverride;
        [SerializeField] private bool applyToChildrenOnEnable = true;
        [SerializeField] private bool includeInactiveChildren = true;
        [NonSerialized] private DeucarianThemeStyle lastAppliedStyle;
        [NonSerialized] private bool warnedIncompleteThemeFamily;

        /// <summary>
        /// Last enabled provider. This is a convenience fallback, not a required singleton.
        /// </summary>
        public static DeucarianThemeProvider Active { get; private set; }

        /// <summary>Currently active concrete theme for this provider.</summary>
        public DeucarianTheme CurrentTheme => ResolveCurrentTheme(true);

        /// <summary>Currently assigned paired theme family, or null when using a standalone theme.</summary>
        public DeucarianThemeFamily CurrentThemeFamily => currentThemeFamily;

        /// <summary>Mode used to resolve the assigned theme family.</summary>
        public DeucarianThemeMode ThemeMode => themeMode;

        /// <summary>Explicit style override. When null, the current theme's visual style is used.</summary>
        public DeucarianThemeStyle StyleOverride => styleOverride;

        /// <summary>Resolved active visual style for this provider.</summary>
        public DeucarianThemeStyle CurrentStyle
        {
            get
            {
                if (styleOverride != null)
                {
                    return styleOverride;
                }

                DeucarianTheme theme = CurrentTheme;
                return theme != null ? theme.VisualStyle : null;
            }
        }

        /// <summary>Raised after this provider changes theme and reapplies child targets.</summary>
        public event Action<DeucarianTheme> ThemeChanged;

        /// <summary>Raised after this provider changes mode and reapplies child targets.</summary>
        public event Action<DeucarianThemeMode> ThemeModeChanged;

        /// <summary>Raised after this provider changes resolved style and reapplies child style targets.</summary>
        public event Action<DeucarianThemeStyle> StyleChanged;

        /// <summary>Sets the active theme and reapplies it to child theme targets.</summary>
        public void SetTheme(DeucarianTheme theme)
        {
            if (currentThemeFamily == null && currentTheme == theme)
            {
                RefreshThemeGraph();
                return;
            }

            currentTheme = theme;
            currentThemeFamily = null;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            RefreshThemeGraph();
        }

        /// <summary>Sets the paired theme family and reapplies its current mode to child targets.</summary>
        public void SetThemeFamily(DeucarianThemeFamily family)
        {
            SetThemeFamily(family, themeMode);
        }

        /// <summary>Atomically sets a paired family and mode, then reapplies the resolved variant once.</summary>
        public void SetThemeFamily(DeucarianThemeFamily family, DeucarianThemeMode mode)
        {
            mode = NormalizeThemeMode(mode);
            bool modeChanged = themeMode != mode;
            bool familyAssignmentChanged = currentThemeFamily != family || currentTheme != null;
            currentThemeFamily = family;
            currentTheme = null;
            themeMode = mode;
            if (familyAssignmentChanged)
            {
                warnedIncompleteThemeFamily = false;
            }

            if (isActiveAndEnabled)
            {
                Active = this;
            }

            RefreshThemeGraph();
            if (modeChanged)
            {
                ThemeModeChanged?.Invoke(themeMode);
            }
        }

        /// <summary>Sets the explicit light or dark mode and reapplies the resolved family variant.</summary>
        public void SetThemeMode(DeucarianThemeMode mode)
        {
            mode = NormalizeThemeMode(mode);
            if (themeMode == mode)
            {
                RefreshThemeGraph();
                return;
            }

            themeMode = mode;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            RefreshThemeGraph();
            ThemeModeChanged?.Invoke(themeMode);
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
            DeucarianTheme theme = ResolveCurrentTheme(true);
            ApplyThemeToChildren(theme, includeInactiveChildren);
            ThemeChanged?.Invoke(theme);
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
            if (asset == currentThemeFamily || asset == styleOverride || asset == currentStyle)
            {
                return true;
            }

            if (asset is DeucarianThemeRuntimeSettings)
            {
                return currentThemeFamily == null && currentTheme == null;
            }

            if (currentThemeFamily != null)
            {
                return UsesThemeGraphAsset(currentThemeFamily.LightTheme, asset)
                    || UsesThemeGraphAsset(currentThemeFamily.DarkTheme, asset);
            }

            return UsesThemeGraphAsset(currentTheme, asset)
                || (currentTheme == null && asset is DeucarianColorRole);
        }

        /// <summary>Applies the current theme to child components implementing <see cref="IDeucarianThemeTarget"/>.</summary>
        public void ApplyThemeToChildren()
        {
            ApplyThemeToChildren(includeInactiveChildren);
        }

        /// <summary>Applies the current theme to child components implementing <see cref="IDeucarianThemeTarget"/>.</summary>
        public void ApplyThemeToChildren(bool includeInactive)
        {
            ApplyThemeToChildren(ResolveCurrentTheme(true), includeInactive);
        }

        private void ApplyThemeToChildren(DeucarianTheme theme, bool includeInactive)
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(includeInactive);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour is IDeucarianThemeTarget target)
                {
                    target.ApplyTheme(theme);
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
            themeMode = NormalizeThemeMode(themeMode);
            warnedIncompleteThemeFamily = false;
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

        private DeucarianTheme ResolveCurrentTheme(bool warnWhenIncomplete)
        {
            if (currentThemeFamily == null)
            {
                warnedIncompleteThemeFamily = false;
                return currentTheme;
            }

            if (currentThemeFamily.IsComplete)
            {
                warnedIncompleteThemeFamily = false;
                return currentThemeFamily.GetTheme(themeMode);
            }

            DeucarianTheme resolvedTheme = currentThemeFamily.ResolveTheme(themeMode);
            if (warnWhenIncomplete && !warnedIncompleteThemeFamily)
            {
                warnedIncompleteThemeFamily = true;
                bool missingLight = currentThemeFamily.LightTheme == null;
                bool missingDark = currentThemeFamily.DarkTheme == null;
                string missingVariant = missingLight && missingDark
                    ? "light and dark"
                    : missingLight ? "light" : "dark";
                string fallbackMessage = resolvedTheme != null
                    ? " The available variant will be used as a runtime fallback."
                    : " No runtime fallback is available.";
                ThemingLog.General.Warning(
                    "Theme family '"
                    + currentThemeFamily.name
                    + "' is incomplete because its "
                    + missingVariant
                    + " theme is not assigned."
                    + fallbackMessage,
                    this);
            }

            return resolvedTheme;
        }

        private static bool UsesThemeGraphAsset(DeucarianTheme theme, UnityObject asset)
        {
            if (theme == null || asset == null)
            {
                return false;
            }

            if (asset == theme || asset == theme.VisualStyle)
            {
                return true;
            }

            DeucarianColorPalette palette = theme.ColorPalette;
            if (asset == palette)
            {
                return true;
            }

            if (palette == null)
            {
                return false;
            }

            return asset == palette.RoleLibrary || asset is DeucarianColorRole;
        }

        private static DeucarianThemeMode NormalizeThemeMode(DeucarianThemeMode mode)
        {
            return mode == DeucarianThemeMode.Light
                ? DeucarianThemeMode.Light
                : DeucarianThemeMode.Dark;
        }
    }
}
