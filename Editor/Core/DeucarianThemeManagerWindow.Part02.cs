using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianThemeManagerWindow
    {

        private void UpdateWorkbenchFooter()
        {
            if (workbenchFooter == null)
            {
                return;
            }

            DeucarianEditorStatus visualStatus;
            string statusLabel;
            string summary;
            string iconId;

            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            string familyName = selection.Family != null ? selection.Family.DisplayName : "No family";
            string styleName = selection.Style != null ? selection.Style.DisplayName : "No style";
            DeucarianThemeManagerActivationStatus status =
                DeucarianThemeManagerWorkflow.Evaluate(
                    projectRuntimeSettings,
                    selection,
                    projectRuntimeSettingsResourceReady,
                    projectRuntimeSettingsResourceMessage);
            int pendingCount = currentPendingChanges != null ? currentPendingChanges.Count : 0;
            if (pendingCount > 0)
            {
                visualStatus = DeucarianEditorStatus.Warning;
                statusLabel = pendingCount + (pendingCount == 1 ? " unapplied change" : " unapplied changes");
                summary = string.Join(" · ", currentPendingChanges);
                iconId = DeucarianEditorIconIds.Warning;
            }
            else if (!string.IsNullOrWhiteSpace(feedbackMessage))
            {
                visualStatus = ToEditorStatus(feedbackType);
                statusLabel = feedbackType == MessageType.Error
                    ? "Error"
                    : feedbackType == MessageType.Warning ? "Attention" : "Updated";
                summary = feedbackMessage;
                iconId = feedbackType == MessageType.Info
                    ? DeucarianEditorIconIds.Info
                    : DeucarianEditorIconIds.Warning;
            }
            else if (status.IsActive)
            {
                visualStatus = DeucarianEditorStatus.Success;
                statusLabel = "Active";
                summary = $"{familyName} · {selection.Mode} · {styleName}";
                iconId = DeucarianEditorIconIds.Check;
            }
            else if (!status.CanActivate)
            {
                visualStatus = DeucarianEditorStatus.Warning;
                statusLabel = "Attention";
                summary = status.Message;
                iconId = DeucarianEditorIconIds.Warning;
            }
            else
            {
                visualStatus = DeucarianEditorStatus.Info;
                statusLabel = "Ready";
                summary = $"{familyName} · {selection.Mode} · {styleName}";
                iconId = DeucarianEditorIconIds.Info;
            }

            workbenchFooter.StatusLabel.text = statusLabel;
            workbenchFooter.Summary.text = summary;
            workbenchFooter.Status.tooltip = pendingCount > 0
                ? string.Join("\n", currentPendingChanges)
                : summary;
            workbenchFooter.Summary.tooltip = workbenchFooter.Status.tooltip;
            workbenchFooter.Version.text = $"com.deucarian.theming {ResolvePackageVersion()}";
            DeucarianEditorWorkbenchSurfaces.SetFooterIcon(workbenchFooter, iconId);
            DeucarianEditorWorkbenchSurfaces.SetFooterStatus(workbenchFooter, visualStatus);
            DeucarianEditorWorkbenchSurfaces.SetFooterBusy(workbenchFooter, false);
        }

        private static DeucarianEditorStatus ToEditorStatus(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Error:
                    return DeucarianEditorStatus.Error;
                case MessageType.Warning:
                    return DeucarianEditorStatus.Warning;
                default:
                    return DeucarianEditorStatus.Info;
            }
        }

        private void NavigateToTheme()
        {
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
            viewMode = ViewMode.Theme;
            feedbackMessage = null;
            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void NavigateToStyleComposer()
        {
            DeucarianThemeStyle style = DeucarianThemingEditorSettings.ActiveStyle;
            if (style == null)
            {
                viewMode = ViewMode.Theme;
                feedbackMessage = "Choose a visual style before opening the composer.";
                feedbackType = MessageType.Warning;
                UpdateWorkbenchToolbar();
                Repaint();
                return;
            }

            EnterStyleComposer(style, false);
        }

        private bool EnterStyleComposer(DeucarianThemeStyle style, bool stageSelection)
        {
            if (style == null)
            {
                return false;
            }

            bool sameSource = composer.Source == style;
            if (!sameSource && IsComposerDraftDirty())
            {
                bool keepEditing = DeucarianThemeEditorConfirmations.ShouldKeepCurrentComposerDraft(
                    GetStyleDisplayName(composer.Source),
                    GetStyleDisplayName(style));
                if (keepEditing)
                {
                    viewMode = ViewMode.StyleComposer;
                    feedbackMessage = "Continuing the existing style composer draft.";
                    feedbackType = MessageType.Info;
                    ApplyComposerPreview();
                    UpdateWorkbenchToolbar();
                    Repaint();
                    return false;
                }
            }

            if (stageSelection)
            {
                DeucarianThemeDraftPolicy.SetDraft(
                    DeucarianThemingEditorSettings.ActiveThemeFamily,
                    DeucarianThemingEditorSettings.ActiveThemeMode,
                    style);
            }

            if (sameSource)
            {
                viewMode = ViewMode.StyleComposer;
                feedbackMessage = null;
                ApplyComposerPreview();
                UpdateWorkbenchToolbar();
                Repaint();
            }
            else
            {
                BeginStyleComposer(style);
            }

            return true;
        }

        private static string GetStyleDisplayName(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(style.DisplayName)
                ? style.name
                : style.DisplayName;
        }

        internal static string BuildComposerSaveDescription(bool updatesExistingStyle)
        {
            string operation = updatesExistingStyle
                ? "updates that file with all choices below"
                : "creates that file from all choices below";
            return "A complete reusable Custom Style is stored as one Unity .asset file. "
                   + "Save Style & Activate "
                   + operation
                   + ", then assigns it to both Light and Dark themes in the selected family.";
        }

        private void NavigateToRuntimeSettings()
        {
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
            runtimeSettingsCandidate = projectRuntimeSettings;
            runtimeCandidateTouched = false;
            validatedRuntimeSettingsCandidate = null;
            RefreshRuntimeSettingsCandidateValidation();
            viewMode = ViewMode.RuntimeSettings;
            feedbackMessage = null;
            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void ExecuteToolbarSecondaryAction()
        {
            switch (viewMode)
            {
                case ViewMode.StyleComposer:
                    ShowComposerMenu();
                    break;
                case ViewMode.RuntimeSettings:
                    CreateRuntimeSettingsFromSavePanel();
                    break;
            }

            UpdateWorkbenchToolbar();
        }

        private void ExecuteToolbarPrimaryAction()
        {
            switch (viewMode)
            {
                case ViewMode.StyleComposer:
                    SaveAndActivateComposer(false);
                    break;
                case ViewMode.RuntimeSettings:
                    UseRuntimeSettingsCandidate();
                    break;
                default:
                    Activate(DeucarianThemeManagerSelection.FromEditorPrefs());
                    break;
            }

            UpdateWorkbenchToolbar();
            Repaint();
        }

    }
}
