using System.Collections.Generic;
using System.Text;
using Deucarian.Theming;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.UIToolkit
{
    /// <summary>
    /// Builds USS custom variable color values from a Deucarian theme and role library.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeucarianUIToolkitThemeVariables : DeucarianThemeTargetBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private DeucarianColorRoleLibrary roleLibrary;
        [SerializeField] private string variablePrefix = "--deucarian-color-";
        [SerializeField] private bool useRoleIdAsVariableName;
        [SerializeField] private List<DeucarianUIToolkitThemeVariableMapping> explicitVariableMappings =
            new List<DeucarianUIToolkitThemeVariableMapping>();

        private readonly Dictionary<string, Color> lastVariableColors = new Dictionary<string, Color>();
        private bool warnedRuntimeVariablesUnsupported;

        /// <summary>UIDocument whose root owns the generated variable context.</summary>
        public UIDocument Document
        {
            get => document;
            set => document = value;
        }

        /// <summary>Role library used to generate variables.</summary>
        public DeucarianColorRoleLibrary RoleLibrary
        {
            get => roleLibrary;
            set => roleLibrary = value;
        }

        /// <summary>Prefix used for generated USS variable names.</summary>
        public string VariablePrefix
        {
            get => variablePrefix;
            set => variablePrefix = string.IsNullOrWhiteSpace(value) ? "--deucarian-color-" : value;
        }

        /// <summary>Whether generated names use the stable role ID instead of the role display name.</summary>
        public bool UseRoleIdAsVariableName
        {
            get => useRoleIdAsVariableName;
            set => useRoleIdAsVariableName = value;
        }

        /// <summary>Latest generated variable color values.</summary>
        public IReadOnlyDictionary<string, Color> LastVariableColors => lastVariableColors;

        /// <summary>Applies, or more precisely generates, variable values for the currently resolved theme.</summary>
        public void ApplyVariablesNow()
        {
            ApplyTheme();
        }

        /// <summary>Builds variable values without mutating scene state.</summary>
        public Dictionary<string, Color> BuildVariableColorMap(DeucarianTheme theme)
        {
            Dictionary<string, Color> map = new Dictionary<string, Color>();
            if (theme == null || roleLibrary == null)
            {
                return map;
            }

            IReadOnlyList<DeucarianColorRole> roles = roleLibrary.Roles;
            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianColorRole role = roles[i];
                if (role == null)
                {
                    continue;
                }

                string variableName = GetVariableName(role);
                if (string.IsNullOrWhiteSpace(variableName))
                {
                    continue;
                }

                map[variableName] = theme.GetColor(role);
            }

            return map;
        }

        /// <summary>Generates USS text from the currently resolved theme.</summary>
        public string GenerateUssText(string selector = ":root")
        {
            DeucarianTheme theme = ResolveTheme(null);
            return GenerateUssText(theme, selector);
        }

        /// <summary>Generates USS text from a supplied theme.</summary>
        public string GenerateUssText(DeucarianTheme theme, string selector = ":root")
        {
            Dictionary<string, Color> map = BuildVariableColorMap(theme);
            string safeSelector = string.IsNullOrWhiteSpace(selector) ? ":root" : selector.Trim();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(safeSelector + " {");

            foreach (KeyValuePair<string, Color> pair in map)
            {
                builder.Append("  ");
                builder.Append(pair.Key);
                builder.Append(": ");
                builder.Append(ToUssColor(pair.Value));
                builder.AppendLine(";");
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>Returns generated variable names for the assigned role library.</summary>
        public List<string> PreviewVariableNames()
        {
            List<string> names = new List<string>();
            if (roleLibrary == null)
            {
                return names;
            }

            IReadOnlyList<DeucarianColorRole> roles = roleLibrary.Roles;
            for (int i = 0; i < roles.Count; i++)
            {
                DeucarianColorRole role = roles[i];
                if (role != null)
                {
                    names.Add(GetVariableName(role));
                }
            }

            return names;
        }

        /// <inheritdoc />
        protected override void ApplyResolvedTheme(DeucarianTheme theme)
        {
            lastVariableColors.Clear();
            Dictionary<string, Color> map = BuildVariableColorMap(theme);
            foreach (KeyValuePair<string, Color> pair in map)
            {
                lastVariableColors[pair.Key] = pair.Value;
            }

            CacheDocument();
            VisualElement root = document != null ? document.rootVisualElement : null;
            if (root != null)
            {
                WarnRuntimeVariablesUnsupported();
            }
        }

        protected override void OnEnable()
        {
            CacheDocument();
            base.OnEnable();
        }

        private void CacheDocument()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
        }

        private string GetVariableName(DeucarianColorRole role)
        {
            string explicitName = FindExplicitVariableName(role);
            if (!string.IsNullOrWhiteSpace(explicitName))
            {
                return NormalizeVariableName(explicitName);
            }

            string source = useRoleIdAsVariableName || string.IsNullOrWhiteSpace(role.DisplayName)
                ? role.Id
                : role.DisplayName;

            return NormalizeVariableName(variablePrefix + DeucarianUIToolkitThemeUtility.ToSafeVariableName(source));
        }

        private string FindExplicitVariableName(DeucarianColorRole role)
        {
            if (role == null || explicitVariableMappings == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < explicitVariableMappings.Count; i++)
            {
                DeucarianUIToolkitThemeVariableMapping mapping = explicitVariableMappings[i];
                if (mapping == null || mapping.Role == null)
                {
                    continue;
                }

                if (mapping.Role == role || mapping.Role.Id == role.Id)
                {
                    return mapping.VariableName;
                }
            }

            return string.Empty;
        }

        private static string NormalizeVariableName(string value)
        {
            string trimmed = string.IsNullOrWhiteSpace(value) ? "--deucarian-color-unnamed" : value.Trim();
            if (trimmed.StartsWith("--"))
            {
                return trimmed;
            }

            return "--" + DeucarianUIToolkitThemeUtility.ToSafeVariableName(trimmed);
        }

        private static string ToUssColor(Color color)
        {
            Color32 color32 = color;
            return $"rgba({color32.r}, {color32.g}, {color32.b}, {color.a:0.###})";
        }

        private void WarnRuntimeVariablesUnsupported()
        {
            if (warnedRuntimeVariablesUnsupported)
            {
                return;
            }

            warnedRuntimeVariablesUnsupported = true;
            Debug.LogWarning(
                "Unity UI Toolkit does not expose a stable runtime API for assigning USS custom variables in Unity 2022.3. Variable values were generated and can be previewed or exported as USS text.",
                this);
        }
    }
}
