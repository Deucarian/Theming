using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Defines a designer-authored audio role with a stable ID and default fallback cue.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio Role", menuName = "Deucarian/Theming/Audio Role")]
    public sealed class DeucarianAudioRole : ScriptableObject
    {
        [SerializeField] private string id = DeucarianBuiltinAudioRoleIds.Hover;
        [SerializeField] private string displayName = "Hover";
        [SerializeField] private string category = DeucarianAudioRoleCategories.UI;
        [SerializeField] private string description = string.Empty;
        [SerializeField] private DeucarianAudioCue defaultCue = new DeucarianAudioCue();
        [SerializeField] private bool isCoreRole;

        /// <summary>Stable role identifier, such as <c>deucarian.ui.audio.hover</c>.</summary>
        public string Id => id;

        /// <summary>Human-readable role name shown to designers.</summary>
        public string DisplayName => displayName;

        /// <summary>Optional grouping label for editor organization.</summary>
        public string Category => category;

        /// <summary>Optional description of where the role should be used.</summary>
        public string Description => description;

        /// <summary>Fallback cue used when an audio palette does not override this role.</summary>
        public DeucarianAudioCue DefaultCue => defaultCue ?? DeucarianAudioCue.Empty;

        /// <summary>Whether this role is part of the built-in Deucarian role set.</summary>
        public bool IsCoreRole => isCoreRole;

        /// <summary>Returns true when the ID is present, trimmed, whitespace-free, and lowercase-friendly.</summary>
        public bool HasValidId => IsValidId(id) && IsLowercaseFriendlyId(id);

        /// <summary>
        /// Configures the role. This is useful for editor tooling and tests that create role assets programmatically.
        /// </summary>
        public void Configure(
            string roleId,
            string roleDisplayName,
            string roleCategory,
            string roleDescription,
            DeucarianAudioCue roleDefaultCue,
            bool coreRole)
        {
            id = NormalizeId(roleId);
            displayName = roleDisplayName ?? string.Empty;
            category = roleCategory ?? string.Empty;
            description = roleDescription ?? string.Empty;
            defaultCue = roleDefaultCue != null ? roleDefaultCue.Clone() : DeucarianAudioCue.Empty;
            isCoreRole = coreRole;
            NotifyChanged();
        }

        /// <summary>Returns a validation warning for this role, or null when the role is valid.</summary>
        public string GetValidationWarning()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "Role ID is required.";
            }

            if (!string.Equals(id, NormalizeId(id), System.StringComparison.Ordinal))
            {
                return "Role ID has leading or trailing whitespace.";
            }

            if (!IsValidId(id))
            {
                return "Role ID should not contain whitespace.";
            }

            if (!IsLowercaseFriendlyId(id))
            {
                return "Role ID should use lowercase-friendly stable identifiers.";
            }

            return null;
        }

        /// <summary>Normalizes a role ID by trimming whitespace and converting null to an empty string.</summary>
        public static string NormalizeId(string value)
        {
            return DeucarianColorRole.NormalizeId(value);
        }

        /// <summary>Returns true when a role ID is non-empty and contains no whitespace.</summary>
        public static bool IsValidId(string value)
        {
            return DeucarianColorRole.IsValidId(value);
        }

        /// <summary>Returns true when a role ID avoids uppercase letters.</summary>
        public static bool IsLowercaseFriendlyId(string value)
        {
            return DeucarianColorRole.IsLowercaseFriendlyId(value);
        }

        private void OnValidate()
        {
            id = NormalizeId(id);
            displayName = displayName ?? string.Empty;
            category = category ?? string.Empty;
            description = description ?? string.Empty;
            defaultCue = defaultCue ?? DeucarianAudioCue.Empty;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}

