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

        private enum ViewMode
        {
            Theme,
            StyleComposer,
            RuntimeSettings
        }

        public static void OpenWindow()
        {
            DeucarianThemeManagerStartupGuard.MarkExplicitOpen();
            DeucarianThemeManagerWindow window = DeucarianEditorWindowPages.GetStandalone<DeucarianThemeManagerWindow>("Theme Manager");
            window.hideFlags |= HideFlags.DontSave;
            DeucarianEditorWorkspace.ConfigureWindow(window);
            window.RefreshAssets();
            window.Show();
        }

        /// <summary>Opens the focused composer for a preset or project-authored custom style.</summary>
        public static void OpenStyleComposer(DeucarianThemeStyle style)
        {
            DeucarianThemeManagerStartupGuard.MarkExplicitOpen();
            DeucarianThemeManagerWindow window = DeucarianEditorWindowPages.GetStandalone<DeucarianThemeManagerWindow>("Theme Manager");
            window.hideFlags |= HideFlags.DontSave;
            DeucarianEditorWorkspace.ConfigureWindow(window);
            window.RefreshAssets();
            if (style != null)
            {
                window.EnterStyleComposer(style, true);
            }

            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            if (!Application.isBatchMode)
            {
                DeucarianEditorWorkspace.ConfigureWindow(this);
            }

            EditorApplication.projectChanged -= HandleProjectChanged;
            EditorApplication.projectChanged += HandleProjectChanged;
            DeucarianThemingMenuActions.TryHydrateActiveAssetsFromProjectDefault();
            RefreshAssets();
            CaptureBaseline(!composer.HasDraft);
            DeucarianThemePreviewCoordinator.ApplySelectedPreview();
        }

        private void OnDisable()
        {
            navigation?.Dispose();
            navigation = null;
            EditorApplication.projectChanged -= HandleProjectChanged;
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
            workspace?.Dispose();
            workspace = null;
            workbenchFooter = null;
            developerToolsDrawer = null;
            developerToolsButton = null;
        }

        private void OnInspectorUpdate() => UpdateWorkbenchToolbar();
        internal void CreateGUI()
        {
            navigation?.Dispose();
            navigation = new DeucarianEditorPageSession(this, DeucarianToolIds.ThemeManager, BuildPage);
        }

        internal static IDeucarianEditorPage CreatePage()
        {
            DeucarianThemeManagerStartupGuard.MarkExplicitOpen();
            return DeucarianEditorWindowPages.Create<DeucarianThemeManagerWindow>(
                (window, root) => window.BuildPage(root), update: window => window.UpdateWorkbenchToolbar());
        }

        private DeucarianEditorPageSession navigation;
        private VisualElement pageRoot;
        private VisualElement PageRoot => pageRoot ?? rootVisualElement;

        private void BuildPage(VisualElement root)
        {
            pageRoot = root;
            workspace?.Dispose();
            PageRoot.Clear();
            workspace = new DeucarianEditorWorkspace(PageRoot, Application.productName, true);
            workspace.Title.text = "Theme Manager";
            workspace.Subtitle.text = "Preview a theme. Apply it when you’re ready.";
            DeucarianEditorWorkspaceNavigation.Populate(workspace, DeucarianToolIds.ThemeManager);
            DeucarianEditorWorkspaceControls.Show(workspace.Scope, false);
            BuildWorkbenchToolbar();
            workspaceContent = new ThemeWorkspaceContent(this, workspace);
            BuildDeveloperToolsDrawer();
            BuildWorkbenchFooter();
            UpdateWorkbenchToolbar();
        }

        private void BuildWorkbenchFooter()
        {
            if (workspace?.Footer == null)
            {
                return;
            }

            workspace.Footer.Clear();
            workbenchFooter = DeucarianEditorWorkbenchSurfaces.CreateFooter(
                "●",
                "Ready",
                "Theme assets are ready.",
                "Refresh",
                RefreshAssets,
                $"com.deucarian.theming {ResolvePackageVersion()}");
            workbenchFooter.Root.name = "deucarian-theme-manager-footer";
            DeucarianEditorCommandBar.ConfigureAction(
                workbenchFooter.Action,
                DeucarianEditorIconIds.Refresh,
                "Refresh",
                "Rescan theme assets and project settings.");
            developerToolsButton = DeucarianEditorWorkbenchSurfaces.AddFooterAction(
                workbenchFooter,
                DeucarianEditorIconIds.Wrench,
                "Developer Tools",
                ToggleDeveloperTools,
                "Open asset creation, repair, and legacy utilities.",
                128f);
            developerToolsButton?.AddToClassList(DeucarianEditorWorkbenchToolbar.ToggleClass);
            workspace.Footer.Add(workbenchFooter.Root);
        }

        private void BuildWorkbenchToolbar()
        {
            toolbarView = workspace == null ? null : new DeucarianThemeManagerToolbar(
                workspace, NavigateToTheme, NavigateToStyleComposer, NavigateToRuntimeSettings,
                ExecuteToolbarSecondaryAction, DiscardAllChanges, ExecuteToolbarPrimaryAction);
        }

        private void UpdateWorkbenchToolbar()
        {
            if (toolbarView == null) return;

            DeucarianThemeManagerSelection selection = DeucarianThemeManagerSelection.FromEditorPrefs();
            toolbarView.SetSelection(viewMode == ViewMode.Theme, viewMode == ViewMode.StyleComposer,
                viewMode == ViewMode.RuntimeSettings, selection.Style != null);
            bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
            DeucarianThemeManagerActivationStatus status = DeucarianThemeManagerWorkflow.Evaluate(
                projectRuntimeSettings, selection, projectRuntimeSettingsResourceReady,
                projectRuntimeSettingsResourceMessage);
            UpdatePendingChangesPresentation(GetPendingChangeDescriptions(status));

            switch (viewMode)
            {
                case ViewMode.StyleComposer:
                    toolbarView.SetSecondary("More", composer.Source != null, composer.Source != null
                        ? "Open additional save and asset actions."
                        : "Choose a visual style before opening composer actions.");
                    bool composerReady = IsComposerReadyToActivate() && !isPlaying;
                    toolbarView.SetPrimary("Save Style & Activate", composerReady, composerReady
                        ? BuildComposerSaveDescription(composer.EditingStyle != null)
                        : isPlaying ? "Exit Play Mode before saving and activating."
                        : "Complete the composer and project runtime setup first.");
                    break;
                case ViewMode.RuntimeSettings:
                    bool canCreateSettings = DeucarianThemeRuntimeSettingsAssets.CanCreateRuntimeSettings(
                        runtimeSettingsResourceCount, isPlaying);
                    toolbarView.SetSecondary("Create Settings...", canCreateSettings, isPlaying
                        ? "Exit Play Mode before creating runtime settings."
                        : runtimeSettingsResourceCount == 1
                            ? "This project already has its one runtime settings resource. Use the existing asset instead."
                            : runtimeSettingsResourceCount > 1
                                ? "Multiple runtime settings resources already exist. Remove the duplicates before continuing."
                                : "Create the single Resources-backed runtime settings asset for this project.");
                    bool needsFamily = RuntimeSettingsCandidateNeedsFamily();
                    bool inUse = runtimeSettingsCandidateValid
                        && runtimeSettingsCandidate == projectRuntimeSettings && !needsFamily;
                    bool candidateReady = !inUse && CanUseRuntimeSettingsCandidate();
                    toolbarView.SetPrimary(inUse ? "In Use" : needsFamily ? "Use & Configure" : "Use Selected",
                        candidateReady, inUse
                            ? "This is the unique runtime settings asset currently used by the project."
                            : candidateReady ? "Use the selected runtime settings for this project."
                            : string.IsNullOrWhiteSpace(runtimeSettingsCandidateMessage)
                                ? "Choose valid runtime settings first." : runtimeSettingsCandidateMessage);
                    break;
                default:
                    toolbarView.HideSecondary();
                    if (status.IsActive) toolbarView.ShowActive();
                    else
                    {
                        bool canActivate = status.CanActivate && !isPlaying;
                        toolbarView.SetPrimary("Activate", canActivate, canActivate
                            ? "Activate the staged family, mode, and visual style."
                            : isPlaying ? "Exit Play Mode before activating a theme." : status.Message);
                    }
                    break;
            }

            UpdateWorkbenchFooter();
            workspaceContent?.Refresh();
        }
    }
}
