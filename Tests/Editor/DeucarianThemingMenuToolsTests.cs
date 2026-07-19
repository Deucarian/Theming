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
            DeucarianThemingEditorSettings.ClearActiveAssets();
            DeucarianThemingEditorSettings.ActiveThemeMode = DeucarianThemeMode.Dark;
            DeucarianThemingEditorSettings.DefaultAssetFolder = testRoot + "/Defaults";
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
        public void TopMenuContainsOnlyQuickEntryPoints()
        {
            string[] expectedMenuItems =
            {
                "Tools/Deucarian/Experience and Interaction/UI and Presentation/Theming/Open Theme Manager",
                "Tools/Deucarian/Experience and Interaction/UI and Presentation/Theming/Create Theme Family"
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
        public void SelectAndPingCompatibilityEntryPoint_SelectsTarget()
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/SelectedTheme.asset");
            Object previousSelection = Selection.activeObject;

            try
            {
                Selection.activeObject = null;

                DeucarianThemingMenuActions.SelectAndPing(theme);

                Assert.AreSame(theme, Selection.activeObject);
            }
            finally
            {
                Selection.activeObject = previousSelection;
            }
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
        public void SettingsStoreAndResolveActiveThemeFamilyAndPreviewMode()
        {
            DeucarianThemeFamily family = CreateAsset<DeucarianThemeFamily>(testRoot + "/ThemeFamily.asset");

            DeucarianThemingEditorSettings.ActiveThemeFamily = family;
            DeucarianThemingEditorSettings.ActiveThemeMode = DeucarianThemeMode.Light;

            Assert.IsNotEmpty(DeucarianThemingEditorSettings.ActiveThemeFamilyGuid);
            Assert.AreSame(family, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, DeucarianThemingEditorSettings.ActiveThemeMode);
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
        public void ProjectDefaultHydratesEveryActiveFamilyAssetWhenLocalPreviewIsEmpty()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/ProjectDefaultFamily.asset");
            DeucarianThemeRuntimeSettings settings = CreateAsset<DeucarianThemeRuntimeSettings>(
                testRoot + "/RuntimeSettings.asset");
            settings.Configure(assets.ThemeFamily, DeucarianThemeMode.Light);
            DeucarianThemingEditorSettings.ClearActiveAssets();

            Assert.IsTrue(DeucarianThemingMenuActions.TryHydrateActiveAssetsFromProjectDefault(settings));
            Assert.AreSame(assets.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, DeucarianThemingEditorSettings.ActiveThemeMode);
            Assert.AreSame(assets.LightTheme, DeucarianThemingEditorSettings.ActiveTheme);
            Assert.AreSame(assets.LightPalette, DeucarianThemingEditorSettings.ActivePalette);
            Assert.AreSame(assets.RoleLibrary, DeucarianThemingEditorSettings.ActiveRoleLibrary);
            Assert.AreSame(assets.LightTheme.VisualStyle, DeucarianThemingEditorSettings.ActiveStyle);
        }

        [Test]
        public void ProjectDefaultHydrationPreservesValidLocalFamilyPreview()
        {
            DeucarianDefaultThemeAssets projectAssets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/ProjectFamily.asset");
            DeucarianDefaultThemeAssets localAssets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/LocalFamily.asset");
            DeucarianThemeRuntimeSettings settings = CreateAsset<DeucarianThemeRuntimeSettings>(
                testRoot + "/RuntimeSettings.asset");
            settings.Configure(projectAssets.ThemeFamily, DeucarianThemeMode.Light);
            DeucarianThemingMenuActions.SetActiveThemeFamilyAndApply(localAssets.ThemeFamily);

            Assert.IsFalse(DeucarianThemingMenuActions.TryHydrateActiveAssetsFromProjectDefault(settings));
            Assert.AreSame(localAssets.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreSame(localAssets.DarkTheme, DeucarianThemingEditorSettings.ActiveTheme);
        }

        [Test]
        public void SetActiveAsProjectDefaultChangesRuntimeSettingsOnlyWhenInvoked()
        {
            DeucarianDefaultThemeAssets originalAssets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/OriginalFamily.asset");
            DeucarianDefaultThemeAssets activeAssets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/ActiveFamily.asset");
            DeucarianThemeRuntimeSettings settings = CreateAsset<DeucarianThemeRuntimeSettings>(
                testRoot + "/RuntimeSettings.asset");
            settings.Configure(originalAssets.ThemeFamily, DeucarianThemeMode.Dark);
            DeucarianThemingMenuActions.SetActiveThemeFamilyAndApply(activeAssets.ThemeFamily);
            DeucarianThemingMenuActions.SetActiveThemeModeAndApply(DeucarianThemeMode.Light);

            Assert.AreSame(originalAssets.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);
            Assert.IsTrue(DeucarianThemingMenuActions.SetActiveThemeFamilyAsProjectDefault(settings));
            Assert.AreSame(activeAssets.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, settings.DefaultThemeMode);
        }

        [Test]
        public void MissingProjectRuntimeSettingsDoesNotCreateAssets()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/ActiveFamily.asset");
            DeucarianThemingMenuActions.SetActiveThemeFamilyAndApply(assets.ThemeFamily);
            int assetCountBefore = AssetDatabase.FindAssets(string.Empty, new[] { testRoot }).Length;

            Assert.IsFalse(DeucarianThemingMenuActions.SetActiveThemeFamilyAsProjectDefault(null));
            Assert.AreEqual(
                assetCountBefore,
                AssetDatabase.FindAssets(string.Empty, new[] { testRoot }).Length);
        }

        [Test]
        public void FindExistingAssetsReturnsFamiliesThemesPalettesAndRoleLibraries()
        {
            DeucarianThemeFamily family = CreateAsset<DeucarianThemeFamily>(testRoot + "/ThemeFamily.asset");
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>(testRoot + "/Palette.asset");
            DeucarianColorRoleLibrary roleLibrary = CreateAsset<DeucarianColorRoleLibrary>(testRoot + "/Role Library.asset");
            DeucarianThemeStyle style = CreateAsset<DeucarianThemeStyle>(testRoot + "/Style.asset");

            DeucarianThemingMenuActions.AssetSearchResult result =
                DeucarianThemingMenuActions.FindExistingAssets(new[] { testRoot });

            CollectionAssert.Contains(result.ThemeFamilies, family);
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
            Assert.NotNull(assets.ThemeFamily);
            Assert.NotNull(assets.LightTheme);
            Assert.NotNull(assets.DarkTheme);
            Assert.NotNull(assets.Palette);
            Assert.NotNull(assets.RoleLibrary);
            Assert.NotNull(assets.DefaultStyle);
            Assert.IsTrue(AssetDatabase.Contains(assets.Theme));
            Assert.IsTrue(AssetDatabase.Contains(assets.ThemeFamily));
            Assert.IsTrue(AssetDatabase.Contains(assets.Palette));
            Assert.IsTrue(AssetDatabase.Contains(assets.RoleLibrary));
            Assert.IsTrue(AssetDatabase.Contains(assets.DefaultStyle));
            Assert.AreSame(assets.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreSame(assets.DarkTheme, DeucarianThemingEditorSettings.ActiveTheme);
            Assert.AreSame(assets.DarkPalette, DeucarianThemingEditorSettings.ActivePalette);
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
        public void AssignActiveStyleToThemeFamilyUpdatesBothVariants()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/StyledThemeFamily.asset");
            DeucarianThemeStyle style = CreateAsset<DeucarianThemeStyle>(testRoot + "/SharedStyle.asset");
            DeucarianThemingEditorSettings.ActiveThemeFamily = assets.ThemeFamily;
            DeucarianThemingEditorSettings.ActiveStyle = style;
            GameObject providerObject = new GameObject("Theme Family Style Provider Test");
            GameObject targetObject = new GameObject("Theme Family Style Target Test");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            StyleTargetProbe probe = targetObject.AddComponent<StyleTargetProbe>();

            try
            {
                provider.SetThemeFamily(assets.ThemeFamily, DeucarianThemeMode.Dark);
                int baseline = probe.ApplyCount;

                Assert.IsTrue(DeucarianThemingMenuActions.AssignActiveStyleToActiveThemeFamily());

                Assert.AreSame(style, assets.LightTheme.VisualStyle);
                Assert.AreSame(style, assets.DarkTheme.VisualStyle);
                Assert.AreEqual(baseline + 1, probe.ApplyCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void AssignActiveStyleToActiveThemeRefreshesMatchingOpenSceneProviders()
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            DeucarianThemeStyle style = CreateAsset<DeucarianThemeStyle>(testRoot + "/Style.asset");
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            DeucarianThemingEditorSettings.ActiveStyle = style;
            GameObject gameObject = new GameObject("Theme Provider Style Refresh Test");
            DeucarianThemeProvider provider = gameObject.AddComponent<DeucarianThemeProvider>();
            DeucarianThemeStyle broadcastStyle = null;

            try
            {
                provider.SetTheme(theme);
                provider.StyleChanged += changedStyle => broadcastStyle = changedStyle;

                Assert.IsTrue(DeucarianThemingMenuActions.AssignActiveStyleToActiveTheme());

                Assert.AreSame(style, theme.VisualStyle);
                Assert.AreSame(style, provider.CurrentStyle);
                Assert.AreSame(style, broadcastStyle);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetActiveThemeAndApplyAssignsThemeToOpenSceneProviders()
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            GameObject gameObject = new GameObject("Theme Provider Active Theme Test");
            DeucarianThemeProvider provider = gameObject.AddComponent<DeucarianThemeProvider>();

            try
            {
                int applied = DeucarianThemingMenuActions.SetActiveThemeAndApply(theme);

                Assert.GreaterOrEqual(applied, 1);
                Assert.AreSame(theme, DeucarianThemingEditorSettings.ActiveTheme);
                Assert.AreSame(theme, provider.CurrentTheme);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetActiveThemeFamilyAndModeAppliesResolvedVariantOnce()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/ApplyThemeFamily.asset");
            GameObject providerObject = new GameObject("Theme Family Provider Test");
            GameObject targetObject = new GameObject("Theme Family Target Test");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            ThemeTargetProbe probe = targetObject.AddComponent<ThemeTargetProbe>();

            try
            {
                int baseline = probe.ApplyCount;
                int applied = DeucarianThemingMenuActions.SetActiveThemeFamilyAndApply(assets.ThemeFamily);

                Assert.GreaterOrEqual(applied, 1);
                Assert.AreSame(assets.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
                Assert.AreSame(assets.DarkTheme, provider.CurrentTheme);
                Assert.AreEqual(DeucarianThemeMode.Dark, provider.ThemeMode);
                Assert.AreEqual(baseline + 1, probe.ApplyCount);

                baseline = probe.ApplyCount;
                DeucarianThemingMenuActions.SetActiveThemeModeAndApply(DeucarianThemeMode.Light);

                Assert.AreSame(assets.LightTheme, DeucarianThemingEditorSettings.ActiveTheme);
                Assert.AreSame(assets.LightPalette, DeucarianThemingEditorSettings.ActivePalette);
                Assert.AreSame(assets.LightTheme, provider.CurrentTheme);
                Assert.AreEqual(DeucarianThemeMode.Light, provider.ThemeMode);
                Assert.AreEqual(baseline + 1, probe.ApplyCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void ApplyingIncompleteFamilyUsesAvailableVariant()
        {
            DeucarianTheme lightTheme = CreateAsset<DeucarianTheme>(testRoot + "/OnlyLightTheme.asset");
            DeucarianThemeFamily family = CreateAsset<DeucarianThemeFamily>(testRoot + "/IncompleteFamily.asset");
            family.Configure("deucarian.test.incomplete", "Incomplete", lightTheme, null);
            GameObject providerObject = new GameObject("Incomplete Family Provider Test");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();

            try
            {
                int applied = DeucarianThemingMenuActions.ApplyThemeFamilyToOpenScene(
                    family,
                    DeucarianThemeMode.Dark,
                    false,
                    false);

                Assert.GreaterOrEqual(applied, 1);
                Assert.AreSame(family, provider.CurrentThemeFamily);
                Assert.AreSame(lightTheme, provider.CurrentTheme);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void EnsureProviderHasThemeUsesLegacyThemeWhenSerializedFamilyHasNoVariants()
        {
            DeucarianTheme legacyTheme = CreateAsset<DeucarianTheme>(testRoot + "/LegacyDefaultTheme.asset");
            DeucarianThemeFamily emptyFamily = CreateAsset<DeucarianThemeFamily>(testRoot + "/EmptyDefaultFamily.asset");
            emptyFamily.Configure("deucarian.test.empty-family", "Empty Family", null, null);
            DeucarianThemeRuntimeSettings settings = CreateAsset<DeucarianThemeRuntimeSettings>(
                testRoot + "/Resources/" + DeucarianThemeRuntimeSettings.ResourceName + ".asset");
            settings.Configure(legacyTheme);

            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty familyProperty = serializedSettings.FindProperty("defaultThemeFamily");
            Assert.NotNull(familyProperty);
            familyProperty.objectReferenceValue = emptyFamily;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Assert.AreSame(legacyTheme, settings.LegacyDefaultTheme);
            Assert.AreSame(emptyFamily, settings.DefaultThemeFamily);

            GameObject providerObject = new GameObject("Malformed Runtime Settings Provider Test");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();

            try
            {
                MethodInfo ensureFromSettings = typeof(DeucarianThemeRuntimeResolver).GetMethod(
                    "EnsureProviderHasThemeFromSettings",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(ensureFromSettings);
                Assert.IsTrue((bool)ensureFromSettings.Invoke(
                    null,
                    new object[] { provider, settings, provider }));
                Assert.IsNull(provider.CurrentThemeFamily);
                Assert.AreSame(legacyTheme, provider.CurrentTheme);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void WrapActiveThemeRefusesWhenFamilyIsAlreadyActive()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/AlreadyActiveThemeFamily.asset");
            DeucarianThemingEditorSettings.ActiveThemeFamily = assets.ThemeFamily;
            DeucarianThemingEditorSettings.ActiveTheme = assets.DarkTheme;

            DeucarianDefaultThemeAssets wrapped = DeucarianThemingMenuActions.WrapActiveThemeInFamily(
                DeucarianThemeMode.Dark,
                testRoot + "/ShouldNotExistThemeFamily.asset");

            Assert.IsNull(wrapped);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<DeucarianThemeFamily>(
                testRoot + "/ShouldNotExistThemeFamily.asset"));
        }

        [Test]
        public void SetActivePaletteAndApplyAssignsPaletteToActiveThemeAndRefreshesProvider()
        {
            DeucarianColorPalette firstPalette = CreateAsset<DeucarianColorPalette>(testRoot + "/FirstPalette.asset");
            DeucarianColorPalette secondPalette = CreateAsset<DeucarianColorPalette>(testRoot + "/SecondPalette.asset");
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            theme.Configure("deucarian.test.theme", "Theme", firstPalette);
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            GameObject providerObject = new GameObject("Theme Provider Palette Test");
            GameObject targetObject = new GameObject("Theme Target Palette Test");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            ThemeTargetProbe probe = targetObject.AddComponent<ThemeTargetProbe>();

            try
            {
                provider.SetTheme(theme);
                int countAfterSetTheme = probe.ApplyCount;

                Assert.IsTrue(DeucarianThemingMenuActions.SetActivePaletteAndApply(secondPalette));

                Assert.AreSame(secondPalette, DeucarianThemingEditorSettings.ActivePalette);
                Assert.AreSame(secondPalette, theme.ColorPalette);
                Assert.AreEqual(countAfterSetTheme + 1, probe.ApplyCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void SetActiveRoleLibraryAndApplyAssignsLibraryToActivePaletteAndRefreshesProvider()
        {
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>(testRoot + "/Palette.asset");
            DeucarianColorRoleLibrary roleLibrary = CreateAsset<DeucarianColorRoleLibrary>(testRoot + "/RoleLibrary.asset");
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            theme.Configure("deucarian.test.theme", "Theme", palette);
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            DeucarianThemingEditorSettings.ActivePalette = palette;
            GameObject providerObject = new GameObject("Theme Provider Library Test");
            GameObject targetObject = new GameObject("Theme Target Library Test");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            ThemeTargetProbe probe = targetObject.AddComponent<ThemeTargetProbe>();

            try
            {
                provider.SetTheme(theme);
                int countAfterSetTheme = probe.ApplyCount;

                Assert.IsTrue(DeucarianThemingMenuActions.SetActiveRoleLibraryAndApply(roleLibrary));

                Assert.AreSame(roleLibrary, DeucarianThemingEditorSettings.ActiveRoleLibrary);
                Assert.AreSame(roleLibrary, palette.RoleLibrary);
                Assert.AreEqual(countAfterSetTheme + 1, probe.ApplyCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void SetActiveStyleAndApplyAssignsStyleToActiveThemeAndRefreshesProvider()
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>(testRoot + "/Theme.asset");
            DeucarianThemeStyle style = CreateAsset<DeucarianThemeStyle>(testRoot + "/Style.asset");
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            GameObject providerObject = new GameObject("Theme Provider Active Style Test");
            GameObject targetObject = new GameObject("Style Target Active Style Test");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            StyleTargetProbe probe = targetObject.AddComponent<StyleTargetProbe>();

            try
            {
                provider.SetTheme(theme);
                int countAfterSetTheme = probe.ApplyCount;

                Assert.IsTrue(DeucarianThemingMenuActions.SetActiveStyleAndApply(style));

                Assert.AreSame(style, DeucarianThemingEditorSettings.ActiveStyle);
                Assert.AreSame(style, theme.VisualStyle);
                Assert.AreEqual(countAfterSetTheme + 1, probe.ApplyCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(providerObject);
            }
        }

        [Test]
        public void ExplicitVariantIsSourceControlledSharedByFamilyAndComposable()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianDefaultThemeAssetFactory.CreateThemeFamily(testRoot + "/VariantThemeFamily.asset");
            DeucarianThemeStyle source = assets.DefaultStyle;
            DeucarianThemeSurfaceProfile sourceSurface = source.SurfaceProfile;
            DeucarianThemeStrokeProfile sourceStroke = source.StrokeProfile;
            DeucarianThemeShapeProfile sourceShape = source.ShapeProfile;
            DeucarianThemeShapeProfile square = CreateAsset<DeucarianThemeShapeProfile>(
                testRoot + "/SquareShape.asset");
            square.Configure(
                DeucarianThemePresentationProfileIds.Shape.Square,
                "Square",
                "Square test shape.",
                0f);
            EditorUtility.SetDirty(square);
            AssetDatabase.SaveAssets();
            DeucarianThemingEditorSettings.ActiveThemeFamily = assets.ThemeFamily;
            DeucarianThemingEditorSettings.ActiveTheme = assets.LightTheme;
            DeucarianThemingEditorSettings.ActiveStyle = source;

            DeucarianThemeStyle variant = DeucarianThemingMenuActions.CreateStyleVariant(
                source,
                testRoot + "/Frosted Square Compact.asset");

            Assert.NotNull(variant);
            Assert.IsTrue(variant.IsVariant);
            Assert.AreEqual("deucarian.style.variant.frosted-square-compact", variant.StyleId);
            Assert.IsTrue(DeucarianColorRole.IsValidId(variant.StyleId));
            Assert.IsTrue(AssetDatabase.Contains(variant));
            Assert.AreSame(variant, assets.LightTheme.VisualStyle);
            Assert.AreSame(variant, assets.DarkTheme.VisualStyle);
            Assert.AreSame(sourceSurface, variant.SurfaceProfile);
            Assert.AreSame(sourceShape, variant.ShapeProfile);
            Assert.AreSame(sourceStroke, variant.StrokeProfile);

            Assert.IsTrue(DeucarianThemingMenuActions.UpdateStyleVariantComposition(
                variant,
                sourceSurface,
                square,
                sourceStroke,
                DeucarianThemeDensity.Compact));

            Assert.AreSame(square, variant.ShapeProfile);
            Assert.AreEqual(0f, variant.CornerRadius);
            Assert.AreEqual(DeucarianThemeDensity.Compact, variant.Density);
            Assert.AreSame(sourceShape, source.ShapeProfile);
            Assert.AreEqual(16f, source.CornerRadius);
            Assert.AreEqual(DeucarianThemeDensity.Comfortable, source.Density);
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

        private sealed class ThemeTargetProbe : MonoBehaviour, IDeucarianThemeTarget
        {
            public int ApplyCount { get; private set; }

            public void ApplyTheme(DeucarianTheme theme)
            {
                ApplyCount++;
            }
        }

        private sealed class StyleTargetProbe : MonoBehaviour, IDeucarianThemeStyleTarget
        {
            public int ApplyCount { get; private set; }

            public void ApplyStyle(DeucarianThemeStyle style)
            {
                ApplyCount++;
            }
        }
    }
}
