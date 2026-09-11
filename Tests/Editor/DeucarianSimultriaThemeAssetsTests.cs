using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianSimultriaThemeAssetsTests
    {
        private string folder;
        [SetUp] public void Setup() => folder = "Assets/SimultriaPresetTests-" + Guid.NewGuid().ToString("N");
        [TearDown] public void Cleanup() { if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder); }

        [Test]
        public void DesignAndSalesPreservesAuthoredHoloColorsAndSeparateInteractionStates()
        {
            var assets = DeucarianSimultriaThemeAssets.CreateDesignAndSales(folder);
            Assert.That(assets.ThemeFamily.IsComplete, Is.True);
            Assert.That(assets.ThemeFamily.name, Is.EqualTo("Simultria DS"));
            Assert.That(assets.DarkPalette.name, Is.EqualTo("Simultria DS Dark Palette"));
            Assert.That(assets.LightPalette.name, Is.EqualTo("Simultria DS Light Palette"));
            var palette = assets.DarkPalette;
            AssertColor(palette, DeucarianBuiltinColorRoleIds.Primary, new Color(.39200002f, .32200003f, .5f, 1));
            AssertColor(palette, DeucarianControlColorRoleIds.TitleText, new Color(.7686275f, .6313726f, .97647065f, 1));
            AssertColor(palette, DeucarianControlColorRoleIds.KeyboardAccent, new Color(.125f, .588f, .953f, 1));
            AssertColor(palette, DeucarianBuiltinColorRoleIds.UiHighlighted, new Color(.94f, .78f, 1, 1));
            Assert.That(palette.GetColorById(DeucarianBuiltinColorRoleIds.UiPressed), Is.Not.EqualTo(palette.GetColorById(DeucarianBuiltinColorRoleIds.UiSelected)));
            Assert.That(assets.LightPalette.GetColorById(DeucarianBuiltinColorRoleIds.Background),
                Is.Not.EqualTo(palette.GetColorById(DeucarianBuiltinColorRoleIds.Background)));
        }

        [Test]
        public void RealisationAndProgressUsesAuthoredGreenAndDoesNotOverwriteExistingEdits()
        {
            var assets = DeucarianSimultriaThemeAssets.CreateRealisationAndProgress(folder);
            Assert.That(assets.ThemeFamily.name, Is.EqualTo("Simultria RP"));
            Assert.That(assets.DarkPalette.name, Is.EqualTo("Simultria RP Dark Palette"));
            Assert.That(assets.LightPalette.name, Is.EqualTo("Simultria RP Light Palette"));
            AssertColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Primary, new Color(.6862745f, .77254903f, .5529412f, 1));
            AssertColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Primary, new Color(.2509804f, .32156864f, .14117648f, 1));
            var role = assets.Roles[0];
            assets.DarkPalette.SetColor(role, Color.cyan);
            EditorUtility.SetDirty(assets.DarkPalette);
            var reused = DeucarianSimultriaThemeAssets.CreateRealisationAndProgress(folder);
            Assert.That(reused.ThemeFamily, Is.SameAs(assets.ThemeFamily));
            Assert.That(reused.DarkPalette.GetColor(role), Is.EqualTo(Color.cyan));
        }

        [Test]
        public void EarlierGenericPresetFilesAreReusedWithoutReplacingTheirEdits()
        {
            var assets = DeucarianSimultriaThemeAssets.CreateDesignAndSales(folder);
            Assert.That(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(assets.ThemeFamily), "ThemeFamily"), Is.Empty);
            Assert.That(AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(assets.DarkPalette), "DarkPalette"), Is.Empty);
            var role = assets.Roles[0];
            assets.DarkPalette.SetColor(role, Color.cyan);
            EditorUtility.SetDirty(assets.DarkPalette);
            var reused = DeucarianSimultriaThemeAssets.CreateDesignAndSales(folder);
            Assert.That(reused.ThemeFamily, Is.SameAs(assets.ThemeFamily));
            Assert.That(reused.ThemeFamily.name, Is.EqualTo("ThemeFamily"));
            Assert.That(reused.DarkPalette, Is.SameAs(assets.DarkPalette));
            Assert.That(reused.DarkPalette.GetColor(role), Is.EqualTo(Color.cyan));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(folder + "/Simultria DS.asset"), Is.Null);
        }

        [Test]
        public void SpecimenShowsBackgroundBehindThePanelSurface()
        {
            var assets = DeucarianSimultriaThemeAssets.CreateRealisationAndProgress(folder);
            var specimen = new DeucarianThemeToolkitSpecimen();
            specimen.Refresh(new DeucarianThemeManagerSelection(assets.ThemeFamily, DeucarianThemeMode.Dark, null));
            var backdrop = specimen.Root.Q("theme-preview-background");
            var panel = specimen.Root.Q("theme-specimen");
            Assert.That(panel.parent, Is.SameAs(backdrop));
            Assert.That(backdrop.style.backgroundColor.value, Is.EqualTo(assets.DarkPalette.GetColorById(DeucarianBuiltinColorRoleIds.Background)));
            Assert.That(panel.style.backgroundColor.value, Is.EqualTo(assets.DarkPalette.GetColorById(DeucarianBuiltinColorRoleIds.Surface)));
            Assert.That(panel.style.backgroundColor.value, Is.Not.EqualTo(backdrop.style.backgroundColor.value));
            Assert.That(specimen.Root.Q<Label>("theme-preview-surface-label").text, Is.EqualTo("Surface"));
        }

        [Test]
        public void SpecimenLabelsTheSurfaceTreatmentRatherThanImplyingAnUntreatedSwatch()
        {
            var assets = DeucarianSimultriaThemeAssets.CreateRealisationAndProgress(folder);
            var style = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            var surface = ScriptableObject.CreateInstance<DeucarianThemeSurfaceProfile>();
            try
            {
                style.SetComposition(surface, null, null, DeucarianThemeDensity.Standard);
                var specimen = new DeucarianThemeToolkitSpecimen();
                specimen.Refresh(new DeucarianThemeManagerSelection(assets.ThemeFamily, DeucarianThemeMode.Dark, style));
                var label = specimen.Root.Q<Label>("theme-preview-surface-label");
                Assert.That(label.text, Is.EqualTo("Surface · Frosted Glass"));
                Assert.That(label.tooltip, Does.Contain(ColorUtility.ToHtmlStringRGBA(assets.DarkPalette.GetColorById(DeucarianBuiltinColorRoleIds.Surface))));
                Assert.That(specimen.Root.Q("theme-specimen").style.backgroundColor.value,
                    Is.EqualTo(style.ResolveSurfaceColor(assets.DarkPalette.GetColorById(DeucarianBuiltinColorRoleIds.Surface))));
            }
            finally { UnityEngine.Object.DestroyImmediate(style); UnityEngine.Object.DestroyImmediate(surface); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProjectNavigationSelectsTheCurrentProjectFamilyAndPreservesDirtyWorkOnCancel(bool cancel)
        {
            var prior = DeucarianThemeManagerSelection.FromEditorPrefs();
            var settings = ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>();
            try
            {
                var ds = DeucarianSimultriaThemeAssets.CreateDesignAndSales(folder + "/DS");
                var rp = DeucarianSimultriaThemeAssets.CreateRealisationAndProgress(folder + "/RP");
                settings.Configure(rp.ThemeFamily, DeucarianThemeMode.Light);
                DeucarianThemingEditorSettings.SetDraftSelection(ds.ThemeFamily, DeucarianThemeMode.Dark, ds.DarkTheme.VisualStyle);
                ds.DarkPalette.SetColor(ds.Roles[0], Color.cyan);
                EditorUtility.SetDirty(ds.DarkPalette);
                bool prompted = false;
                bool opened = DeucarianThemeProjectNavigation.TrySelectProjectPalette(settings, true,
                    (_, message, __, ___) => { prompted = true; Assert.That(message, Does.Contain("will be kept")); return !cancel; });
                Assert.That(prompted, Is.True);
                Assert.That(opened, Is.EqualTo(!cancel));
                Assert.That(DeucarianThemingEditorSettings.ActiveThemeFamily, Is.SameAs(cancel ? ds.ThemeFamily : rp.ThemeFamily));
                Assert.That(DeucarianThemingEditorSettings.ActivePalette, Is.SameAs(cancel ? ds.DarkPalette : rp.LightPalette));
                Assert.That(ds.DarkPalette.GetColor(ds.Roles[0]), Is.EqualTo(Color.cyan));
            }
            finally
            {
                DeucarianThemingEditorSettings.SetDraftSelection(prior.Family, prior.Mode, prior.Style);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        private static void AssertColor(DeucarianColorPalette palette, string role, Color expected)
        {
            Assert.That(palette.TryGetColorById(role, out var value), Is.True);
            Assert.That(Vector4.Distance(value, expected), Is.LessThan(.00001f));
        }
    }
}
