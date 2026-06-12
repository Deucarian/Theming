using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Theming
{
    /// <summary>
    /// Applies theme colors to all Unity UI Selectable ColorBlock states.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class DeucarianSelectableThemeColors : DeucarianThemeTargetBehaviour
    {
        [SerializeField] private DeucarianColorRole normalRole;
        [SerializeField] private DeucarianColorRole highlightedRole;
        [SerializeField] private DeucarianColorRole pressedRole;
        [SerializeField] private DeucarianColorRole selectedRole;
        [SerializeField] private DeucarianColorRole disabledRole;

        private Selectable target;
        private bool warnedMissingRole;

        /// <summary>Role used for <see cref="ColorBlock.normalColor"/>.</summary>
        public DeucarianColorRole NormalRole
        {
            get => normalRole;
            set
            {
                normalRole = value;
                ApplyThemeIfEnabled();
            }
        }

        /// <summary>Role used for <see cref="ColorBlock.highlightedColor"/>.</summary>
        public DeucarianColorRole HighlightedRole
        {
            get => highlightedRole;
            set
            {
                highlightedRole = value;
                ApplyThemeIfEnabled();
            }
        }

        /// <summary>Role used for <see cref="ColorBlock.pressedColor"/>.</summary>
        public DeucarianColorRole PressedRole
        {
            get => pressedRole;
            set
            {
                pressedRole = value;
                ApplyThemeIfEnabled();
            }
        }

        /// <summary>Role used for <see cref="ColorBlock.selectedColor"/>.</summary>
        public DeucarianColorRole SelectedRole
        {
            get => selectedRole;
            set
            {
                selectedRole = value;
                ApplyThemeIfEnabled();
            }
        }

        /// <summary>Role used for <see cref="ColorBlock.disabledColor"/>.</summary>
        public DeucarianColorRole DisabledRole
        {
            get => disabledRole;
            set
            {
                disabledRole = value;
                ApplyThemeIfEnabled();
            }
        }

        /// <inheritdoc />
        protected override void ApplyResolvedTheme(DeucarianTheme theme)
        {
            CacheTarget();
            if (target == null)
            {
                return;
            }

            ColorBlock colors = target.colors;
            colors.normalColor = ResolveColor(theme, normalRole, colors.normalColor, "normal");
            colors.highlightedColor = ResolveColor(theme, highlightedRole, colors.highlightedColor, "highlighted");
            colors.pressedColor = ResolveColor(theme, pressedRole, colors.pressedColor, "pressed");
            colors.selectedColor = ResolveColor(theme, selectedRole, colors.selectedColor, "selected");
            colors.disabledColor = ResolveColor(theme, disabledRole, colors.disabledColor, "disabled");
            target.colors = colors;
        }

        private Color ResolveColor(DeucarianTheme theme, DeucarianColorRole role, Color fallback, string stateName)
        {
            if (role != null)
            {
                return theme.GetColor(role);
            }

            WarnOnce(ref warnedMissingRole, $"Selectable theme colors has no role assigned for the '{stateName}' state.");
            return fallback;
        }

        private void CacheTarget()
        {
            if (target == null)
            {
                target = GetComponent<Selectable>();
            }
        }

        private void ApplyThemeIfEnabled()
        {
            if (isActiveAndEnabled)
            {
                ApplyTheme();
            }
        }

        private void Awake()
        {
            CacheTarget();
        }

        private void Reset()
        {
            CacheTarget();
        }
    }
}
