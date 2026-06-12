using System;
using Deucarian.Theming;
using UnityEngine;

namespace Deucarian.Theming.UIToolkit
{
    /// <summary>
    /// Maps a UI Toolkit selector target to a color role and style property.
    /// </summary>
    [Serializable]
    public sealed class DeucarianUIToolkitThemeBinding
    {
        [SerializeField] private string elementName = string.Empty;
        [SerializeField] private string elementClass = string.Empty;
        [SerializeField] private string ussSelector = string.Empty;
        [SerializeField] private DeucarianColorRole colorRole;
        [SerializeField] private DeucarianUIToolkitStyleProperty styleProperty;
        [SerializeField] private string customCssVariableName = string.Empty;
        [SerializeField] private bool includeChildren;

        /// <summary>Element name used when no USS selector is configured.</summary>
        public string ElementName => elementName;

        /// <summary>Element class used when no USS selector or element name is configured.</summary>
        public string ElementClass => elementClass;

        /// <summary>Preferred selector. Simple selectors such as <c>.class</c>, <c>#name</c>, and type names are supported.</summary>
        public string UssSelector => ussSelector;

        /// <summary>Role whose color should be applied.</summary>
        public DeucarianColorRole ColorRole => colorRole;

        /// <summary>Style property that receives the resolved color.</summary>
        public DeucarianUIToolkitStyleProperty StyleProperty => styleProperty;

        /// <summary>USS custom variable name used when <see cref="StyleProperty"/> is CustomCssVariable.</summary>
        public string CustomCssVariableName => customCssVariableName;

        /// <summary>Whether matching elements should also apply to their descendants.</summary>
        public bool IncludeChildren => includeChildren;

        /// <summary>Configures this binding for tests and editor tooling.</summary>
        public void Configure(
            DeucarianColorRole role,
            DeucarianUIToolkitStyleProperty property,
            string selector = "",
            string name = "",
            string className = "",
            string variableName = "",
            bool applyToChildren = false)
        {
            colorRole = role;
            styleProperty = property;
            ussSelector = selector ?? string.Empty;
            elementName = name ?? string.Empty;
            elementClass = className ?? string.Empty;
            customCssVariableName = variableName ?? string.Empty;
            includeChildren = applyToChildren;
        }
    }
}
