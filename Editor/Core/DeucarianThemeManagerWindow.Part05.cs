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

        private void ToggleDeveloperTools()
        {
            SetDeveloperToolsOpen(!developerToolsOpen);
        }

        private void SetDeveloperToolsOpen(bool open)
        {
            developerToolsOpen = open;
            if (developerToolsDrawer != null)
            {
                DeucarianEditorWorkbenchSurfaces.SetDrawerExpanded(
                    developerToolsDrawer.Root,
                    developerToolsOpen);
            }

            DeucarianEditorCommandBar.SetActive(
                developerToolsButton,
                developerToolsOpen);
        }

        private void ShowComposerMenu()
        {
            GenericMenu menu = new GenericMenu();
            if (IsComposerComplete())
            {
                menu.AddItem(new GUIContent("Save As New Custom Style..."), false, () =>
                {
                    SaveAndActivateComposer(true);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Save As New Custom Style..."));
            }

            if (composer.EditingStyle != null)
            {
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Select Style Asset"), false, () =>
                    DeucarianEditorSelection.SelectAndPing(composer.EditingStyle));
            }

            menu.ShowAsContext();
        }

        private void BeginStyleComposer(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return;
            }

            composer.Source = style;
            composer.EditingStyle = style.IsCustomStyle ? style : null;
            composer.Surface = style.SurfaceProfile;
            composer.Corners = style.ShapeProfile;
            composer.Border = style.StrokeProfile;
            composer.Size = style.Density;
            composer.Typography = style.TypographyProfile;
            feedbackMessage = null;
            viewMode = ViewMode.StyleComposer;
            ApplyComposerPreview();
            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void SaveAndActivateComposer(bool saveAsNew)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !composer.IsComplete)
            {
                feedbackMessage = "Exit Play Mode and choose Surface, Corners, Border and Size before saving.";
                feedbackType = MessageType.Warning;
                return;
            }
            RefreshRuntimeSettingsValidation();
            if (!projectRuntimeSettingsResourceReady)
            {
                feedbackMessage = projectRuntimeSettingsResourceMessage;
                feedbackType = MessageType.Error;
                return;
            }
            string assetPath = null;
            if (composer.EditingStyle == null || saveAsNew)
            {
                string sourcePath = AssetDatabase.GetAssetPath(composer.Source);
                string folder = string.IsNullOrWhiteSpace(sourcePath)
                    ? DeucarianThemingEditorSettings.DefaultAssetFolder
                    : sourcePath.Substring(0, sourcePath.LastIndexOf('/'));
                assetPath = EditorUtility.SaveFilePanelInProject("Save Complete Custom Style",
                    composer.Source != null ? GetStyleDisplayName(composer.Source) + " Custom" : "Custom Theme Style",
                    "asset", "Save the reusable style in your project.", folder);
                if (string.IsNullOrWhiteSpace(assetPath)) return;
            }
            var result = DeucarianThemeStyleAuthoring.SaveAndActivate(composer, projectRuntimeSettings,
                DeucarianThemeManagerSelection.FromEditorPrefs(), saveAsNew, assetPath);
            feedbackMessage = result.Message;
            feedbackType = result.Succeeded ? MessageType.Info : MessageType.Error;
            if (result.Succeeded)
            {
                composer.Reset(result.Style);
                viewMode = ViewMode.Theme;
                DeucarianThemePreviewCoordinator.ClearComposerPreview();
                CaptureBaseline();
            }
            RefreshAssets();
        }

        internal static bool RollbackCreatedCustomStyle(
            string assetPath,
            DeucarianThemeManagerSelection previousDraft)
        {
            string normalizedPath = DeucarianThemingEditorSettings.NormalizeAssetPath(assetPath);
            bool removed = true;
            if (!string.IsNullOrWhiteSpace(normalizedPath)
                && AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null)
            {
                removed = AssetDatabase.DeleteAsset(normalizedPath);
            }

            DeucarianThemeDraftPolicy.SetDraft(
                previousDraft.Family,
                previousDraft.Mode,
                previousDraft.Style);
            return removed;
        }

        private void Activate(DeucarianThemeManagerSelection selection)
        {
            RefreshRuntimeSettingsValidation();
            DeucarianThemeManagerActivationResult result =
                DeucarianThemeManagerWorkflow.Activate(projectRuntimeSettings, selection);
            feedbackMessage = result.Message;
            feedbackType = result.Succeeded ? MessageType.Info : MessageType.Error;
            if (result.Succeeded)
            {
                RefreshAssets();
                CaptureBaseline();
            }
        }

        private void CreateThemeFamily()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianThemingMenuActions.CreateThemeFamilyFromSavePanel();
            if (assets == null)
            {
                return;
            }

            DeucarianThemeStyle style = assets.DefaultStyle
                                        ?? DeucarianThemeDraftPolicy.ResolveSuggestedStyle(
                                            assets.ThemeFamily,
                                            DeucarianThemingEditorSettings.ActiveThemeMode);
            DeucarianThemeDraftPolicy.SetDraft(
                assets.ThemeFamily,
                DeucarianThemingEditorSettings.ActiveThemeMode,
                style);
            RefreshAssets();
        }

        private void CreateRuntimeSettingsFromSavePanel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                feedbackMessage = "Exit Play Mode before creating runtime settings.";
                feedbackType = MessageType.Warning;
                return;
            }

            RefreshRuntimeSettingsValidation();
            if (!DeucarianThemeRuntimeSettingsAssets.CanCreateRuntimeSettings(runtimeSettingsResourceCount, false))
            {
                feedbackMessage = runtimeSettingsResourceCount == 1
                    ? "This project already has its one runtime settings resource. Select and configure that asset instead of creating another."
                    : "Multiple runtime settings resources already exist. Remove the duplicates before continuing.";
                feedbackType = MessageType.Warning;
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Create Runtime Theme Settings",
                DeucarianThemeRuntimeSettings.ResourceName,
                "asset",
                "Create this exact filename inside a Resources folder.",
                "Assets/Resources");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            DeucarianThemeRuntimeSettings created = DeucarianThemeRuntimeSettingsAssets.CreateRuntimeSettingsAtPath(path);
            if (created == null)
            {
                int resourceCount = DeucarianThemeRuntimeSettingsAssets.FindRuntimeSettingsResourceAssets().Count;
                feedbackMessage = resourceCount > 0
                    ? "A runtime settings resource already exists. Select and configure that asset instead of creating a duplicate."
                    : "Use the exact filename DeucarianThemeRuntimeSettings.asset inside a Resources folder.";
                feedbackType = MessageType.Error;
                return;
            }

            DeucarianThemeManagerSelection draft =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            if (DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(draft.Family))
            {
                created.Configure(draft.Family, draft.Mode);
                EditorUtility.SetDirty(created);
                AssetDatabase.SaveAssetIfDirty(created);
            }

            runtimeSettingsCandidate = created;
            if (DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(created.DefaultThemeFamily))
            {
                ReturnToTheme("Runtime settings were created and configured from the staged family.", MessageType.Info);
            }
            else
            {
                feedbackMessage = "Runtime settings were created. Choose a complete staged family, then use Configure Runtime Settings again.";
                feedbackType = MessageType.Warning;
            }
        }

    }
}
