using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Samples
{
    public sealed class UIToolkitThemingDemo : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private DeucarianThemeProvider provider;
        [SerializeField] private DeucarianThemeFamily family;
        private Button light;
        private Button dark;

        private void Start()
        {
            provider.SetThemeFamily(family != null ? family : DeucarianVisualDefaults.LoadFamily(), DeucarianThemeMode.Dark);
            light = document.rootVisualElement.Q<Button>("light");
            dark = document.rootVisualElement.Q<Button>("dark");
            if (light != null) light.clicked += UseLight;
            if (dark != null) dark.clicked += UseDark;
        }

        private void UseLight() => provider.SetThemeMode(DeucarianThemeMode.Light);
        private void UseDark() => provider.SetThemeMode(DeucarianThemeMode.Dark);
        private void OnDestroy()
        {
            if (light != null) light.clicked -= UseLight;
            if (dark != null) dark.clicked -= UseDark;
        }
    }
}
