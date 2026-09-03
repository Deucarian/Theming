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

        private void ShowPrimaryActionButton()
        {
            if (toolbarPrimarySlot != null && toolbarPrimaryAction?.parent != toolbarPrimarySlot)
            {
                DeucarianEditorCommandBar.SetReservedContent(
                    toolbarPrimarySlot,
                    toolbarPrimaryAction);
            }
        }

        private void ShowPrimaryActiveStatus()
        {
            if (toolbarPrimarySlot != null && toolbarPrimaryStatus?.parent != toolbarPrimarySlot)
            {
                DeucarianEditorCommandBar.SetReservedContent(
                    toolbarPrimarySlot,
                    toolbarPrimaryStatus);
            }
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

            bool sameSource = composerSource == style;
            if (!sameSource && IsComposerDraftDirty())
            {
                bool keepEditing = ShouldKeepCurrentComposerDraft(
                    GetStyleDisplayName(composerSource),
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
                SetDraft(
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

        internal static bool ShouldKeepCurrentComposerDraft(
            string currentStyleName,
            string requestedStyleName,
            Func<string, string, string, string, string, int> showDialog = null)
        {
            Func<string, string, string, string, string, int> dialog = showDialog
                ?? EditorUtility.DisplayDialogComplex;
            string current = string.IsNullOrWhiteSpace(currentStyleName)
                ? "the current style"
                : currentStyleName;
            string requested = string.IsNullOrWhiteSpace(requestedStyleName)
                ? "the selected style"
                : requestedStyleName;
            int choice = dialog(
                "Keep Style Composer Changes?",
                $"{current} has unapplied composer changes. Keep editing it, or discard those composer changes and switch to {requested}?",
                "Keep editing",
                "Cancel",
                "Discard draft and switch");
            return choice != 2;
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

        private void DrawWindowGui()
        {
            using (DeucarianEditorWorkbenchGUI.BeginEmbeddedPage(GUILayout.ExpandHeight(true)))
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    scrollPosition = scrollView.scrollPosition;
                    GUILayout.Space(8f);

                    switch (viewMode)
                    {
                        case ViewMode.StyleComposer:
                            DrawStyleComposer();
                            break;
                        case ViewMode.RuntimeSettings:
                            DrawRuntimeSettingsSetup();
                            break;
                        default:
                            DrawThemeManager();
                            break;
                    }

                    UpdateWorkbenchToolbar();
                    GUILayout.Space(8f);
                }
            }
        }

        private void DrawThemeManager()
        {
            EnsureSearchResult();

            DeucarianThemeRuntimeSettings settings = projectRuntimeSettings;
            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            DeucarianThemeManagerActivationStatus status =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    selection,
                    projectRuntimeSettingsResourceReady,
                    projectRuntimeSettingsResourceMessage);

            DrawFlatSplit(
                () =>
                {
                    EditorGUILayout.LabelField("Current Theme", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    EditorGUILayout.LabelField(
                        "Stage the project family, mode, and shared visual style.",
                        DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
                    GUILayout.Space(6f);
                    DrawCurrentThemeCard(selection, status);
                },
                () =>
                {
                    EditorGUILayout.LabelField("Live Preview", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    EditorGUILayout.LabelField(
                        "Palette, surfaces, controls, status, and typography in one specimen.",
                        DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
                    GUILayout.Space(6f);
                    DrawThemePreview(
                        selection.ResolvedTheme,
                        selection.Style,
                        selection.Style != null ? selection.Style.SurfaceProfile : null,
                        selection.Style != null ? selection.Style.ShapeProfile : null,
                        selection.Style != null ? selection.Style.StrokeProfile : null,
                        selection.Style != null ? selection.Style.Density : DeucarianThemeDensity.Unspecified,
                        selection.Style != null ? selection.Style.TypographyProfile : null);
                });

            GUILayout.Space(8f);

            DrawContextualSetup(settings, selection, status);
        }

        private void DrawCurrentThemeCard(
            DeucarianThemeManagerSelection selection,
            DeucarianThemeManagerActivationStatus status)
        {
            DrawStatus(status);
            GUILayout.Space(8f);

            DrawAssetDropdown(
                DirtyLabel("Theme Family", status.FamilyDirty),
                selection.Family,
                searchResult.ThemeFamilies,
                family =>
                {
                    DeucarianThemeStyle suggestedStyle = ResolveSuggestedStyle(family, selection.Mode)
                                                         ?? selection.Style;
                    SetDraft(family, selection.Mode, suggestedStyle);
                    UpdateWorkbenchToolbar();
                    Repaint();
                });

            EditorGUI.BeginChangeCheck();
            DeucarianThemeMode mode = (DeucarianThemeMode)DrawWorkbenchEnumPopup(
                DirtyLabel("Mode", status.ModeDirty),
                selection.Mode);
            if (EditorGUI.EndChangeCheck())
            {
                SetDraft(selection.Family, mode, selection.Style);
                UpdateWorkbenchToolbar();
                GUIUtility.ExitGUI();
            }

            DrawAssetDropdown(
                DirtyLabel("Visual Style", status.StyleDirty),
                selection.Style,
                searchResult.Styles,
                style =>
                {
                    SetDraft(selection.Family, selection.Mode, style);
                    UpdateWorkbenchToolbar();
                    Repaint();
                });

            GUILayout.Space(6f);
            DrawResolvedSummary(selection);
            DrawStyleSummary(selection.Style);
            DrawAudioSummary(selection.ResolvedTheme);
        }

        private static void DrawAudioSummary(DeucarianTheme theme)
        {
            DeucarianAudioPaletteSet set = theme != null ? theme.AudioPaletteSet : null;
            DeucarianAudioExperience previewExperience =
                DeucarianAudioPaletteLabWindow.PreviewExperience;
            DeucarianAudioPalette resolved = set != null
                ? set.GetPalette(previewExperience) ?? set.DefaultPalette
                : null;
            int warningCount = set != null ? set.GetValidationWarnings().Count : 1;

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Audio Palette", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
            EditorGUILayout.LabelField(
                set != null
                    ? $"{set.name} · {previewExperience} · "
                      + (resolved != null ? resolved.DisplayName : "Missing palette")
                    : "No Audio Palette Set is linked to the resolved theme.",
                DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
            EditorGUILayout.LabelField(
                warningCount == 0 ? "Audio validation: ready" : $"Audio validation: {warningCount} issue(s)",
                DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
            using (new EditorGUI.DisabledScope(set == null))
            {
                if (GUILayout.Button("Open Audio Palette Lab"))
                {
                    DeucarianAudioPaletteLabWindow.Open(set);
                }
            }
        }
    }
}
