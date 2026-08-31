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


        internal static string BuildDeveloperToolConfirmationMessage(
            string actionName,
            string description)
        {
            string safeDescription = string.IsNullOrWhiteSpace(description)
                ? "This tool may create or modify project assets."
                : description.Trim();
            return safeDescription
                + "\n\nThis operation may create or modify project assets. Continue with '"
                + (actionName ?? "this developer tool")
                + "'?";
        }

        internal static bool ConfirmDeveloperToolAction(
            string actionName,
            string description,
            Func<string, string, string, string, bool> confirmation = null)
        {
            Func<string, string, string, string, bool> confirmationHandler = confirmation
                ?? ((title, message, ok, cancel) => EditorUtility.DisplayDialog(
                    title,
                    message,
                    ok,
                    cancel));
            return confirmationHandler(
                "Developer Tools — " + (actionName ?? "Action"),
                BuildDeveloperToolConfirmationMessage(actionName, description),
                "Continue",
                "Cancel");
        }

        internal static bool TryExecuteDeveloperToolAction(
            string actionName,
            string description,
            Action action,
            Func<string, string, string, string, bool> confirmation = null)
        {
            if (!ConfirmDeveloperToolAction(actionName, description, confirmation))
            {
                return false;
            }

            action?.Invoke();
            return true;
        }

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

            if (composerEditingStyle != null)
            {
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Select Style Asset"), false, () =>
                    DeucarianEditorSelection.SelectAndPing(composerEditingStyle));
            }

            menu.ShowAsContext();
        }

        private void BeginStyleComposer(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return;
            }

            composerSource = style;
            composerEditingStyle = style.IsCustomStyle ? style : null;
            composerSurface = style.SurfaceProfile;
            composerCorners = style.ShapeProfile;
            composerBorder = style.StrokeProfile;
            composerSize = style.Density;
            composerTypography = style.TypographyProfile;
            feedbackMessage = null;
            viewMode = ViewMode.StyleComposer;
            ApplyComposerPreview();
            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void SaveAndActivateComposer(bool saveAsNew)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                feedbackMessage = "Exit Play Mode before saving or activating a custom style.";
                feedbackType = MessageType.Warning;
                return;
            }

            if (!IsComposerComplete())
            {
                feedbackMessage = "Choose all four presentation components before saving.";
                feedbackType = MessageType.Warning;
                return;
            }

            RefreshRuntimeSettingsValidation();
            DeucarianThemeManagerSelection previousDraft =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            if (!projectRuntimeSettingsResourceReady)
            {
                feedbackMessage = projectRuntimeSettingsResourceMessage;
                feedbackType = MessageType.Error;
                return;
            }

            if (projectRuntimeSettings == null
                || !DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(previousDraft.Family))
            {
                feedbackMessage = "Complete the staged theme family before saving a custom style.";
                feedbackType = MessageType.Error;
                return;
            }

            DeucarianThemeStyle style = null;
            DeucarianThemeManagerStyleEdit? stagedStyleEdit = null;
            string createdStylePath = null;
            if (composerEditingStyle != null && !saveAsNew)
            {
                style = composerEditingStyle;
                stagedStyleEdit = new DeucarianThemeManagerStyleEdit(
                    style,
                    composerSurface,
                    composerCorners,
                    composerBorder,
                    composerSize,
                    composerTypography);
            }
            else
            {
                string sourcePath = AssetDatabase.GetAssetPath(composerSource);
                string defaultFolder = string.IsNullOrWhiteSpace(sourcePath)
                    ? DeucarianThemingEditorSettings.DefaultAssetFolder
                    : sourcePath.Substring(0, sourcePath.LastIndexOf('/'));
                string suggestedName = string.IsNullOrWhiteSpace(composerSource.DisplayName)
                    ? "Custom Theme Style"
                    : composerSource.DisplayName + " Custom";
                string assetPath = EditorUtility.SaveFilePanelInProject(
                    "Save Complete Custom Style",
                    suggestedName,
                    "asset",
                    "This Unity .asset file is the complete reusable style: Surface, Corners, Border, Size, and optional Typography. Choose a source-controlled location.",
                    defaultFolder);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    return;
                }

                createdStylePath = assetPath;
                style = DeucarianThemingMenuActions.CreateCustomStyle(
                    composerSource,
                    assetPath,
                    composerSurface,
                    composerCorners,
                    composerBorder,
                    composerSize,
                    composerTypography);
            }

            if (style == null)
            {
                feedbackMessage = "The custom style could not be saved.";
                feedbackType = MessageType.Error;
                return;
            }

            DeucarianThemeManagerSelection selection = new DeucarianThemeManagerSelection(
                DeucarianThemingEditorSettings.ActiveThemeFamily,
                DeucarianThemingEditorSettings.ActiveThemeMode,
                style);
            SetDraft(selection.Family, selection.Mode, style);
            DeucarianThemeManagerActivationResult result = stagedStyleEdit.HasValue
                ? DeucarianThemeManagerWorkflow.Activate(
                    projectRuntimeSettings,
                    selection,
                    stagedStyleEdit.Value)
                : DeucarianThemeManagerWorkflow.Activate(
                    projectRuntimeSettings,
                    selection);
            feedbackMessage = result.Succeeded
                ? $"Saved and activated the complete Custom Style '{GetStyleDisplayName(style)}' for both Light and Dark themes. {result.Message}"
                : result.Message;
            feedbackType = result.Succeeded ? MessageType.Info : MessageType.Error;
            if (!result.Succeeded && !string.IsNullOrWhiteSpace(createdStylePath))
            {
                bool cleanedUp = RollbackCreatedCustomStyle(createdStylePath, previousDraft);
                feedbackMessage += cleanedUp
                    ? " The new custom style asset was removed; your previous staged selection was restored."
                    : " The new asset could not be removed automatically. Delete it before retrying.";
                RefreshAssets();
            }

            if (result.Succeeded)
            {
                composerSource = style;
                composerEditingStyle = style;
                viewMode = ViewMode.Theme;
                DeucarianThemePreviewCoordinator.ClearComposerPreview();
                RefreshAssets();
                CaptureBaseline();
            }
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

            SetDraft(
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
                                        ?? ResolveSuggestedStyle(
                                            assets.ThemeFamily,
                                            DeucarianThemingEditorSettings.ActiveThemeMode);
            SetDraft(
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
            if (!CanCreateRuntimeSettings(runtimeSettingsResourceCount, false))
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

            DeucarianThemeRuntimeSettings created = CreateRuntimeSettingsAtPath(path);
            if (created == null)
            {
                int resourceCount = FindRuntimeSettingsResourceAssets().Count;
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

        internal static DeucarianThemeRuntimeSettings CreateRuntimeSettingsAtPath(string assetPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ThemingLog.Editor.Warning("Exit Play Mode before creating runtime settings.");
                return null;
            }

            string normalizedPath = DeucarianThemingEditorSettings.NormalizeAssetPath(assetPath);
            if (!IsRuntimeSettingsResourcePath(normalizedPath)
                || AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null
                || FindRuntimeSettingsResourceAssets().Count > 0)
            {
                return null;
            }

            int slash = normalizedPath.LastIndexOf('/');
            if (slash > 0)
            {
                DeucarianThemingMenuActions.EnsureAssetFolder(normalizedPath.Substring(0, slash));
            }

            DeucarianThemeRuntimeSettings settings =
                CreateInstance<DeucarianThemeRuntimeSettings>();
            AssetDatabase.CreateAsset(settings, normalizedPath);
            AssetDatabase.SaveAssetIfDirty(settings);
            AssetDatabase.Refresh();
            return settings;
        }

        internal static bool IsRuntimeSettingsResourcePath(string assetPath)
        {
            string normalizedPath = DeucarianThemingEditorSettings.NormalizeAssetPath(assetPath);
            if (string.IsNullOrWhiteSpace(normalizedPath)
                || !normalizedPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return false;
            }

            string expectedFile = "/" + DeucarianThemeRuntimeSettings.ResourceName + ".asset";
            return normalizedPath.EndsWith(expectedFile, StringComparison.OrdinalIgnoreCase)
                   && normalizedPath.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
