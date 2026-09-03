using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Maps designer-authored audio role assets to concrete UI feedback cues.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio Palette", menuName = "Deucarian/Theming/Audio Palette")]
    public sealed class DeucarianAudioPalette : ScriptableObject
    {
        [SerializeField] private string paletteId = "deucarian.audio-palette.default";
        [SerializeField] private string displayName = "Default Audio";
        [SerializeField] private DeucarianAudioRoleLibrary roleLibrary;
        [SerializeField] private List<DeucarianAudioEntry> entries = new List<DeucarianAudioEntry>();

        [NonSerialized] private Dictionary<DeucarianAudioRole, DeucarianAudioCue> cueByRole;
        [NonSerialized] private Dictionary<string, DeucarianAudioCue> cueById;

        /// <summary>Safe empty fallback used when an audio cue is null or cannot be resolved.</summary>
        public static DeucarianAudioCue MissingCue => DeucarianAudioCue.Empty;

        /// <summary>Stable audio palette identifier.</summary>
        public string PaletteId => paletteId;

        /// <summary>Human-readable audio palette name.</summary>
        public string DisplayName => displayName;

        /// <summary>Optional library used for role ID fallback lookups and editor helpers.</summary>
        public DeucarianAudioRoleLibrary RoleLibrary => roleLibrary;

        /// <summary>Ordered palette entries. Duplicate entries are allowed but validated.</summary>
        public IReadOnlyList<DeucarianAudioEntry> Entries => entries;

        /// <summary>Configures palette metadata and the optional role library.</summary>
        public void Configure(string id, string name, DeucarianAudioRoleLibrary library)
        {
            paletteId = DeucarianAudioRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            roleLibrary = library;
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Sets the optional role library used by this palette.</summary>
        public void SetRoleLibrary(DeucarianAudioRoleLibrary library)
        {
            if (roleLibrary == library)
            {
                return;
            }

            roleLibrary = library;
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Returns the palette cue, role default cue, or a safe empty cue when unresolved.</summary>
        public DeucarianAudioCue GetCue(DeucarianAudioRole role)
        {
            return TryGetCue(role, out DeucarianAudioCue cue) ? cue : MissingCue;
        }

        /// <summary>
        /// Resolves a cue by role. Returns true for explicit palette entries and role default fallbacks.
        /// </summary>
        public bool TryGetCue(DeucarianAudioRole role, out DeucarianAudioCue cue)
        {
            if (role == null)
            {
                cue = MissingCue;
                return false;
            }

            if (TryGetExplicitCue(role, out cue))
            {
                return true;
            }

            cue = SanitizeCue(role.DefaultCue);
            return true;
        }

        /// <summary>Returns the palette cue by role ID, role default fallback, or an empty cue when unresolved.</summary>
        public DeucarianAudioCue GetCueById(string roleId)
        {
            return TryGetCueById(roleId, out DeucarianAudioCue cue) ? cue : MissingCue;
        }

        /// <summary>
        /// Resolves a cue by role ID. Explicit palette entries win; the role library supplies default fallbacks.
        /// </summary>
        public bool TryGetCueById(string roleId, out DeucarianAudioCue cue)
        {
            string normalizedId = DeucarianAudioRole.NormalizeId(roleId);
            if (string.IsNullOrEmpty(normalizedId))
            {
                cue = MissingCue;
                return false;
            }

            if (TryGetExplicitCueById(normalizedId, out cue))
            {
                return true;
            }

            if (roleLibrary != null && roleLibrary.TryGetRoleById(normalizedId, out DeucarianAudioRole role))
            {
                cue = SanitizeCue(role.DefaultCue);
                return true;
            }

            cue = MissingCue;
            return false;
        }

        /// <summary>Returns only a cue explicitly authored in this palette.</summary>
        public bool TryGetExplicitCue(DeucarianAudioRole role, out DeucarianAudioCue cue)
        {
            EnsureCache();
            if (role != null && cueByRole.TryGetValue(role, out cue))
            {
                cue = SanitizeCue(cue);
                return true;
            }

            if (role != null && DeucarianAudioRole.IsValidId(role.Id))
            {
                return TryGetExplicitCueById(role.Id, out cue);
            }

            cue = MissingCue;
            return false;
        }

        /// <summary>Returns only a cue explicitly authored in this palette by role ID.</summary>
        public bool TryGetExplicitCueById(string roleId, out DeucarianAudioCue cue)
        {
            EnsureCache();
            string normalizedId = DeucarianAudioRole.NormalizeId(roleId);
            if (!string.IsNullOrEmpty(normalizedId) && cueById.TryGetValue(normalizedId, out cue))
            {
                cue = SanitizeCue(cue);
                return true;
            }

            cue = MissingCue;
            return false;
        }

        /// <summary>Adds a palette entry without removing duplicates.</summary>
        public void AddEntry(DeucarianAudioRole role, DeucarianAudioCue cue, string note = "")
        {
            EnsureEntryList();
            entries.Add(new DeucarianAudioEntry(role, cue, note));
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Adds or updates the first entry matching the role reference or role ID.</summary>
        public void SetCue(DeucarianAudioRole role, DeucarianAudioCue cue, string note = "")
        {
            EnsureEntryList();

            if (role == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianAudioEntry entry = entries[i];
                if (EntryMatchesRole(entry, role))
                {
                    entry.Configure(role, cue, note);
                    RebuildCache();
                    NotifyChanged();
                    return;
                }
            }

            entries.Add(new DeucarianAudioEntry(role, cue, note));
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Removes all entries and rebuilds lookup caches.</summary>
        public void ClearEntries()
        {
            EnsureEntryList();
            entries.Clear();
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Removes null entries or entries with no role and returns how many were removed.</summary>
        public int RemoveNullEntries()
        {
            EnsureEntryList();

            int removed = entries.RemoveAll(entry => entry == null || entry.Role == null);
            if (removed > 0)
            {
                RebuildCache();
                NotifyChanged();
            }

            return removed;
        }

        /// <summary>Resets one palette entry to its role's default cue.</summary>
        public bool ResetEntryToRoleDefault(int index)
        {
            EnsureEntryList();

            if (index < 0 || index >= entries.Count)
            {
                return false;
            }

            DeucarianAudioEntry entry = entries[index];
            if (entry == null || entry.Role == null)
            {
                return false;
            }

            entry.Configure(entry.Role, entry.Role.DefaultCue, entry.Note);
            RebuildCache();
            NotifyChanged();
            return true;
        }

        /// <summary>Adds missing entries for roles from the assigned library, using each role's default cue.</summary>
        public int AddMissingRolesFromLibrary()
        {
            EnsureEntryList();

            if (roleLibrary == null)
            {
                return 0;
            }

            int added = 0;
            IReadOnlyList<DeucarianAudioRole> libraryRoles = roleLibrary.Roles;
            for (int i = 0; i < libraryRoles.Count; i++)
            {
                DeucarianAudioRole role = libraryRoles[i];
                if (role == null || HasEntryForRole(role))
                {
                    continue;
                }

                entries.Add(new DeucarianAudioEntry(role, role.DefaultCue));
                added++;
            }

            if (added > 0)
            {
                RebuildCache();
                NotifyChanged();
            }

            return added;
        }

        /// <summary>Sorts entries by role category, display name, and ID.</summary>
        public void SortEntriesByCategoryAndName()
        {
            EnsureEntryList();
            entries.Sort(CompareEntries);
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Returns duplicate role IDs in deterministic entry order.</summary>
        public List<string> GetDuplicateEntryRoleIds()
        {
            EnsureEntryList();

            List<string> duplicates = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianAudioEntry entry = entries[i];
                DeucarianAudioRole role = entry != null ? entry.Role : null;
                if (role == null || !DeucarianAudioRole.IsValidId(role.Id))
                {
                    continue;
                }

                if (!seen.Add(role.Id) && reported.Add(role.Id))
                {
                    duplicates.Add(role.Id);
                }
            }

            return duplicates;
        }

        /// <summary>Returns validation warnings for null entries, null roles, and duplicate role IDs.</summary>
        public List<string> GetValidationWarnings()
        {
            EnsureEntryList();

            List<string> warnings = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianAudioEntry entry = entries[i];
                if (entry == null)
                {
                    warnings.Add($"Entry {i} is null.");
                    continue;
                }

                if (entry.Role == null)
                {
                    warnings.Add($"Entry {i} has no role.");
                    continue;
                }

                string roleWarning = entry.Role.GetValidationWarning();
                if (!string.IsNullOrEmpty(roleWarning))
                {
                    warnings.Add($"{entry.Role.name}: {roleWarning}");
                }

                string cueWarning = entry.Cue.GetValidationWarning();
                if (!string.IsNullOrEmpty(cueWarning))
                {
                    warnings.Add($"{entry.Role.name}: {cueWarning}");
                }
            }

            List<string> duplicateIds = GetDuplicateEntryRoleIds();
            for (int i = 0; i < duplicateIds.Count; i++)
            {
                warnings.Add($"Duplicate palette entry for role ID: {duplicateIds[i]}");
            }

            return warnings;
        }

        /// <summary>Rebuilds lookup caches. The first duplicate palette entry wins.</summary>
        public void RebuildCache()
        {
            EnsureEntryList();
            cueByRole = new Dictionary<DeucarianAudioRole, DeucarianAudioCue>();
            cueById = new Dictionary<string, DeucarianAudioCue>(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianAudioEntry entry = entries[i];
                DeucarianAudioRole role = entry != null ? entry.Role : null;
                if (role == null)
                {
                    continue;
                }

                if (!cueByRole.ContainsKey(role))
                {
                    cueByRole.Add(role, SanitizeCue(entry.Cue));
                }

                if (DeucarianAudioRole.IsValidId(role.Id) && !cueById.ContainsKey(role.Id))
                {
                    cueById.Add(role.Id, SanitizeCue(entry.Cue));
                }
            }
        }

        private bool HasEntryForRole(DeucarianAudioRole role)
        {
            EnsureEntryList();

            for (int i = 0; i < entries.Count; i++)
            {
                if (EntryMatchesRole(entries[i], role))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EntryMatchesRole(DeucarianAudioEntry entry, DeucarianAudioRole role)
        {
            if (entry == null || role == null || entry.Role == null)
            {
                return false;
            }

            if (entry.Role == role)
            {
                return true;
            }

            return DeucarianAudioRole.IsValidId(entry.Role.Id)
                && DeucarianAudioRole.IsValidId(role.Id)
                && string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal);
        }

        private static int CompareEntries(DeucarianAudioEntry left, DeucarianAudioEntry right)
        {
            DeucarianAudioRole leftRole = left != null ? left.Role : null;
            DeucarianAudioRole rightRole = right != null ? right.Role : null;

            if (leftRole == rightRole)
            {
                return 0;
            }

            if (leftRole == null)
            {
                return 1;
            }

            if (rightRole == null)
            {
                return -1;
            }

            int categoryCompare = string.Compare(leftRole.Category, rightRole.Category, StringComparison.OrdinalIgnoreCase);
            if (categoryCompare != 0)
            {
                return categoryCompare;
            }

            int nameCompare = string.Compare(leftRole.DisplayName, rightRole.DisplayName, StringComparison.OrdinalIgnoreCase);
            if (nameCompare != 0)
            {
                return nameCompare;
            }

            return string.Compare(leftRole.Id, rightRole.Id, StringComparison.OrdinalIgnoreCase);
        }

        private static DeucarianAudioCue SanitizeCue(DeucarianAudioCue cue)
        {
            return cue ?? MissingCue;
        }

        private void EnsureCache()
        {
            if (cueByRole == null || cueById == null)
            {
                RebuildCache();
            }
        }

        private void EnsureEntryList()
        {
            if (entries == null)
            {
                entries = new List<DeucarianAudioEntry>();
            }
        }

        private void OnEnable()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            paletteId = DeucarianAudioRole.NormalizeId(paletteId);
            displayName = displayName ?? string.Empty;
            RebuildCache();
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
