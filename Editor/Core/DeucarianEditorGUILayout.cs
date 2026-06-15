using System;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Shared IMGUI helpers for Deucarian editor tooling windows.
    /// </summary>
    public static class DeucarianEditorGUILayout
    {
        private const float SelectButtonWidth = 64f;

        /// <summary>
        /// Draws the standard Deucarian asset row: object field with an inline Select/Ping button.
        /// </summary>
        public static T DrawAssetFieldWithSelectButton<T>(
            string label,
            T asset,
            Action<T> setAsset,
            Func<T> resolveMissingAsset = null)
            where T : UnityEngine.Object
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                T nextAsset = (T)EditorGUILayout.ObjectField(label, asset, typeof(T), false);
                if (EditorGUI.EndChangeCheck())
                {
                    asset = nextAsset;
                    setAsset?.Invoke(asset);
                }

                using (new EditorGUI.DisabledScope(asset == null && resolveMissingAsset == null))
                {
                    if (GUILayout.Button(new GUIContent("Select", "Select and ping this asset in the Project window."), GUILayout.Width(SelectButtonWidth)))
                    {
                        T selectedAsset = asset;
                        if (selectedAsset == null && resolveMissingAsset != null)
                        {
                            selectedAsset = resolveMissingAsset();
                        }

                        if (selectedAsset != null)
                        {
                            if (selectedAsset != asset)
                            {
                                asset = selectedAsset;
                                setAsset?.Invoke(asset);
                            }

                            Selection.activeObject = selectedAsset;
                            EditorGUIUtility.PingObject(selectedAsset);
                        }
                    }
                }
            }

            return asset;
        }
    }
}
