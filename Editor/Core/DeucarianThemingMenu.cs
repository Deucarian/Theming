using UnityEditor;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// High-level Deucarian menu entries for theme setup and discovery.
    /// </summary>
    public static class DeucarianThemingMenu
    {
        private const string MenuRoot = "Tools/Deucarian/Theming/";

        [MenuItem(MenuRoot + "Open Theme Manager", priority = 100)]
        public static void OpenThemeManager()
        {
            DeucarianThemeManagerWindow.OpenWindow();
        }

        [MenuItem(MenuRoot + "Create Missing Default Theme Assets", priority = 110)]
        public static void CreateMissingDefaultThemeAssets()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets();
            DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
        }

        [MenuItem(MenuRoot + "Create Game Theme Assets", priority = 111)]
        public static void CreateGameThemeAssets()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets();
            DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
        }

        [MenuItem(MenuRoot + "Select Active Theme", priority = 120)]
        public static void SelectActiveTheme()
        {
            DeucarianThemingMenuActions.SelectActiveTheme();
        }

        [MenuItem(MenuRoot + "Select Active Palette", priority = 121)]
        public static void SelectActivePalette()
        {
            DeucarianThemingMenuActions.SelectActivePalette();
        }

        [MenuItem(MenuRoot + "Select Role Library", priority = 122)]
        public static void SelectRoleLibrary()
        {
            DeucarianThemingMenuActions.SelectActiveRoleLibrary();
        }

        [MenuItem(MenuRoot + "Open Theme Assets Folder", priority = 130)]
        public static void OpenThemeAssetsFolder()
        {
            DeucarianThemingMenuActions.OpenThemeAssetsFolder();
        }

        [MenuItem(MenuRoot + "Apply Active Theme To Open Scene", priority = 140)]
        public static void ApplyActiveThemeToOpenScene()
        {
            DeucarianThemingMenuActions.ApplyActiveThemeToOpenScene();
        }
    }
}
