using System.Collections.Generic;
using Deucarian.Theming.UIToolkit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianUIToolkitThemeTypographyTests
    {
        private const string InterTypographyPath =
            "Packages/com.deucarian.theming/Runtime/Fonts/InterTypography.asset";
        private const string MontserratTypographyPath =
            "Packages/com.deucarian.theming/Runtime/Fonts/MontserratTypography.asset";

        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void ResolveFontReturnsTypographySourceFont()
        {
            DeucarianThemeTypographyProfile typography = LoadTypography(InterTypographyPath);
            DeucarianThemeStyle style = CreateStyle(typography);

            Font font = DeucarianUIToolkitThemeTypography.ResolveFont(style);

            Assert.AreSame(typography.ResolvedFontAsset.sourceFontFile, font);
        }

        [Test]
        public void ApplyStyleAssignsResolvedFont()
        {
            DeucarianThemeTypographyProfile typography = LoadTypography(InterTypographyPath);
            DeucarianThemeStyle style = CreateStyle(typography);
            VisualElement element = new VisualElement();

            bool applied = DeucarianUIToolkitThemeTypography.ApplyStyle(element, style);

            Assert.IsTrue(applied);
            Assert.AreSame(typography.ResolvedFontAsset.sourceFontFile, element.style.unityFont.value);
        }

        [Test]
        public void ApplyPrefersNearbyProviderStyleOverride()
        {
            DeucarianThemeStyle themeStyle = CreateStyle(LoadTypography(InterTypographyPath));
            DeucarianThemeStyle overrideStyle = CreateStyle(LoadTypography(MontserratTypographyPath));
            DeucarianTheme theme = CreateTheme(themeStyle);
            GameObject providerObject = CreateGameObject("Typography Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetTheme(theme);
            provider.SetStyle(overrideStyle);
            GameObject contextObject = CreateGameObject("Typography Context");
            contextObject.transform.SetParent(providerObject.transform, false);
            VisualElement element = new VisualElement();

            bool applied = DeucarianUIToolkitThemeTypography.Apply(
                element,
                theme,
                contextObject.transform);

            Assert.IsTrue(applied);
            Assert.AreSame(
                overrideStyle.TypographyProfile.ResolvedFontAsset.sourceFontFile,
                element.style.unityFont.value);
        }

        [Test]
        public void MissingElementOrTypographyDoesNotApply()
        {
            VisualElement element = new VisualElement();

            Assert.IsFalse(DeucarianUIToolkitThemeTypography.ApplyStyle(null, null));
            Assert.IsFalse(DeucarianUIToolkitThemeTypography.ApplyStyle(element, null));
            Assert.AreEqual(StyleKeyword.Null, element.style.unityFont.keyword);
        }

        private DeucarianThemeTypographyProfile LoadTypography(string path)
        {
            DeucarianThemeTypographyProfile typography =
                AssetDatabase.LoadAssetAtPath<DeucarianThemeTypographyProfile>(path);
            Assert.NotNull(typography, path);
            Assert.NotNull(typography.ResolvedFontAsset, path);
            Assert.NotNull(typography.ResolvedFontAsset.sourceFontFile, path);
            return typography;
        }

        private DeucarianThemeStyle CreateStyle(DeucarianThemeTypographyProfile typography)
        {
            DeucarianThemeStyle style = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            style.SetComposition(
                null,
                null,
                null,
                DeucarianThemeDensity.Standard,
                typography);
            createdObjects.Add(style);
            return style;
        }

        private DeucarianTheme CreateTheme(DeucarianThemeStyle style)
        {
            DeucarianTheme theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            theme.Configure("deucarian.test.uitoolkit-typography", "UI Toolkit Typography", null, style);
            createdObjects.Add(theme);
            return theme;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
