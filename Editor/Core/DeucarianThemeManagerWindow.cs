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
    /// <summary>
    /// Focused editor workflow for staging, composing, and explicitly activating project themes.
    /// </summary>
    public sealed class DeucarianThemeManagerWindow : EditorWindow
    {
        private const string WallpaperFadeName = "deucarian-theme-manager-top-safe-fade";
        private const string PreferredSizeKey = "Deucarian.Theming.ThemeManager.PreferredSize.920x560";
        private const float PreviewStackBreakpoint = 760f;
        private const float SecondaryActionSlotWidth = 132f;
        private const float DiscardActionSlotWidth = 148f;
        private const float PrimaryActionSlotWidth = 140f;
        private static readonly Vector2 PreferredSize = new Vector2(920f, 560f);

        private enum ViewMode
        {
            Theme,
            StyleComposer,
            RuntimeSettings
        }

        private ViewMode viewMode;
        private Vector2 scrollPosition;
        private DeucarianThemingMenuActions.AssetSearchResult searchResult;
        private DeucarianThemeRuntimeSettings runtimeSettingsCandidate;
        private DeucarianThemeRuntimeSettings validatedRuntimeSettingsCandidate;
        private bool runtimeSettingsCandidateValid;
        private string runtimeSettingsCandidateMessage = string.Empty;
        private DeucarianThemeRuntimeSettings projectRuntimeSettings;
        private bool projectRuntimeSettingsResourceReady;
        private string projectRuntimeSettingsResourceMessage = string.Empty;
        private string feedbackMessage;
        private MessageType feedbackType = MessageType.Info;

        private DeucarianThemeStyle composerSource;
        private DeucarianThemeStyle composerEditingStyle;
        private DeucarianThemeSurfaceProfile composerSurface;
        private DeucarianThemeShapeProfile composerCorners;
        private DeucarianThemeStrokeProfile composerBorder;
        private DeucarianThemeDensity composerSize;
        private DeucarianThemeTypographyProfile composerTypography;
        private DeucarianThemeManagerSelection baselineSelection;
        private DeucarianThemeRuntimeSettings baselineRuntimeSettings;
        private bool baselineCaptured;
        private bool runtimeCandidateTouched;

        private DeucarianEditorWorkbench workbench;
        private DeucarianEditorWorkbenchFooter workbenchFooter;
        private Button themeViewButton;
        private Button runtimeSettingsViewButton;
        private Button toolbarSecondaryAction;
        private Button toolbarPrimaryAction;
        private Button discardChangesButton;
        private VisualElement toolbarSecondarySlot;
        private VisualElement discardChangesSlot;
        private VisualElement toolbarPrimarySlot;
        private VisualElement toolbarPrimaryStatus;
        private DeucarianEditorWorkbenchDrawer developerToolsDrawer;
        private Button developerToolsButton;
        private bool developerToolsOpen;
        private IReadOnlyList<string> currentPendingChanges = Array.Empty<string>();

        internal DeucarianEditorWorkbench WorkbenchForTests => workbench;
        internal DeucarianEditorWorkbenchFooter FooterForTests => workbenchFooter;
        internal DeucarianEditorWorkbenchDrawer DeveloperToolsDrawerForTests => developerToolsDrawer;

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

            DeucarianEditorCommandBar.SetActive(
                themeViewButton,
                viewMode == ViewMode.Theme || viewMode == ViewMode.StyleComposer);
            DeucarianEditorCommandBar.SetActive(
                runtimeSettingsViewButton,
                viewMode == ViewMode.RuntimeSettings);

            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
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
                    DeucarianEditorCommandBar.SetText(
                        toolbarSecondaryAction,
                        "More");
                    toolbarSecondaryAction.SetEnabled(composerSource != null);
                    toolbarSecondaryAction.tooltip = composerSource != null
                        ? "Open additional save and asset actions."
                        : "Choose a visual style before opening composer actions.";
                    DeucarianEditorCommandBar.SetText(
                        toolbarPrimaryAction,
                        "Save & Activate");
                    bool composerReady = IsComposerReadyToActivate() && !isPlaying;
                    toolbarPrimaryAction.SetEnabled(composerReady);
                    toolbarPrimaryAction.tooltip = composerReady
                        ? "Save the composed style and activate it."
                        : isPlaying
                            ? "Exit Play Mode before saving and activating."
                            : "Complete the composer and project runtime setup first.";
                    ShowPrimaryActionButton();
                    break;

                case ViewMode.RuntimeSettings:
                    DeucarianEditorCommandBar.SetText(
                        toolbarSecondaryAction,
                        "Create Settings...");
                    toolbarSecondaryAction.SetEnabled(!isPlaying);
                    toolbarSecondaryAction.tooltip = isPlaying
                        ? "Exit Play Mode before creating runtime settings."
                        : "Create a Resources-backed runtime settings asset.";
                    DeucarianEditorCommandBar.SetText(
                        toolbarPrimaryAction,
                        RuntimeSettingsCandidateNeedsFamily()
                            ? "Use & Configure"
                            : "Use Selected");
                    bool candidateReady = CanUseRuntimeSettingsCandidate();
                    toolbarPrimaryAction.SetEnabled(candidateReady);
                    toolbarPrimaryAction.tooltip = candidateReady
                        ? "Use the selected runtime settings for this project."
                        : string.IsNullOrWhiteSpace(runtimeSettingsCandidateMessage)
                            ? "Choose valid runtime settings first."
                            : runtimeSettingsCandidateMessage;
                    ShowPrimaryActionButton();
                    break;

                default:
                    bool composerDraftDirty = IsComposerDraftDirty();
                    string composerActionLabel = ResolveComposerActionLabel(
                        selection.Style,
                        composerSource,
                        composerDraftDirty);
                    DeucarianEditorCommandBar.SetText(
                        toolbarSecondaryAction,
                        composerActionLabel);
                    toolbarSecondaryAction.SetEnabled(selection.Style != null);
                    toolbarSecondaryAction.tooltip = selection.Style != null
                        ? composerActionLabel == "Resume Style Edit"
                            ? "Resume the unapplied composer draft for the selected visual style."
                            : selection.Style.IsCustomStyle
                                ? "Edit the selected custom style in the composer."
                                : "Create a custom style from the selected visual style."
                        : "Choose a visual style before opening the composer.";
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

        private void UpdateWorkbenchFooter()
        {
            if (workbenchFooter == null)
            {
                return;
            }

            DeucarianEditorStatus visualStatus;
            string statusLabel;
            string summary;
            string iconId;

            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            string familyName = selection.Family != null ? selection.Family.DisplayName : "No family";
            string styleName = selection.Style != null ? selection.Style.DisplayName : "No style";
            DeucarianThemeManagerActivationStatus status =
                DeucarianThemeManagerWorkflow.Evaluate(
                    projectRuntimeSettings,
                    selection,
                    projectRuntimeSettingsResourceReady,
                    projectRuntimeSettingsResourceMessage);
            int pendingCount = currentPendingChanges != null ? currentPendingChanges.Count : 0;
            if (pendingCount > 0)
            {
                visualStatus = DeucarianEditorStatus.Warning;
                statusLabel = pendingCount + (pendingCount == 1 ? " unapplied change" : " unapplied changes");
                summary = string.Join(" · ", currentPendingChanges);
                iconId = DeucarianEditorIconIds.Warning;
            }
            else if (!string.IsNullOrWhiteSpace(feedbackMessage))
            {
                visualStatus = ToEditorStatus(feedbackType);
                statusLabel = feedbackType == MessageType.Error
                    ? "Error"
                    : feedbackType == MessageType.Warning ? "Attention" : "Updated";
                summary = feedbackMessage;
                iconId = feedbackType == MessageType.Info
                    ? DeucarianEditorIconIds.Info
                    : DeucarianEditorIconIds.Warning;
            }
            else if (status.IsActive)
            {
                visualStatus = DeucarianEditorStatus.Success;
                statusLabel = "Active";
                summary = $"{familyName} · {selection.Mode} · {styleName}";
                iconId = DeucarianEditorIconIds.Check;
            }
            else if (!status.CanActivate)
            {
                visualStatus = DeucarianEditorStatus.Warning;
                statusLabel = "Attention";
                summary = status.Message;
                iconId = DeucarianEditorIconIds.Warning;
            }
            else
            {
                visualStatus = DeucarianEditorStatus.Info;
                statusLabel = "Ready";
                summary = $"{familyName} · {selection.Mode} · {styleName}";
                iconId = DeucarianEditorIconIds.Info;
            }

            workbenchFooter.StatusLabel.text = statusLabel;
            workbenchFooter.Summary.text = summary;
            workbenchFooter.Status.tooltip = pendingCount > 0
                ? string.Join("\n", currentPendingChanges)
                : summary;
            workbenchFooter.Summary.tooltip = workbenchFooter.Status.tooltip;
            workbenchFooter.Version.text = $"com.deucarian.theming {ResolvePackageVersion()}";
            DeucarianEditorWorkbenchSurfaces.SetFooterIcon(workbenchFooter, iconId);
            DeucarianEditorWorkbenchSurfaces.SetFooterStatus(workbenchFooter, visualStatus);
            DeucarianEditorWorkbenchSurfaces.SetFooterBusy(workbenchFooter, false);
        }

        private void ShowPrimaryActionButton()
        {
            if (toolbarPrimarySlot != null && toolbarPrimaryAction?.parent != toolbarPrimarySlot)
            {
                DeucarianEditorCommandBar.SetReservedContent(
                    toolbarPrimarySlot,
                    toolbarPrimaryAction);
            }
        }

        private void ShowPrimaryActiveStatus()
        {
            if (toolbarPrimarySlot != null && toolbarPrimaryStatus?.parent != toolbarPrimarySlot)
            {
                DeucarianEditorCommandBar.SetReservedContent(
                    toolbarPrimarySlot,
                    toolbarPrimaryStatus);
            }
        }

        private static DeucarianEditorStatus ToEditorStatus(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Error:
                    return DeucarianEditorStatus.Error;
                case MessageType.Warning:
                    return DeucarianEditorStatus.Warning;
                default:
                    return DeucarianEditorStatus.Info;
            }
        }

        private void NavigateToTheme()
        {
            viewMode = ViewMode.Theme;
            feedbackMessage = null;
            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void NavigateToStyleComposer()
        {
            DeucarianThemeStyle style = DeucarianThemingEditorSettings.ActiveStyle;
            if (style == null)
            {
                viewMode = ViewMode.Theme;
                feedbackMessage = "Choose a visual style before opening the composer.";
                feedbackType = MessageType.Warning;
                UpdateWorkbenchToolbar();
                Repaint();
                return;
            }

            EnterStyleComposer(style, false);
        }

        private bool EnterStyleComposer(DeucarianThemeStyle style, bool stageSelection)
        {
            if (style == null)
            {
                return false;
            }

            bool sameSource = composerSource == style;
            if (!sameSource && IsComposerDraftDirty())
            {
                bool keepEditing = ShouldKeepCurrentComposerDraft(
                    GetStyleDisplayName(composerSource),
                    GetStyleDisplayName(style));
                if (keepEditing)
                {
                    viewMode = ViewMode.StyleComposer;
                    feedbackMessage = "Continuing the existing style composer draft.";
                    feedbackType = MessageType.Info;
                    UpdateWorkbenchToolbar();
                    Repaint();
                    return false;
                }
            }

            if (stageSelection)
            {
                SetDraft(
                    DeucarianThemingEditorSettings.ActiveThemeFamily,
                    DeucarianThemingEditorSettings.ActiveThemeMode,
                    style);
            }

            if (sameSource)
            {
                viewMode = ViewMode.StyleComposer;
                feedbackMessage = null;
                UpdateWorkbenchToolbar();
                Repaint();
            }
            else
            {
                BeginStyleComposer(style);
            }

            return true;
        }

        internal static bool ShouldKeepCurrentComposerDraft(
            string currentStyleName,
            string requestedStyleName,
            Func<string, string, string, string, string, int> showDialog = null)
        {
            Func<string, string, string, string, string, int> dialog = showDialog
                ?? EditorUtility.DisplayDialogComplex;
            string current = string.IsNullOrWhiteSpace(currentStyleName)
                ? "the current style"
                : currentStyleName;
            string requested = string.IsNullOrWhiteSpace(requestedStyleName)
                ? "the selected style"
                : requestedStyleName;
            int choice = dialog(
                "Keep Style Composer Changes?",
                $"{current} has unapplied composer changes. Keep editing it, or discard those composer changes and switch to {requested}?",
                "Keep editing",
                "Cancel",
                "Discard draft and switch");
            return choice != 2;
        }

        internal static string ResolveComposerActionLabel(
            DeucarianThemeStyle selectedStyle,
            DeucarianThemeStyle composerStyle,
            bool composerDraftDirty)
        {
            if (selectedStyle != null
                && selectedStyle == composerStyle
                && composerDraftDirty)
            {
                return "Resume Style Edit";
            }

            return selectedStyle != null && selectedStyle.IsCustomStyle
                ? "Edit Style"
                : "Customize Style";
        }

        private static string GetStyleDisplayName(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(style.DisplayName)
                ? style.name
                : style.DisplayName;
        }

        private void NavigateToRuntimeSettings()
        {
            runtimeSettingsCandidate = projectRuntimeSettings;
            runtimeCandidateTouched = false;
            validatedRuntimeSettingsCandidate = null;
            RefreshRuntimeSettingsCandidateValidation();
            viewMode = ViewMode.RuntimeSettings;
            feedbackMessage = null;
            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void ExecuteToolbarSecondaryAction()
        {
            switch (viewMode)
            {
                case ViewMode.StyleComposer:
                    ShowComposerMenu();
                    break;
                case ViewMode.RuntimeSettings:
                    CreateRuntimeSettingsFromSavePanel();
                    break;
                default:
                    NavigateToStyleComposer();
                    break;
            }

            UpdateWorkbenchToolbar();
        }

        private void ExecuteToolbarPrimaryAction()
        {
            switch (viewMode)
            {
                case ViewMode.StyleComposer:
                    SaveAndActivateComposer(false);
                    break;
                case ViewMode.RuntimeSettings:
                    UseRuntimeSettingsCandidate();
                    break;
                default:
                    Activate(DeucarianThemeManagerSelection.FromEditorPrefs());
                    break;
            }

            UpdateWorkbenchToolbar();
            Repaint();
        }

        private void DrawWindowGui()
        {
            using (DeucarianEditorWorkbenchGUI.BeginEmbeddedPage(GUILayout.ExpandHeight(true)))
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    scrollPosition = scrollView.scrollPosition;
                    GUILayout.Space(8f);

                    switch (viewMode)
                    {
                        case ViewMode.StyleComposer:
                            DrawStyleComposer();
                            break;
                        case ViewMode.RuntimeSettings:
                            DrawRuntimeSettingsSetup();
                            break;
                        default:
                            DrawThemeManager();
                            break;
                    }

                    UpdateWorkbenchToolbar();
                    GUILayout.Space(8f);
                }
            }
        }

        private void DrawThemeManager()
        {
            EnsureSearchResult();

            DeucarianThemeRuntimeSettings settings = projectRuntimeSettings;
            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            DeucarianThemeManagerActivationStatus status =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    selection,
                    projectRuntimeSettingsResourceReady,
                    projectRuntimeSettingsResourceMessage);

            DrawFlatSplit(
                () =>
                {
                    EditorGUILayout.LabelField("Current Theme", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    EditorGUILayout.LabelField(
                        "Stage the project family, mode, and shared visual style.",
                        DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
                    GUILayout.Space(6f);
                    DrawCurrentThemeCard(selection, status);
                },
                () =>
                {
                    EditorGUILayout.LabelField("Live Preview", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
                    EditorGUILayout.LabelField(
                        "Palette, surfaces, controls, status, and typography in one specimen.",
                        DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
                    GUILayout.Space(6f);
                    DrawThemePreview(
                        selection.ResolvedTheme,
                        selection.Style,
                        selection.Style != null ? selection.Style.SurfaceProfile : null,
                        selection.Style != null ? selection.Style.ShapeProfile : null,
                        selection.Style != null ? selection.Style.StrokeProfile : null,
                        selection.Style != null ? selection.Style.Density : DeucarianThemeDensity.Unspecified,
                        selection.Style != null ? selection.Style.TypographyProfile : null);
                });

            GUILayout.Space(8f);

            DrawContextualSetup(settings, selection, status);
        }

        private void DrawCurrentThemeCard(
            DeucarianThemeManagerSelection selection,
            DeucarianThemeManagerActivationStatus status)
        {
            DrawStatus(status);
            GUILayout.Space(8f);

            DrawAssetDropdown(
                DirtyLabel("Theme Family", status.FamilyDirty),
                selection.Family,
                searchResult.ThemeFamilies,
                family =>
                {
                    DeucarianThemeStyle suggestedStyle = ResolveSuggestedStyle(family, selection.Mode)
                                                         ?? selection.Style;
                    SetDraft(family, selection.Mode, suggestedStyle);
                    UpdateWorkbenchToolbar();
                    Repaint();
                });

            EditorGUI.BeginChangeCheck();
            DeucarianThemeMode mode = (DeucarianThemeMode)DrawWorkbenchEnumPopup(
                DirtyLabel("Mode", status.ModeDirty),
                selection.Mode);
            if (EditorGUI.EndChangeCheck())
            {
                SetDraft(selection.Family, mode, selection.Style);
                UpdateWorkbenchToolbar();
                GUIUtility.ExitGUI();
            }

            DrawAssetDropdown(
                DirtyLabel("Visual Style", status.StyleDirty),
                selection.Style,
                searchResult.Styles,
                style =>
                {
                    SetDraft(selection.Family, selection.Mode, style);
                    UpdateWorkbenchToolbar();
                    Repaint();
                });

            GUILayout.Space(6f);
            DrawResolvedSummary(selection);
            DrawStyleSummary(selection.Style);
        }

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
                DrawSectionHeading("Project Setup");
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
                        "Compose reusable presentation profiles. Typography is optional and falls back to project TMP settings.",
                        DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
                    GUILayout.Space(6f);
                    DrawComposerFields();
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
                    "Theme / Style Composer",
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

        private void DrawComposerPreview()
        {
            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            DrawThemePreview(
                selection.ResolvedTheme,
                null,
                composerSurface,
                composerCorners,
                composerBorder,
                composerSize,
                composerTypography);
        }

        private static void DrawThemePreview(
            DeucarianTheme theme,
            DeucarianThemeStyle style,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity density,
            DeucarianThemeTypographyProfile typography)
        {
            Rect previewRect = GUILayoutUtility.GetRect(260f, 232f, GUILayout.ExpandWidth(true));
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color baseColor = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.SurfaceRaised,
                new Color(0.10f, 0.16f, 0.20f, 0.96f));
            Color surfaceColor = surface != null
                ? surface.ResolveSurfaceColor(baseColor)
                : style != null ? style.ResolveSurfaceColor(baseColor) : baseColor;
            Color borderColor = border != null
                ? border.ResolveBorderColor(surfaceColor)
                : style != null
                    ? style.ResolveBorderColor(surfaceColor)
                    : new Color(0.65f, 0.78f, 0.86f, 0.5f);
            float radius = corners != null
                ? corners.CornerRadius
                : style != null ? style.CornerRadius : 8f;
            float borderWidth = border != null
                ? border.BorderWidth
                : style != null ? style.BorderWidth : 1f;
            Rect panelRect = new Rect(
                previewRect.x + 2f,
                previewRect.y + 2f,
                Mathf.Max(0f, previewRect.width - 4f),
                Mathf.Max(0f, previewRect.height - 4f));
            Rect panelContentRect = DrawPreviewSurface(
                panelRect,
                surfaceColor,
                borderColor,
                radius,
                borderWidth);

            if (surface != null && surface.UseGeneratedNoiseTexture)
            {
                Texture2D texture = surface.GetGeneratedTexture();
                if (texture != null)
                {
                    Color previousColor = GUI.color;
                    GUI.color = surface.TextureTint;
                    GUI.DrawTexture(panelContentRect, texture, ScaleMode.StretchToFill, true);
                    GUI.color = previousColor;
                }
            }

            TMP_FontAsset tmpFont = typography != null
                ? typography.ResolvedFontAsset
                : DeucarianThemeTypographyProfile.ProjectDefaultFontAsset;
            Font sourceFont = ResolvePreviewFont(typography, out string fontLabel, out bool usingFallback);
            DeucarianThemeTextStyle titleToken = typography != null
                ? typography.Title
                : DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Title);
            DeucarianThemeTextStyle bodyToken = typography != null
                ? typography.Body
                : DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Body);
            DeucarianThemeTextStyle captionToken = typography != null
                ? typography.Caption
                : DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Caption);

            Color textPrimary = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.TextPrimary,
                new Color(0.92f, 0.95f, 0.97f, 1f));
            Color textSecondary = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.TextSecondary,
                new Color(0.72f, 0.78f, 0.82f, 1f));
            Color textMuted = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.TextMuted,
                new Color(0.55f, 0.62f, 0.67f, 1f));
            Color accent = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.Accent,
                new Color(0.24f, 0.76f, 0.68f, 1f));
            Color success = ResolvePreviewColor(
                theme,
                DeucarianBuiltinColorRoleIds.Success,
                new Color(0.34f, 0.76f, 0.52f, 1f));

            Rect content = new Rect(
                panelContentRect.x + 14f,
                panelContentRect.y + 12f,
                Mathf.Max(0f, panelContentRect.width - 28f),
                Mathf.Max(0f, panelContentRect.height - 24f));
            GUIStyle titleStyle = CreatePreviewTextStyle(sourceFont, titleToken, textPrimary, FontStyle.Bold);
            GUIStyle bodyStyle = CreatePreviewTextStyle(sourceFont, bodyToken, textSecondary, FontStyle.Normal);
            GUIStyle captionStyle = CreatePreviewTextStyle(sourceFont, captionToken, textMuted, FontStyle.Normal);

            GUI.Label(new Rect(content.x, content.y, content.width, 26f), "Theme preview", titleStyle);
            GUI.Label(
                new Rect(content.x, content.y + 28f, content.width, 34f),
                "A single specimen for typography, fields, actions, and status.",
                bodyStyle);
            GUI.Label(
                new Rect(content.x, content.y + 60f, content.width, 18f),
                "Caption · semantic role tokens",
                captionStyle);

            Rect inputRect = new Rect(content.x, content.y + 84f, content.width, 30f);
            Color inputFill = Color.Lerp(surfaceColor, textPrimary, 0.06f);
            DrawPreviewSurface(inputRect, inputFill, borderColor, Mathf.Max(3f, radius - 6f), borderWidth);
            GUI.Label(
                new Rect(inputRect.x + 9f, inputRect.y + 5f, inputRect.width - 18f, inputRect.height - 10f),
                "Sample input",
                bodyStyle);

            float controlHeight = ResolvePreviewControlHeight(density);
            float buttonGap = 8f;
            float buttonWidth = Mathf.Max(72f, (content.width - buttonGap) * 0.5f);
            Rect primaryRect = new Rect(content.x, content.y + 124f, buttonWidth, controlHeight);
            Rect secondaryRect = new Rect(
                primaryRect.xMax + buttonGap,
                primaryRect.y,
                Mathf.Max(0f, content.xMax - primaryRect.xMax - buttonGap),
                controlHeight);
            DrawPreviewSurface(primaryRect, accent, accent, Mathf.Max(3f, radius - 6f), 1f);
            DrawPreviewSurface(
                secondaryRect,
                Color.Lerp(surfaceColor, textPrimary, 0.08f),
                borderColor,
                Mathf.Max(3f, radius - 6f),
                borderWidth);
            GUIStyle buttonStyle = CreatePreviewTextStyle(sourceFont, bodyToken, textPrimary, FontStyle.Bold);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(primaryRect, "Primary", buttonStyle);
            GUI.Label(secondaryRect, "Secondary", buttonStyle);

            Rect statusRect = new Rect(content.x, content.y + 166f, content.width, 18f);
            EditorGUI.DrawRect(new Rect(statusRect.x, statusRect.y + 5f, 7f, 7f), success);
            GUI.Label(
                new Rect(statusRect.x + 13f, statusRect.y, statusRect.width - 13f, statusRect.height),
                "Success · ready to activate",
                captionStyle);
            string resolvedFontName = tmpFont != null ? tmpFont.name : "TMP project default";
            string fontNote = usingFallback
                ? $"{resolvedFontName} · editor fallback ({fontLabel})"
                : $"{resolvedFontName} · source font preview";
            GUI.Label(
                new Rect(content.x, content.yMax - 16f, content.width, 16f),
                fontNote,
                captionStyle);
        }

        internal static Font ResolvePreviewFont(
            DeucarianThemeTypographyProfile typography,
            out string fontLabel,
            out bool usingFallback)
        {
            TMP_FontAsset tmpFont = typography != null
                ? typography.ResolvedFontAsset
                : DeucarianThemeTypographyProfile.ProjectDefaultFontAsset;
            Font sourceFont = tmpFont != null ? tmpFont.sourceFontFile : null;
            fontLabel = sourceFont != null
                ? sourceFont.name
                : EditorStyles.label.font != null ? EditorStyles.label.font.name : "Unity editor font";
            usingFallback = sourceFont == null;
            return sourceFont != null ? sourceFont : EditorStyles.label.font;
        }

        private static GUIStyle CreatePreviewTextStyle(
            Font font,
            DeucarianThemeTextStyle token,
            Color color,
            FontStyle fallbackStyle)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                font = font,
                fontSize = Mathf.Max(1, Mathf.RoundToInt(token.FontSize)),
                fontStyle = ResolveUnityFontStyle(token.FontStyle, fallbackStyle),
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = color;
            return style;
        }

        private static FontStyle ResolveUnityFontStyle(FontStyles styles, FontStyle fallback)
        {
            bool bold = (styles & FontStyles.Bold) != 0;
            bool italic = (styles & FontStyles.Italic) != 0;
            if (bold && italic)
            {
                return FontStyle.BoldAndItalic;
            }

            if (bold)
            {
                return FontStyle.Bold;
            }

            if (italic)
            {
                return FontStyle.Italic;
            }

            return styles == FontStyles.Normal ? FontStyle.Normal : fallback;
        }

        private static Color ResolvePreviewColor(DeucarianTheme theme, string roleId, Color fallback)
        {
            return theme != null && theme.TryGetColorById(roleId, out Color color)
                ? color
                : fallback;
        }

        internal static Rect DrawPreviewSurface(
            Rect rect,
            Color fillColor,
            Color borderColor,
            float radius,
            float borderWidth)
        {
            float safeWidth = Mathf.Clamp(
                borderWidth,
                0f,
                Mathf.Min(rect.width, rect.height) * 0.5f);
            if (safeWidth <= 0f)
            {
                DeucarianEditorVisualShell.DrawInsetSurface(
                    rect,
                    fillColor,
                    fillColor,
                    radius);
                return rect;
            }

            // Fill the outer surface with the border color, then inset the content by the
            // configured pixel width. This makes 2 px and wider profiles visibly distinct.
            DeucarianEditorVisualShell.DrawInsetSurface(
                rect,
                borderColor,
                borderColor,
                radius);
            Rect contentRect = new Rect(
                rect.x + safeWidth,
                rect.y + safeWidth,
                Mathf.Max(0f, rect.width - safeWidth * 2f),
                Mathf.Max(0f, rect.height - safeWidth * 2f));
            DeucarianEditorVisualShell.DrawInsetSurface(
                contentRect,
                fillColor,
                fillColor,
                Mathf.Max(0f, radius - safeWidth));
            return contentRect;
        }

        private void BuildDeveloperToolsDrawer()
        {
            if (workbench?.Drawer == null)
            {
                return;
            }

            workbench.Drawer.Clear();
            developerToolsOpen = false;
            developerToolsDrawer = DeucarianEditorWorkbenchSurfaces.CreateDrawer(false);
            developerToolsDrawer.Root.name = "deucarian-theme-manager-developer-tools";

            VisualElement header = DeucarianEditorWorkbenchSurfaces.CreateDrawerHeader(
                "Developer Tools");
            header.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                DeucarianEditorIconIds.OpenFolder,
                "Open assets folder",
                DeucarianThemingMenuActions.OpenThemeAssetsFolder,
                "Reveal the Deucarian theme assets folder in the Project window."));
            header.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                DeucarianEditorIconIds.ChevronDown,
                "Close",
                () => SetDeveloperToolsOpen(false),
                "Close Developer Tools."));
            developerToolsDrawer.Content.Add(header);

            VisualElement columns = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumns();
            VisualElement create = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Create");
            AddDeveloperToolAction(
                create,
                DeucarianEditorIconIds.CreateFolder,
                "Theme family...",
                CreateThemeFamily,
                "Opens a save dialog and creates a theme family with its palette and style references at the chosen project location.");
            AddDeveloperToolAction(
                create,
                DeucarianEditorIconIds.CreatePackage,
                "Starter assets",
                () => DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets(),
                "Creates any missing default Deucarian theme assets and repairs their built-in references when needed.");
            AddDeveloperToolAction(
                create,
                DeucarianEditorIconIds.Palette,
                "Built-in theme styles",
                () => DeucarianThemingMenuActions.CreateBuiltinThemeStyleAssets(),
                "Creates or repairs the package's built-in visual-style assets in the default theming folder.");
            AddDeveloperToolAction(
                create,
                DeucarianEditorIconIds.Monitor,
                "UI Toolkit demo assets",
                () => DeucarianUIToolkitDemoAssetFactory.CreateDemoAssets(),
                "Creates or updates the UI Toolkit demo assets under the project's Deucarian theming folder.");

            VisualElement repair = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Repair");
            AddDeveloperToolAction(
                repair,
                DeucarianEditorIconIds.Wrench,
                "Selected theme family",
                () => DeucarianThemingMenuActions.RepairActiveThemeFamilySetup(),
                "Repairs missing built-in references on the currently selected theme family and its related assets.");
            AddDeveloperToolAction(
                repair,
                DeucarianEditorIconIds.Refresh,
                "Selected palette",
                () => DeucarianThemingMenuActions.RepairActivePaletteSetup(),
                "Repairs the active palette's built-in theme and visual-style setup.");

            VisualElement legacy = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Legacy");
            legacy.style.marginRight = 0f;
            AddDeveloperToolAction(
                legacy,
                DeucarianEditorIconIds.History,
                "Create minimal palette...",
                () => DeucarianThemingMenuActions.CreateMinimalPaletteFromSavePanel(),
                "Opens a save dialog and creates a minimal legacy palette asset at the chosen project location.");

            columns.Add(create);
            columns.Add(repair);
            columns.Add(legacy);
            developerToolsDrawer.Content.Add(columns);
            workbench.Drawer.Add(developerToolsDrawer.Root);
        }

        private void AddDeveloperToolAction(
            VisualElement column,
            string iconId,
            string text,
            Action action,
            string confirmationDescription)
        {
            column?.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                iconId,
                text,
                () =>
                {
                    if (!TryExecuteDeveloperToolAction(
                            text,
                            confirmationDescription,
                            action))
                    {
                        return;
                    }

                    RefreshAssets();
                },
                confirmationDescription));
        }

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
                    "Create Custom Theme Style",
                    suggestedName,
                    "asset",
                    "Choose a source-controlled location for the reusable custom style.",
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
            feedbackMessage = result.Message;
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

        private static UnityEngine.Object DrawWorkbenchObjectField(
            string label,
            UnityEngine.Object value,
            Type objectType,
            bool allowSceneObjects)
        {
            GetWorkbenchFieldRects(out Rect labelRect, out Rect fieldRect);
            EditorGUI.LabelField(
                labelRect,
                new GUIContent(label ?? string.Empty, label ?? string.Empty),
                DeucarianEditorWorkbenchGUI.LabelStyle);
            return EditorGUI.ObjectField(fieldRect, value, objectType, allowSceneObjects);
        }

        private static Enum DrawWorkbenchEnumPopup(string label, Enum value)
        {
            GetWorkbenchFieldRects(out Rect labelRect, out Rect fieldRect);
            EditorGUI.LabelField(
                labelRect,
                new GUIContent(label ?? string.Empty, label ?? string.Empty),
                DeucarianEditorWorkbenchGUI.LabelStyle);
            return EditorGUI.EnumPopup(fieldRect, value);
        }

        private static void GetWorkbenchFieldRects(out Rect labelRect, out Rect fieldRect)
        {
            Rect row = EditorGUILayout.GetControlRect();
            float labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, row.width);
            labelRect = new Rect(row.x, row.y, labelWidth, row.height);
            fieldRect = new Rect(
                labelRect.xMax,
                row.y,
                Mathf.Max(0f, row.xMax - labelRect.xMax),
                row.height);
        }

        private static void DrawAssetDropdown<T>(
            string label,
            T selected,
            System.Collections.Generic.IReadOnlyList<T> assets,
            Action<T> onSelected)
            where T : UnityEngine.Object
        {
            Rect row = EditorGUILayout.GetControlRect();
            Rect labelRect = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
            Rect fieldRect = new Rect(
                labelRect.xMax,
                row.y,
                Mathf.Max(0f, row.xMax - labelRect.xMax),
                row.height);
            EditorGUI.LabelField(
                labelRect,
                new GUIContent(label ?? string.Empty, label ?? string.Empty),
                DeucarianEditorWorkbenchGUI.LabelStyle);

            string valueLabel = selected != null ? selected.name : "None";
            string tooltip = selected != null ? AssetDatabase.GetAssetPath(selected) : string.Empty;
            if (EditorGUI.DropdownButton(
                    fieldRect,
                    new GUIContent(valueLabel, tooltip),
                    FocusType.Keyboard))
            {
                UnityEditor.PopupWindow.Show(
                    fieldRect,
                    new ThemeAssetPickerPopup<T>(assets, selected, onSelected));
            }
        }

        private sealed class ThemeAssetPickerPopup<T> : PopupWindowContent
            where T : UnityEngine.Object
        {
            private readonly System.Collections.Generic.IReadOnlyList<T> assets;
            private readonly T selected;
            private readonly Action<T> onSelected;
            private string search = string.Empty;
            private Vector2 pickerScroll;

            public ThemeAssetPickerPopup(
                System.Collections.Generic.IReadOnlyList<T> assets,
                T selected,
                Action<T> onSelected)
            {
                this.assets = assets ?? Array.Empty<T>();
                this.selected = selected;
                this.onSelected = onSelected;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(360f, 300f);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.Space(5f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(6f);
                    search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                    GUILayout.Space(6f);
                }

                using (var scrollView = new EditorGUILayout.ScrollViewScope(pickerScroll))
                {
                    pickerScroll = scrollView.scrollPosition;
                    DrawChoice(null, "None", string.Empty);
                    for (int i = 0; i < assets.Count; i++)
                    {
                        T asset = assets[i];
                        if (asset == null)
                        {
                            continue;
                        }

                        string assetPath = AssetDatabase.GetAssetPath(asset);
                        string searchableText = asset.name + " " + assetPath;
                        if (!string.IsNullOrWhiteSpace(search)
                            && searchableText.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        DrawChoice(asset, asset.name, assetPath);
                    }
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    Event.current.Use();
                }
            }

            private void DrawChoice(T asset, string displayName, string path)
            {
                bool isSelected = asset == selected;
                string label = (isSelected ? "[x] " : "      ") + displayName;
                GUIContent content = new GUIContent(label, path);
                if (!GUILayout.Button(content, EditorStyles.label, GUILayout.Height(24f)))
                {
                    return;
                }

                onSelected?.Invoke(asset);
                editorWindow.Close();
            }
        }

        private static string ResolvePackageVersion()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(DeucarianThemeManagerWindow).Assembly);
            return package != null && !string.IsNullOrWhiteSpace(package.version)
                ? package.version
                : "development";
        }
    }
}
