using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Deucarian.Theming
{
    /// <summary>
    /// Resolves project-level runtime themes without app-specific settings assets.
    /// </summary>
    public static class DeucarianThemeRuntimeResolver
    {
        private static readonly HashSet<string> WarnedMessages = new HashSet<string>();

        /// <summary>Resources name used for the runtime theming settings asset.</summary>
        public static string RuntimeSettingsResourceName => DeucarianThemeRuntimeSettings.ResourceName;

        /// <summary>Loads the runtime theming settings from Resources.</summary>
        public static DeucarianThemeRuntimeSettings LoadSettings()
        {
            return Resources.Load<DeucarianThemeRuntimeSettings>(DeucarianThemeRuntimeSettings.ResourceName);
        }

        /// <summary>Resolves the configured runtime default family, or null for legacy standalone settings.</summary>
        public static DeucarianThemeFamily ResolveDefaultThemeFamily()
        {
            DeucarianThemeRuntimeSettings settings = LoadSettings();
            return settings != null ? settings.DefaultThemeFamily : null;
        }

        /// <summary>Resolves the configured runtime default mode, defaulting to dark when settings are absent.</summary>
        public static DeucarianThemeMode ResolveDefaultThemeMode()
        {
            DeucarianThemeRuntimeSettings settings = LoadSettings();
            return settings != null ? settings.DefaultThemeMode : DeucarianThemeMode.Dark;
        }

        /// <summary>Resolves the runtime default theme from settings, or null when none is configured.</summary>
        public static DeucarianTheme ResolveDefaultTheme(UnityObject context = null)
        {
            return ResolveDefaultThemeFromSettings(LoadSettings(), context);
        }

        private static DeucarianTheme ResolveDefaultThemeFromSettings(
            DeucarianThemeRuntimeSettings settings,
            UnityObject context)
        {
            if (settings != null && settings.DefaultThemeFamily != null)
            {
                DeucarianThemeFamily family = settings.DefaultThemeFamily;
                if (!family.IsComplete)
                {
                    bool missingLight = family.LightTheme == null;
                    bool missingDark = family.DarkTheme == null;
                    string missingVariant = missingLight && missingDark
                        ? "light and dark"
                        : missingLight ? "light" : "dark";
                    string fallbackMessage = family.ResolveTheme(settings.DefaultThemeMode) != null
                        ? " The available variant will be used as a runtime fallback."
                        : " No runtime fallback is available.";
                    WarnOnce(
                        "incomplete-default-family-" + family.GetInstanceID(),
                        "Deucarian runtime theme family '"
                        + family.name
                        + "' is incomplete because its "
                        + missingVariant
                        + " theme is not assigned."
                        + fallbackMessage,
                        context != null ? context : family);
                }

                DeucarianTheme familyTheme = family.ResolveTheme(settings.DefaultThemeMode);
                if (familyTheme != null)
                {
                    return familyTheme;
                }
            }

            if (settings != null && settings.LegacyDefaultTheme != null)
            {
                return settings.LegacyDefaultTheme;
            }

            if (settings != null && settings.DefaultThemeFamily != null)
            {
                return null;
            }

            if (settings == null)
            {
                WarnOnce(
                    "missing-runtime-settings",
                    "Deucarian runtime theme settings were not found in Resources as '"
                    + DeucarianThemeRuntimeSettings.ResourceName
                    + "'.",
                    context);
            }
            else
            {
                WarnOnce(
                    "missing-default-theme",
                    "Deucarian runtime theme settings has no usable default theme or theme family assigned.",
                    context);
            }

            return null;
        }

        /// <summary>Finds the nearest provider for a component, then falls back to the active provider.</summary>
        public static DeucarianThemeProvider FindProvider(Component context)
        {
            if (context != null)
            {
                DeucarianThemeProvider parentProvider = context.GetComponentInParent<DeucarianThemeProvider>(true);
                if (parentProvider != null)
                {
                    return parentProvider;
                }
            }

            return DeucarianThemeProvider.Active;
        }

        /// <summary>Resolves a theme from a provider, then from the runtime default settings.</summary>
        public static DeucarianTheme ResolveTheme(Component context)
        {
            DeucarianThemeProvider provider = FindProvider(context);
            if (provider != null && provider.CurrentTheme != null)
            {
                return provider.CurrentTheme;
            }

            return ResolveDefaultTheme(context);
        }

        /// <summary>Assigns the runtime default theme to an empty provider.</summary>
        public static bool EnsureProviderHasTheme(DeucarianThemeProvider provider, UnityObject context = null)
        {
            if (provider == null)
            {
                return false;
            }

            if (provider.CurrentThemeFamily != null)
            {
                return provider.CurrentTheme != null;
            }

            if (provider.CurrentTheme != null)
            {
                return true;
            }

            return EnsureProviderHasThemeFromSettings(provider, LoadSettings(), context);
        }

        private static bool EnsureProviderHasThemeFromSettings(
            DeucarianThemeProvider provider,
            DeucarianThemeRuntimeSettings settings,
            UnityObject context)
        {
            DeucarianThemeFamily family = settings != null ? settings.DefaultThemeFamily : null;
            if (family != null && family.ResolveTheme(settings.DefaultThemeMode) != null)
            {
                provider.SetThemeFamily(family, settings.DefaultThemeMode);
                return true;
            }

            DeucarianTheme theme = ResolveDefaultThemeFromSettings(settings, context);
            if (theme != null)
            {
                provider.SetTheme(theme);
            }

            return provider.CurrentTheme != null;
        }

        private static void WarnOnce(string key, string message, UnityObject context)
        {
            if (!WarnedMessages.Add(key))
            {
                return;
            }

            ThemingLog.General.Warning(message, context);
        }
    }
}
