using Deucarian.Editor;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianAudioPaletteLabWindow
    {
        public static void PreviewDefinition(DeucarianAudioRole role)
        {
            OpenWindow();
            var window = DeucarianEditorWindowPages.GetStandalone<DeucarianAudioPaletteLabWindow>();
            window.SelectRole(role);
            window.Play(role.DefaultCue);
        }
    }
}
