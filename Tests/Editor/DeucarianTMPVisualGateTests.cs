using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianTMPVisualGateTests
    {
        private const string Folder = "Assets/DeucarianTMPVisualGateTests";
        private readonly List<Object> objects = new List<Object>();
        private DeucarianThemeRuntimeSettings settings;

        [SetUp]
        public void Setup()
        {
            Assert.That(DeucarianThemeRuntimeSettingsAssets.FindRuntimeSettingsResourceAssets(), Is.Empty,
                "Run these asset-isolation tests in a consumer without project runtime settings.");
            Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False, "Do not overwrite existing assets.");
            settings = DeucarianThemeRuntimeSettingsAssets.CreateRuntimeSettingsAtPath(
                Folder + "/Resources/DeucarianThemeRuntimeSettings.asset");
            Assert.That(settings, Is.Not.Null);
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            if (settings != null) AssetDatabase.DeleteAsset(Folder);
            settings = null;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VisualOffSkipsExplicitAndDefaultTypography(bool nullStyle)
        {
            var text = Text(GameObject("Text"));
            RestoreAuthoredText(text);
            var originalFont = text.font;
            var adapter = text.GetComponent<DeucarianTMPThemeTypography>();
            var style = Style(out _);
            settings.SetFeatures(false, true);

            adapter.ApplyStyle(nullStyle ? null : style);

            AssertAuthoredText(text);
            Assert.That(text.font, Is.SameAs(originalFont));
        }

        [Test]
        public void AudioOffDoesNotDisableVisualTypography()
        {
            var text = Text(GameObject("Text"));
            var style = Style(out _);
            settings.SetFeatures(true, false);

            text.GetComponent<DeucarianTMPThemeTypography>().ApplyStyle(style);

            Assert.That(text.fontSize, Is.EqualTo(30f));
            Assert.That(text.fontStyle, Is.EqualTo(FontStyles.Bold));
            Assert.That(text.characterSpacing, Is.EqualTo(2f));
            Assert.That(text.lineSpacing, Is.EqualTo(4f));
        }

        [Test]
        public void ProviderRefreshAndTypographyEditsDoNotOverrideRestoredTextWhileVisualsAreOff()
        {
            var parent = GameObject("Provider");
            var text = Text(GameObject("Text"));
            text.transform.SetParent(parent.transform, false);
            var style = Style(out var typography);
            var provider = parent.AddComponent<DeucarianThemeProvider>();
            provider.SetStyle(style);
            parent.SetActive(true);
            provider.RefreshThemeGraph();
            Assert.That(text.fontSize, Is.EqualTo(30f));

            settings.SetFeatures(false, true);
            RestoreAuthoredText(text);
            var originalFont = text.font;
            provider.RefreshThemeGraph();
            AssertAuthoredText(text);
            typography.Configure(null, new DeucarianThemeTextStyle(36f, FontStyles.Italic, 5f, 6f),
                new DeucarianThemeTextStyle(15f), new DeucarianThemeTextStyle(10f));
            AssertAuthoredText(text);
            Assert.That(text.font, Is.SameAs(originalFont));

            settings.SetFeatures(true, true);
            provider.RefreshThemeGraph();
            Assert.That(text.fontSize, Is.EqualTo(36f));
            Assert.That(text.fontStyle, Is.EqualTo(FontStyles.Italic));
            Assert.That(text.characterSpacing, Is.EqualTo(5f));
            Assert.That(text.lineSpacing, Is.EqualTo(6f));
        }

        private TMP_Text Text(GameObject gameObject)
        {
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            var adapter = gameObject.AddComponent<DeucarianTMPThemeTypography>();
            adapter.TextRole = DeucarianThemeTextRole.Title;
            return text;
        }

        private DeucarianThemeStyle Style(out DeucarianThemeTypographyProfile typography)
        {
            typography = Asset<DeucarianThemeTypographyProfile>();
            typography.Configure(null, new DeucarianThemeTextStyle(30f, FontStyles.Bold, 2f, 4f),
                new DeucarianThemeTextStyle(15f), new DeucarianThemeTextStyle(10f));
            var style = Asset<DeucarianThemeStyle>();
            style.SetComposition(null, null, null, DeucarianThemeDensity.Standard, typography);
            return style;
        }

        private static void RestoreAuthoredText(TMP_Text text)
        {
            text.fontSize = 17f;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 1f;
            text.lineSpacing = 3f;
        }

        private static void AssertAuthoredText(TMP_Text text)
        {
            Assert.That(text.fontSize, Is.EqualTo(17f));
            Assert.That(text.fontStyle, Is.EqualTo(FontStyles.Normal));
            Assert.That(text.characterSpacing, Is.EqualTo(1f));
            Assert.That(text.lineSpacing, Is.EqualTo(3f));
        }

        private GameObject GameObject(string name)
        {
            var value = new GameObject(name);
            value.SetActive(false);
            objects.Add(value);
            return value;
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            objects.Add(value);
            return value;
        }
    }
}
