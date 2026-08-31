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


        private static void DrawStatus(DeucarianThemeManagerActivationStatus status)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string label;
                DeucarianEditorStatus visualStatus;
                if (!status.HasRuntimeSettings)
                {
                    label = "Setup required";
                    visualStatus = DeucarianEditorStatus.Error;
                }
                else if (!status.RuntimeSettingsReady)
                {
                    label = "Setup incomplete";
                    visualStatus = DeucarianEditorStatus.Warning;
                }
                else if (!status.SelectionValid)
                {
                    label = "Incomplete";
                    visualStatus = DeucarianEditorStatus.Warning;
                }
                else if (status.IsActive)
                {
                    label = "Active";
                    visualStatus = DeucarianEditorStatus.Success;
                }
                else if (!status.HasDraftChanges)
                {
                    label = "Needs sync";
                    visualStatus = DeucarianEditorStatus.Warning;
                }
                else
                {
                    label = "Not active";
                    visualStatus = DeucarianEditorStatus.Info;
                }

                DeucarianEditorStatusBadge.Draw(label, visualStatus, GUILayout.Width(112f));
                EditorGUILayout.LabelField(status.Message, DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
            }
        }

        private static void DrawResolvedSummary(DeucarianThemeManagerSelection selection)
        {
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Resolved Theme",
                selection.ResolvedTheme != null ? selection.ResolvedTheme.DisplayName : "Not resolved",
                "Derived from the selected family and mode.");
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Palette",
                selection.ResolvedPalette != null ? selection.ResolvedPalette.DisplayName : "Not resolved",
                "Derived from the resolved theme.");
        }

        private static void DrawStyleSummary(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return;
            }

            GUILayout.Space(2f);
            EditorGUILayout.LabelField("Appearance", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
            const string tooltip = "This value is composed by the selected visual style. Use Style Composer to change it.";
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Surface",
                style.SurfaceProfile != null ? style.SurfaceProfile.DisplayName : "Legacy inline",
                tooltip);
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Corners",
                style.ShapeProfile != null ? style.ShapeProfile.DisplayName : "Legacy inline",
                tooltip);
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Border",
                style.StrokeProfile != null ? style.StrokeProfile.DisplayName : "Legacy inline",
                tooltip);
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Size",
                style.Density == DeucarianThemeDensity.Unspecified
                    ? "Legacy automatic"
                    : style.Density.ToString(),
                tooltip);
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Typography",
                style.TypographyProfile != null
                    ? style.TypographyProfile.DisplayName
                    : "Project TMP default",
                tooltip);
        }

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
                    runtimeSettingsCandidate = (DeucarianThemeRuntimeSettings)DrawWorkbenchObjectField(
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

        internal static bool CanCreateRuntimeSettings(
            int existingRuntimeSettingsCount,
            bool isPlaying)
        {
            return existingRuntimeSettingsCount == 0 && !isPlaying;
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
            if (composerSource == null)
            {
                ReturnToTheme("Choose a visual style before customizing it.", MessageType.Warning);
                return;
            }

            DrawStyleComposerContext();

            string composerTitle = composerEditingStyle != null
                ? composerEditingStyle.DisplayName
                : composerSource.DisplayName;
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
                BuildComposerSaveDescription(composerEditingStyle != null),
                MessageType.Info);

            bool complete = IsComposerComplete();
            DeucarianThemeManagerSelection candidate = new DeucarianThemeManagerSelection(
                DeucarianThemingEditorSettings.ActiveThemeFamily,
                DeucarianThemingEditorSettings.ActiveThemeMode,
                composerEditingStyle ?? composerSource);
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
            DeucarianThemeStyle comparison = composerEditingStyle != null
                ? composerEditingStyle
                : composerSource;

            composerSurface = (DeucarianThemeSurfaceProfile)DrawWorkbenchObjectField(
                DirtyLabel("Surface", composerSurface != comparison.SurfaceProfile),
                composerSurface,
                typeof(DeucarianThemeSurfaceProfile),
                false);
            composerCorners = (DeucarianThemeShapeProfile)DrawWorkbenchObjectField(
                DirtyLabel("Corners", composerCorners != comparison.ShapeProfile),
                composerCorners,
                typeof(DeucarianThemeShapeProfile),
                false);
            composerBorder = (DeucarianThemeStrokeProfile)DrawWorkbenchObjectField(
                DirtyLabel("Border", composerBorder != comparison.StrokeProfile),
                composerBorder,
                typeof(DeucarianThemeStrokeProfile),
                false);
            composerSize = (DeucarianThemeDensity)DrawWorkbenchEnumPopup(
                DirtyLabel("Size", composerSize != comparison.Density),
                composerSize);
            composerTypography = (DeucarianThemeTypographyProfile)DrawWorkbenchObjectField(
                DirtyLabel("Typography", composerTypography != comparison.TypographyProfile),
                composerTypography,
                typeof(DeucarianThemeTypographyProfile),
                false);
        }
    }
}
