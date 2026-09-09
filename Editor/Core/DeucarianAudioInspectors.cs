using Deucarian.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    [CustomEditor(typeof(DeucarianAudioRole))]
    public sealed class DeucarianAudioRoleEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>
            DeucarianEditorInspector.Create(OnInspectorGUI);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianAudioRole role = (DeucarianAudioRole)target;
            string warning = role.GetValidationWarning();
            DeucarianEditorStatusPanel.DrawValidationCard(
                "Audio role validation",
                string.IsNullOrEmpty(warning)
                    ? new List<string>()
                    : new List<string> { warning },
                string.IsNullOrEmpty(warning)
                    ? DeucarianEditorStatus.Success
                    : DeucarianEditorStatus.Warning);
        }
    }

    [CustomEditor(typeof(DeucarianAudioRoleLibrary))]
    public sealed class DeucarianAudioRoleLibraryEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>
            DeucarianEditorInspector.Create(OnInspectorGUI);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianAudioRoleLibrary library = (DeucarianAudioRoleLibrary)target;
            List<string> warnings = library.GetValidationWarnings();
            DeucarianEditorStatusPanel.DrawValidationCard(
                "Audio role library validation",
                warnings,
                warnings.Count == 0
                    ? DeucarianEditorStatus.Success
                    : DeucarianEditorStatus.Warning);

            EditorGUILayout.Space();
            if (DeucarianEditorActionGUI.Button("Remove Null Roles"))
            {
                Undo.RecordObject(library, "Remove Null Audio Roles");
                library.RemoveNullRoles();
                EditorUtility.SetDirty(library);
            }

            if (DeucarianEditorActionGUI.Button("Sort By Category Then Display Name"))
            {
                Undo.RecordObject(library, "Sort Audio Roles");
                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }
        }
    }

    [CustomEditor(typeof(DeucarianAudioPaletteSet))]
    public sealed class DeucarianAudioPaletteSetEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>
            DeucarianEditorInspector.Create(OnInspectorGUI);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianAudioPaletteSet set = (DeucarianAudioPaletteSet)target;
            DeucarianEditorStatusPanel.DrawValidationCard(
                "Audio profile validation",
                set.GetValidationWarnings(),
                set.GetValidationWarnings().Count == 0
                    ? DeucarianEditorStatus.Success
                    : DeucarianEditorStatus.Warning);

            if (DeucarianEditorActionGUI.Button("Open Audio Palette Lab"))
            {
                DeucarianAudioPaletteLabWindow.Open(set);
            }
        }
    }

    [CustomEditor(typeof(DeucarianAudioPalette))]
    public sealed class DeucarianAudioPaletteEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>
            DeucarianEditorInspector.Create(OnInspectorGUI);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DeucarianAudioPalette palette = (DeucarianAudioPalette)target;
            List<string> warnings = palette.GetValidationWarnings();
            for (int i = 0; i < warnings.Count; i++)
            {
                DeucarianEditorTextGUI.HelpBox(warnings[i], MessageType.Warning);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(palette.RoleLibrary == null))
            {
                if (DeucarianEditorActionGUI.Button("Add Missing Roles From Library"))
                {
                    Undo.RecordObject(palette, "Add Missing Audio Roles");
                    palette.AddMissingRolesFromLibrary();
                    EditorUtility.SetDirty(palette);
                }
            }

            if (DeucarianEditorActionGUI.Button("Remove Null Entries"))
            {
                Undo.RecordObject(palette, "Remove Null Audio Entries");
                palette.RemoveNullEntries();
                EditorUtility.SetDirty(palette);
            }

            using (new EditorGUI.DisabledScope(false))
            {
                if (DeucarianEditorActionGUI.Button("Sort By Category Then Display Name"))
                {
                    Undo.RecordObject(palette, "Sort Audio Palette Entries");
                    palette.SortEntriesByCategoryAndName();
                    EditorUtility.SetDirty(palette);
                }
            }
        }
    }
}
