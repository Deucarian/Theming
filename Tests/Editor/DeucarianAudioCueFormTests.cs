using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianAudioCueFormTests
    {
        private string path;
        private DeucarianAudioPalette palette;
        private DeucarianAudioRole role;
        private AudioClip clip;
        private DeucarianAudioCueForm form;
        private VisualElement root;
        private CueTestWindow window;

        [SetUp]
        public void SetUp()
        {
            Assert.That(DeucarianThemeRuntimeResolver.LoadSettings(), Is.Null, "Use an isolated test consumer.");
            path = "Assets/AudioCueForm-" + System.Guid.NewGuid().ToString("N") + ".asset";
            role = ScriptableObject.CreateInstance<DeucarianAudioRole>();
            role.Configure("test.audio-form", "Test", "UI", "", DeucarianAudioCue.Empty, false);
            clip = AudioClip.Create("Test cue", 64, 1, 8000, false);
            palette = ScriptableObject.CreateInstance<DeucarianAudioPalette>();
            palette.AddEntry(role, new DeucarianAudioCue(new[] { clip }, .4f, .9f, 1.1f), "Keep my note");
            AssetDatabase.CreateAsset(palette, path);
            window = ScriptableObject.CreateInstance<CueTestWindow>();
            window.Show();
            root = window.rootVisualElement;
            form = new DeucarianAudioCueForm(root, role,
                () => new DeucarianAudioResolution(palette.GetCue(role), DeucarianAudioResolutionSource.ExperiencePalette, palette), () => { });
        }

        [TearDown]
        public void TearDown()
        {
            form?.Dispose();
            if (window != null) window.Close();
            if (palette != null && !string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
            if (role != null) Object.DestroyImmediate(role);
            if (clip != null) Object.DestroyImmediate(clip);
        }

        [Test]
        public void EditingVolumeAndPitchPreservesVariantsNotesAndRole()
        {
            root.Q<Slider>("audio-cue-volume").value = .7f;
            root.Q<Slider>("audio-cue-pitch").value = 1.5f;
            var cue = palette.GetCue(role);
            Assert.That(cue.Volume, Is.EqualTo(.7f).Within(.001));
            Assert.That(cue.MinimumPitch, Is.EqualTo(1.4f).Within(.001));
            Assert.That(cue.MaximumPitch, Is.EqualTo(1.6f).Within(.001));
            Assert.That(cue.Clip, Is.SameAs(clip));
            Assert.That(cue.Variants.Count, Is.EqualTo(1));
            Assert.That(palette.Entries[0].Note, Is.EqualTo("Keep my note"));
            Assert.That(palette.Entries[0].Role, Is.SameAs(role));
        }

        [Test]
        public void PitchBoundsStayOrderedAndSourceRemovalInvalidatesBinding()
        {
            root.Q<FloatField>("audio-minimum-pitch").value = 2;
            Assert.That(palette.GetCue(role).MaximumPitch, Is.EqualTo(2));
            root.Q<FloatField>("audio-maximum-pitch").value = .5f;
            Assert.That(palette.GetCue(role).MinimumPitch, Is.EqualTo(.5f));
            palette.ClearEntries();
            Assert.That(form.MatchesSource, Is.False);
            form.Refresh();
            Assert.That(root.Q<Slider>("audio-cue-volume").enabledInHierarchy, Is.False);
        }

        [Test]
        public void NonFiniteInputAndDisposedControlsCannotChangeTheAsset()
        {
            root.Q<FloatField>("audio-minimum-pitch").value = float.NaN;
            root.Q<FloatField>("audio-maximum-pitch").value = float.PositiveInfinity;
            Assert.That(palette.GetCue(role).MinimumPitch, Is.EqualTo(.9f).Within(.001));
            Assert.That(palette.GetCue(role).MaximumPitch, Is.EqualTo(1.1f).Within(.001));
            form.Dispose();
            root.Q<Slider>("audio-cue-volume").value = .8f;
            Assert.That(palette.GetCue(role).Volume, Is.EqualTo(.4f).Within(.001));
        }

        public sealed class CueTestWindow : EditorWindow { }
    }
}
