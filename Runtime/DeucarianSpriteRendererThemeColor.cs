using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Applies a theme color role to a SpriteRenderer component.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DeucarianSpriteRendererThemeColor : DeucarianThemeColorTarget
    {
        private SpriteRenderer target;

        protected override void CacheTarget()
        {
            if (target == null)
            {
                target = GetComponent<SpriteRenderer>();
            }
        }

        protected override void ApplyColor(Color color)
        {
            if (target == null)
            {
                return;
            }

            target.color = color;
        }

        private void Reset()
        {
            CacheTarget();
        }
    }
}
