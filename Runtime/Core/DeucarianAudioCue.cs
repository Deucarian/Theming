using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Designer-authored variants and playback values for one semantic role.</summary>
    [Serializable]
    public sealed class DeucarianAudioCue
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private List<AudioClip> variants = new List<AudioClip>();
        [SerializeField] private float volume = 1f;
        [SerializeField] private float minimumPitch = 1f;
        [SerializeField] private float maximumPitch = 1f;
        [SerializeField] private bool intentionalSilence;

        /// <summary>Compatibility primary clip. Prefer variant selection for playback.</summary>
        public AudioClip Clip => clip != null ? clip : FirstUsableVariant();

        public IReadOnlyList<AudioClip> Variants => variants ?? (IReadOnlyList<AudioClip>)Array.Empty<AudioClip>();

        /// <summary>Number of non-null clips available for variant selection.</summary>
        public int UsableVariantCount => CountUsableVariants();
        public float Volume => IsFinite(volume) ? Mathf.Max(0f, volume) : 1f;
        public float MinimumPitch => SanitizePitch(Mathf.Min(minimumPitch, maximumPitch));
        public float MaximumPitch => SanitizePitch(Mathf.Max(minimumPitch, maximumPitch));
        public bool IntentionalSilence => intentionalSilence;
        public bool HasClip => CountUsableVariants() > 0;

        public static DeucarianAudioCue Empty => new DeucarianAudioCue(null, 1f);

        public static DeucarianAudioCue Silent()
        {
            DeucarianAudioCue cue = new DeucarianAudioCue();
            cue.intentionalSilence = true;
            return cue;
        }

        public DeucarianAudioCue()
        {
        }

        public DeucarianAudioCue(AudioClip cueClip, float cueVolume = 1f)
        {
            Configure(cueClip, cueVolume);
        }

        public DeucarianAudioCue(
            IEnumerable<AudioClip> cueVariants,
            float cueVolume,
            float pitchMinimum = 1f,
            float pitchMaximum = 1f,
            bool intentionallySilent = false)
        {
            Configure(cueVariants, cueVolume, pitchMinimum, pitchMaximum, intentionallySilent);
        }

        public void Configure(AudioClip cueClip, float cueVolume = 1f)
        {
            clip = cueClip;
            variants = variants ?? new List<AudioClip>();
            variants.Clear();
            volume = SanitizeVolume(cueVolume);
            minimumPitch = 1f;
            maximumPitch = 1f;
            intentionalSilence = false;
        }

        public void Configure(
            IEnumerable<AudioClip> cueVariants,
            float cueVolume,
            float pitchMinimum = 1f,
            float pitchMaximum = 1f,
            bool intentionallySilent = false)
        {
            clip = null;
            variants = cueVariants != null
                ? new List<AudioClip>(cueVariants)
                : new List<AudioClip>();
            volume = SanitizeVolume(cueVolume);
            minimumPitch = SanitizePitch(Mathf.Min(pitchMinimum, pitchMaximum));
            maximumPitch = SanitizePitch(Mathf.Max(pitchMinimum, pitchMaximum));
            intentionalSilence = intentionallySilent;
        }

        /// <summary>Selects a usable variant and avoids the previous raw index when possible.</summary>
        public bool TrySelectVariant(
            int selector,
            int previousRawIndex,
            out AudioClip selectedClip,
            out int selectedRawIndex)
        {
            List<int> usable = BuildUsableRawIndices();
            if (usable.Count == 0 || intentionalSilence)
            {
                selectedClip = null;
                selectedRawIndex = -1;
                return false;
            }

            int start = PositiveModulo(selector, usable.Count);
            selectedRawIndex = usable[start];
            if (usable.Count > 1 && selectedRawIndex == previousRawIndex)
            {
                selectedRawIndex = usable[(start + 1) % usable.Count];
            }

            selectedClip = GetRawVariant(selectedRawIndex);
            return selectedClip != null;
        }

        public float ResolvePitch(float normalized)
        {
            return Mathf.Lerp(MinimumPitch, MaximumPitch, Mathf.Clamp01(normalized));
        }

        public DeucarianAudioCue Clone()
        {
            List<AudioClip> all = new List<AudioClip>();
            if (clip != null)
            {
                all.Add(clip);
            }

            if (variants != null)
            {
                all.AddRange(variants);
            }

            return new DeucarianAudioCue(
                all,
                Volume,
                MinimumPitch,
                MaximumPitch,
                intentionalSilence);
        }

        public string GetValidationWarning()
        {
            if (!IsFinite(volume) || volume < 0f)
            {
                return "Volume must be a finite non-negative value.";
            }

            if (!IsFinite(minimumPitch) || !IsFinite(maximumPitch) ||
                minimumPitch <= 0f || maximumPitch <= 0f)
            {
                return "Pitch values must be finite and greater than zero.";
            }

            if (minimumPitch > maximumPitch)
            {
                return "Minimum pitch cannot be greater than maximum pitch.";
            }

            return null;
        }

        private List<int> BuildUsableRawIndices()
        {
            List<int> result = new List<int>();
            if (clip != null)
            {
                result.Add(0);
            }

            if (variants != null)
            {
                for (int i = 0; i < variants.Count; i++)
                {
                    if (variants[i] != null)
                    {
                        result.Add(i + 1);
                    }
                }
            }

            return result;
        }

        private int CountUsableVariants()
        {
            int count = clip != null ? 1 : 0;
            if (variants != null)
            {
                for (int i = 0; i < variants.Count; i++)
                {
                    if (variants[i] != null)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private AudioClip FirstUsableVariant()
        {
            if (variants == null)
            {
                return null;
            }

            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i] != null)
                {
                    return variants[i];
                }
            }

            return null;
        }

        private AudioClip GetRawVariant(int rawIndex)
        {
            if (rawIndex == 0)
            {
                return clip;
            }

            int variantIndex = rawIndex - 1;
            return variants != null && variantIndex >= 0 && variantIndex < variants.Count
                ? variants[variantIndex]
                : null;
        }

        private static int PositiveModulo(int value, int modulo)
        {
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static float SanitizeVolume(float value)
        {
            return IsFinite(value) ? Mathf.Max(0f, value) : 1f;
        }

        private static float SanitizePitch(float value)
        {
            return IsFinite(value) && value > 0f ? Mathf.Clamp(value, 0.1f, 3f) : 1f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
