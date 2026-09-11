using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemingProjectPage : IDeucarianEditorPage
    {
        internal const string ToolId = "deucarian.theming.project-setup";
        private readonly IDeucarianThemingProjectSettingsStore store;
        private readonly DeucarianThemingConnectionObserver connections;
        private readonly DeucarianEditorWorkspace workspace;
        private readonly DeucarianEditorFeatureSection visual;
        private readonly DeucarianEditorFeatureSection audio;
        private readonly DeucarianEditorWorkspaceForm visualForm;
        private readonly DeucarianEditorWorkspaceForm audioForm;
        private readonly VisualElement connectionList;
        private readonly Label message;
        private bool disposed;
        private bool active;
        private string saveProblem;
        private DeucarianThemeRuntimeSettings settingsSnapshot;

        internal DeucarianThemingProjectPage(IDeucarianThemingProjectSettingsStore store,
            DeucarianThemingConnectionObserver connections)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.connections = connections ?? throw new ArgumentNullException(nameof(connections));
            settingsSnapshot = store.Read();
            Root = new VisualElement();
            workspace = new DeucarianEditorWorkspace(Root, Application.productName);
            workspace.Title.text = "Theming";
            workspace.Subtitle.text = "Choose visual styling and audio for this project.";
            workspace.FooterLeading.text = "Project settings";
            DeucarianEditorWorkspaceControls.Show(workspace.Footer, false);
            DeucarianEditorWorkspaceControls.Show(workspace.Tabs, false);
            DeucarianEditorWorkspaceControls.Show(workspace.Scope, false);
            DeucarianEditorWorkspaceNavigation.Populate(workspace, ToolId);
            var scroll = DeucarianEditorWorkspaceControls.Scroll("theming-project-scroll");
            scroll.AddToClassList("dw-feature-scroll");
            var content = DeucarianEditorWorkspaceControls.Region("theming-project-settings", "dw-feature-page");
            scroll.Add(content);
            workspace.Content.Add(scroll);
            visual = new DeucarianEditorFeatureSection("theming-visual", "Visual styling",
                "Keep the app's existing colors, fonts and shapes.", DeucarianEditorIconIds.Palette,
                value => Write(settings => settings.SetFeatures(value, settings.UseAudio)));
            visualForm = new DeucarianEditorWorkspaceForm(visual.Details);
            visualForm.Asset("theming-visual-family", "Theme family", typeof(DeucarianThemeFamily),
                () => Settings?.DefaultThemeFamily, value => Write(settings => settings.Configure(value as DeucarianThemeFamily, settings.DefaultThemeMode)));
            visualForm.Choice("theming-visual-mode", "Mode", Enum.GetNames(typeof(DeucarianThemeMode)),
                () => (int)(Settings?.DefaultThemeMode ?? DeucarianThemeMode.Dark),
                value => Write(settings => settings.SetDefaultThemeMode((DeucarianThemeMode)value)));
            visualForm.Action("theming-open-visual", "Open Visual Palettes", () =>
                DeucarianEditorNavigation.Open(Root, DeucarianToolIds.ThemeManager, DeucarianThemeProjectNavigation.ProjectPaletteRoute));
            var presets = visualForm.Section("Simultria presets", true);
            presets.Action("theming-preset-ds", "Use Design & Sales · purple / pink", () => UsePreset(true), () => store.CanWrite);
            presets.Action("theming-preset-rp", "Use Realisation & Progress · green", () => UsePreset(false), () => store.CanWrite);
            presets.Note(() => "Creates editable project palettes. Existing copies keep your changes.");
            content.Add(visual.Root);
            audio = new DeucarianEditorFeatureSection("theming-audio", "Audio",
                "Sounds for interactions and feedback.", DeucarianEditorIconIds.Audio,
                value => Write(settings => settings.SetFeatures(settings.UseVisualStyling, value)));
            audioForm = new DeucarianEditorWorkspaceForm(audio.Details);
            audioForm.Asset("theming-audio-palette", "Palette set", typeof(DeucarianAudioPaletteSet),
                () => Settings?.DefaultAudioPaletteSet,
                value => Write(settings => settings.ConfigureAudio(value as DeucarianAudioPaletteSet, settings.DefaultAudioExperience)));
            audioForm.Choice("theming-audio-experience", "Experience", Enum.GetNames(typeof(DeucarianAudioExperience)),
                () => (int)(Settings?.DefaultAudioExperience ?? DeucarianAudioExperience.Default),
                value => Write(settings => settings.ConfigureAudio(settings.DefaultAudioPaletteSet, (DeucarianAudioExperience)value)));
            var connectionRegion = DeucarianEditorWorkspaceControls.Region(null, "dw-feature-connections");
            connectionRegion.Add(DeucarianEditorWorkspaceControls.Label("Connected in this app", "dw-feature-connections-title"));
            connectionList = DeucarianEditorWorkspaceControls.Region("theming-connections", "dw-feature-connection-list");
            connectionRegion.Add(connectionList);
            audio.Details.Add(connectionRegion);
            var openAudio = DeucarianEditorWorkspaceControls.Button("Open Audio Palette Lab  →", OpenAudio, true);
            openAudio.name = "theming-open-audio";
            audio.Actions.Add(openAudio);
            content.Add(audio.Root);
            message = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-feature-status");
            content.Add(message);
            Refresh();
        }

        internal static IDeucarianEditorPage Create() => new DeucarianThemingProjectPage(
            new DeucarianThemingProjectSettingsStore(), new DeucarianThemingConnectionObserver());
        public VisualElement Root { get; }
        private DeucarianThemeRuntimeSettings Settings => settingsSnapshot;

        public void Activate(string route)
        {
            if (disposed || active) return;
            active = true;
            connections.Changed += RefreshConnections;
            connections.Start();
            EditorApplication.projectChanged += Refresh;
            EditorApplication.playModeStateChanged += OnPlayMode;
            Undo.undoRedoPerformed += Refresh;
            DeucarianThemeAssetChangeBus.AssetChanged += OnAssetChanged;
            Refresh();
        }
        public void Deactivate()
        {
            if (!active) return;
            active = false;
            connections.Changed -= RefreshConnections;
            connections.Dispose();
            EditorApplication.projectChanged -= Refresh;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            Undo.undoRedoPerformed -= Refresh;
            DeucarianThemeAssetChangeBus.AssetChanged -= OnAssetChanged;
        }
        public void Update(Rect bounds) { }
        private void OnPlayMode(PlayModeStateChange state) => Refresh();
        private void OnAssetChanged(UnityEngine.Object asset) { if (asset is DeucarianThemeRuntimeSettings) Refresh(); }

        private void Write(Action<DeucarianThemeRuntimeSettings> change)
        {
            if (disposed) return;
            try { store.Write(change); saveProblem = null; }
            catch (Exception exception)
            {
                saveProblem = "Could not save project settings. Check the Console, then try again.";
                ThemingLog.Editor.Exception(exception, "Could not save project Theming settings.");
            }
            Refresh();
        }
        private void OpenAudio()
        {
            var palette = Settings?.DefaultAudioPaletteSet;
            string path = palette != null ? AssetDatabase.GetAssetPath(palette) : null;
            string route = string.IsNullOrEmpty(path) ? null : "palette:" + AssetDatabase.AssetPathToGUID(path);
            DeucarianEditorNavigation.Open(Root, DeucarianEditorWorkspaceNavigation.AudioToolId, route);
        }

        private void UsePreset(bool designAndSales)
        {
            var assets = designAndSales ? DeucarianSimultriaThemeAssets.CreateDesignAndSales()
                : DeucarianSimultriaThemeAssets.CreateRealisationAndProgress();
            Write(settings => settings.Configure(assets.ThemeFamily, DeucarianThemeMode.Dark));
            if (Settings?.DefaultThemeFamily == assets.ThemeFamily)
                Selection.activeObject = assets.ThemeFamily;
        }
        private void Refresh()
        {
            if (disposed) return;
            settingsSnapshot = store.Read();
            var settings = Settings;
            bool useVisual = settings == null || settings.UseVisualStyling;
            bool useAudio = settings == null || settings.UseAudio;
            visual.Description.text = useVisual ? "Apply Deucarian colors, fonts and shapes."
                : "Keep the app's existing colors, fonts and shapes.";
            visual.SetState(useVisual, !useVisual ? "Deucarian visual palettes are not applied." :
                settings?.DefaultTheme == null ? "No project default. Existing component themes remain in charge." : null);
            audio.SetState(useAudio, !useAudio ? "Deucarian audio feedback is not played." : null);
            visualForm.Refresh();
            audioForm.Refresh();
            visual.Switch.SetEnabled(store.CanWrite);
            audio.Switch.SetEnabled(store.CanWrite);
            visual.Details.SetEnabled(store.CanWrite);
            audio.Details.SetEnabled(store.CanWrite);
            message.text = store.Problem ?? saveProblem ?? (EditorApplication.isPlayingOrWillChangePlaymode
                ? "Exit Play Mode to change project defaults." : string.Empty);
            DeucarianEditorWorkspaceControls.Show(message, message.text.Length > 0);
            RefreshConnections();
        }
        private void RefreshConnections()
        {
            if (disposed) return;
            connectionList.Clear();
            connectionList.Add(DeucarianEditorFeatureSection.Connection("theming-connection-buttons", "Buttons", connections.Buttons,
                connections.Buttons ? "A loaded button adapter is connected, or a button cue was played." : "No button connection detected in loaded scenes. Run the app to observe dynamic integrations."));
            connectionList.Add(DeucarianEditorFeatureSection.Connection("theming-connection-keyboard", "Keyboard", connections.Keyboard,
                connections.Keyboard ? "A keyboard cue was played by a loaded themed audio player." : "No keyboard cue observed. Run the app and press a key to verify this integration."));
            connectionList.Add(DeucarianEditorFeatureSection.Connection("theming-connection-warnings", "Warnings", connections.Warnings,
                connections.Warnings ? "A warning or error cue was played by a loaded themed audio player." : "No warning cue observed. Trigger a warning to verify audio feedback."));
        }
        public void Dispose()
        {
            if (disposed) return;
            Deactivate();
            disposed = true;
            workspace.Dispose();
        }
    }
}
