using System;
using System.Collections.Generic;
using System.Linq;
using Deucarian.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemingInspectorList
    {
        private readonly SerializedObject serialized;
        private readonly string path;
        private readonly DeucarianThemingInspectorListKind kind;
        private readonly DeucarianThemingInspectorListFilterState state;
        private readonly VisualElement items;
        private readonly PopupField<string> category;
        private readonly Label count;
        private string signature;
        internal VisualElement Root { get; }

        internal DeucarianThemingInspectorList(SerializedObject serialized, string path,
            DeucarianThemingInspectorListKind kind, DeucarianThemingInspectorListFilterState state,
            string searchLabel, Action changed = null)
        {
            this.serialized = serialized; this.path = path; this.kind = kind; this.state = state;
            Root = new VisualElement();
            var form = new DeucarianEditorWorkspaceForm(Root);
            form.Text("search-" + path, searchLabel, () => state.SearchText, value =>
            { state.SearchText = value; Refresh(); changed?.Invoke(); });
            category = new PopupField<string>(new List<string> { DeucarianThemingInspectorListFilter.AllCategories }, 0,
                DeucarianThemingInspectorListFilter.GetCategoryDisplayLabel,
                DeucarianThemingInspectorListFilter.GetCategoryDisplayLabel);
            Root.Add(DeucarianEditorWorkspaceControls.Field("Category", category));
            category.RegisterValueChangedCallback(evt =>
            { state.SelectedCategory = evt.newValue; Refresh(); changed?.Invoke(); });
            count = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-muted"); Root.Add(count);
            items = new VisualElement(); Root.Add(items);
            DeucarianEditorInspector.Observe(Root, serialized, () => { Refresh(); changed?.Invoke(); });
        }

        internal void Refresh()
        {
            var list = serialized.FindProperty(path);
            if (list == null || !list.isArray)
            {
                items.Clear(); state.SetVisibleIndices(Array.Empty<int>());
                count.text = "The serialized list is unavailable."; return;
            }
            var categories = DeucarianThemingInspectorListFilter.GetCategories(list, kind).ToList();
            if (!categories.Contains(state.SelectedCategory)) state.SelectedCategory = DeucarianThemingInspectorListFilter.AllCategories;
            category.choices = categories; category.SetValueWithoutNotify(state.SelectedCategory);
            var visible = DeucarianThemingInspectorListFilter.GetVisibleIndices(list, kind, state.SearchText, state.SelectedCategory);
            state.SetVisibleIndices(visible);
            count.text = state.IsFiltering ? $"Showing {visible.Count} of {list.arraySize}. Clear filters to add or reorder." : string.Empty;
            string next = (state.IsFiltering ? "filtered:" + string.Join(",", visible) : "all") + ":" + list.arraySize;
            if (signature == next) return;
            signature = next; items.Unbind(); items.Clear();
            if (!state.IsFiltering || serialized.isEditingMultipleObjects)
            {
                AddProperty(list, items); return;
            }
            if (visible.Count == 0)
                items.Add(DeucarianEditorWorkspaceControls.Label("No matching items.", "dw-muted"));
            int renderedCount = list.arraySize;
            foreach (int index in visible)
            {
                var row = new VisualElement(); items.Add(row);
                row.Add(DeucarianEditorWorkspaceControls.Actions(
                    DeucarianEditorWorkspaceControls.Label("Element " + index),
                    DeucarianEditorWorkspaceControls.Button("Remove", () =>
                    {
                        serialized.Update();
                        var current = serialized.FindProperty(path);
                        var currentVisible = DeucarianThemingInspectorListFilter.GetVisibleIndices(current, kind, state.SearchText, state.SelectedCategory);
                        if (current == null || current.arraySize != renderedCount || !currentVisible.SequenceEqual(visible)) { Refresh(); return; }
                        DeucarianThemingInspectorListFilter.DeleteAt(current, index, "Remove Filtered Theming List Item");
                        signature = null; Refresh();
                    }, DeucarianEditorButtonRole.Destructive)));
                AddProperty(list.GetArrayElementAtIndex(index), row);
            }
        }

        private void AddProperty(SerializedProperty property, VisualElement parent)
        {
            var field = new PropertyField(property.Copy()) { name = property.propertyPath };
            field.AddToClassList("dw-native-property"); parent.Add(field); field.Bind(serialized);
        }
    }
}
