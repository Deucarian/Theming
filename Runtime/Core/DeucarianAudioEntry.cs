using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Maps an audio role asset to a concrete cue inside an audio palette.
    /// </summary>
    [Serializable]
    public sealed class DeucarianAudioEntry
    {
        [SerializeField] private DeucarianAudioRole role;
        [SerializeField] private DeucarianAudioCue cue = new DeucarianAudioCue();
        [SerializeField] private string note = string.Empty;

        /// <summary>Audio role this entry maps.</summary>
        public DeucarianAudioRole Role => role;

        /// <summary>Palette cue for the role. A missing cue is returned as an empty safe cue.</summary>
        public DeucarianAudioCue Cue => cue ?? DeucarianAudioCue.Empty;

        /// <summary>Optional designer note for this mapping.</summary>
        public string Note => note;

        public DeucarianAudioEntry()
        {
        }

        public DeucarianAudioEntry(DeucarianAudioRole role, DeucarianAudioCue cue, string note = "")
        {
            Configure(role, cue, note);
        }

        /// <summary>Updates this entry.</summary>
        public void Configure(DeucarianAudioRole entryRole, DeucarianAudioCue entryCue, string entryNote = "")
        {
            role = entryRole;
            cue = entryCue != null ? entryCue.Clone() : DeucarianAudioCue.Empty;
            note = entryNote ?? string.Empty;
        }
    }
}

