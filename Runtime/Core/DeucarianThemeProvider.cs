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
#if UNITY_EDITOR
        [NonSerialized] private bool hasEditorPreview;
        [NonSerialized] private DeucarianThemeFamily editorPreviewThemeFamily;
        [NonSerialized] private DeucarianThemeMode editorPreviewThemeMode;
        [NonSerialized] private DeucarianThemeStyle editorPreviewStyleOverride;
#endif

        /// <summary>
        /// Last enabled provider. This is a convenience fallback, not a required singleton.
        /// </summary>
        public static DeucarianThemeProvider Active { get; private set; }

        /// <summary>Currently active concrete theme for this provider.</summary>
        public DeucarianTheme CurrentTheme => ResolveCurrentTheme(true);

        /// <summary>Currently assigned paired theme family, or null when using a standalone theme.</summary>
        public DeucarianThemeFamily CurrentThemeFamily
        {
            get
            {
#if UNITY_EDITOR
                if (hasEditorPreview)
                {
                    return editorPreviewThemeFamily;
                }
#endif
                return currentThemeFamily;
            }
        }

        /// <summary>Mode used to resolve the assigned theme family.</summary>
        public DeucarianThemeMode ThemeMode
        {
            get
            {
#if UNITY_EDITOR
                if (hasEditorPreview)
                {
                    return editorPreviewThemeMode;
                }
#endif
                return themeMode;
            }
        }

        /// <summary>Explicit style override. When null, the current theme's visual style is used.</summary>
        public DeucarianThemeStyle StyleOverride
        {
            get
            {
#if UNITY_EDITOR
                if (hasEditorPreview)
                {
                    return editorPreviewStyleOverride;
                }
#endif
                return styleOverride;
            }
        }

        /// <summary>Resolved active visual style for this provider.</summary>
        public DeucarianThemeStyle CurrentStyle
        {
            get
            {
                DeucarianThemeStyle effectiveOverride = StyleOverride;
                if (effectiveOverride != null)
                {
                    return effectiveOverride;
                }

                DeucarianTheme theme = CurrentTheme;
                return theme != null ? theme.VisualStyle : null;
            }
        }

        internal DeucarianTheme ConfiguredTheme => ResolveConfiguredTheme(true);

        internal DeucarianThemeFamily ConfiguredThemeFamily => currentThemeFamily;

        internal DeucarianThemeMode ConfiguredThemeMode => themeMode;

        internal DeucarianThemeStyle ConfiguredStyleOverride => styleOverride;

        internal DeucarianThemeStyle ConfiguredStyle
        {
            get
            {
                if (styleOverride != null)
                {
                    return styleOverride;
                }

                DeucarianTheme theme = ResolveConfiguredTheme(true);
                return theme != null ? theme.VisualStyle : null;
            }
        }

#if UNITY_EDITOR
        internal bool HasEditorPreview => hasEditorPreview;
