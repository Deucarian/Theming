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

        /// <summary>Resolves the runtime default theme from settings, or null when none is configured.</summary>
        public static DeucarianTheme ResolveDefaultTheme(UnityObject context = null)
        {
            DeucarianThemeRuntimeSettings settings = LoadSettings();
            if (settings != null && settings.DefaultTheme != null)
            {
                return settings.DefaultTheme;
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
                    "Deucarian runtime theme settings has no default theme assigned.",
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

            if (provider.CurrentTheme != null)
            {
                return true;
            }

            DeucarianTheme theme = ResolveDefaultTheme(context);
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
