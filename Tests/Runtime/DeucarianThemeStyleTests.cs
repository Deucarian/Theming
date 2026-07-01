using System.Collections.Generic;
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
    }
}
