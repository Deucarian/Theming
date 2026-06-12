using System;
using Deucarian.Theming;
using UnityEngine;

namespace Deucarian.Theming.UIToolkit
{
    /// <summary>
    /// Explicit mapping from a color role to a USS custom variable name.
    /// </summary>
    [Serializable]
    public sealed class DeucarianUIToolkitThemeVariableMapping
    {
        [SerializeField] private DeucarianColorRole role;
        [SerializeField] private string variableName = string.Empty;

        /// <summary>Role assigned to the custom variable.</summary>
        public DeucarianColorRole Role => role;

        /// <summary>USS custom variable name, including or excluding the leading <c>--</c>.</summary>
        public string VariableName => variableName;

        /// <summary>Configures this mapping for tests and editor tooling.</summary>
        public void Configure(DeucarianColorRole mappedRole, string mappedVariableName)
        {
            role = mappedRole;
            variableName = mappedVariableName ?? string.Empty;
        }
    }
}
