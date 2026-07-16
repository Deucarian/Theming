using UnityEditor;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// High-level Deucarian menu entries for theme setup and discovery.
    /// </summary>
    public static class DeucarianThemingMenu
    {
        private const string ToolsMenuRoot = "Tools/Deucarian/Theming/";

        [MenuItem(ToolsMenuRoot + "Open Theme Manager", priority = 100)]
        public static void OpenThemeManager()
        {
            DeucarianThemeManagerWindow.OpenWindow();
        }

        [MenuItem(ToolsMenuRoot + "Create Theme Family", priority = 105)]
        public static void CreateThemeFamily()
        {
            DeucarianThemingMenuActions.CreateThemeFamilyFromSavePanel();
        }

        public static void CreateMinimalPalette()
        {
            DeucarianThemingMenuActions.CreateMinimalPaletteFromSavePanel();
        }

        public static void CreateMissingDefaultThemeAssets()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets();
            if (assets != null)
            {
                DeucarianThemingMenuActions.SelectAndPing(assets.ThemeFamily);
            }
        }

        public static void CreateGameThemeAssets()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets();
            if (assets != null)
            {
                DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
            }
        }

        public static void CreatePaletteFromActiveTheme()
        {
            DeucarianThemingMenuActions.CreatePaletteFromActiveThemeFromSavePanel();
        }

        public static void RepairPaletteSetup()
        {
            DeucarianThemingMenuActions.RepairActivePaletteSetup();
        }

        public static void RepairGeneratedAssetNames()
        {
            DeucarianThemingMenuActions.RepairGeneratedAssetNames();
        }

        public static void CreateBuiltinThemeStyleAssets()
        {
            DeucarianThemingMenuActions.CreateBuiltinThemeStyleAssets();
        }

        public static void AssignActiveStyleToActiveTheme()
        {
            DeucarianThemingMenuActions.AssignActiveStyleToActiveTheme();
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

        public static void SelectActiveStyle()
        {
            DeucarianThemingMenuActions.SelectActiveStyle();
        }

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
