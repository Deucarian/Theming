using System;
using Deucarian.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemeReloadTests
    {
        private string theme, family, palette, library, style;
        private DeucarianThemeMode mode;
        private DeucarianThemeManagerSelection previousPreview;
        private DeucarianThemeStyle previousComposer;

        [SetUp]
        public void RememberSelection()
        {
            theme = DeucarianThemingEditorSettings.ActiveThemeGuid;
            family = DeucarianThemingEditorSettings.ActiveThemeFamilyGuid;
            palette = DeucarianThemingEditorSettings.ActivePaletteGuid;
            library = DeucarianThemingEditorSettings.ActiveRoleLibraryGuid;
            style = DeucarianThemingEditorSettings.ActiveStyleGuid;
            mode = DeucarianThemingEditorSettings.ActiveThemeMode;
            previousPreview = DeucarianThemePreviewCoordinator.SelectedPreview;
            if (DeucarianThemePreviewCoordinator.HasComposerPreview)
            {
                previousComposer = UnityEngine.Object.Instantiate(DeucarianThemePreviewCoordinator.ComposerPreviewStyle);
                previousComposer.name = DeucarianThemePreviewCoordinator.ComposerPreviewStyle.name;
                previousComposer.hideFlags = HideFlags.HideAndDontSave;
            }
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
        }

        [TearDown]
        public void RestoreSelection()
        {
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
            DeucarianThemingEditorSettings.ActiveThemeGuid = theme;
            DeucarianThemingEditorSettings.ActiveThemeFamilyGuid = family;
            DeucarianThemingEditorSettings.ActivePaletteGuid = palette;
            DeucarianThemingEditorSettings.ActiveRoleLibraryGuid = library;
            DeucarianThemingEditorSettings.ActiveStyleGuid = style;
            DeucarianThemingEditorSettings.ActiveThemeMode = mode;
            try
            {
                if (previousComposer != null)
                {
                    DeucarianThemePreviewCoordinator.ApplyComposerPreview(previousPreview, previousComposer,
                        previousComposer.SurfaceProfile, previousComposer.ShapeProfile, previousComposer.StrokeProfile,
                        previousComposer.Density, previousComposer.TypographyProfile);
                    DeucarianThemePreviewCoordinator.ComposerPreviewStyle.name = previousComposer.name;
                }
                else DeucarianThemePreviewCoordinator.ApplySelectedPreview();
            }
            finally
            {
                if (previousComposer != null) UnityEngine.Object.DestroyImmediate(previousComposer);
                previousComposer = null;
            }
        }

        [Test]
        public void SandboxSelectionUsesStagedLightAndStyleWithoutApplyingProjectDefaults()
        {
            var familyAsset = DeucarianVisualDefaults.LoadFamily();
            var applied = DeucarianThemeRuntimeResolver.ResolveDefaultTheme();
            var light = familyAsset.ResolveTheme(DeucarianThemeMode.Light);
            var originalStyle = light.VisualStyle;
            DeucarianThemingEditorSettings.SetDraftSelection(familyAsset, DeucarianThemeMode.Light, originalStyle);

            var selection = DeucarianEditorThemePreview.Capture();

            Assert.That(selection.Theme, Is.SameAs(light));
            Assert.That(selection.Mode, Is.EqualTo(DeucarianThemeMode.Light));
            Assert.That(selection.Style, Is.SameAs(originalStyle));
            Assert.That(DeucarianThemeRuntimeResolver.ResolveDefaultTheme(), Is.SameAs(applied));
            Assert.That(light.VisualStyle, Is.SameAs(originalStyle));
            Assert.That(selection.Label, Does.Contain("Light"));
        }

        [Test]
        public void SandboxIncludesUnsavedComposerStyleWithoutChangingSource()
        {
            var familyAsset = DeucarianVisualDefaults.LoadFamily();
            var source = familyAsset.LightTheme.VisualStyle;
            var originalDensity = source.Density;
            DeucarianThemingEditorSettings.SetDraftSelection(familyAsset, DeucarianThemeMode.Light, source);
            DeucarianThemePreviewCoordinator.ApplyComposerPreview(DeucarianThemeManagerSelection.FromEditorPrefs(),
                source, source.SurfaceProfile, source.ShapeProfile, source.StrokeProfile, DeucarianThemeDensity.Compact, source.TypographyProfile);

            var selection = DeucarianEditorThemePreview.Capture();

            Assert.That(selection.Theme, Is.SameAs(familyAsset.LightTheme));
            Assert.That(selection.Style, Is.SameAs(DeucarianThemePreviewCoordinator.ComposerPreviewStyle));
            Assert.That(selection.Style, Is.Not.SameAs(source));
            Assert.That(selection.IsDraft, Is.True);
            Assert.That(source.Density, Is.EqualTo(originalDensity));
        }

        [Test]
        public void ThemeControllerRestoresComposerTabAndPendingChoicesAfterRecreation()
        {
            var familyAsset = DeucarianVisualDefaults.LoadFamily();
            var source = familyAsset.LightTheme.VisualStyle;
            DeucarianThemingEditorSettings.SetDraftSelection(familyAsset, DeucarianThemeMode.Light, source);
            var first = ScriptableObject.CreateInstance<DeucarianThemeManagerWindow>();
            DeucarianThemeManagerWindow second = null;
            try
            {
                string json = JsonUtility.ToJson(new ThemeFixture
                {
                    view = 1, category = 2, captured = true, touched = true,
                    baselineFamily = Guid(familyAsset), baselineMode = DeucarianThemeMode.Dark,
                    baselineStyle = Guid(source), source = Guid(source), surface = Guid(source.SurfaceProfile),
                    corners = Guid(source.ShapeProfile), border = Guid(source.StrokeProfile),
                    typography = Guid(source.TypographyProfile), size = DeucarianThemeDensity.Compact
                });
                first.RestoreReloadState(json);
                string captured = first.CaptureReloadState();
                UnityEngine.Object.DestroyImmediate(first); first = null;
                second = ScriptableObject.CreateInstance<DeucarianThemeManagerWindow>();
                second.RestoreReloadState(captured);

                Assert.That(second.CaptureReloadState(), Is.EqualTo(captured));
                Assert.That(DeucarianThemePreviewCoordinator.HasComposerPreview, Is.True);
                Assert.That(DeucarianThemePreviewCoordinator.ComposerPreviewStyle.Density, Is.EqualTo(DeucarianThemeDensity.Compact));
                Assert.That(DeucarianThemingEditorSettings.ActiveThemeMode, Is.EqualTo(DeucarianThemeMode.Light));
            }
            finally { if (first != null) UnityEngine.Object.DestroyImmediate(first); if (second != null) UnityEngine.Object.DestroyImmediate(second); }
        }

        [Test]
        public void AudioControllerRestoresDefinitionDraftWithoutStartingPlayback()
        {
            var first = ScriptableObject.CreateInstance<DeucarianAudioPaletteLabWindow>();
            DeucarianAudioPaletteLabWindow second = null;
            try
            {
                first.RestoreReloadState("{\"tab\":1,\"category\":2,\"search\":\"hover\",\"experience\":3,\"useIntensity\":true,\"intensity\":0.3,\"definitions\":{\"Search\":\"tone\",\"CreateName\":\"My new cue\"}}");
                string captured = first.CaptureReloadState();
                UnityEngine.Object.DestroyImmediate(first); first = null;
                second = ScriptableObject.CreateInstance<DeucarianAudioPaletteLabWindow>();
                second.RestoreReloadState(captured);
                Assert.That(second.CaptureReloadState(), Is.EqualTo(captured));
                Assert.That(captured, Does.Contain("My new cue"));
            }
            finally { if (first != null) UnityEngine.Object.DestroyImmediate(first); if (second != null) UnityEngine.Object.DestroyImmediate(second); }
        }

        private static string Guid(UnityEngine.Object asset) => DeucarianThemingEditorSettings.GetAssetGuid(asset);
        [Serializable]
        private sealed class ThemeFixture
        {
            public int view, category;
            public bool captured, touched;
            public string baselineFamily, baselineStyle, source, surface, corners, border, typography;
            public DeucarianThemeMode baselineMode;
            public DeucarianThemeDensity size;
        }
    }
}
