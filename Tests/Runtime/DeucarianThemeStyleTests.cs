using System.Collections.Generic;
using System.Reflection;
using Deucarian.Theming.UIToolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UIImage = UnityEngine.UI.Image;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianThemeStyleTests
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
        public void ThemeStoresVisualStyleAndResolvesColorById()
        {
            DeucarianColorRole role = CreateRole(DeucarianBuiltinColorRoleIds.Core.SurfaceRaised, Color.black);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            DeucarianThemeStyle style = CreateFrostedStyle();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();

            palette.AddEntry(role, Color.cyan);
            theme.Configure("deucarian.theme.style-test", "Style Test", palette, style);

            Assert.AreSame(style, theme.VisualStyle);
            Assert.IsTrue(theme.TryGetColorById(DeucarianBuiltinColorRoleIds.Core.SurfaceRaised, out Color color));
            Assert.AreEqual(Color.cyan, color);
        }

        [Test]
        public void StyleResolvesSurfaceAndBorderFromBaseColor()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            Color surface = style.ResolveSurfaceColor(new Color(0.1f, 0.15f, 0.2f, 0.9f));
            Color border = style.ResolveBorderColor(surface);

            Assert.Greater(surface.r, 0.1f);
            Assert.AreEqual(0.558f, surface.a, 0.001f);
            Assert.AreEqual(0.48f, border.a, 0.001f);
            Assert.AreEqual(1f, style.BorderWidth);
            Assert.AreEqual(16f, style.CornerRadius);
        }

        [Test]
        public void BuiltinPresetCreatesRuntimeFrostedGlassStyle()
        {
            DeucarianThemeStyle style = DeucarianThemeStylePresets.CreateRuntimeStyle(
                DeucarianThemeStyleIds.FrostedGlass);
            createdObjects.Add(style);

            Assert.IsNotNull(style);
            Assert.AreEqual(DeucarianThemeStyleIds.FrostedGlass, style.StyleId);
            Assert.AreEqual(DeucarianThemeStyleSurfaceTreatment.FrostedGlass, style.SurfaceTreatment);
            Assert.AreEqual(HideFlags.HideAndDontSave, style.hideFlags);
            Assert.IsNotNull(style.GetGeneratedTexture());
        }

        [Test]
        public void BuiltinPresetsExposeThreeDefaultStyles()
        {
            IReadOnlyList<DeucarianThemeStylePreset> presets = DeucarianThemeStylePresets.BuiltinStyles;

            Assert.AreEqual(3, presets.Count);
            Assert.IsTrue(DeucarianThemeStylePresets.TryGetBuiltinStyle(
                DeucarianThemeStyleIds.MaterialDark,
                out DeucarianThemeStylePreset materialDark));
            Assert.AreEqual(DeucarianThemeStyleSurfaceTreatment.Material, materialDark.SurfaceTreatment);
        }

        [Test]
        public void ComposedStyleResolvesIndependentSurfaceShapeStrokeAndDensity()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            DeucarianThemeSurfaceProfile surface = CreateAsset<DeucarianThemeSurfaceProfile>();
            DeucarianThemeShapeProfile square = CreateAsset<DeucarianThemeShapeProfile>();
            DeucarianThemeStrokeProfile borderless = CreateAsset<DeucarianThemeStrokeProfile>();
            surface.Configure(
                DeucarianThemePresentationProfileIds.Surface.FrostedGlass,
                "Frosted Glass",
                "Test surface.",
                DeucarianThemeStyleSurfaceTreatment.FrostedGlass,
                style.DarkSurfaceTint,
                style.LightSurfaceTint,
                style.SurfaceTintStrength,
                style.SurfaceAlphaMultiplier,
                style.MinimumSurfaceAlpha,
                style.MaximumSurfaceAlpha,
                true,
                style.TextureTint,
                style.GeneratedTextureSize,
                4,
                0.92f);
            square.Configure(
                DeucarianThemePresentationProfileIds.Shape.Square,
                "Square",
                "Test shape.",
                0f);
            borderless.Configure(
                DeucarianThemePresentationProfileIds.Stroke.Borderless,
                "Borderless",
                "Test stroke.",
                Color.clear,
                0f,
                0f,
                0f);

            style.SetComposition(
                surface,
                square,
                borderless,
                DeucarianThemeDensity.Compact,
                true);

            Assert.IsTrue(style.IsComposed);
            Assert.IsTrue(style.IsVariant);
            Assert.AreSame(surface, style.SurfaceProfile);
            Assert.AreEqual(DeucarianThemeStyleSurfaceTreatment.FrostedGlass, style.SurfaceTreatment);
            Assert.AreEqual(0f, style.CornerRadius);
            Assert.AreEqual(0f, style.BorderWidth);
            Assert.AreEqual(DeucarianThemeDensity.Compact, style.Density);
            Assert.IsNotNull(style.GetGeneratedTexture());
        }

        [Test]
        public void ComponentMutationRefreshesStyleProvider()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            DeucarianThemeShapeProfile shape = CreateAsset<DeucarianThemeShapeProfile>();
            shape.Configure(
                DeucarianThemePresentationProfileIds.Shape.Rounded,
                "Rounded",
                "Test shape.",
                16f);
            style.SetComposition(null, shape, null, DeucarianThemeDensity.Comfortable, true);
            GameObject providerObject = CreateGameObject("Provider");
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            StyleTargetProbe probe = targetObject.AddComponent<StyleTargetProbe>();
            provider.SetStyle(style);
            StartProviderAssetChangeTracking(provider);
            int countBeforeMutation = probe.ApplyCount;
            int eventCount = 0;
            provider.StyleChanged += _ => eventCount++;

            try
            {
                shape.Configure(
                    DeucarianThemePresentationProfileIds.Shape.Square,
                    "Square",
                    "Updated test shape.",
                    0f);

                Assert.AreEqual(0f, style.CornerRadius);
                Assert.AreEqual(countBeforeMutation + 1, probe.ApplyCount);
                Assert.AreEqual(1, eventCount);
                Assert.IsTrue(provider.UsesThemeAsset(shape));
            }
            finally
            {
                StopProviderAssetChangeTracking(provider);
            }
        }

        [Test]
        public void ProviderAppliesStyleOverrideToChildTargets()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            GameObject providerObject = CreateGameObject("Provider");
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            StyleTargetProbe probe = targetObject.AddComponent<StyleTargetProbe>();

            provider.SetStyle(style);

            Assert.AreSame(style, provider.CurrentStyle);
            Assert.AreSame(style, probe.AppliedStyle);
            Assert.AreEqual(1, probe.ApplyCount);
        }

        [Test]
        public void ProviderUsesThemeStyleWhenThemeChanges()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            theme.Configure("deucarian.theme.with-style", "With Style", null, style);
            GameObject providerObject = CreateGameObject("Provider");
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            StyleTargetProbe probe = targetObject.AddComponent<StyleTargetProbe>();

            provider.SetTheme(theme);

            Assert.AreSame(style, provider.CurrentStyle);
            Assert.AreSame(style, probe.AppliedStyle);
        }

        [Test]
        public void ProviderRefreshStyleAppliesThemeVisualStyleMutation()
        {
            DeucarianThemeStyle firstStyle = CreateFrostedStyle();
            DeucarianThemeStyle secondStyle = DeucarianThemeStylePresets.CreateRuntimeStyle(
                DeucarianThemeStyleIds.MaterialDark);
            createdObjects.Add(secondStyle);
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            theme.Configure("deucarian.theme.with-style", "With Style", null, firstStyle);
            GameObject providerObject = CreateGameObject("Provider");
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            StyleTargetProbe probe = targetObject.AddComponent<StyleTargetProbe>();
            int styleChangedCount = 0;
            DeucarianThemeStyle broadcastStyle = null;

            provider.SetTheme(theme);
            StartProviderAssetChangeTracking(provider);
            int countBeforeMutation = probe.ApplyCount;
            provider.StyleChanged += style =>
            {
                styleChangedCount++;
                broadcastStyle = style;
            };

            try
            {
                theme.SetVisualStyle(secondStyle);

                Assert.AreSame(secondStyle, provider.CurrentStyle);
                Assert.AreSame(secondStyle, probe.AppliedStyle);
                Assert.AreSame(secondStyle, broadcastStyle);
                Assert.AreEqual(countBeforeMutation + 1, probe.ApplyCount);
                Assert.AreEqual(1, styleChangedCount);
            }
            finally
            {
                StopProviderAssetChangeTracking(provider);
            }
        }

        [Test]
        public void ProviderRefreshesThemeTargetsWhenPaletteAssetChanges()
        {
            DeucarianColorRole role = CreateRole(DeucarianBuiltinColorRoleIds.Core.Primary, Color.black);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            palette.AddEntry(role, Color.red);
            theme.Configure("deucarian.theme.palette-refresh", "Palette Refresh", palette);
            GameObject providerObject = CreateGameObject("Provider");
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            ThemeTargetProbe probe = targetObject.AddComponent<ThemeTargetProbe>();

            provider.SetTheme(theme);
            StartProviderAssetChangeTracking(provider);
            int countAfterSetTheme = probe.ApplyCount;

            try
            {
                palette.SetColor(role, Color.green);

                Assert.AreEqual(countAfterSetTheme + 1, probe.ApplyCount);
                Assert.AreSame(theme, probe.AppliedTheme);
            }
            finally
            {
                StopProviderAssetChangeTracking(provider);
            }
        }

        [Test]
        public void ProviderRefreshesThemeTargetsWhenRoleAssetChanges()
        {
            DeucarianColorRole role = CreateRole(DeucarianBuiltinColorRoleIds.Core.Primary, Color.black);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            theme.Configure("deucarian.theme.role-refresh", "Role Refresh", palette);
            GameObject providerObject = CreateGameObject("Provider");
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            ThemeTargetProbe probe = targetObject.AddComponent<ThemeTargetProbe>();

            provider.SetTheme(theme);
            StartProviderAssetChangeTracking(provider);
            int countAfterSetTheme = probe.ApplyCount;

            try
            {
                role.Configure(
                    DeucarianBuiltinColorRoleIds.Core.Primary,
                    "Primary Updated",
                    "Tests",
                    "Updated role.",
                    Color.blue,
                    false);

                Assert.AreEqual(countAfterSetTheme + 1, probe.ApplyCount);
                Assert.AreSame(theme, probe.AppliedTheme);
            }
            finally
            {
                StopProviderAssetChangeTracking(provider);
            }
        }

        [Test]
        public void ProviderRefreshesStyleTargetsWhenStyleAssetChanges()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            GameObject providerObject = CreateGameObject("Provider");
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            StyleTargetProbe probe = targetObject.AddComponent<StyleTargetProbe>();

            provider.SetStyle(style);
            StartProviderAssetChangeTracking(provider);
            int countAfterSetStyle = probe.ApplyCount;

            try
            {
                style.Configure(
                    DeucarianThemeStyleIds.FrostedGlass,
                    "Frosted Glass",
                    "Updated style.",
                    DeucarianThemeStyleSurfaceTreatment.FrostedGlass,
                    style.DarkSurfaceTint,
                    style.LightSurfaceTint,
                    0.42f,
                    style.SurfaceAlphaMultiplier,
                    style.MinimumSurfaceAlpha,
                    style.MaximumSurfaceAlpha,
                    style.BorderTint,
                    style.BorderTintStrength,
                    style.BorderAlpha,
                    style.BorderWidth,
                    style.CornerRadius,
                    true,
                    style.TextureTint,
                    style.GeneratedTextureSize,
                    style.GeneratedTextureBlurRadius,
                    style.GeneratedTextureBlurStrength);

                Assert.AreEqual(countAfterSetStyle + 1, probe.ApplyCount);
                Assert.AreSame(style, probe.AppliedStyle);
            }
            finally
            {
                StopProviderAssetChangeTracking(provider);
            }
        }

        [Test]
        public void GeneratedFrostedTextureUsesBlurControls()
        {
            DeucarianThemeStyle rawStyle = CreateFrostedStyle();
            rawStyle.Configure(
                DeucarianThemeStyleIds.FrostedGlass,
                "Raw Frosted Glass",
                "Unblurred texture.",
                DeucarianThemeStyleSurfaceTreatment.FrostedGlass,
                rawStyle.DarkSurfaceTint,
                rawStyle.LightSurfaceTint,
                rawStyle.SurfaceTintStrength,
                rawStyle.SurfaceAlphaMultiplier,
                rawStyle.MinimumSurfaceAlpha,
                rawStyle.MaximumSurfaceAlpha,
                rawStyle.BorderTint,
                rawStyle.BorderTintStrength,
                rawStyle.BorderAlpha,
                rawStyle.BorderWidth,
                rawStyle.CornerRadius,
                true,
                rawStyle.TextureTint,
                48,
                0,
                0f);
            DeucarianThemeStyle blurredStyle = CreateFrostedStyle();
            blurredStyle.Configure(
                DeucarianThemeStyleIds.FrostedGlass,
                "Blurred Frosted Glass",
                "Blurred texture.",
                DeucarianThemeStyleSurfaceTreatment.FrostedGlass,
                blurredStyle.DarkSurfaceTint,
                blurredStyle.LightSurfaceTint,
                blurredStyle.SurfaceTintStrength,
                blurredStyle.SurfaceAlphaMultiplier,
                blurredStyle.MinimumSurfaceAlpha,
                blurredStyle.MaximumSurfaceAlpha,
                blurredStyle.BorderTint,
                blurredStyle.BorderTintStrength,
                blurredStyle.BorderAlpha,
                blurredStyle.BorderWidth,
                blurredStyle.CornerRadius,
                true,
                blurredStyle.TextureTint,
                48,
                4,
                1f);

            Texture2D rawTexture = rawStyle.GetGeneratedTexture();
            Texture2D blurredTexture = blurredStyle.GetGeneratedTexture();

            Assert.AreEqual(4, blurredStyle.GeneratedTextureBlurRadius);
            Assert.AreEqual(1f, blurredStyle.GeneratedTextureBlurStrength);
            Assert.Less(AverageNeighborDelta(blurredTexture), AverageNeighborDelta(rawTexture));
        }

        [Test]
        public void UIToolkitStyleUtilityAppliesPanelProperties()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            VisualElement element = new VisualElement();

            Assert.IsTrue(DeucarianUIToolkitThemeStyleUtility.ApplyPanel(element, Color.black, style));

            Assert.AreEqual(style.BorderWidth, element.style.borderLeftWidth.value);
            Assert.AreEqual(style.CornerRadius, element.style.borderTopLeftRadius.value.value);
            Assert.AreEqual(style.TextureTint, element.style.unityBackgroundImageTintColor.value);
        }

        [Test]
        public void UGUIStyleUtilityAppliesPanelAndOutline()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            GameObject gameObject = CreateGameObject("UGUI Panel");
            UIImage image = gameObject.AddComponent<UIImage>();
            Outline outline = gameObject.AddComponent<Outline>();

            Assert.IsTrue(DeucarianUGUIThemeStyleUtility.ApplyPanel(image, Color.black, style));
            Assert.IsTrue(DeucarianUGUIThemeStyleUtility.ApplyOutline(outline, image.color, style));

            Assert.AreEqual(style.ResolveSurfaceColor(Color.black), image.color);
            Assert.AreEqual(style.ResolveBorderColor(image.color), outline.effectColor);
        }

        [Test]
        public void UGUIStyleUtilityAppliesGeneratedTextureToImagePanels()
        {
            DeucarianThemeStyle style = CreateFrostedStyle();
            GameObject gameObject = CreateGameObject("UGUI Image Panel");
            UIImage image = gameObject.AddComponent<UIImage>();

            Assert.IsTrue(DeucarianUGUIThemeStyleUtility.ApplyPanelImage(image, Color.black, style));

            Assert.NotNull(image.sprite);
            Assert.AreSame(style.GetGeneratedTexture(), image.sprite.texture);
            Assert.AreEqual(UIImage.Type.Tiled, image.type);
        }

        private DeucarianThemeStyle CreateFrostedStyle()
        {
            DeucarianThemeStyle style = CreateAsset<DeucarianThemeStyle>();
            style.Configure(
                DeucarianThemeStyleIds.FrostedGlass,
                "Frosted Glass",
                "Test frosted glass style.",
                DeucarianThemeStyleSurfaceTreatment.FrostedGlass,
                new Color(0.78f, 0.9f, 1f, 1f),
                new Color(0.86f, 0.94f, 1f, 1f),
                0.24f,
                0.62f,
                0.48f,
                0.68f,
                Color.white,
                0.58f,
                0.48f,
                1f,
                16f,
                true,
                new Color(1f, 1f, 1f, 0.08f),
                32);
            return style;
        }

        private static float AverageNeighborDelta(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;
            float total = 0f;
            int count = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int left = pixels[y * width + x].r;
                    int right = pixels[y * width + x + 1].r;
                    total += Mathf.Abs(left - right);
                    count++;
                }
            }

            return count == 0 ? 0f : total / count;
        }

        private DeucarianColorRole CreateRole(string id, Color defaultColor)
        {
            DeucarianColorRole role = CreateAsset<DeucarianColorRole>();
            role.Configure(id, id, "Tests", string.Empty, defaultColor, false);
            return role;
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

        private static void StartProviderAssetChangeTracking(DeucarianThemeProvider provider)
        {
            InvokeProviderLifecycle(provider, "OnDisable");
            InvokeProviderLifecycle(provider, "OnEnable");
        }

        private static void StopProviderAssetChangeTracking(DeucarianThemeProvider provider)
        {
            InvokeProviderLifecycle(provider, "OnDisable");
        }

        private static void InvokeProviderLifecycle(DeucarianThemeProvider provider, string methodName)
        {
            MethodInfo method = typeof(DeucarianThemeProvider).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, methodName);
            method.Invoke(provider, null);
        }

        private sealed class StyleTargetProbe : MonoBehaviour, IDeucarianThemeStyleTarget
        {
            public DeucarianThemeStyle AppliedStyle { get; private set; }
            public int ApplyCount { get; private set; }

            public void ApplyStyle(DeucarianThemeStyle style)
            {
                AppliedStyle = style;
                ApplyCount++;
            }
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
    }
}
