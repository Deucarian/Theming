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

        private void DrawContextualSetup(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            DeucarianThemeManagerActivationStatus status)
        {
            if (!status.RuntimeSettingsReady)
            {
                DrawSectionHeading("Runtime Setup");
                EditorGUILayout.HelpBox(
                    status.HasRuntimeSettings
                        ? status.Message
                        : "A source-controlled runtime settings asset connects editor activation to builds.",
                    MessageType.Warning);
                if (DeucarianEditorWorkbenchGUI.DrawCompactIconAction(
                        DeucarianEditorIconIds.Wrench,
                        "Configure Runtime Settings...",
                        "Open the runtime settings setup view.",
                        !EditorApplication.isPlayingOrWillChangePlaymode,
                        true))
                {
                    runtimeSettingsCandidate = settings;
                    runtimeCandidateTouched = false;
                    validatedRuntimeSettingsCandidate = null;
                    viewMode = ViewMode.RuntimeSettings;
                    feedbackMessage = null;
                    UpdateWorkbenchToolbar();
                    GUIUtility.ExitGUI();
                }
                return;
            }

            if (selection.Family == null)
            {
                DrawSectionHeading("Choose a Theme Family");
                EditorGUILayout.HelpBox(
                    "No family is selected. Choose an existing family above or create one.",
                    MessageType.Info);
                if (DeucarianEditorWorkbenchGUI.DrawCompactIconAction(
                        DeucarianEditorIconIds.CreatePackage,
                        "Create Theme Family...",
                        "Create a complete theme family asset.",
                        !EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    CreateThemeFamily();
                }
            }
            else if (!selection.Family.IsComplete)
            {
                DrawSectionHeading("Family Needs Repair");
                EditorGUILayout.HelpBox(
                    "Both a Light and Dark theme are required before activation.",
                    MessageType.Warning);
                if (DeucarianEditorWorkbenchGUI.DrawCompactIconAction(
                        DeucarianEditorIconIds.Wrench,
                        "Repair Selected Family",
                        "Repair the selected family without replacing customized profiles.",
                        !EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    DeucarianThemingMenuActions.RepairActiveThemeFamilySetup();
                    RefreshAssets();
                }
            }
        }

        private void DrawRuntimeSettingsSetup()
        {
            DeucarianEditorWorkbenchGUI.DrawPanel(
                "Configure Project",
                () =>
                {
                    EditorGUILayout.HelpBox(
                        "The project loads exactly one runtime settings asset. It selects one starting mode, while its theme family still contains both Light and Dark themes.",
                        MessageType.Info);
                    GUILayout.Space(4f);
                    EditorGUI.BeginChangeCheck();
                    runtimeSettingsCandidate = (DeucarianThemeRuntimeSettings)DeucarianThemeManagerFields.DrawWorkbenchObjectField(
                        "Existing Settings",
                        runtimeSettingsCandidate,
                        typeof(DeucarianThemeRuntimeSettings),
                        false);
                    if (EditorGUI.EndChangeCheck()
                        || validatedRuntimeSettingsCandidate != runtimeSettingsCandidate)
                    {
                        runtimeCandidateTouched = true;
                        RefreshRuntimeSettingsCandidateValidation();
                    }

                    if (runtimeSettingsCandidate != null)
                    {
                        if (!runtimeSettingsCandidateValid)
                        {
                            EditorGUILayout.HelpBox(
                                runtimeSettingsCandidateMessage,
                                MessageType.Warning);
                        }
                    }

                    DeucarianThemeManagerSelection draft =
                        DeucarianThemeManagerSelection.FromEditorPrefs();
                    bool draftFamilyReady =
                        DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(draft.Family);
                    bool candidateNeedsFamily = runtimeSettingsCandidate != null
                                                && !DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(
                                                    runtimeSettingsCandidate.DefaultThemeFamily);
                    if (candidateNeedsFamily && !draftFamilyReady)
                    {
                        EditorGUILayout.HelpBox(
                            "Choose or repair a complete staged family before configuring these settings.",
                            MessageType.Info);
                    }
                });

        }

        private bool RuntimeSettingsCandidateNeedsFamily()
        {
            return runtimeSettingsCandidate != null
                   && !DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(
                       runtimeSettingsCandidate.DefaultThemeFamily);
        }

        private bool CanUseRuntimeSettingsCandidate()
        {
            DeucarianThemeManagerSelection draft =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            return runtimeSettingsCandidateValid
                   && (!RuntimeSettingsCandidateNeedsFamily()
                       || DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(draft.Family))
                   && !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private void UseRuntimeSettingsCandidate()
        {
            if (!CanUseRuntimeSettingsCandidate())
            {
                return;
            }

            AssetDatabase.Refresh();
            RefreshRuntimeSettingsValidation();
            RefreshRuntimeSettingsCandidateValidation();
            if (!runtimeSettingsCandidateValid)
            {
                feedbackMessage = runtimeSettingsCandidateMessage;
                feedbackType = MessageType.Error;
                UpdateWorkbenchToolbar();
                return;
            }

            if (RuntimeSettingsCandidateNeedsFamily())
            {
                DeucarianThemeManagerSelection draft =
                    DeucarianThemeManagerSelection.FromEditorPrefs();
                Undo.RecordObject(runtimeSettingsCandidate, "Configure Deucarian Runtime Settings");
                runtimeSettingsCandidate.Configure(draft.Family, draft.Mode);
                EditorUtility.SetDirty(runtimeSettingsCandidate);
                AssetDatabase.SaveAssetIfDirty(runtimeSettingsCandidate);
            }

            ReturnToTheme("Runtime settings are ready.", MessageType.Info);
            CaptureBaseline();
        }

        private void DrawStyleComposer()
        {
            if (composer.Source == null)
            {
                ReturnToTheme("Choose a visual style before customizing it.", MessageType.Warning);
                return;
            }

            DrawStyleComposerContext();

            string composerTitle = composer.EditingStyle != null
                ? composer.EditingStyle.DisplayName
                : composer.Source.DisplayName;
            DrawFlatSplit(
                () =>
                {
                    EditorGUILayout.LabelField(composerTitle, DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    EditorGUILayout.LabelField(
                        "Compose one complete reusable Custom Style. Surface, Corners, Border, and Size are required; Typography is optional.",
                        DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
                    GUILayout.Space(6f);
                    EditorGUI.BeginChangeCheck();
                    DrawComposerFields();
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplyComposerPreview();
                        UpdateWorkbenchToolbar();
                    }
                },
                () =>
                {
                    EditorGUILayout.LabelField("Live Preview", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    EditorGUILayout.LabelField(
                        "The preview uses the staged palette and the source font when TMP exposes it.",
                        DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
                    GUILayout.Space(6f);
                    DrawComposerPreview();
                });

            EditorGUILayout.HelpBox(
                BuildComposerSaveDescription(composer.EditingStyle != null),
                MessageType.Info);

            bool complete = IsComposerComplete();
            DeucarianThemeManagerSelection candidate = new DeucarianThemeManagerSelection(
                DeucarianThemingEditorSettings.ActiveThemeFamily,
                DeucarianThemingEditorSettings.ActiveThemeMode,
                composer.EditingStyle ?? composer.Source);
            DeucarianThemeRuntimeSettings settings = projectRuntimeSettings;
            bool projectReady = settings != null
                                && projectRuntimeSettingsResourceReady
                                && DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(
                                    candidate.Family);

            if (!complete)
            {
                EditorGUILayout.HelpBox(
                    "Choose Surface, Corners, Border, and Size before saving.",
                    MessageType.Warning);
            }
            else if (!projectReady)
            {
                EditorGUILayout.HelpBox(
                    "Complete the project theme setup before saving and activating this style.",
                    MessageType.Warning);
            }

        }

        private void DrawStyleComposerContext()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "Style Composer",
                    DeucarianEditorWorkbenchGUI.BoldLabelStyle,
                    GUILayout.ExpandWidth(true));
                var backContent = new GUIContent(
                    "Back to Theme",
                    "Return to Theme without clearing the current composer draft.");
                bool back = GUILayout.Button(
                    backContent,
                    DeucarianEditorWorkbenchGUI.LabelStyle,
                    GUILayout.ExpandWidth(false),
                    GUILayout.Height(DeucarianEditorLayoutMetrics.TextLineHeight));
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                if (back)
                {
                    NavigateToTheme();
                }
            }

            GUILayout.Space(DeucarianEditorSpacing.Small);
        }

        private bool IsComposerReadyToActivate()
        {
            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            return IsComposerComplete()
                   && projectRuntimeSettings != null
                   && projectRuntimeSettingsResourceReady
                   && DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(selection.Family);
        }

        private void DrawComposerFields()
        {
            DeucarianThemeStyle comparison = composer.EditingStyle != null
                ? composer.EditingStyle
                : composer.Source;

            composer.Surface = (DeucarianThemeSurfaceProfile)DeucarianThemeManagerFields.DrawWorkbenchObjectField(
                DirtyLabel("Surface", composer.Surface != comparison.SurfaceProfile),
                composer.Surface,
                typeof(DeucarianThemeSurfaceProfile),
                false);
            composer.Corners = (DeucarianThemeShapeProfile)DeucarianThemeManagerFields.DrawWorkbenchObjectField(
                DirtyLabel("Corners", composer.Corners != comparison.ShapeProfile),
                composer.Corners,
                typeof(DeucarianThemeShapeProfile),
                false);
            composer.Border = (DeucarianThemeStrokeProfile)DeucarianThemeManagerFields.DrawWorkbenchObjectField(
                DirtyLabel("Border", composer.Border != comparison.StrokeProfile),
                composer.Border,
                typeof(DeucarianThemeStrokeProfile),
                false);
            composer.Size = (DeucarianThemeDensity)DeucarianThemeManagerFields.DrawWorkbenchEnumPopup(
                DirtyLabel("Size", composer.Size != comparison.Density),
                composer.Size);
            composer.Typography = (DeucarianThemeTypographyProfile)DeucarianThemeManagerFields.DrawWorkbenchObjectField(
                DirtyLabel("Typography", composer.Typography != comparison.TypographyProfile),
                composer.Typography,
                typeof(DeucarianThemeTypographyProfile),
                false);
        }
    }
}
