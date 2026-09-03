using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>Auditions semantic audio using an explicit experience, independent of build target.</summary>
    public sealed partial class DeucarianAudioPaletteLabWindow : EditorWindow
    {
        private const string ExperiencePreferenceKey =
            "Deucarian.Theming.AudioPaletteLab.Experience";
        private static readonly string[] ExperienceLabels =
            { "Default", "XR", "WebGL", "Desktop", "Mobile" };

        private DeucarianAudioPaletteSet paletteSet;
        private DeucarianTheme theme;
        private DeucarianAudioExperience experience;
        private int categoryFilter;
        private string search = string.Empty;
        private Vector2 roleScroll;
        private DeucarianAudioRole selectedRole;
        private IDeucarianAudioPreviewService preview;
        private int previewSequence;
        private int previousVariant = -1;
        private string feedback = "Select an Audio Palette Set and role.";

        public static void OpenWindow()
        {
            DeucarianAudioPaletteLabWindow window = GetWindow<DeucarianAudioPaletteLabWindow>(
                "Audio Palette Lab");
            window.minSize = new Vector2(520f, 420f);
            window.TryAdoptSelection();
            window.Show();
            window.Focus();
        }

        public static void Open(DeucarianAudioPaletteSet set)
        {
            OpenWindow();
            DeucarianAudioPaletteLabWindow window = GetWindow<DeucarianAudioPaletteLabWindow>();
            window.paletteSet = set;
            window.selectedRole = null;
            window.Repaint();
        }

        private void OnEnable()
        {
            preview = new DeucarianAudioPreviewService();
            experience = PreviewExperience;
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
            AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            TryAdoptSelection();
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            StopPreview();
        }

        private void OnGUI()
        {
            DeucarianEditorChrome.DrawPackageHeader(
                "theming",
                "Audio Palette Lab",
                "Resolve and audition semantic audio for an explicit product experience.");

            DrawContextFields();
            paletteSet = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Palette Set",
                paletteSet,
                onValueChanged: HandlePaletteSetChanged);
            DeucarianAudioExperience selectedExperience =
                (DeucarianAudioExperience)DeucarianEditorSegmentedControl.Draw(
                (int)experience,
                ExperienceLabels);
            if (selectedExperience != experience)
            {
                HandleExperienceChanged(selectedExperience);
            }
            DeucarianEditorFields.DrawReadonlyTextField(
                "Unity build target",
                EditorUserBuildSettings.activeBuildTarget.ToString());
            if (experience == DeucarianAudioExperience.XR &&
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                EditorGUILayout.HelpBox(
                    "Explicit XR is active. It overrides Android/Mobile inference for this preview.",
                    MessageType.Info);
            }

            DeucarianEditorResponsiveLayoutState layout =
                DeucarianEditorResponsiveLayout.Calculate(position.width, position.height);
            if (layout.Wide)
            {
                EditorGUILayout.BeginHorizontal();
                DrawRoleBrowser();
                DrawPreview();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawRoleBrowser();
                DrawPreview();
            }

            DrawTestPad();
            DrawValidationSummary();

            DeucarianEditorStatusPanel.DrawStatusBar(
                paletteSet != null ? paletteSet.name : "No palette set",
                feedback,
                experience.ToString());
        }

        private void DrawRoleBrowser()
        {
            DeucarianEditorChrome.BeginSection();
            DeucarianEditorChrome.DrawSectionHeader("Semantic roles");
            search = DeucarianEditorSearchField.Draw(search, "Search roles");
            categoryFilter = DeucarianEditorSegmentedControl.Draw(
                categoryFilter,
                new[] { "All", "UI", "Input", "Feedback" });

            IReadOnlyList<DeucarianAudioRole> roles = CollectRoles();
            roleScroll = EditorGUILayout.BeginScrollView(roleScroll, GUILayout.MinHeight(150f));
            int shown = 0;
            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianAudioRole role = roles[i];
                if (!MatchesSearch(role))
                {
                    continue;
                }

                shown++;
                bool selected = role == selectedRole;
                if (GUILayout.Toggle(
                    selected,
                    DescribeRoleRow(role),
                    selected ? DeucarianEditorButtons.PrimaryStyle : DeucarianEditorButtons.SecondaryStyle))
                {
                    SelectRole(role);
                }
            }

            if (shown == 0)
            {
                EditorGUILayout.HelpBox(
                    paletteSet == null ? "Choose a palette set." : "No matching roles were found.",
                    MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            DeucarianEditorChrome.EndSection();
        }

        private void DrawPreview()
        {
            DeucarianEditorPreviewLabChrome.Begin(
                "Resolved preview",
                "Playback is manual. Changing role or experience never auto-plays sound.");

            if (!TryResolve(out DeucarianAudioResolution resolution))
            {
                DeucarianEditorStatusPanel.DrawStatusCard(
                    "Select a role with a resolvable cue.",
                    DeucarianEditorStatus.Info);
                DeucarianEditorPreviewLabChrome.End();
                return;
            }

            DeucarianEditorFields.DrawReadonlyTextField("Role", selectedRole.Id);
            DeucarianEditorFields.DrawReadonlyTextField("Source", DescribeSource(resolution));
            DeucarianEditorFields.DrawReadonlyTextField(
                "Volume / pitch",
                $"{resolution.Cue.Volume:0.00}  ·  {resolution.Cue.MinimumPitch:0.00}–{resolution.Cue.MaximumPitch:0.00}");

            DeucarianEditorStatus status = resolution.Cue.IntentionalSilence
                ? DeucarianEditorStatus.Info
                : resolution.IsAudible
                    ? DeucarianEditorStatus.Success
                    : DeucarianEditorStatus.Warning;
            DeucarianEditorStatusPanel.DrawStatusCard(DescribeResolution(resolution), status);

            EditorGUILayout.BeginHorizontal();
            bool canPlay = preview != null && preview.IsAvailable && resolution.IsAudible;
            if (DeucarianEditorMiniToolbar.Button("Play next variant", canPlay))
            {
                Play(resolution.Cue);
            }

            if (DeucarianEditorMiniToolbar.Button("Stop", preview != null && preview.IsAvailable))
            {
                StopPreview();
                feedback = "Preview stopped.";
            }

            DeucarianEditorMiniToolbar.SelectButton(resolution.SourcePalette);
            EditorGUILayout.EndHorizontal();

            if (preview == null || !preview.IsAvailable)
            {
                EditorGUILayout.HelpBox(
                    Application.isBatchMode
                        ? "Audio preview is disabled in headless mode."
                        : "This Unity editor version does not expose audio preview.",
                    MessageType.Info);
            }

            DeucarianEditorPreviewLabChrome.End();
        }

        private IReadOnlyList<DeucarianAudioRole> CollectRoles()
        {
            List<DeucarianAudioRole> roles = new List<DeucarianAudioRole>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            AddPaletteRoles(paletteSet != null ? paletteSet.DefaultPalette : null, roles, ids);
            if (paletteSet != null)
            {
                AddPaletteRoles(paletteSet.GetPalette(experience), roles, ids);
            }

            roles.Sort((left, right) => string.Compare(
                left.DisplayName,
                right.DisplayName,
                StringComparison.OrdinalIgnoreCase));
            return roles;
        }

        private static void AddPaletteRoles(
            DeucarianAudioPalette palette,
            ICollection<DeucarianAudioRole> roles,
            ISet<string> ids)
        {
            if (palette == null || palette.RoleLibrary == null)
            {
                return;
            }

            IReadOnlyList<DeucarianAudioRole> source = palette.RoleLibrary.Roles;
            for (int i = 0; i < source.Count; i++)
            {
                DeucarianAudioRole role = source[i];
                if (role != null && ids.Add(role.Id))
                {
                    roles.Add(role);
                }
            }
        }

        private bool MatchesSearch(DeucarianAudioRole role)
        {
            if (role == null)
            {
                return false;
            }

            string value = search == null ? string.Empty : search.Trim();
            bool categoryMatches = categoryFilter == 0
                || categoryFilter == 1 && role.Category == DeucarianAudioRoleCategories.UI
                || categoryFilter == 2 && role.Category == DeucarianAudioRoleCategories.Input
                || categoryFilter == 3 && role.Category == DeucarianAudioRoleCategories.Feedback;
            return categoryMatches && (value.Length == 0
                || role.DisplayName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
                || role.Category.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
                || role.Id.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool TryResolve(out DeucarianAudioResolution resolution)
        {
            resolution = DeucarianAudioResolution.Missing;
            return paletteSet != null && selectedRole != null &&
                paletteSet.TryResolve(selectedRole, experience, out resolution);
        }

        private void Play(DeucarianAudioCue cue)
        {
            previewSequence++;
            if (!cue.TrySelectVariant(
                    previewSequence,
                    previousVariant,
                    out AudioClip clip,
                    out int variant))
            {
                feedback = "The resolved cue has no audible clip.";
                return;
            }

            if (preview.Play(clip))
            {
                previousVariant = variant;
                feedback = $"Playing {clip.name} for {experience}.";
            }
            else
            {
                feedback = "Unity could not start the editor preview.";
            }
        }

        private void SelectRole(DeucarianAudioRole role)
        {
            if (selectedRole == role)
            {
                return;
            }

            StopPreview();
            selectedRole = role;
            previousVariant = -1;
            feedback = role != null ? $"Selected {role.DisplayName}." : "No role selected.";
        }

        private void HandlePaletteSetChanged(DeucarianAudioPaletteSet set)
        {
            StopPreview();
            paletteSet = set;
            selectedRole = null;
            feedback = set != null ? $"Loaded {set.name}." : "Select an Audio Palette Set.";
        }

        private void HandleExperienceChanged(DeucarianAudioExperience value)
        {
            StopPreview();
            experience = value;
            EditorPrefs.SetInt(ExperiencePreferenceKey, (int)value);
            previousVariant = -1;
            feedback = $"Preview experience changed to {experience}.";
        }

        private void TryAdoptSelection()
        {
            if (Selection.activeObject is DeucarianAudioPaletteSet set)
            {
                paletteSet = set;
            }
            else if (Selection.activeObject is DeucarianTheme theme && theme.AudioPaletteSet != null)
            {
                this.theme = theme;
                paletteSet = theme.AudioPaletteSet;
            }
        }

        private void HandlePlayModeChanged(PlayModeStateChange state)
        {
            StopPreview();
        }

        private void StopPreview()
        {
            preview?.Stop();
        }

        private static string DescribeSource(DeucarianAudioResolution resolution)
        {
            return resolution.SourcePalette != null
                ? $"{resolution.Source} · {resolution.SourcePalette.name}"
                : resolution.Source.ToString();
        }

        private static string DescribeResolution(DeucarianAudioResolution resolution)
        {
            if (resolution.Cue.IntentionalSilence)
            {
                return "This role is intentionally silent for the selected experience.";
            }

            return resolution.IsAudible
                ? "An audible cue is assigned and ready to preview."
                : "The fallback chain resolved, but it contains no audio clip.";
        }

        internal static DeucarianAudioExperience PreviewExperience
        {
            get
            {
                int stored = EditorPrefs.GetInt(
                    ExperiencePreferenceKey,
                    (int)DeucarianAudioExperience.Default);
                return Enum.IsDefined(typeof(DeucarianAudioExperience), stored)
                    ? (DeucarianAudioExperience)stored
                    : DeucarianAudioExperience.Default;
            }
        }

        internal void SetPreviewServiceForTests(IDeucarianAudioPreviewService service)
        {
            StopPreview();
            preview = service;
        }

        internal bool PreviewForTests(DeucarianAudioCue cue)
        {
            Play(cue);
            return preview != null && preview.IsPlaying;
        }

        internal void StopPreviewForTests()
        {
            StopPreview();
        }

        internal void ChangeExperienceForTests(DeucarianAudioExperience value)
        {
            HandleExperienceChanged(value);
        }

        internal void DisableForTests()
        {
            OnDisable();
        }
    }
}
