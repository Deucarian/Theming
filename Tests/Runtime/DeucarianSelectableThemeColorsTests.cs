using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianSelectableThemeColorsTests
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
        public void SelectableThemeColorsPreservesMultiplierAndFadeDuration()
        {
            DeucarianSelectableThemeColors target = CreateSelectableTarget(out Button button);
            DeucarianTheme theme = CreateTheme(
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow,
                Color.gray,
                out DeucarianColorRole normal,
                out DeucarianColorRole highlighted,
                out DeucarianColorRole pressed,
                out DeucarianColorRole selected,
                out DeucarianColorRole disabled);

            ColorBlock colors = button.colors;
            colors.colorMultiplier = 2.25f;
            colors.fadeDuration = 0.42f;
            button.colors = colors;

            AssignRoles(target, normal, highlighted, pressed, selected, disabled);
            target.ApplyTheme(theme);

            Assert.AreEqual(2.25f, button.colors.colorMultiplier);
            Assert.AreEqual(0.42f, button.colors.fadeDuration);
        }

        [Test]
        public void SelectableThemeColorsAppliesAllSelectableStates()
        {
            DeucarianSelectableThemeColors target = CreateSelectableTarget(out Button button);
            DeucarianTheme theme = CreateTheme(
                Color.red,
                Color.green,
                Color.blue,
                Color.yellow,
                Color.gray,
                out DeucarianColorRole normal,
                out DeucarianColorRole highlighted,
                out DeucarianColorRole pressed,
                out DeucarianColorRole selected,
                out DeucarianColorRole disabled);

            AssignRoles(target, normal, highlighted, pressed, selected, disabled);
            target.ApplyTheme(theme);

            ColorBlock colors = button.colors;
            Assert.AreEqual(Color.red, colors.normalColor);
            Assert.AreEqual(Color.green, colors.highlightedColor);
            Assert.AreEqual(Color.blue, colors.pressedColor);
            Assert.AreEqual(Color.yellow, colors.selectedColor);
            Assert.AreEqual(Color.gray, colors.disabledColor);
        }

        [Test]
        public void ThemeTargetsReapplyWhenProviderThemeChanges()
        {
            DeucarianTheme firstTheme = CreateSingleRoleTheme(Color.red, out DeucarianColorRole role);
            DeucarianTheme secondTheme = CreateSingleRoleThemeWithExistingRole(role, Color.blue);
            GameObject providerObject = CreateGameObject("Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();

            GameObject targetObject = CreateGameObject("Target");
            targetObject.SetActive(false);
            Button button = targetObject.AddComponent<Button>();
            DeucarianSelectableThemeColors target = targetObject.AddComponent<DeucarianSelectableThemeColors>();
            AssignRoles(target, role, role, role, role, role);

            provider.SetTheme(firstTheme);
            targetObject.SetActive(true);
            Assert.AreEqual(Color.red, button.colors.normalColor);

            provider.SetTheme(secondTheme);
            Assert.AreEqual(Color.blue, button.colors.normalColor);
        }

        [Test]
        public void ChildApplicationAppliesThemeToAllChildTargets()
        {
            DeucarianTheme theme = CreateSingleRoleTheme(Color.cyan, out DeucarianColorRole role);
            GameObject providerObject = CreateGameObject("Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();

            DeucarianSelectableThemeColors first = CreateSelectableTarget(out Button firstButton, providerObject.transform);
            DeucarianSelectableThemeColors second = CreateSelectableTarget(out Button secondButton, providerObject.transform);
            first.ApplyOnEnable = false;
            second.ApplyOnEnable = false;
            AssignRoles(first, role, role, role, role, role);
            AssignRoles(second, role, role, role, role, role);

            provider.SetTheme(theme);
            provider.ApplyThemeToChildren();

            Assert.AreEqual(Color.cyan, firstButton.colors.normalColor);
            Assert.AreEqual(Color.cyan, secondButton.colors.normalColor);
        }

        private DeucarianSelectableThemeColors CreateSelectableTarget(out Button button, Transform parent = null)
        {
            GameObject gameObject = CreateGameObject("Selectable", parent);
            gameObject.SetActive(false);
            button = gameObject.AddComponent<Button>();
            DeucarianSelectableThemeColors target = gameObject.AddComponent<DeucarianSelectableThemeColors>();
            return target;
        }

        private void AssignRoles(
            DeucarianSelectableThemeColors target,
            DeucarianColorRole normal,
            DeucarianColorRole highlighted,
            DeucarianColorRole pressed,
            DeucarianColorRole selected,
            DeucarianColorRole disabled)
        {
            target.NormalRole = normal;
            target.HighlightedRole = highlighted;
            target.PressedRole = pressed;
            target.SelectedRole = selected;
            target.DisabledRole = disabled;
        }

        private DeucarianTheme CreateTheme(
            Color normalColor,
            Color highlightedColor,
            Color pressedColor,
            Color selectedColor,
            Color disabledColor,
            out DeucarianColorRole normal,
            out DeucarianColorRole highlighted,
            out DeucarianColorRole pressed,
            out DeucarianColorRole selected,
            out DeucarianColorRole disabled)
        {
            normal = CreateRole("deucarian.test.normal", normalColor);
            highlighted = CreateRole("deucarian.test.highlighted", highlightedColor);
            pressed = CreateRole("deucarian.test.pressed", pressedColor);
            selected = CreateRole("deucarian.test.selected", selectedColor);
            disabled = CreateRole("deucarian.test.disabled", disabledColor);

            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            palette.SetColor(normal, normalColor);
            palette.SetColor(highlighted, highlightedColor);
            palette.SetColor(pressed, pressedColor);
            palette.SetColor(selected, selectedColor);
            palette.SetColor(disabled, disabledColor);

            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            theme.Configure("deucarian.test.theme", "Test Theme", palette);
            return theme;
        }

        private DeucarianTheme CreateSingleRoleTheme(Color color, out DeucarianColorRole role)
        {
            role = CreateRole("deucarian.test.shared", color);
            return CreateSingleRoleThemeWithExistingRole(role, color);
        }

        private DeucarianTheme CreateSingleRoleThemeWithExistingRole(DeucarianColorRole role, Color color)
        {
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            palette.SetColor(role, color);

            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            theme.Configure("deucarian.test.theme." + color.ToString(), "Test Theme", palette);
            return theme;
        }

        private DeucarianColorRole CreateRole(string id, Color defaultColor)
        {
            DeucarianColorRole role = CreateAsset<DeucarianColorRole>();
            role.name = id;
            role.Configure(id, id, DeucarianColorRoleCategories.UiState, string.Empty, defaultColor, false);
            return role;
        }

        private T CreateAsset<T>()
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }

        private GameObject CreateGameObject(string name, Transform parent = null)
        {
            GameObject gameObject = new GameObject(name);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
