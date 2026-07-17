using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianUGUIAndTMPAdapterTests
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
        public void GraphicAdapterAppliesColor()
        {
            DeucarianTheme theme = CreateTheme(Color.red, out DeucarianColorRole role);
            GameObject gameObject = CreateGameObject("Graphic");
            gameObject.SetActive(false);
            Image image = gameObject.AddComponent<Image>();
            DeucarianGraphicThemeColor adapter = gameObject.AddComponent<DeucarianGraphicThemeColor>();

            adapter.ColorRole = role;
            adapter.ApplyTheme(theme);

            Assert.AreEqual(Color.red, image.color);
        }

        [Test]
        public void TMPAdapterAppliesColor()
        {
            DeucarianTheme theme = CreateTheme(Color.green, out DeucarianColorRole role);
            GameObject gameObject = CreateGameObject("TMP");
            gameObject.SetActive(false);
            TMP_Text text = gameObject.AddComponent<TextMeshProUGUI>();
            DeucarianTMPThemeColor adapter = gameObject.AddComponent<DeucarianTMPThemeColor>();

            adapter.ColorRole = role;
            adapter.ApplyTheme(theme);

            Assert.AreEqual(Color.green, text.color);
        }

        [Test]
        public void TMPTypographyAdapterAppliesSelectedRoleMetrics()
        {
            DeucarianThemeTypographyProfile typography = ScriptableObject.CreateInstance<DeucarianThemeTypographyProfile>();
            typography.Configure(
                null,
                new DeucarianThemeTextStyle(24f, FontStyles.Bold, 2f, 3f),
                new DeucarianThemeTextStyle(15f),
                new DeucarianThemeTextStyle(10f));
            createdObjects.Add(typography);

            DeucarianThemeStyle style = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            style.SetComposition(null, null, null, DeucarianThemeDensity.Standard, typography);
            createdObjects.Add(style);

            GameObject gameObject = CreateGameObject("TMP Typography");
            gameObject.SetActive(false);
            TMP_Text text = gameObject.AddComponent<TextMeshProUGUI>();
            DeucarianTMPThemeTypography adapter = gameObject.AddComponent<DeucarianTMPThemeTypography>();
            adapter.TextRole = DeucarianThemeTextRole.Title;

            adapter.ApplyStyle(style);

            Assert.AreEqual(24f, text.fontSize);
            Assert.AreEqual(FontStyles.Bold, text.fontStyle);
            Assert.AreEqual(2f, text.characterSpacing);
            Assert.AreEqual(3f, text.lineSpacing);
        }

        [Test]
        public void TMPTypographyAdapterUsesDefaultRoleWhenProfileIsNull()
        {
            DeucarianThemeStyle style = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            createdObjects.Add(style);
            GameObject gameObject = CreateGameObject("TMP Typography Default");
            gameObject.SetActive(false);
            TMP_Text text = gameObject.AddComponent<TextMeshProUGUI>();
            DeucarianTMPThemeTypography adapter = gameObject.AddComponent<DeucarianTMPThemeTypography>();
            adapter.TextRole = DeucarianThemeTextRole.Caption;

            adapter.ApplyStyle(style);

            Assert.AreEqual(11f, text.fontSize);
        }

        private DeucarianTheme CreateTheme(Color color, out DeucarianColorRole role)
        {
            role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            role.Configure("deucarian.test.adapter", "Adapter", DeucarianColorRoleCategories.UiState, string.Empty, color, false);
            createdObjects.Add(role);

            DeucarianColorPalette palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            palette.SetColor(role, color);
            createdObjects.Add(palette);

            DeucarianTheme theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            theme.Configure("deucarian.test.theme", "Test Theme", palette);
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
