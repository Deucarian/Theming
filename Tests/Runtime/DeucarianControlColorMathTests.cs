using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianControlColorMathTests
    {
        [Test]
        public void TintPreservesExactLegacyClampingAndAlpha()
        {
            var random = new System.Random(417);
            for (int i = 0; i < 1000; i++)
            {
                var color = new Color((float)random.NextDouble() * 3 - 1, (float)random.NextDouble(),
                    (float)random.NextDouble() * 3, (float)random.NextDouble());
                float factor = (float)random.NextDouble() * 4 - 1;
                var expected = new Color(Mathf.Clamp01(color.r * factor), Mathf.Clamp01(color.g * factor), Mathf.Clamp01(color.b * factor), color.a);
                Assert.That(DeucarianControlColorMath.Tint(color, factor), Is.EqualTo(expected));
            }
        }

        [Test]
        public void MultiplyRetainsHdrValuesAndMultipliesAlpha()
        {
            Assert.That(DeucarianControlColorMath.Multiply(new Color(2, .5f, 1, .5f), new Color(2, 2, .25f, .5f)),
                Is.EqualTo(new Color(4, 1, .25f, .25f)));
        }
    }
}
