using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianBundledDefaultsTests
    {
        [TestCase(DeucarianThemeMode.Light)]
        [TestCase(DeucarianThemeMode.Dark)]
        public void InstalledDefaultsAreCompleteAssetsWithCanonicalColorsAndAudio(DeucarianThemeMode mode)
        {
            var family = DeucarianVisualDefaults.LoadFamily();
            Assert.That(family, Is.Not.Null);
            Assert.That(family.IsComplete, Is.True);
            var theme = DeucarianVisualDefaults.LoadTheme(mode);
            Assert.That(AssetDatabase.GetAssetPath(theme), Does.StartWith(DeucarianBundledVisualAssetGenerator.Root));
            Assert.That(theme.ColorPalette.ThemeMode, Is.EqualTo(mode));
            Assert.That(theme.VisualStyle.TypographyProfile, Is.Not.Null);
            Assert.That(theme.AudioPaletteSet, Is.SameAs(DeucarianAudioDefaults.LoadPaletteSet()));
            foreach (var definition in DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions(mode))
            {
                Assert.That(theme.TryGetColorById(definition.Id, out var color), Is.True, definition.Id);
                Assert.That(color, Is.EqualTo(definition.DefaultColor), definition.Id);
            }
            foreach (DeucarianAudioExperience experience in System.Enum.GetValues(typeof(DeucarianAudioExperience)))
            {
                Assert.That(theme.AudioPaletteSet.TryResolveById(DeucarianBuiltinAudioRoleIds.Warning, experience, out var cue), Is.True);
                Assert.That(cue.Cue.TrySelectVariant(1, -1, out var clip, out _), Is.True);
                Assert.That(clip.samples, Is.GreaterThan(0));
            }
        }

        [Test]
        public void EmptySettingsUseBundledThemeWithoutReplacingExplicitOverridesOrFeatureFlags()
        {
            var settings = ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>();
            var custom = ScriptableObject.CreateInstance<DeucarianTheme>();
            try
            {
                var resolve = typeof(DeucarianThemeRuntimeResolver).GetMethod("ResolveDefaultThemeFromSettings", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(resolve.Invoke(null, new object[] { null, null }), Is.SameAs(DeucarianVisualDefaults.LoadTheme()));
                settings.SetFeatures(false, false);
                Assert.That(resolve.Invoke(null, new object[] { settings, null }), Is.SameAs(DeucarianVisualDefaults.LoadTheme()));
                Assert.That(settings.UseVisualStyling, Is.False);
                Assert.That(settings.UseAudio, Is.False);
                settings.Configure(custom);
                Assert.That(resolve.Invoke(null, new object[] { settings, null }), Is.SameAs(custom));
                Assert.That(settings.LegacyDefaultTheme, Is.SameAs(custom));
            }
            finally { Object.DestroyImmediate(settings); Object.DestroyImmediate(custom); }
        }
    }
}
