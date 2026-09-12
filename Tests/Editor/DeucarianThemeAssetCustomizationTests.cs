using NUnit.Framework;
using UnityEditor;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemeAssetCustomizationTests
    {
        private const string Path = "Assets/__DeucarianThemeCustomizationTest.asset";
        [SetUp] public void SetUp() => Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(Path), "Do not overwrite an existing asset.");
        [TearDown] public void TearDown() => AssetDatabase.DeleteAsset(Path);

        [Test]
        public void AudioCustomizationCopiesPalettesButKeepsSharedClipsAndRoles()
        {
            var source = DeucarianAudioDefaults.LoadPaletteSet();
            Assert.NotNull(source);
            var copy = (DeucarianAudioPaletteSet)DeucarianThemeAssetCustomization.Copy(source, Path);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
            copy = AssetDatabase.LoadAssetAtPath<DeucarianAudioPaletteSet>(Path);
            Assert.AreNotSame(source.DefaultPalette, copy.DefaultPalette);
            Assert.AreEqual(Path, AssetDatabase.GetAssetPath(copy.DefaultPalette));
            Assert.That(new Deucarian.Editor.DeucarianEditorAssetCatalog(typeof(DeucarianAudioPalette)).Find(),
                Does.Contain(copy.DefaultPalette), "The shared chooser must include copied palette subassets.");
            Assert.AreSame(source.DefaultPalette.Entries[0].Role, copy.DefaultPalette.Entries[0].Role);
            Assert.AreSame(source.DefaultPalette.Entries[0].Cue.Clip, copy.DefaultPalette.Entries[0].Cue.Clip);
            for (int index = 0; index < source.Profiles.Count; index++)
            {
                Assert.AreNotSame(source.Profiles[index].Palette, copy.Profiles[index].Palette);
                Assert.AreEqual(Path, AssetDatabase.GetAssetPath(copy.Profiles[index].Palette));
            }
        }

        [Test]
        public void VisualCustomizationCopiesBothModesAndTheirEditableContent()
        {
            var source = DeucarianVisualDefaults.LoadFamily();
            Assert.NotNull(source);
            var copy = (DeucarianThemeFamily)DeucarianThemeAssetCustomization.Copy(source, Path);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);
            copy = AssetDatabase.LoadAssetAtPath<DeucarianThemeFamily>(Path);
            Assert.That(copy.IsComplete, Is.True);
            Assert.AreNotSame(source.LightTheme, copy.LightTheme);
            Assert.AreNotSame(source.DarkTheme, copy.DarkTheme);
            Assert.AreEqual(Path, AssetDatabase.GetAssetPath(copy.LightTheme));
            Assert.AreEqual(Path, AssetDatabase.GetAssetPath(copy.DarkTheme));
            Assert.That(AssetDatabase.GetAssetPath(source), Does.StartWith("Packages/"));
        }
    }
}
