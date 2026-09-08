using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianProcessedAudioPreviewTests
    {
        [Test]
        public void ProcessedAuditionAppliesGainAndPitchWithoutChangingTheOriginalClip()
        {
            var source = AudioClip.Create("Original", 16, 1, 8000, false);
            var input = new float[16];
            for (int i = 0; i < input.Length; i++) input[i] = 0.8f;
            source.SetData(input, 0);
            AudioClip processed = null;
            try
            {
                processed = DeucarianAudioPreviewBuffer.Create(source, 0.5f, 2f);
                Assert.That(processed.samples, Is.EqualTo(8));
                var output = new float[8];
                processed.GetData(output, 0);
                foreach (float sample in output) Assert.That(sample, Is.EqualTo(0.4f).Within(0.0001f));
                source.GetData(input, 0);
                foreach (float sample in input) Assert.That(sample, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.That(processed.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
            }
            finally { Object.DestroyImmediate(processed); Object.DestroyImmediate(source); }
        }

        [Test]
        public void ResamplingKeepsStereoChannelsSeparate()
        {
            float[] output = DeucarianAudioPreviewBuffer.Resample(new[] { 1f, 0f, 0f, 1f }, 2, 0.5f, 0.5f, 4);
            Assert.That(output[0], Is.EqualTo(0.5f));
            Assert.That(output[1], Is.Zero);
            Assert.That(output[2], Is.EqualTo(0.25f));
            Assert.That(output[3], Is.EqualTo(0.25f));
        }
    }
}
