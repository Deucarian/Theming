using System;
using System.Collections.Generic;

namespace Deucarian.Theming
{
    /// <summary>
    /// Calculates palette-entry validation results without owning or mutating palette state.
    /// </summary>
    internal static class DeucarianColorPaletteValidationPolicy
    {
        public static List<string> GetDuplicateRoleIds(
            IReadOnlyList<DeucarianColorEntry> entries)
        {
            List<string> duplicates = new List<string>();
            if (entries == null)
            {
                return duplicates;
            }

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

        public static List<string> GetWarnings(
            IReadOnlyList<DeucarianColorEntry> entries)
        {
            List<string> warnings = new List<string>();
            if (entries == null)
            {
                return warnings;
            }

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

            List<string> duplicateIds = GetDuplicateRoleIds(entries);
            for (int i = 0; i < duplicateIds.Count; i++)
            {
                warnings.Add($"Duplicate palette entry for role ID: {duplicateIds[i]}");
            }

            return warnings;
        }
    }
}
