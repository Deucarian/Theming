using UnityEditor;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// High-level Deucarian menu entries for theme setup and discovery.
    /// </summary>
    public static class DeucarianThemingMenu
    {
        private const string ToolsMenuRoot = "Tools/Deucarian/Theming/";
        private const string DeucarianMenuRoot = "Deucarian/Theming/";

        [MenuItem(ToolsMenuRoot + "Open Theme Manager", priority = 100)]
        [MenuItem(DeucarianMenuRoot + "Open Theme Manager", priority = 100)]
        public static void OpenThemeManager()
        {
            DeucarianThemeManagerWindow.OpenWindow();
        }

        [MenuItem(ToolsMenuRoot + "Create Minimal Palette", priority = 105)]
        [MenuItem(DeucarianMenuRoot + "Create Minimal Palette", priority = 105)]
        public static void CreateMinimalPalette()
        {
            DeucarianThemingMenuActions.CreateMinimalPaletteFromSavePanel();
        }

        [MenuItem(ToolsMenuRoot + "Create Missing Default Theme Assets", priority = 110)]
        [MenuItem(DeucarianMenuRoot + "Create Missing Default Theme Assets", priority = 110)]
        public static void CreateMissingDefaultThemeAssets()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets();
            DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
        }

        [MenuItem(ToolsMenuRoot + "Create Game Theme Assets", priority = 111)]
        [MenuItem(DeucarianMenuRoot + "Create Game Theme Assets", priority = 111)]
        public static void CreateGameThemeAssets()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets();
            DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
        }

        [MenuItem(ToolsMenuRoot + "Create Palette From Active Theme", priority = 112)]
        [MenuItem(DeucarianMenuRoot + "Create Palette From Active Theme", priority = 112)]
        public static void CreatePaletteFromActiveTheme()
        {
            DeucarianThemingMenuActions.CreatePaletteFromActiveThemeFromSavePanel();
        }

        [MenuItem(ToolsMenuRoot + "Repair Palette Setup", priority = 113)]
        [MenuItem(DeucarianMenuRoot + "Repair Palette Setup", priority = 113)]
        public static void RepairPaletteSetup()
        {
            DeucarianThemingMenuActions.RepairActivePaletteSetup();
        }

        [MenuItem(ToolsMenuRoot + "Repair Generated Asset Names", priority = 114)]
        [MenuItem(DeucarianMenuRoot + "Repair Generated Asset Names", priority = 114)]
        public static void RepairGeneratedAssetNames()
        {
            DeucarianThemingMenuActions.RepairGeneratedAssetNames();
        }

        [MenuItem(ToolsMenuRoot + "Select Active Theme", priority = 120)]
        [MenuItem(DeucarianMenuRoot + "Select Active Theme", priority = 120)]
        public static void SelectActiveTheme()
        {
            DeucarianThemingMenuActions.SelectActiveTheme();
        }

        [MenuItem(ToolsMenuRoot + "Select Active Palette", priority = 121)]
        [MenuItem(DeucarianMenuRoot + "Select Active Palette", priority = 121)]
        public static void SelectActivePalette()
        {
            DeucarianThemingMenuActions.SelectActivePalette();
        }

        [MenuItem(ToolsMenuRoot + "Select Role Library", priority = 122)]
        [MenuItem(DeucarianMenuRoot + "Select Role Library", priority = 122)]
        public static void SelectRoleLibrary()
        {
            DeucarianThemingMenuActions.SelectActiveRoleLibrary();
        }

        [MenuItem(ToolsMenuRoot + "Open Theme Assets Folder", priority = 130)]
        [MenuItem(DeucarianMenuRoot + "Open Theme Assets Folder", priority = 130)]
        public static void OpenThemeAssetsFolder()
        {
            DeucarianThemingMenuActions.OpenThemeAssetsFolder();
        }

        [MenuItem(ToolsMenuRoot + "Apply Active Theme To Open Scene", priority = 140)]
        [MenuItem(DeucarianMenuRoot + "Apply Active Theme To Open Scene", priority = 140)]
        public static void ApplyActiveThemeToOpenScene()
        {
            DeucarianThemingMenuActions.ApplyActiveThemeToOpenScene();
        }
    }
}
