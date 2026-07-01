using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Maps designer-authored color role assets to concrete Unity colors.
    /// </summary>
    [CreateAssetMenu(fileName = "Color Palette", menuName = "Deucarian/Theming/Color Palette")]
    public sealed class DeucarianColorPalette : ScriptableObject
    {
        /// <summary>Obvious fallback used when a role is null or cannot be resolved.</summary>
        public static readonly Color MissingColor = Color.magenta;

        [SerializeField] private string paletteId = "deucarian.palette.default";
        [SerializeField] private string displayName = "Default";
        [SerializeField] private DeucarianColorRoleLibrary roleLibrary;
        [SerializeField] private List<DeucarianColorEntry> entries = new List<DeucarianColorEntry>();

        [NonSerialized] private Dictionary<DeucarianColorRole, Color> colorByRole;
        [NonSerialized] private Dictionary<string, Color> colorById;

        /// <summary>Stable palette identifier.</summary>
        public string PaletteId => paletteId;

        /// <summary>Human-readable palette name.</summary>
        public string DisplayName => displayName;

        /// <summary>Optional library used for role ID fallback lookups and editor helpers.</summary>
        public DeucarianColorRoleLibrary RoleLibrary => roleLibrary;

        /// <summary>Ordered palette entries. Duplicate entries are allowed but validated.</summary>
        public IReadOnlyList<DeucarianColorEntry> Entries => entries;

        /// <summary>Configures palette metadata and the optional role library.</summary>
        public void Configure(string id, string name, DeucarianColorRoleLibrary library)
        {
            paletteId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            roleLibrary = library;
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Sets the optional role library used by this palette.</summary>
        public void SetRoleLibrary(DeucarianColorRoleLibrary library)
        {
            if (roleLibrary == library)
            {
                return;
            }

            roleLibrary = library;
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Returns the palette color, role default color, or magenta for a null role.</summary>
        public Color GetColor(DeucarianColorRole role)
        {
            return TryGetColor(role, out Color color) ? color : MissingColor;
        }

        /// <summary>
        /// Resolves a color by role. Returns true for explicit palette entries and role default fallbacks.
        /// </summary>
        public bool TryGetColor(DeucarianColorRole role, out Color color)
        {
            EnsureCache();

            if (role == null)
            {
                color = MissingColor;
                return false;
            }

            if (colorByRole.TryGetValue(role, out color))
            {
                return true;
            }

            color = role.DefaultColor;
            return true;
        }

        /// <summary>Returns the palette color by role ID, role default fallback, or magenta when unresolved.</summary>
        public Color GetColorById(string roleId)
        {
            return TryGetColorById(roleId, out Color color) ? color : MissingColor;
        }

        /// <summary>
        /// Resolves a color by role ID. Explicit palette entries win; the role library supplies default fallbacks.
        /// </summary>
        public bool TryGetColorById(string roleId, out Color color)
        {
            EnsureCache();

            string normalizedId = DeucarianColorRole.NormalizeId(roleId);
            if (string.IsNullOrEmpty(normalizedId))
            {
                color = MissingColor;
                return false;
            }

            if (colorById.TryGetValue(normalizedId, out color))
            {
                return true;
            }

            if (roleLibrary != null && roleLibrary.TryGetRoleById(normalizedId, out DeucarianColorRole role))
            {
                color = role.DefaultColor;
                return true;
            }

            color = MissingColor;
            return false;
        }

        /// <summary>Adds a palette entry without removing duplicates.</summary>
        public void AddEntry(DeucarianColorRole role, Color color, string note = "")
        {
            EnsureEntryList();
            entries.Add(new DeucarianColorEntry(role, color, note));
            RebuildCache();
            NotifyChanged();
        }

        /// <summary>Adds or updates the first entry matching the role reference or role ID.</summary>
        public void SetColor(DeucarianColorRole role, Color color, string note = "")
        {
            EnsureEntryList();

            if (role == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (EntryMatchesRole(entry, role))
                {
                    entry.Configure(role, color, note);
                    RebuildCache();
                    NotifyChanged();
                    return;
                }
            }

            entries.Add(new DeucarianColorEntry(role, color, note));
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

        /// <summary>Resets one palette entry to its role's default color.</summary>
        public bool ResetEntryToRoleDefault(int index)
        {
            EnsureEntryList();

            if (index < 0 || index >= entries.Count)
            {
                return false;
            }

            DeucarianColorEntry entry = entries[index];
            if (entry == null || entry.Role == null)
            {
                return false;
            }

            entry.Configure(entry.Role, entry.Role.DefaultColor, entry.Note);
            RebuildCache();
            NotifyChanged();
            return true;
        }

        /// <summary>Adds missing entries for roles from the assigned library, using each role's default color.</summary>
        public int AddMissingRolesFromLibrary()
        {
            EnsureEntryList();

            if (roleLibrary == null)
            {
                return 0;
            }

            int added = 0;
            IReadOnlyList<DeucarianColorRole> libraryRoles = roleLibrary.Roles;
            for (int i = 0; i < libraryRoles.Count; i++)
            {
                DeucarianColorRole role = libraryRoles[i];
                if (role == null || HasEntryForRole(role))
                {
                    continue;
                }

                entries.Add(new DeucarianColorEntry(role, role.DefaultColor));
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
                DeucarianColorEntry entry = entries[i];
                DeucarianColorRole role = entry != null ? entry.Role : null;
                if (role == null || !DeucarianColorRole.IsValidId(role.Id))
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
                DeucarianColorEntry entry = entries[i];
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
            colorByRole = new Dictionary<DeucarianColorRole, Color>();
            colorById = new Dictionary<string, Color>(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                DeucarianColorRole role = entry != null ? entry.Role : null;
                if (role == null)
                {
                    continue;
                }

                if (!colorByRole.ContainsKey(role))
                {
                    colorByRole.Add(role, entry.Color);
                }

                if (DeucarianColorRole.IsValidId(role.Id) && !colorById.ContainsKey(role.Id))
                {
                    colorById.Add(role.Id, entry.Color);
                }
            }
        }

        private bool HasEntryForRole(DeucarianColorRole role)
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

        private static bool EntryMatchesRole(DeucarianColorEntry entry, DeucarianColorRole role)
        {
            if (entry == null || role == null || entry.Role == null)
            {
                return false;
            }

            if (entry.Role == role)
            {
                return true;
            }

            return DeucarianColorRole.IsValidId(entry.Role.Id)
                && DeucarianColorRole.IsValidId(role.Id)
                && string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal);
        }

        private static int CompareEntries(DeucarianColorEntry left, DeucarianColorEntry right)
        {
            DeucarianColorRole leftRole = left != null ? left.Role : null;
            DeucarianColorRole rightRole = right != null ? right.Role : null;

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

        private void EnsureCache()
        {
            if (colorByRole == null || colorById == null)
            {
                RebuildCache();
            }
        }

        private void EnsureEntryList()
        {
            if (entries == null)
            {
                entries = new List<DeucarianColorEntry>();
            }
        }

        private void OnEnable()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            paletteId = DeucarianColorRole.NormalizeId(paletteId);
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
