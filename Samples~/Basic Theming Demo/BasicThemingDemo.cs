using UnityEngine;

namespace Deucarian.Theming.Samples
{
    /// <summary>Inspector-wired example: the provider owns appearance; buttons only request a mode.</summary>
    public sealed class BasicThemingDemo : MonoBehaviour
    {
        [SerializeField] private DeucarianThemeProvider provider;
        [SerializeField] private DeucarianThemeFamily family;
        [SerializeField] private DeucarianThemeMode initialMode = DeucarianThemeMode.Dark;

        private void Start()
        {
            if (provider == null) provider = GetComponent<DeucarianThemeProvider>();
            if (family == null) family = DeucarianVisualDefaults.LoadFamily();
            if (provider != null) provider.SetThemeFamily(family, initialMode);
        }

        public void UseLight() => provider?.SetThemeMode(DeucarianThemeMode.Light);
        public void UseDark() => provider?.SetThemeMode(DeucarianThemeMode.Dark);
    }
}
