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
                    DeucarianEditorTextGUI.LabelField(composerTitle, DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    DeucarianEditorTextGUI.LabelField(
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
                    DeucarianEditorTextGUI.LabelField("Live Preview", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    DeucarianEditorTextGUI.LabelField(
                        "The preview uses the staged palette and the source font when TMP exposes it.",
                        DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
                    GUILayout.Space(6f);
                    DrawComposerPreview();
                });

            DeucarianEditorTextGUI.HelpBox(
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
                DeucarianEditorTextGUI.HelpBox(
                    "Choose Surface, Corners, Border, and Size before saving.",
                    MessageType.Warning);
            }
            else if (!projectReady)
            {
                DeucarianEditorTextGUI.HelpBox(
                    "Complete the project theme setup before saving and activating this style.",
                    MessageType.Warning);
            }

        }

        private void DrawStyleComposerContext()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DeucarianEditorTextGUI.LabelField(
                    "Style Composer",
                    DeucarianEditorWorkbenchGUI.BoldLabelStyle,
                    GUILayout.ExpandWidth(true));
                var backContent = new GUIContent(
                    "Back to Theme",
                    "Return to Theme without clearing the current composer draft.");
                bool back = DeucarianEditorActionGUI.Button(
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
