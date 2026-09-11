using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>A declared AudioRole identity. Reuse a named definition or select it in the Inspector.</summary>
    [Serializable]
    public class AudioRoleKey : IEquatable<AudioRoleKey>
    {
        [SerializeField] private string definitionId;

        /// <summary>For central definition sets and generated declarations; ordinary callers reuse those keys.</summary>
        protected AudioRoleKey(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                throw new ArgumentException("A AudioRoleKey definition needs a non-empty stable ID without surrounding whitespace.", nameof(id));
            definitionId = id;
        }

        public string Id => !string.IsNullOrWhiteSpace(definitionId) ? definitionId :
            throw new InvalidOperationException("No AudioRoleKey is selected. Select an existing definition in the Inspector or assign a named key from a AudioRoleKeySet declaration.");
        public bool Equals(AudioRoleKey other) => other != null && string.Equals(definitionId, other.definitionId, StringComparison.Ordinal);
        public override bool Equals(object other) => other is AudioRoleKey key && Equals(key);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(definitionId ?? string.Empty);
        public override string ToString() => definitionId ?? string.Empty;
    }
}
