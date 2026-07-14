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
        [SerializeField] private bool supportsThemeFamily;
        [SerializeField] private string familyFileName = "ThemeFamily.asset";
        [SerializeField] private string familyId = "deucarian.theme-family.theme-pack";
        [SerializeField] private string familyDisplayName = "Theme Pack";
        [SerializeField] private string lightPaletteFileName = "LightColorPalette.asset";
        [SerializeField] private string lightPaletteId = "deucarian.palette.theme-pack.light";
        [SerializeField] private string lightPaletteDisplayName = "Theme Pack Light Palette";
        [SerializeField] private string darkPaletteFileName = "DarkColorPalette.asset";
        [SerializeField] private string darkPaletteId = "deucarian.palette.theme-pack.dark";
        [SerializeField] private string darkPaletteDisplayName = "Theme Pack Dark Palette";
        [SerializeField] private string lightThemeFileName = "LightTheme.asset";
        [SerializeField] private string lightThemeId = "deucarian.theme.theme-pack.light";
        [SerializeField] private string lightThemeDisplayName = "Theme Pack Light";
        [SerializeField] private string darkThemeFileName = "DarkTheme.asset";
        [SerializeField] private string darkThemeId = "deucarian.theme.theme-pack.dark";
        [SerializeField] private string darkThemeDisplayName = "Theme Pack Dark";
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
        public bool SupportsThemeFamily => supportsThemeFamily;
        public string FamilyFileName => familyFileName;
        public string FamilyId => familyId;
        public string FamilyDisplayName => familyDisplayName;
        public string LightPaletteFileName => lightPaletteFileName;
        public string LightPaletteId => lightPaletteId;
        public string LightPaletteDisplayName => lightPaletteDisplayName;
        public string DarkPaletteFileName => darkPaletteFileName;
        public string DarkPaletteId => darkPaletteId;
        public string DarkPaletteDisplayName => darkPaletteDisplayName;
        public string LightThemeFileName => lightThemeFileName;
        public string LightThemeId => lightThemeId;
        public string LightThemeDisplayName => lightThemeDisplayName;
        public string DarkThemeFileName => darkThemeFileName;
        public string DarkThemeId => darkThemeId;
        public string DarkThemeDisplayName => darkThemeDisplayName;
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
            supportsThemeFamily = false;
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

        /// <summary>Configures a pack that authors paired light and dark theme-family assets.</summary>
        public void Configure(
            string id,
            string name,
            string libraryFileName,
            string familyAssetFileName,
            string lightPaletteAssetFileName,
            string darkPaletteAssetFileName,
            string lightThemeAssetFileName,
            string darkThemeAssetFileName,
            string packFamilyId,
            string packFamilyName,
            string packLightPaletteId,
            string packLightPaletteName,
            string packDarkPaletteId,
            string packDarkPaletteName,
            string packLightThemeId,
            string packLightThemeName,
            string packDarkThemeId,
            string packDarkThemeName,
            string styleId,
            IEnumerable<DeucarianThemePackRole> roleDefinitions)
        {
            supportsThemeFamily = true;
            packId = DeucarianColorRole.NormalizeId(id);
            displayName = name ?? string.Empty;
            roleLibraryFileName = NormalizeFileName(libraryFileName, "ColorRoleLibrary.asset");
            familyFileName = NormalizeFileName(familyAssetFileName, "ThemeFamily.asset");
            familyId = DeucarianColorRole.NormalizeId(packFamilyId);
            familyDisplayName = packFamilyName ?? string.Empty;
            lightPaletteFileName = NormalizeFileName(lightPaletteAssetFileName, "LightColorPalette.asset");
            lightPaletteId = DeucarianColorRole.NormalizeId(packLightPaletteId);
            lightPaletteDisplayName = packLightPaletteName ?? string.Empty;
            darkPaletteFileName = NormalizeFileName(darkPaletteAssetFileName, "DarkColorPalette.asset");
            darkPaletteId = DeucarianColorRole.NormalizeId(packDarkPaletteId);
            darkPaletteDisplayName = packDarkPaletteName ?? string.Empty;
            lightThemeFileName = NormalizeFileName(lightThemeAssetFileName, "LightTheme.asset");
            lightThemeId = DeucarianColorRole.NormalizeId(packLightThemeId);
            lightThemeDisplayName = packLightThemeName ?? string.Empty;
            darkThemeFileName = NormalizeFileName(darkThemeAssetFileName, "DarkTheme.asset");
            darkThemeId = DeucarianColorRole.NormalizeId(packDarkThemeId);
            darkThemeDisplayName = packDarkThemeName ?? string.Empty;

            // Keep old pack consumers deterministic by exposing the dark variant as the legacy default.
            paletteFileName = darkPaletteFileName;
            paletteId = darkPaletteId;
            paletteDisplayName = darkPaletteDisplayName;
            themeFileName = darkThemeFileName;
            themeId = darkThemeId;
            themeDisplayName = darkThemeDisplayName;
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
            familyFileName = NormalizeFileName(familyFileName, "ThemeFamily.asset");
            familyId = DeucarianColorRole.NormalizeId(familyId);
            familyDisplayName = familyDisplayName ?? string.Empty;
            lightPaletteFileName = NormalizeFileName(lightPaletteFileName, "LightColorPalette.asset");
            lightPaletteId = DeucarianColorRole.NormalizeId(lightPaletteId);
            lightPaletteDisplayName = lightPaletteDisplayName ?? string.Empty;
            darkPaletteFileName = NormalizeFileName(darkPaletteFileName, "DarkColorPalette.asset");
            darkPaletteId = DeucarianColorRole.NormalizeId(darkPaletteId);
            darkPaletteDisplayName = darkPaletteDisplayName ?? string.Empty;
            lightThemeFileName = NormalizeFileName(lightThemeFileName, "LightTheme.asset");
            lightThemeId = DeucarianColorRole.NormalizeId(lightThemeId);
            lightThemeDisplayName = lightThemeDisplayName ?? string.Empty;
            darkThemeFileName = NormalizeFileName(darkThemeFileName, "DarkTheme.asset");
            darkThemeId = DeucarianColorRole.NormalizeId(darkThemeId);
            darkThemeDisplayName = darkThemeDisplayName ?? string.Empty;
            if (supportsThemeFamily)
            {
                paletteFileName = darkPaletteFileName;
                paletteId = darkPaletteId;
                paletteDisplayName = darkPaletteDisplayName;
                themeFileName = darkThemeFileName;
                themeId = darkThemeId;
                themeDisplayName = darkThemeDisplayName;
            }

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
        [SerializeField] private bool hasPairedColors;
        [SerializeField] private Color lightColor = Color.white;
        [SerializeField] private Color darkColor = Color.white;
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

        public DeucarianThemePackRole(
            string roleAssetName,
            string roleId,
            string roleDisplayName,
            string roleCategory,
            string roleDescription,
            Color roleLightColor,
            Color roleDarkColor,
            bool coreRole)
        {
            Configure(
                roleAssetName,
                roleId,
                roleDisplayName,
                roleCategory,
                roleDescription,
                roleLightColor,
                roleDarkColor,
                coreRole);
        }

        public string AssetName => assetName;
        public string Id => id;
        public string DisplayName => displayName;
        public string Category => category;
        public string Description => description;
        public Color DefaultColor => defaultColor;
        public bool HasPairedColors => hasPairedColors;
        public Color LightColor => hasPairedColors ? lightColor : defaultColor;
        public Color DarkColor => hasPairedColors ? darkColor : defaultColor;
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
            hasPairedColors = false;
            lightColor = roleDefaultColor;
            darkColor = roleDefaultColor;
            isCoreRole = coreRole;
        }

        public void Configure(
            string roleAssetName,
            string roleId,
            string roleDisplayName,
            string roleCategory,
            string roleDescription,
            Color roleLightColor,
            Color roleDarkColor,
            bool coreRole)
        {
            assetName = roleAssetName ?? string.Empty;
            id = DeucarianColorRole.NormalizeId(roleId);
            displayName = roleDisplayName ?? string.Empty;
            category = roleCategory ?? string.Empty;
            description = roleDescription ?? string.Empty;
            lightColor = roleLightColor;
            darkColor = roleDarkColor;
            defaultColor = roleDarkColor;
            hasPairedColors = true;
            isCoreRole = coreRole;
        }

        public DeucarianThemePackRole Clone()
        {
            return hasPairedColors
                ? new DeucarianThemePackRole(
                    assetName,
                    id,
                    displayName,
                    category,
                    description,
                    lightColor,
                    darkColor,
                    isCoreRole)
                : new DeucarianThemePackRole(
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
            if (hasPairedColors)
            {
                defaultColor = darkColor;
            }
            else
            {
                lightColor = defaultColor;
                darkColor = defaultColor;
            }
        }
    }
}
