using Deucarian.Editor;
using System.Collections.Generic;
using Deucarian.Theming.UIToolkit;
using UnityEditor;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    [CustomEditor(typeof(DeucarianUIToolkitThemeApplier))]
    public sealed class DeucarianUIToolkitThemeApplierEditor : UnityEditor.Editor
    {
        private readonly DeucarianThemingInspectorListFilterState bindingsFilter = new DeucarianThemingInspectorListFilterState();
        private List<string> validationWarnings = new List<string>();

        public override VisualElement CreateInspectorGUI()
        {
            var applier = (DeucarianUIToolkitThemeApplier)target;
            var view = new DeucarianThemingInspectorView("UI Toolkit theme bindings", serializedObject);
            view.Properties("bindings");
            var list = new DeucarianThemingInspectorList(serializedObject, "bindings",
                DeucarianThemingInspectorListKind.UIToolkitBindings, bindingsFilter, "Search bindings", view.Refresh);
            view.Fields.Add(list.Root);
            var summary = new Foldout { text = "Binding matches", value = false };
            summary.AddToClassList("dw-foldout"); view.Fields.Add(summary);
            view.OnRefresh(() =>
            {
                list.Refresh(); summary.Clear(); view.Warnings(validationWarnings);
                if (applier.Bindings == null || applier.Bindings.Count == 0)
                {
                    view.Warn("No theme bindings are configured.", HelpBoxMessageType.Info); return;
                }
                foreach (int index in GetBindingSummaryIndices(applier.Bindings.Count, bindingsFilter.VisibleIndices))
                {
                    var binding = applier.Bindings[index];
                    if (binding == null) { summary.Add(new HelpBox($"Binding {index} is empty.", HelpBoxMessageType.Warning)); continue; }
                    summary.Add(DeucarianEditorWorkspaceControls.Label(
                        $"Binding {index} · {GetSelectorLabel(binding)} → {binding.StyleProperty} · {applier.CountMatches(binding)} matches", "dw-muted"));
                    if (binding.ColorRole == null) summary.Add(new HelpBox($"Binding {index} has no color role.", HelpBoxMessageType.Warning));
                    if (GetSelectorLabel(binding) == "<root>")
                        summary.Add(DeucarianEditorWorkspaceControls.Label("Targets the UIDocument root.", "dw-muted"));
                }
            });
            view.Action("Apply now", applier.ApplyNow);
            view.Action("Validate bindings", () => validationWarnings = applier.ValidateBindings());
            return view.Build();
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

    }

    [CustomEditor(typeof(DeucarianUIToolkitThemeVariables))]
    public sealed class DeucarianUIToolkitThemeVariablesEditor : UnityEditor.Editor
    {
        private readonly DeucarianThemingInspectorListFilterState variableMappingsFilter = new DeucarianThemingInspectorListFilterState();
        private List<string> previewNames = new List<string>();
        public override VisualElement CreateInspectorGUI()
        {
            var variables = (DeucarianUIToolkitThemeVariables)target;
            var view = new DeucarianThemingInspectorView("UI Toolkit theme variables", serializedObject);
            view.Properties("explicitVariableMappings");
            view.Fields.Add(new DeucarianThemingInspectorList(serializedObject, "explicitVariableMappings",
                DeucarianThemingInspectorListKind.UIToolkitVariableMappings, variableMappingsFilter, "Search mappings").Root);
            var names = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-muted"); view.Fields.Add(names);
            view.OnRefresh(() =>
            {
                if (variables.RoleLibrary == null) view.Warn("Assign a role library to generate variables.");
                names.text = previewNames == null ? string.Empty : string.Join("\n", previewNames);
            });
            view.Action("Apply variables", variables.ApplyVariablesNow);
            view.Action("Preview names", () => previewNames = variables.PreviewVariableNames());
            return view.Build();
        }
    }
}
