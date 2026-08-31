using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianThemeManagerWindow
    {


        private static UnityEngine.Object DrawWorkbenchObjectField(
            string label,
            UnityEngine.Object value,
            Type objectType,
            bool allowSceneObjects)
        {
            GetWorkbenchFieldRects(out Rect labelRect, out Rect fieldRect);
            EditorGUI.LabelField(
                labelRect,
                new GUIContent(label ?? string.Empty, label ?? string.Empty),
                DeucarianEditorWorkbenchGUI.LabelStyle);
            return EditorGUI.ObjectField(fieldRect, value, objectType, allowSceneObjects);
        }

        private static Enum DrawWorkbenchEnumPopup(string label, Enum value)
        {
            GetWorkbenchFieldRects(out Rect labelRect, out Rect fieldRect);
            EditorGUI.LabelField(
                labelRect,
                new GUIContent(label ?? string.Empty, label ?? string.Empty),
                DeucarianEditorWorkbenchGUI.LabelStyle);
            return EditorGUI.EnumPopup(fieldRect, value);
        }

        private static void GetWorkbenchFieldRects(out Rect labelRect, out Rect fieldRect)
        {
            Rect row = EditorGUILayout.GetControlRect();
            float labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, row.width);
            labelRect = new Rect(row.x, row.y, labelWidth, row.height);
            fieldRect = new Rect(
                labelRect.xMax,
                row.y,
                Mathf.Max(0f, row.xMax - labelRect.xMax),
                row.height);
        }

        private static void DrawAssetDropdown<T>(
            string label,
            T selected,
            System.Collections.Generic.IReadOnlyList<T> assets,
            Action<T> onSelected)
            where T : UnityEngine.Object
        {
            Rect row = EditorGUILayout.GetControlRect();
            Rect labelRect = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
            Rect fieldRect = new Rect(
                labelRect.xMax,
                row.y,
                Mathf.Max(0f, row.xMax - labelRect.xMax),
                row.height);
            EditorGUI.LabelField(
                labelRect,
                new GUIContent(label ?? string.Empty, label ?? string.Empty),
                DeucarianEditorWorkbenchGUI.LabelStyle);

            string valueLabel = selected != null ? selected.name : "None";
            string tooltip = selected != null ? AssetDatabase.GetAssetPath(selected) : string.Empty;
            if (EditorGUI.DropdownButton(
                    fieldRect,
                    new GUIContent(valueLabel, tooltip),
                    FocusType.Keyboard))
            {
                UnityEditor.PopupWindow.Show(
                    fieldRect,
                    new ThemeAssetPickerPopup<T>(assets, selected, onSelected));
            }
        }

        private sealed class ThemeAssetPickerPopup<T> : PopupWindowContent
            where T : UnityEngine.Object
        {
            private readonly System.Collections.Generic.IReadOnlyList<T> assets;
            private readonly T selected;
            private readonly Action<T> onSelected;
            private string search = string.Empty;
            private Vector2 pickerScroll;

            public ThemeAssetPickerPopup(
                System.Collections.Generic.IReadOnlyList<T> assets,
                T selected,
                Action<T> onSelected)
            {
                this.assets = assets ?? Array.Empty<T>();
                this.selected = selected;
                this.onSelected = onSelected;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(360f, 300f);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.Space(5f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(6f);
                    search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                    GUILayout.Space(6f);
                }

                using (var scrollView = new EditorGUILayout.ScrollViewScope(pickerScroll))
                {
                    pickerScroll = scrollView.scrollPosition;
                    DrawChoice(null, "None", string.Empty);
                    for (int i = 0; i < assets.Count; i++)
                    {
                        T asset = assets[i];
                        if (asset == null)
                        {
                            continue;
                        }

                        string assetPath = AssetDatabase.GetAssetPath(asset);
                        string searchableText = asset.name + " " + assetPath;
                        if (!string.IsNullOrWhiteSpace(search)
                            && searchableText.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        DrawChoice(asset, asset.name, assetPath);
                    }
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    Event.current.Use();
                }
            }

            private void DrawChoice(T asset, string displayName, string path)
            {
                bool isSelected = asset == selected;
                string label = (isSelected ? "[x] " : "      ") + displayName;
                GUIContent content = new GUIContent(label, path);
                if (!GUILayout.Button(content, EditorStyles.label, GUILayout.Height(24f)))
                {
                    return;
                }

                onSelected?.Invoke(asset);
                editorWindow.Close();
            }
        }

        private static string ResolvePackageVersion()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(DeucarianThemeManagerWindow).Assembly);
            return package != null && !string.IsNullOrWhiteSpace(package.version)
                ? package.version
                : "development";
        }
    }
}
