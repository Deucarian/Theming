using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    public sealed class DeucarianThemingEditorFeatureGate : IDisposable
    {
        private readonly DeucarianEditorCapabilityGate gate;
        private readonly VisualElement[] related;
        private readonly Func<bool> enabled;
        private bool disposed;
        private DeucarianEditorWorkspace workspace;
        private VisualElement body;
        private bool scopeWasStacked;
        private DisplayStyle tabsDisplay;
        private bool? previous;
        public VisualElement Root => gate.Root;

        public DeucarianThemingEditorFeatureGate(VisualElement controls, bool audio, Action whenDisabled = null,
            params VisualElement[] related)
        {
            this.related = related ?? Array.Empty<VisualElement>();
            enabled = audio ? (Func<bool>)(() => DeucarianThemeRuntimeResolver.UseAudio)
                : () => DeucarianThemeRuntimeResolver.UseVisualStyling;
            string feature = audio ? "Audio" : "Visual styling";
            gate = new DeucarianEditorCapabilityGate(controls, enabled, feature + " is off",
                "Enable it in Project setup to edit " + (audio ? "audio" : "visual") + " palettes.",
                "Go to Project setup", () => DeucarianEditorNavigation.Open(Root, DeucarianThemingProjectPage.ToolId), whenDisabled,
                audio ? DeucarianEditorIconIds.Audio : DeucarianEditorIconIds.Palette);
            Root.RegisterCallback<AttachToPanelEvent>(OnAttach);
            Root.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            Refresh();
        }

        public static DeucarianThemingEditorFeatureGate Wrap(DeucarianEditorWorkspace workspace, bool audio, Action whenDisabled = null)
        {
            var controls = DeucarianEditorWorkspaceControls.Region(null, "dw-gated-content");
            controls.style.flexGrow = 1;
            controls.style.minHeight = 0;
            var body = DeucarianEditorWorkspaceControls.Region(null, "dw-gated-content");
            body.style.flexGrow = 1;
            body.style.minHeight = 0;
            bool scopeFirst = workspace.Scope.parent.IndexOf(workspace.Scope) < workspace.Tabs.parent.IndexOf(workspace.Tabs);
            var context = DeucarianEditorWorkspaceControls.Scroll("theming-gated-context");
            context.AddToClassList("dw-context-scroll");
            context.Add(scopeFirst ? workspace.Scope : workspace.Tabs);
            context.Add(scopeFirst ? workspace.Tabs : workspace.Scope);
            controls.Add(context);
            while (workspace.Content.childCount > 0) body.Add(workspace.Content[0]);
            controls.Add(body);
            var result = new DeucarianThemingEditorFeatureGate(controls, audio, whenDisabled,
                workspace.Scope, workspace.Tabs, workspace.PageActions, workspace.Drawer, workspace.Footer);
            workspace.Content.Add(result.Root);
            result.workspace = workspace;
            result.body = body;
            result.Refresh();
            return result;
        }

        public void Refresh()
        {
            if (disposed) return;
            gate.Refresh();
            bool active = enabled();
            foreach (var control in related) control?.SetEnabled(active);
            if (workspace == null) return;
            if (!active && previous != false)
            {
                scopeWasStacked = workspace.Scope.ClassListContains("dw-scope-stacked");
                tabsDisplay = workspace.Tabs.style.display.value;
            }
            if (!active)
            {
                workspace.SetScopeStacked();
                DeucarianEditorWorkspaceControls.Show(workspace.Tabs, false);
                DeucarianEditorWorkspaceControls.Show(workspace.Scope, true);
            }
            else if (previous == false)
            {
                workspace.SetScopeStacked(scopeWasStacked);
                workspace.Tabs.style.display = tabsDisplay;
            }
            DeucarianEditorWorkspaceControls.Show(body, active);
            previous = active;
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            Unsubscribe();
            DeucarianThemeAssetChangeBus.AssetChanged += OnAssetChanged;
            EditorApplication.projectChanged += Refresh;
            Undo.undoRedoPerformed += Refresh;
            Refresh();
        }
        private void OnDetach(DetachFromPanelEvent evt) => Unsubscribe();
        private void OnAssetChanged(UnityEngine.Object asset) { if (asset is DeucarianThemeRuntimeSettings) Refresh(); }
        private void Unsubscribe()
        {
            DeucarianThemeAssetChangeBus.AssetChanged -= OnAssetChanged;
            EditorApplication.projectChanged -= Refresh;
            Undo.undoRedoPerformed -= Refresh;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Unsubscribe();
            Root.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            Root.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
        }
    }
}
