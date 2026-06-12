using TMPro;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Applies a theme color role to a TMP text component.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class DeucarianTMPThemeColor : DeucarianThemeColorTarget
    {
        private TMP_Text target;

        protected override void CacheTarget()
        {
            if (target == null)
            {
                target = GetComponent<TMP_Text>();
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
