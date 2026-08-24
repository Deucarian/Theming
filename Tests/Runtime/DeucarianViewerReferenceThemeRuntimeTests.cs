using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianViewerReferenceThemeRuntimeTests
    {
        private readonly List<GameObject> createdObjects =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void CompositionInstallsOneCanonicalReferenceRuntime()
        {
            const string preferenceKey =
                "deucarian.tests.viewer-theme-composition";
            PlayerPrefs.DeleteKey(preferenceKey);
            GameObject host = CreateObject("Reference Theme Host");

            try
            {
                var published = new List<string>();
                DeucarianViewerReferenceThemeRuntime runtime =
                    DeucarianViewerReferenceThemeComposition.Install(
                        host,
                        preferenceKey: preferenceKey,
                        publishSnapshot: published.Add);
                DeucarianViewerReferenceThemeRuntime installedAgain =
                    DeucarianViewerReferenceThemeComposition.Install(
                        host,
                        runtime.Provider,
                        preferenceKey,
                        published.Add);

                Assert.That(installedAgain.Provider, Is.SameAs(runtime.Provider));
                Assert.That(
                    installedAgain.ModeController,
                    Is.SameAs(runtime.ModeController));
                Assert.That(
                    installedAgain.SnapshotPublisher,
                    Is.SameAs(runtime.SnapshotPublisher));
                Assert.That(
                    host.GetComponents<DeucarianThemeProvider>().Length,
                    Is.EqualTo(1));
                Assert.That(
                    host.GetComponents<DeucarianThemeModeController>().Length,
                    Is.EqualTo(1));
                Assert.That(
                    host.GetComponents<
                        DeucarianViewerThemeSnapshotPublisher>().Length,
                    Is.EqualTo(1));
                Assert.That(
                    runtime.Provider.CurrentThemeFamily,
                    Is.SameAs(
                        DeucarianViewerReferenceThemePreset.Resolve()
                            .ThemeFamily));
                Assert.That(
                    runtime.Provider.ThemeMode,
                    Is.EqualTo(
                        DeucarianViewerReferenceThemePreset.DefaultMode));
                Assert.That(published, Is.Not.Empty);
            }
            finally
            {
                PlayerPrefs.DeleteKey(preferenceKey);
            }
        }

        [Test]
        public void ModeControllerRestoresAndPersistsThroughInjectedStore()
        {
            GameObject host = CreateObject("Theme Mode Host");
            DeucarianThemeProvider provider =
                host.AddComponent<DeucarianThemeProvider>();
            DeucarianThemeModeController controller =
                host.AddComponent<DeucarianThemeModeController>();
            var store = new MemoryThemeModeStore(
                DeucarianThemeMode.Light);
            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();

            controller.Bind(
                provider,
                profile.ThemeFamily,
                DeucarianThemeMode.Dark,
                store);

            Assert.That(provider.ThemeMode, Is.EqualTo(DeucarianThemeMode.Light));
            Assert.That(controller.SetThemeMode(DeucarianThemeMode.Dark), Is.True);
            Assert.That(provider.ThemeMode, Is.EqualTo(DeucarianThemeMode.Dark));
            Assert.That(store.SavedModes, Is.EqualTo(new[] { DeucarianThemeMode.Dark }));
        }

        [Test]
        public void SnapshotUsesCanonicalCamelCaseShapeAndReferenceRoles()
        {
            DeucarianTheme theme =
                DeucarianViewerReferenceThemePreset.Resolve().DarkTheme;

            DeucarianViewerThemeSnapshot snapshot =
                DeucarianViewerThemeSnapshot.FromTheme(theme);
            string json = snapshot.ToJson();

            Assert.That(snapshot.IsValid, Is.True);
            Assert.That(snapshot.MissingRoles, Is.Empty);
            Assert.That(snapshot.ThemeId, Is.EqualTo(theme.ThemeId));
            Assert.That(snapshot.IsDark, Is.True);
            Assert.That(snapshot.Background, Does.StartWith("#"));
            Assert.That(json, Does.Contain("\"themeId\""));
            Assert.That(json, Does.Contain("\"surfaceRaised\""));
            Assert.That(json, Does.Contain("\"interactionSelected\""));
            Assert.That(json, Does.Not.Contain("ThemeId"));
        }

        [Test]
        public void SnapshotPublisherPublishesOnlyMaterialChanges()
        {
            GameObject host = CreateObject("Theme Publisher Host");
            DeucarianThemeProvider provider =
                host.AddComponent<DeucarianThemeProvider>();
            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();
            provider.SetThemeFamily(
                profile.ThemeFamily,
                DeucarianThemeMode.Dark);
            DeucarianViewerThemeSnapshotPublisher publisher =
                host.AddComponent<DeucarianViewerThemeSnapshotPublisher>();
            var published = new List<string>();

            publisher.Bind(provider, published.Add);
            publisher.Bind(provider, published.Add);
            provider.SetThemeMode(DeucarianThemeMode.Light);

            Assert.That(published.Count, Is.EqualTo(2));
            Assert.That(published[0], Is.Not.EqualTo(published[1]));
            Assert.That(
                publisher.LastPublishedJson,
                Is.EqualTo(published[1]));
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class MemoryThemeModeStore : IDeucarianThemeModeStore
        {
            private readonly DeucarianThemeMode restoredMode;

            public MemoryThemeModeStore(DeucarianThemeMode restoredMode)
            {
                this.restoredMode = restoredMode;
            }

            public List<DeucarianThemeMode> SavedModes { get; } =
                new List<DeucarianThemeMode>();

            public bool TryLoad(out DeucarianThemeMode mode)
            {
                mode = restoredMode;
                return true;
            }

            public void Save(DeucarianThemeMode mode)
            {
                SavedModes.Add(mode);
            }
        }
    }
}
