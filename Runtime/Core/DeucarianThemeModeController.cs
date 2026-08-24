using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Persistence boundary used by the reusable theme-mode controller.</summary>
    public interface IDeucarianThemeModeStore
    {
        bool TryLoad(out DeucarianThemeMode mode);

        void Save(DeucarianThemeMode mode);
    }

    /// <summary>PlayerPrefs-backed theme-mode persistence with a consumer-selected key.</summary>
    public sealed class DeucarianPlayerPrefsThemeModeStore :
        IDeucarianThemeModeStore
    {
        public DeucarianPlayerPrefsThemeModeStore(string preferenceKey)
        {
            PreferenceKey = string.IsNullOrWhiteSpace(preferenceKey)
                ? DeucarianThemeModeController.DefaultPreferenceKey
                : preferenceKey.Trim();
        }

        public string PreferenceKey { get; }

        public bool TryLoad(out DeucarianThemeMode mode)
        {
            if (!PlayerPrefs.HasKey(PreferenceKey))
            {
                mode = DeucarianViewerReferenceThemePreset.DefaultMode;
                return false;
            }

            int storedValue = PlayerPrefs.GetInt(PreferenceKey, int.MinValue);
            if (storedValue == (int)DeucarianThemeMode.Light ||
                storedValue == (int)DeucarianThemeMode.Dark)
            {
                mode = (DeucarianThemeMode)storedValue;
                return true;
            }

            mode = DeucarianViewerReferenceThemePreset.DefaultMode;
            return false;
        }

        public void Save(DeucarianThemeMode mode)
        {
            if (!IsValid(mode))
            {
                return;
            }

            PlayerPrefs.SetInt(PreferenceKey, (int)mode);
            PlayerPrefs.Save();
        }

        private static bool IsValid(DeucarianThemeMode mode)
        {
            return mode == DeucarianThemeMode.Light ||
                   mode == DeucarianThemeMode.Dark;
        }
    }

    /// <summary>
    /// Owns a viewer provider's persisted light/dark selection without owning
    /// application or product state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeucarianThemeModeController : MonoBehaviour
    {
        public const string DefaultPreferenceKey =
            "deucarian.viewer.theme.mode";

        private DeucarianThemeProvider provider;
        private IDeucarianThemeModeStore store;
        private bool isBound;
        private bool isListening;
        private bool suppressProviderPersistence;

        public DeucarianThemeProvider Provider => provider;

        /// <summary>Binds the package-owned reference family and persistence.</summary>
        public void BindReferenceTheme(
            DeucarianThemeProvider targetProvider,
            string preferenceKey = DefaultPreferenceKey)
        {
            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();
            Bind(
                targetProvider,
                profile.ThemeFamily,
                DeucarianViewerReferenceThemePreset.DefaultMode,
                new DeucarianPlayerPrefsThemeModeStore(preferenceKey));
        }

        /// <summary>Binds an explicit family and persistence boundary.</summary>
        public void Bind(
            DeucarianThemeProvider targetProvider,
            DeucarianThemeFamily family,
            DeucarianThemeMode defaultMode,
            IDeucarianThemeModeStore modeStore)
        {
            Unsubscribe();
            provider = targetProvider;
            store = modeStore ??
                new DeucarianPlayerPrefsThemeModeStore(
                    DefaultPreferenceKey);
            isBound = provider != null;
            if (!isBound)
            {
                return;
            }

            DeucarianThemeMode fallback = Normalize(defaultMode);
            DeucarianThemeMode restored;
            if (!store.TryLoad(out restored) || !IsValid(restored))
            {
                restored = fallback;
            }

            if (family != null)
            {
                provider.SetThemeFamily(family, restored);
            }
            else
            {
                provider.SetThemeMode(restored);
            }

            Subscribe();
        }

        /// <summary>Changes and persists the selected theme mode.</summary>
        public bool SetThemeMode(DeucarianThemeMode mode)
        {
            if (provider == null || !IsValid(mode))
            {
                return false;
            }

            if (provider.ThemeMode == mode)
            {
                return true;
            }

            suppressProviderPersistence = true;
            try
            {
                provider.SetThemeMode(mode);
            }
            finally
            {
                suppressProviderPersistence = false;
            }

            store.Save(mode);
            return true;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            provider = null;
            store = null;
            isBound = false;
        }

        private void Subscribe()
        {
            if (!isBound || provider == null || isListening ||
                !isActiveAndEnabled)
            {
                return;
            }

            provider.ThemeModeChanged += OnProviderThemeModeChanged;
            isListening = true;
        }

        private void Unsubscribe()
        {
            if (!isListening)
            {
                return;
            }

            if (provider != null)
            {
                provider.ThemeModeChanged -= OnProviderThemeModeChanged;
            }

            isListening = false;
        }

        private void OnProviderThemeModeChanged(DeucarianThemeMode mode)
        {
            if (!suppressProviderPersistence && store != null && IsValid(mode))
            {
                store.Save(mode);
            }
        }

        private static DeucarianThemeMode Normalize(DeucarianThemeMode mode)
        {
            return IsValid(mode)
                ? mode
                : DeucarianViewerReferenceThemePreset.DefaultMode;
        }

        private static bool IsValid(DeucarianThemeMode mode)
        {
            return mode == DeucarianThemeMode.Light ||
                   mode == DeucarianThemeMode.Dark;
        }
    }
}
