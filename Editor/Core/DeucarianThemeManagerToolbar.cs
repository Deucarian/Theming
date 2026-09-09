using System;
using Deucarian.Editor;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemeManagerToolbar
    {
        private readonly DeucarianEditorChoiceBar tabs;
        private readonly Button secondary;
        private readonly Button primary;
        private readonly Button discard;

        internal DeucarianThemeManagerToolbar(DeucarianEditorWorkspace workspace,
            Action navigateToTheme, Action navigateToStyleComposer, Action navigateToRuntimeSettings,
            Action executeSecondary, Action discardAllChanges, Action executePrimary)
        {
            tabs = new DeucarianEditorChoiceBar(new[] { "Theme", "Style Composer", "Project setup" }, 0, true);
            tabs.Changed += value => { if (value == 0) navigateToTheme(); else if (value == 1) navigateToStyleComposer(); else navigateToRuntimeSettings(); };
            tabs.ElementAt(0).name = "deucarian-theme-manager-view-theme";
            tabs.ElementAt(1).name = "deucarian-theme-manager-view-style";
            tabs.ElementAt(2).name = "deucarian-theme-manager-view-runtime-settings";
            workspace.Tabs.Add(tabs);
            secondary = DeucarianEditorWorkspaceControls.Button("More", executeSecondary);
            secondary.name = "deucarian-theme-manager-toolbar-secondary";
            discard = DeucarianEditorWorkspaceControls.Button("Discard changes", discardAllChanges);
            discard.name = "deucarian-theme-manager-discard-changes";
            primary = DeucarianEditorWorkspaceControls.Button("Apply to project", executePrimary, true);
            primary.name = "deucarian-theme-manager-toolbar-primary";
            workspace.PageActions.Add(secondary);
            workspace.PageActions.Add(discard);
            workspace.PageActions.Add(primary);
        }

        internal void SetSelection(bool theme, bool composer, bool runtimeSettings, bool styleAvailable)
        {
            tabs.SetValueWithoutNotify(composer ? 1 : runtimeSettings ? 2 : 0);
            tabs.SetChoiceEnabled(1, styleAvailable, styleAvailable ? "Compose selected style" : "Choose a visual style first.");
        }
        internal void SetSecondary(string text, bool enabled, string tooltip)
        {
            DeucarianEditorWorkspaceControls.Show(secondary, true);
            Configure(secondary, text, enabled, tooltip);
        }
        internal void HideSecondary() => DeucarianEditorWorkspaceControls.Show(secondary, false);
        internal void SetPrimary(string text, bool enabled, string tooltip) => Configure(primary, text == "Activate" ? "Apply to project" : text, enabled, tooltip);
        internal void ShowActive() => Configure(primary, "Active in project", false, "This preview matches the active project theme.");
        internal void SetPendingChanges(int count, bool isPlaying)
        {
            DeucarianEditorWorkspaceControls.Show(discard, count > 0);
            discard.SetEnabled(count > 0 && !isPlaying);
        }
        private static void Configure(Button button, string text, bool enabled, string tooltip)
        {
            button.text = text;
            button.SetEnabled(enabled);
            button.tooltip = tooltip;
        }
    }
}
