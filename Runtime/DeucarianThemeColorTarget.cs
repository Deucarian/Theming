using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Base class for components that apply a theme color role to a Unity component.
    /// </summary>
    public abstract class DeucarianThemeColorTarget : DeucarianThemeTargetBehaviour
    {
        [SerializeField] private DeucarianColorRole colorRole;

        private bool warnedMissingRole;

        /// <summary>Role whose color should be applied.</summary>
        public DeucarianColorRole ColorRole
        {
            get => colorRole;
            set
            {
                colorRole = value;
                if (isActiveAndEnabled)
                {
                    ApplyTheme();
                }
            }
        }

        /// <inheritdoc />
        protected override void ApplyResolvedTheme(DeucarianTheme theme)
        {
            CacheTarget();

            if (colorRole == null)
            {
                WarnOnce(ref warnedMissingRole, "Theme color target has no color role assigned.");
                return;
            }

            warnedMissingRole = false;

            Color color = theme.GetColor(colorRole);
            ApplyColor(color);
        }

        /// <summary>Allows derived adapters to cache their target component.</summary>
        protected virtual void CacheTarget()
        {
        }

        /// <summary>Applies the resolved color to the concrete target component.</summary>
        protected abstract void ApplyColor(Color color);

        protected virtual void Awake()
        {
            CacheTarget();
        }
    }
}
