using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianForegroundContrastTests
    {
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
