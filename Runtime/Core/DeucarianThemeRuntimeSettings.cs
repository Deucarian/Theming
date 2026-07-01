using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Runtime Resources asset that points builds at the project default theme.
    /// </summary>
    [CreateAssetMenu(
        fileName = ResourceName,
        menuName = "Deucarian/Theming/Runtime Settings")]
    public sealed class DeucarianThemeRuntimeSettings : ScriptableObject
    {
        public const string ResourceName = "DeucarianThemeRuntimeSettings";

        [SerializeField] private DeucarianTheme defaultTheme;

        /// <summary>Theme used when a runtime provider or target needs a project default.</summary>
        public DeucarianTheme DefaultTheme => defaultTheme;

        /// <summary>Configures the runtime default theme.</summary>
        public void Configure(DeucarianTheme theme)
        {
            defaultTheme = theme;
            NotifyChanged();
        }

        private void OnValidate()
        {
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
