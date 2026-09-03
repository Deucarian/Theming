using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianAudioPaletteLabTests
    {
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
