using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Access to the redistributable semantic audio defaults bundled with Theming.</summary>
    public static class DeucarianAudioDefaults
    {
        public const string ResourcePath =
            "Deucarian/Theming/Audio/Defaults/DefaultAudioPaletteSet";

        public static DeucarianAudioPaletteSet LoadPaletteSet()
        {
            return Resources.Load<DeucarianAudioPaletteSet>(ResourcePath);
        }
    }
}
