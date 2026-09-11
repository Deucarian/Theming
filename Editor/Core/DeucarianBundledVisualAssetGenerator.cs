using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>Maintainer-only generation of committed assets from the canonical brand and style presets.</summary>
    public static class DeucarianBundledVisualAssetGenerator
    {
        public const string Root = "Packages/com.deucarian.theming/Runtime/Resources/Deucarian/Theming/Visual/Defaults";

        public static void Generate()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DeucarianTheme).Assembly);
            if (package == null || (package.source != PackageSource.Local && package.source != PackageSource.Embedded))
                throw new InvalidOperationException("Generate bundled assets from a local Theming checkout, never an installed Git or registry cache.");

            DeucarianThemeAssetStore.EnsureFolder(Root + "/Roles");
            var library = Asset<DeucarianColorRoleLibrary>("DefaultColorRoleLibrary.asset");
            var light = Palette("DefaultLightColorPalette.asset", library, DeucarianThemeMode.Light);
            var dark = Palette("DefaultDarkColorPalette.asset", library, DeucarianThemeMode.Dark);
            var lightRoles = DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Light);
            var darkRoles = DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Dark);
            for (int i = 0; i < darkRoles.Count; i++)
            {
                var definition = darkRoles[i];
                if (lightRoles[i].Id != definition.Id) throw new InvalidOperationException("Paired brand roles must have matching IDs.");
                var role = Asset<DeucarianColorRole>("Roles/" + definition.Id + ".asset");
                role.Configure(definition.Id, definition.DisplayName, definition.Category, definition.Description,
                    lightRoles[i].DefaultColor, definition.DefaultColor, true);
                library.AddRole(role);
                light.SetColor(role, lightRoles[i].DefaultColor);
                dark.SetColor(role, definition.DefaultColor);
                EditorUtility.SetDirty(role);
            }
            library.SortRolesByCategoryAndName();
            var surfaces = DeucarianBuiltinThemeStyleAssets.CreateBuiltinSurfaceProfiles(Root + "/Surfaces", false);
            var shapes = DeucarianBuiltinThemeStyleAssets.CreateBuiltinShapeProfiles(Root + "/Shapes", false);
            var strokes = DeucarianBuiltinThemeStyleAssets.CreateBuiltinStrokeProfiles(Root + "/Strokes", false);
            var style = Asset<DeucarianThemeStyle>("DefaultVisualStyle.asset");
            if (!DeucarianThemeStylePresets.TryGetBuiltinStyle(DeucarianThemeStyleIds.FrostedGlass, out var preset))
                throw new InvalidOperationException("The default visual style preset is missing.");
            preset.Configure(style);
            style.SetComposition(
                DeucarianBuiltinThemeStyleAssets.FindSurfaceProfile(surfaces, preset.SurfaceProfileId),
                DeucarianBuiltinThemeStyleAssets.FindShapeProfile(shapes, preset.ShapeProfileId),
                DeucarianBuiltinThemeStyleAssets.FindStrokeProfile(strokes, preset.StrokeProfileId),
                preset.Density, DeucarianBundledTypographyAssets.InterProfile);
            var audio = DeucarianAudioDefaults.LoadPaletteSet();
            if (audio == null) throw new InvalidOperationException("Bundled audio defaults must exist before generating visual defaults.");
            var lightTheme = Theme("DefaultLightTheme.asset", light, style, audio, "Light");
            var darkTheme = Theme("DefaultDarkTheme.asset", dark, style, audio, "Dark");
            var family = Asset<DeucarianThemeFamily>("DefaultThemeFamily.asset");
            family.Configure("deucarian.theme-family.default", "Deucarian Default", lightTheme, darkTheme);
            foreach (var asset in new UnityEngine.Object[] { library, light, dark, style, lightTheme, darkTheme, family })
                EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static T Asset<T>(string relativePath) where T : ScriptableObject =>
            DeucarianThemeAssetStore.LoadOrCreateAsset(Root + "/" + relativePath,
                ScriptableObject.CreateInstance<T>, false, out _);

        private static DeucarianColorPalette Palette(string path, DeucarianColorRoleLibrary library, DeucarianThemeMode mode)
        {
            var palette = Asset<DeucarianColorPalette>(path);
            palette.Configure("deucarian.palette.default." + mode.ToString().ToLowerInvariant(), "Deucarian Default " + mode, library, mode);
            palette.ClearEntries();
            return palette;
        }

        private static DeucarianTheme Theme(string path, DeucarianColorPalette palette, DeucarianThemeStyle style,
            DeucarianAudioPaletteSet audio, string mode)
        {
            var theme = Asset<DeucarianTheme>(path);
            theme.Configure("deucarian.theme.default." + mode.ToLowerInvariant(), "Deucarian Default " + mode,
                palette, style, audio.DefaultPalette, audio);
            return theme;
        }
    }
}
