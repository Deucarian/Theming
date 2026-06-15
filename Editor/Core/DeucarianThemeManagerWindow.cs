using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Editor window for discovering, creating, selecting, and applying Deucarian theme assets.
    /// </summary>
    public sealed class DeucarianThemeManagerWindow : EditorWindow
    {
        private DeucarianThemingMenuActions.AssetSearchResult searchResult;
        private Vector2 scrollPosition;

        public static void OpenWindow()
        {
            DeucarianThemeManagerWindow window = GetWindow<DeucarianThemeManagerWindow>("Theme Manager");
            window.minSize = new Vector2(420f, 360f);
            window.RefreshAssets(true);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAssets(false);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DeucarianEditorChrome.DrawPackageHeader(
                "theming",
                "Deucarian Theming",
                "Create, discover, select, and apply runtime theme assets.");

            DrawActiveAssetFields();
            DrawFolderField();
            DrawAssetSummary();
            DrawActionButtons();
            DeucarianEditorChrome.DrawFooterVersion("com.deucarian.theming", "0.3.0");

            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveAssetFields()
        {
            DeucarianEditorChrome.DrawSectionHeader("Active Assets");
            DeucarianEditorChrome.BeginSection();

            DeucarianThemingEditorSettings.ActiveTheme = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Active Theme",
                DeucarianThemingEditorSettings.ActiveTheme,
                "Select",
                theme => DeucarianThemingEditorSettings.ActiveTheme = theme,
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveTheme());

            DeucarianThemingEditorSettings.ActivePalette = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Active Palette",
                DeucarianThemingEditorSettings.ActivePalette,
                "Select",
                palette => DeucarianThemingEditorSettings.ActivePalette = palette,
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActivePalette());

            DeucarianThemingEditorSettings.ActiveRoleLibrary = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Role Library",
                DeucarianThemingEditorSettings.ActiveRoleLibrary,
                "Select",
                roleLibrary => DeucarianThemingEditorSettings.ActiveRoleLibrary = roleLibrary,
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveRoleLibrary());

            DeucarianEditorChrome.EndSection();
        }

        private void DrawFolderField()
        {
            DeucarianEditorChrome.DrawSectionHeader("Default Asset Folder");
            DeucarianEditorChrome.BeginSection();

            EditorGUI.BeginChangeCheck();
            string folder = EditorGUILayout.TextField("Path", DeucarianThemingEditorSettings.DefaultAssetFolder);
            if (EditorGUI.EndChangeCheck())
            {
                DeucarianThemingEditorSettings.DefaultAssetFolder = folder;
            }

            DeucarianEditorChrome.EndSection();
        }

        private void DrawAssetSummary()
        {
            EnsureSearchResult();

            DeucarianEditorChrome.DrawSectionHeader("Found Assets");
            DeucarianEditorChrome.BeginSection();
            EditorGUILayout.LabelField("Theme Assets Count", searchResult.Themes.Count.ToString());
            EditorGUILayout.LabelField("Palette Assets Count", searchResult.Palettes.Count.ToString());
            EditorGUILayout.LabelField("Role Library Assets Count", searchResult.RoleLibraries.Count.ToString());

            EditorGUILayout.Space();
            DeucarianEditorStatusBadge.Draw("Themes: " + searchResult.Themes.Count, DeucarianEditorStatus.Info);
            DeucarianEditorStatusBadge.Draw("Palettes: " + searchResult.Palettes.Count, DeucarianEditorStatus.Info);
            DeucarianEditorStatusBadge.Draw("Role Libraries: " + searchResult.RoleLibraries.Count, DeucarianEditorStatus.Info);

            if (searchResult.Themes.Count == 0 && searchResult.Palettes.Count == 0 && searchResult.RoleLibraries.Count == 0)
            {
                DeucarianEditorChrome.DrawInlineHelp(
                    "No Deucarian theme assets were found in this project. Create the default assets to get started.",
                    MessageType.Info);
            }

            DeucarianEditorChrome.EndSection();
        }

        private void DrawActionButtons()
        {
            DeucarianEditorChrome.DrawSectionHeader("Actions");
            DeucarianEditorChrome.BeginSection();

            if (GUILayout.Button("Find Existing Assets"))
            {
                RefreshAssets(true);
            }

            if (GUILayout.Button("Create Missing Default Theme Assets"))
            {
                DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets();
                DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
                RefreshAssets(true);
            }

            if (GUILayout.Button("Create Game Theme Assets"))
            {
                DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets();
                DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
                RefreshAssets(true);
            }

            if (GUILayout.Button("Create UI Toolkit Demo Assets"))
            {
                DeucarianUIToolkitDemoAssetFactory.CreateDemoAssets();
            }

            if (GUILayout.Button("Apply Active Theme To Open Scene"))
            {
                DeucarianThemingMenuActions.ApplyActiveThemeToOpenScene();
            }

            if (GUILayout.Button("Open Theme Assets Folder"))
            {
                DeucarianThemingMenuActions.OpenThemeAssetsFolder();
            }

            DeucarianEditorChrome.EndSection();
        }

        private void RefreshAssets(bool autoSelectSingles)
        {
            searchResult = DeucarianThemingMenuActions.FindExistingAssets(null, autoSelectSingles);
            Repaint();
        }

        private void EnsureSearchResult()
        {
            if (searchResult == null)
            {
                RefreshAssets(false);
            }
        }
    }
}
