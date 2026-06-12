using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Maps a color role asset to a concrete color inside a palette.
    /// </summary>
    [Serializable]
    public sealed class DeucarianColorEntry
    {
        [SerializeField] private DeucarianColorRole role;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private string note = string.Empty;

        /// <summary>Color role this entry maps.</summary>
        public DeucarianColorRole Role => role;

        /// <summary>Palette color for the role.</summary>
        public Color Color => color;

        /// <summary>Optional designer note for this mapping.</summary>
        public string Note => note;

        public DeucarianColorEntry()
        {
        }

        public DeucarianColorEntry(DeucarianColorRole role, Color color, string note = "")
        {
            Configure(role, color, note);
        }

        /// <summary>Updates this entry.</summary>
        public void Configure(DeucarianColorRole entryRole, Color entryColor, string entryNote = "")
        {
            role = entryRole;
            color = entryColor;
            note = entryNote ?? string.Empty;
        }
    }
}
