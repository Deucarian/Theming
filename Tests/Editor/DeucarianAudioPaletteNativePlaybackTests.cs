using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianAudioPaletteNativePlaybackTests
    {
        [UnityTest]
        public IEnumerator PalettePlayButtonsReachNativeProcessedAudioAndStopCleansUp()
        {
            if (Application.isBatchMode) Assert.Ignore("Requires a non-batch editor audio device.");
            Assert.That(DeucarianThemeRuntimeResolver.UseAudio, Is.True, "Use an isolated consumer with audio enabled.");
            var clip = AudioClip.Create("Palette button audition test", 44100, 1, 44100, false);
            var pcm = new float[44100];
            for (int i = 0; i < pcm.Length; i++) pcm[i] = Mathf.Sin(i * 440 * 2 * Mathf.PI / 44100) * .1f;
            clip.SetData(pcm, 0);
            var set = ScriptableObject.CreateInstance<DeucarianAudioPaletteSet>();
            var palette = ScriptableObject.CreateInstance<DeucarianAudioPalette>();
            var role = ScriptableObject.CreateInstance<DeucarianAudioRole>();
            var library = ScriptableObject.CreateInstance<DeucarianAudioRoleLibrary>();
            var cue = new DeucarianAudioCue(new[] { clip }, .4f, 1.2f, 1.2f);
            role.Configure(DeucarianBuiltinAudioRoleIds.Activate, "Activate", "UI", "Native playback fixture", cue, false);
            library.AddRole(role); palette.SetRoleLibrary(library); palette.SetCue(role, cue); set.Configure(palette, null);
            var previousSelection = Selection.activeObject;
            Selection.activeObject = set;
            var window = ScriptableObject.CreateInstance<DeucarianAudioPaletteLabWindow>();
            window.position = new Rect(40, 40, 1500, 850);
            try
            {
                window.Show();
                for (int i = 0; i < 10; i++) yield return null;
                var root = window.rootVisualElement;
                foreach (var foldout in root.Query<Foldout>().ToList()) foldout.value = true;
                for (int action = 0; action < 3; action++)
                {
                    var play = action == 0 ? root.Q<Button>("audio-play")
                        : action == 1 ? root.Q<Button>(className: "dw-collection-action")
                        : root.Q<Button>("audio-pad-" + DeucarianBuiltinAudioRoleIds.Activate);
                    Assert.That(play, Is.Not.Null); Assert.That(play.enabledInHierarchy, Is.True);
                    play.Focus(); yield return null;
                    using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = play; play.SendEvent(evt); }
                    var source = Resources.FindObjectsOfTypeAll<AudioSource>().Single(s => s.clip == clip);
                    Assert.That(source.volume, Is.EqualTo(.4f).Within(.001));
                    Assert.That(source.pitch, Is.EqualTo(1.2f).Within(.001));
                    var output = new float[512]; float peak = 0;
                    double deadline = EditorApplication.timeSinceStartup + .5;
                    while (source != null && EditorApplication.timeSinceStartup < deadline)
                    {
                        source.GetOutputData(output, 0);
                        foreach (float sample in output) peak = Mathf.Max(peak, Mathf.Abs(sample));
                        yield return null;
                    }
                    Assert.That(peak, Is.GreaterThan(.001f), "The UI action must produce audio, not only a success label.");
                    var stop = root.Q<Button>("audio-stop"); stop.Focus(); yield return null;
                    using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = stop; stop.SendEvent(evt); }
                    Assert.That(source == null, Is.True);
                }
            }
            finally
            {
                window.Close(); Selection.activeObject = previousSelection;
                Object.DestroyImmediate(set); Object.DestroyImmediate(palette);
                Object.DestroyImmediate(library); Object.DestroyImmediate(role); Object.DestroyImmediate(clip);
            }
        }
    }
}
