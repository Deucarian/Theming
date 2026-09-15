using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    /// <summary>Auditions semantic audio using an explicit experience, independent of build target.</summary>
    public sealed partial class DeucarianAudioPaletteLabWindow : EditorWindow
    {
        private const string ExperiencePreferenceKey =
            "Deucarian.Theming.AudioPaletteLab.Experience";
        private static readonly string[] ExperienceLabels =
            { "Default", "XR", "WebGL", "Desktop", "Mobile" };

        [SerializeField] private DeucarianAudioPaletteSet paletteSet;
        [SerializeField] private DeucarianTheme theme;
        [SerializeField] private DeucarianAudioExperience experience;
        [SerializeField] private int categoryFilter;
        [SerializeField] private string search = string.Empty;
        [SerializeField] private bool advanced;
        [SerializeField] private bool useIntensity;
        [SerializeField, Range(0, 1)] private float intensity = 0.5f;
        private AudioPaletteWorkspace workspace;
        private AudioClip lastClip;
        [SerializeField] private DeucarianAudioRole selectedRole;
        private IDeucarianAudioPreviewService preview;
        private int previewSequence;
        private int previousVariant = -1;
        private string feedback = "Select an Audio Palette Set and role.";

        public static void OpenWindow()
        {
            DeucarianAudioPaletteLabWindow window = DeucarianEditorWindowPages.GetStandalone<DeucarianAudioPaletteLabWindow>(
                "Audio Palette Lab");
            window.navigation?.Navigate(DeucarianEditorWorkspaceNavigation.AudioToolId);
            DeucarianEditorWorkspace.ConfigureWindow(window);
            window.TryAdoptSelection();
            window.workspace?.Refresh();
            window.Show();
            window.Focus();
        }

        public static void Open(DeucarianAudioPaletteSet set)
        {
            OpenWindow();
            DeucarianAudioPaletteLabWindow window = DeucarianEditorWindowPages.GetStandalone<DeucarianAudioPaletteLabWindow>();
            window.HandlePaletteSetChanged(set);
            window.workspace?.Refresh();
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
            navigation?.Dispose();
            navigation = null;
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            StopPreview();
            workspace?.Dispose();
            workspace = null;
        }
        public void CreateGUI()
        {
            navigation?.Dispose();
            navigation = new DeucarianEditorPageSession(this, DeucarianEditorWorkspaceNavigation.AudioToolId, BuildPage, ActivatePage, deactivateHome: StopPreview);
        }

        internal static IDeucarianEditorPage CreatePage() =>
            DeucarianEditorWindowPages.Create<DeucarianAudioPaletteLabWindow>(
                (window, root) => window.BuildPage(root), activate: (window, route) => window.ActivatePage(route), deactivate: window => window.StopPreview());

        private void ActivatePage(string route)
        {
            if (route != null && route.StartsWith("palette:", StringComparison.Ordinal))
            {
                string path = AssetDatabase.GUIDToAssetPath(route.Substring(8));
                var selected = AssetDatabase.LoadAssetAtPath<DeucarianAudioPaletteSet>(path);
                if (selected != null) HandlePaletteSetChanged(selected);
            }
            workspace?.Refresh();
        }

        private DeucarianEditorPageSession navigation;
        private VisualElement pageRoot;
        private VisualElement PageRoot => pageRoot ?? rootVisualElement;

        private void BuildPage(VisualElement root)
        {
            pageRoot = root;
            workspace?.Dispose();
            PageRoot.Clear();
            workspace = new AudioPaletteWorkspace(this);
        }

        private void OnProjectChange() => workspace?.Refresh();

        private IReadOnlyList<DeucarianAudioRole> CollectRoles() => DeucarianAudioRoleBrowserModel.Collect(paletteSet, experience);

        private bool MatchesSearch(DeucarianAudioRole role) => DeucarianAudioRoleBrowserModel.MatchesSearch(role, search, categoryFilter);

        private bool TryResolve(out DeucarianAudioResolution resolution)
        {
            resolution = DeucarianAudioResolution.Missing;
            return paletteSet != null && selectedRole != null &&
                paletteSet.TryResolve(selectedRole, experience, out resolution);
        }

        private void Play(DeucarianAudioCue cue, bool processed = true)
        {
            if (!DeucarianThemeRuntimeResolver.UseAudio)
            {
                StopPreview();
                feedback = "Audio is off in Project setup.";
                return;
            }
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

            var modifiers = useIntensity ? DeucarianAudioPlaybackModifiers.FromIntensity(intensity) : DeucarianAudioPlaybackModifiers.Identity;
            float volume = modifiers.ApplyVolume(cue.Volume);
            float pitch = modifiers.ApplyPitch(cue.ResolvePitch((previewSequence * 0.618034f) % 1));
            var processedPreview = preview as IDeucarianProcessedAudioPreviewService;
            bool played = processed && processedPreview != null ? processedPreview.PlayProcessed(clip, volume, pitch) : preview.Play(clip);
            if (played)
            {
                lastClip = clip;
                previousVariant = variant;
                feedback = processed && processedPreview != null
                    ? $"{clip.name} · volume {volume:0.00} · pitch {pitch:0.00} · {experience}"
                    : $"Original clip: {clip.name} · {experience}";
            }
            else
            {
                feedback = processedPreview?.LastError ?? "Unity could not start the editor preview. Check that editor audio is enabled.";
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
            lastClip = null;
            previousVariant = -1;
            feedback = string.Empty;
        }

        private void HandlePaletteSetChanged(DeucarianAudioPaletteSet set)
        {
            StopPreview();
            paletteSet = set;
            lastClip = null;
            selectedRole = null;
            feedback = set != null ? $"Loaded {set.name}." : "Select an Audio Palette Set.";
        }

        private void HandleExperienceChanged(DeucarianAudioExperience value)
        {
            StopPreview();
            experience = value;
            lastClip = null;
            DeucarianEditorProjectPreferences.SetInt(ExperiencePreferenceKey, (int)value);
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
            if (paletteSet == null)
            {
                paletteSet = DeucarianThemeRuntimeResolver.LoadSettings()?.DefaultAudioPaletteSet ?? DeucarianAudioDefaults.LoadPaletteSet();
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
                int stored = DeucarianEditorProjectPreferences.GetInt(
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
