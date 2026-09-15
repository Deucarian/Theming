using System;
using System.Collections.Generic;
using Deucarian.Media.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Deucarian.Editor;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemingProjectSettingsTests
    {
        private const string Folder = "Assets/DeucarianProjectAdoptionTests";
        private const string Path = Folder + "/Resources/DeucarianThemeRuntimeSettings.asset";
        private readonly List<Object> objects = new List<Object>();
        private DeucarianThemeRuntimeSettings settings;

        [SetUp]
        public void Setup()
        {
            Assert.That(DeucarianThemeRuntimeSettingsAssets.FindRuntimeSettingsResourceAssets(), Is.Empty,
                "Run these asset-isolation tests in a consumer without project runtime settings.");
            Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False, "Do not overwrite existing assets.");
            settings = DeucarianThemeRuntimeSettingsAssets.CreateRuntimeSettingsAtPath(Path);
            Assert.That(settings, Is.Not.Null);
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            if (settings != null) AssetDatabase.DeleteAsset(Folder);
            settings = null;
        }

        [Test]
        public void FeaturesDefaultOnAndRemainIndependentWithoutErasingAssets()
        {
            Assert.That(settings.UseVisualStyling && settings.UseAudio, Is.True);
            var theme = Asset<DeucarianTheme>();
            var palette = Asset<DeucarianAudioPaletteSet>();
            settings.Configure(theme);
            settings.ConfigureAudio(palette, DeucarianAudioExperience.XR);
            settings.SetFeatures(false, true);
            Assert.That(DeucarianThemeRuntimeResolver.UseVisualStyling, Is.False);
            Assert.That(DeucarianThemeRuntimeResolver.UseAudio, Is.True);
            settings.SetFeatures(true, false);
            Assert.That(settings.DefaultTheme, Is.SameAs(theme));
            Assert.That(settings.DefaultAudioPaletteSet, Is.SameAs(palette));
            Assert.That(settings.DefaultAudioExperience, Is.EqualTo(DeucarianAudioExperience.XR));
            Assert.That(DeucarianThemeRuntimeResolver.UseVisualStyling, Is.True);
            Assert.That(DeucarianThemeRuntimeResolver.UseAudio, Is.False);
        }

        [Test]
        public void OldSerializedAssetsKeepBothFeaturesEnabled()
        {
            var legacy = Asset<DeucarianThemeRuntimeSettings>();
            EditorJsonUtility.FromJsonOverwrite("{\"defaultThemeMode\":0}", legacy);
            Assert.That(legacy.UseVisualStyling && legacy.UseAudio, Is.True);
        }

        [Test]
        public void RepeatedSettingsDoNotPublishAndInvalidExperienceDoesNotMutate()
        {
            int count = 0;
            Action<Object> listener = asset => { if (asset == settings) count++; };
            DeucarianThemeAssetChangeBus.AssetChanged += listener;
            try
            {
                settings.SetFeatures(true, true);
                settings.ConfigureAudio(null, DeucarianAudioExperience.Default);
                Assert.That(count, Is.Zero);
                settings.SetFeatures(false, true);
                Assert.That(count, Is.EqualTo(1));
                Assert.Throws<ArgumentOutOfRangeException>(() => settings.ConfigureAudio(null, (DeucarianAudioExperience)999));
                Assert.That(settings.DefaultAudioExperience, Is.EqualTo(DeucarianAudioExperience.Default));
            }
            finally { DeucarianThemeAssetChangeBus.AssetChanged -= listener; }
        }

        [Test]
        public void VisualOffSkipsExplicitThemesButDoesNotHideMixedThemeFromAudio()
        {
            var theme = Asset<DeucarianTheme>();
            settings.Configure(theme);
            settings.SetFeatures(false, true);
            var go = GameObject("Visual target");
            go.SetActive(false);
            var target = go.AddComponent<ProjectAdoptionVisualTarget>();
            target.ThemeOverride = theme;
            go.SetActive(true);
            target.ApplyTheme(theme);
            Assert.That(target.Applied, Is.Zero);
            Assert.That(DeucarianThemeRuntimeResolver.ResolveDefaultTheme(), Is.SameAs(theme));
            settings.SetFeatures(true, true);
            Assert.That(target.Applied, Is.EqualTo(1));
            go.SetActive(false);
            settings.SetFeatures(false, true);
            settings.SetFeatures(true, true);
            Assert.That(target.Applied, Is.EqualTo(1), "Disabled targets must unsubscribe.");
        }

        [Test]
        public void AudioOffBlocksEveryPlaybackEntryPointIncludingDirectOverrides()
        {
            settings.SetFeatures(false, false);
            var player = Player();
            player.PaletteSetOverride = DeucarianAudioDefaults.LoadPaletteSet();
            var role = Asset<DeucarianAudioRole>();
            var cue = new DeucarianAudioCue(Clip());
            role.Configure("test.adoption", "Test", "UI", "", cue, false);
            Assert.That(player.PlayRole(role), Is.False);
            Assert.That(player.PlayRoleById(DeucarianBuiltinAudioRoleIds.Key), Is.False);
            Assert.That(player.Play(cue), Is.False);
            Assert.That(player.PlayResolved(DeucarianBuiltinAudioRoleIds.Warning, cue), Is.False);
            Assert.That(player.PlayedRoleIds, Is.Empty);
        }

        [Test]
        public void ProjectAudioDefaultsAndExplicitOverridesHavePredictablePrecedence()
        {
            settings.SetFeatures(false, true);
            var project = DeucarianAudioDefaults.LoadPaletteSet();
            settings.ConfigureAudio(project, DeucarianAudioExperience.XR);
            var player = Player();
            Assert.That(player.CurrentPaletteSet, Is.SameAs(project));
            Assert.That(player.CurrentExperience, Is.EqualTo(DeucarianAudioExperience.XR));
            Assert.That(player.PlayRoleById(DeucarianBuiltinAudioRoleIds.Key), Is.True);
            var explicitSet = Asset<DeucarianAudioPaletteSet>();
            player.PaletteSetOverride = explicitSet;
            player.UseProviderExperience = false;
            player.ExperienceOverride = DeucarianAudioExperience.WebGL;
            Assert.That(player.CurrentPaletteSet, Is.SameAs(explicitSet));
            Assert.That(player.CurrentExperience, Is.EqualTo(DeucarianAudioExperience.WebGL));
            player.PaletteSetOverride = null;
            player.ThemeOverride = Asset<DeucarianTheme>();
            Assert.That(player.CurrentPaletteSet, Is.Null, "An explicit mixed theme owns its audio.");
        }

        [Test]
        public void ConnectionsRequireAnIntegrationOrActualSuccessfulPlayback()
        {
            settings.SetFeatures(false, true);
            var player = Player();
            player.PaletteSetOverride = DeucarianAudioDefaults.LoadPaletteSet();
            using (var observer = new DeucarianThemingConnectionObserver())
            {
                observer.Start();
                Assert.That(observer.Buttons || observer.Keyboard || observer.Warnings, Is.False);
                player.PlayRoleById(DeucarianBuiltinAudioRoleIds.Key);
                player.PlayRoleById(DeucarianBuiltinAudioRoleIds.Warning);
                Assert.That(observer.Keyboard && observer.Warnings, Is.True);
                Assert.That(observer.Buttons, Is.False);
                observer.Dispose();
                player.PlayRoleById(DeucarianBuiltinAudioRoleIds.Press);
                Assert.That(observer.Buttons, Is.False, "Deactivated pages must not observe playback.");
                observer.Start();
                Assert.That(observer.Buttons, Is.True, "Reopened pages read the player's usage history.");
            }
        }

        [Test]
        public void StorePersistsAnExplicitChoiceAndSupportsUndo()
        {
            var store = new DeucarianThemingProjectSettingsStore();
            Assert.That(store.Read(), Is.SameAs(settings));
            store.Write(value => value.SetFeatures(false, true));
            Undo.FlushUndoRecordObjects();
            Assert.That(settings.UseVisualStyling, Is.False);
            Undo.PerformUndo();
            Assert.That(settings.UseVisualStyling, Is.True);
        }

        [Test]
        public void DisabledPalettePagesExplainAdoptionAndKeepNavigationAccessible()
        {
            settings.SetFeatures(false, false);
            foreach (string id in new[] { DeucarianToolIds.ThemeManager, DeucarianEditorWorkspaceNavigation.AudioToolId })
            {
                Assert.That(DeucarianToolRegistry.TryGet(id, out var tool), Is.True);
                Assert.That(tool.IsFeatureEnabled(), Is.False);
                using (var page = tool.CreatePage())
                {
                    page.Activate(null);
                    Assert.That(page.Root.Q("capability-disabled").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(page.Root.Q<Button>("capability-open-settings").enabledInHierarchy, Is.True);
                    Assert.That(page.Root.Q(className: "dw-gated-content").enabledSelf, Is.False);
                    Assert.That(page.Root.Q<SliderInt>("workspace-scale-slider").enabledInHierarchy, Is.True);
                    page.Deactivate();
                }
            }
        }

        [Test]
        public void AudioLabRejectsAuditionWhenProjectAudioIsOff()
        {
            settings.SetFeatures(true, false);
            var lab = Asset<DeucarianAudioPaletteLabWindow>();
            var preview = new Preview();
            lab.SetPreviewServiceForTests(preview);
            Assert.That(lab.PreviewForTests(new DeucarianAudioCue(Clip())), Is.False);
            Assert.That(preview.Plays, Is.Zero);
        }

        [Test]
        public void RuntimeFeatureSwitchNotifiesTargetsToReleaseTheirPresentation()
        {
            settings.Configure(Asset<DeucarianTheme>());
            var target = GameObject("Visual lifecycle").AddComponent<ProjectAdoptionVisualTarget>();
            int applied = target.Applied;
            settings.SetFeatures(false, true);
            Assert.That(target.Disabled, Is.EqualTo(1));
            Assert.That(target.Applied, Is.EqualTo(applied));
            settings.SetFeatures(true, true);
            Assert.That(target.Applied, Is.GreaterThan(applied));
        }

        private sealed class Preview : IDeucarianAudioPreviewService
        {
            internal int Plays;
            public bool IsAvailable => true;
            public bool IsPlaying { get; private set; }
            public bool Play(AudioClip clip) { Plays++; IsPlaying = true; return true; }
            public void Stop() { IsPlaying = false; }
        }

        private T Asset<T>() where T : ScriptableObject
        { var value = ScriptableObject.CreateInstance<T>(); objects.Add(value); return value; }
        private AudioClip Clip()
        { var value = AudioClip.Create("Adoption test", 128, 1, 8000, false); objects.Add(value); return value; }
        private GameObject GameObject(string name)
        { var value = new GameObject(name); objects.Add(value); return value; }
        private DeucarianThemeAudioPlayer Player()
        {
            var go = GameObject("Adoption audio");
            go.AddComponent<UnityAudioOneShotOutput>();
            return go.AddComponent<DeucarianThemeAudioPlayer>();
        }
    }

    [ExecuteAlways]
    public sealed class ProjectAdoptionVisualTarget : DeucarianThemeTargetBehaviour
    {
        public int Applied { get; private set; }
        public int Disabled { get; private set; }
        protected override void ApplyResolvedTheme(DeucarianTheme theme) => Applied++;
        protected override void OnVisualStylingDisabled() => Disabled++;
    }
}
