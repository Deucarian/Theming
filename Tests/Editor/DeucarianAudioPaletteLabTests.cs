using NUnit.Framework;
using System.Collections;
using System.Linq;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianAudioPaletteLabTests
    {
        [UnityTest]
        public IEnumerator SwitchingAndRefreshingPreserveAudioControlsAndDraftModifiers()
        {
            var window = ScriptableObject.CreateInstance<AudioSwitchTestWindow>();
            var set = ScriptableObject.CreateInstance<DeucarianAudioPaletteSet>();
            var palette = ScriptableObject.CreateInstance<DeucarianAudioPalette>();
            var role = ScriptableObject.CreateInstance<DeucarianAudioRole>();
            var library = ScriptableObject.CreateInstance<DeucarianAudioRoleLibrary>();
            role.Configure("test.switching.click", "Test click", "UI", "Switching fixture", DeucarianAudioCue.Silent(), false);
            library.AddRole(role);
            palette.SetRoleLibrary(library);
            palette.SetCue(role, DeucarianAudioCue.Silent());
            set.Configure(palette, null);
            var previousSelection = Selection.activeObject;
            Selection.activeObject = set;
            window.Show();
            try
            {
                using (var session = new DeucarianEditorPageSession(window, "test.home", _ => { }))
                {
                    session.Navigate(DeucarianEditorWorkspaceNavigation.AudioToolId);
                    for (int i = 0; i < 4; i++) yield return null;
                    var root = window.rootVisualElement;
                    var intensity = root.Q<Toggle>("audio-use-intensity");
                    var value = root.Q<FloatField>("audio-intensity");
                    var sections = root.Q("workspace-details").Query<Foldout>().ToList();
                    Assert.That(sections.Count, Is.EqualTo(3));
                    foreach (var section in sections) section.value = true;
                    intensity.value = true;
                    value.value = 0.73f;
                    var firstRole = root.Q<Button>(className: "dw-collection-select");
                    Assert.That(firstRole, Is.Not.Null, "The audio page must contain actual palette roles.");
                    firstRole.Focus();
                    yield return null;
                    using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = firstRole; firstRole.SendEvent(evt); }
                    var selectedRow = root.Q("workspace-collection-rows").Query(className: "dw-selected").First();
                    Assert.That(selectedRow, Is.Not.Null);
                    for (int i = 0; i < 4; i++) yield return null;
                    var details = root.Q<ScrollView>("workspace-details");
                    details.scrollOffset = new Vector2(0, 50);
                    yield return null;
                    float scrollPosition = details.scrollOffset.y;
                    for (int cycle = 0; cycle < 3; cycle++)
                    {
                        session.Navigate("test.home");
                        yield return null;
                        session.Navigate(DeucarianEditorWorkspaceNavigation.AudioToolId);
                        for (int i = 0; i < 3; i++) yield return null;
                        session.Navigate(DeucarianEditorWorkspaceNavigation.AudioToolId);
                        Assert.That(root.Q<Toggle>("audio-use-intensity"), Is.SameAs(intensity));
                        Assert.That(root.Q<FloatField>("audio-intensity"), Is.SameAs(value));
                        Assert.That(intensity.value, Is.True);
                        Assert.That(value.value, Is.EqualTo(0.73f));
                        foreach (var section in sections) Assert.That(section.value, Is.True);
                        Assert.That(root.Contains(selectedRow), Is.True);
                        Assert.That(selectedRow.ClassListContains("dw-selected"), Is.True);
                        Assert.That(details.scrollOffset.y, Is.EqualTo(scrollPosition).Within(1));
                    }
                }
            }
            finally
            {
                Selection.activeObject = previousSelection;
                window.Close();
                Object.DestroyImmediate(set);
                Object.DestroyImmediate(palette);
                Object.DestroyImmediate(role);
                Object.DestroyImmediate(library);
            }
        }

        public sealed class AudioSwitchTestWindow : EditorWindow { }

        private sealed class FakePreview : IDeucarianAudioPreviewService
        {
            public bool IsAvailable => true;
            public bool IsPlaying { get; private set; }
            public int PlayCount { get; private set; }
            public int StopCount { get; private set; }

            public bool Play(AudioClip clip)
            {
                Stop();
                if (clip == null)
                {
                    return false;
                }

                PlayCount++;
                IsPlaying = true;
                return true;
            }

            public void Stop()
            {
                StopCount++;
                IsPlaying = false;
            }
        }

        [Test]
        public void PreviewIsExplicitAndChangingExperienceStopsIt()
        {
            DeucarianAudioPaletteLabWindow window =
                ScriptableObject.CreateInstance<DeucarianAudioPaletteLabWindow>();
            FakePreview preview = new FakePreview();
            AudioClip clip = AudioClip.Create("Preview", 16, 1, 8000, false);
            try
            {
                window.SetPreviewServiceForTests(preview);
                int stopsBeforePlay = preview.StopCount;
                Assert.IsTrue(window.PreviewForTests(new DeucarianAudioCue(clip)));
                Assert.AreEqual(1, preview.PlayCount);
                Assert.Greater(preview.StopCount, stopsBeforePlay);

                window.ChangeExperienceForTests(DeucarianAudioExperience.XR);
                Assert.IsFalse(preview.IsPlaying);
                Assert.AreEqual(1, preview.PlayCount, "Profile changes must never auto-play.");
            }
            finally
            {
                window.DisableForTests();
                Object.DestroyImmediate(window);
                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void DisableCleansUpPreviewWithoutCreatingSceneObjects()
        {
            int rootsBefore = UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().GetRootGameObjects().Length;
            DeucarianAudioPaletteLabWindow window =
                ScriptableObject.CreateInstance<DeucarianAudioPaletteLabWindow>();
            FakePreview preview = new FakePreview();
            window.SetPreviewServiceForTests(preview);

            window.DisableForTests();

            Assert.Greater(preview.StopCount, 0);
            Assert.AreEqual(
                rootsBefore,
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Length);
            Object.DestroyImmediate(window);
        }
    }
}
