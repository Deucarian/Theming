using System;
using Deucarian.Theming.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemeAssetStoreTests
    {
        private string root;

        [SetUp]
        public void SetUp()
        {
            root = "Assets/ThemeAssetStoreTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", root.Substring("Assets/".Length));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(root);
        }

        [Test]
        public void ReusingAnAssetPreservesItsIdentityAndRepairsOnlyItsObjectName()
        {
            string path = root + "/Palette.asset";
            var first = DeucarianThemeAssetStore.LoadOrCreateAsset(
                path, ScriptableObject.CreateInstance<DeucarianColorPalette>, false, out bool created);
            Assert.IsTrue(created);
            first.name = "Renamed object";
            first.Configure("custom.id", "My palette", null);
            string guid = AssetDatabase.AssetPathToGUID(path);

            var second = DeucarianThemeAssetStore.LoadOrCreateAsset<DeucarianColorPalette>(
                path, () => throw new InvalidOperationException("Must reuse existing asset"), false, out created);

            Assert.IsFalse(created);
            Assert.AreSame(first, second);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.AreEqual("Palette", second.name);
            Assert.AreEqual("custom.id", second.PaletteId);
        }

        [Test]
        public void ConflictingTypeUsesAUniquePathWithoutDeletingTheExistingAsset()
        {
            string path = root + "/Shared.asset";
            var theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            AssetDatabase.CreateAsset(theme, path);
            string guid = AssetDatabase.AssetPathToGUID(path);

            var palette = DeucarianThemeAssetStore.LoadOrCreateAsset(
                path, ScriptableObject.CreateInstance<DeucarianColorPalette>, false, out bool created);

            Assert.IsTrue(created);
            Assert.AreSame(theme, AssetDatabase.LoadMainAssetAtPath(path));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.AreNotEqual(path, AssetDatabase.GetAssetPath(palette));
        }

        [Test]
        public void CreatingFoldersIsIdempotentAndPreservesExistingAssets()
        {
            string nested = root + "/Support/Roles";
            DeucarianThemeAssetStore.EnsureFolder(nested);
            string guid = AssetDatabase.AssetPathToGUID(nested);
            DeucarianThemeAssetStore.EnsureFolder(nested + "/");
            Assert.IsTrue(AssetDatabase.IsValidFolder(nested));
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(nested));
        }

        [TestCase(null, "")]
        [TestCase("  ", "")]
        [TestCase(" Assets\\Themes\\ ", "Assets/Themes")]
        public void PublicSettingsNormalizationUsesTheSharedNamingPolicy(string input, string expected)
        {
            Assert.AreEqual(expected, DeucarianThemeAssetNaming.NormalizeAssetPath(input));
            Assert.AreEqual(expected, DeucarianThemingEditorSettings.NormalizeAssetPath(input));
        }

        [TestCase("Library/Theme.asset")]
        [TestCase("Assets/Themes/Theme.txt")]
        [TestCase("AssetsElsewhere/Theme.asset")]
        public void AssetPathValidationRejectsNonProjectAssetPaths(string path)
        {
            Assert.Throws<ArgumentException>(() => DeucarianThemeAssetNaming.ValidateAssetPath(path, "path"));
        }
    }
}
