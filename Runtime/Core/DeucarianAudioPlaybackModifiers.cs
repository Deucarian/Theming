using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Per-playback volume and pitch multipliers supplied by an interaction source.
    /// The palette remains the source of the base cue values.
    /// </summary>
    [Serializable]
    public readonly struct DeucarianAudioPlaybackModifiers
    {
        private readonly float volumeScale;
        private readonly float pitchScale;
        private readonly bool configured;

        public DeucarianAudioPlaybackModifiers(float playbackVolumeScale, float playbackPitchScale)
        {
            volumeScale = SanitizePositive(playbackVolumeScale);
            pitchScale = SanitizePositive(playbackPitchScale);
            configured = true;
        }

        /// <summary>A safe no-op modifier. A default-initialized value is also identity.</summary>
        public static DeucarianAudioPlaybackModifiers Identity =>
            new DeucarianAudioPlaybackModifiers(1f, 1f);

        public float VolumeScale => configured ? volumeScale : 1f;
        public float PitchScale => configured ? pitchScale : 1f;

        /// <summary>
        /// Maps normalized interaction intensity, such as key press velocity, to playback scales.
        /// </summary>
        public static DeucarianAudioPlaybackModifiers FromIntensity(
            float normalizedIntensity,
            float minimumVolumeScale = 0.7f,
            float maximumVolumeScale = 1f,
            float minimumPitchScale = 0.97f,
            float maximumPitchScale = 1.03f)
        {
            float intensity = IsFinite(normalizedIntensity)
                ? Mathf.Clamp01(normalizedIntensity)
                : 0.5f;
            return new DeucarianAudioPlaybackModifiers(
                Mathf.Lerp(minimumVolumeScale, maximumVolumeScale, intensity),
                Mathf.Lerp(minimumPitchScale, maximumPitchScale, intensity));
        }

        public float ApplyVolume(float baseVolume)
        {
            float value = IsFinite(baseVolume) ? Mathf.Max(0f, baseVolume) : 1f;
            return Mathf.Clamp01(value * VolumeScale);
        }

        public float ApplyPitch(float basePitch)
        {
            float value = IsFinite(basePitch) && basePitch > 0f ? basePitch : 1f;
            return Mathf.Clamp(value * PitchScale, 0.1f, 3f);
        }

        private static float SanitizePositive(float value)
        {
            return IsFinite(value) && value >= 0f ? value : 1f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
