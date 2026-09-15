using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemingInspectorView
    {
        private readonly SerializedObject serialized;
        private readonly VisualElement feedback;
        private readonly List<Action> refreshers = new List<Action>();
        internal VisualElement Root { get; }
        internal VisualElement Fields { get; }
        internal VisualElement Actions { get; }

        internal DeucarianThemingInspectorView(string title, SerializedObject serialized)
        {
            this.serialized = serialized;
            Root = DeucarianEditorInspector.CreateToolkit(title);
            Fields = new VisualElement(); Root.Add(Fields);
            feedback = new VisualElement(); Root.Add(feedback);
            Actions = DeucarianEditorWorkspaceControls.Actions(); Root.Add(Actions);
        }

        internal void Properties(params string[] excluded) => DeucarianEditorInspector.Properties(Fields, serialized, excluded);
        internal void OnRefresh(Action refresh) => refreshers.Add(refresh);
        internal void Warn(string warning, HelpBoxMessageType tone = HelpBoxMessageType.Warning)
        {
            if (!string.IsNullOrWhiteSpace(warning)) feedback.Add(new HelpBox(warning, tone));
        }
        internal void Warnings(IEnumerable<string> warnings)
        {
            if (warnings != null) foreach (string warning in warnings) Warn(warning);
        }
        internal void Action(string label, Action run, Func<bool> enabled = null, string undo = null)
        {
            var button = DeucarianEditorWorkspaceControls.Button(label, () =>
            {
                if (serialized.targetObject == null || (enabled != null && !enabled())) return;
                if (undo != null) Undo.RecordObjects(serialized.targetObjects, undo);
                run();
                if (undo != null) foreach (var target in serialized.targetObjects) EditorUtility.SetDirty(target);
                serialized.Update(); Refresh();
            });
            Actions.Add(button);
            OnRefresh(() => button.SetEnabled(serialized.targetObject != null && (enabled == null || enabled())));
        }
        internal VisualElement Build()
        {
            DeucarianEditorInspector.Observe(Root, serialized, Refresh);
            return Root;
        }
        internal void Refresh()
        {
            feedback.Clear();
            if (serialized.targetObject == null) return;
            foreach (var refresh in refreshers) refresh();
        }
    }
}
