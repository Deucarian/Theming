using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianThemeManagerWindow
    {
        private sealed class ThemeWorkspaceContent
        {
            private readonly DeucarianThemeManagerWindow owner;
            private readonly VisualElement themePage;
            private readonly VisualElement setupPage;
            private readonly VisualElement composerPage;
            private readonly DeucarianEditorWorkspaceForm themeForm;
            private readonly DeucarianEditorWorkspaceForm setupForm;

            internal ThemeWorkspaceContent(DeucarianThemeManagerWindow owner, DeucarianEditorWorkspace workspace)
            {
                this.owner = owner;
                var configuration = DeucarianEditorWorkspaceControls.Scroll("theme-configuration");
                var preview = DeucarianEditorWorkspaceControls.Scroll("theme-preview");
                themePage = DeucarianEditorWorkspaceControls.Split(configuration, preview);
                workspace.Content.Add(themePage);
                themeForm = new DeucarianEditorWorkspaceForm(configuration);
                var choice = themeForm.Section("Preview selection");
                choice.Asset("theme-family", "Family", typeof(DeucarianThemeFamily), () => Selection.Family, value => {
                    var family = value as DeucarianThemeFamily;
                    var selection = Selection;
                    SetDraft(family, selection.Mode, DeucarianThemeDraftPolicy.ResolveSuggestedStyle(family, selection.Mode) ?? selection.Style);
                });
                choice.Choice("theme-mode", "Mode", Enum.GetNames(typeof(DeucarianThemeMode)), () => (int)Selection.Mode,
                    value => SetDraft(Selection.Family, (DeucarianThemeMode)value, Selection.Style));
                choice.Asset("theme-style", "Style", typeof(DeucarianThemeStyle), () => Selection.Style,
                    value => SetDraft(Selection.Family, Selection.Mode, value as DeucarianThemeStyle));
                choice.Note(() => Status.IsActive ? "This selection is active in the project." : Status.Message);
                var setup = themeForm.Section("Project connection");
                setup.ReadOnly("theme-active-settings", "Settings", () => owner.projectRuntimeSettings != null ? owner.projectRuntimeSettings.name : "Not configured");
                setup.Action("theme-configure", "Configure project…", owner.NavigateToRuntimeSettings);
                setup.Action("theme-create-family", "Create theme family…", owner.CreateThemeFamily,
                    () => !EditorApplication.isPlayingOrWillChangePlaymode);
                setup.Action("theme-repair-family", "Repair selected family", () => { DeucarianThemingMenuActions.RepairActiveThemeFamilySetup(); owner.RefreshAssets(); },
                    () => Selection.Family != null && !Selection.Family.IsComplete && !EditorApplication.isPlayingOrWillChangePlaymode);
                preview.Add(DeucarianEditorWorkspaceControls.Label("Live preview", "dw-section-title"));
                preview.Add(DeucarianEditorWorkspaceControls.Label("Preview only · project assets change only when you apply.", "dw-muted"));
                preview.Add(DeucarianEditorWorkspaceControls.Embedded(DrawPreview, "theme-live-specimen"));
                setupPage = DeucarianEditorWorkspaceControls.Scroll("theme-project-setup");
                setupForm = new DeucarianEditorWorkspaceForm(setupPage);
                var settings = setupForm.Section("Connect the project");
                settings.Note(() => "One Resources-backed runtime settings asset connects your chosen theme to builds.");
                settings.Asset("theme-runtime-settings", "Settings", typeof(DeucarianThemeRuntimeSettings), () => owner.runtimeSettingsCandidate, value => {
                    owner.runtimeSettingsCandidate = value as DeucarianThemeRuntimeSettings;
                    owner.runtimeCandidateTouched = true;
                    owner.RefreshRuntimeSettingsCandidateValidation();
                    owner.UpdateWorkbenchToolbar();
                });
                settings.Note(() => owner.runtimeSettingsCandidateMessage);
                workspace.Content.Add(setupPage);
                composerPage = DeucarianEditorWorkspaceControls.Embedded(() => {
                    using (DeucarianEditorWorkbenchGUI.BeginEmbeddedPage(GUILayout.ExpandHeight(true)))
                    using (var scroll = new EditorGUILayout.ScrollViewScope(owner.scrollPosition))
                    {
                        owner.scrollPosition = scroll.scrollPosition;
                        owner.DrawStyleComposer();
                        owner.UpdateWorkbenchToolbar();
                    }
                }, "theme-style-composer");
                workspace.Content.Add(composerPage);
                Refresh();
            }

            private DeucarianThemeManagerSelection Selection => DeucarianThemeManagerSelection.FromEditorPrefs();
            private DeucarianThemeManagerActivationStatus Status => DeucarianThemeManagerWorkflow.Evaluate(
                owner.projectRuntimeSettings, Selection, owner.projectRuntimeSettingsResourceReady, owner.projectRuntimeSettingsResourceMessage);

            internal void Refresh()
            {
                DeucarianEditorWorkspaceControls.Show(themePage, owner.viewMode == ViewMode.Theme);
                DeucarianEditorWorkspaceControls.Show(setupPage, owner.viewMode == ViewMode.RuntimeSettings);
                DeucarianEditorWorkspaceControls.Show(composerPage, owner.viewMode == ViewMode.StyleComposer);
                themeForm.Refresh();
                setupForm.Refresh();
            }

            private void SetDraft(DeucarianThemeFamily family, DeucarianThemeMode mode, DeucarianThemeStyle style)
            {
                DeucarianThemeDraftPolicy.SetDraft(family, mode, style);
                owner.UpdateWorkbenchToolbar();
                owner.Repaint();
            }

            private void DrawPreview()
            {
                var selection = Selection;
                if (selection.ResolvedTheme == null)
                {
                    EditorGUILayout.HelpBox("Choose a theme family to preview its colours and controls.", MessageType.Info);
                    return;
                }
                var style = selection.Style;
                DeucarianThemeSpecimenRenderer.DrawThemePreview(selection.ResolvedTheme, style,
                    style != null ? style.SurfaceProfile : null, style != null ? style.ShapeProfile : null,
                    style != null ? style.StrokeProfile : null, style != null ? style.Density : DeucarianThemeDensity.Unspecified,
                    style != null ? style.TypographyProfile : null);
            }
        }
    }
}
