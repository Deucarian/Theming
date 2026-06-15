using System.Collections.Generic;
using Deucarian.Theming.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianDefaultThemeAssetFactoryTests
    {
        private const string TestRootBase = "Assets/DeucarianThemingEditorTests";
        private const float ColorTolerance = 0.0001f;

        private static readonly string[] RequiredMinimalRoleIds =
        {
            DeucarianBuiltinColorRoleIds.Core.Background,
            DeucarianBuiltinColorRoleIds.Core.Surface,
            DeucarianBuiltinColorRoleIds.Core.SurfaceRaised,
            DeucarianBuiltinColorRoleIds.Core.Primary,
            DeucarianBuiltinColorRoleIds.Core.Secondary,
            DeucarianBuiltinColorRoleIds.Core.Accent,
            DeucarianBuiltinColorRoleIds.Text.Primary,
            DeucarianBuiltinColorRoleIds.Text.Secondary,
            DeucarianBuiltinColorRoleIds.Text.Muted,
            DeucarianBuiltinColorRoleIds.Text.Disabled,
            DeucarianBuiltinColorRoleIds.Status.Success,
            DeucarianBuiltinColorRoleIds.Status.Warning,
            DeucarianBuiltinColorRoleIds.Status.Error,
            DeucarianBuiltinColorRoleIds.Status.Info,
            DeucarianBuiltinColorRoleIds.UI.Normal,
            DeucarianBuiltinColorRoleIds.UI.Highlighted,
            DeucarianBuiltinColorRoleIds.UI.Pressed,
            DeucarianBuiltinColorRoleIds.UI.Selected,
            DeucarianBuiltinColorRoleIds.UI.Disabled,
            DeucarianBuiltinColorRoleIds.UI.Focused
        };

        private static readonly string[] GameRoleIds =
        {
            DeucarianBuiltinColorRoleIds.Gameplay.Health,
            DeucarianBuiltinColorRoleIds.Gameplay.Mana,
            DeucarianBuiltinColorRoleIds.Gameplay.Stamina,
            DeucarianBuiltinColorRoleIds.Gameplay.Experience,
            DeucarianBuiltinColorRoleIds.Gameplay.Interactable,
            DeucarianBuiltinColorRoleIds.Gameplay.Highlight,
            DeucarianBuiltinColorRoleIds.Faction.Ally,
            DeucarianBuiltinColorRoleIds.Faction.Enemy,
            DeucarianBuiltinColorRoleIds.Faction.Neutral,
            DeucarianBuiltinColorRoleIds.ItemRarity.Common,
            DeucarianBuiltinColorRoleIds.ItemRarity.Uncommon,
            DeucarianBuiltinColorRoleIds.ItemRarity.Rare,
            DeucarianBuiltinColorRoleIds.ItemRarity.Epic,
            DeucarianBuiltinColorRoleIds.ItemRarity.Legendary
        };

        private string previousThemeGuid;
        private string previousPaletteGuid;
        private string previousRoleLibraryGuid;
        private string previousDefaultAssetFolder;
        private string testRoot;

        [SetUp]
        public void SetUp()
        {
            previousThemeGuid = DeucarianThemingEditorSettings.ActiveThemeGuid;
            previousPaletteGuid = DeucarianThemingEditorSettings.ActivePaletteGuid;
            previousRoleLibraryGuid = DeucarianThemingEditorSettings.ActiveRoleLibraryGuid;
            previousDefaultAssetFolder = DeucarianThemingEditorSettings.DefaultAssetFolder;
            testRoot = TestRootBase + "/" + System.Guid.NewGuid().ToString("N");

            AssetDatabase.DeleteAsset(testRoot);
            ClearActiveSelections();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(testRoot);
            DeucarianThemingEditorSettings.ActiveThemeGuid = previousThemeGuid;
            DeucarianThemingEditorSettings.ActivePaletteGuid = previousPaletteGuid;
            DeucarianThemingEditorSettings.ActiveRoleLibraryGuid = previousRoleLibraryGuid;
            DeucarianThemingEditorSettings.DefaultAssetFolder = previousDefaultAssetFolder;
        }

        [Test]
        public void DefaultAssetCreationCreatesRoleAssetsPaletteAndTheme()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(testRoot + "/Defaults");

            Assert.NotNull(assets.RoleLibrary);
            Assert.NotNull(assets.Palette);
            Assert.NotNull(assets.Theme);
            Assert.AreEqual(RequiredMinimalRoleIds.Length, assets.Roles.Count);
            Assert.IsTrue(AssetDatabase.Contains(assets.RoleLibrary));
            Assert.IsTrue(AssetDatabase.Contains(assets.Palette));
            Assert.IsTrue(AssetDatabase.Contains(assets.Theme));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Defaults/Roles"));
            AssertRequiredRolesExist(assets.RoleLibrary, RequiredMinimalRoleIds);
        }

        [Test]
        public void MinimalDefaultRoleLibraryContainsNoGameItemOrFactionRoles()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(testRoot + "/Defaults");

            Assert.IsFalse(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.Gameplay.Health, out _));
            Assert.IsFalse(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.ItemRarity.Legendary, out _));
            Assert.IsFalse(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.Faction.Enemy, out _));

            IReadOnlyList<DeucarianColorRole> roles = assets.RoleLibrary.Roles;
            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianColorRole role = roles[i];
                Assert.NotNull(role);
                Assert.False(role.Id.StartsWith("deucarian.game", System.StringComparison.Ordinal), role.Id);
                Assert.False(role.Id.StartsWith("deucarian.item", System.StringComparison.Ordinal), role.Id);
                Assert.AreNotEqual(DeucarianColorRoleCategories.Gameplay, role.Category);
                Assert.AreNotEqual(DeucarianColorRoleCategories.ItemRarity, role.Category);
                Assert.AreNotEqual(DeucarianColorRoleCategories.Faction, role.Category);
            }
        }

        [Test]
        public void GameThemeAssetCreationContainsGameplayItemRarityAndFactionRoles()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateGameThemeAssets(testRoot + "/Game");

            Assert.NotNull(assets.RoleLibrary);
            Assert.NotNull(assets.Palette);
            Assert.NotNull(assets.Theme);
            Assert.AreEqual(GameRoleIds.Length, assets.Roles.Count);
            AssertRequiredRolesExist(assets.RoleLibrary, GameRoleIds);
        }

        [Test]
        public void DefaultPaletteHasNoMagentaFallbackColors()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(testRoot + "/Defaults");

            IReadOnlyList<DeucarianColorEntry> entries = assets.Palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                Assert.NotNull(entry);
                Assert.NotNull(entry.Role);
                Assert.False(ColorsMatch(DeucarianColorPalette.MissingColor, entry.Color), entry.Role.Id);
                Assert.False(ColorsMatch(DeucarianColorPalette.MissingColor, entry.Role.DefaultColor), entry.Role.Id);
            }
        }

        [Test]
        public void DefaultPaletteUsesExpectedDeucarianBrandColors()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(testRoot + "/Defaults");

            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Core.Background, "#0D1218");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Core.Surface, "#1A2330");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Core.SurfaceRaised, "#2C3A4D");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Core.Primary, "#5A6FA0");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Core.Secondary, "#3BA69A");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Core.Accent, "#276065");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Text.Primary, "#C4CAD1");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Text.Secondary, "#A8B0BA");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Text.Muted, "#6F7A86");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Text.Disabled, "#3C444F");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Status.Success, "#3BA69A");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Status.Warning, "#A87932");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Status.Error, "#A04444");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.Status.Info, "#5A6FA0");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.UI.Normal, "#1A2330");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.UI.Highlighted, "#2C3A4D");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.UI.Pressed, "#276065");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.UI.Selected, "#3BA69A");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.UI.Disabled, "#3C444F");
            AssertPaletteColor(assets.Palette, DeucarianBuiltinColorRoleIds.UI.Focused, "#5A6FA0");
        }

        [Test]
        public void BuiltinRoleIdFlatConstantsStillExistAndMatchNestedGroups()
        {
            Assert.AreEqual(DeucarianBuiltinColorRoleIds.Core.Background, DeucarianBuiltinColorRoleIds.Background);
            Assert.AreEqual(DeucarianBuiltinColorRoleIds.Text.Primary, DeucarianBuiltinColorRoleIds.TextPrimary);
            Assert.AreEqual(DeucarianBuiltinColorRoleIds.Text.Muted, DeucarianBuiltinColorRoleIds.TextMuted);
            Assert.AreEqual(DeucarianBuiltinColorRoleIds.Status.Info, DeucarianBuiltinColorRoleIds.Info);
            Assert.AreEqual(DeucarianBuiltinColorRoleIds.UI.Normal, DeucarianBuiltinColorRoleIds.UiNormal);
            Assert.AreEqual(DeucarianBuiltinColorRoleIds.Gameplay.Health, DeucarianBuiltinColorRoleIds.Health);
            Assert.AreEqual(DeucarianBuiltinColorRoleIds.ItemRarity.Legendary, DeucarianBuiltinColorRoleIds.ItemLegendary);
            Assert.AreEqual(DeucarianBuiltinColorRoleIds.Faction.Enemy, DeucarianBuiltinColorRoleIds.Enemy);
        }

        [Test]
        public void DefaultAssetCreationStoresActiveAssetGuids()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets(testRoot + "/Defaults");

            Assert.AreEqual(assets.Theme, DeucarianThemingEditorSettings.ActiveTheme);
            Assert.AreEqual(assets.Palette, DeucarianThemingEditorSettings.ActivePalette);
            Assert.AreEqual(assets.RoleLibrary, DeucarianThemingEditorSettings.ActiveRoleLibrary);
        }

        [Test]
        public void ThemeManagerWindowCanOpen()
        {
            if (Application.isBatchMode)
            {
                Assert.Pass("EditorWindow display checks are skipped in batch mode.");
            }

            DeucarianThemeManagerWindow.OpenWindow();

            DeucarianThemeManagerWindow[] windows =
                Resources.FindObjectsOfTypeAll<DeucarianThemeManagerWindow>();

            Assert.GreaterOrEqual(windows.Length, 1);

            for (int i = 0; i < windows.Length; i++)
            {
                windows[i].Close();
            }
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

        private static void ClearActiveSelections()
        {
            DeucarianThemingEditorSettings.ClearActiveAssets();
        }

        private static void AssertRequiredRolesExist(DeucarianColorRoleLibrary library, IReadOnlyList<string> roleIds)
        {
            for (int i = 0; i < roleIds.Count; i++)
            {
                Assert.IsTrue(library.TryGetRoleById(roleIds[i], out _), roleIds[i]);
            }
        }

        private static void AssertPaletteColor(DeucarianColorPalette palette, string roleId, string expectedHex)
        {
            Assert.True(palette.TryGetColorById(roleId, out Color color), roleId);
            AssertColor(expectedHex, color);
        }

        private static void AssertColor(string expectedHex, Color actual)
        {
            Assert.True(ColorUtility.TryParseHtmlString(expectedHex, out Color expected), expectedHex);
            Assert.True(ColorsMatch(expected, actual), $"{expectedHex} != {actual}");
        }

        private static bool ColorsMatch(Color expected, Color actual)
        {
            return Mathf.Abs(expected.r - actual.r) <= ColorTolerance
                && Mathf.Abs(expected.g - actual.g) <= ColorTolerance
                && Mathf.Abs(expected.b - actual.b) <= ColorTolerance
                && Mathf.Abs(expected.a - actual.a) <= ColorTolerance;
        }
    }
}
