using System.Collections.Generic;
using System.Linq;
using Deucarian.Theming.UIToolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianUIToolkitThemeUtilityTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

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
        public void BindingResolvesRootWhenNoSelectorIsProvided()
        {
            VisualElement root = new VisualElement();
            DeucarianUIToolkitThemeBinding binding = new DeucarianUIToolkitThemeBinding();

            List<VisualElement> matches = DeucarianUIToolkitThemeUtility.ResolveElements(root, binding).ToList();

            Assert.AreEqual(1, matches.Count);
            Assert.AreSame(root, matches[0]);
        }

        [Test]
        public void BindingResolvesByElementName()
        {
            VisualElement root = CreateTree(out VisualElement named, out _, out _);
            DeucarianUIToolkitThemeBinding binding = new DeucarianUIToolkitThemeBinding();
            binding.Configure(null, DeucarianUIToolkitStyleProperty.BackgroundColor, name: "viewer-title");

            List<VisualElement> matches = DeucarianUIToolkitThemeUtility.ResolveElements(root, binding).ToList();

            Assert.Contains(named, matches);
        }

        [Test]
        public void BindingResolvesByClass()
        {
            VisualElement root = CreateTree(out _, out VisualElement classed, out _);
            DeucarianUIToolkitThemeBinding binding = new DeucarianUIToolkitThemeBinding();
            binding.Configure(null, DeucarianUIToolkitStyleProperty.BackgroundColor, className: "viewer-panel");

            List<VisualElement> matches = DeucarianUIToolkitThemeUtility.ResolveElements(root, binding).ToList();

            Assert.Contains(classed, matches);
        }

        [Test]
        public void BindingResolvesByUssSelector()
        {
            VisualElement root = CreateTree(out _, out _, out VisualElement button);
            DeucarianUIToolkitThemeBinding binding = new DeucarianUIToolkitThemeBinding();
            binding.Configure(null, DeucarianUIToolkitStyleProperty.BackgroundColor, selector: ".viewer-button");

            List<VisualElement> matches = DeucarianUIToolkitThemeUtility.ResolveElements(root, binding).ToList();

            Assert.Contains(button, matches);
        }

        [Test]
        public void BackgroundColorIsApplied()
        {
            VisualElement element = new VisualElement();

            DeucarianUIToolkitThemeUtility.ApplyColor(
                element,
                DeucarianUIToolkitStyleProperty.BackgroundColor,
                Color.red,
                string.Empty);

            Assert.AreEqual(Color.red, element.style.backgroundColor.value);
        }

        [Test]
        public void TextColorIsApplied()
        {
            VisualElement element = new VisualElement();

            DeucarianUIToolkitThemeUtility.ApplyColor(
                element,
                DeucarianUIToolkitStyleProperty.TextColor,
                Color.green,
                string.Empty);

            Assert.AreEqual(Color.green, element.style.color.value);
        }

        [Test]
        public void BorderColorAppliesAllSides()
        {
            VisualElement element = new VisualElement();

            DeucarianUIToolkitThemeUtility.ApplyColor(
                element,
                DeucarianUIToolkitStyleProperty.BorderColor,
                Color.blue,
                string.Empty);

            Assert.AreEqual(Color.blue, element.style.borderTopColor.value);
            Assert.AreEqual(Color.blue, element.style.borderRightColor.value);
            Assert.AreEqual(Color.blue, element.style.borderBottomColor.value);
            Assert.AreEqual(Color.blue, element.style.borderLeftColor.value);
        }

        [Test]
        public void ThemeReapplyUpdatesUIToolkitElementAfterProviderThemeChange()
        {
            DeucarianTheme firstTheme = CreateTheme(Color.red, out DeucarianColorRole role);
            DeucarianTheme secondTheme = CreateThemeForExistingRole(role, Color.yellow);
            GameObject providerObject = CreateGameObject("Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            GameObject targetObject = CreateGameObject("Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            targetObject.SetActive(false);
            TestUIToolkitThemeTarget target = targetObject.AddComponent<TestUIToolkitThemeTarget>();
            VisualElement element = new VisualElement();
            target.Configure(element, role);

            provider.SetTheme(firstTheme);
            targetObject.SetActive(true);
            Assert.AreEqual(Color.red, element.style.backgroundColor.value);

            provider.SetTheme(secondTheme);
            Assert.AreEqual(Color.yellow, element.style.backgroundColor.value);
        }

        [Test]
        public void MissingDocumentDoesNotThrow()
        {
            LogAssert.ignoreFailingMessages = true;
            DeucarianTheme theme = CreateTheme(Color.red, out _);
            GameObject gameObject = CreateGameObject("Applier");
            DeucarianUIToolkitThemeApplier applier = gameObject.AddComponent<DeucarianUIToolkitThemeApplier>();

            Assert.DoesNotThrow(() => applier.ApplyTheme(theme));
        }

        [Test]
        public void MissingRoleDoesNotThrow()
        {
            DeucarianTheme theme = CreateTheme(Color.red, out _);

            Assert.DoesNotThrow(() => DeucarianUIToolkitThemeUtility.GetColorOrFallback(theme, null));
        }

        [Test]
        public void SafeVariableNameGenerationWorks()
        {
            Assert.AreEqual("reportviewer-text-primary", DeucarianUIToolkitThemeUtility.ToSafeVariableName("ReportViewer.Text Primary"));
            Assert.AreEqual("unnamed", DeucarianUIToolkitThemeUtility.ToSafeVariableName("   "));
        }

        private static VisualElement CreateTree(out VisualElement named, out VisualElement classed, out VisualElement button)
        {
            VisualElement root = new VisualElement();
            classed = new VisualElement();
            classed.AddToClassList("viewer-panel");

            named = new Label();
            named.name = "viewer-title";
            named.AddToClassList("viewer-title");

            button = new Button();
            button.name = "viewer-button";
            button.AddToClassList("viewer-button");

            root.Add(classed);
            classed.Add(named);
            classed.Add(button);
            return root;
        }

        private DeucarianTheme CreateTheme(Color color, out DeucarianColorRole role)
        {
            role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            role.Configure("deucarian.test.uitoolkit", "UI Toolkit", DeucarianColorRoleCategories.UiState, string.Empty, color, false);
            createdObjects.Add(role);
            return CreateThemeForExistingRole(role, color);
        }

        private DeucarianTheme CreateThemeForExistingRole(DeucarianColorRole role, Color color)
        {
            DeucarianColorPalette palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            palette.SetColor(role, color);
            createdObjects.Add(palette);

            DeucarianTheme theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            theme.Configure("deucarian.test.theme." + color, "Test Theme", palette);
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

    public sealed class TestUIToolkitThemeTarget : DeucarianThemeTargetBehaviour
    {
        private VisualElement element;
        private DeucarianColorRole role;

        public void Configure(VisualElement targetElement, DeucarianColorRole targetRole)
        {
            element = targetElement;
            role = targetRole;
        }

        protected override void ApplyResolvedTheme(DeucarianTheme theme)
        {
            DeucarianUIToolkitThemeUtility.ApplyColor(
                element,
                DeucarianUIToolkitStyleProperty.BackgroundColor,
                theme.GetColor(role),
                string.Empty);
        }
    }
}
