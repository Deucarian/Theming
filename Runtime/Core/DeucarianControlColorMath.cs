using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Control color composition shared by runtime UI adapters; never changes alpha during tinting.</summary>
    public static class DeucarianControlColorMath
    {
        public static Color Tint(Color color, float factor) => new Color(
            Mathf.Clamp01(color.r * factor), Mathf.Clamp01(color.g * factor), Mathf.Clamp01(color.b * factor), color.a);

        public static Color Multiply(Color color, Color multiplier) => new Color(
            color.r * multiplier.r, color.g * multiplier.g, color.b * multiplier.b, color.a * multiplier.a);
    }
}
