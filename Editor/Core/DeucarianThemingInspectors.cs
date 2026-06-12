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
            DrawDuplicateRoleIds(library);
            DrawWarnings(library.GetValidationWarnings());

            EditorGUILayout.Space();
            if (GUILayout.Button("Remove Null Roles"))
            {
                Undo.RecordObject(library, "Remove Null Color Roles");
                library.RemoveNullRoles();
                EditorUtility.SetDirty(library);
            }

            if (GUILayout.Button("Sort By Category Then Display Name"))
            {
                Undo.RecordObject(library, "Sort Color Roles");
                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }
        }

        private static void DrawDuplicateRoleIds(DeucarianColorRoleLibrary library)
        {
            List<string> duplicateIds = library.GetDuplicateRoleIds();
            if (duplicateIds.Count == 0)
            {
                return;
            }

            EditorGUILayout.HelpBox("Duplicate role IDs:\n" + string.Join("\n", duplicateIds), MessageType.Error);
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

            if (GUILayout.Button("Remove Null Entries"))
            {
                Undo.RecordObject(palette, "Remove Null Palette Entries");
                int removed = palette.RemoveNullEntries();
                EditorUtility.SetDirty(palette);
                Debug.Log($"Removed {removed} null entries from {palette.name}.", palette);
            }

            if (GUILayout.Button("Sort By Category Then Display Name"))
            {
                Undo.RecordObject(palette, "Sort Palette Entries");
                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }

            using (new EditorGUI.DisabledScope(palette.Entries.Count == 0))
            {
                if (GUILayout.Button("Reset Entry To Role Default"))
                {
                    ShowResetEntryMenu(palette);
                }
            }
        }

        private static void ShowResetEntryMenu(DeucarianColorPalette palette)
        {
            GenericMenu menu = new GenericMenu();
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                DeucarianColorRole role = entry != null ? entry.Role : null;

                if (role == null)
                {
                    menu.AddDisabledItem(new GUIContent($"Entry {i}: Missing Role"));
                    continue;
                }

                int entryIndex = i;
                string label = $"{role.Category}/{role.DisplayName} ({role.Id})";
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    Undo.RecordObject(palette, "Reset Palette Entry To Role Default");
                    palette.ResetEntryToRoleDefault(entryIndex);
                    EditorUtility.SetDirty(palette);
                });
            }

            menu.ShowAsContext();
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
