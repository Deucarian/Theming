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
            DeucarianThemeManagerWindow window = GetWindow<DeucarianThemeManagerWindow>("Theme Manager");
            window.hideFlags |= HideFlags.DontSave;
            window.minSize = new Vector2(520f, 420f);
            ApplyPreferredSizeOnce(window);
            window.RefreshAssets();
            window.Show();
        }

        /// <summary>Opens the focused composer for a preset or project-authored custom style.</summary>
        public static void OpenStyleComposer(DeucarianThemeStyle style)
        {
            DeucarianThemeManagerStartupGuard.MarkExplicitOpen();
            DeucarianThemeManagerWindow window = GetWindow<DeucarianThemeManagerWindow>("Theme Manager");
            window.hideFlags |= HideFlags.DontSave;
            window.minSize = new Vector2(520f, 420f);
            ApplyPreferredSizeOnce(window);
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
            minSize = new Vector2(520f, 420f);
            if (!Application.isBatchMode)
            {
                ApplyPreferredSizeOnce(this);
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
            EditorApplication.projectChanged -= HandleProjectChanged;
            DeucarianThemePreviewCoordinator.ClearComposerPreview();
            workbench?.Dispose();
            workbench = null;
            workbenchFooter = null;
            developerToolsDrawer = null;
            developerToolsButton = null;
        }

        internal void CreateGUI()
        {
            workbench?.Dispose();
            workbench = DeucarianEditorWorkbench.Create(
                rootVisualElement,
                new DeucarianEditorWorkbenchOptions
                {
                    // Package headers are intentionally disabled for now. Keep the
                    // shared header implementation available for a future UI pass.
                    // IncludeHeader = true,
                    IncludeToolbar = true,
                    IncludeDrawer = true,
                    IncludeFooter = true,
                    // HeaderPackageKey = "theming",
                    // HeaderTitle = "Deucarian Theming",
                    // HeaderSubtitle = "Compose, preview, and activate the project theme.",
                    ToolbarLayout = DeucarianEditorWorkbenchToolbarLayout.StableActionLanes,
                    DrawerMode = DeucarianEditorWorkbenchDrawerMode.Overlay,
                    TopSafeFadeName = WallpaperFadeName
                });
            if (workbench.Content == null || workbench.Toolbar == null)
            {
                return;
            }

            BuildWorkbenchToolbar();
            IMGUIContainer content = workbench.AddImGuiContent(
                DrawWindowGui,
                "deucarian-theme-manager-content");
            content.style.flexGrow = 1f;
            content.style.minHeight = 0f;
            content.style.backgroundColor = Color.clear;
            BuildDeveloperToolsDrawer();
            BuildWorkbenchFooter();
            UpdateWorkbenchToolbar();
        }

        private void BuildWorkbenchFooter()
        {
            if (workbench?.Footer == null)
            {
                return;
            }

            workbench.Footer.Clear();
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
            workbench.Footer.Add(workbenchFooter.Root);
        }

        private void BuildWorkbenchToolbar()
        {
            toolbarView = workbench?.Toolbar == null ? null : new DeucarianThemeManagerToolbar(
                workbench.Toolbar, NavigateToTheme, NavigateToStyleComposer, NavigateToRuntimeSettings,
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
        }
    }
}
