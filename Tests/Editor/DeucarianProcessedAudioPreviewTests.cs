using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianProcessedAudioPreviewTests
    {
        [Test]
        public void ProcessedAuditionBorrowsOriginalClipAndAppliesGainPitchWithoutSceneChanges()
        {
            var clip = AudioClip.Create("Original", 8000, 1, 8000, false);
            var scene = SceneManager.GetActiveScene();
            bool dirty = scene.isDirty;
            using var preview = new DeucarianAudioPreviewSource();
            try
            {
                preview.Play(clip, .4f, 1.5f);
                AudioSource source = preview.Source;
                Assert.That(source.clip, Is.SameAs(clip));
                Assert.That(source.volume, Is.EqualTo(.4f));
                Assert.That(source.pitch, Is.EqualTo(1.5f).Within(.001));
                Assert.That(source.spatialBlend, Is.Zero);
                Assert.That(source.playOnAwake, Is.False);
                Assert.That(source.ignoreListenerPause, Is.True);
                Assert.That(source.gameObject.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
                Assert.That(scene.isDirty, Is.EqualTo(dirty));
                preview.Dispose(); preview.Dispose();
                Assert.That(source == null, Is.True);
                Assert.That(clip != null, Is.True, "The source asset is borrowed, not owned.");
            }
            finally { Object.DestroyImmediate(clip); }
        }

        [Test]
        public void RepeatedPreviewReleasesItsPreviousSourceAndSanitizesModifiers()
        {
            var clip = AudioClip.Create("Original", 8000, 1, 8000, false);
            using var preview = new DeucarianAudioPreviewSource();
            try
            {
                preview.Play(clip, .2f, 1);
                AudioSource first = preview.Source;
                preview.Play(clip, 3, float.NaN);
                Assert.That(first == null, Is.True);
                Assert.That(preview.Source.volume, Is.EqualTo(1));
                Assert.That(preview.Source.pitch, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(clip); }
        }

        [UnityTest]
        public IEnumerator NativeProcessedPreviewProducesAudioOutsidePlayMode()
        {
            if (Application.isBatchMode) Assert.Ignore("Run this audio-output check in a non-batch editor with an audio device.");
            Assert.That(Application.isPlaying, Is.False);
            var clip = AudioClip.Create("Audition test tone", 44100, 1, 44100, false);
            var pcm = new float[44100];
            for (int i = 0; i < pcm.Length; i++) pcm[i] = Mathf.Sin(i * 440 * 2 * Mathf.PI / 44100) * .1f;
            clip.SetData(pcm, 0);
            using var preview = new DeucarianAudioPreviewSource();
            try
            {
                preview.Play(clip, .5f, 1.2f);
                var output = new float[512];
                bool advanced = false;
                float peak = 0;
                float mixedPeak = 0;
                double until = EditorApplication.timeSinceStartup + 2;
                while (EditorApplication.timeSinceStartup < until && preview.Source != null)
                {
                    advanced |= preview.Source.timeSamples > 0;
                    preview.Source.GetOutputData(output, 0);
                    foreach (float sample in output) peak = Mathf.Max(peak, Mathf.Abs(sample));
                    AudioListener.GetOutputData(output, 0);
                    foreach (float sample in output) mixedPeak = Mathf.Max(mixedPeak, Mathf.Abs(sample));
                    yield return null;
                }
                Assert.That(advanced, Is.True, "Editor playback must advance beyond sample zero.");
                Assert.That(peak, Is.GreaterThan(.001f), "The audio output must contain a non-silent signal.");
                Assert.That(mixedPeak, Is.GreaterThan(.001f), "The final audio mix must also contain the preview signal.");
            }
            finally { Object.DestroyImmediate(clip); }
        }
    }
}
