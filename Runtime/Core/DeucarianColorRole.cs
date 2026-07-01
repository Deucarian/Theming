using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Defines a designer-authored color role with a stable ID and default fallback color.
    /// </summary>
    [CreateAssetMenu(fileName = "Color Role", menuName = "Deucarian/Theming/Color Role")]
    public sealed class DeucarianColorRole : ScriptableObject
    {
        [SerializeField] private string id = DeucarianBuiltinColorRoleIds.Primary;
        [SerializeField] private string displayName = "Primary";
        [SerializeField] private string category = DeucarianColorRoleCategories.Semantic;
        [SerializeField] private string description = string.Empty;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private bool isCoreRole;

        /// <summary>Stable role identifier, such as <c>deucarian.primary</c>.</summary>
        public string Id => id;

        /// <summary>Human-readable role name shown to designers.</summary>
        public string DisplayName => displayName;

        /// <summary>Optional grouping label for editor organization.</summary>
        public string Category => category;

        /// <summary>Optional description of where the role should be used.</summary>
        public string Description => description;

        /// <summary>Fallback color used when a palette does not override this role.</summary>
        public Color DefaultColor => defaultColor;

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
            Color roleDefaultColor,
            bool coreRole)
        {
            id = NormalizeId(roleId);
            displayName = roleDisplayName ?? string.Empty;
            category = roleCategory ?? string.Empty;
            description = roleDescription ?? string.Empty;
            defaultColor = roleDefaultColor;
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

            if (!string.Equals(id, NormalizeId(id), StringComparison.Ordinal))
            {
                return "Role ID has leading or trailing whitespace.";
            }

            if (ContainsWhitespace(id))
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
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>Returns true when a role ID is non-empty and contains no whitespace.</summary>
        public static bool IsValidId(string value)
        {
            string normalized = NormalizeId(value);
            return !string.IsNullOrEmpty(normalized) && !ContainsWhitespace(normalized);
        }

        /// <summary>Returns true when a role ID avoids uppercase letters.</summary>
        public static bool IsLowercaseFriendlyId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsLetter(character) && char.ToLowerInvariant(character) != character)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsWhitespace(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            id = NormalizeId(id);
            displayName = displayName ?? string.Empty;
            category = category ?? string.Empty;
            description = description ?? string.Empty;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }
    }
}
