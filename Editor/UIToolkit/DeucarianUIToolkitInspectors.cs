using System.Collections.Generic;
using Deucarian.Theming.UIToolkit;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    [CustomEditor(typeof(DeucarianUIToolkitThemeApplier))]
    public sealed class DeucarianUIToolkitThemeApplierEditor : UnityEditor.Editor
    {
        private readonly DeucarianThemingInspectorListFilterState bindingsFilter =
            new DeucarianThemingInspectorListFilterState();
        private List<string> validationWarnings = new List<string>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DeucarianThemingInspectorListFilter.DrawInspectorProperties(
                serializedObject,
                "bindings",
                DeucarianThemingInspectorListKind.UIToolkitBindings,
                bindingsFilter,
                "Search bindings");
            serializedObject.ApplyModifiedProperties();

            DeucarianUIToolkitThemeApplier applier = (DeucarianUIToolkitThemeApplier)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply Now"))
            {
                applier.ApplyNow();
            }

            if (GUILayout.Button("Validate Bindings"))
            {
                validationWarnings = applier.ValidateBindings();
            }

            DrawBindingSummary(applier, bindingsFilter.VisibleIndices);
            DrawWarnings(validationWarnings);
        }

        private static void DrawBindingSummary(
            DeucarianUIToolkitThemeApplier applier,
            IReadOnlyList<int> visibleIndices)
        {
            IReadOnlyList<DeucarianUIToolkitThemeBinding> bindings = applier.Bindings;
            if (bindings == null || bindings.Count == 0)
            {
                EditorGUILayout.HelpBox("No UI Toolkit theme bindings are configured.", MessageType.Info);
                return;
            }

            IReadOnlyList<int> summaryIndices = GetBindingSummaryIndices(bindings.Count, visibleIndices);
            for (int i = 0; i < summaryIndices.Count; i++)
            {
                int bindingIndex = summaryIndices[i];
                DeucarianUIToolkitThemeBinding binding = bindings[bindingIndex];
                if (binding == null)
                {
                    EditorGUILayout.HelpBox($"Binding {bindingIndex} is null.", MessageType.Warning);
                    continue;
                }

                string selector = GetSelectorLabel(binding);
                int matchCount = applier.CountMatches(binding);
                EditorGUILayout.LabelField(
                    $"Binding {bindingIndex}",
                    $"{selector} -> {binding.StyleProperty} ({matchCount} matches)");

                if (binding.ColorRole == null)
                {
                    EditorGUILayout.HelpBox($"Binding {bindingIndex} has no color role.", MessageType.Warning);
                }

                if (string.IsNullOrWhiteSpace(binding.UssSelector)
                    && string.IsNullOrWhiteSpace(binding.ElementName)
                    && string.IsNullOrWhiteSpace(binding.ElementClass))
                {
                    EditorGUILayout.HelpBox(
                        $"Binding {bindingIndex} targets the UIDocument root.",
                        MessageType.Info);
                }
            }
        }

        internal static IReadOnlyList<int> GetBindingSummaryIndices(
            int bindingCount,
            IReadOnlyList<int> visibleIndices)
        {
            List<int> result = new List<int>();
            if (visibleIndices == null || bindingCount <= 0)
            {
                return result;
            }

            for (int i = 0; i < visibleIndices.Count; i++)
            {
                int index = visibleIndices[i];
                if (index >= 0 && index < bindingCount)
                {
                    result.Add(index);
                }
            }

            return result;
        }

        private static string GetSelectorLabel(DeucarianUIToolkitThemeBinding binding)
        {
            if (!string.IsNullOrWhiteSpace(binding.UssSelector))
            {
                return binding.UssSelector;
            }

            if (!string.IsNullOrWhiteSpace(binding.ElementName))
            {
                return "#" + binding.ElementName;
            }

            if (!string.IsNullOrWhiteSpace(binding.ElementClass))
            {
                return "." + binding.ElementClass;
            }

            return "<root>";
        }

        private static void DrawWarnings(List<string> warnings)
        {
            if (warnings == null)
            {
                return;
            }

            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(DeucarianUIToolkitThemeVariables))]
    public sealed class DeucarianUIToolkitThemeVariablesEditor : UnityEditor.Editor
    {
        private readonly DeucarianThemingInspectorListFilterState variableMappingsFilter =
            new DeucarianThemingInspectorListFilterState();
        private List<string> previewNames = new List<string>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DeucarianThemingInspectorListFilter.DrawInspectorProperties(
                serializedObject,
                "explicitVariableMappings",
                DeucarianThemingInspectorListKind.UIToolkitVariableMappings,
                variableMappingsFilter,
                "Search variable mappings");
            serializedObject.ApplyModifiedProperties();

            DeucarianUIToolkitThemeVariables variables = (DeucarianUIToolkitThemeVariables)target;

            EditorGUILayout.Space();
            if (variables.RoleLibrary == null)
            {
                EditorGUILayout.HelpBox("Assign a role library to generate UI Toolkit variables.", MessageType.Warning);
            }

            if (GUILayout.Button("Apply Variables Now"))
            {
                variables.ApplyVariablesNow();
            }

            if (GUILayout.Button("Preview Variable Names"))
            {
                previewNames = variables.PreviewVariableNames();
            }

            if (previewNames != null && previewNames.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", previewNames), MessageType.Info);
            }
        }
    }
}
