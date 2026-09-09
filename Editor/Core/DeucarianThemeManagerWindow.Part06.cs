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

        private static string DirtyLabel(string label, bool dirty)
        {
            return dirty ? label + " *" : label;
        }

        internal static bool ShouldStackPreview(float width)
        {
            return width < PreviewStackBreakpoint;
        }

        private void DrawFlatSplit(Action drawConfiguration, Action drawPreview)
        {
            if (ShouldStackPreview(position.width))
            {
                drawConfiguration?.Invoke();
                GUILayout.Space(10f);
                DeucarianEditorWorkbenchGUI.DrawSeparator();
                GUILayout.Space(8f);
                drawPreview?.Invoke();
                return;
            }

            float configurationWidth = Mathf.Clamp(position.width * 0.40f, 300f, 370f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(configurationWidth)))
                {
                    drawConfiguration?.Invoke();
                }

                GUILayout.Space(12f);
                Rect divider = GUILayoutUtility.GetRect(
                    1f,
                    232f,
                    GUILayout.Width(1f),
                    GUILayout.ExpandHeight(true));
                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(divider, DeucarianEditorTheme.BorderSubtle);
                }

                GUILayout.Space(12f);
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    drawPreview?.Invoke();
                }
            }
        }

        private static void DrawSectionHeading(string heading)
        {
            GUILayout.Space(8f);
            EditorGUILayout.LabelField(heading, DeucarianEditorWorkbenchGUI.BoldLabelStyle);
        }

        private IReadOnlyList<string> GetPendingChangeDescriptions(
            DeucarianThemeManagerActivationStatus status)
        {
            DeucarianThemeStyle comparison = composer.EditingStyle != null
                ? composer.EditingStyle
                : composer.Source;
            bool hasComposer = comparison != null;
            return DeucarianThemeDraftPolicy.CollectPendingChangeDescriptions(
                status,
                hasComposer && composer.Surface != comparison.SurfaceProfile,
                hasComposer && composer.Corners != comparison.ShapeProfile,
                hasComposer && composer.Border != comparison.StrokeProfile,
                hasComposer && composer.Size != comparison.Density,
                hasComposer && composer.Typography != comparison.TypographyProfile,
                runtimeCandidateTouched && runtimeSettingsCandidate != baselineRuntimeSettings);
        }

        private bool IsComposerDraftDirty() => composer.IsDirty;

        private void UpdatePendingChangesPresentation(IReadOnlyList<string> changes)
        {
            currentPendingChanges = changes ?? Array.Empty<string>();
            toolbarView?.SetPendingChanges(currentPendingChanges.Count,
                EditorApplication.isPlayingOrWillChangePlaymode);
        }

        private void DiscardAllChanges()
        {
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
            RefreshRuntimeSettingsValidation();
            DeucarianThemeManagerSelection selection = projectRuntimeSettings != null
                && projectRuntimeSettings.DefaultThemeFamily != null
                ? DeucarianThemeDraftPolicy.ResolveProjectSelection(projectRuntimeSettings)
                : baselineCaptured ? baselineSelection : DeucarianThemeManagerSelection.FromEditorPrefs();
            DeucarianThemeDraftPolicy.SetDraft(selection.Family, selection.Mode, selection.Style);

            runtimeSettingsCandidate = projectRuntimeSettings != null
                ? projectRuntimeSettings
                : baselineRuntimeSettings;
            runtimeCandidateTouched = false;
            validatedRuntimeSettingsCandidate = null;
            RefreshRuntimeSettingsCandidateValidation();
            ResetComposerFromStyle(selection.Style);
            feedbackMessage = "Unapplied changes were discarded.";
            feedbackType = MessageType.Info;
            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void CaptureBaseline(bool resetComposer = true)
        {
            baselineRuntimeSettings = projectRuntimeSettings;
            baselineSelection = projectRuntimeSettings != null
                && projectRuntimeSettings.DefaultThemeFamily != null
                ? DeucarianThemeDraftPolicy.ResolveProjectSelection(projectRuntimeSettings)
                : DeucarianThemeManagerSelection.FromEditorPrefs();
            baselineCaptured = true;
            runtimeSettingsCandidate = baselineRuntimeSettings;
            runtimeCandidateTouched = false;
            if (resetComposer) ResetComposerFromStyle(baselineSelection.Style);
        }

        private void ResetComposerFromStyle(DeucarianThemeStyle style) => composer.Reset(style);

        internal static void ApplyPreferredSizeOnce(DeucarianThemeManagerWindow window)
        {
            if (window != null) DeucarianEditorWorkspace.ConfigureWindow(window);
        }

        private bool IsComposerComplete() => composer.IsComplete;

        private void ReturnToTheme(string message, MessageType type)
        {
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
            feedbackMessage = message;
            feedbackType = type;
            viewMode = ViewMode.Theme;
            RefreshAssets();
        }

        private void RefreshAssets()
        {
            DeucarianThemingMenuActions.TryHydrateActiveAssetsFromProjectDefault();
            searchResult = DeucarianThemingMenuActions.FindExistingAssets(null, false);
            RefreshRuntimeSettingsValidation();
            validatedRuntimeSettingsCandidate = null;
            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void HandleProjectChanged()
        {
            RefreshAssets();
        }

        private void RefreshRuntimeSettingsValidation()
        {
            runtimeSettingsResourceCount = DeucarianThemeRuntimeSettingsAssets.FindRuntimeSettingsResourceAssets().Count;
            projectRuntimeSettings = DeucarianThemingMenuActions.ResolveProjectRuntimeSettings();
            projectRuntimeSettingsResourceReady = DeucarianThemeRuntimeSettingsAssets.TryValidateRuntimeSettingsCandidate(
                projectRuntimeSettings,
                out projectRuntimeSettingsResourceMessage);
        }

        private void RefreshRuntimeSettingsCandidateValidation()
        {
            validatedRuntimeSettingsCandidate = runtimeSettingsCandidate;
            runtimeSettingsCandidateValid = DeucarianThemeRuntimeSettingsAssets.TryValidateRuntimeSettingsCandidate(
                runtimeSettingsCandidate,
                out runtimeSettingsCandidateMessage);
        }

        private void EnsureSearchResult()
        {
            if (searchResult == null)
            {
                RefreshAssets();
            }
        }
    }
}
