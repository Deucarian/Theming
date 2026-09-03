using System.Collections.Generic;
using Deucarian.Media.Unity;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Resolves semantic roles and delegates one-shots to Deucarian Media.</summary>
    [DisallowMultipleComponent]
    public sealed class DeucarianThemeAudioPlayer : MonoBehaviour
    {
        [SerializeField] private DeucarianTheme themeOverride;
        [SerializeField] private DeucarianAudioPaletteSet paletteSetOverride;
        [SerializeField] private DeucarianThemeProvider themeProvider;
        [SerializeField] private UnityAudioOneShotOutput output;
        [SerializeField] private DeucarianAudioExperience experienceOverride =
            DeucarianAudioExperience.Default;
        [SerializeField] private bool useProviderExperience = true;

        private readonly Dictionary<string, int> lastVariantByRole =
            new Dictionary<string, int>();
        private System.Random random;
        private bool warnedMissingOutput;

        public DeucarianTheme ThemeOverride
        {
            get => themeOverride;
            set => themeOverride = value;
        }

        public DeucarianThemeProvider ThemeProvider
        {
            get => themeProvider;
            set => themeProvider = value;
        }

        /// <summary>Optional direct palette composition for hosts that do not own a visual theme.</summary>
        public DeucarianAudioPaletteSet PaletteSetOverride
        {
            get => paletteSetOverride;
            set => paletteSetOverride = value;
        }

        public UnityAudioOneShotOutput Output
        {
            get => output;
            set => output = value;
        }

        public DeucarianAudioExperience ExperienceOverride
        {
            get => experienceOverride;
            set => experienceOverride = value;
        }

        public bool UseProviderExperience
        {
            get => useProviderExperience;
            set => useProviderExperience = value;
        }

        public DeucarianAudioExperience CurrentExperience => ResolveExperience();

        public bool PlayRole(DeucarianAudioRole role)
        {
            return PlayRole(role, DeucarianAudioPlaybackModifiers.Identity);
        }

        public bool PlayRole(
            DeucarianAudioRole role,
            DeucarianAudioPlaybackModifiers modifiers)
        {
            if (paletteSetOverride != null && role != null &&
                paletteSetOverride.TryResolve(role, ResolveExperience(), out DeucarianAudioResolution direct))
            {
                return Play(role.Id, direct.Cue, modifiers);
            }

            DeucarianTheme theme = ResolveTheme();
            if (theme == null || role == null ||
                !theme.TryResolveAudio(role, ResolveExperience(), out DeucarianAudioResolution resolution))
            {
                return false;
            }

            return Play(role.Id, resolution.Cue, modifiers);
        }

        public bool PlayRoleById(string roleId)
        {
            return PlayRoleById(roleId, DeucarianAudioPlaybackModifiers.Identity);
        }

        public bool PlayRoleById(
            string roleId,
            DeucarianAudioPlaybackModifiers modifiers)
        {
            if (paletteSetOverride != null && !string.IsNullOrWhiteSpace(roleId) &&
                paletteSetOverride.TryResolveById(
                    roleId,
                    ResolveExperience(),
                    out DeucarianAudioResolution direct))
            {
                return Play(DeucarianAudioRole.NormalizeId(roleId), direct.Cue, modifiers);
            }

            DeucarianTheme theme = ResolveTheme();
            if (theme == null || string.IsNullOrWhiteSpace(roleId) ||
                !theme.TryResolveAudioById(
                    roleId,
                    ResolveExperience(),
                    out DeucarianAudioResolution resolution))
            {
                return false;
            }

            return Play(DeucarianAudioRole.NormalizeId(roleId), resolution.Cue, modifiers);
        }

        /// <summary>Compatibility cue playback when a caller already resolved semantics.</summary>
        public bool Play(DeucarianAudioCue cue)
        {
            return Play(cue, DeucarianAudioPlaybackModifiers.Identity);
        }

        public bool Play(
            DeucarianAudioCue cue,
            DeucarianAudioPlaybackModifiers modifiers)
        {
            return Play(string.Empty, cue, modifiers);
        }

        public bool PlayResolved(string roleId, DeucarianAudioCue cue)
        {
            return PlayResolved(roleId, cue, DeucarianAudioPlaybackModifiers.Identity);
        }

        public bool PlayResolved(
            string roleId,
            DeucarianAudioCue cue,
            DeucarianAudioPlaybackModifiers modifiers)
        {
            return Play(DeucarianAudioRole.NormalizeId(roleId), cue, modifiers);
        }

        public void StopAll()
        {
            ResolveOutput()?.StopAll();
        }

        private bool Play(
            string roleId,
            DeucarianAudioCue cue,
            DeucarianAudioPlaybackModifiers modifiers)
        {
            if (cue == null || cue.IntentionalSilence)
            {
                return false;
            }

            EnsureRandom();
            int previousIndex = lastVariantByRole.TryGetValue(roleId, out int lastIndex)
                ? lastIndex
                : -1;
            if (!cue.TrySelectVariant(
                    random.Next(),
                    previousIndex,
                    out AudioClip clip,
                    out int selectedIndex))
            {
                return false;
            }

            UnityAudioOneShotOutput resolvedOutput = ResolveOutput();
            if (resolvedOutput == null)
            {
                WarnMissingOutput();
                return false;
            }

            float pitch = modifiers.ApplyPitch(cue.ResolvePitch((float)random.NextDouble()));
            bool played = resolvedOutput.TryPlay(clip, modifiers.ApplyVolume(cue.Volume), pitch);
            if (played)
            {
                warnedMissingOutput = false;
                lastVariantByRole[roleId] = selectedIndex;
            }

            return played;
        }

        private DeucarianTheme ResolveTheme()
        {
            if (themeOverride != null)
            {
                return themeOverride;
            }

            DeucarianThemeProvider provider = ResolveProvider();
            if (provider != null && provider.CurrentTheme != null)
            {
                return provider.CurrentTheme;
            }

            return DeucarianThemeRuntimeResolver.ResolveDefaultTheme(this);
        }

        private DeucarianAudioExperience ResolveExperience()
        {
            DeucarianThemeProvider provider = ResolveProvider();
            return useProviderExperience && provider != null
                ? provider.AudioExperience
                : experienceOverride;
        }

        private DeucarianThemeProvider ResolveProvider()
        {
            if (themeProvider == null)
            {
                themeProvider = GetComponentInParent<DeucarianThemeProvider>();
            }

            return themeProvider != null ? themeProvider : DeucarianThemeProvider.Active;
        }

        private UnityAudioOneShotOutput ResolveOutput()
        {
            if (output == null)
            {
                output = GetComponent<UnityAudioOneShotOutput>();
            }

            return output;
        }

        private void WarnMissingOutput()
        {
            if (warnedMissingOutput)
            {
                return;
            }

            warnedMissingOutput = true;
            ThemingLog.General.Warning(
                "Themed audio player needs a Deucarian Media UnityAudioOneShotOutput.",
                this);
        }

        private void EnsureRandom()
        {
            if (random == null)
            {
                random = new System.Random(GetInstanceID());
            }
        }

        private void Reset()
        {
            themeProvider = GetComponentInParent<DeucarianThemeProvider>();
            output = GetComponent<UnityAudioOneShotOutput>();
        }
    }
}
