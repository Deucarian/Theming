using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Reusable corner treatment for panel-like themed surfaces.</summary>
    [CreateAssetMenu(fileName = "Theme Shape Profile", menuName = "Deucarian/Theming/Theme Shape Profile")]
    public sealed class DeucarianThemeShapeProfile : ScriptableObject
    {
        [SerializeField] private string profileId = DeucarianThemePresentationProfileIds.Shape.Rounded;
        [SerializeField] private string displayName = "Rounded";
        [SerializeField] private string description = "Large rounded panel corners.";
        [SerializeField, Min(0f)] private float cornerRadius = 16f;

        public string ProfileId => profileId;
        public string DisplayName => displayName;
        public string Description => description;
        public float CornerRadius => cornerRadius;

        public void Configure(string id, string name, string profileDescription, float radius)
        {
            profileId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            description = profileDescription ?? string.Empty;
            cornerRadius = Mathf.Max(0f, radius);
            NotifyChanged();
        }

        private void OnValidate()
        {
            profileId = DeucarianColorRole.NormalizeId(profileId);
            displayName = displayName ?? string.Empty;
            description = description ?? string.Empty;
            cornerRadius = Mathf.Max(0f, cornerRadius);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
