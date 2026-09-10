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
        private const float PreviewStackBreakpoint = 760f;

        [SerializeField] private ViewMode viewMode;
        [SerializeField] private Vector2 scrollPosition;
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

        [SerializeField] private DeucarianThemeStyleDraft composer = new DeucarianThemeStyleDraft();
        private DeucarianThemeManagerSelection baselineSelection;
        private DeucarianThemeRuntimeSettings baselineRuntimeSettings;
        private bool baselineCaptured;
        private bool runtimeCandidateTouched;

        private DeucarianEditorWorkspace workspace;
        private ThemeWorkspaceContent workspaceContent;
        private DeucarianThemeManagerToolbar toolbarView;
        private DeucarianEditorWorkbenchFooter workbenchFooter;
        private DeucarianEditorWorkbenchDrawer developerToolsDrawer;
        private Button developerToolsButton;
        private bool developerToolsOpen;
        private IReadOnlyList<string> currentPendingChanges = Array.Empty<string>();
        private int runtimeSettingsResourceCount;

        internal DeucarianEditorWorkspace WorkspaceForTests => workspace;
        internal DeucarianEditorWorkbenchFooter FooterForTests => workbenchFooter;
        internal DeucarianEditorWorkbenchDrawer DeveloperToolsDrawerForTests => developerToolsDrawer;
    }
}
