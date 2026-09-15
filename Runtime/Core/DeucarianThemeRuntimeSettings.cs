using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Runtime Resources asset that points builds at the project default theme.
    /// </summary>
    [CreateAssetMenu(
        fileName = ResourceName,
        menuName = "Deucarian/Theming/Runtime Settings")]
    public sealed class DeucarianThemeRuntimeSettings : ScriptableObject
    {
        public const string ResourceName = "DeucarianThemeRuntimeSettings";

        [SerializeField] private DeucarianTheme defaultTheme;
        [SerializeField] private DeucarianThemeFamily defaultThemeFamily;
        [SerializeField] private DeucarianThemeMode defaultThemeMode = DeucarianThemeMode.Dark;
        [SerializeField] private bool useVisualStyling = true;
        [SerializeField] private bool useAudio = true;
        [SerializeField] private DeucarianAudioPaletteSet defaultAudioPaletteSet;
        [SerializeField] private DeucarianAudioExperience defaultAudioExperience;

        public bool UseVisualStyling => useVisualStyling;
        public bool UseAudio => useAudio;
        public DeucarianAudioPaletteSet DefaultAudioPaletteSet => defaultAudioPaletteSet;
        public DeucarianAudioExperience DefaultAudioExperience => defaultAudioExperience;

        public void SetFeatures(bool visualStyling, bool audio)
        {
            if (useVisualStyling == visualStyling && useAudio == audio) return;
            useVisualStyling = visualStyling;
            useAudio = audio;
            NotifyChanged();
        }

        public void ConfigureAudio(DeucarianAudioPaletteSet paletteSet, DeucarianAudioExperience experience)
        {
            if (!System.Enum.IsDefined(typeof(DeucarianAudioExperience), experience))
                throw new System.ArgumentOutOfRangeException(nameof(experience));
            if (defaultAudioPaletteSet == paletteSet && defaultAudioExperience == experience) return;
            defaultAudioPaletteSet = paletteSet;
            defaultAudioExperience = experience;
            NotifyChanged();
        }

        /// <summary>Resolved concrete theme used when a runtime provider or target needs a project default.</summary>
        public DeucarianTheme DefaultTheme => ResolvedDefaultTheme;

        /// <summary>Legacy standalone backing theme, or null when a paired family is configured.</summary>
        public DeucarianTheme LegacyDefaultTheme => defaultTheme;

        /// <summary>Paired family used as the project default, or null for legacy standalone configuration.</summary>
        public DeucarianThemeFamily DefaultThemeFamily => defaultThemeFamily;

        /// <summary>Initial explicit mode used to resolve the project default family.</summary>
        public DeucarianThemeMode DefaultThemeMode => defaultThemeMode;

        /// <summary>Resolved concrete project default, including incomplete-family and legacy fallback.</summary>
        public DeucarianTheme ResolvedDefaultTheme
        {
            get
            {
                if (defaultThemeFamily != null)
                {
                    DeucarianTheme familyTheme = defaultThemeFamily.ResolveTheme(defaultThemeMode);
                    if (familyTheme != null)
                    {
                        return familyTheme;
                    }
                }

                return defaultTheme;
            }
        }

        /// <summary>Configures the runtime default theme.</summary>
        public void Configure(DeucarianTheme theme)
        {
            defaultTheme = theme;
            defaultThemeFamily = null;
            NotifyChanged();
        }

        /// <summary>Configures a paired runtime default and its explicit initial mode.</summary>
        public void Configure(DeucarianThemeFamily family, DeucarianThemeMode mode)
        {
            defaultTheme = null;
            defaultThemeFamily = family;
            defaultThemeMode = NormalizeThemeMode(mode);
            NotifyChanged();
        }

        /// <summary>Configures a paired runtime default, using dark mode unless specified.</summary>
        public void ConfigureThemeFamily(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode = DeucarianThemeMode.Dark)
        {
            Configure(family, mode);
        }

        /// <summary>Changes the initial mode used by the configured project default family.</summary>
        public void SetDefaultThemeMode(DeucarianThemeMode mode)
        {
            mode = NormalizeThemeMode(mode);
            if (defaultThemeMode == mode)
            {
                return;
            }

            defaultThemeMode = mode;
            NotifyChanged();
        }

        private void OnValidate()
        {
            defaultThemeMode = NormalizeThemeMode(defaultThemeMode);
            if (!System.Enum.IsDefined(typeof(DeucarianAudioExperience), defaultAudioExperience))
                defaultAudioExperience = DeucarianAudioExperience.Default;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }

        private static DeucarianThemeMode NormalizeThemeMode(DeucarianThemeMode mode)
        {
            return mode == DeucarianThemeMode.Light
                ? DeucarianThemeMode.Light
                : DeucarianThemeMode.Dark;
        }
    }
}
