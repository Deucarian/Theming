using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Designer-maintained collection of available color role assets.
    /// </summary>
    [CreateAssetMenu(fileName = "Color Role Library", menuName = "Deucarian/Theming/Color Role Library")]
    public sealed class DeucarianColorRoleLibrary : ScriptableObject
    {
        [SerializeField] private List<DeucarianColorRole> roles = new List<DeucarianColorRole>();

        [NonSerialized] private Dictionary<string, DeucarianColorRole> roleById;

        /// <summary>Ordered role assets in this library.</summary>
        public IReadOnlyList<DeucarianColorRole> Roles => roles;

        /// <summary>Finds the first role with the requested stable ID.</summary>
        public bool TryGetRoleById(string id, out DeucarianColorRole role)
        {
            EnsureCache();

            string normalizedId = DeucarianColorRole.NormalizeId(id);
            if (string.IsNullOrEmpty(normalizedId))
            {
                role = null;
                return false;
            }

            return roleById.TryGetValue(normalizedId, out role);
        }

        /// <summary>Adds a role reference if it is not already present.</summary>
        public bool AddRole(DeucarianColorRole role)
        {
            EnsureRoleList();

            if (role == null || roles.Contains(role))
            {
                return false;
            }

            roles.Add(role);
            RebuildCache();
            return true;
        }

        /// <summary>Removes null role entries and returns how many were removed.</summary>
        public int RemoveNullRoles()
        {
            EnsureRoleList();

            int removed = roles.RemoveAll(role => role == null);
            if (removed > 0)
            {
                RebuildCache();
            }

            return removed;
        }

        /// <summary>Sorts roles by category, display name, then ID.</summary>
        public void SortRolesByCategoryAndName()
        {
            EnsureRoleList();
            roles.Sort(CompareRoles);
            RebuildCache();
        }

        /// <summary>Returns duplicate role IDs in deterministic library order.</summary>
        public List<string> GetDuplicateRoleIds()
        {
            EnsureRoleList();

            List<string> duplicates = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianColorRole role = roles[i];
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

        /// <summary>Returns validation warnings for null entries, invalid IDs, and duplicate IDs.</summary>
        public List<string> GetValidationWarnings()
        {
            EnsureRoleList();

            List<string> warnings = new List<string>();
            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianColorRole role = roles[i];
                if (role == null)
                {
                    warnings.Add($"Entry {i} is null.");
                    continue;
                }

                string roleWarning = role.GetValidationWarning();
                if (!string.IsNullOrEmpty(roleWarning))
                {
                    warnings.Add($"{role.name}: {roleWarning}");
                }
            }

            List<string> duplicateIds = GetDuplicateRoleIds();
            for (int i = 0; i < duplicateIds.Count; i++)
            {
                warnings.Add($"Duplicate role ID: {duplicateIds[i]}");
            }

            return warnings;
        }

        /// <summary>Rebuilds the lookup cache. The first role for a duplicate ID wins.</summary>
        public void RebuildCache()
        {
            EnsureRoleList();
            roleById = new Dictionary<string, DeucarianColorRole>(StringComparer.Ordinal);

            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianColorRole role = roles[i];
                if (role == null || !DeucarianColorRole.IsValidId(role.Id))
                {
                    continue;
                }

                if (!roleById.ContainsKey(role.Id))
                {
                    roleById.Add(role.Id, role);
                }
            }
        }

        private static int CompareRoles(DeucarianColorRole left, DeucarianColorRole right)
        {
            if (left == right)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int categoryCompare = string.Compare(left.Category, right.Category, StringComparison.OrdinalIgnoreCase);
            if (categoryCompare != 0)
            {
                return categoryCompare;
            }

            int nameCompare = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            if (nameCompare != 0)
            {
                return nameCompare;
            }

            return string.Compare(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureCache()
        {
            if (roleById == null)
            {
                RebuildCache();
            }
        }

        private void EnsureRoleList()
        {
            if (roles == null)
            {
                roles = new List<DeucarianColorRole>();
            }
        }

        private void OnEnable()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            RebuildCache();
        }
    }
}
