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


        internal static bool TryValidateRuntimeSettingsCandidate(
            DeucarianThemeRuntimeSettings candidate,
            out string message)
        {
            if (candidate == null)
            {
                message = "Select an existing runtime settings asset.";
                return false;
            }

            string path = AssetDatabase.GetAssetPath(candidate);
            if (!IsRuntimeSettingsResourcePath(path))
            {
                message = "The asset must use the exact DeucarianThemeRuntimeSettings.asset filename inside a Resources folder.";
                return false;
            }

            IReadOnlyList<DeucarianThemeRuntimeSettings> resources =
                FindRuntimeSettingsResourceAssets();
            if (resources.Count != 1)
            {
                message = resources.Count == 0
                    ? "Unity cannot find this settings asset as a runtime resource."
                    : $"Found {resources.Count} runtime settings resources. Keep exactly one and remove or rename the duplicates.";
                return false;
            }

            if (resources[0] != candidate
                || DeucarianThemingMenuActions.ResolveProjectRuntimeSettings() != candidate)
            {
                message = "Unity resolves a different runtime settings asset. Keep one exact resource and select it here.";
                return false;
            }

            message = "Runtime settings resource is unique and resolvable.";
            return true;
        }

        internal static IReadOnlyList<DeucarianThemeRuntimeSettings> FindRuntimeSettingsResourceAssets()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:" + nameof(DeucarianThemeRuntimeSettings));
            var settings = new List<DeucarianThemeRuntimeSettings>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!IsRuntimeSettingsResourcePath(path))
                {
                    continue;
                }

                DeucarianThemeRuntimeSettings asset =
                    AssetDatabase.LoadAssetAtPath<DeucarianThemeRuntimeSettings>(path);
                if (asset != null)
                {
                    settings.Add(asset);
                }
            }

            return settings;
        }

        private static DeucarianThemeStyle ResolveSuggestedStyle(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode)
        {
            if (family == null)
            {
                return null;
            }

            if (DeucarianThemeManagerWorkflow.TryResolveSharedStyle(family, out DeucarianThemeStyle sharedStyle))
            {
                return sharedStyle;
            }

            DeucarianTheme theme = family.ResolveTheme(mode);
            return theme != null ? theme.VisualStyle : null;
        }

        private static void SetDraft(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            DeucarianThemeStyle style)
        {
            DeucarianThemingEditorSettings.SetDraftSelection(family, mode, style);
            DeucarianThemePreviewCoordinator.ApplySelectedPreview();
        }

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
            DeucarianThemeStyle comparison = composerEditingStyle != null
                ? composerEditingStyle
                : composerSource;
            bool hasComposer = comparison != null;
            return CollectPendingChangeDescriptions(
                status,
                hasComposer && composerSurface != comparison.SurfaceProfile,
                hasComposer && composerCorners != comparison.ShapeProfile,
                hasComposer && composerBorder != comparison.StrokeProfile,
                hasComposer && composerSize != comparison.Density,
                hasComposer && composerTypography != comparison.TypographyProfile,
                runtimeCandidateTouched && runtimeSettingsCandidate != baselineRuntimeSettings);
        }

        private bool IsComposerDraftDirty()
        {
            DeucarianThemeStyle comparison = composerEditingStyle != null
                ? composerEditingStyle
                : composerSource;
            return IsComposerDraftDirty(
                comparison,
                composerSurface,
                composerCorners,
                composerBorder,
                composerSize,
                composerTypography);
        }

        internal static bool IsComposerDraftDirty(
            DeucarianThemeStyle comparison,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size,
            DeucarianThemeTypographyProfile typography)
        {
            return comparison != null
                   && (surface != comparison.SurfaceProfile
                       || corners != comparison.ShapeProfile
                       || border != comparison.StrokeProfile
                       || size != comparison.Density
                       || typography != comparison.TypographyProfile);
        }

        internal static IReadOnlyList<string> CollectPendingChangeDescriptions(
            DeucarianThemeManagerActivationStatus status,
            bool surfaceDirty,
            bool cornersDirty,
            bool borderDirty,
            bool sizeDirty,
            bool typographyDirty,
            bool runtimeSettingsDirty)
        {
            var changes = new List<string>();
            if (status.FamilyDirty) changes.Add("Theme family");
            if (status.ModeDirty) changes.Add("Mode");
            if (status.StyleDirty) changes.Add("Visual style");
            if (surfaceDirty) changes.Add("Composer surface");
            if (cornersDirty) changes.Add("Composer corners");
            if (borderDirty) changes.Add("Composer border");
            if (sizeDirty) changes.Add("Composer size");
            if (typographyDirty) changes.Add("Composer typography");
            if (runtimeSettingsDirty) changes.Add("Runtime settings candidate");
            return changes;
        }

        private void UpdatePendingChangesPresentation(IReadOnlyList<string> changes)
        {
            int count = changes != null ? changes.Count : 0;
            bool visible = count > 0;
            currentPendingChanges = changes ?? Array.Empty<string>();
            if (discardChangesButton != null)
            {
                bool canDiscard = visible && !EditorApplication.isPlayingOrWillChangePlaymode;
                DeucarianEditorCommandBar.SetReservedVisible(
                    discardChangesSlot,
                    true);
                discardChangesButton.SetEnabled(canDiscard);
                discardChangesButton.tooltip = canDiscard
                    ? "Restore the active project theme and clear every unapplied draft."
                    : visible
                        ? "Exit Play Mode before discarding staged changes."
                        : "There are no unapplied changes to discard.";
            }
        }

        private void DiscardAllChanges()
        {
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
            RefreshRuntimeSettingsValidation();
            DeucarianThemeManagerSelection selection = projectRuntimeSettings != null
                && projectRuntimeSettings.DefaultThemeFamily != null
                ? ResolveProjectSelection(projectRuntimeSettings)
                : baselineCaptured ? baselineSelection : DeucarianThemeManagerSelection.FromEditorPrefs();
            SetDraft(selection.Family, selection.Mode, selection.Style);

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

        private void CaptureBaseline()
        {
            baselineRuntimeSettings = projectRuntimeSettings;
            baselineSelection = projectRuntimeSettings != null
                && projectRuntimeSettings.DefaultThemeFamily != null
                ? ResolveProjectSelection(projectRuntimeSettings)
                : DeucarianThemeManagerSelection.FromEditorPrefs();
            baselineCaptured = true;
            runtimeSettingsCandidate = baselineRuntimeSettings;
            runtimeCandidateTouched = false;
            ResetComposerFromStyle(baselineSelection.Style);
        }

        private static DeucarianThemeManagerSelection ResolveProjectSelection(
            DeucarianThemeRuntimeSettings settings)
        {
            if (settings == null)
            {
                return DeucarianThemeManagerSelection.FromEditorPrefs();
            }

            DeucarianThemeFamily family = settings.DefaultThemeFamily;
            DeucarianThemeMode mode = settings.DefaultThemeMode;
            DeucarianThemeStyle style;
            if (!DeucarianThemeManagerWorkflow.TryResolveSharedStyle(family, out style))
            {
                DeucarianTheme resolvedTheme = family != null ? family.ResolveTheme(mode) : settings.DefaultTheme;
                style = resolvedTheme != null ? resolvedTheme.VisualStyle : null;
            }

            return new DeucarianThemeManagerSelection(family, mode, style);
        }

        private void ResetComposerFromStyle(DeucarianThemeStyle style)
        {
            composerSource = style;
            composerEditingStyle = style != null && style.IsCustomStyle ? style : null;
            composerSurface = style != null ? style.SurfaceProfile : null;
            composerCorners = style != null ? style.ShapeProfile : null;
            composerBorder = style != null ? style.StrokeProfile : null;
            composerSize = style != null ? style.Density : DeucarianThemeDensity.Unspecified;
            composerTypography = style != null ? style.TypographyProfile : null;
        }

        internal static void ApplyPreferredSizeOnce(DeucarianThemeManagerWindow window)
        {
            if (window == null || EditorPrefs.GetBool(PreferredSizeKey, false))
            {
                return;
            }

            Rect current = window.position;
            window.position = new Rect(current.x, current.y, PreferredSize.x, PreferredSize.y);
            EditorPrefs.SetBool(PreferredSizeKey, true);
        }

        private bool IsComposerComplete()
        {
            return composerSurface != null
                   && composerCorners != null
                   && composerBorder != null
                   && composerSize != DeucarianThemeDensity.Unspecified;
        }

        private static float ResolvePreviewControlHeight(DeucarianThemeDensity size)
        {
            switch (size)
            {
                case DeucarianThemeDensity.Compact:
                    return 28f;
                case DeucarianThemeDensity.Standard:
                    return 30f;
                default:
                    return 32f;
            }
        }

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
            runtimeSettingsResourceCount = FindRuntimeSettingsResourceAssets().Count;
            projectRuntimeSettings = DeucarianThemingMenuActions.ResolveProjectRuntimeSettings();
            projectRuntimeSettingsResourceReady = TryValidateRuntimeSettingsCandidate(
                projectRuntimeSettings,
                out projectRuntimeSettingsResourceMessage);
        }

        private void RefreshRuntimeSettingsCandidateValidation()
        {
            validatedRuntimeSettingsCandidate = runtimeSettingsCandidate;
            runtimeSettingsCandidateValid = TryValidateRuntimeSettingsCandidate(
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
