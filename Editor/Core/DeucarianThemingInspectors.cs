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
        private readonly DeucarianThemingInspectorListFilterState rolesFilter =
            new DeucarianThemingInspectorListFilterState();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "roles");
            DeucarianThemingInspectorListFilter.Draw(
                serializedObject.FindProperty("roles"),
                DeucarianThemingInspectorListKind.ColorRoleLibraryRoles,
                rolesFilter,
                "Search roles");
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

            using (new EditorGUI.DisabledScope(rolesFilter.IsFiltering))
            {
                if (GUILayout.Button("Sort By Category Then Display Name"))
                {
                    Undo.RecordObject(library, "Sort Color Roles");
                    library.SortRolesByCategoryAndName();
                    EditorUtility.SetDirty(library);
                }
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
        private readonly DeucarianThemingInspectorListFilterState entriesFilter =
            new DeucarianThemingInspectorListFilterState();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "entries");
            DeucarianThemingInspectorListFilter.Draw(
                serializedObject.FindProperty("entries"),
                DeucarianThemingInspectorListKind.ColorPaletteEntries,
                entriesFilter,
                "Search palette entries");
            serializedObject.ApplyModifiedProperties();

            DeucarianColorPalette palette = (DeucarianColorPalette)target;
            DrawWarnings(palette.GetValidationWarnings());

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(palette.RoleLibrary == null || entriesFilter.IsFiltering))
            {
                if (GUILayout.Button("Add Missing Roles From Library"))
                {
                    Undo.RecordObject(palette, "Add Missing Palette Roles");
                    int added = palette.AddMissingRolesFromLibrary();
                    EditorUtility.SetDirty(palette);
                    ThemingLog.Editor.Info($"Added {added} missing role entries to {palette.name}.", palette);
                }
            }

            if (GUILayout.Button("Remove Null Entries"))
            {
                Undo.RecordObject(palette, "Remove Null Palette Entries");
                int removed = palette.RemoveNullEntries();
                EditorUtility.SetDirty(palette);
                ThemingLog.Editor.Info($"Removed {removed} null entries from {palette.name}.", palette);
            }

            using (new EditorGUI.DisabledScope(entriesFilter.IsFiltering))
            {
                if (GUILayout.Button("Sort By Category Then Display Name"))
                {
                    Undo.RecordObject(palette, "Sort Palette Entries");
                    palette.SortEntriesByCategoryAndName();
                    EditorUtility.SetDirty(palette);
                }
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

    [CustomEditor(typeof(DeucarianThemePack))]
    [CanEditMultipleObjects]
    public sealed class DeucarianThemePackEditor : UnityEditor.Editor
    {
        private readonly DeucarianThemingInspectorListFilterState rolesFilter =
            new DeucarianThemingInspectorListFilterState();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (UsesNativeUnfilteredInspector(serializedObject))
            {
                DrawDefaultInspector();
            }
            else
            {
                DrawPropertiesExcluding(serializedObject, "roles");
                DeucarianThemingInspectorListFilter.Draw(
                    serializedObject.FindProperty("roles"),
                    DeucarianThemingInspectorListKind.ThemePackRoles,
                    rolesFilter,
                    "Search theme pack roles");
            }

            serializedObject.ApplyModifiedProperties();
        }

        internal static bool UsesNativeUnfilteredInspector(SerializedObject inspectedObject)
        {
            return inspectedObject != null && inspectedObject.isEditingMultipleObjects;
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
                if (GUILayout.Button("Select Palette"))
                {
                    Selection.activeObject = theme.ColorPalette;
                    EditorGUIUtility.PingObject(theme.ColorPalette);
                }
            }
        }
    }

    [CustomEditor(typeof(DeucarianThemeFamily))]
    public sealed class DeucarianThemeFamilyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianThemeFamily family = (DeucarianThemeFamily)target;
            if (string.IsNullOrWhiteSpace(family.FamilyId))
            {
                EditorGUILayout.HelpBox("Theme family has no stable family ID.", MessageType.Warning);
            }

            if (string.IsNullOrWhiteSpace(family.DisplayName))
            {
                EditorGUILayout.HelpBox("Theme family has no display name.", MessageType.Warning);
            }

            if (!family.IsComplete)
            {
                EditorGUILayout.HelpBox(
                    "Theme families require both a light theme and a dark theme for authoring. Runtime will fall back to the available variant when one is missing.",
                    MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawSelectThemeButton("Select Light Theme", family.LightTheme);
            DrawSelectThemeButton("Select Dark Theme", family.DarkTheme);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Repair Theme Family"))
            {
                DeucarianDefaultThemeAssets assets = DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(family);
                DeucarianThemingEditorSettings.ActiveThemeFamily = assets.ThemeFamily;
            }
        }

        private static void DrawSelectThemeButton(string label, DeucarianTheme theme)
        {
            using (new EditorGUI.DisabledScope(theme == null))
            {
                if (GUILayout.Button(label))
                {
                    Selection.activeObject = theme;
                    EditorGUIUtility.PingObject(theme);
                }
            }
        }
    }

    [CustomEditor(typeof(DeucarianThemeProvider))]
    public sealed class DeucarianThemeProviderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            SerializedProperty standaloneTheme = serializedObject.FindProperty("currentTheme");
            SerializedProperty familyProperty = serializedObject.FindProperty("currentThemeFamily");
            DeucarianThemeFamily family = familyProperty != null
                ? familyProperty.objectReferenceValue as DeucarianThemeFamily
                : null;

            if (standaloneTheme != null
                && standaloneTheme.objectReferenceValue != null
                && family != null)
            {
                EditorGUILayout.HelpBox(
                    "A provider cannot author both a standalone theme and a theme family. Use SetTheme or SetThemeFamily to choose one source.",
                    MessageType.Error);
            }

            if (family != null && !family.IsComplete)
            {
                EditorGUILayout.HelpBox(
                    "The assigned theme family is incomplete. Runtime will fall back to its available variant, but authoring requires both light and dark themes.",
                    MessageType.Error);
            }
        }
    }

    [CustomEditor(typeof(DeucarianThemeRuntimeSettings))]
    public sealed class DeucarianThemeRuntimeSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            SerializedProperty standaloneTheme = serializedObject.FindProperty("defaultTheme");
            SerializedProperty familyProperty = serializedObject.FindProperty("defaultThemeFamily");
            DeucarianThemeFamily family = familyProperty != null
                ? familyProperty.objectReferenceValue as DeucarianThemeFamily
                : null;

            if (standaloneTheme != null
                && standaloneTheme.objectReferenceValue != null
                && family != null)
            {
                EditorGUILayout.HelpBox(
                    "Runtime settings cannot author both a standalone default theme and a default theme family. Configure one source only.",
                    MessageType.Error);
            }

            if (family != null && !family.IsComplete)
            {
                EditorGUILayout.HelpBox(
                    "The default theme family is incomplete. Runtime will fall back to its available variant, but authoring requires both light and dark themes.",
                    MessageType.Error);
            }
        }
    }
}
