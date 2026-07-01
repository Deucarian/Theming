using System.Collections.Generic;
using System.Reflection;
using Deucarian.Theming.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemingMenuToolsTests
    {
        private const string TestRootBase = "Assets/DeucarianThemingMenuEditorTests";

        private string previousThemeGuid;
        private string previousPaletteGuid;
        private string previousRoleLibraryGuid;
        private string previousStyleGuid;
        private string previousDefaultAssetFolder;
        private string testRoot;

        [SetUp]
        public void SetUp()
        {
            previousThemeGuid = DeucarianThemingEditorSettings.ActiveThemeGuid;
            previousPaletteGuid = DeucarianThemingEditorSettings.ActivePaletteGuid;
            previousRoleLibraryGuid = DeucarianThemingEditorSettings.ActiveRoleLibraryGuid;
            previousStyleGuid = DeucarianThemingEditorSettings.ActiveStyleGuid;
            previousDefaultAssetFolder = DeucarianThemingEditorSettings.DefaultAssetFolder;
            testRoot = TestRootBase + "/" + System.Guid.NewGuid().ToString("N");

            AssetDatabase.DeleteAsset(testRoot);
            DeucarianThemingEditorSettings.ClearActiveAssets();
            DeucarianThemingEditorSettings.DefaultAssetFolder = testRoot + "/Defaults";
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(testRoot);
            DeucarianThemingEditorSettings.ActiveThemeGuid = previousThemeGuid;
            DeucarianThemingEditorSettings.ActivePaletteGuid = previousPaletteGuid;
            DeucarianThemingEditorSettings.ActiveRoleLibraryGuid = previousRoleLibraryGuid;
            DeucarianThemingEditorSettings.ActiveStyleGuid = previousStyleGuid;
            DeucarianThemingEditorSettings.DefaultAssetFolder = previousDefaultAssetFolder;
        }

        [Test]
        public void TopMenuContainsOnlyQuickEntryPoints()
        {
            string[] expectedMenuItems =
            {
                "Tools/Deucarian/Theming/Open Theme Manager",
                "Tools/Deucarian/Theming/Create Minimal Palette"
            };

            List<string> actualMenuItems = new List<string>();
            MethodInfo[] methods = typeof(DeucarianThemingMenu).GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            for (int i = 0; i < methods.Length; i++)
            {
                object[] attributes = methods[i].GetCustomAttributes(typeof(MenuItem), false);
                for (int j = 0; j < attributes.Length; j++)
                {
                    actualMenuItems.Add(((MenuItem)attributes[j]).menuItem);
                }
            }

            CollectionAssert.AreEquivalent(expectedMenuItems, actualMenuItems);
        }

        [Test]
        public void SettingsStoreAndResolveActiveThemeGuid()
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");

            DeucarianThemingEditorSettings.ActiveTheme = theme;

            Assert.IsNotEmpty(DeucarianThemingEditorSettings.ActiveThemeGuid);
            Assert.AreSame(theme, DeucarianThemingEditorSettings.ActiveTheme);
        }

        [Test]
        public void SettingsStoreAndResolveActiveStyleGuid()
        {
            DeucarianThemeStyle style = CreateAsset<DeucarianThemeStyle>(testRoot + "/Style.asset");

            DeucarianThemingEditorSettings.ActiveStyle = style;

            Assert.IsNotEmpty(DeucarianThemingEditorSettings.ActiveStyleGuid);
            Assert.AreSame(style, DeucarianThemingEditorSettings.ActiveStyle);
        }

        [Test]
        public void FindExistingAssetsReturnsThemesPalettesAndRoleLibraries()
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>(testRoot + "/Palette.asset");
            DeucarianColorRoleLibrary roleLibrary = CreateAsset<DeucarianColorRoleLibrary>(testRoot + "/Role Library.asset");
            DeucarianThemeStyle style = CreateAsset<DeucarianThemeStyle>(testRoot + "/Style.asset");

            DeucarianThemingMenuActions.AssetSearchResult result =
                DeucarianThemingMenuActions.FindExistingAssets(new[] { testRoot });

            CollectionAssert.Contains(result.Themes, theme);
            CollectionAssert.Contains(result.Palettes, palette);
            CollectionAssert.Contains(result.RoleLibraries, roleLibrary);
            CollectionAssert.Contains(result.Styles, style);
        }

        [Test]
        public void CreateMissingDefaultsCreatesScriptableObjectAssets()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets(testRoot + "/Defaults");

            Assert.NotNull(assets.Theme);
            Assert.NotNull(assets.Palette);
            Assert.NotNull(assets.RoleLibrary);
            Assert.NotNull(assets.DefaultStyle);
            Assert.IsTrue(AssetDatabase.Contains(assets.Theme));
            Assert.IsTrue(AssetDatabase.Contains(assets.Palette));
            Assert.IsTrue(AssetDatabase.Contains(assets.RoleLibrary));
            Assert.IsTrue(AssetDatabase.Contains(assets.DefaultStyle));
            Assert.AreSame(assets.Theme, DeucarianThemingEditorSettings.ActiveTheme);
            Assert.AreSame(assets.Palette, DeucarianThemingEditorSettings.ActivePalette);
            Assert.AreSame(assets.RoleLibrary, DeucarianThemingEditorSettings.ActiveRoleLibrary);
            Assert.AreSame(assets.DefaultStyle, DeucarianThemingEditorSettings.ActiveStyle);
        }

        [Test]
        public void SelectActiveThemeCreatesDefaultsWhenNoThemeExistsInSearch()
        {
            DeucarianTheme theme = DeucarianThemingMenuActions.ResolveOrCreateActiveTheme(
                false,
                new[] { testRoot + "/EmptySearch" },
                testRoot + "/Defaults");

            Assert.NotNull(theme);
            Assert.IsTrue(AssetDatabase.Contains(theme));
            Assert.AreSame(theme, DeucarianThemingEditorSettings.ActiveTheme);
        }

        [Test]
        public void AssignActiveStyleToActiveThemeStoresStyleOnTheme()
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            DeucarianThemeStyle style = CreateAsset<DeucarianThemeStyle>(testRoot + "/Style.asset");
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            DeucarianThemingEditorSettings.ActiveStyle = style;

            Assert.IsTrue(DeucarianThemingMenuActions.AssignActiveStyleToActiveTheme());

            Assert.AreSame(style, theme.VisualStyle);
        }

        [Test]
        public void ApplyingActiveThemeAssignsItToProviders()
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            GameObject gameObject = new GameObject("Theme Provider Test");
            DeucarianThemeProvider provider = gameObject.AddComponent<DeucarianThemeProvider>();

            try
            {
                int applied = DeucarianThemingMenuActions.ApplyActiveThemeToOpenScene(false, false);

                Assert.GreaterOrEqual(applied, 1);
                Assert.AreSame(theme, provider.CurrentTheme);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static T CreateAsset<T>(string path)
            where T : ScriptableObject
        {
            string folder = path.Substring(0, path.LastIndexOf('/'));
            DeucarianThemingMenuActions.EnsureAssetFolder(folder);
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }
    }
}
