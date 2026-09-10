using System;
using System.Collections;
using Deucarian.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemingProjectPageTests
    {
        private sealed class Store : IDeucarianThemingProjectSettingsStore
        {
            public DeucarianThemeRuntimeSettings Settings;
            public int Writes;
            public bool CanWrite { get; set; } = true;
            public string Problem { get; set; }
            public DeucarianThemeRuntimeSettings Read() => Settings;
            public void Write(Action<DeucarianThemeRuntimeSettings> change)
            { if (CanWrite) { Writes++; change(Settings); } }
        }

        [Test]
        public void OpeningRefreshingAndDisposingAnUnconfiguredPageAreReadOnly()
        {
            var store = new Store();
            using (var page = new DeucarianThemingProjectPage(store, new DeucarianThemingConnectionObserver()))
            {
                page.Activate(null);
                page.Deactivate();
                page.Activate(null);
                Assert.That(store.Writes, Is.Zero);
                Assert.That(page.Root.Q<Toggle>("theming-visual-enabled").value, Is.True);
                Assert.That(page.Root.Q<Toggle>("theming-audio-enabled").value, Is.True);
                page.Dispose();
                page.Activate(null);
                Assert.That(store.Writes, Is.Zero);
            }
        }

        [Test]
        public void AllThemingPagesShareOneSubmenuWithoutChangingStableToolIds()
        {
            string[] ids = { DeucarianThemingProjectPage.ToolId, DeucarianToolIds.ThemeManager,
                DeucarianEditorWorkspaceNavigation.AudioToolId };
            string[] labels = { "Project setup", "Visual palettes", "Audio palettes" };
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.That(DeucarianToolRegistry.TryGet(ids[i], out var tool), Is.True);
                Assert.That(tool.NavigationPath, Is.EqualTo("Theming"));
                Assert.That(tool.NavigationLabel, Is.EqualTo(labels[i]));
                Assert.That(tool.ShowNavigationIcon, Is.False);
                Assert.That(tool.CreatePage, Is.Not.Null);
            }
        }

        [UnityTest]
        public IEnumerator TogglesPersistIndependentlyAndReadOnlyStateKeepsPreviewAccessible()
        {
            var settings = ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>();
            var window = ScriptableObject.CreateInstance<AdoptionPageTestWindow>();
            var store = new Store { Settings = settings };
            window.Show();
            using (var page = new DeucarianThemingProjectPage(store, new DeucarianThemingConnectionObserver()))
            {
                try
                {
                    window.rootVisualElement.Add(page.Root);
                    page.Activate(null);
                    yield return null;
                    page.Root.Q<Toggle>("theming-visual-enabled").value = false;
                    Assert.That(settings.UseVisualStyling, Is.False);
                    Assert.That(settings.UseAudio, Is.True);
                    Assert.That(store.Writes, Is.EqualTo(1));
                    Assert.That(page.Root.Q("theming-visual-details").style.display.value, Is.EqualTo(DisplayStyle.None));
                    store.CanWrite = false;
                    store.Problem = "Duplicate settings";
                    page.Deactivate();
                    page.Activate(null);
                    Assert.That(page.Root.Q<Toggle>("theming-audio-enabled").enabledInHierarchy, Is.False);
                    Assert.That(page.Root.Q<Button>("theming-open-audio").enabledInHierarchy, Is.True);
                }
                finally { window.Close(); UnityEngine.Object.DestroyImmediate(settings); }
            }
        }

        [UnityTest]
        public IEnumerator AudioActionNavigatesInsideTheSameHostAndSetupCanBeRevisited()
        {
            var window = ScriptableObject.CreateInstance<AdoptionPageTestWindow>();
            window.Show();
            try
            {
                using (var session = new DeucarianEditorPageSession(window, "test.home", _ => { }))
                {
                    session.Navigate(DeucarianThemingProjectPage.ToolId);
                    for (int i = 0; i < 4; i++) yield return null;
                    var button = window.rootVisualElement.Q<Button>("theming-open-audio");
                    var setup = window.rootVisualElement.Q("theming-project-settings");
                    int before = Resources.FindObjectsOfTypeAll<DeucarianEditorToolWindow>().Length;
                    button.Focus();
                    yield return null;
                    using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
                    for (int i = 0; i < 4; i++) yield return null;
                    Assert.That(window.rootVisualElement.Q("workspace-collection-rows"), Is.Not.Null);
                    Assert.That(Resources.FindObjectsOfTypeAll<DeucarianEditorToolWindow>().Length, Is.EqualTo(before));
                    session.Navigate(DeucarianToolIds.ThemeManager);
                    yield return null;
                    session.Navigate(DeucarianThemingProjectPage.ToolId);
                    Assert.That(window.rootVisualElement.Q("theming-project-settings"), Is.SameAs(setup));
                }
            }
            finally { window.Close(); }
        }
    }

    internal sealed class AdoptionPageTestWindow : EditorWindow { }
}
