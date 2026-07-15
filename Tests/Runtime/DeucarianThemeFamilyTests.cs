using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianThemeFamilyTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void FamilyResolvesExactLightAndDarkVariants()
        {
            DeucarianTheme light = CreateTheme("light");
            DeucarianTheme dark = CreateTheme("dark");
            DeucarianThemeFamily family = CreateFamily(light, dark);

            Assert.IsTrue(family.IsComplete);
            Assert.AreSame(light, family.GetTheme(DeucarianThemeMode.Light));
            Assert.AreSame(dark, family.GetTheme(DeucarianThemeMode.Dark));
            Assert.IsTrue(family.TryGetTheme(DeucarianThemeMode.Light, out DeucarianTheme resolvedLight));
            Assert.AreSame(light, resolvedLight);
        }

        [Test]
        public void IncompleteFamilyFallsBackToAvailableVariant()
        {
            DeucarianTheme dark = CreateTheme("dark");
            DeucarianThemeFamily family = CreateFamily(null, dark);

            Assert.IsFalse(family.IsComplete);
            Assert.IsFalse(family.TryGetTheme(DeucarianThemeMode.Light, out _));
            Assert.AreSame(dark, family.ResolveTheme(DeucarianThemeMode.Light));
            Assert.AreSame(dark, family.ResolveTheme(DeucarianThemeMode.Dark));
        }

        [Test]
        public void FamilyMutationsPublishAssetChangeNotifications()
        {
            DeucarianTheme light = CreateTheme("light");
            DeucarianTheme dark = CreateTheme("dark");
            DeucarianThemeFamily family = CreateAsset<DeucarianThemeFamily>();
            UnityEngine.Object changedAsset = null;
            Action<UnityEngine.Object> handler = asset => changedAsset = asset;
            DeucarianThemeAssetChangeBus.AssetChanged += handler;

            try
            {
                family.Configure("deucarian.test.family", "Test Family", light, dark);
                Assert.AreSame(family, changedAsset);

                changedAsset = null;
                family.SetTheme(DeucarianThemeMode.Light, dark);
                Assert.AreSame(family, changedAsset);
            }
            finally
            {
                DeucarianThemeAssetChangeBus.AssetChanged -= handler;
            }
        }

        [Test]
        public void SharedVisualStylePublishesOneConsolidatedFamilyChange()
        {
            DeucarianTheme light = CreateTheme("light-shared-style");
            DeucarianTheme dark = CreateTheme("dark-shared-style");
            DeucarianThemeFamily family = CreateFamily(light, dark);
            DeucarianThemeStyle style = CreateRuntimeStyle(DeucarianThemeStyleIds.FrostedGlass);
            int notificationCount = 0;
            UnityEngine.Object changedAsset = null;
            Action<UnityEngine.Object> handler = asset =>
            {
                notificationCount++;
                changedAsset = asset;
            };
            DeucarianThemeAssetChangeBus.AssetChanged += handler;

            try
            {
                Assert.IsTrue(family.SetSharedVisualStyle(style));
                Assert.AreSame(style, light.VisualStyle);
                Assert.AreSame(style, dark.VisualStyle);
                Assert.AreEqual(1, notificationCount);
                Assert.AreSame(family, changedAsset);

                Assert.IsFalse(family.SetSharedVisualStyle(style));
                Assert.AreEqual(1, notificationCount);
            }
            finally
            {
                DeucarianThemeAssetChangeBus.AssetChanged -= handler;
            }
        }

        [Test]
        public void ProviderDefaultsToDarkAndResolvesFamilyVariant()
        {
            DeucarianTheme light = CreateTheme("light");
            DeucarianTheme dark = CreateTheme("dark");
            DeucarianThemeFamily family = CreateFamily(light, dark);
            DeucarianThemeProvider provider = CreateProviderWithProbes(
                out ThemeTargetProbe themeProbe,
                out _);

            provider.SetThemeFamily(family);

            Assert.AreEqual(DeucarianThemeMode.Dark, provider.ThemeMode);
            Assert.AreSame(family, provider.CurrentThemeFamily);
            Assert.AreSame(dark, provider.CurrentTheme);
            Assert.AreSame(dark, themeProbe.AppliedTheme);
        }

        [Test]
        public void ProviderModeSwitchReappliesThemeAndPerVariantStyle()
        {
            DeucarianThemeStyle lightStyle = CreateRuntimeStyle(DeucarianThemeStyleIds.FrostedGlass);
            DeucarianThemeStyle darkStyle = CreateRuntimeStyle(DeucarianThemeStyleIds.MaterialDark);
            DeucarianTheme light = CreateTheme("light", lightStyle);
            DeucarianTheme dark = CreateTheme("dark", darkStyle);
            DeucarianThemeFamily family = CreateFamily(light, dark);
            DeucarianThemeProvider provider = CreateProviderWithProbes(
                out ThemeTargetProbe themeProbe,
                out StyleTargetProbe styleProbe);
            int themeChangedCount = 0;
            int modeChangedCount = 0;
            DeucarianTheme changedTheme = null;
            DeucarianThemeMode changedMode = DeucarianThemeMode.Dark;

            provider.SetThemeFamily(family);
            provider.ThemeChanged += theme =>
            {
                themeChangedCount++;
                changedTheme = theme;
            };
            provider.ThemeModeChanged += mode =>
            {
                modeChangedCount++;
                changedMode = mode;
            };

            provider.SetThemeMode(DeucarianThemeMode.Light);

            Assert.AreSame(light, provider.CurrentTheme);
            Assert.AreSame(light, themeProbe.AppliedTheme);
            Assert.AreSame(lightStyle, provider.CurrentStyle);
            Assert.AreSame(lightStyle, styleProbe.AppliedStyle);
            Assert.AreSame(light, changedTheme);
            Assert.AreEqual(DeucarianThemeMode.Light, changedMode);
            Assert.AreEqual(1, themeChangedCount);
            Assert.AreEqual(1, modeChangedCount);
        }

        [Test]
        public void ProviderAtomicallySetsFamilyAndModeWithOneRefresh()
        {
            DeucarianTheme light = CreateTheme("light");
            DeucarianTheme dark = CreateTheme("dark");
            DeucarianThemeFamily family = CreateFamily(light, dark);
            DeucarianThemeProvider provider = CreateProviderWithProbes(
                out ThemeTargetProbe themeProbe,
                out _);
            int themeChangedCount = 0;
            int modeChangedCount = 0;
            provider.ThemeChanged += _ => themeChangedCount++;
            provider.ThemeModeChanged += _ => modeChangedCount++;

            provider.SetThemeFamily(family, DeucarianThemeMode.Light);

            Assert.AreSame(family, provider.CurrentThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, provider.ThemeMode);
            Assert.AreSame(light, provider.CurrentTheme);
            Assert.AreSame(light, themeProbe.AppliedTheme);
            Assert.AreEqual(1, themeProbe.ApplyCount);
            Assert.AreEqual(1, themeChangedCount);
            Assert.AreEqual(1, modeChangedCount);

            provider.SetThemeFamily(family, DeucarianThemeMode.Light);

            Assert.AreEqual(2, themeProbe.ApplyCount);
            Assert.AreEqual(2, themeChangedCount);
            Assert.AreEqual(1, modeChangedCount);
        }

        [Test]
        public void ProviderWarnsOnceAndFallsBackForIncompleteFamily()
        {
            DeucarianTheme dark = CreateTheme("dark");
            DeucarianThemeFamily family = CreateFamily(null, dark);
            DeucarianThemeProvider provider = CreateProviderWithProbes(out _, out _);
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Theme family .* is incomplete.*runtime fallback", RegexOptions.IgnoreCase));

            provider.SetThemeFamily(family);
            Assert.AreSame(dark, provider.CurrentTheme);
            provider.SetThemeMode(DeucarianThemeMode.Light);
            Assert.AreSame(dark, provider.CurrentTheme);
            provider.SetThemeFamily(family, DeucarianThemeMode.Light);
            provider.RefreshThemeGraph();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ProviderTracksFamilyAndBothVariantAssetGraphs()
        {
            DeucarianThemeStyle lightStyle = CreateRuntimeStyle(DeucarianThemeStyleIds.FrostedGlass);
            DeucarianThemeStyle darkStyle = CreateRuntimeStyle(DeucarianThemeStyleIds.MaterialDark);
            DeucarianColorPalette lightPalette = CreateAsset<DeucarianColorPalette>();
            DeucarianColorPalette darkPalette = CreateAsset<DeucarianColorPalette>();
            DeucarianTheme light = CreateTheme("light", lightStyle, lightPalette);
            DeucarianTheme dark = CreateTheme("dark", darkStyle, darkPalette);
            DeucarianThemeFamily family = CreateFamily(light, dark);
            DeucarianThemeProvider provider = CreateProviderWithProbes(
                out ThemeTargetProbe themeProbe,
                out _);

            provider.SetThemeFamily(family);
            InvokePrivate(provider, "OnDisable");
            InvokePrivate(provider, "OnEnable");

            try
            {
                Assert.IsTrue(provider.UsesThemeAsset(family));
                Assert.IsTrue(provider.UsesThemeAsset(light));
                Assert.IsTrue(provider.UsesThemeAsset(dark));
                Assert.IsTrue(provider.UsesThemeAsset(lightStyle));
                Assert.IsTrue(provider.UsesThemeAsset(darkStyle));
                Assert.IsTrue(provider.UsesThemeAsset(lightPalette));
                Assert.IsTrue(provider.UsesThemeAsset(darkPalette));

                DeucarianTheme replacementDark = CreateTheme("replacement-dark", darkStyle, darkPalette);
                int applyCount = themeProbe.ApplyCount;
                family.SetTheme(DeucarianThemeMode.Dark, replacementDark);

                Assert.AreEqual(applyCount + 1, themeProbe.ApplyCount);
                Assert.AreSame(replacementDark, themeProbe.AppliedTheme);
            }
            finally
            {
                InvokePrivate(provider, "OnDisable");
            }
        }

        [Test]
        public void StandaloneSetThemeRemainsSupportedAndClearsFamily()
        {
            DeucarianTheme light = CreateTheme("light");
            DeucarianTheme dark = CreateTheme("dark");
            DeucarianTheme standalone = CreateTheme("standalone");
            DeucarianThemeProvider provider = CreateProviderWithProbes(
                out ThemeTargetProbe themeProbe,
                out _);
            provider.SetThemeFamily(CreateFamily(light, dark));

            provider.SetTheme(standalone);

            Assert.IsNull(provider.CurrentThemeFamily);
            Assert.AreSame(standalone, provider.CurrentTheme);
            Assert.AreSame(standalone, themeProbe.AppliedTheme);
        }

        [Test]
        public void RuntimeSettingsResolveFamiliesAndPreserveLegacyConfiguration()
        {
            DeucarianTheme light = CreateTheme("light");
            DeucarianTheme dark = CreateTheme("dark");
            DeucarianThemeFamily family = CreateFamily(light, dark);
            DeucarianThemeRuntimeSettings settings = CreateAsset<DeucarianThemeRuntimeSettings>();

            Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);

            settings.Configure(family, DeucarianThemeMode.Light);
            Assert.AreSame(family, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, settings.DefaultThemeMode);
            Assert.AreSame(light, settings.DefaultTheme);
            Assert.AreSame(light, settings.ResolvedDefaultTheme);
            Assert.IsNull(settings.LegacyDefaultTheme);

            settings.SetDefaultThemeMode(DeucarianThemeMode.Dark);
            Assert.AreSame(dark, settings.DefaultTheme);

            settings.Configure(light);
            Assert.IsNull(settings.DefaultThemeFamily);
            Assert.AreSame(light, settings.LegacyDefaultTheme);
            Assert.AreSame(light, settings.DefaultTheme);

            settings.ConfigureThemeFamily(family);
            Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);
            Assert.AreSame(dark, settings.DefaultTheme);
        }

        [Test]
        public void RuntimeSettingsUseAvailableVariantForIncompleteFamily()
        {
            DeucarianTheme light = CreateTheme("light");
            DeucarianThemeFamily family = CreateFamily(light, null);
            DeucarianThemeRuntimeSettings settings = CreateAsset<DeucarianThemeRuntimeSettings>();

            settings.Configure(family, DeucarianThemeMode.Dark);

            Assert.AreSame(light, settings.DefaultTheme);
        }

        [Test]
        public void RuntimeSettingsFallBackToLegacyThemeWhenFamilyHasNoVariant()
        {
            DeucarianTheme legacy = CreateTheme("legacy");
            DeucarianThemeFamily malformedFamily = CreateFamily(null, null);
            DeucarianThemeRuntimeSettings settings = CreateAsset<DeucarianThemeRuntimeSettings>();
            settings.Configure(legacy);
            SetPrivateField(settings, "defaultThemeFamily", malformedFamily);

            Assert.AreSame(legacy, settings.LegacyDefaultTheme);
            Assert.AreSame(legacy, settings.ResolvedDefaultTheme);
            Assert.AreSame(legacy, settings.DefaultTheme);
        }

        [Test]
        public void ProviderInitializationUsesLegacyFallbackWhenConfiguredFamilyHasNoVariant()
        {
            DeucarianTheme legacy = CreateTheme("legacy-provider-default");
            DeucarianThemeFamily malformedFamily = CreateFamily(null, null);
            DeucarianThemeRuntimeSettings settings = CreateAsset<DeucarianThemeRuntimeSettings>();
            settings.Configure(legacy);
            SetPrivateField(settings, "defaultThemeFamily", malformedFamily);
            DeucarianThemeProvider provider = CreateProviderWithProbes(out ThemeTargetProbe probe, out _);
            LogAssert.Expect(
                LogType.Warning,
                new Regex(
                    "runtime theme family .* is incomplete.*No runtime fallback",
                    RegexOptions.IgnoreCase));

            bool assigned = InvokeEnsureProviderHasThemeFromSettings(provider, settings);

            Assert.IsTrue(assigned);
            Assert.IsNull(provider.CurrentThemeFamily);
            Assert.AreSame(legacy, provider.CurrentTheme);
            Assert.AreSame(legacy, probe.AppliedTheme);
        }

        [Test]
        public void PairedThemePackExposesMetadataAndDarkLegacyDefaults()
        {
            Color lightColor = new Color(0.9f, 0.8f, 0.7f, 1f);
            Color darkColor = new Color(0.1f, 0.2f, 0.3f, 1f);
            DeucarianThemePackRole role = new DeucarianThemePackRole(
                "Primary",
                DeucarianBuiltinColorRoleIds.Core.Primary,
                "Primary",
                DeucarianColorRoleCategories.Semantic,
                "Primary role.",
                lightColor,
                darkColor,
                true);
            DeucarianThemePack pack = CreateAsset<DeucarianThemePack>();

            ConfigurePairedPack(pack, role);

            Assert.IsTrue(pack.SupportsThemeFamily);
            Assert.AreEqual("Family.asset", pack.FamilyFileName);
            Assert.AreEqual("deucarian.test.family", pack.FamilyId);
            Assert.AreEqual("LightPalette.asset", pack.LightPaletteFileName);
            Assert.AreEqual("DarkPalette.asset", pack.DarkPaletteFileName);
            Assert.AreEqual("LightTheme.asset", pack.LightThemeFileName);
            Assert.AreEqual("DarkTheme.asset", pack.DarkThemeFileName);
            Assert.AreEqual(pack.DarkPaletteFileName, pack.PaletteFileName);
            Assert.AreEqual(pack.DarkPaletteId, pack.PaletteId);
            Assert.AreEqual(pack.DarkThemeFileName, pack.ThemeFileName);
            Assert.AreEqual(pack.DarkThemeId, pack.ThemeId);
            Assert.AreEqual(1, pack.Roles.Count);

            DeucarianThemePackRole clonedRole = pack.Roles[0];
            Assert.AreNotSame(role, clonedRole);
            Assert.IsTrue(clonedRole.HasPairedColors);
            Assert.AreEqual(lightColor, clonedRole.LightColor);
            Assert.AreEqual(darkColor, clonedRole.DarkColor);
            Assert.AreEqual(darkColor, clonedRole.DefaultColor);
        }

        [Test]
        public void LegacyPackRoleUsesOneColorForBothModes()
        {
            DeucarianThemePackRole role = new DeucarianThemePackRole(
                "Primary",
                DeucarianBuiltinColorRoleIds.Core.Primary,
                "Primary",
                DeucarianColorRoleCategories.Semantic,
                "Primary role.",
                Color.cyan,
                true);

            Assert.IsFalse(role.HasPairedColors);
            Assert.AreEqual(Color.cyan, role.DefaultColor);
            Assert.AreEqual(Color.cyan, role.LightColor);
            Assert.AreEqual(Color.cyan, role.DarkColor);
        }

        [Test]
        public void PairedPackValidationRestoresDarkLegacyAliases()
        {
            Color lightColor = Color.white;
            Color darkColor = Color.black;
            DeucarianThemePackRole role = new DeucarianThemePackRole(
                "Primary",
                DeucarianBuiltinColorRoleIds.Core.Primary,
                "Primary",
                DeucarianColorRoleCategories.Semantic,
                "Primary role.",
                lightColor,
                darkColor,
                true);
            DeucarianThemePack pack = CreateAsset<DeucarianThemePack>();
            ConfigurePairedPack(pack, role);
            DeucarianThemePackRole storedRole = pack.Roles[0];
            SetPrivateField(pack, "paletteFileName", "WrongPalette.asset");
            SetPrivateField(pack, "paletteId", "wrong.palette");
            SetPrivateField(pack, "paletteDisplayName", "Wrong Palette");
            SetPrivateField(pack, "themeFileName", "WrongTheme.asset");
            SetPrivateField(pack, "themeId", "wrong.theme");
            SetPrivateField(pack, "themeDisplayName", "Wrong Theme");
            SetPrivateField(storedRole, "defaultColor", Color.magenta);

            InvokePrivate(pack, "OnValidate");

            Assert.AreEqual(pack.DarkPaletteFileName, pack.PaletteFileName);
            Assert.AreEqual(pack.DarkPaletteId, pack.PaletteId);
            Assert.AreEqual(pack.DarkPaletteDisplayName, pack.PaletteDisplayName);
            Assert.AreEqual(pack.DarkThemeFileName, pack.ThemeFileName);
            Assert.AreEqual(pack.DarkThemeId, pack.ThemeId);
            Assert.AreEqual(pack.DarkThemeDisplayName, pack.ThemeDisplayName);
            Assert.AreEqual(darkColor, storedRole.DefaultColor);
        }

        private void ConfigurePairedPack(DeucarianThemePack pack, DeucarianThemePackRole role)
        {
            pack.Configure(
                "deucarian.test.pack",
                "Test Pack",
                "Roles",
                "Family",
                "LightPalette",
                "DarkPalette",
                "LightTheme",
                "DarkTheme",
                "deucarian.test.family",
                "Test Family",
                "deucarian.test.palette.light",
                "Light Palette",
                "deucarian.test.palette.dark",
                "Dark Palette",
                "deucarian.test.theme.light",
                "Light Theme",
                "deucarian.test.theme.dark",
                "Dark Theme",
                DeucarianThemeStyleIds.FrostedGlass,
                new[] { role });
        }

        private DeucarianThemeProvider CreateProviderWithProbes(
            out ThemeTargetProbe themeProbe,
            out StyleTargetProbe styleProbe)
        {
            GameObject providerObject = CreateGameObject("Provider");
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            themeProbe = targetObject.AddComponent<ThemeTargetProbe>();
            styleProbe = targetObject.AddComponent<StyleTargetProbe>();
            return provider;
        }

        private DeucarianThemeFamily CreateFamily(DeucarianTheme light, DeucarianTheme dark)
        {
            DeucarianThemeFamily family = CreateAsset<DeucarianThemeFamily>();
            family.name = "Test Theme Family";
            family.Configure("deucarian.test.family", "Test Theme Family", light, dark);
            return family;
        }

        private DeucarianTheme CreateTheme(
            string suffix,
            DeucarianThemeStyle style = null,
            DeucarianColorPalette palette = null)
        {
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            theme.Configure("deucarian.test.theme." + suffix, suffix, palette, style);
            return theme;
        }

        private DeucarianThemeStyle CreateRuntimeStyle(string styleId)
        {
            DeucarianThemeStyle style = DeucarianThemeStylePresets.CreateRuntimeStyle(styleId);
            createdObjects.Add(style);
            return style;
        }

        private GameObject CreateGameObject(string objectName)
        {
            GameObject gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private T CreateAsset<T>()
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, methodName);
            method.Invoke(target, null);
        }

        private static bool InvokeEnsureProviderHasThemeFromSettings(
            DeucarianThemeProvider provider,
            DeucarianThemeRuntimeSettings settings)
        {
            MethodInfo method = typeof(DeucarianThemeRuntimeResolver).GetMethod(
                "EnsureProviderHasThemeFromSettings",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (bool)method.Invoke(null, new object[] { provider, settings, null });
        }

        private sealed class ThemeTargetProbe : MonoBehaviour, IDeucarianThemeTarget
        {
            public DeucarianTheme AppliedTheme { get; private set; }
            public int ApplyCount { get; private set; }

            public void ApplyTheme(DeucarianTheme theme)
            {
                AppliedTheme = theme;
                ApplyCount++;
            }
        }

        private sealed class StyleTargetProbe : MonoBehaviour, IDeucarianThemeStyleTarget
        {
            public DeucarianThemeStyle AppliedStyle { get; private set; }

            public void ApplyStyle(DeucarianThemeStyle style)
            {
                AppliedStyle = style;
            }
        }
    }
}
