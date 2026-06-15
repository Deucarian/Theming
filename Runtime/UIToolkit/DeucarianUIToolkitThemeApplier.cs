using System.Collections.Generic;
using Deucarian.Theming;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.UIToolkit
{
    /// <summary>
    /// Applies Deucarian theme colors to a UIDocument visual tree.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeucarianUIToolkitThemeApplier : DeucarianThemeTargetBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private List<DeucarianUIToolkitThemeBinding> bindings = new List<DeucarianUIToolkitThemeBinding>();
        [SerializeField] private bool reapplyOnGeometryChanged = true;
        [SerializeField] private bool logMissingElements = true;
        [SerializeField] private bool logMissingRoles = true;

        private VisualElement geometryCallbackRoot;
        private bool scheduledApply;
        private bool appliedAfterGeometry;
        private bool warnedMissingDocument;
        private readonly HashSet<string> warnedBindingMessages = new HashSet<string>();

        /// <summary>UIDocument whose visual tree receives theme colors.</summary>
        public UIDocument Document
        {
            get => document;
            set
            {
                if (document == value)
                {
                    return;
                }

                UnregisterGeometryCallback();
                document = value;
                RegisterGeometryCallback();

                if (isActiveAndEnabled)
                {
                    ApplyTheme();
                    ScheduleApply();
                }
            }
        }

        /// <summary>Bindings applied to the document root.</summary>
        public IReadOnlyList<DeucarianUIToolkitThemeBinding> Bindings => bindings;

        /// <summary>Whether this applier re-applies once after the first geometry change.</summary>
        public bool ReapplyOnGeometryChanged
        {
            get => reapplyOnGeometryChanged;
            set => reapplyOnGeometryChanged = value;
        }

        /// <summary>Applies the currently resolved theme immediately.</summary>
        public void ApplyNow()
        {
            ApplyTheme();
        }

        /// <summary>Returns validation warnings for the configured bindings and current document.</summary>
        public List<string> ValidateBindings()
        {
            List<string> warnings = new List<string>();
            VisualElement root = GetRoot();

            for (int i = 0; i < bindings.Count; i++)
            {
                DeucarianUIToolkitThemeBinding binding = bindings[i];
                if (binding == null)
                {
                    warnings.Add($"Binding {i} is null.");
                    continue;
                }

                if (binding.ColorRole == null)
                {
                    warnings.Add($"Binding {i} has no color role.");
                }

                if (TargetsRoot(binding))
                {
                    warnings.Add($"Binding {i} has no selector, name, or class, so it targets the UIDocument root.");
                }

                if (root != null)
                {
                    int count = CountMatches(root, binding);
                    if (count == 0)
                    {
                        warnings.Add($"Binding {i} matched no elements.");
                    }
                }
            }

            if (root == null)
            {
                warnings.Add("No UIDocument root is available.");
            }

            return warnings;
        }

        /// <summary>Counts matching elements for a binding against the current document root.</summary>
        public int CountMatches(DeucarianUIToolkitThemeBinding binding)
        {
            VisualElement root = GetRoot();
            return root == null ? 0 : CountMatches(root, binding);
        }

        /// <summary>Applies a theme to an explicit root. Useful for editor tooling and tests.</summary>
        public void ApplyToRoot(VisualElement root, DeucarianTheme theme)
        {
            if (root == null || theme == null)
            {
                return;
            }

            ApplyBindings(root, theme);
        }

        /// <inheritdoc />
        protected override void ApplyResolvedTheme(DeucarianTheme theme)
        {
            CacheDocument();
            VisualElement root = GetRoot();
            if (root == null)
            {
                WarnMissingDocument();
                ScheduleApply();
                return;
            }

            warnedMissingDocument = false;
            ApplyBindings(root, theme);
        }

        protected override void OnEnable()
        {
            CacheDocument();
            RegisterGeometryCallback();
            base.OnEnable();
            ScheduleApply();
        }

        protected override void OnDisable()
        {
            UnregisterGeometryCallback();
            scheduledApply = false;
            appliedAfterGeometry = false;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            UnregisterGeometryCallback();
            base.OnDestroy();
        }

        private void ApplyBindings(VisualElement root, DeucarianTheme theme)
        {
            if (bindings == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                DeucarianUIToolkitThemeBinding binding = bindings[i];
                if (binding == null)
                {
                    continue;
                }

                if (binding.ColorRole == null)
                {
                    WarnBinding(logMissingRoles, $"UI Toolkit theme binding {i} has no color role.");
                    continue;
                }

                Color color = DeucarianUIToolkitThemeUtility.GetColorOrFallback(theme, binding.ColorRole);
                int matchCount = 0;

                foreach (VisualElement element in DeucarianUIToolkitThemeUtility.ResolveElements(root, binding))
                {
                    DeucarianUIToolkitThemeUtility.ApplyColor(
                        element,
                        binding.StyleProperty,
                        color,
                        binding.CustomCssVariableName);
                    matchCount++;
                }

                if (matchCount == 0)
                {
                    WarnBinding(logMissingElements, $"UI Toolkit theme binding {i} matched no elements.");
                }
            }
        }

        private void CacheDocument()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
        }

        private VisualElement GetRoot()
        {
            CacheDocument();
            return document != null ? document.rootVisualElement : null;
        }

        private void RegisterGeometryCallback()
        {
            if (!reapplyOnGeometryChanged)
            {
                return;
            }

            VisualElement root = GetRoot();
            if (root == null || root == geometryCallbackRoot)
            {
                return;
            }

            UnregisterGeometryCallback();
            geometryCallbackRoot = root;
            geometryCallbackRoot.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterGeometryCallback()
        {
            if (geometryCallbackRoot == null)
            {
                return;
            }

            geometryCallbackRoot.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            geometryCallbackRoot = null;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!reapplyOnGeometryChanged || appliedAfterGeometry)
            {
                return;
            }

            appliedAfterGeometry = true;
            ApplyTheme();
        }

        private void ScheduleApply()
        {
            if (scheduledApply)
            {
                return;
            }

            VisualElement root = GetRoot();
            if (root == null)
            {
                return;
            }

            scheduledApply = true;
            root.schedule.Execute(() =>
            {
                scheduledApply = false;
                if (isActiveAndEnabled)
                {
                    RegisterGeometryCallback();
                    ApplyTheme();
                }
            }).ExecuteLater(0);
        }

        private void WarnMissingDocument()
        {
            if (!logMissingElements || warnedMissingDocument)
            {
                return;
            }

            warnedMissingDocument = true;
            ThemingLog.UIToolkit.Warning("UI Toolkit theme applier has no UIDocument root to theme.", this);
        }

        private void WarnBinding(bool shouldLog, string message)
        {
            if (!shouldLog || warnedBindingMessages.Contains(message))
            {
                return;
            }

            warnedBindingMessages.Add(message);
            ThemingLog.UIToolkit.Warning(message, this);
        }

        private static bool TargetsRoot(DeucarianUIToolkitThemeBinding binding)
        {
            return string.IsNullOrWhiteSpace(binding.UssSelector)
                && string.IsNullOrWhiteSpace(binding.ElementName)
                && string.IsNullOrWhiteSpace(binding.ElementClass);
        }

        private static int CountMatches(VisualElement root, DeucarianUIToolkitThemeBinding binding)
        {
            int count = 0;
            foreach (VisualElement ignored in DeucarianUIToolkitThemeUtility.ResolveElements(root, binding))
            {
                count++;
            }

            return count;
        }
    }
}
