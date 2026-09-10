using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianAudioPaletteLabWindow
    {
        private sealed class AudioPaletteWorkspace : IDisposable
        {
            private readonly DeucarianAudioPaletteLabWindow owner;
            private readonly DeucarianEditorCollectionWorkspace view;
            private readonly DeucarianEditorWorkspaceForm context;
            private DeucarianEditorWorkspaceForm details;
            private VisualElement emptyDetails;
            private VisualElement paletteDetails;
            private VisualElement roleDetails;
            private Label selectedHeading;
            private readonly DeucarianThemingEditorFeatureGate featureGate;

            internal AudioPaletteWorkspace(DeucarianAudioPaletteLabWindow owner)
            {
                this.owner = owner;
                view = new DeucarianEditorCollectionWorkspace(owner.PageRoot, Application.productName,
                    "Audio Palette Lab", "Find a role. Hear its sound. Compare experiences.", "audio", "Search audio roles…");
                view.Workspace.PageActions.Add(DeucarianEditorWorkspaceControls.Button("Stop audio", owner.StopPreview));
                var categories = new DeucarianEditorChoiceBar(new[] { "All roles", "UI", "Input", "Feedback" }, owner.categoryFilter, true);
                categories.Changed += value => { owner.categoryFilter = value; Refresh(); };
                view.Workspace.Tabs.Add(categories);
                context = new DeucarianEditorWorkspaceForm(view.Workspace.Scope);
                context.Asset("audio-palette-set", "Palette", typeof(DeucarianAudioPaletteSet), () => owner.paletteSet,
                    value => { owner.HandlePaletteSetChanged(value as DeucarianAudioPaletteSet); Refresh(); });
                context.Choice("audio-experience", "Experience", ExperienceLabels, () => (int)owner.experience,
                    value => { owner.HandleExperienceChanged((DeucarianAudioExperience)value); Refresh(); });
                view.Workspace.SearchField.SetValueWithoutNotify(owner.search);
                view.Workspace.SetSearchPrompt("Search audio roles…");
                view.Workspace.SearchField.RegisterValueChangedCallback(evt => { owner.search = evt.newValue ?? ""; Refresh(); });
                Refresh();
                featureGate = DeucarianThemingEditorFeatureGate.Wrap(view.Workspace, true, owner.StopPreview);
            }

            internal void Refresh()
            {
                context.Refresh();
                var rows = new List<DeucarianEditorCollectionItem>();
                foreach (var role in owner.CollectRoles())
                {
                    if (!owner.MatchesSearch(role)) continue;
                    DeucarianAudioResolution resolution = DeucarianAudioResolution.Missing;
                    bool resolved = owner.paletteSet != null && owner.paletteSet.TryResolve(role, owner.experience, out resolution);
                    string clip = resolved && resolution.Cue.Clip != null ? resolution.Cue.Clip.name : resolved && resolution.Cue.IntentionalSilence ? "Intentionally silent" : "No clip";
                    string source = resolved ? resolution.Source.ToString() : "Missing";
                    rows.Add(new DeucarianEditorCollectionItem(role.Id, role.DisplayName, clip, source,
                        () => { owner.SelectRole(role); Refresh(); }, "Play",
                        () => { owner.SelectRole(role); PlaySelected(); Refresh(); },
                        resolved && resolution.IsAudible && owner.preview != null && owner.preview.IsAvailable));
                }
                view.SetItems(rows, owner.selectedRole != null ? owner.selectedRole.Id : null,
                    owner.paletteSet == null ? "Choose a project palette or try the package defaults." : "No roles match your search.");
                if (details == null) BuildDetails();
                DeucarianEditorWorkspaceControls.Show(emptyDetails, owner.paletteSet == null);
                DeucarianEditorWorkspaceControls.Show(paletteDetails, owner.paletteSet != null);
                DeucarianEditorWorkspaceControls.Show(roleDetails, owner.selectedRole != null);
                selectedHeading.text = owner.selectedRole != null ? owner.selectedRole.DisplayName : "Select a role";
                details.Refresh();
                paletteForm.Refresh();
                roleForm.Refresh();
                view.Workspace.FooterLeading.text = owner.feedback;
                view.Workspace.FooterTrailing.text = "Editor audition · " + owner.experience;
                featureGate?.Refresh();
            }

            private void BuildDetails()
            {
                details = new DeucarianEditorWorkspaceForm(view.Details);
                {
                    var empty = details.Section("Choose your audio");
                    emptyDetails = empty.Root;
                    empty.Note(() => "Select a project palette to hear your application’s sounds. Package defaults are an explicit audition choice.");
                    empty.Action("audio-project-palettes", "Browse project palettes…", BrowsePalettes);
                    empty.Action("audio-defaults", "Try package defaults", () => { owner.HandlePaletteSetChanged(DeucarianAudioDefaults.LoadPaletteSet()); Refresh(); });
                }
                paletteDetails = new VisualElement();
                view.Details.Add(paletteDetails);
                paletteForm = new DeucarianEditorWorkspaceForm(paletteDetails);
                var selected = paletteForm.Section("Select a role");
                selectedHeading = selected.Root.Q<Label>();
                roleDetails = new VisualElement();
                selected.Root.Add(roleDetails);
                roleForm = new DeucarianEditorWorkspaceForm(roleDetails);
                {
                    roleForm.ReadOnly("audio-resolved-source", "Source", () => owner.TryResolve(out var resolution) ? DescribeSource(resolution) : "No matching cue");
                    roleForm.ReadOnly("audio-resolved-clip", "Clip", () => {
                        if (!owner.TryResolve(out var resolution)) return "No clip";
                        var clip = owner.lastClip != null ? owner.lastClip : resolution.Cue.Clip;
                        return clip != null ? clip.name : resolution.Cue.IntentionalSilence ? "Intentionally silent" : "No clip";
                    });
                    roleForm.ReadOnly("audio-cue-levels", "Volume / pitch", () => owner.TryResolve(out var r)
                        ? $"{r.Cue.Volume:0.00} · {r.Cue.MinimumPitch:0.00}–{r.Cue.MaximumPitch:0.00}" : "—");
                    roleForm.Note(() => owner.TryResolve(out var r) ? DescribeResolution(r) : "Assign a cue in the source palette.");
                    roleForm.Action("audio-play", "Play processed", () => { PlaySelected(); Refresh(); }, CanPlay, true);
                    roleForm.Action("audio-original", "Play original clip", () => { PlaySelected(false); Refresh(); }, CanPlay);
                    roleForm.Action("audio-locate", "Locate source palette", () => DeucarianEditorSelection.SelectAndPing(owner.ResolveRelevantPalette()));
                }
                var modifiers = paletteForm.Section("Press intensity", true);
                modifiers.Toggle("audio-use-intensity", "Simulate intensity", () => owner.useIntensity, value => { owner.useIntensity = value; Refresh(); });
                var intensityField = modifiers.Slider("audio-intensity", "Intensity", 0, 1, () => owner.intensity, value => { owner.intensity = float.IsNaN(value) ? 0.5f : Mathf.Clamp01(value); Refresh(); });
                modifiers.VisibleWhen(intensityField, () => owner.useIntensity);
                var pad = paletteForm.Section("Test pad", true);
                for (int i = 0; i < TestPadRoleIds.Length; i++)
                {
                    string id = TestPadRoleIds[i];
                    pad.Action("audio-pad-" + id, TestPadLabels[i], () => { owner.SelectRole(owner.FindRole(id)); PlaySelected(); Refresh(); },
                        () => CanPlayRole(id));
                }
                var assets = paletteForm.Section("Assets & coverage", true);
                assets.Asset("audio-theme", "Theme", typeof(DeucarianTheme), () => owner.theme,
                    value => { owner.HandleThemeChanged(value as DeucarianTheme); Refresh(); });
                assets.Action("audio-browse", "Browse project palettes…", BrowsePalettes);
                assets.Action("audio-defaults", "Try package defaults", () => { owner.HandlePaletteSetChanged(DeucarianAudioDefaults.LoadPaletteSet()); Refresh(); });
                assets.Note(() => {
                    if (owner.paletteSet == null) return "No palette.";
                    var warnings = owner.paletteSet.GetValidationWarnings();
                    var relevant = owner.ResolveRelevantPalette();
                    if (relevant != null) warnings.AddRange(relevant.GetValidationWarnings());
                    return warnings.Count == 0 ? "No palette validation warnings." : string.Join("\n", warnings);
                });
                paletteForm.Note(() => owner.preview == null || !owner.preview.IsAvailable
                    ? Application.isBatchMode ? "Audio playback is unavailable in headless mode." : "Audio playback is unavailable in this editor."
                    : "Selection and experience changes never play audio automatically.");
            }

            private DeucarianEditorWorkspaceForm paletteForm;
            private DeucarianEditorWorkspaceForm roleForm;

            private bool CanPlay() => owner.TryResolve(out var resolution) && resolution.IsAudible && owner.preview != null && owner.preview.IsAvailable;
            private bool CanPlayRole(string id)
            {
                var role = owner.FindRole(id);
                return role != null && owner.paletteSet != null && owner.paletteSet.TryResolve(role, owner.experience, out var r)
                    && r.IsAudible && owner.preview != null && owner.preview.IsAvailable;
            }
            private void PlaySelected(bool processed = true)
            {
                if (CanPlay() && owner.TryResolve(out var resolution)) owner.Play(resolution.Cue, processed);
            }
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
            public void Dispose() { featureGate?.Dispose(); view.Dispose(); }
        }
    }
}
