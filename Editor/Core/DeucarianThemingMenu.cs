using UnityEditor;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// High-level Deucarian menu entries for theme setup and discovery.
    /// </summary>
    public static class DeucarianThemingMenu
    {
        private const string MenuRoot = "Deucarian/Theming/";

        [MenuItem(MenuRoot + "Open Theme Manager", priority = 100)]
        public static void OpenThemeManager()
        {
            DeucarianThemeManagerWindow.OpenWindow();
        }

        public static void CreateMissingDefaultThemeAssets()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets();
            DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
        }

        public static void SelectActiveTheme()
        {
            DeucarianThemingMenuActions.SelectActiveTheme();
        }

        public static void SelectActivePalette()
        {
            DeucarianThemingMenuActions.SelectActivePalette();
        }

        public static void SelectRoleLibrary()
        {
            DeucarianThemingMenuActions.SelectActiveRoleLibrary();
        }

        [MenuItem(MenuRoot + "Open Theme Assets Folder", priority = 130)]
        public static void OpenThemeAssetsFolder()
        {
            DeucarianThemingMenuActions.OpenThemeAssetsFolder();
        }

        public static void ApplyActiveThemeToOpenScene()
        {
            DeucarianThemingMenuActions.ApplyActiveThemeToOpenScene();
        }
    }
}
