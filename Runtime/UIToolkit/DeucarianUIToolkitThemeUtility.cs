using System;
using System.Collections.Generic;
using Deucarian.Theming;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.UIToolkit
{
    /// <summary>
    /// Null-safe helpers for applying Deucarian themes to UI Toolkit visual trees.
    /// </summary>
    public static class DeucarianUIToolkitThemeUtility
    {
        private static bool warnedCustomVariables;

        /// <summary>Resolves a color from a theme and role, or returns magenta when either is missing.</summary>
        public static Color GetColorOrFallback(DeucarianTheme theme, DeucarianColorRole role)
        {
            if (theme == null || role == null)
            {
                return DeucarianColorPalette.MissingColor;
            }

            return theme.GetColor(role);
        }

        /// <summary>Resolves elements from a root according to binding priority: selector, name, class, then root.</summary>
        public static IEnumerable<VisualElement> ResolveElements(VisualElement root, DeucarianUIToolkitThemeBinding binding)
        {
            if (root == null)
            {
                yield break;
            }

            if (binding == null)
            {
                yield return root;
                yield break;
            }

            List<VisualElement> matches = ResolveBaseElements(root, binding);
            HashSet<VisualElement> yielded = new HashSet<VisualElement>();

            for (int i = 0; i < matches.Count; i++)
            {
                VisualElement element = matches[i];
                if (element == null || !yielded.Add(element))
                {
                    continue;
                }

                yield return element;

                if (!binding.IncludeChildren)
                {
                    continue;
                }

                List<VisualElement> children = element.Query<VisualElement>().ToList();
                for (int childIndex = 0; childIndex < children.Count; childIndex++)
                {
                    VisualElement child = children[childIndex];
                    if (child != null && yielded.Add(child))
                    {
                        yield return child;
                    }
                }
            }
        }

        /// <summary>Applies a color to a supported UI Toolkit style property.</summary>
        public static void ApplyColor(
            VisualElement element,
            DeucarianUIToolkitStyleProperty property,
            Color color,
            string customCssVariableName)
        {
            if (element == null)
            {
                return;
            }

            StyleColor styleColor = new StyleColor(color);
            switch (property)
            {
                case DeucarianUIToolkitStyleProperty.BackgroundColor:
                    element.style.backgroundColor = styleColor;
                    break;
                case DeucarianUIToolkitStyleProperty.TextColor:
                    element.style.color = styleColor;
                    break;
                case DeucarianUIToolkitStyleProperty.BorderColor:
                    element.style.borderTopColor = styleColor;
                    element.style.borderRightColor = styleColor;
                    element.style.borderBottomColor = styleColor;
                    element.style.borderLeftColor = styleColor;
                    break;
                case DeucarianUIToolkitStyleProperty.BorderTopColor:
                    element.style.borderTopColor = styleColor;
                    break;
                case DeucarianUIToolkitStyleProperty.BorderRightColor:
                    element.style.borderRightColor = styleColor;
                    break;
                case DeucarianUIToolkitStyleProperty.BorderBottomColor:
                    element.style.borderBottomColor = styleColor;
                    break;
                case DeucarianUIToolkitStyleProperty.BorderLeftColor:
                    element.style.borderLeftColor = styleColor;
                    break;
                case DeucarianUIToolkitStyleProperty.UnityBackgroundImageTintColor:
                    element.style.unityBackgroundImageTintColor = styleColor;
                    break;
                case DeucarianUIToolkitStyleProperty.CustomCssVariable:
                    WarnCustomVariablesUnsupported(customCssVariableName);
                    break;
                default:
                    break;
            }
        }

        /// <summary>Converts arbitrary text into a lowercase USS-variable-friendly name body.</summary>
        public static string ToSafeVariableName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "unnamed";
            }

            string normalized = input.Trim().ToLowerInvariant();
            char[] buffer = new char[normalized.Length];
            int length = 0;
            bool previousWasDash = false;

            for (int i = 0; i < normalized.Length; i++)
            {
                char character = normalized[i];
                bool keep = char.IsLetterOrDigit(character);
                bool dash = character == '-' || character == '_' || character == '.' || char.IsWhiteSpace(character);

                if (keep)
                {
                    buffer[length++] = character;
                    previousWasDash = false;
                }
                else if (dash && !previousWasDash && length > 0)
                {
                    buffer[length++] = '-';
                    previousWasDash = true;
                }
            }

            while (length > 0 && buffer[length - 1] == '-')
            {
                length--;
            }

            return length == 0 ? "unnamed" : new string(buffer, 0, length);
        }

        private static List<VisualElement> ResolveBaseElements(VisualElement root, DeucarianUIToolkitThemeBinding binding)
        {
            string selector = binding.UssSelector;
            if (!string.IsNullOrWhiteSpace(selector))
            {
                return ResolveSelector(root, selector.Trim());
            }

            if (!string.IsNullOrWhiteSpace(binding.ElementName))
            {
                return root.Query<VisualElement>(name: binding.ElementName.Trim()).ToList();
            }

            if (!string.IsNullOrWhiteSpace(binding.ElementClass))
            {
                return root.Query<VisualElement>(className: binding.ElementClass.Trim()).ToList();
            }

            return new List<VisualElement> { root };
        }

        private static List<VisualElement> ResolveSelector(VisualElement root, string selector)
        {
            if (string.IsNullOrWhiteSpace(selector))
            {
                return new List<VisualElement> { root };
            }

            if (selector[0] == '#')
            {
                return root.Query<VisualElement>(name: selector.Substring(1)).ToList();
            }

            if (selector[0] == '.')
            {
                return root.Query<VisualElement>(className: selector.Substring(1)).ToList();
            }

            int classIndex = selector.IndexOf('.');
            if (classIndex > 0 && classIndex < selector.Length - 1)
            {
                string typeName = selector.Substring(0, classIndex);
                string className = selector.Substring(classIndex + 1);
                return FilterByTypeName(root.Query<VisualElement>(className: className).ToList(), typeName);
            }

            List<VisualElement> all = root.Query<VisualElement>().ToList();
            List<VisualElement> matches = new List<VisualElement>();
            for (int i = 0; i < all.Count; i++)
            {
                VisualElement element = all[i];
                if (element == null)
                {
                    continue;
                }

                if (string.Equals(element.name, selector, StringComparison.Ordinal)
                    || element.ClassListContains(selector)
                    || string.Equals(element.GetType().Name, selector, StringComparison.Ordinal)
                    || string.Equals(element.GetType().FullName, selector, StringComparison.Ordinal))
                {
                    matches.Add(element);
                }
            }

            return matches;
        }

        private static List<VisualElement> FilterByTypeName(List<VisualElement> elements, string typeName)
        {
            List<VisualElement> matches = new List<VisualElement>();
            for (int i = 0; i < elements.Count; i++)
            {
                VisualElement element = elements[i];
                if (element == null)
                {
                    continue;
                }

                if (string.Equals(element.GetType().Name, typeName, StringComparison.Ordinal)
                    || string.Equals(element.GetType().FullName, typeName, StringComparison.Ordinal))
                {
                    matches.Add(element);
                }
            }

            return matches;
        }

        private static void WarnCustomVariablesUnsupported(string customCssVariableName)
        {
            if (warnedCustomVariables)
            {
                return;
            }

            warnedCustomVariables = true;
            string variableName = string.IsNullOrWhiteSpace(customCssVariableName) ? "the requested variable" : customCssVariableName;
            Debug.LogWarning(
                $"Runtime USS custom variable assignment is not exposed consistently by Unity UI Toolkit. '{variableName}' was not applied; use DeucarianUIToolkitThemeVariables to preview/generate USS variable values.");
        }
    }
}
