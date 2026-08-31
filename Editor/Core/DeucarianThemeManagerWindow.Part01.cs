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
            CaptureBaseline();
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
            VisualElement toolbar = workbench?.Toolbar;
            if (toolbar == null)
            {
                return;
            }

            toolbar.Clear();
            var lanes = DeucarianEditorCommandBar.CreateLanes(toolbar);
            themeViewButton = DeucarianEditorCommandBar.CreateToggle(
                "Theme",
                NavigateToTheme);
            themeViewButton.name = "deucarian-theme-manager-view-theme";
            styleComposerViewButton = DeucarianEditorCommandBar.CreateToggle(
                "Style Composer",
                NavigateToStyleComposer,
                false,
                null,
                "Open the selected Visual Style in Style Composer.");
            styleComposerViewButton.name = "deucarian-theme-manager-view-style";
            runtimeSettingsViewButton = DeucarianEditorCommandBar.CreateToggle(
                "Runtime Settings",
                NavigateToRuntimeSettings);
            runtimeSettingsViewButton.name = "deucarian-theme-manager-view-runtime-settings";
            toolbarSecondaryAction = DeucarianEditorCommandBar.CreateAction(
                DeucarianEditorIconIds.Wrench,
                string.Empty,
                ExecuteToolbarSecondaryAction,
                false,
                "Open the contextual style or setup action.");
            toolbarSecondaryAction.name = "deucarian-theme-manager-toolbar-secondary";
            discardChangesButton = DeucarianEditorCommandBar.CreateAction(
                DeucarianEditorIconIds.Undo,
                "Discard changes",
                DiscardAllChanges,
                false,
                "Restore the active project theme and clear every unapplied draft.");
            discardChangesButton.name = "deucarian-theme-manager-discard-changes";
            toolbarPrimaryAction = DeucarianEditorCommandBar.CreateAction(
                DeucarianEditorIconIds.Check,
                string.Empty,
                ExecuteToolbarPrimaryAction,
                true,
                "Apply the current staged theme selection.");
            toolbarPrimaryAction.name = "deucarian-theme-manager-toolbar-primary";

            toolbarSecondarySlot = DeucarianEditorCommandBar.CreateReservedSlot(
                SecondaryActionSlotWidth);
            discardChangesSlot = DeucarianEditorCommandBar.CreateReservedSlot(
                DiscardActionSlotWidth);
            toolbarPrimarySlot = DeucarianEditorCommandBar.CreateReservedSlot(
                PrimaryActionSlotWidth);
            toolbarPrimaryStatus = DeucarianEditorCommandBar.CreateState(
                DeucarianEditorIconIds.Check,
                "Active",
                "The staged selection is active in project runtime settings.");
            toolbarPrimaryStatus.name = "deucarian-theme-manager-toolbar-primary-status";
            DeucarianEditorCommandBar.SetReservedContent(
                toolbarSecondarySlot,
                toolbarSecondaryAction);
            DeucarianEditorCommandBar.SetReservedContent(
                discardChangesSlot,
                discardChangesButton,
                true);
            DeucarianEditorCommandBar.SetReservedContent(
                toolbarPrimarySlot,
                toolbarPrimaryAction);

            lanes.Leading.Add(themeViewButton);
            lanes.Leading.Add(styleComposerViewButton);
            lanes.Leading.Add(runtimeSettingsViewButton);
            lanes.Trailing.Add(toolbarSecondarySlot);
            lanes.Trailing.Add(discardChangesSlot);
            lanes.Trailing.Add(toolbarPrimarySlot);
        }

        private void UpdateWorkbenchToolbar()
        {
            if (workbench?.Toolbar == null || toolbarPrimaryAction == null)
            {
                return;
            }

            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            DeucarianEditorCommandBar.SetActive(
                themeViewButton,
                viewMode == ViewMode.Theme);
            DeucarianEditorCommandBar.SetActive(
                styleComposerViewButton,
                viewMode == ViewMode.StyleComposer);
            styleComposerViewButton?.SetEnabled(selection.Style != null);
            if (styleComposerViewButton != null)
            {
                styleComposerViewButton.tooltip = selection.Style != null
                    ? "Open the selected Visual Style in Style Composer."
                    : "Choose a Visual Style on the Theme tab before opening Style Composer.";
            }
            DeucarianEditorCommandBar.SetActive(
                runtimeSettingsViewButton,
                viewMode == ViewMode.RuntimeSettings);

            bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
            DeucarianThemeManagerActivationStatus status =
                DeucarianThemeManagerWorkflow.Evaluate(
                    projectRuntimeSettings,
                    selection,
                    projectRuntimeSettingsResourceReady,
                    projectRuntimeSettingsResourceMessage);
            IReadOnlyList<string> pendingChanges = GetPendingChangeDescriptions(status);
            UpdatePendingChangesPresentation(pendingChanges);

            switch (viewMode)
            {
                case ViewMode.StyleComposer:
                    DeucarianEditorCommandBar.SetReservedVisible(
                        toolbarSecondarySlot,
                        true);
                    DeucarianEditorCommandBar.SetText(
                        toolbarSecondaryAction,
                        "More");
                    toolbarSecondaryAction.SetEnabled(composerSource != null);
                    toolbarSecondaryAction.tooltip = composerSource != null
                        ? "Open additional save and asset actions."
                        : "Choose a visual style before opening composer actions.";
                    DeucarianEditorCommandBar.SetText(
                        toolbarPrimaryAction,
                        "Save Style & Activate");
                    bool composerReady = IsComposerReadyToActivate() && !isPlaying;
                    toolbarPrimaryAction.SetEnabled(composerReady);
                    toolbarPrimaryAction.tooltip = composerReady
                        ? BuildComposerSaveDescription(composerEditingStyle != null)
                        : isPlaying
                            ? "Exit Play Mode before saving and activating."
                            : "Complete the composer and project runtime setup first.";
                    ShowPrimaryActionButton();
                    break;

                case ViewMode.RuntimeSettings:
                    DeucarianEditorCommandBar.SetReservedVisible(
                        toolbarSecondarySlot,
                        true);
                    DeucarianEditorCommandBar.SetText(
                        toolbarSecondaryAction,
                        "Create Settings...");
                    bool canCreateSettings = CanCreateRuntimeSettings(
                        runtimeSettingsResourceCount,
                        isPlaying);
                    toolbarSecondaryAction.SetEnabled(canCreateSettings);
                    toolbarSecondaryAction.tooltip = isPlaying
                        ? "Exit Play Mode before creating runtime settings."
                        : runtimeSettingsResourceCount == 1
                            ? "This project already has its one runtime settings resource. Use the existing asset instead."
                            : runtimeSettingsResourceCount > 1
                                ? "Multiple runtime settings resources already exist. Remove the duplicates before continuing."
                            : "Create the single Resources-backed runtime settings asset for this project.";
                    bool runtimeSettingsInUse = runtimeSettingsCandidateValid
                                                && runtimeSettingsCandidate == projectRuntimeSettings
                                                && !RuntimeSettingsCandidateNeedsFamily();
                    DeucarianEditorCommandBar.SetText(
                        toolbarPrimaryAction,
                        runtimeSettingsInUse
                            ? "In Use"
                            : RuntimeSettingsCandidateNeedsFamily()
                            ? "Use & Configure"
                            : "Use Selected");
                    bool candidateReady = !runtimeSettingsInUse
                                          && CanUseRuntimeSettingsCandidate();
                    toolbarPrimaryAction.SetEnabled(candidateReady);
                    toolbarPrimaryAction.tooltip = runtimeSettingsInUse
                        ? "This is the unique runtime settings asset currently used by the project."
                        : candidateReady
                        ? "Use the selected runtime settings for this project."
                        : string.IsNullOrWhiteSpace(runtimeSettingsCandidateMessage)
                            ? "Choose valid runtime settings first."
                            : runtimeSettingsCandidateMessage;
                    ShowPrimaryActionButton();
                    break;

                default:
                    DeucarianEditorCommandBar.SetReservedVisible(
                        toolbarSecondarySlot,
                        false);
                    if (status.IsActive)
                    {
                        ShowPrimaryActiveStatus();
                    }
                    else
                    {
                        DeucarianEditorCommandBar.SetText(
                            toolbarPrimaryAction,
                            "Activate");
                        bool canActivate = status.CanActivate && !isPlaying;
                        toolbarPrimaryAction.SetEnabled(canActivate);
                        toolbarPrimaryAction.tooltip = canActivate
                            ? "Activate the staged family, mode, and visual style."
                            : isPlaying
                                ? "Exit Play Mode before activating a theme."
                                : status.Message;
                        ShowPrimaryActionButton();
                    }
                    break;
            }

            UpdateWorkbenchFooter();
        }
    }
}
