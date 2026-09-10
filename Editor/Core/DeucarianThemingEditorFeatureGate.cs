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
        public VisualElement Root => gate.Root;

        public DeucarianThemingEditorFeatureGate(VisualElement controls, bool audio, Action whenDisabled = null,
            params VisualElement[] related)
        {
            this.related = related ?? Array.Empty<VisualElement>();
            enabled = audio ? (Func<bool>)(() => DeucarianThemeRuntimeResolver.UseAudio)
                : () => DeucarianThemeRuntimeResolver.UseVisualStyling;
            string feature = audio ? "Audio" : "Visual styling";
            gate = new DeucarianEditorCapabilityGate(controls, enabled, feature + " is off",
                "Your palettes are saved. Enable " + feature.ToLowerInvariant() + " in Project setup to use them.",
                "Go to Project setup", () => DeucarianEditorNavigation.Open(Root, DeucarianThemingProjectPage.ToolId), whenDisabled);
            Root.RegisterCallback<AttachToPanelEvent>(OnAttach);
            Root.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            Refresh();
        }

        public static DeucarianThemingEditorFeatureGate Wrap(DeucarianEditorWorkspace workspace, bool audio, Action whenDisabled = null)
        {
            var controls = DeucarianEditorWorkspaceControls.Region(null, "dw-gated-content");
            controls.style.flexGrow = 1;
            controls.style.minHeight = 0;
            while (workspace.Content.childCount > 0) controls.Add(workspace.Content[0]);
            var result = new DeucarianThemingEditorFeatureGate(controls, audio, whenDisabled,
                workspace.Scope, workspace.Tabs, workspace.PageActions, workspace.Drawer, workspace.Footer);
            workspace.Content.Add(result.Root);
            return result;
        }

        public void Refresh()
        {
            if (disposed) return;
            gate.Refresh();
            foreach (var control in related) control?.SetEnabled(enabled());
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
