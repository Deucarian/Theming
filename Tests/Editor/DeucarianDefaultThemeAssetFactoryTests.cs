using System.Collections.Generic;
using System.IO;
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

        private static readonly string[] RequiredStyleIds =
        {
            DeucarianThemeStyleIds.FrostedGlass,
            DeucarianThemeStyleIds.MaterialDark,
            DeucarianThemeStyleIds.FluentAcrylic
        };

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
            ClearActiveSelections();
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
        public void DefaultAssetCreationCreatesRoleAssetsPaletteAndTheme()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(testRoot + "/Defaults");

            Assert.NotNull(assets.RoleLibrary);
            Assert.NotNull(assets.Palette);
            Assert.NotNull(assets.Theme);
            Assert.NotNull(assets.DefaultStyle);
            Assert.AreEqual(RequiredMinimalRoleIds.Length, assets.Roles.Count);
            Assert.AreEqual(RequiredStyleIds.Length, assets.Styles.Count);
            Assert.IsTrue(AssetDatabase.Contains(assets.RoleLibrary));
            Assert.IsTrue(AssetDatabase.Contains(assets.Palette));
            Assert.IsTrue(AssetDatabase.Contains(assets.Theme));
            Assert.IsTrue(AssetDatabase.Contains(assets.DefaultStyle));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Defaults/Roles"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Defaults/Styles"));
            AssertRequiredRolesExist(assets.RoleLibrary, RequiredMinimalRoleIds);
            AssertRequiredStylesExist(assets.Styles, RequiredStyleIds);
            Assert.AreSame(assets.DefaultStyle, assets.Theme.VisualStyle);
        }

        [Test]
        public void BuiltinStyleAssetCreationCreatesFrostedMaterialAndFluentStyles()
        {
            IReadOnlyList<DeucarianThemeStyle> styles =
                DeucarianDefaultThemeAssetFactory.CreateBuiltinThemeStyleAssets(testRoot + "/Styles");

            Assert.AreEqual(RequiredStyleIds.Length, styles.Count);
            AssertRequiredStylesExist(styles, RequiredStyleIds);
            AssertStyle(styles, DeucarianThemeStyleIds.FrostedGlass, DeucarianThemeStyleSurfaceTreatment.FrostedGlass, true);
            AssertStyle(styles, DeucarianThemeStyleIds.MaterialDark, DeucarianThemeStyleSurfaceTreatment.Material, false);
            AssertStyle(styles, DeucarianThemeStyleIds.FluentAcrylic, DeucarianThemeStyleSurfaceTreatment.FluentAcrylic, true);
        }

        [Test]
        public void CreateMinimalPaletteCreatesPaletteThemeLibraryAndRoles()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianThemingMenuActions.CreateMinimalPalette(testRoot + "/SimultriaPalette.asset");

            Assert.NotNull(assets.Palette);
            Assert.NotNull(assets.Theme);
            Assert.NotNull(assets.RoleLibrary);
            Assert.AreEqual(RequiredMinimalRoleIds.Length, assets.Roles.Count);
            Assert.AreSame(assets.Palette, assets.Theme.ColorPalette);
            Assert.AreSame(assets.RoleLibrary, assets.Palette.RoleLibrary);
            Assert.AreSame(assets.Palette, DeucarianThemingEditorSettings.ActivePalette);
            Assert.AreSame(assets.Theme, DeucarianThemingEditorSettings.ActiveTheme);
            Assert.AreSame(assets.RoleLibrary, DeucarianThemingEditorSettings.ActiveRoleLibrary);
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/SimultriaPalette Support/Roles"));
            AssertRequiredRolesExist(assets.RoleLibrary, RequiredMinimalRoleIds);
        }

        [Test]
        public void RepairPaletteSetupFillsMissingSupportAssetsAndReferences()
        {
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>(testRoot + "/StandalonePalette.asset");

            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.RepairPaletteSetup(palette);

            Assert.NotNull(assets.RoleLibrary);
            Assert.NotNull(assets.Theme);
            Assert.AreSame(palette, assets.Palette);
            Assert.AreSame(assets.RoleLibrary, palette.RoleLibrary);
            Assert.AreSame(palette, assets.Theme.ColorPalette);
            AssertRequiredRolesExist(assets.RoleLibrary, RequiredMinimalRoleIds);

            for (int i = 0; i < RequiredMinimalRoleIds.Length; i++)
            {
                Assert.True(palette.TryGetColorById(RequiredMinimalRoleIds[i], out _), RequiredMinimalRoleIds[i]);
            }
        }

        [Test]
        public void RepairPaletteSetupDoesNotOverwriteUserSetColors()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(testRoot + "/CustomPalette.asset");
            Assert.True(assets.RoleLibrary.TryGetRoleById(
                DeucarianBuiltinColorRoleIds.Core.Primary,
                out DeucarianColorRole primaryRole));

            Color userColor = new Color(0.12f, 0.34f, 0.56f, 1f);
            assets.Palette.SetColor(primaryRole, userColor, "User color");
            EditorUtility.SetDirty(assets.Palette);
            AssetDatabase.SaveAssets();

            DeucarianDefaultThemeAssets repaired = DeucarianDefaultThemeAssetFactory.RepairPaletteSetup(assets.Palette);

            Assert.True(repaired.Palette.TryGetColor(primaryRole, out Color repairedColor));
            Assert.True(ColorsMatch(userColor, repairedColor), repairedColor.ToString());
        }

        [Test]
        public void GeneratedAssetObjectNamesMatchFileNames()
        {
            DeucarianDefaultThemeAssets defaultAssets =
                DeucarianDefaultThemeAssetFactory.CreateDefaultThemeAssets(testRoot + "/Defaults");
            DeucarianDefaultThemeAssets minimalAssets =
                DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(testRoot + "/SimultriaPalette.asset");

            AssertObjectNameMatchesFile(defaultAssets.RoleLibrary);
            AssertObjectNameMatchesFile(defaultAssets.Palette);
            AssertObjectNameMatchesFile(defaultAssets.Theme);
            AssertObjectNameMatchesFile(defaultAssets.DefaultStyle);
            AssertObjectNameMatchesFile(minimalAssets.RoleLibrary);
            AssertObjectNameMatchesFile(minimalAssets.Palette);
            AssertObjectNameMatchesFile(minimalAssets.Theme);

            for (int i = 0; i < defaultAssets.Roles.Count; i++)
            {
                AssertObjectNameMatchesFile(defaultAssets.Roles[i]);
            }

            for (int i = 0; i < minimalAssets.Roles.Count; i++)
            {
                AssertObjectNameMatchesFile(minimalAssets.Roles[i]);
            }

            for (int i = 0; i < defaultAssets.Styles.Count; i++)
            {
                AssertObjectNameMatchesFile(defaultAssets.Styles[i]);
            }
        }

        [Test]
        public void GeneratedDisplayNamesCanDifferFromAssetNames()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(testRoot + "/SimultriaPalette.asset");

            Assert.AreEqual("SimultriaPalette", assets.Palette.name);
            Assert.AreEqual("Simultria Palette", assets.Palette.DisplayName);
            Assert.AreEqual("SimultriaTheme", assets.Theme.name);
            Assert.AreEqual("Simultria Theme", assets.Theme.DisplayName);
        }

        [Test]
        public void MinimalPaletteContainsNoGameSpecificRoles()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(testRoot + "/MinimalPalette.asset");

            Assert.IsFalse(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.Gameplay.Health, out _));
            Assert.IsFalse(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.ItemRarity.Legendary, out _));
            Assert.IsFalse(assets.RoleLibrary.TryGetRoleById(DeucarianBuiltinColorRoleIds.Faction.Enemy, out _));
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
            Assert.AreEqual(assets.DefaultStyle, DeucarianThemingEditorSettings.ActiveStyle);
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

        [Test]
        public void RepairGeneratedAssetNamesMatchesMainObjectNamesToFiles()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(testRoot + "/NameRepairPalette.asset");
            assets.Palette.name = "Name Repair Palette";
            assets.Theme.name = "Name Repair Theme";
            EditorUtility.SetDirty(assets.Palette);
            EditorUtility.SetDirty(assets.Theme);
            AssetDatabase.SaveAssets();

            int repaired = DeucarianThemingMenuActions.RepairGeneratedAssetNames(new[] { testRoot });

            Assert.AreEqual(2, repaired);
            AssertObjectNameMatchesFile(assets.Palette);
            AssertObjectNameMatchesFile(assets.Theme);
            Assert.AreEqual("Name Repair Palette", assets.Palette.DisplayName);
            Assert.AreEqual("Name Repair Theme", assets.Theme.DisplayName);
        }

        private static void ClearActiveSelections()
        {
            DeucarianThemingEditorSettings.ClearActiveAssets();
        }

        private static T CreateAsset<T>(string path)
            where T : ScriptableObject
        {
            string folder = path.Substring(0, path.LastIndexOf('/'));
            DeucarianThemingMenuActions.EnsureAssetFolder(folder);
            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private static void AssertRequiredRolesExist(DeucarianColorRoleLibrary library, IReadOnlyList<string> roleIds)
        {
            for (int i = 0; i < roleIds.Count; i++)
            {
                Assert.IsTrue(library.TryGetRoleById(roleIds[i], out _), roleIds[i]);
            }
        }

        private static void AssertRequiredStylesExist(IReadOnlyList<DeucarianThemeStyle> styles, IReadOnlyList<string> styleIds)
        {
            for (int i = 0; i < styleIds.Count; i++)
            {
                Assert.IsTrue(ContainsStyle(styles, styleIds[i]), styleIds[i]);
            }
        }

        private static void AssertStyle(
            IReadOnlyList<DeucarianThemeStyle> styles,
            string styleId,
            DeucarianThemeStyleSurfaceTreatment treatment,
            bool usesTexture)
        {
            DeucarianThemeStyle style = FindStyle(styles, styleId);
            Assert.NotNull(style, styleId);
            Assert.AreEqual(treatment, style.SurfaceTreatment);
            Assert.AreEqual(usesTexture, style.UseGeneratedNoiseTexture);
            Assert.IsFalse(string.IsNullOrWhiteSpace(style.Description));
        }

        private static bool ContainsStyle(IReadOnlyList<DeucarianThemeStyle> styles, string styleId)
        {
            return FindStyle(styles, styleId) != null;
        }

        private static DeucarianThemeStyle FindStyle(IReadOnlyList<DeucarianThemeStyle> styles, string styleId)
        {
            for (int i = 0; i < styles.Count; i++)
            {
                DeucarianThemeStyle style = styles[i];
                if (style != null && style.StyleId == styleId)
                {
                    return style;
                }
            }

            return null;
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

        private static void AssertObjectNameMatchesFile(Object asset)
        {
            Assert.NotNull(asset);
            string path = AssetDatabase.GetAssetPath(asset);
            Assert.IsNotEmpty(path);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(path), asset.name);
        }
    }
}
