using System.Collections.Generic;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    [CustomEditor(typeof(DeucarianColorRole))]
    public sealed class DeucarianColorRoleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianColorRole role = (DeucarianColorRole)target;
            string warning = role.GetValidationWarning();
            if (!string.IsNullOrEmpty(warning))
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(DeucarianColorRoleLibrary))]
    public sealed class DeucarianColorRoleLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianColorRoleLibrary library = (DeucarianColorRoleLibrary)target;
            DrawWarnings(library.GetValidationWarnings());

            EditorGUILayout.Space();
            if (GUILayout.Button("Remove Null Entries"))
            {
                Undo.RecordObject(library, "Remove Null Color Roles");
                library.RemoveNullRoles();
                EditorUtility.SetDirty(library);
            }

            if (GUILayout.Button("Sort Roles By Category/Name"))
            {
                Undo.RecordObject(library, "Sort Color Roles");
                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }
        }

        private static void DrawWarnings(List<string> warnings)
        {
            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(DeucarianColorPalette))]
    public sealed class DeucarianColorPaletteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianColorPalette palette = (DeucarianColorPalette)target;
            DrawWarnings(palette.GetValidationWarnings());

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(palette.RoleLibrary == null))
            {
                if (GUILayout.Button("Add Missing Roles From Library"))
                {
                    Undo.RecordObject(palette, "Add Missing Palette Roles");
                    int added = palette.AddMissingRolesFromLibrary();
                    EditorUtility.SetDirty(palette);
                    Debug.Log($"Added {added} missing role entries to {palette.name}.", palette);
                }
            }

            if (GUILayout.Button("Sort Entries By Category/Name"))
            {
                Undo.RecordObject(palette, "Sort Palette Entries");
                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }
        }

        private static void DrawWarnings(List<string> warnings)
        {
            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(DeucarianTheme))]
    public sealed class DeucarianThemeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianTheme theme = (DeucarianTheme)target;
            if (theme.ColorPalette == null)
            {
                EditorGUILayout.HelpBox("Theme has no color palette assigned.", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(theme.ColorPalette == null))
            {
                if (GUILayout.Button("Ping Palette"))
                {
                    Selection.activeObject = theme.ColorPalette;
                    EditorGUIUtility.PingObject(theme.ColorPalette);
                }
            }
        }
    }
}
