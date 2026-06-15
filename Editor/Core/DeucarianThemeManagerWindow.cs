using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Package-local manager window for selecting and applying Deucarian theme assets.
    /// </summary>
    public sealed class DeucarianThemeManagerWindow : EditorWindow
    {
        private const string WindowTitle = "Theme Manager";
        private const float MinWidth = 520f;
        private const float MinHeight = 420f;

        private Vector2 scrollPosition;

        [MenuItem("Deucarian/Theming/Open Theme Manager")]
        public static void Open()
        {
            DeucarianThemeManagerWindow window = GetWindow<DeucarianThemeManagerWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(MinWidth, MinHeight);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Deucarian Theme Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawActiveAssetField(
                "Active Theme",
                DeucarianThemingEditorAssetUtility.GetActiveTheme(),
                DeucarianThemingEditorAssetUtility.SetActiveTheme);
            DrawActiveAssetField(
                "Active Palette",
                DeucarianThemingEditorAssetUtility.GetActivePalette(),
                DeucarianThemingEditorAssetUtility.SetActivePalette);
            DrawActiveAssetField(
                "Active Role Library",
                DeucarianThemingEditorAssetUtility.GetActiveRoleLibrary(),
                DeucarianThemingEditorAssetUtility.SetActiveRoleLibrary);

            EditorGUILayout.Space();
            DrawActions();

            EditorGUILayout.Space();
            DrawAssetList(
                "Themes",
                DeucarianThemingEditorAssetUtility.GetActiveTheme(),
                DeucarianThemingEditorAssetUtility.SetActiveTheme);
            DrawAssetList(
                "Palettes",
                DeucarianThemingEditorAssetUtility.GetActivePalette(),
                DeucarianThemingEditorAssetUtility.SetActivePalette);
            DrawAssetList(
                "Role Libraries",
                DeucarianThemingEditorAssetUtility.GetActiveRoleLibrary(),
                DeucarianThemingEditorAssetUtility.SetActiveRoleLibrary);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Missing Default Theme Assets"))
                {
                    DeucarianDefaultThemeAssets assets =
                        DeucarianThemingEditorAssetUtility.CreateMissingDefaultThemeAssets(
                            DeucarianDefaultThemeAssetFactory.DefaultRootFolder);
                    DeucarianThemingEditorAssetUtility.SelectAndPing(assets.Theme);
                }

                if (GUILayout.Button("Open Theme Assets Folder"))
                {
                    DeucarianThemingEditorAssetUtility.OpenThemeAssetsFolder();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Active Theme To Open Scene"))
                {
                    DeucarianThemingEditorAssetUtility.ApplyActiveThemeToOpenSceneFromMenu();
                }
            }
        }

        private static void DrawActiveAssetField<T>(string label, T current, Action<T> setActive)
            where T : Object
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                T next = (T)EditorGUILayout.ObjectField(label, current, typeof(T), false);
                if (EditorGUI.EndChangeCheck())
                {
                    setActive(next);
                }

                using (new EditorGUI.DisabledScope(current == null))
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(56f)))
                    {
                        DeucarianThemingEditorAssetUtility.SelectAndPing(current);
                    }
                }
            }
        }

        private static void DrawAssetList<T>(string title, T activeAsset, Action<T> setActive)
            where T : Object
        {
            IReadOnlyList<T> assets = DeucarianThemingEditorAssetUtility.FindAssets<T>();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title + " (" + assets.Count + ")", EditorStyles.boldLabel);

            if (assets.Count == 0)
            {
                EditorGUILayout.LabelField("No saved assets found.", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < assets.Count; i++)
            {
                DrawAssetRow(assets[i], activeAsset, setActive);
            }
        }

        private static void DrawAssetRow<T>(T asset, T activeAsset, Action<T> setActive)
            where T : Object
        {
            string path = AssetDatabase.GetAssetPath(asset);
            bool isActive = asset == activeAsset;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    new GUIContent(asset.name, path),
                    GUILayout.MinWidth(140f));
                EditorGUILayout.LabelField(
                    new GUIContent(path, path),
                    EditorStyles.miniLabel,
                    GUILayout.MinWidth(160f));

                using (new EditorGUI.DisabledScope(isActive))
                {
                    if (GUILayout.Button(isActive ? "Active" : "Set Active", GUILayout.Width(82f)))
                    {
                        setActive(asset);
                        DeucarianThemingEditorAssetUtility.SelectAndPing(asset);
                    }
                }

                if (GUILayout.Button("Ping", GUILayout.Width(56f)))
                {
                    DeucarianThemingEditorAssetUtility.SelectAndPing(asset);
                }
            }
        }
    }
}
