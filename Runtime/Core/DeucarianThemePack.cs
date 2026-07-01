using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Package-provided definition for creating or repairing theme assets.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme Pack", menuName = "Deucarian/Theming/Theme Pack")]
    public sealed class DeucarianThemePack : ScriptableObject
    {
        [SerializeField] private string packId = "deucarian.theme-pack";
        [SerializeField] private string displayName = "Theme Pack";
        [SerializeField] private string roleLibraryFileName = "ColorRoleLibrary.asset";
        [SerializeField] private string paletteFileName = "ColorPalette.asset";
        [SerializeField] private string themeFileName = "Theme.asset";
        [SerializeField] private string paletteId = "deucarian.palette.theme-pack";
        [SerializeField] private string paletteDisplayName = "Theme Pack Palette";
        [SerializeField] private string themeId = "deucarian.theme.theme-pack";
        [SerializeField] private string themeDisplayName = "Theme Pack";
        [SerializeField] private string defaultStyleId = DeucarianThemeStyleIds.FrostedGlass;
        [SerializeField] private List<DeucarianThemePackRole> roles = new List<DeucarianThemePackRole>();

        public string PackId => packId;
        public string DisplayName => displayName;
        public string RoleLibraryFileName => roleLibraryFileName;
        public string PaletteFileName => paletteFileName;
        public string ThemeFileName => themeFileName;
        public string PaletteId => paletteId;
        public string PaletteDisplayName => paletteDisplayName;
        public string ThemeId => themeId;
        public string ThemeDisplayName => themeDisplayName;
        public string DefaultStyleId => defaultStyleId;
        public IReadOnlyList<DeucarianThemePackRole> Roles => roles;

        public void Configure(
            string id,
            string name,
            string libraryFileName,
            string paletteAssetFileName,
            string themeAssetFileName,
            string packPaletteId,
            string packPaletteName,
            string packThemeId,
            string packThemeName,
            string styleId,
            IEnumerable<DeucarianThemePackRole> roleDefinitions)
        {
            packId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            roleLibraryFileName = NormalizeFileName(libraryFileName, "ColorRoleLibrary.asset");
            paletteFileName = NormalizeFileName(paletteAssetFileName, "ColorPalette.asset");
            themeFileName = NormalizeFileName(themeAssetFileName, "Theme.asset");
            paletteId = DeucarianColorRole.NormalizeId(packPaletteId);
            paletteDisplayName = packPaletteName ?? string.Empty;
            themeId = DeucarianColorRole.NormalizeId(packThemeId);
            themeDisplayName = packThemeName ?? string.Empty;
            defaultStyleId = DeucarianColorRole.NormalizeId(styleId);
            SetRoles(roleDefinitions);
            NotifyChanged();
        }

        public void SetRoles(IEnumerable<DeucarianThemePackRole> roleDefinitions)
        {
            roles = new List<DeucarianThemePackRole>();
            if (roleDefinitions == null)
            {
                return;
            }

            foreach (DeucarianThemePackRole role in roleDefinitions)
            {
                if (role != null)
                {
                    roles.Add(role.Clone());
                }
            }

            NotifyChanged();
        }

        private void OnValidate()
        {
            packId = DeucarianColorRole.NormalizeId(packId);
            displayName = displayName ?? string.Empty;
            roleLibraryFileName = NormalizeFileName(roleLibraryFileName, "ColorRoleLibrary.asset");
            paletteFileName = NormalizeFileName(paletteFileName, "ColorPalette.asset");
            themeFileName = NormalizeFileName(themeFileName, "Theme.asset");
            paletteId = DeucarianColorRole.NormalizeId(paletteId);
            paletteDisplayName = paletteDisplayName ?? string.Empty;
            themeId = DeucarianColorRole.NormalizeId(themeId);
            themeDisplayName = themeDisplayName ?? string.Empty;
            defaultStyleId = DeucarianColorRole.NormalizeId(defaultStyleId);

            if (roles == null)
            {
                roles = new List<DeucarianThemePackRole>();
                return;
            }

            for (int i = 0; i < roles.Count; i++)
            {
                roles[i]?.Normalize();
            }

            NotifyChanged();
        }

        private void NotifyChanged()
        {
            DeucarianThemeAssetChangeBus.NotifyChanged(this);
        }

        private static string NormalizeFileName(string value, string fallback)
        {
            string fileName = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return fileName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : fileName + ".asset";
        }
    }

    [Serializable]
    public sealed class DeucarianThemePackRole
    {
        [SerializeField] private string assetName = "Color Role";
        [SerializeField] private string id = DeucarianBuiltinColorRoleIds.Primary;
        [SerializeField] private string displayName = "Color Role";
        [SerializeField] private string category = DeucarianColorRoleCategories.Semantic;
        [SerializeField] private string description = string.Empty;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private bool isCoreRole;

        public DeucarianThemePackRole()
        {
        }

        public DeucarianThemePackRole(
            string roleAssetName,
            string roleId,
            string roleDisplayName,
            string roleCategory,
            string roleDescription,
            Color roleDefaultColor,
            bool coreRole)
        {
            Configure(
                roleAssetName,
                roleId,
                roleDisplayName,
                roleCategory,
                roleDescription,
                roleDefaultColor,
                coreRole);
        }

        public string AssetName => assetName;
        public string Id => id;
        public string DisplayName => displayName;
        public string Category => category;
        public string Description => description;
        public Color DefaultColor => defaultColor;
        public bool IsCoreRole => isCoreRole;

        public void Configure(
            string roleAssetName,
            string roleId,
            string roleDisplayName,
            string roleCategory,
            string roleDescription,
            Color roleDefaultColor,
            bool coreRole)
        {
            assetName = roleAssetName ?? string.Empty;
            id = DeucarianColorRole.NormalizeId(roleId);
            displayName = roleDisplayName ?? string.Empty;
            category = roleCategory ?? string.Empty;
            description = roleDescription ?? string.Empty;
            defaultColor = roleDefaultColor;
            isCoreRole = coreRole;
        }

        public DeucarianThemePackRole Clone()
        {
            return new DeucarianThemePackRole(
                assetName,
                id,
                displayName,
                category,
                description,
                defaultColor,
                isCoreRole);
        }

        internal void Normalize()
        {
            assetName = assetName ?? string.Empty;
            id = DeucarianColorRole.NormalizeId(id);
            displayName = displayName ?? string.Empty;
            category = category ?? string.Empty;
            description = description ?? string.Empty;
        }
    }
}
