using TMPro;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Applies a semantic typography role from a Deucarian visual style to TMP text.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class DeucarianTMPThemeTypography : MonoBehaviour, IDeucarianThemeStyleTarget
    {
        [SerializeField] private DeucarianThemeTextRole textRole = DeucarianThemeTextRole.Body;
        [SerializeField, Min(0)] private float minimumFontSize;

        private TMP_Text target;

        public DeucarianThemeTextRole TextRole
        {
            get => textRole;
            set => textRole = value;
        }

        /// <summary>Optional readability constraint in the target canvas's units; zero follows the role size exactly.</summary>
        public float MinimumFontSize { get => minimumFontSize; set => minimumFontSize = Mathf.Clamp(value, 0, 512); }

        public void ApplyStyle(DeucarianThemeStyle style)
        {
            if (!DeucarianThemeRuntimeResolver.UseVisualStyling) return;
            CacheTarget();
            if (target == null)
            {
                return;
            }

            DeucarianThemeTypographyProfile typography = style != null
                ? style.TypographyProfile
                : null;
            DeucarianThemeTextStyle textStyle = typography != null
                ? typography.GetStyle(textRole)
                : DeucarianThemeTextStyle.DefaultFor(textRole);
            TMP_FontAsset font = typography != null
                ? typography.ResolvedFontAsset
                : DeucarianThemeTypographyProfile.ProjectDefaultFontAsset;

            if (font != null)
            {
                target.font = font;
            }

            target.fontSize = Mathf.Max(textStyle.FontSize, minimumFontSize);
            target.fontStyle = textStyle.FontStyle;
            target.characterSpacing = textStyle.CharacterSpacing;
            target.lineSpacing = textStyle.LineSpacing;
        }

        private void CacheTarget()
        {
            if (target == null)
            {
                target = GetComponent<TMP_Text>();
            }
        }

        private void Reset()
        {
            CacheTarget();
        }
    }
}
