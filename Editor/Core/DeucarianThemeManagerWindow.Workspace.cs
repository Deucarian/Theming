using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ui = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianThemeManagerWindow
    {
        private sealed class ThemeWorkspaceContent
        {
            private readonly DeucarianThemeManagerWindow owner;
            private readonly DeucarianEditorWorkspace workspace;
            private readonly VisualElement formRoot, split, setupPage, composerPage;
            private readonly DeucarianEditorWorkspaceForm context, setupForm, composerForm;
            private readonly DeucarianThemeToolkitSpecimen specimen;
            private readonly DeucarianThemeToolkitSpecimen composerSpecimen;
            private DeucarianEditorWorkspaceForm visualForm;
            private DeucarianThemePaletteForm colors;
            private DeucarianColorPalette renderedPalette;
            private DeucarianThemeStyle renderedStyle;
            private int renderedCategory = -1;

            internal ThemeWorkspaceContent(DeucarianThemeManagerWindow owner, DeucarianEditorWorkspace workspace)
            {
                this.owner = owner; this.workspace = workspace;
                workspace.SetScopeBeforeTabs();
                context = new DeucarianEditorWorkspaceForm(workspace.Scope);
                if (Selection.Family == null && Selection.ResolvedPalette == null)
                    DeucarianThemeDraftPolicy.SetDraft(DeucarianVisualDefaults.LoadFamily(), Selection.Mode, DeucarianVisualDefaults.LoadTheme(Selection.Mode)?.VisualStyle);
                context.AssetWithActions("theme-family", "Theme family", typeof(DeucarianThemeFamily), () => Selection.Family, value => {
                    var family = value as DeucarianThemeFamily;
                    SetDraft(family, Selection.Mode, DeucarianThemeDraftPolicy.ResolveSuggestedStyle(family, Selection.Mode) ?? Selection.Style);
                }, DeucarianThemeAssetCustomization.CreateFamily, DeucarianThemeAssetCustomization.Customize, DeucarianVisualDefaults.LoadFamily);
                context.Choice("theme-mode", "Mode", Enum.GetNames(typeof(DeucarianThemeMode)), () => (int)Selection.Mode,
                    value => SetDraft(Selection.Family, (DeucarianThemeMode)value, Selection.Style));
                context.AssetWithActions("theme-style", "Style", typeof(DeucarianThemeStyle), () => Selection.Style,
                    value => SetDraft(Selection.Family, Selection.Mode, value as DeucarianThemeStyle),
                    customize: DeucarianThemeAssetCustomization.Customize);
                formRoot = Ui.Scroll("theme-configuration");
                var preview = Ui.Scroll("theme-preview");
                specimen = new DeucarianThemeToolkitSpecimen(); preview.Add(specimen.Root);
                split = Ui.Split(formRoot, preview);
                split.AddToClassList("dw-visual-palette-split");
                workspace.Content.Insert(0, split);
                setupPage = Ui.Scroll("theme-project-setup");
                setupForm = new DeucarianEditorWorkspaceForm(setupPage);
                setupPage.Add(Ui.Label("Runtime settings", "dw-section-title"));
                setupForm.AssetWithActions("theme-runtime-settings", "Settings", typeof(DeucarianThemeRuntimeSettings), () => owner.runtimeSettingsCandidate, value => {
                    owner.runtimeSettingsCandidate = value as DeucarianThemeRuntimeSettings;
                    owner.runtimeCandidateTouched = true;
                    owner.RefreshRuntimeSettingsCandidateValidation();
                    owner.UpdateWorkbenchToolbar();
                });
                setupForm.Note(() => owner.runtimeSettingsCandidateMessage);
                setupForm.Action("theme-back-to-palettes", "Back to visual palettes", owner.NavigateToTheme);
                workspace.Content.Insert(1, setupPage);
                composerPage = Ui.Scroll("theme-style-composer");
                composerForm = new DeucarianEditorWorkspaceForm(composerPage);
                composerPage.Add(Ui.Label("Compose a style", "dw-section-title"));
                composerForm.ReadOnly("theme-composer-source", "Based on", () => owner.composer.Source != null ? owner.composer.Source.DisplayName : "Choose a style");
                composerForm.AssetWithActions("theme-composer-surface", "Surface", typeof(DeucarianThemeSurfaceProfile), () => owner.composer.Surface,
                    value => ChangeComposer(() => owner.composer.Surface = value as DeucarianThemeSurfaceProfile), customize: DeucarianThemeAssetCustomization.Customize);
                composerForm.AssetWithActions("theme-composer-corners", "Corners", typeof(DeucarianThemeShapeProfile), () => owner.composer.Corners,
                    value => ChangeComposer(() => owner.composer.Corners = value as DeucarianThemeShapeProfile), customize: DeucarianThemeAssetCustomization.Customize);
                composerForm.AssetWithActions("theme-composer-border", "Border", typeof(DeucarianThemeStrokeProfile), () => owner.composer.Border,
                    value => ChangeComposer(() => owner.composer.Border = value as DeucarianThemeStrokeProfile), customize: DeucarianThemeAssetCustomization.Customize);
                composerForm.Choice("theme-composer-size", "Size", Enum.GetNames(typeof(DeucarianThemeDensity)), () => (int)owner.composer.Size,
                    value => ChangeComposer(() => owner.composer.Size = (DeucarianThemeDensity)value));
                composerForm.AssetWithActions("theme-composer-typography", "Typography", typeof(DeucarianThemeTypographyProfile), () => owner.composer.Typography,
                    value => ChangeComposer(() => owner.composer.Typography = value as DeucarianThemeTypographyProfile), customize: DeucarianThemeAssetCustomization.Customize);
                composerForm.Action("theme-composer-back", "Back to visual palettes", owner.NavigateToTheme);
                composerSpecimen = new DeucarianThemeToolkitSpecimen();
                composerPage.Add(composerSpecimen.Root);
                workspace.Content.Insert(2, composerPage);
                Refresh();
            }

            private DeucarianThemeManagerSelection Selection => DeucarianThemeManagerSelection.FromEditorPrefs();
            internal void Refresh()
            {
                bool theme = owner.viewMode == ViewMode.Theme;
                workspace.Subtitle.text = theme ? "Edit and preview the visual palettes used by your app."
                    : owner.viewMode == ViewMode.StyleComposer ? "Combine surface, shape, border and typography profiles into a style."
                    : "Choose the runtime settings asset used by project theming.";
                Ui.Show(split, theme);
                Ui.Show(workspace.Scope, theme);
                Ui.Show(workspace.Tabs, theme);
                Ui.Show(setupPage, owner.viewMode == ViewMode.RuntimeSettings);
                Ui.Show(composerPage, owner.viewMode == ViewMode.StyleComposer);
                if (renderedPalette != Selection.ResolvedPalette || renderedStyle != Selection.Style || renderedCategory != owner.paletteCategory) BuildVisualForm();
                context.Refresh(); setupForm.Refresh(); composerForm.Refresh(); visualForm?.Refresh(); colors?.Refresh();
                specimen.Refresh(Selection);
                specimen.ShowControls(theme && owner.paletteCategory > 0);
                if (owner.viewMode == ViewMode.StyleComposer) composerSpecimen.Refresh(Selection, owner.composer);
            }

            private void BuildVisualForm()
            {
                formRoot.Clear();
                renderedPalette = Selection.ResolvedPalette; renderedStyle = Selection.Style; renderedCategory = owner.paletteCategory;
                visualForm = new DeucarianEditorWorkspaceForm(formRoot); colors = null;
                if (renderedCategory == 0)
                    colors = new DeucarianThemePaletteForm(formRoot, renderedPalette, () => { specimen.Refresh(Selection); owner.UpdateWorkbenchToolbar(); });
                else
                {
                    if (renderedCategory == 1)
                    {
                        visualForm.ReadOnly("theme-font", "Typography", () => Selection.Style?.TypographyProfile?.DisplayName ?? "Project default");
                        visualForm.ReadOnly("theme-font-title", "Title size", () => Selection.Style?.TypographyProfile?.Title.FontSize.ToString("0.#") ?? "Default");
                        visualForm.ReadOnly("theme-font-body", "Body size", () => Selection.Style?.TypographyProfile?.Body.FontSize.ToString("0.#") ?? "Default");
                    }
                    else
                    {
                        visualForm.ReadOnly("theme-shape", "Corners", () => Selection.Style?.ShapeProfile?.DisplayName ?? "No profile");
                        visualForm.ReadOnly("theme-surface", "Surface", () => Selection.Style?.SurfaceProfile?.DisplayName ?? "No profile");
                        visualForm.ReadOnly("theme-density", "Size", () => Selection.Style != null ? Selection.Style.Density.ToString() : "Default");
                    }
                    visualForm.Action("theme-compose-style", "Compose style", owner.NavigateToStyleComposer, () => Selection.Style != null);
                }
                var more = visualForm.Section("More options", true);
                more.Action("theme-project-setup", "Project setup", () => DeucarianEditorNavigation.Open(owner.PageRoot, DeucarianThemingProjectPage.ToolId));
                more.Action("theme-configure", "Runtime settings", owner.NavigateToRuntimeSettings);
                more.Action("theme-create-family", "Create theme family…", owner.CreateThemeFamily, () => !EditorApplication.isPlayingOrWillChangePlaymode);
                more.Action("theme-repair-family", "Repair selected family", () => { DeucarianThemingMenuActions.RepairActiveThemeFamilySetup(); owner.RefreshAssets(); },
                    () => Selection.Family != null && !Selection.Family.IsComplete && !EditorApplication.isPlayingOrWillChangePlaymode);
                more.Action("theme-developer-tools", "Asset tools", owner.ToggleDeveloperTools);
                more.Action("theme-save-palette", "Save palette", () => AssetDatabase.SaveAssetIfDirty(renderedPalette),
                    () => DeucarianThemePaletteForm.IsProjectOwned(renderedPalette) && EditorUtility.IsDirty(renderedPalette));
            }

            private void ChangeComposer(Action change) { change(); owner.ApplyComposerPreview(); owner.UpdateWorkbenchToolbar(); }
            private void SetDraft(DeucarianThemeFamily family, DeucarianThemeMode mode, DeucarianThemeStyle style)
            { DeucarianThemeDraftPolicy.SetDraft(family, mode, style); owner.UpdateWorkbenchToolbar(); owner.Repaint(); }
        }
    }
}
