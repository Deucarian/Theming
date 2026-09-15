using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianForegroundContrastTests
    {
        [Test]
        public void PaletteContrastUsesExactTintedColoursInBothDirections()
        {
            var dark = new Color(0.20f, 0.27f, 0.32f);
            var light = new Color(0.91f, 0.94f, 0.96f);
            var palette = new DeucarianForegroundPalette(light, dark);
            Assert.That(DeucarianForegroundContrast.Resolve(light, light, palette), Is.EqualTo(dark));
            Assert.That(DeucarianForegroundContrast.Resolve(dark, dark, palette), Is.EqualTo(light));
            Assert.That(DeucarianForegroundContrast.Resolve(light, dark, palette), Is.EqualTo(light));
        }

        [Test]
        public void LimitedPaletteKeepsItsBestAuthoredColourInsteadOfInventingBlackOrWhite()
        {
            var dark = new Color(0.3f, 0.4f, 0.5f);
            var light = new Color(0.6f, 0.7f, 0.8f);
            var surface = new Color(0.5f, 0.5f, 0.5f);
            var actual = DeucarianForegroundContrast.Resolve(surface, surface,
                new DeucarianForegroundPalette(dark, light));
            Assert.That(actual, Is.EqualTo(dark).Or.EqualTo(light));
            Assert.That(DeucarianForegroundContrast.Ratio(actual, surface), Is.EqualTo(Mathf.Max(
                DeucarianForegroundContrast.Ratio(dark, surface),
                DeucarianForegroundContrast.Ratio(light, surface))));
        }

        [Test]
        public void OptionalForegroundOverrideLivesInTheExistingPalette()
        {
            var role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            var colours = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            var theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            try
            {
                var authored = new Color(0.1f, 0.2f, 0.15f);
                role.Configure(DeucarianControlColorRoleIds.ForegroundDark, "Dark foreground",
                    DeucarianColorRoleCategories.UiState, "", authored, false);
                colours.SetColor(role, authored);
                theme.Configure("test.foreground", "Foreground", colours);
                var palette = DeucarianForegroundPalette.FromTheme(theme, Color.gray, Color.white);
                Assert.That(palette.Dark, Is.EqualTo(authored));
                Assert.That(palette.Light, Is.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(theme);
                Object.DestroyImmediate(colours);
                Object.DestroyImmediate(role);
            }
        }

        [Test]
        public void BlackAndWhiteHaveExpectedContrastAndAlphaIsComposited()
        {
            Assert.That(DeucarianForegroundContrast.Ratio(Color.white, Color.black), Is.EqualTo(21f).Within(0.001f));
            Assert.That(DeucarianForegroundContrast.Ratio(new Color(1, 1, 1, 0), Color.black), Is.EqualTo(1f));
            Assert.That(DeucarianForegroundContrast.Composite(new Color(1, 1, 1, 0.5f), Color.black),
                Is.EqualTo(new Color(0.5f, 0.5f, 0.5f, 1)));
        }

        [Test]
        public void ReadableThemeColourIsPreservedAndFallbackWorksAcrossRgbCube()
        {
            Color preferred = new Color(0.9f, 0.8f, 1f);
            Assert.That(DeucarianForegroundContrast.Resolve(preferred, Color.black), Is.EqualTo(preferred));
            for (int r = 0; r <= 8; r++)
            for (int g = 0; g <= 8; g++)
            for (int b = 0; b <= 8; b++)
            {
                var background = new Color(r / 8f, g / 8f, b / 8f);
                var text = DeucarianForegroundContrast.Resolve(preferred, background);
                Assert.That(DeucarianForegroundContrast.Ratio(text, background), Is.GreaterThanOrEqualTo(4.5f));
            }
        }
    }
}
