using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Reusable border treatment for panel-like themed surfaces.</summary>
    [CreateAssetMenu(fileName = "Theme Stroke Profile", menuName = "Deucarian/Theming/Theme Stroke Profile")]
    public sealed class DeucarianThemeStrokeProfile : ScriptableObject
    {
        [SerializeField] private string profileId = DeucarianThemePresentationProfileIds.Stroke.Frosted;
        [SerializeField] private string displayName = "Frosted Highlight";
        [SerializeField] private string description = "Soft highlighted glass border.";
        [SerializeField] private Color borderTint = Color.white;
        [SerializeField, Range(0f, 1f)] private float borderTintStrength = 0.58f;
        [SerializeField, Range(0f, 1f)] private float borderAlpha = 0.48f;
        [SerializeField, Min(0f)] private float borderWidth = 1f;

        public string ProfileId => profileId;
        public string DisplayName => displayName;
        public string Description => description;
        public Color BorderTint => borderTint;
        public float BorderTintStrength => borderTintStrength;
        public float BorderAlpha => borderAlpha;
        public float BorderWidth => borderWidth;

        public void Configure(
            string id,
            string name,
            string profileDescription,
            Color tint,
            float tintStrength,
            float alpha,
            float width)
        {
            profileId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            description = profileDescription ?? string.Empty;
            borderTint = tint;
            borderTintStrength = Mathf.Clamp01(tintStrength);
            borderAlpha = Mathf.Clamp01(alpha);
            borderWidth = Mathf.Max(0f, width);
            NotifyChanged();
        }

        public Color ResolveBorderColor(Color surfaceColor)
        {
            Color border = Color.Lerp(surfaceColor, borderTint, borderTintStrength);
            border.a = borderAlpha;
            return border;
        }

        private void OnValidate()
        {
            profileId = DeucarianColorRole.NormalizeId(profileId);
            displayName = displayName ?? string.Empty;
            description = description ?? string.Empty;
            borderTintStrength = Mathf.Clamp01(borderTintStrength);
            borderAlpha = Mathf.Clamp01(borderAlpha);
            borderWidth = Mathf.Max(0f, borderWidth);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
