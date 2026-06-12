using System.Collections.Generic;
using Deucarian.Theming.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianDefaultThemeAssetFactoryTests
    {
        private const string TestRoot = "Assets/DeucarianThemingEditorTests";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [Test]
        public void DefaultAssetCreationCreatesRoleAssetsPaletteAndTheme()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(TestRoot + "/Defaults");

            Assert.NotNull(assets.RoleLibrary);
            Assert.NotNull(assets.Palette);
            Assert.NotNull(assets.Theme);
            Assert.GreaterOrEqual(assets.Roles.Count, 32);
            Assert.IsTrue(AssetDatabase.Contains(assets.RoleLibrary));
            Assert.IsTrue(AssetDatabase.Contains(assets.Palette));
            Assert.IsTrue(AssetDatabase.Contains(assets.Theme));
            Assert.IsTrue(AssetDatabase.IsValidFolder(TestRoot + "/Defaults/Roles"));
            Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.UiNormal, out _));
            Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.UiDisabled, out _));
            Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.ItemLegendary, out _));
        }

        [Test]
        public void DuplicateValidationDetectsDuplicateIds()
        {
            DeucarianColorRole first = ScriptableObject.CreateInstance<DeucarianColorRole>();
            DeucarianColorRole second = ScriptableObject.CreateInstance<DeucarianColorRole>();
            DeucarianColorRoleLibrary library = ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>();

            try
            {
                first.Configure("deucarian.test.duplicate", "Duplicate A", "Tests", string.Empty, Color.white, false);
                second.Configure("deucarian.test.duplicate", "Duplicate B", "Tests", string.Empty, Color.black, false);
                library.AddRole(first);
                library.AddRole(second);

                List<string> duplicates = library.GetDuplicateRoleIds();
                CollectionAssert.Contains(duplicates, "deucarian.test.duplicate");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(library);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }
    }
}
