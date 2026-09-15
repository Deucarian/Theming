using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Read-only starting assets included with the package; sample import is not required.</summary>
    public static class DeucarianVisualDefaults
    {
        public const string ResourcePath = "Deucarian/Theming/Visual/Defaults/DefaultThemeFamily";

        /// <summary>Loads the bundled Deucarian light/dark family. Create project copies before editing.</summary>
        public static DeucarianThemeFamily LoadFamily() => Resources.Load<DeucarianThemeFamily>(ResourcePath);

        /// <summary>Loads a bundled visual theme with its default audio palette set.</summary>
        public static DeucarianTheme LoadTheme(DeucarianThemeMode mode = DeucarianThemeMode.Dark) =>
            LoadFamily()?.ResolveTheme(mode);
    }
}
