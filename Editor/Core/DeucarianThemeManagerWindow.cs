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
    public sealed partial class DeucarianThemeManagerWindow : EditorWindow
    {
        private const string WallpaperFadeName = "deucarian-theme-manager-top-safe-fade";
        private const string PreferredSizeKey = "Deucarian.Theming.ThemeManager.PreferredSize.920x560";
        private const float PreviewStackBreakpoint = 760f;
        private const float SecondaryActionSlotWidth = 132f;
        private const float DiscardActionSlotWidth = 148f;
        private const float PrimaryActionSlotWidth = 168f;
        private static readonly Vector2 PreferredSize = new Vector2(920f, 560f);

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
        private Button styleComposerViewButton;
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
        private int runtimeSettingsResourceCount;

        internal DeucarianEditorWorkbench WorkbenchForTests => workbench;
        internal DeucarianEditorWorkbenchFooter FooterForTests => workbenchFooter;
        internal DeucarianEditorWorkbenchDrawer DeveloperToolsDrawerForTests => developerToolsDrawer;
    }
}
