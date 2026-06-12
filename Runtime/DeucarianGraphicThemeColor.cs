using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Theming
{
    /// <summary>
    /// Applies a theme color role to a uGUI Graphic component.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class DeucarianGraphicThemeColor : DeucarianThemeColorTarget
    {
        private Graphic target;

        protected override void CacheTarget()
        {
            if (target == null)
            {
                target = GetComponent<Graphic>();
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
