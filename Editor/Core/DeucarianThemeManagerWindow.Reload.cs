using System;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianThemeManagerWindow
    {
        /// <summary>Captures only authoring choices; project assets remain owned by Unity's asset serialization.</summary>
        public string CaptureReloadState() => JsonUtility.ToJson(new ReloadState
        {
            view = (int)viewMode, category = paletteCategory, scroll = scrollPosition,
            candidate = Guid(runtimeSettingsCandidate), touched = runtimeCandidateTouched,
            baseline = Guid(baselineRuntimeSettings), captured = baselineCaptured,
            baselineFamily = Guid(baselineSelection.Family), baselineMode = baselineSelection.Mode,
            baselineStyle = Guid(baselineSelection.Style), source = Guid(composer.Source),
            editing = Guid(composer.EditingStyle), surface = Guid(composer.Surface), corners = Guid(composer.Corners),
            border = Guid(composer.Border), size = composer.Size, typography = Guid(composer.Typography)
        });

        public void RestoreReloadState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            var value = JsonUtility.FromJson<ReloadState>(state);
            if (value == null) return;
            viewMode = (ViewMode)Mathf.Clamp(value.view, 0, 2);
            paletteCategory = Mathf.Clamp(value.category, 0, 2);
            scrollPosition = value.scroll;
            runtimeSettingsCandidate = Asset<DeucarianThemeRuntimeSettings>(value.candidate);
            runtimeCandidateTouched = value.touched;
            baselineRuntimeSettings = Asset<DeucarianThemeRuntimeSettings>(value.baseline);
            baselineCaptured = value.captured;
            baselineSelection = new DeucarianThemeManagerSelection(Asset<DeucarianThemeFamily>(value.baselineFamily),
                value.baselineMode, Asset<DeucarianThemeStyle>(value.baselineStyle));
            composer = new DeucarianThemeStyleDraft
            {
                Source = Asset<DeucarianThemeStyle>(value.source), EditingStyle = Asset<DeucarianThemeStyle>(value.editing),
                Surface = Asset<DeucarianThemeSurfaceProfile>(value.surface), Corners = Asset<DeucarianThemeShapeProfile>(value.corners),
                Border = Asset<DeucarianThemeStrokeProfile>(value.border), Size = value.size,
                Typography = Asset<DeucarianThemeTypographyProfile>(value.typography)
            };
            RefreshRuntimeSettingsCandidateValidation();
            if (viewMode == ViewMode.StyleComposer && composer.HasDraft) ApplyComposerPreview();
        }

        private static string Guid(UnityEngine.Object asset) => DeucarianThemingEditorSettings.GetAssetGuid(asset);
        private static T Asset<T>(string guid) where T : UnityEngine.Object => DeucarianThemingEditorSettings.LoadAssetByGuid<T>(guid);

        [Serializable]
        private sealed class ReloadState
        {
            public int view, category;
            public Vector2 scroll;
            public string candidate, baseline, baselineFamily, baselineStyle;
            public bool touched, captured;
            public DeucarianThemeMode baselineMode;
            public string source, editing, surface, corners, border, typography;
            public DeucarianThemeDensity size;
        }
    }
}
