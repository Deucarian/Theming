using System;
using System.Collections.Generic;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianAudioRoleBrowserModel
    {
        internal static IReadOnlyList<DeucarianAudioRole> Collect(DeucarianAudioPaletteSet paletteSet, DeucarianAudioExperience experience)
        {
            List<DeucarianAudioRole> roles = new List<DeucarianAudioRole>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            AddPaletteRoles(paletteSet != null ? paletteSet.DefaultPalette : null, roles, ids);
            if (paletteSet != null)
            {
                AddPaletteRoles(paletteSet.GetPalette(experience), roles, ids);
            }

            roles.Sort((left, right) => string.Compare(
                left.DisplayName,
                right.DisplayName,
                StringComparison.OrdinalIgnoreCase));
            return roles;
        }

        private static void AddPaletteRoles(
            DeucarianAudioPalette palette,
            ICollection<DeucarianAudioRole> roles,
            ISet<string> ids)
        {
            if (palette == null || palette.RoleLibrary == null)
            {
                return;
            }

            IReadOnlyList<DeucarianAudioRole> source = palette.RoleLibrary.Roles;
            for (int i = 0; i < source.Count; i++)
            {
                DeucarianAudioRole role = source[i];
                if (role != null && ids.Add(role.Id))
                {
                    roles.Add(role);
                }
            }
        }

        internal static bool MatchesSearch(DeucarianAudioRole role, string search, int categoryFilter)
        {
            if (role == null)
            {
                return false;
            }

            string value = search == null ? string.Empty : search.Trim();
            bool categoryMatches = categoryFilter == 0
                || categoryFilter == 1 && role.Category == DeucarianAudioRoleCategories.UI
                || categoryFilter == 2 && role.Category == DeucarianAudioRoleCategories.Input
                || categoryFilter == 3 && role.Category == DeucarianAudioRoleCategories.Feedback;
            return categoryMatches && (value.Length == 0
                || role.DisplayName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
                || role.Category.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
                || role.Id.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        internal static DeucarianAudioRole Find(DeucarianAudioPaletteSet paletteSet, DeucarianAudioExperience experience, string id)
        {
            IReadOnlyList<DeucarianAudioRole> roles = Collect(paletteSet, experience);
            for (int i = 0; i < roles.Count; i++)
            {
                if (roles[i] != null && roles[i].Id == id)
                {
                    return roles[i];
                }
            }

            return null;
        }

        internal static string Describe(DeucarianAudioPaletteSet paletteSet, DeucarianAudioExperience experience, DeucarianAudioRole role)
        {
            if (paletteSet == null || role == null ||
                !paletteSet.TryResolve(role, experience, out DeucarianAudioResolution resolution))
            {
                return role != null ? $"{role.DisplayName}  ·  missing" : "Missing role";
            }

            string state = resolution.Cue.IntentionalSilence
                ? "muted"
                : resolution.IsAudible ? $"{resolution.Cue.UsableVariantCount} clip(s)" : "missing";
            return $"{role.DisplayName}  ·  {resolution.Source}  ·  {state}";
        }
    }
}
