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
        private string previousThemeFamilyGuid;
        private DeucarianThemeMode previousThemeMode;
        private string previousPaletteGuid;
        private string previousRoleLibraryGuid;
        private string previousStyleGuid;
        private string previousDefaultAssetFolder;
        private string testRoot;

        [SetUp]
        public void SetUp()
        {
            previousThemeGuid = DeucarianThemingEditorSettings.ActiveThemeGuid;
            previousThemeFamilyGuid = DeucarianThemingEditorSettings.ActiveThemeFamilyGuid;
            previousThemeMode = DeucarianThemingEditorSettings.ActiveThemeMode;
            previousPaletteGuid = DeucarianThemingEditorSettings.ActivePaletteGuid;
            previousRoleLibraryGuid = DeucarianThemingEditorSettings.ActiveRoleLibraryGuid;
            previousStyleGuid = DeucarianThemingEditorSettings.ActiveStyleGuid;
            previousDefaultAssetFolder = DeucarianThemingEditorSettings.DefaultAssetFolder;
            testRoot = TestRootBase + "/" + System.Guid.NewGuid().ToString("N");

            AssetDatabase.DeleteAsset(testRoot);
            ClearActiveSelections();
            DeucarianThemingEditorSettings.ActiveThemeMode = DeucarianThemeMode.Dark;
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(testRoot);
            DeucarianThemingEditorSettings.ActiveThemeGuid = previousThemeGuid;
            DeucarianThemingEditorSettings.ActiveThemeFamilyGuid = previousThemeFamilyGuid;
            DeucarianThemingEditorSettings.ActiveThemeMode = previousThemeMode;
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
            Assert.NotNull(assets.ThemeFamily);
            Assert.NotNull(assets.LightPalette);
            Assert.NotNull(assets.DarkPalette);
            Assert.NotNull(assets.LightTheme);
            Assert.NotNull(assets.DarkTheme);
            Assert.NotNull(assets.Palette);
            Assert.NotNull(assets.Theme);
            Assert.NotNull(assets.DefaultStyle);
            Assert.AreEqual(RequiredMinimalRoleIds.Length, assets.Roles.Count);
            Assert.AreEqual(RequiredStyleIds.Length, assets.Styles.Count);
            Assert.IsTrue(AssetDatabase.Contains(assets.RoleLibrary));
            Assert.IsTrue(AssetDatabase.Contains(assets.ThemeFamily));
            Assert.IsTrue(AssetDatabase.Contains(assets.Palette));
            Assert.IsTrue(AssetDatabase.Contains(assets.Theme));
            Assert.IsTrue(AssetDatabase.Contains(assets.DefaultStyle));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Defaults/Roles"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Defaults/Styles"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Defaults/Styles/Components/Surfaces"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Defaults/Styles/Components/Shapes"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Defaults/Styles/Components/Strokes"));
            AssertRequiredRolesExist(assets.RoleLibrary, RequiredMinimalRoleIds);
            AssertRequiredStylesExist(assets.Styles, RequiredStyleIds);
            Assert.IsTrue(assets.ThemeFamily.IsComplete);
            Assert.AreSame(assets.LightTheme, assets.ThemeFamily.LightTheme);
            Assert.AreSame(assets.DarkTheme, assets.ThemeFamily.DarkTheme);
            Assert.AreSame(assets.LightPalette, assets.LightTheme.ColorPalette);
            Assert.AreSame(assets.DarkPalette, assets.DarkTheme.ColorPalette);
            Assert.AreSame(assets.RoleLibrary, assets.LightPalette.RoleLibrary);
            Assert.AreSame(assets.RoleLibrary, assets.DarkPalette.RoleLibrary);
            Assert.IsTrue(assets.LightPalette.HasThemeMode);
            Assert.AreEqual(DeucarianThemeMode.Light, assets.LightPalette.ThemeMode);
            Assert.IsTrue(assets.DarkPalette.HasThemeMode);
            Assert.AreEqual(DeucarianThemeMode.Dark, assets.DarkPalette.ThemeMode);
            Assert.AreSame(assets.DarkPalette, assets.Palette);
            Assert.AreSame(assets.DarkTheme, assets.Theme);
            Assert.AreSame(assets.DefaultStyle, assets.LightTheme.VisualStyle);
            Assert.AreSame(assets.DefaultStyle, assets.Theme.VisualStyle);
            Assert.AreEqual(testRoot + "/Defaults/DefaultLightColorPalette.asset", AssetDatabase.GetAssetPath(assets.LightPalette));
            Assert.AreEqual(testRoot + "/Defaults/DefaultDarkColorPalette.asset", AssetDatabase.GetAssetPath(assets.DarkPalette));
            Assert.AreEqual(testRoot + "/Defaults/DefaultLightTheme.asset", AssetDatabase.GetAssetPath(assets.LightTheme));
            Assert.AreEqual(testRoot + "/Defaults/DefaultTheme.asset", AssetDatabase.GetAssetPath(assets.DarkTheme));
            Assert.AreEqual(testRoot + "/Defaults/DefaultThemeFamily.asset", AssetDatabase.GetAssetPath(assets.ThemeFamily));
            Assert.AreEqual("deucarian.palette.default.light", assets.LightPalette.PaletteId);
            Assert.AreEqual("deucarian.palette.default", assets.DarkPalette.PaletteId);
            Assert.AreEqual("deucarian.theme.default.light", assets.LightTheme.ThemeId);
            Assert.AreEqual("deucarian.theme.default", assets.DarkTheme.ThemeId);
            Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                DeucarianBuiltinColorRoleIds.Core.Primary,
                out DeucarianColorRole pairedPrimaryRole));
            Assert.IsTrue(pairedPrimaryRole.HasPairedDefaultColors);
            AssertColor("#174367", pairedPrimaryRole.GetDefaultColor(DeucarianThemeMode.Light));
            AssertColor("#87BDD7", pairedPrimaryRole.GetDefaultColor(DeucarianThemeMode.Dark));
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
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Styles/Components/Surfaces"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Styles/Components/Shapes"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Styles/Components/Strokes"));
        }

        [Test]
        public void CreateThemeFamilyCreatesPairedVariantsWithSharedRolesAndStyle()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/SimultriaThemeFamily.asset");

            Assert.NotNull(assets.ThemeFamily);
            Assert.IsTrue(assets.ThemeFamily.IsComplete);
            Assert.AreSame(assets.LightTheme, assets.ThemeFamily.LightTheme);
            Assert.AreSame(assets.DarkTheme, assets.ThemeFamily.DarkTheme);
            Assert.AreNotSame(assets.LightPalette, assets.DarkPalette);
            Assert.AreSame(assets.RoleLibrary, assets.LightPalette.RoleLibrary);
            Assert.AreSame(assets.RoleLibrary, assets.DarkPalette.RoleLibrary);
            Assert.AreEqual(DeucarianThemeMode.Light, assets.LightPalette.ThemeMode);
            Assert.AreEqual(DeucarianThemeMode.Dark, assets.DarkPalette.ThemeMode);
            Assert.AreSame(assets.DefaultStyle, assets.LightTheme.VisualStyle);
            Assert.AreSame(assets.DefaultStyle, assets.DarkTheme.VisualStyle);
            Assert.AreEqual("deucarian.theme-family.simultria", assets.ThemeFamily.FamilyId);
            Assert.AreEqual("Simultria Theme", assets.ThemeFamily.DisplayName);
            Assert.AreEqual(RequiredMinimalRoleIds.Length, assets.LightPalette.Entries.Count);
            Assert.AreEqual(RequiredMinimalRoleIds.Length, assets.DarkPalette.Entries.Count);
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Simultria Theme Support/Roles"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/Simultria Theme Support/Styles"));
        }

        [Test]
        public void RepairThemeFamilyPreservesEditedVariantColorsAndStableAssets()
        {
            string familyPath = testRoot + "/CustomThemeFamily.asset";
            DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.CreateThemeFamily(familyPath);
            Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                DeucarianBuiltinColorRoleIds.Core.Primary,
                out DeucarianColorRole primaryRole));

            Color userLightColor = new Color(0.12f, 0.34f, 0.56f, 1f);
            assets.LightPalette.SetColor(primaryRole, userLightColor, "User light override");
            EditorUtility.SetDirty(assets.LightPalette);
            AssetDatabase.SaveAssets();

            string familyGuid = AssetDatabase.AssetPathToGUID(familyPath);
            string lightThemeGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(assets.LightTheme));
            DeucarianDefaultThemeAssets repaired =
                DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(assets.ThemeFamily);

            Assert.AreSame(assets.ThemeFamily, repaired.ThemeFamily);
            Assert.AreSame(assets.LightTheme, repaired.LightTheme);
            Assert.AreEqual(familyGuid, AssetDatabase.AssetPathToGUID(familyPath));
            Assert.AreEqual(lightThemeGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(repaired.LightTheme)));
            Assert.IsTrue(repaired.LightPalette.TryGetColor(primaryRole, out Color repairedColor));
            Assert.IsTrue(ColorsMatch(userLightColor, repairedColor), repairedColor.ToString());
        }

        [Test]
        public void RepairThemeFamilyPreservesLegacyRoleMetadataAndDarkDefault()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/LegacyRoleThemeFamily.asset");
            Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                DeucarianBuiltinColorRoleIds.Core.Primary,
                out DeucarianColorRole primaryRole));
            Color legacyDark = new Color(0.19f, 0.29f, 0.39f, 1f);
            primaryRole.Configure(
                primaryRole.Id,
                "Project Primary",
                "Project Semantic",
                "Project-authored legacy role.",
                legacyDark,
                false);
            EditorUtility.SetDirty(primaryRole);
            AssetDatabase.SaveAssets();

            DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(assets.ThemeFamily);

            Assert.AreEqual("Project Primary", primaryRole.DisplayName);
            Assert.AreEqual("Project Semantic", primaryRole.Category);
            Assert.AreEqual("Project-authored legacy role.", primaryRole.Description);
            Assert.IsFalse(primaryRole.IsCoreRole);
            Assert.IsTrue(primaryRole.HasPairedDefaultColors);
            AssertColor("#174367", primaryRole.LightDefaultColor);
            Assert.IsTrue(ColorsMatch(legacyDark, primaryRole.DarkDefaultColor));
        }

        [Test]
        public void RepairThemeFamilyFillsEachMissingRoleDefaultIndependently()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/MissingRoleDefaultsThemeFamily.asset");
            Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                DeucarianBuiltinColorRoleIds.Core.Surface,
                out DeucarianColorRole surfaceRole));
            Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                DeucarianBuiltinColorRoleIds.Core.Primary,
                out DeucarianColorRole primaryRole));
            Color preservedSurfaceDark = new Color(0.21f, 0.31f, 0.41f, 1f);
            Color preservedPrimaryLight = new Color(0.51f, 0.61f, 0.71f, 1f);
            surfaceRole.Configure(
                surfaceRole.Id,
                surfaceRole.DisplayName,
                surfaceRole.Category,
                surfaceRole.Description,
                DeucarianColorPalette.MissingColor,
                preservedSurfaceDark,
                surfaceRole.IsCoreRole);
            primaryRole.Configure(
                primaryRole.Id,
                primaryRole.DisplayName,
                primaryRole.Category,
                primaryRole.Description,
                preservedPrimaryLight,
                DeucarianColorPalette.MissingColor,
                primaryRole.IsCoreRole);
            EditorUtility.SetDirty(surfaceRole);
            EditorUtility.SetDirty(primaryRole);
            AssetDatabase.SaveAssets();

            DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(assets.ThemeFamily);

            AssertColor("#FFFFFF", surfaceRole.LightDefaultColor);
            Assert.IsTrue(ColorsMatch(preservedSurfaceDark, surfaceRole.DarkDefaultColor));
            Assert.IsTrue(ColorsMatch(preservedPrimaryLight, primaryRole.LightDefaultColor));
            AssertColor("#87BDD7", primaryRole.DarkDefaultColor);
        }

        [Test]
        public void RepairThemeFamilyAddsCustomSharedLibraryRolesToBothVariants()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/CustomRolesThemeFamily.asset");
            DeucarianColorRole customRole = CreateAsset<DeucarianColorRole>(testRoot + "/CustomRole.asset");
            Color lightDefault = new Color(0.1f, 0.2f, 0.3f, 1f);
            Color darkDefault = new Color(0.7f, 0.8f, 0.9f, 1f);
            customRole.Configure(
                "deucarian.test.custom-family-role",
                "Custom Family Role",
                "Tests",
                "Custom shared role.",
                lightDefault,
                darkDefault,
                false);
            assets.RoleLibrary.AddRole(customRole);
            EditorUtility.SetDirty(customRole);
            EditorUtility.SetDirty(assets.RoleLibrary);
            AssetDatabase.SaveAssets();

            DeucarianDefaultThemeAssets repaired =
                DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(assets.ThemeFamily);

            Assert.IsTrue(TryGetExplicitPaletteColor(repaired.LightPalette, customRole, out Color lightColor));
            Assert.IsTrue(TryGetExplicitPaletteColor(repaired.DarkPalette, customRole, out Color darkColor));
            Assert.IsTrue(ColorsMatch(lightDefault, lightColor));
            Assert.IsTrue(ColorsMatch(darkDefault, darkColor));
        }

        [Test]
        public void WrapExistingThemeRequiresExplicitSlotAndDoesNotGuessOtherVariant()
        {
            DeucarianDefaultThemeAssets standalone =
                DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(testRoot + "/LegacyPalette.asset");

            DeucarianDefaultThemeAssets wrapped = DeucarianDefaultThemeAssetFactory.WrapExistingThemeInFamily(
                standalone.Theme,
                DeucarianThemeMode.Light,
                testRoot + "/LegacyThemeFamily.asset");

            Assert.NotNull(wrapped.ThemeFamily);
            Assert.AreSame(standalone.Theme, wrapped.ThemeFamily.LightTheme);
            Assert.IsNull(wrapped.ThemeFamily.DarkTheme);
            Assert.IsFalse(wrapped.ThemeFamily.IsComplete);
            Assert.AreSame(standalone.Theme, wrapped.ThemeFamily.ResolveTheme(DeucarianThemeMode.Dark));
        }

        [Test]
        public void RepairWrappedThemeFamilyCreatesMissingCounterpartWithoutReplacingExistingTheme()
        {
            DeucarianDefaultThemeAssets standalone =
                DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(testRoot + "/RepairLegacyPalette.asset");
            DeucarianDefaultThemeAssets wrapped = DeucarianDefaultThemeAssetFactory.WrapExistingThemeInFamily(
                standalone.Theme,
                DeucarianThemeMode.Light,
                testRoot + "/RepairLegacyThemeFamily.asset");

            DeucarianDefaultThemeAssets repaired =
                DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(wrapped.ThemeFamily);

            Assert.IsTrue(repaired.ThemeFamily.IsComplete);
            Assert.AreSame(standalone.Theme, repaired.LightTheme);
            Assert.NotNull(repaired.DarkTheme);
            Assert.AreNotSame(repaired.LightTheme, repaired.DarkTheme);
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

            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Core.Background, "#07111F");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Core.Surface, "#0B1A2C");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Core.SurfaceRaised, "#102B46");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Core.Primary, "#87BDD7");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Core.Secondary, "#64C1B6");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Core.Accent, "#F0BC69");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Text.Primary, "#F6F8FA");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Text.Secondary, "#EDF1F4");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Text.Muted, "#D7DEE5");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Text.Disabled, "#52606D");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Status.Success, "#64C1B6");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Status.Warning, "#F0BC69");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Status.Error, "#FFB4AB");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.Status.Info, "#87BDD7");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.UI.Normal, "#0B1A2C");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.UI.Highlighted, "#102B46");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.UI.Pressed, "#87BDD7");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.UI.Selected, "#64C1B6");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.UI.Disabled, "#174367");
            AssertPaletteColor(assets.DarkPalette, DeucarianBuiltinColorRoleIds.UI.Focused, "#87BDD7");

            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Core.Background, "#F6F8FA");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Core.Surface, "#FFFFFF");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Core.SurfaceRaised, "#FFFFFF");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Core.Primary, "#174367");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Core.Secondary, "#0B6B68");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Core.Accent, "#9A5A0A");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Text.Primary, "#0B1117");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Text.Secondary, "#263442");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Text.Muted, "#52606D");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Text.Disabled, "#8997A5");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Status.Success, "#0B6B68");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Status.Warning, "#9A5A0A");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Status.Error, "#9F241A");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.Status.Info, "#1E5F8D");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.UI.Normal, "#FFFFFF");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.UI.Highlighted, "#FFFFFF");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.UI.Pressed, "#174367");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.UI.Selected, "#0B6B68");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.UI.Disabled, "#D7DEE5");
            AssertPaletteColor(assets.LightPalette, DeucarianBuiltinColorRoleIds.UI.Focused, "#1E5F8D");
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

            Assert.AreEqual(assets.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, DeucarianThemingEditorSettings.ActiveThemeMode);
            Assert.AreEqual(assets.DarkTheme, DeucarianThemingEditorSettings.ActiveTheme);
            Assert.AreEqual(assets.DarkPalette, DeucarianThemingEditorSettings.ActivePalette);
            Assert.AreEqual(assets.RoleLibrary, DeucarianThemingEditorSettings.ActiveRoleLibrary);
            Assert.AreEqual(assets.DefaultStyle, DeucarianThemingEditorSettings.ActiveStyle);
        }

        [Test]
        public void ThemePackCreationCreatesPackAssetsStylesAndRuntimeSettings()
        {
            DeucarianThemePack themePack = CreateReportViewerThemePack();

            try
            {
                DeucarianDefaultThemeAssets assets =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, testRoot + "/ReportViewer");
                DeucarianThemeRuntimeSettings settings =
                    DeucarianThemePackAssetFactory.CreateOrRepairRuntimeSettings(
                        testRoot + "/ReportViewer/Resources",
                        assets.Theme);

                Assert.NotNull(assets.RoleLibrary);
                Assert.NotNull(assets.Palette);
                Assert.NotNull(assets.Theme);
                Assert.NotNull(assets.DefaultStyle);
                Assert.AreEqual(2, assets.Roles.Count);
                Assert.AreEqual(RequiredStyleIds.Length, assets.Styles.Count);
                Assert.AreSame(assets.Palette, assets.Theme.ColorPalette);
                Assert.AreSame(assets.DefaultStyle, assets.Theme.VisualStyle);
                Assert.AreSame(assets.Theme, settings.DefaultTheme);
                Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/ReportViewer/Roles"));
                Assert.IsTrue(AssetDatabase.IsValidFolder(testRoot + "/ReportViewer/Styles"));
                AssertRequiredRolesExist(
                    assets.RoleLibrary,
                    new[]
                    {
                        DeucarianBuiltinColorRoleIds.Core.Surface,
                        "reportviewer.navigation.active"
                    });
            }
            finally
            {
                Object.DestroyImmediate(themePack);
            }
        }

        [Test]
        public void PairedThemePackCreatesFamilyVariantsAndFamilyRuntimeSettings()
        {
            DeucarianThemePack themePack = CreatePairedReportViewerThemePack();

            try
            {
                DeucarianDefaultThemeAssets assets =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, testRoot + "/PairedReportViewer");
                DeucarianThemeRuntimeSettings settings =
                    DeucarianThemePackAssetFactory.CreateOrRepairRuntimeSettings(
                        testRoot + "/PairedReportViewer/Resources",
                        assets.ThemeFamily,
                        DeucarianThemeMode.Dark);

                Assert.NotNull(assets.ThemeFamily);
                Assert.IsTrue(assets.ThemeFamily.IsComplete);
                Assert.AreSame(assets.LightTheme, assets.ThemeFamily.LightTheme);
                Assert.AreSame(assets.DarkTheme, assets.ThemeFamily.DarkTheme);
                Assert.AreSame(assets.RoleLibrary, assets.LightPalette.RoleLibrary);
                Assert.AreSame(assets.RoleLibrary, assets.DarkPalette.RoleLibrary);
                Assert.IsTrue(assets.LightPalette.HasThemeMode);
                Assert.AreEqual(DeucarianThemeMode.Light, assets.LightPalette.ThemeMode);
                Assert.IsTrue(assets.DarkPalette.HasThemeMode);
                Assert.AreEqual(DeucarianThemeMode.Dark, assets.DarkPalette.ThemeMode);
                Assert.AreSame(assets.DefaultStyle, assets.LightTheme.VisualStyle);
                Assert.AreSame(assets.DefaultStyle, assets.DarkTheme.VisualStyle);
                Assert.AreSame(assets.ThemeFamily, settings.DefaultThemeFamily);
                Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);
                Assert.AreSame(assets.DarkTheme, settings.DefaultTheme);
                AssertPaletteColor(assets.LightPalette, "reportviewer.navigation.active", "#174367");
                AssertPaletteColor(assets.DarkPalette, "reportviewer.navigation.active", "#87BDD7");
                Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                    "reportviewer.navigation.active",
                    out DeucarianColorRole activeRole));
                Assert.IsTrue(activeRole.HasPairedDefaultColors);
            }
            finally
            {
                Object.DestroyImmediate(themePack);
            }
        }

        [Test]
        public void PairedThemePackRepairPreservesEditedLightAndDarkColors()
        {
            DeucarianThemePack themePack = CreatePairedReportViewerThemePack();

            try
            {
                DeucarianDefaultThemeAssets assets =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, testRoot + "/PairedRepair");
                Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                    "reportviewer.navigation.active",
                    out DeucarianColorRole activeRole));
                Color lightOverride = new Color(0.2f, 0.3f, 0.4f, 1f);
                Color darkOverride = new Color(0.7f, 0.6f, 0.5f, 1f);
                assets.LightPalette.SetColor(activeRole, lightOverride, "Light override");
                assets.DarkPalette.SetColor(activeRole, darkOverride, "Dark override");
                EditorUtility.SetDirty(assets.LightPalette);
                EditorUtility.SetDirty(assets.DarkPalette);
                AssetDatabase.SaveAssets();

                DeucarianDefaultThemeAssets repaired =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, testRoot + "/PairedRepair");

                Assert.IsTrue(repaired.LightPalette.TryGetColor(activeRole, out Color repairedLight));
                Assert.IsTrue(repaired.DarkPalette.TryGetColor(activeRole, out Color repairedDark));
                Assert.IsTrue(ColorsMatch(lightOverride, repairedLight));
                Assert.IsTrue(ColorsMatch(darkOverride, repairedDark));
            }
            finally
            {
                Object.DestroyImmediate(themePack);
            }
        }

        [Test]
        public void PairedThemePackRepairPreservesMovedReferencedVariantsAndLibrary()
        {
            DeucarianThemePack themePack = CreatePairedReportViewerThemePack();

            try
            {
                string root = testRoot + "/MovedPairedPack";
                DeucarianDefaultThemeAssets assets =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, root);
                string customFolder = root + "/Project Authored";
                DeucarianThemingMenuActions.EnsureAssetFolder(customFolder);
                AssertMoveAsset(assets.LightTheme, customFolder + "/ProjectLightTheme.asset");
                AssertMoveAsset(assets.DarkTheme, customFolder + "/ProjectDarkTheme.asset");
                AssertMoveAsset(assets.LightPalette, customFolder + "/ProjectLightPalette.asset");
                AssertMoveAsset(assets.DarkPalette, customFolder + "/ProjectDarkPalette.asset");
                AssertMoveAsset(assets.RoleLibrary, customFolder + "/ProjectRoleLibrary.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                DeucarianDefaultThemeAssets repaired =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, root);

                Assert.AreSame(assets.LightTheme, repaired.LightTheme);
                Assert.AreSame(assets.DarkTheme, repaired.DarkTheme);
                Assert.AreSame(assets.LightPalette, repaired.LightPalette);
                Assert.AreSame(assets.DarkPalette, repaired.DarkPalette);
                Assert.AreSame(assets.RoleLibrary, repaired.RoleLibrary);
                Assert.AreSame(repaired.LightTheme, repaired.ThemeFamily.LightTheme);
                Assert.AreSame(repaired.DarkTheme, repaired.ThemeFamily.DarkTheme);
                Assert.AreSame(repaired.LightPalette, repaired.LightTheme.ColorPalette);
                Assert.AreSame(repaired.DarkPalette, repaired.DarkTheme.ColorPalette);
                Assert.AreSame(repaired.RoleLibrary, repaired.LightPalette.RoleLibrary);
                Assert.AreSame(repaired.RoleLibrary, repaired.DarkPalette.RoleLibrary);
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<DeucarianTheme>(root + "/SimultriaLightTheme.asset"));
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<DeucarianTheme>(root + "/SimultriaDarkTheme.asset"));
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<DeucarianColorPalette>(root + "/SimultriaLightPalette.asset"));
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<DeucarianColorPalette>(root + "/SimultriaDarkPalette.asset"));
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<DeucarianColorRoleLibrary>(
                    root + "/SimultriaColorRoleLibrary.asset"));
            }
            finally
            {
                Object.DestroyImmediate(themePack);
            }
        }

        [Test]
        public void PairedThemePackRepairFillsEachMissingRoleDefaultIndependently()
        {
            DeucarianThemePack themePack = CreatePairedReportViewerThemePack();

            try
            {
                string root = testRoot + "/PairedRoleRepair";
                DeucarianDefaultThemeAssets assets =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, root);
                Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                    DeucarianBuiltinColorRoleIds.Core.Surface,
                    out DeucarianColorRole surfaceRole));
                Assert.IsTrue(assets.RoleLibrary.TryGetRoleById(
                    "reportviewer.navigation.active",
                    out DeucarianColorRole navigationRole));
                Color preservedSurfaceDark = new Color(0.2f, 0.25f, 0.3f, 1f);
                Color preservedNavigationLight = new Color(0.4f, 0.45f, 0.5f, 1f);
                surfaceRole.Configure(
                    surfaceRole.Id,
                    surfaceRole.DisplayName,
                    surfaceRole.Category,
                    surfaceRole.Description,
                    DeucarianColorPalette.MissingColor,
                    preservedSurfaceDark,
                    surfaceRole.IsCoreRole);
                navigationRole.Configure(
                    navigationRole.Id,
                    navigationRole.DisplayName,
                    navigationRole.Category,
                    navigationRole.Description,
                    preservedNavigationLight,
                    DeucarianColorPalette.MissingColor,
                    navigationRole.IsCoreRole);
                EditorUtility.SetDirty(surfaceRole);
                EditorUtility.SetDirty(navigationRole);
                AssetDatabase.SaveAssets();

                DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, root);

                AssertColor("#FFFFFF", surfaceRole.LightDefaultColor);
                Assert.IsTrue(ColorsMatch(preservedSurfaceDark, surfaceRole.DarkDefaultColor));
                Assert.IsTrue(ColorsMatch(preservedNavigationLight, navigationRole.LightDefaultColor));
                AssertColor("#87BDD7", navigationRole.DarkDefaultColor);
            }
            finally
            {
                Object.DestroyImmediate(themePack);
            }
        }

        [Test]
        public void RuntimeSettingsFactoryTransitionsBetweenLegacyThemeAndFamily()
        {
            DeucarianDefaultThemeAssets familyAssets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/SettingsThemeFamily.asset");
            DeucarianDefaultThemeAssets legacyAssets =
                DeucarianDefaultThemeAssetFactory.CreateMinimalPalette(testRoot + "/SettingsLegacyPalette.asset");
            string resourcesFolder = testRoot + "/Settings/Resources";

            DeucarianThemeRuntimeSettings settings = DeucarianThemePackAssetFactory.CreateOrRepairRuntimeSettings(
                resourcesFolder,
                legacyAssets.Theme);
            Assert.AreSame(legacyAssets.Theme, settings.LegacyDefaultTheme);
            Assert.IsNull(settings.DefaultThemeFamily);

            settings = DeucarianThemePackAssetFactory.CreateOrRepairRuntimeSettings(
                resourcesFolder,
                familyAssets.ThemeFamily,
                DeucarianThemeMode.Light);
            Assert.IsNull(settings.LegacyDefaultTheme);
            Assert.AreSame(familyAssets.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreSame(familyAssets.LightTheme, settings.DefaultTheme);

            settings = DeucarianThemePackAssetFactory.CreateOrRepairRuntimeSettings(
                resourcesFolder,
                legacyAssets.Theme);
            Assert.AreSame(legacyAssets.Theme, settings.LegacyDefaultTheme);
            Assert.IsNull(settings.DefaultThemeFamily);
            Assert.AreSame(legacyAssets.Theme, settings.DefaultTheme);
        }

        [Test]
        public void ThemePackRepairDoesNotOverwriteUserColors()
        {
            DeucarianThemePack themePack = CreateReportViewerThemePack();

            try
            {
                DeucarianDefaultThemeAssets assets =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, testRoot + "/ReportViewer");
                Assert.True(assets.RoleLibrary.TryGetRoleById(
                    "reportviewer.navigation.active",
                    out DeucarianColorRole activeRole));

                Color userColor = new Color(0.2f, 0.4f, 0.6f, 1f);
                assets.Palette.SetColor(activeRole, userColor, "User override");
                EditorUtility.SetDirty(assets.Palette);
                AssetDatabase.SaveAssets();

                DeucarianDefaultThemeAssets repaired =
                    DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(themePack, testRoot + "/ReportViewer");

                Assert.True(repaired.Palette.TryGetColor(activeRole, out Color repairedColor));
                Assert.True(ColorsMatch(userColor, repairedColor), repairedColor.ToString());
            }
            finally
            {
                Object.DestroyImmediate(themePack);
            }
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
                Assert.AreNotEqual(
                    0,
                    (int)(windows[i].hideFlags & HideFlags.DontSave));
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

        private static DeucarianThemePack CreateReportViewerThemePack()
        {
            DeucarianThemePack themePack = ScriptableObject.CreateInstance<DeucarianThemePack>();
            themePack.Configure(
                "simultria.reportviewer.theme-pack",
                "Simultria Report Viewer",
                "SimultriaColorRoleLibrary.asset",
                "SimultriaPalette.asset",
                "SimultriaTheme.asset",
                "simultria.reportviewer.palette",
                "Simultria Palette",
                "simultria.reportviewer.theme",
                "Simultria Theme",
                DeucarianThemeStyleIds.FrostedGlass,
                new[]
                {
                    new DeucarianThemePackRole(
                        "DeucarianSurface",
                        DeucarianBuiltinColorRoleIds.Core.Surface,
                        "Surface",
                        DeucarianColorRoleCategories.Semantic,
                        "Base color for viewer panels.",
                        new Color(0.11f, 0.14f, 0.18f, 0.88f),
                        true),
                    new DeucarianThemePackRole(
                        "ReportViewerNavigationActive",
                        "reportviewer.navigation.active",
                        "Report Viewer Navigation Active",
                        DeucarianColorRoleCategories.UiState,
                        "Active navigation control color.",
                        new Color(99f / 255f, 66f / 255f, 150f / 255f, 1f),
                        false)
                });
            return themePack;
        }

        private static DeucarianThemePack CreatePairedReportViewerThemePack()
        {
            DeucarianThemePack themePack = ScriptableObject.CreateInstance<DeucarianThemePack>();
            themePack.Configure(
                "simultria.reportviewer.paired-theme-pack",
                "Simultria Report Viewer Paired",
                "SimultriaColorRoleLibrary.asset",
                "SimultriaThemeFamily.asset",
                "SimultriaLightPalette.asset",
                "SimultriaDarkPalette.asset",
                "SimultriaLightTheme.asset",
                "SimultriaDarkTheme.asset",
                "simultria.reportviewer.theme-family",
                "Simultria Report Viewer",
                "simultria.reportviewer.palette.light",
                "Simultria Light Palette",
                "simultria.reportviewer.palette.dark",
                "Simultria Dark Palette",
                "simultria.reportviewer.theme.light",
                "Simultria Light",
                "simultria.reportviewer.theme.dark",
                "Simultria Dark",
                DeucarianThemeStyleIds.FrostedGlass,
                new[]
                {
                    new DeucarianThemePackRole(
                        "DeucarianSurface",
                        DeucarianBuiltinColorRoleIds.Core.Surface,
                        "Surface",
                        DeucarianColorRoleCategories.Semantic,
                        "Base color for viewer panels.",
                        new Color32(0xFF, 0xFF, 0xFF, 0xFF),
                        new Color32(0x0B, 0x1A, 0x2C, 0xFF),
                        true),
                    new DeucarianThemePackRole(
                        "ReportViewerNavigationActive",
                        "reportviewer.navigation.active",
                        "Report Viewer Navigation Active",
                        DeucarianColorRoleCategories.UiState,
                        "Active navigation control color.",
                        new Color32(0x17, 0x43, 0x67, 0xFF),
                        new Color32(0x87, 0xBD, 0xD7, 0xFF),
                        false)
                });
            return themePack;
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
            Assert.IsTrue(style.IsComposed, styleId);
            Assert.NotNull(style.SurfaceProfile, styleId);
            Assert.NotNull(style.ShapeProfile, styleId);
            Assert.NotNull(style.StrokeProfile, styleId);

            if (styleId == DeucarianThemeStyleIds.FrostedGlass)
            {
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Surface.FrostedGlass, style.SurfaceProfile.ProfileId);
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Shape.Rounded, style.ShapeProfile.ProfileId);
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Stroke.Frosted, style.StrokeProfile.ProfileId);
                Assert.AreEqual(DeucarianThemeDensity.Comfortable, style.Density);
                Assert.AreEqual(16f, style.CornerRadius);
            }
            else if (styleId == DeucarianThemeStyleIds.FluentAcrylic)
            {
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Surface.FluentAcrylic, style.SurfaceProfile.ProfileId);
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Shape.Soft, style.ShapeProfile.ProfileId);
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Stroke.Acrylic, style.StrokeProfile.ProfileId);
                Assert.AreEqual(DeucarianThemeDensity.Standard, style.Density);
                Assert.AreEqual(8f, style.CornerRadius);
            }
            else if (styleId == DeucarianThemeStyleIds.MaterialDark)
            {
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Surface.Material, style.SurfaceProfile.ProfileId);
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Shape.Tight, style.ShapeProfile.ProfileId);
                Assert.AreEqual(DeucarianThemePresentationProfileIds.Stroke.Material, style.StrokeProfile.ProfileId);
                Assert.AreEqual(DeucarianThemeDensity.Compact, style.Density);
                Assert.AreEqual(4f, style.CornerRadius);
            }
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

        private static bool TryGetExplicitPaletteColor(
            DeucarianColorPalette palette,
            DeucarianColorRole role,
            out Color color)
        {
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry != null && entry.Role == role)
                {
                    color = entry.Color;
                    return true;
                }
            }

            color = default(Color);
            return false;
        }

        private static void AssertObjectNameMatchesFile(Object asset)
        {
            Assert.NotNull(asset);
            string path = AssetDatabase.GetAssetPath(asset);
            Assert.IsNotEmpty(path);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(path), asset.name);
        }

        private static void AssertMoveAsset(Object asset, string destinationPath)
        {
            Assert.NotNull(asset);
            string error = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(asset), destinationPath);
            Assert.IsTrue(string.IsNullOrEmpty(error), error);
        }
    }
}
