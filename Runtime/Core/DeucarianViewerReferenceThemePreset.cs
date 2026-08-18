using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Cached, consumer-neutral runtime theme composition for reusable 3D viewers.
    /// </summary>
    public sealed class DeucarianViewerReferenceThemeProfile
    {
        internal DeucarianViewerReferenceThemeProfile(
            DeucarianColorRoleLibrary roleLibrary,
            DeucarianColorPalette lightPalette,
            DeucarianColorPalette darkPalette,
            DeucarianThemeStyle visualStyle,
            DeucarianTheme lightTheme,
            DeucarianTheme darkTheme,
            DeucarianThemeFamily themeFamily)
        {
            RoleLibrary = roleLibrary;
            LightPalette = lightPalette;
            DarkPalette = darkPalette;
            VisualStyle = visualStyle;
            LightTheme = lightTheme;
            DarkTheme = darkTheme;
            ThemeFamily = themeFamily;
        }

        /// <summary>Built-in semantic roles used by both palette variants.</summary>
        public DeucarianColorRoleLibrary RoleLibrary { get; }

        /// <summary>Canonical light viewer palette.</summary>
        public DeucarianColorPalette LightPalette { get; }

        /// <summary>Canonical dark viewer palette.</summary>
        public DeucarianColorPalette DarkPalette { get; }

        /// <summary>Shared Frosted Glass chrome style.</summary>
        public DeucarianThemeStyle VisualStyle { get; }

        /// <summary>Light theme variant.</summary>
        public DeucarianTheme LightTheme { get; }

        /// <summary>Dark theme variant.</summary>
        public DeucarianTheme DarkTheme { get; }

        /// <summary>Canonical startup variant.</summary>
        public DeucarianTheme DefaultTheme => DarkTheme;

        /// <summary>Complete light/dark viewer family.</summary>
        public DeucarianThemeFamily ThemeFamily { get; }

        /// <summary>Resolves the exact theme variant for a mode.</summary>
        public DeucarianTheme ResolveTheme(DeucarianThemeMode mode)
        {
            return ThemeFamily.GetTheme(mode);
        }

        /// <summary>Resolves a semantic color from the selected variant.</summary>
        public bool TryGetColor(
            DeucarianThemeMode mode,
            string roleId,
            out Color color)
        {
            DeucarianTheme theme = ResolveTheme(mode);
            if (theme != null)
            {
                return theme.TryGetColorById(roleId, out color);
            }

            color = DeucarianColorPalette.MissingColor;
            return false;
        }
    }

    /// <summary>
    /// Creates the canonical cached runtime theme used by reference viewer compositions.
    /// The returned runtime objects are shared and should be treated as read-only.
    /// </summary>
    public static class DeucarianViewerReferenceThemePreset
    {
        public const string FamilyId = "deucarian.theme-family.viewer-reference";
        public const string LightThemeId = "deucarian.theme.viewer-reference.light";
        public const string DarkThemeId = "deucarian.theme.viewer-reference.dark";
        public const string LightPaletteId = "deucarian.palette.viewer-reference.light";
        public const string DarkPaletteId = "deucarian.palette.viewer-reference.dark";
        public const string TypographyResourcePath =
            "Deucarian/Theming/ViewerReferenceTypography";
        public const DeucarianThemeMode DefaultMode = DeucarianThemeMode.Dark;

        private static readonly SemanticColorDefinition[] SemanticColors =
        {
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Background,
                "Background",
                DeucarianColorRoleCategories.Semantic,
                "Base viewer background.",
                new Color(0.9529412f, 0.94509804f, 0.96862745f, 1f),
                new Color(0.07f, 0.08f, 0.09f, 0.88f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Surface,
                "Surface",
                DeucarianColorRoleCategories.Semantic,
                "Base viewer panel surface.",
                Color.white,
                new Color(0.11f, 0.14f, 0.18f, 0.88f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.SurfaceRaised,
                "Surface Raised",
                DeucarianColorRoleCategories.Semantic,
                "Raised viewer control surface.",
                new Color(1f, 1f, 1f, 0.96862745f),
                new Color(0.11f, 0.14f, 0.18f, 0.94f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Primary,
                "Primary",
                DeucarianColorRoleCategories.Semantic,
                "Primary viewer interaction color.",
                new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f),
                new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Secondary,
                "Secondary",
                DeucarianColorRoleCategories.Semantic,
                "Secondary viewer interaction color.",
                new Color(0.49803922f, 0.32941177f, 0.7529412f, 1f),
                new Color(0.49803922f, 0.32941177f, 0.7529412f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Accent,
                "Accent",
                DeucarianColorRoleCategories.Semantic,
                "High-contrast viewer accent.",
                new Color(0.76862746f, 0.6313726f, 0.9764706f, 0.5019608f),
                new Color(0.76862746f, 0.6313726f, 0.9764706f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.TextPrimary,
                "Text Primary",
                DeucarianColorRoleCategories.Text,
                "Primary text and active icon color.",
                new Color(0.18039216f, 0.14509805f, 0.21960784f, 1f),
                new Color(0.95f, 0.97f, 0.96f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.TextSecondary,
                "Text Secondary",
                DeucarianColorRoleCategories.Text,
                "Secondary viewer copy.",
                new Color(0.33333334f, 0.29411766f, 0.38039216f, 1f),
                new Color(0.82f, 0.9f, 0.86f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.TextMuted,
                "Text Muted",
                DeucarianColorRoleCategories.Text,
                "Muted contextual text and inactive icons.",
                new Color(0.45490196f, 0.42352942f, 0.49019608f, 1f),
                new Color(0.78f, 0.82f, 0.8f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.TextDisabled,
                "Text Disabled",
                DeucarianColorRoleCategories.Text,
                "Disabled text and icon color.",
                new Color(0.6666667f, 0.6431373f, 0.69411767f, 1f),
                new Color(0.75686276f, 0.7647059f, 0.76862746f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Success,
                "Success",
                DeucarianColorRoleCategories.Status,
                "Successful or ready state.",
                new Color(0.18431373f, 0.44705883f, 0.34901962f, 1f),
                new Color(0.48235294f, 0.8117647f, 0.64705884f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Warning,
                "Warning",
                DeucarianColorRoleCategories.Status,
                "Warning state.",
                new Color(0.6039216f, 0.41568628f, 0.14117648f, 1f),
                new Color(0.84705883f, 0.68235296f, 0.38039216f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Error,
                "Error",
                DeucarianColorRoleCategories.Status,
                "Error state.",
                new Color(0.6039216f, 0.24313726f, 0.21960784f, 1f),
                new Color(0.8666667f, 0.5058824f, 0.47058824f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.Info,
                "Information",
                DeucarianColorRoleCategories.Status,
                "Informational state.",
                new Color(0.24705882f, 0.4f, 0.45882353f, 1f),
                new Color(0.4627451f, 0.6509804f, 0.72156864f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.UiNormal,
                "UI Normal",
                DeucarianColorRoleCategories.UiState,
                "Resting control background.",
                new Color(1f, 1f, 1f, 0f),
                new Color(1f, 1f, 1f, 0f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.UiHighlighted,
                "UI Highlighted",
                DeucarianColorRoleCategories.UiState,
                "Hovered control background.",
                new Color(0.76862746f, 0.6313726f, 0.9764706f, 0.2509804f),
                new Color(0.76862746f, 0.6313726f, 0.9764706f, 0.35f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.UiPressed,
                "UI Pressed",
                DeucarianColorRoleCategories.UiState,
                "Pressed control background.",
                new Color(0.3882353f, 0.25882354f, 0.5882353f, 0.2f),
                new Color(0.49803922f, 0.32941177f, 0.7529412f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.UiSelected,
                "UI Selected",
                DeucarianColorRoleCategories.UiState,
                "Selected control background.",
                new Color(0.49803922f, 0.32941177f, 0.7529412f, 0.5019608f),
                new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.UiDisabled,
                "UI Disabled",
                DeucarianColorRoleCategories.UiState,
                "Disabled control and icon color.",
                new Color(0.6666667f, 0.6431373f, 0.69411767f, 1f),
                new Color(0.75686276f, 0.7647059f, 0.76862746f, 1f)),
            new SemanticColorDefinition(
                DeucarianBuiltinColorRoleIds.UiFocused,
                "UI Focused",
                DeucarianColorRoleCategories.UiState,
                "Focused control outline or background.",
                new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f),
                new Color(0.76862746f, 0.6313726f, 0.9764706f, 1f))
        };

        private static DeucarianViewerReferenceThemeProfile cachedProfile;

        /// <summary>Creates the preset once and returns the shared runtime profile.</summary>
        public static DeucarianViewerReferenceThemeProfile Resolve()
        {
            if (!IsAlive(cachedProfile))
            {
                cachedProfile = CreateProfile();
            }

            return cachedProfile;
        }

        private static DeucarianViewerReferenceThemeProfile CreateProfile()
        {
            DeucarianColorRoleLibrary roleLibrary =
                CreateRuntimeAsset<DeucarianColorRoleLibrary>(
                    "Viewer Reference Color Roles");
            for (int i = 0; i < SemanticColors.Length; i++)
            {
                SemanticColorDefinition definition = SemanticColors[i];
                DeucarianColorRole role = CreateRuntimeAsset<DeucarianColorRole>(
                    definition.DisplayName);
                role.Configure(
                    definition.RoleId,
                    definition.DisplayName,
                    definition.Category,
                    definition.Description,
                    definition.LightColor,
                    definition.DarkColor,
                    true);
                roleLibrary.AddRole(role);
            }

            DeucarianColorPalette lightPalette = CreatePalette(
                LightPaletteId,
                "Viewer Reference Light Palette",
                roleLibrary,
                DeucarianThemeMode.Light);
            DeucarianColorPalette darkPalette = CreatePalette(
                DarkPaletteId,
                "Viewer Reference Dark Palette",
                roleLibrary,
                DeucarianThemeMode.Dark);
            DeucarianThemeStyle visualStyle =
                DeucarianThemeStylePresets.CreateRuntimeStyle(
                    DeucarianThemeStyleIds.FrostedGlass);
            visualStyle.name = "Viewer Reference Frosted Glass";
            visualStyle.SetComposition(
                CreateSurfaceProfile(
                    DeucarianThemePresentationProfileIds.Surface.FrostedGlass),
                CreateShapeProfile(
                    DeucarianThemePresentationProfileIds.Shape.Rounded),
                CreateStrokeProfile(
                    DeucarianThemePresentationProfileIds.Stroke.Frosted),
                DeucarianThemeDensity.Comfortable,
                RequireReferenceTypography());

            DeucarianTheme lightTheme = CreateRuntimeAsset<DeucarianTheme>(
                "Viewer Reference Light");
            lightTheme.Configure(
                LightThemeId,
                "Viewer Reference Light",
                lightPalette,
                visualStyle);
            DeucarianTheme darkTheme = CreateRuntimeAsset<DeucarianTheme>(
                "Viewer Reference Dark");
            darkTheme.Configure(
                DarkThemeId,
                "Viewer Reference Dark",
                darkPalette,
                visualStyle);
            DeucarianThemeFamily family = CreateRuntimeAsset<DeucarianThemeFamily>(
                "Viewer Reference Theme Family");
            family.Configure(
                FamilyId,
                "Viewer Reference",
                lightTheme,
                darkTheme);

            return new DeucarianViewerReferenceThemeProfile(
                roleLibrary,
                lightPalette,
                darkPalette,
                visualStyle,
                lightTheme,
                darkTheme,
                family);
        }

        private static DeucarianColorPalette CreatePalette(
            string id,
            string displayName,
            DeucarianColorRoleLibrary roleLibrary,
            DeucarianThemeMode mode)
        {
            DeucarianColorPalette palette =
                CreateRuntimeAsset<DeucarianColorPalette>(displayName);
            palette.Configure(id, displayName, roleLibrary, mode);
            palette.AddMissingRolesFromLibrary();
            return palette;
        }

        private static T CreateRuntimeAsset<T>(string assetName)
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            asset.hideFlags = HideFlags.HideAndDontSave;
            return asset;
        }

        private static DeucarianThemeTypographyProfile
            RequireReferenceTypography()
        {
            DeucarianThemeTypographyProfile typography =
                Resources.Load<DeucarianThemeTypographyProfile>(
                    TypographyResourcePath);
            if (typography == null || typography.ResolvedFontAsset == null)
            {
                throw new InvalidOperationException(
                    "The package-owned viewer reference typography resource " +
                    "is missing or has no font asset.");
            }

            return typography;
        }

        private static DeucarianThemeSurfaceProfile CreateSurfaceProfile(
            string profileId)
        {
            for (int i = 0;
                 i < DeucarianThemePresentationProfilePresets
                     .BuiltinSurfaces.Count;
                 i++)
            {
                DeucarianThemeSurfaceProfilePreset preset =
                    DeucarianThemePresentationProfilePresets
                        .BuiltinSurfaces[i];
                if (!string.Equals(
                        preset.Id,
                        profileId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                DeucarianThemeSurfaceProfile profile =
                    CreateRuntimeAsset<DeucarianThemeSurfaceProfile>(
                        preset.DisplayName);
                preset.Configure(profile);
                return profile;
            }

            throw new InvalidOperationException(
                "Viewer reference surface profile was not found: " +
                profileId);
        }

        private static DeucarianThemeShapeProfile CreateShapeProfile(
            string profileId)
        {
            for (int i = 0;
                 i < DeucarianThemePresentationProfilePresets
                     .BuiltinShapes.Count;
                 i++)
            {
                DeucarianThemeShapeProfilePreset preset =
                    DeucarianThemePresentationProfilePresets
                        .BuiltinShapes[i];
                if (!string.Equals(
                        preset.Id,
                        profileId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                DeucarianThemeShapeProfile profile =
                    CreateRuntimeAsset<DeucarianThemeShapeProfile>(
                        preset.DisplayName);
                preset.Configure(profile);
                return profile;
            }

            throw new InvalidOperationException(
                "Viewer reference shape profile was not found: " +
                profileId);
        }

        private static DeucarianThemeStrokeProfile CreateStrokeProfile(
            string profileId)
        {
            for (int i = 0;
                 i < DeucarianThemePresentationProfilePresets
                     .BuiltinStrokes.Count;
                 i++)
            {
                DeucarianThemeStrokeProfilePreset preset =
                    DeucarianThemePresentationProfilePresets
                        .BuiltinStrokes[i];
                if (!string.Equals(
                        preset.Id,
                        profileId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                DeucarianThemeStrokeProfile profile =
                    CreateRuntimeAsset<DeucarianThemeStrokeProfile>(
                        preset.DisplayName);
                preset.Configure(profile);
                return profile;
            }

            throw new InvalidOperationException(
                "Viewer reference stroke profile was not found: " +
                profileId);
        }

        private static bool IsAlive(
            DeucarianViewerReferenceThemeProfile profile)
        {
            return profile != null &&
                   profile.RoleLibrary != null &&
                   profile.LightPalette != null &&
                   profile.DarkPalette != null &&
                   profile.VisualStyle != null &&
                   profile.VisualStyle.IsComposed &&
                   profile.VisualStyle.TypographyProfile != null &&
                   profile.VisualStyle.TypographyProfile.ResolvedFontAsset != null &&
                   profile.LightTheme != null &&
                   profile.DarkTheme != null &&
                   profile.ThemeFamily != null;
        }

        private readonly struct SemanticColorDefinition
        {
            public SemanticColorDefinition(
                string roleId,
                string displayName,
                string category,
                string description,
                Color lightColor,
                Color darkColor)
            {
                RoleId = roleId;
                DisplayName = displayName;
                Category = category;
                Description = description;
                LightColor = lightColor;
                DarkColor = darkColor;
            }

            public string RoleId { get; }
            public string DisplayName { get; }
            public string Category { get; }
            public string Description { get; }
            public Color LightColor { get; }
            public Color DarkColor { get; }
        }
    }
}
