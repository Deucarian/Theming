using System;
using Deucarian.Media.Unity;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianAudioPreviewBuffer
    {
        private const int MaximumSamples = 8 * 1024 * 1024;

        public static AudioClip Create(AudioClip source, float volume, float pitch)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.loadType != AudioClipLoadType.DecompressOnLoad)
                throw new InvalidOperationException("Processed audition needs Decompress On Load in the clip's import settings. Original audition remains available.");
            if (source.loadState != AudioDataLoadState.Loaded)
            {
                source.LoadAudioData();
                if (source.loadState != AudioDataLoadState.Loaded)
                    throw new InvalidOperationException("Audio data is loading. Try processed audition again in a moment.");
            }
            var modifiers = DeucarianAudioPlaybackModifiers.Identity;
            pitch = modifiers.ApplyPitch(pitch);
            volume = modifiers.ApplyVolume(volume);
            long inputCount = (long)source.samples * source.channels;
            int frames = Math.Max(1, Mathf.CeilToInt(source.samples / pitch));
            long outputCount = (long)frames * source.channels;
            if (inputCount > MaximumSamples || outputCount > MaximumSamples)
                throw new InvalidOperationException("This clip is too long for bounded SFX processing. Use Original audition or a shorter sound.");
            var input = new float[(int)inputCount];
            if (!source.GetData(input, 0)) throw new InvalidOperationException("Unity could not read this clip's audio samples. Use Original audition or check its import settings.");
            float[] output = Resample(input, source.channels, pitch, volume, frames);
            var result = AudioClip.Create(source.name + " (processed preview)", frames, source.channels, source.frequency, false);
            result.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                if (result.SetData(output, 0)) return result;
                throw new InvalidOperationException("Unity could not create the processed preview.");
            }
            catch
            {
                UnityMediaResourceLease.CreateOwned(result).Dispose();
                throw;
            }
        }

        internal static float[] Resample(float[] input, int channels, float pitch, float volume, int frames)
        {
            var result = new float[frames * channels];
            int sourceFrames = input.Length / channels;
            for (int frame = 0; frame < frames; frame++)
            {
                float position = Math.Min(frame * pitch, sourceFrames - 1);
                int left = (int)position;
                int right = Math.Min(left + 1, sourceFrames - 1);
                for (int channel = 0; channel < channels; channel++)
                    result[frame * channels + channel] = Mathf.LerpUnclamped(input[left * channels + channel],
                        input[right * channels + channel], position - left) * volume;
            }
            return result;
        }
    }
}
