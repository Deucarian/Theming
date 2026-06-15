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

            DrawActiveAssetFields();
            EditorGUILayout.Space();
            DrawFolderField();
            EditorGUILayout.Space();
            DrawAssetSummary();
            EditorGUILayout.Space();
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveAssetFields()
        {
            EditorGUILayout.LabelField("Active Assets", EditorStyles.boldLabel);

            DeucarianEditorGUILayout.DrawAssetFieldWithSelectButton(
                "Active Theme",
                DeucarianThemingEditorSettings.ActiveTheme,
                theme => DeucarianThemingEditorSettings.ActiveTheme = theme,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveTheme());

            DeucarianEditorGUILayout.DrawAssetFieldWithSelectButton(
                "Active Palette",
                DeucarianThemingEditorSettings.ActivePalette,
                palette => DeucarianThemingEditorSettings.ActivePalette = palette,
                () => DeucarianThemingMenuActions.ResolveOrCreateActivePalette());

            DeucarianEditorGUILayout.DrawAssetFieldWithSelectButton(
                "Role Library",
                DeucarianThemingEditorSettings.ActiveRoleLibrary,
                roleLibrary => DeucarianThemingEditorSettings.ActiveRoleLibrary = roleLibrary,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveRoleLibrary());
        }

        private void DrawFolderField()
        {
            EditorGUILayout.LabelField("Default Asset Folder", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            string folder = EditorGUILayout.TextField("Path", DeucarianThemingEditorSettings.DefaultAssetFolder);
            if (EditorGUI.EndChangeCheck())
            {
                DeucarianThemingEditorSettings.DefaultAssetFolder = folder;
            }
        }

        private void DrawAssetSummary()
        {
            EnsureSearchResult();

            EditorGUILayout.LabelField("Found Assets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Theme Assets Count", searchResult.Themes.Count.ToString());
            EditorGUILayout.LabelField("Palette Assets Count", searchResult.Palettes.Count.ToString());
            EditorGUILayout.LabelField("Role Library Assets Count", searchResult.RoleLibraries.Count.ToString());

            if (searchResult.Themes.Count == 0 && searchResult.Palettes.Count == 0 && searchResult.RoleLibraries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No Deucarian theme assets were found in this project. Create the default assets to get started.",
                    MessageType.Info);
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

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
