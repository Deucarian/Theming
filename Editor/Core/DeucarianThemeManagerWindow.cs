using System.Collections.Generic;
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
            DrawAssetPickers();
            EditorGUILayout.Space();
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveAssetFields()
        {
            EditorGUILayout.LabelField("Active Assets", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            DeucarianTheme theme = (DeucarianTheme)EditorGUILayout.ObjectField(
                "Active Theme",
                DeucarianThemingEditorSettings.ActiveTheme,
                typeof(DeucarianTheme),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                DeucarianThemingEditorSettings.ActiveTheme = theme;
            }

            EditorGUI.BeginChangeCheck();
            DeucarianColorPalette palette = (DeucarianColorPalette)EditorGUILayout.ObjectField(
                "Active Palette",
                DeucarianThemingEditorSettings.ActivePalette,
                typeof(DeucarianColorPalette),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                DeucarianThemingEditorSettings.ActivePalette = palette;
            }

            EditorGUI.BeginChangeCheck();
            DeucarianColorRoleLibrary roleLibrary = (DeucarianColorRoleLibrary)EditorGUILayout.ObjectField(
                "Role Library",
                DeucarianThemingEditorSettings.ActiveRoleLibrary,
                typeof(DeucarianColorRoleLibrary),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                DeucarianThemingEditorSettings.ActiveRoleLibrary = roleLibrary;
            }
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

        private void DrawAssetPickers()
        {
            EnsureSearchResult();

            DrawAssetPopup(
                "Found Theme",
                searchResult.Themes,
                DeucarianThemingEditorSettings.ActiveTheme,
                theme => DeucarianThemingEditorSettings.ActiveTheme = theme);
            DrawAssetPopup(
                "Found Palette",
                searchResult.Palettes,
                DeucarianThemingEditorSettings.ActivePalette,
                palette => DeucarianThemingEditorSettings.ActivePalette = palette);
            DrawAssetPopup(
                "Found Role Library",
                searchResult.RoleLibraries,
                DeucarianThemingEditorSettings.ActiveRoleLibrary,
                roleLibrary => DeucarianThemingEditorSettings.ActiveRoleLibrary = roleLibrary);
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

            using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActiveTheme == null))
            {
                if (GUILayout.Button("Select Active Theme"))
                {
                    DeucarianThemingMenuActions.SelectAndPing(DeucarianThemingEditorSettings.ActiveTheme);
                }
            }

            using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActivePalette == null))
            {
                if (GUILayout.Button("Select Active Palette"))
                {
                    DeucarianThemingMenuActions.SelectAndPing(DeucarianThemingEditorSettings.ActivePalette);
                }
            }

            using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActiveRoleLibrary == null))
            {
                if (GUILayout.Button("Select Role Library"))
                {
                    DeucarianThemingMenuActions.SelectAndPing(DeucarianThemingEditorSettings.ActiveRoleLibrary);
                }
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

        private static void DrawAssetPopup<T>(
            string label,
            IReadOnlyList<T> assets,
            T activeAsset,
            System.Action<T> setActiveAsset)
            where T : UnityEngine.Object
        {
            if (assets.Count <= 1)
            {
                return;
            }

            string[] options = new string[assets.Count + 1];
            options[0] = "None";
            int selectedIndex = 0;

            for (int i = 0; i < assets.Count; i++)
            {
                T asset = assets[i];
                options[i + 1] = asset.name + " (" + AssetDatabase.GetAssetPath(asset) + ")";
                if (asset == activeAsset)
                {
                    selectedIndex = i + 1;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(label, selectedIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                setActiveAsset(newIndex == 0 ? null : assets[newIndex - 1]);
            }
        }
    }
}
