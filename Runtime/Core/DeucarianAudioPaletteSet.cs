using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    [Serializable]
    public sealed class DeucarianAudioPaletteProfile
    {
        [SerializeField] private DeucarianAudioExperience experience = DeucarianAudioExperience.XR;
        [SerializeField] private DeucarianAudioPalette palette;

        public DeucarianAudioExperience Experience => experience;
        public DeucarianAudioPalette Palette => palette;

        public void Configure(DeucarianAudioExperience profileExperience, DeucarianAudioPalette profilePalette)
        {
            experience = profileExperience;
            palette = profilePalette;
        }
    }

    /// <summary>Default palette plus explicit product-experience overrides.</summary>
    [CreateAssetMenu(fileName = "Audio Palette Set", menuName = "Deucarian/Theming/Audio Palette Set")]
    public sealed class DeucarianAudioPaletteSet : ScriptableObject
    {
        [SerializeField] private DeucarianAudioPalette defaultPalette;
        [SerializeField] private List<DeucarianAudioPaletteProfile> profiles =
            new List<DeucarianAudioPaletteProfile>();

        public DeucarianAudioPalette DefaultPalette => defaultPalette;
        public IReadOnlyList<DeucarianAudioPaletteProfile> Profiles => profiles;

        public void Configure(
            DeucarianAudioPalette fallbackPalette,
            IEnumerable<DeucarianAudioPaletteProfile> experienceProfiles)
        {
            defaultPalette = fallbackPalette;
            profiles = experienceProfiles != null
                ? new List<DeucarianAudioPaletteProfile>(experienceProfiles)
                : new List<DeucarianAudioPaletteProfile>();
            NotifyChanged();
        }

        public DeucarianAudioPalette GetPalette(DeucarianAudioExperience experience)
        {
            if (experience == DeucarianAudioExperience.Default)
            {
                return defaultPalette;
            }

            EnsureProfiles();
            for (int i = 0; i < profiles.Count; i++)
            {
                DeucarianAudioPaletteProfile profile = profiles[i];
                if (profile != null && profile.Experience == experience)
                {
                    return profile.Palette;
                }
            }

            return null;
        }

        public bool TryResolve(
            DeucarianAudioRole role,
            DeucarianAudioExperience experience,
            out DeucarianAudioResolution resolution)
        {
            if (role == null)
            {
                resolution = DeucarianAudioResolution.Missing;
                return false;
            }

            DeucarianAudioPalette experiencePalette = GetPalette(experience);
            if (experiencePalette != null &&
                experiencePalette.TryGetExplicitCue(role, out DeucarianAudioCue experienceCue))
            {
                resolution = new DeucarianAudioResolution(
                    experienceCue,
                    DeucarianAudioResolutionSource.ExperiencePalette,
                    experiencePalette);
                return true;
            }

            if (defaultPalette != null &&
                defaultPalette.TryGetExplicitCue(role, out DeucarianAudioCue fallbackCue))
            {
                resolution = new DeucarianAudioResolution(
                    fallbackCue,
                    DeucarianAudioResolutionSource.DefaultPalette,
                    defaultPalette);
                return true;
            }

            resolution = new DeucarianAudioResolution(
                role.DefaultCue,
                DeucarianAudioResolutionSource.RoleDefault,
                null);
            return true;
        }

        public bool TryResolveById(
            string roleId,
            DeucarianAudioExperience experience,
            out DeucarianAudioResolution resolution)
        {
            DeucarianAudioRole role = ResolveRole(roleId, experience);
            return TryResolve(role, experience, out resolution);
        }

        public List<string> GetValidationWarnings()
        {
            EnsureProfiles();
            List<string> warnings = new List<string>();
            if (defaultPalette == null)
            {
                warnings.Add("A default audio palette is required.");
            }

            HashSet<DeucarianAudioExperience> seen = new HashSet<DeucarianAudioExperience>();
            for (int i = 0; i < profiles.Count; i++)
            {
                DeucarianAudioPaletteProfile profile = profiles[i];
                if (profile == null)
                {
                    warnings.Add($"Profile {i} is null.");
                    continue;
                }

                if (profile.Experience == DeucarianAudioExperience.Default)
                {
                    warnings.Add($"Profile {i} uses Default; assign the default palette field instead.");
                }
                else if (!seen.Add(profile.Experience))
                {
                    warnings.Add($"Duplicate experience profile: {profile.Experience}.");
                }
            }

            return warnings;
        }

        private DeucarianAudioRole ResolveRole(string roleId, DeucarianAudioExperience experience)
        {
            DeucarianAudioPalette experiencePalette = GetPalette(experience);
            if (experiencePalette != null && experiencePalette.RoleLibrary != null &&
                experiencePalette.RoleLibrary.TryGetRoleById(roleId, out DeucarianAudioRole role))
            {
                return role;
            }

            if (defaultPalette != null && defaultPalette.RoleLibrary != null &&
                defaultPalette.RoleLibrary.TryGetRoleById(roleId, out role))
            {
                return role;
            }

            return null;
        }

        private void EnsureProfiles()
        {
            if (profiles == null)
            {
                profiles = new List<DeucarianAudioPaletteProfile>();
            }
        }

        private void OnValidate()
        {
            EnsureProfiles();
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