#endif

        /// <summary>Raised after this provider changes theme and reapplies child targets.</summary>
        public event Action<DeucarianTheme> ThemeChanged;

        /// <summary>Raised after this provider changes mode and reapplies child targets.</summary>
        public event Action<DeucarianThemeMode> ThemeModeChanged;

        /// <summary>Raised after this provider changes resolved style and reapplies child style targets.</summary>
        public event Action<DeucarianThemeStyle> StyleChanged;

        /// <summary>Sets the active theme and reapplies it to child theme targets.</summary>
        public void SetTheme(DeucarianTheme theme)
        {
#if UNITY_EDITOR
            DeucarianThemeMode previousEffectiveMode = ThemeMode;
            ClearEditorPreviewState();
#endif
            if (currentThemeFamily == null && currentTheme == theme)
            {
                RefreshThemeGraph();
#if UNITY_EDITOR
                NotifyThemeModeChangedIfNeeded(previousEffectiveMode);
#endif
                return;
            }

            currentTheme = theme;
            currentThemeFamily = null;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            RefreshThemeGraph();
#if UNITY_EDITOR
            NotifyThemeModeChangedIfNeeded(previousEffectiveMode);
#endif
        }

        /// <summary>Sets the paired theme family and reapplies its current mode to child targets.</summary>
        public void SetThemeFamily(DeucarianThemeFamily family)
        {
            SetThemeFamily(family, themeMode);
        }

        /// <summary>Atomically sets a paired family and mode, then reapplies the resolved variant once.</summary>
        public void SetThemeFamily(DeucarianThemeFamily family, DeucarianThemeMode mode)
        {
            DeucarianThemeMode previousEffectiveMode = ThemeMode;
#if UNITY_EDITOR
            ClearEditorPreviewState();
#endif
            mode = NormalizeThemeMode(mode);
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
            if (previousEffectiveMode != ThemeMode)
            {
                ThemeModeChanged?.Invoke(themeMode);
            }
        }

        /// <summary>Sets the explicit light or dark mode and reapplies the resolved family variant.</summary>
        public void SetThemeMode(DeucarianThemeMode mode)
        {
            DeucarianThemeMode previousEffectiveMode = ThemeMode;
#if UNITY_EDITOR
            ClearEditorPreviewState();
#endif
            mode = NormalizeThemeMode(mode);
            if (themeMode == mode)
            {
                RefreshThemeGraph();
                if (previousEffectiveMode != ThemeMode)
                {
                    ThemeModeChanged?.Invoke(themeMode);
                }
                return;
            }

            themeMode = mode;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            RefreshThemeGraph();
            if (previousEffectiveMode != ThemeMode)
            {
                ThemeModeChanged?.Invoke(themeMode);
            }
        }

        /// <summary>Sets the provider style override and reapplies it to child style targets.</summary>
        public void SetStyle(DeucarianThemeStyle style)
        {
#if UNITY_EDITOR
            DeucarianThemeMode previousEffectiveMode = ThemeMode;
            bool previewCleared = ClearEditorPreviewState();
#else
            const bool previewCleared = false;
#endif
            if (styleOverride == style)
            {
                if (previewCleared)
                {
                    RefreshThemeGraph();
                }
                else
                {
                    RefreshStyle();
                }
#if UNITY_EDITOR
                NotifyThemeModeChangedIfNeeded(previousEffectiveMode);
#endif
                return;
            }

            styleOverride = style;
            if (isActiveAndEnabled)
            {
                Active = this;
            }

            if (previewCleared)
            {
                RefreshThemeGraph();
            }
            else
            {
                ApplyResolvedStyleAndNotify();
            }
#if UNITY_EDITOR
            NotifyThemeModeChangedIfNeeded(previousEffectiveMode);
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Applies an editor-only presentation without changing the serialized provider configuration.
        /// Public runtime setters clear this preview so later application behavior always wins.
        /// </summary>
        internal void SetEditorPreview(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            DeucarianThemeStyle style)
        {
            if (family == null)
            {
                ClearEditorPreview();
                return;
            }

            DeucarianThemeMode previousEffectiveMode = ThemeMode;
            editorPreviewThemeFamily = family;
            editorPreviewThemeMode = NormalizeThemeMode(mode);
            DeucarianTheme resolvedTheme = family.ResolveTheme(editorPreviewThemeMode);
            editorPreviewStyleOverride = resolvedTheme != null && resolvedTheme.VisualStyle == style
                ? null
                : style;
            hasEditorPreview = true;
            warnedIncompleteThemeFamily = false;

            RefreshThemeGraph();
            NotifyThemeModeChangedIfNeeded(previousEffectiveMode);
        }

        internal bool ClearEditorPreview()
        {
            DeucarianThemeMode previousEffectiveMode = ThemeMode;
            if (!ClearEditorPreviewState())
            {
                return false;
            }

            RefreshThemeGraph();
            NotifyThemeModeChangedIfNeeded(previousEffectiveMode);
            return true;
        }

        private bool ClearEditorPreviewState()
        {
            if (!hasEditorPreview)
            {
                return false;
            }

            hasEditorPreview = false;
            editorPreviewThemeFamily = null;
            editorPreviewThemeMode = DeucarianThemeMode.Dark;
            editorPreviewStyleOverride = null;
            warnedIncompleteThemeFamily = false;
            return true;
        }

        private void NotifyThemeModeChangedIfNeeded(DeucarianThemeMode previousMode)
        {
            if (previousMode != ThemeMode)
            {
                ThemeModeChanged?.Invoke(ThemeMode);
            }
        }
#endif

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

            DeucarianThemeFamily effectiveFamily = CurrentThemeFamily;
            DeucarianThemeStyle effectiveStyleOverride = StyleOverride;
            DeucarianThemeStyle currentStyle = CurrentStyle;
            if (asset == effectiveFamily || asset == effectiveStyleOverride || asset == currentStyle)
            {
                return true;
            }

            if (currentStyle != null && currentStyle.UsesComponentAsset(asset))
            {
                return true;
            }

            if (asset is DeucarianThemeRuntimeSettings)
            {
                return currentThemeFamily == null && currentTheme == null;
            }

            if (effectiveFamily != null)
            {
                return UsesThemeGraphAsset(effectiveFamily.LightTheme, asset)
                    || UsesThemeGraphAsset(effectiveFamily.DarkTheme, asset);
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
#if UNITY_EDITOR
            ClearEditorPreviewState();
#endif
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
#if UNITY_EDITOR
            if (hasEditorPreview)
            {
                return ResolveThemeSource(
                    null,
                    editorPreviewThemeFamily,
                    editorPreviewThemeMode,
                    warnWhenIncomplete);
            }
#endif
            return ResolveConfiguredTheme(warnWhenIncomplete);
        }

        private DeucarianTheme ResolveConfiguredTheme(bool warnWhenIncomplete)
        {
            return ResolveThemeSource(currentTheme, currentThemeFamily, themeMode, warnWhenIncomplete);
        }

        private DeucarianTheme ResolveThemeSource(
            DeucarianTheme standaloneTheme,
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            bool warnWhenIncomplete)
        {
            if (family == null)
            {
                warnedIncompleteThemeFamily = false;
                return standaloneTheme;
            }

            if (family.IsComplete)
            {
                warnedIncompleteThemeFamily = false;
                return family.GetTheme(mode);
            }

            DeucarianTheme resolvedTheme = family.ResolveTheme(mode);
            if (warnWhenIncomplete && !warnedIncompleteThemeFamily)
            {
                warnedIncompleteThemeFamily = true;
                bool missingLight = family.LightTheme == null;
                bool missingDark = family.DarkTheme == null;
                string missingVariant = missingLight && missingDark
                    ? "light and dark"
                    : missingLight ? "light" : "dark";
                string fallbackMessage = resolvedTheme != null
                    ? " The available variant will be used as a runtime fallback."
                    : " No runtime fallback is available.";
                ThemingLog.General.Warning(
                    "Theme family '"
                    + family.name
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

            if (theme.VisualStyle != null && theme.VisualStyle.UsesComponentAsset(asset))
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
