using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
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
        private const string DeveloperToolsKey = "Deucarian.Theming.ThemeManager.DeveloperTools";
        private const string WallpaperFadeName = "deucarian-theme-manager-top-safe-fade";

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

        private DeucarianEditorWorkbench workbench;
        private Button themeViewButton;
        private Button styleComposerViewButton;
        private Button runtimeSettingsViewButton;
        private Label toolbarSummary;
        private Button toolbarSecondaryAction;
        private Button toolbarPrimaryAction;

        internal DeucarianEditorWorkbench WorkbenchForTests => workbench;

        public static void OpenWindow()
        {
            DeucarianThemeManagerWindow window = GetWindow<DeucarianThemeManagerWindow>("Theme Manager");
            window.minSize = new Vector2(440f, 420f);
            window.RefreshAssets();
            window.Show();
        }

        /// <summary>Opens the focused composer for a preset or project-authored custom style.</summary>
        public static void OpenStyleComposer(DeucarianThemeStyle style)
        {
            DeucarianThemeManagerWindow window = GetWindow<DeucarianThemeManagerWindow>("Theme Manager");
            window.minSize = new Vector2(440f, 420f);
            window.RefreshAssets();
            if (style != null)
            {
                SetDraft(
                    DeucarianThemingEditorSettings.ActiveThemeFamily,
                    DeucarianThemingEditorSettings.ActiveThemeMode,
                    style);
                window.BeginStyleComposer(style);
            }

            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged -= HandleProjectChanged;
            EditorApplication.projectChanged += HandleProjectChanged;
            DeucarianThemingMenuActions.TryHydrateActiveAssetsFromProjectDefault();
            RefreshAssets();
            DeucarianThemePreviewCoordinator.ApplySelectedPreview();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= HandleProjectChanged;
            workbench?.Dispose();
            workbench = null;
        }

        internal void CreateGUI()
        {
            workbench?.Dispose();
            workbench = DeucarianEditorWorkbench.Create(
                rootVisualElement,
                new DeucarianEditorWorkbenchOptions
                {
                    IncludeToolbar = true,
                    IncludeDrawer = false,
                    IncludeFooter = false,
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
            UpdateWorkbenchToolbar();
        }

        private void BuildWorkbenchToolbar()
        {
            VisualElement toolbar = workbench?.Toolbar;
            if (toolbar == null)
            {
                return;
            }

            toolbar.Clear();
            themeViewButton = DeucarianEditorWorkbenchToolbar.CreateToggleButton(
                "Theme",
                NavigateToTheme);
            themeViewButton.name = "deucarian-theme-manager-view-theme";
            styleComposerViewButton = DeucarianEditorWorkbenchToolbar.CreateToggleButton(
                "Style Composer",
                NavigateToStyleComposer);
            styleComposerViewButton.name = "deucarian-theme-manager-view-style";
            runtimeSettingsViewButton = DeucarianEditorWorkbenchToolbar.CreateToggleButton(
                "Runtime Settings",
                NavigateToRuntimeSettings);
            runtimeSettingsViewButton.name = "deucarian-theme-manager-view-runtime-settings";
            toolbarSummary = DeucarianEditorWorkbenchToolbar.CreateSummary(string.Empty);
            toolbarSummary.name = "deucarian-theme-manager-toolbar-summary";
            toolbarSecondaryAction = DeucarianEditorWorkbenchToolbar.CreateActionButton(
                string.Empty,
                ExecuteToolbarSecondaryAction);
            toolbarSecondaryAction.name = "deucarian-theme-manager-toolbar-secondary";
            toolbarPrimaryAction = DeucarianEditorWorkbenchToolbar.CreateActionButton(
                string.Empty,
                ExecuteToolbarPrimaryAction,
                true);
            toolbarPrimaryAction.name = "deucarian-theme-manager-toolbar-primary";

            toolbar.Add(themeViewButton);
            toolbar.Add(styleComposerViewButton);
            toolbar.Add(runtimeSettingsViewButton);
            toolbar.Add(toolbarSummary);
            toolbar.Add(DeucarianEditorWorkbenchToolbar.CreateSpacer());
            toolbar.Add(toolbarSecondaryAction);
            toolbar.Add(toolbarPrimaryAction);
        }

        private void UpdateWorkbenchToolbar()
        {
            if (workbench?.Toolbar == null || toolbarPrimaryAction == null)
            {
                return;
            }

            DeucarianEditorWorkbenchToolbar.SetToggleActive(themeViewButton, viewMode == ViewMode.Theme);
            DeucarianEditorWorkbenchToolbar.SetToggleActive(
                styleComposerViewButton,
                viewMode == ViewMode.StyleComposer);
            DeucarianEditorWorkbenchToolbar.SetToggleActive(
                runtimeSettingsViewButton,
                viewMode == ViewMode.RuntimeSettings);

            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            string familyName = selection.Family != null ? selection.Family.DisplayName : "No family";
            string styleName = selection.Style != null ? selection.Style.DisplayName : "No style";
            bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;

            switch (viewMode)
            {
                case ViewMode.StyleComposer:
                    toolbarSummary.text = composerEditingStyle != null
                        ? $"Editing {composerEditingStyle.DisplayName}"
                        : $"Customizing {(composerSource != null ? composerSource.DisplayName : styleName)}";
                    toolbarSecondaryAction.text = "More";
                    toolbarSecondaryAction.SetEnabled(composerSource != null);
                    toolbarPrimaryAction.text = "Save & Activate";
                    toolbarPrimaryAction.SetEnabled(IsComposerReadyToActivate() && !isPlaying);
                    break;

                case ViewMode.RuntimeSettings:
                    toolbarSummary.text = runtimeSettingsCandidate != null
                        ? runtimeSettingsCandidate.name
                        : "Choose runtime settings";
                    toolbarSecondaryAction.text = "Create Settings...";
                    toolbarSecondaryAction.SetEnabled(!isPlaying);
                    toolbarPrimaryAction.text = RuntimeSettingsCandidateNeedsFamily()
                        ? "Use & Configure"
                        : "Use Selected";
                    toolbarPrimaryAction.SetEnabled(CanUseRuntimeSettingsCandidate());
                    break;

                default:
                    DeucarianThemeManagerActivationStatus status =
                        DeucarianThemeManagerWorkflow.Evaluate(
                            projectRuntimeSettings,
                            selection,
                            projectRuntimeSettingsResourceReady,
                            projectRuntimeSettingsResourceMessage);
                    toolbarSummary.text = $"{familyName} · {selection.Mode} · {styleName}";
                    toolbarSecondaryAction.text = "Customize Style";
                    toolbarSecondaryAction.SetEnabled(selection.Style != null);
                    toolbarPrimaryAction.text = status.IsActive ? "Active" : "Activate";
                    toolbarPrimaryAction.SetEnabled(status.CanActivate && !isPlaying);
                    break;
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
            DeucarianThemeStyle style = composerSource
                                        ?? DeucarianThemingEditorSettings.ActiveStyle;
            if (style == null)
            {
                viewMode = ViewMode.Theme;
                feedbackMessage = "Choose a visual style before opening the composer.";
                feedbackType = MessageType.Warning;
                UpdateWorkbenchToolbar();
                Repaint();
                return;
            }

            BeginStyleComposer(style);
        }

        private void NavigateToRuntimeSettings()
        {
            runtimeSettingsCandidate = projectRuntimeSettings;
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
            using (DeucarianEditorWorkbenchPanelScope surface =
                   DeucarianEditorWorkbenchGUI.BeginSurface(
                       DeucarianEditorWorkbenchGUI.WindowStyle,
                       DeucarianEditorWorkbenchGUI.MainBackgroundColor,
                       DeucarianEditorWorkbenchGUI.PanelBorderColor))
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
                    GUILayout.Space(4f);
                    DeucarianEditorChrome.DrawFooterVersion("com.deucarian.theming", ResolvePackageVersion());
                    GUILayout.Space(4f);
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

            DeucarianEditorWorkbenchGUI.DrawPanel(
                "Current Theme",
                () => DrawCurrentThemeCard(selection, status));

            DrawContextualSetup(settings, selection, status);

            if (!string.IsNullOrWhiteSpace(feedbackMessage))
            {
                EditorGUILayout.HelpBox(feedbackMessage, feedbackType);
                GUILayout.Space(4f);
            }

            using (DeucarianEditorFoldoutScope scope =
                   DeucarianEditorAccordion.BeginFoldoutCardScope(
                       DeveloperToolsKey,
                       "Developer Tools",
                       "Asset discovery, creation, repair, and legacy utilities.",
                       false))
            {
                if (scope.Open)
                {
                    DrawDeveloperTools();
                }
            }
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
            DeucarianThemeMode mode = (DeucarianThemeMode)EditorGUILayout.EnumPopup(
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
            using (DeucarianEditorWorkbenchPanelScope summarySurface =
                   DeucarianEditorWorkbenchGUI.BeginSurface(
                       DeucarianEditorWorkbenchGUI.SampleRowStyle,
                       DeucarianEditorWorkbenchGUI.SampleRowBackgroundColor,
                       DeucarianEditorWorkbenchGUI.PanelBorderColor))
            {
                DrawResolvedSummary(selection);
            }
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
                EditorGUILayout.LabelField(status.Message, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static void DrawResolvedSummary(DeucarianThemeManagerSelection selection)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Resolved Theme",
                    selection.ResolvedTheme,
                    typeof(DeucarianTheme),
                    false);
                EditorGUILayout.ObjectField(
                    "Palette",
                    selection.ResolvedPalette,
                    typeof(DeucarianColorPalette),
                    false);
            }
        }

        private static void DrawStyleSummary(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return;
            }

            GUILayout.Space(2f);
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Surface",
                    style.SurfaceProfile != null ? style.SurfaceProfile.DisplayName : "Legacy inline");
                EditorGUILayout.TextField(
                    "Corners",
                    style.ShapeProfile != null ? style.ShapeProfile.DisplayName : "Legacy inline");
                EditorGUILayout.TextField(
                    "Border",
                    style.StrokeProfile != null ? style.StrokeProfile.DisplayName : "Legacy inline");
                EditorGUILayout.TextField(
                    "Size",
                    style.Density == DeucarianThemeDensity.Unspecified
                        ? "Legacy automatic"
                        : style.Density.ToString());
            }
        }

        private void DrawContextualSetup(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            DeucarianThemeManagerActivationStatus status)
        {
            if (!status.RuntimeSettingsReady)
            {
                DeucarianEditorWorkbenchGUI.DrawPanel(
                    "Project Setup",
                    () =>
                    {
                        EditorGUILayout.HelpBox(
                            status.HasRuntimeSettings
                                ? status.Message
                                : "A source-controlled runtime settings asset connects editor activation to builds.",
                            MessageType.Warning);
                        if (DrawWorkbenchAction(
                                "Configure Runtime Settings...",
                                !EditorApplication.isPlayingOrWillChangePlaymode,
                                true))
                        {
                            runtimeSettingsCandidate = settings;
                            validatedRuntimeSettingsCandidate = null;
                            viewMode = ViewMode.RuntimeSettings;
                            feedbackMessage = null;
                            UpdateWorkbenchToolbar();
                            GUIUtility.ExitGUI();
                        }
                    });
                return;
            }

            if (selection.Family == null)
            {
                DeucarianEditorWorkbenchGUI.DrawPanel(
                    "Choose a Theme Family",
                    () =>
                    {
                        EditorGUILayout.HelpBox(
                            "No family is selected. Choose an existing family above or create one.",
                            MessageType.Info);
                        if (DrawWorkbenchAction(
                                "Create Theme Family...",
                                !EditorApplication.isPlayingOrWillChangePlaymode))
                        {
                            CreateThemeFamily();
                        }
                    });
            }
            else if (!selection.Family.IsComplete)
            {
                DeucarianEditorWorkbenchGUI.DrawPanel(
                    "Family Needs Repair",
                    () =>
                    {
                        EditorGUILayout.HelpBox(
                            "Both a Light and Dark theme are required before activation.",
                            MessageType.Warning);
                        if (DrawWorkbenchAction(
                                "Repair Selected Family",
                                !EditorApplication.isPlayingOrWillChangePlaymode))
                        {
                            DeucarianThemingMenuActions.RepairActiveThemeFamilySetup();
                            RefreshAssets();
                        }
                    });
            }
        }

        private void DrawRuntimeSettingsSetup()
        {
            DeucarianEditorWorkbenchGUI.DrawPanel(
                "Configure Project",
                () =>
                {
                    EditorGUI.BeginChangeCheck();
                    runtimeSettingsCandidate = (DeucarianThemeRuntimeSettings)EditorGUILayout.ObjectField(
                        "Existing Settings",
                        runtimeSettingsCandidate,
                        typeof(DeucarianThemeRuntimeSettings),
                        false);
                    if (EditorGUI.EndChangeCheck()
                        || validatedRuntimeSettingsCandidate != runtimeSettingsCandidate)
                    {
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

            if (!string.IsNullOrWhiteSpace(feedbackMessage))
            {
                EditorGUILayout.HelpBox(feedbackMessage, feedbackType);
            }
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
        }

        private void DrawStyleComposer()
        {
            if (composerSource == null)
            {
                ReturnToTheme("Choose a visual style before customizing it.", MessageType.Warning);
                return;
            }

            DeucarianEditorWorkbenchGUI.DrawPanel(
                composerEditingStyle != null ? composerEditingStyle.DisplayName : composerSource.DisplayName,
                DrawComposerFields);

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

            if (!string.IsNullOrWhiteSpace(feedbackMessage))
            {
                EditorGUILayout.HelpBox(feedbackMessage, feedbackType);
            }
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

            composerSurface = (DeucarianThemeSurfaceProfile)EditorGUILayout.ObjectField(
                DirtyLabel("Surface", composerSurface != comparison.SurfaceProfile),
                composerSurface,
                typeof(DeucarianThemeSurfaceProfile),
                false);
            composerCorners = (DeucarianThemeShapeProfile)EditorGUILayout.ObjectField(
                DirtyLabel("Corners", composerCorners != comparison.ShapeProfile),
                composerCorners,
                typeof(DeucarianThemeShapeProfile),
                false);
            composerBorder = (DeucarianThemeStrokeProfile)EditorGUILayout.ObjectField(
                DirtyLabel("Border", composerBorder != comparison.StrokeProfile),
                composerBorder,
                typeof(DeucarianThemeStrokeProfile),
                false);
            composerSize = (DeucarianThemeDensity)EditorGUILayout.EnumPopup(
                DirtyLabel("Size", composerSize != comparison.Density),
                composerSize);

            GUILayout.Space(8f);
            DrawComposerPreview();
        }

        private void DrawComposerPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            Rect previewRect = GUILayoutUtility.GetRect(120f, 82f, GUILayout.ExpandWidth(true));
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color baseColor = new Color(0.10f, 0.16f, 0.20f, 0.92f);
            Color surfaceColor = composerSurface != null
                ? composerSurface.ResolveSurfaceColor(baseColor)
                : baseColor;
            Color borderColor = composerBorder != null
                ? composerBorder.ResolveBorderColor(surfaceColor)
                : new Color(0.65f, 0.78f, 0.86f, 0.5f);
            float radius = composerCorners != null ? composerCorners.CornerRadius : 8f;
            float borderWidth = composerBorder != null ? composerBorder.BorderWidth : 1f;
            Rect panelRect = new Rect(
                previewRect.x + 10f,
                previewRect.y + 8f,
                Mathf.Max(0f, previewRect.width - 20f),
                64f);
            Rect panelContentRect = DrawPreviewSurface(
                panelRect,
                surfaceColor,
                borderColor,
                radius,
                borderWidth);

            if (composerSurface != null && composerSurface.UseGeneratedNoiseTexture)
            {
                Texture2D texture = composerSurface.GetGeneratedTexture();
                if (texture != null)
                {
                    Color previousColor = GUI.color;
                    GUI.color = composerSurface.TextureTint;
                    GUI.DrawTexture(panelContentRect, texture, ScaleMode.StretchToFill, true);
                    GUI.color = previousColor;
                }
            }

            float buttonHeight = ResolvePreviewControlHeight(composerSize);
            Rect buttonRect = new Rect(
                panelRect.x + 10f,
                panelRect.center.y - buttonHeight * 0.5f,
                94f,
                buttonHeight);
            DrawPreviewSurface(
                buttonRect,
                new Color(surfaceColor.r + 0.05f, surfaceColor.g + 0.05f, surfaceColor.b + 0.05f, 0.96f),
                borderColor,
                Mathf.Max(0f, radius - 4f),
                borderWidth);
            GUI.Label(buttonRect, "Button", new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            });
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

        private void DrawDeveloperTools()
        {
            EnsureSearchResult();
            string counts = $"{searchResult.ThemeFamilies.Count} families | "
                            + $"{searchResult.Themes.Count} themes | "
                            + $"{searchResult.Palettes.Count} palettes | "
                            + $"{searchResult.Styles.Count} styles";
            EditorGUILayout.LabelField(counts, EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (DrawWorkbenchAction("Refresh Assets"))
                {
                    RefreshAssets();
                }

                if (DrawWorkbenchAction("Open Assets Folder"))
                {
                    DeucarianThemingMenuActions.OpenThemeAssetsFolder();
                }

                if (DrawWorkbenchAction("More Tools..."))
                {
                    ShowDeveloperToolsMenu();
                }
            }
        }

        private void ShowDeveloperToolsMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create/Theme Family..."), false, CreateThemeFamily);
            menu.AddItem(new GUIContent("Create/Starter Assets"), false, () =>
            {
                DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets();
                RefreshAssets();
            });
            menu.AddItem(new GUIContent("Create/Built-in Theme Styles"), false, () =>
            {
                DeucarianThemingMenuActions.CreateBuiltinThemeStyleAssets();
                RefreshAssets();
            });
            menu.AddItem(new GUIContent("Create/UI Toolkit Demo Assets"), false, () =>
            {
                DeucarianUIToolkitDemoAssetFactory.CreateDemoAssets();
                RefreshAssets();
            });
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Repair/Selected Theme Family"), false, () =>
            {
                DeucarianThemingMenuActions.RepairActiveThemeFamilySetup();
                RefreshAssets();
            });
            menu.AddItem(new GUIContent("Repair/Selected Palette"), false, () =>
            {
                DeucarianThemingMenuActions.RepairActivePaletteSetup();
                RefreshAssets();
            });
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Legacy/Create Minimal Palette..."), false, () =>
            {
                DeucarianThemingMenuActions.CreateMinimalPaletteFromSavePanel();
                RefreshAssets();
            });
            menu.ShowAsContext();
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
                    DeucarianThemingMenuActions.SelectAndPing(composerEditingStyle));
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
                    composerSize);
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
                    composerSize);
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

        private static bool DrawWorkbenchAction(
            string text,
            bool enabled = true,
            bool primary = false,
            params GUILayoutOption[] options)
        {
            using (new EditorGUI.DisabledScope(!enabled))
            {
                return GUILayout.Button(
                    text,
                    primary
                        ? DeucarianEditorWorkbenchGUI.PrimaryButtonStyle
                        : DeucarianEditorWorkbenchGUI.SecondaryButtonStyle,
                    options);
            }
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
            EditorGUI.LabelField(labelRect, label);

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
