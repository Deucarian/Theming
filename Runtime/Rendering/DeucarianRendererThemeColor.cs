using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Applies a theme color role to a Renderer using MaterialPropertyBlock.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class DeucarianRendererThemeColor : DeucarianThemeColorTarget
    {
        [SerializeField] private string materialColorProperty = "_BaseColor";

        private Renderer target;
        private MaterialPropertyBlock propertyBlock;

        /// <summary>Shader color property that receives the resolved theme color.</summary>
        public string MaterialColorProperty
        {
            get => materialColorProperty;
            set => materialColorProperty = string.IsNullOrWhiteSpace(value) ? "_BaseColor" : value;
        }

        protected override void CacheTarget()
        {
            if (target == null)
            {
                target = GetComponent<Renderer>();
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        protected override void ApplyColor(Color color)
        {
            if (target == null || propertyBlock == null)
            {
                return;
            }

            string propertyName = string.IsNullOrWhiteSpace(materialColorProperty) ? "_BaseColor" : materialColorProperty;
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(propertyName, color);
            target.SetPropertyBlock(propertyBlock);
        }

        private void Reset()
        {
            materialColorProperty = "_BaseColor";
            CacheTarget();
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(materialColorProperty))
            {
                materialColorProperty = "_BaseColor";
            }
        }
    }
}
