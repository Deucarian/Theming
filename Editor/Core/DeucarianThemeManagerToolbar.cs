using System;
using Deucarian.Editor;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemeManagerToolbar
    {
        private const float SecondaryActionSlotWidth = 132f;
        private const float DiscardActionSlotWidth = 148f;
        private const float PrimaryActionSlotWidth = 168f;
        private readonly Button themeViewButton;
        private readonly Button styleComposerViewButton;
        private readonly Button runtimeSettingsViewButton;
        private readonly Button toolbarSecondaryAction;
        private readonly Button toolbarPrimaryAction;
        private readonly Button discardChangesButton;
        private readonly VisualElement toolbarSecondarySlot;
        private readonly VisualElement discardChangesSlot;
        private readonly VisualElement toolbarPrimarySlot;
        private readonly VisualElement toolbarPrimaryStatus;

        internal DeucarianThemeManagerToolbar(VisualElement toolbar,
            Action navigateToTheme, Action navigateToStyleComposer, Action navigateToRuntimeSettings,
            Action executeToolbarSecondaryAction, Action discardAllChanges, Action executeToolbarPrimaryAction)
        {
            toolbar.Clear();
            var lanes = DeucarianEditorCommandBar.CreateLanes(toolbar);
            themeViewButton = DeucarianEditorCommandBar.CreateToggle(
                "Theme",
                navigateToTheme);
            themeViewButton.name = "deucarian-theme-manager-view-theme";
            styleComposerViewButton = DeucarianEditorCommandBar.CreateToggle(
                "Style Composer",
                navigateToStyleComposer,
                false,
                null,
                "Open the selected Visual Style in Style Composer.");
            styleComposerViewButton.name = "deucarian-theme-manager-view-style";
            runtimeSettingsViewButton = DeucarianEditorCommandBar.CreateToggle(
                "Runtime Settings",
                navigateToRuntimeSettings);
            runtimeSettingsViewButton.name = "deucarian-theme-manager-view-runtime-settings";
            toolbarSecondaryAction = DeucarianEditorCommandBar.CreateAction(
                DeucarianEditorIconIds.Wrench,
                string.Empty,
                executeToolbarSecondaryAction,
                false,
                "Open the contextual style or setup action.");
            toolbarSecondaryAction.name = "deucarian-theme-manager-toolbar-secondary";
            discardChangesButton = DeucarianEditorCommandBar.CreateAction(
                DeucarianEditorIconIds.Undo,
                "Discard changes",
                discardAllChanges,
                false,
                "Restore the active project theme and clear every unapplied draft.");
            discardChangesButton.name = "deucarian-theme-manager-discard-changes";
            toolbarPrimaryAction = DeucarianEditorCommandBar.CreateAction(
                DeucarianEditorIconIds.Check,
                string.Empty,
                executeToolbarPrimaryAction,
                true,
                "Apply the current staged theme selection.");
            toolbarPrimaryAction.name = "deucarian-theme-manager-toolbar-primary";

            toolbarSecondarySlot = DeucarianEditorCommandBar.CreateReservedSlot(
                SecondaryActionSlotWidth);
            discardChangesSlot = DeucarianEditorCommandBar.CreateReservedSlot(
                DiscardActionSlotWidth);
            toolbarPrimarySlot = DeucarianEditorCommandBar.CreateReservedSlot(
                PrimaryActionSlotWidth);
            toolbarPrimaryStatus = DeucarianEditorCommandBar.CreateState(
                DeucarianEditorIconIds.Check,
                "Active",
                "The staged selection is active in project runtime settings.");
            toolbarPrimaryStatus.name = "deucarian-theme-manager-toolbar-primary-status";
            DeucarianEditorCommandBar.SetReservedContent(
                toolbarSecondarySlot,
                toolbarSecondaryAction);
            DeucarianEditorCommandBar.SetReservedContent(
                discardChangesSlot,
                discardChangesButton,
                true);
            DeucarianEditorCommandBar.SetReservedContent(
                toolbarPrimarySlot,
                toolbarPrimaryAction);

            lanes.Leading.Add(themeViewButton);
            lanes.Leading.Add(styleComposerViewButton);
            lanes.Leading.Add(runtimeSettingsViewButton);
            lanes.Trailing.Add(toolbarSecondarySlot);
            lanes.Trailing.Add(discardChangesSlot);
            lanes.Trailing.Add(toolbarPrimarySlot);
        }

        internal void SetSelection(bool theme, bool composer, bool runtimeSettings, bool styleAvailable)
        {
            DeucarianEditorCommandBar.SetActive(themeViewButton, theme);
            DeucarianEditorCommandBar.SetActive(styleComposerViewButton, composer);
            DeucarianEditorCommandBar.SetActive(runtimeSettingsViewButton, runtimeSettings);
            styleComposerViewButton.SetEnabled(styleAvailable);
            styleComposerViewButton.tooltip = styleAvailable
                ? "Open the selected Visual Style in Style Composer."
                : "Choose a Visual Style on the Theme tab before opening Style Composer.";
        }

        internal void SetSecondary(string text, bool enabled, string tooltip)
        {
            DeucarianEditorCommandBar.SetReservedVisible(toolbarSecondarySlot, true);
            Configure(toolbarSecondaryAction, text, enabled, tooltip);
        }

        internal void HideSecondary()
        {
            DeucarianEditorCommandBar.SetReservedVisible(toolbarSecondarySlot, false);
        }

        internal void SetPrimary(string text, bool enabled, string tooltip)
        {
            Configure(toolbarPrimaryAction, text, enabled, tooltip);
            ShowPrimary(toolbarPrimaryAction);
        }

        internal void ShowActive() => ShowPrimary(toolbarPrimaryStatus);

        internal void SetPendingChanges(int count, bool isPlaying)
        {
            bool visible = count > 0;
            bool canDiscard = visible && !isPlaying;
            DeucarianEditorCommandBar.SetReservedVisible(discardChangesSlot, true);
            discardChangesButton.SetEnabled(canDiscard);
            discardChangesButton.tooltip = canDiscard
                ? "Restore the active project theme and clear every unapplied draft."
                : visible ? "Exit Play Mode before discarding staged changes."
                : "There are no unapplied changes to discard.";
        }

        private void ShowPrimary(VisualElement content)
        {
            if (content.parent != toolbarPrimarySlot)
                DeucarianEditorCommandBar.SetReservedContent(toolbarPrimarySlot, content);
        }

        private static void Configure(Button button, string text, bool enabled, string tooltip)
        {
            DeucarianEditorCommandBar.SetText(button, text);
            button.SetEnabled(enabled);
            button.tooltip = tooltip;
        }
    }
}
