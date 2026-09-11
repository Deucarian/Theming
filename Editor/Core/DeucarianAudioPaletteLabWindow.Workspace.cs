using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ui = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianAudioPaletteLabWindow
    {
        private sealed class AudioPaletteWorkspace : IDisposable
        {
            private readonly DeucarianAudioPaletteLabWindow owner;
            private readonly DeucarianEditorCollectionWorkspace view;
            private readonly DeucarianEditorWorkspaceForm context;
            private readonly DeucarianThemingEditorFeatureGate featureGate;
            private readonly Deucarian.Editor.Definitions.DeucarianDefinitionPanel definitions;
            private DeucarianEditorWorkspaceForm details;
            private DeucarianAudioCueForm cueForm;
            private DeucarianAudioRole renderedRole;
            private DeucarianAudioPalette renderedPalette;
            private bool renderedEmpty, built;
            private Label feedback;

            internal AudioPaletteWorkspace(DeucarianAudioPaletteLabWindow owner)
            {
                this.owner = owner;
                view = new DeucarianEditorCollectionWorkspace(owner.PageRoot, Application.productName,
                    "Audio palettes", "Assign and preview sounds for UI, input and feedback.", "audio", "Find an audio role…");
                view.Workspace.SetScopeBeforeTabs();
                view.Workspace.SetScopeStacked();
                Ui.Show(view.Workspace.Footer, false);
                var categories = new DeucarianEditorChoiceBar(new[] { "All roles", "UI", "Input", "Feedback" }, owner.categoryFilter, true);
                categories.Changed += value => { owner.categoryFilter = value; Refresh(); };
                view.Workspace.Tabs.Add(categories);
                var definitionRoot = Ui.Region("audio-definitions", "dw-content");
                view.Workspace.Content.Add(definitionRoot);
                definitions = new Deucarian.Editor.Definitions.DeucarianDefinitionPanel(definitionRoot,
                    new Definitions.AudioRoleDefinitionSchema(), asset => owner.Play(((DeucarianAudioRole)asset).DefaultCue));
                Ui.Show(definitionRoot, false);
                var mode = new DeucarianEditorChoiceBar(new[] { "Palettes", "Definitions" }, 0, true);
                mode.Changed += value =>
                {
                    owner.StopPreview();
                    Ui.Show(view.Collection, value == 0);
                    Ui.Show(view.Workspace.Scope, value == 0);
                    Ui.Show(categories, value == 0);
                    Ui.Show(definitionRoot, value == 1);
                };
                view.Workspace.Tabs.Insert(0, mode);
                context = new DeucarianEditorWorkspaceForm(view.Workspace.Scope);
                context.Asset("audio-palette-set", "Palette set", typeof(DeucarianAudioPaletteSet), () => owner.paletteSet,
                    value => { owner.HandlePaletteSetChanged(value as DeucarianAudioPaletteSet); Refresh(); });
                context.Choice("audio-experience", "Experience", ExperienceLabels, () => (int)owner.experience,
                    value => { owner.HandleExperienceChanged((DeucarianAudioExperience)value); Refresh(); });
                view.Workspace.SearchField.SetValueWithoutNotify(owner.search);
                view.Workspace.SearchField.RegisterValueChangedCallback(evt => { owner.search = evt.newValue ?? ""; Refresh(); });
                Refresh();
                featureGate = DeucarianThemingEditorFeatureGate.Wrap(view.Workspace, true, owner.StopPreview);
                Undo.undoRedoPerformed += Refresh;
                DeucarianThemeAssetChangeBus.AssetChanged += OnAssetChanged;
            }

            private void OnAssetChanged(UnityEngine.Object asset)
            {
                if (asset is DeucarianAudioPalette || asset is DeucarianAudioPaletteSet || asset is DeucarianAudioRoleLibrary) Refresh();
            }

            internal void Refresh()
            {
                context.Refresh();
                var matching = new List<DeucarianAudioRole>();
                foreach (var role in owner.CollectRoles()) if (owner.MatchesSearch(role)) matching.Add(role);
                if (owner.selectedRole == null || !matching.Contains(owner.selectedRole))
                    owner.SelectRole(matching.Count > 0 ? matching[0] : null);
                var rows = new List<DeucarianEditorCollectionItem>();
                foreach (var role in matching)
                {
                    var resolution = DeucarianAudioResolution.Missing;
                    bool resolved = owner.paletteSet != null && owner.paletteSet.TryResolve(role, owner.experience, out resolution);
                    string clip = resolved && resolution.Cue.Clip != null ? resolution.Cue.Clip.name
                        : resolved && resolution.Cue.IntentionalSilence ? "Intentionally silent" : "No clip";
                    rows.Add(new DeucarianEditorCollectionItem(role.Id, role.DisplayName, clip, null,
                        () => { owner.SelectRole(role); Refresh(); }, "Play sound",
                        () => { owner.SelectRole(role); PlaySelected(); Refresh(); },
                        resolved && resolution.IsAudible && owner.preview != null && owner.preview.IsAvailable,
                        RoleIcon(role), DeucarianEditorIconIds.Play));
                }
                view.SetItems(rows, owner.selectedRole != null ? owner.selectedRole.Id : null,
                    owner.paletteSet == null ? "Choose your project's audio palette set." : "No roles match your search.");
                var source = owner.TryResolve(out var selected) ? selected.SourcePalette : null;
                if (!built || renderedRole != owner.selectedRole || renderedPalette != source ||
                    renderedEmpty != (owner.paletteSet == null) || (cueForm != null && !cueForm.MatchesSource)) BuildDetails();
                cueForm?.Refresh();
                details?.Refresh();
                if (feedback != null) { feedback.text = owner.feedback; Ui.Show(feedback, !string.IsNullOrEmpty(owner.feedback)); }
                featureGate?.Refresh();
            }

            private void BuildDetails()
            {
                cueForm?.Dispose(); cueForm = null; feedback = null;
                built = true; renderedRole = owner.selectedRole; renderedEmpty = owner.paletteSet == null;
                renderedPalette = owner.TryResolve(out var resolution) ? resolution.SourcePalette : null;
                var root = view.Details; root.Clear();
                details = new DeucarianEditorWorkspaceForm(root);
                if (renderedEmpty)
                {
                    root.Add(Ui.Label("Choose your audio", "dw-section-title"));
                    root.Add(Ui.Label("Use a project palette, or audition the package defaults.", "dw-muted"));
                    root.Add(Ui.Actions(Ui.Button("Browse project palettes…", BrowsePalettes),
                        Ui.Button("Try package defaults", () => { owner.HandlePaletteSetChanged(DeucarianAudioDefaults.LoadPaletteSet()); Refresh(); })));
                    return;
                }
                if (owner.selectedRole == null) { root.Add(Ui.Label("Select a role", "dw-section-title")); return; }
                root.Add(Ui.Label(owner.selectedRole.DisplayName, "dw-detail-title"));
                cueForm = new DeucarianAudioCueForm(root, owner.selectedRole,
                    () => owner.TryResolve(out var current) ? current : DeucarianAudioResolution.Missing,
                    () => { owner.StopPreview(); owner.lastClip = null; Refresh(); });
                root.Add(Ui.Divider());
                var play = Ui.IconButton("Play sound", DeucarianEditorIconIds.Play,
                    () => { PlaySelected(); Refresh(); }, DeucarianEditorButtonRole.Primary);
                play.name = "audio-play";
                var locate = Ui.IconButton("Locate clip", DeucarianEditorIconIds.Folder,
                    () => { if (owner.TryResolve(out var current)) DeucarianEditorSelection.SelectAndPing(current.Cue.Clip); });
                locate.name = "audio-locate";
                root.Add(Ui.Actions(play, locate));
                feedback = Ui.Label(owner.feedback, "dw-muted"); root.Add(feedback);
                play.schedule.Execute(() => { play.SetEnabled(CanPlay()); locate.SetEnabled(owner.TryResolve(out var r) && r.Cue.Clip != null); }).Every(250);
                BuildAdvanced(details);
            }

            private void BuildAdvanced(DeucarianEditorWorkspaceForm parent)
            {
                var advanced = parent.Section("More options", true);
                advanced.ReadOnly("audio-resolved-source", "Source palette", () => owner.TryResolve(out var r) ? DescribeSource(r) : "Role default");
                advanced.Action("audio-locate-palette", "Locate source palette", () => DeucarianEditorSelection.SelectAndPing(renderedPalette));
                advanced.Action("audio-save-palette", "Save sound changes", () =>
                {
                    if (CanSavePalette()) AssetDatabase.SaveAssetIfDirty(renderedPalette);
                }, CanSavePalette);
                advanced.Action("audio-original", "Play original clip", () => { PlaySelected(false); Refresh(); }, CanPlay);
                advanced.Action("audio-stop", "Stop audio", owner.StopPreview);
                var modifiers = advanced.Section("Press intensity", true);
                modifiers.Toggle("audio-use-intensity", "Simulate intensity", () => owner.useIntensity, value => { owner.useIntensity = value; Refresh(); });
                var intensityField = modifiers.Slider("audio-intensity", "Intensity", 0, 1, () => owner.intensity,
                    value => { owner.intensity = float.IsNaN(value) ? 0.5f : Mathf.Clamp01(value); Refresh(); });
                modifiers.VisibleWhen(intensityField, () => owner.useIntensity);
                var pad = advanced.Section("Test pad", true);
                for (int i = 0; i < TestPadRoleIds.Length; i++)
                {
                    string id = TestPadRoleIds[i];
                    pad.Action("audio-pad-" + id, TestPadLabels[i], () => { owner.SelectRole(owner.FindRole(id)); PlaySelected(); Refresh(); }, () => CanPlayRole(id));
                }
                advanced.Asset("audio-theme", "Theme", typeof(DeucarianTheme), () => owner.theme,
                    value => { owner.HandleThemeChanged(value as DeucarianTheme); Refresh(); });
                advanced.Action("audio-browse", "Browse project palettes…", BrowsePalettes);
                advanced.Note(() => owner.paletteSet == null ? string.Empty : string.Join("\n", owner.paletteSet.GetValidationWarnings()));
            }

            private bool CanPlay() => DeucarianThemeRuntimeResolver.UseAudio && owner.TryResolve(out var r) && r.IsAudible && owner.preview != null && owner.preview.IsAvailable;
            private bool CanSavePalette() => DeucarianThemeRuntimeResolver.UseAudio && renderedPalette != null &&
                EditorUtility.IsDirty(renderedPalette) && AssetDatabase.GetAssetPath(renderedPalette).Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal);
            private bool CanPlayRole(string id)
            {
                var role = owner.FindRole(id);
                return DeucarianThemeRuntimeResolver.UseAudio && role != null && owner.paletteSet != null &&
                    owner.paletteSet.TryResolve(role, owner.experience, out var r) && r.IsAudible && owner.preview != null && owner.preview.IsAvailable;
            }
            private void PlaySelected(bool processed = true)
            { if (CanPlay() && owner.TryResolve(out var resolution)) owner.Play(resolution.Cue, processed); }
            private static string RoleIcon(DeucarianAudioRole role) => role.Id == DeucarianBuiltinAudioRoleIds.Warning
                ? DeucarianEditorIconIds.Warning : role.Id == DeucarianBuiltinAudioRoleIds.Error ? DeucarianEditorIconIds.Error
                : role.Category == DeucarianAudioRoleCategories.Input ? DeucarianEditorIconIds.Keyboard
                : role.Id == DeucarianBuiltinAudioRoleIds.Hover ? DeucarianEditorIconIds.Pointer : DeucarianEditorIconIds.Press;

            private void BrowsePalettes()
            {
                var menu = new GenericMenu();
                string[] guids = AssetDatabase.FindAssets("t:DeucarianAudioPaletteSet", new[] { "Assets" });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    menu.AddItem(new GUIContent(path), false, () => { owner.HandlePaletteSetChanged(AssetDatabase.LoadAssetAtPath<DeucarianAudioPaletteSet>(path)); Refresh(); });
                }
                if (guids.Length == 0) menu.AddDisabledItem(new GUIContent("No project palettes"));
                menu.ShowAsContext();
            }
            public void Dispose()
            {
                Undo.undoRedoPerformed -= Refresh;
                DeucarianThemeAssetChangeBus.AssetChanged -= OnAssetChanged;
                definitions.Dispose(); cueForm?.Dispose(); featureGate?.Dispose(); view.Dispose();
            }
        }
    }
}
