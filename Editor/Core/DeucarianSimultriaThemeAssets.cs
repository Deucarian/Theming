using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BuiltinRoleDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.BuiltinRoleDefinition;
using ThemeFamilyPresetDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.ThemeFamilyPresetDefinition;

namespace Deucarian.Theming.Editor
{
    /// <summary>Creates editable copies of the authored Simultria Design & Sales / Realisation & Progress palettes.</summary>
    public static class DeucarianSimultriaThemeAssets
    {
        public const string DefaultRoot = "Assets/Deucarian/Theming/Simultria";
        public static DeucarianDefaultThemeAssets CreateDesignAndSales(string rootFolder = DefaultRoot + "/DS") =>
            Create(rootFolder, "ds", "Design & Sales", DSLight(), DSDark());
        public static DeucarianDefaultThemeAssets CreateRealisationAndProgress(string rootFolder = DefaultRoot + "/RP") =>
            Create(rootFolder, "rp", "Realisation & Progress", RPLight(), RPDark());

        private static DeucarianDefaultThemeAssets Create(string rootFolder, string id, string label,
            Dictionary<string, Color> light, Dictionary<string, Color> dark)
        {
            string prefix = "Simultria " + id.ToUpperInvariant();
            return DeucarianThemeFamilyAssetCreation.CreateThemeFamilyAssets(rootFolder, false,
                Definitions(light, false), Definitions(dark, id == "ds"),
                new ThemeFamilyPresetDefinition(
                    PreserveExistingPath(rootFolder, "ColorRoles.asset", prefix + " Color Roles.asset"),
                    PreserveExistingPath(rootFolder, "LightPalette.asset", prefix + " Light Palette.asset"),
                    PreserveExistingPath(rootFolder, "DarkPalette.asset", prefix + " Dark Palette.asset"),
                    PreserveExistingPath(rootFolder, "LightTheme.asset", prefix + " Light Theme.asset"),
                    PreserveExistingPath(rootFolder, "DarkTheme.asset", prefix + " Dark Theme.asset"),
                    PreserveExistingPath(rootFolder, "ThemeFamily.asset", prefix + ".asset"),
                    "simultria.palette." + id + ".light", "Simultria " + label + " Light",
                    "simultria.palette." + id + ".dark", "Simultria " + label + " Dark",
                    "simultria.theme." + id + ".light", "Simultria " + label + " Light",
                    "simultria.theme." + id + ".dark", "Simultria " + label + " Dark",
                    "simultria.theme-family." + id, "Simultria " + label));
        }

        private static string PreserveExistingPath(string rootFolder, string previousName, string descriptiveName) =>
            AssetDatabase.LoadMainAssetAtPath(DeucarianThemeAssetNaming.NormalizeAssetPath(rootFolder).TrimEnd('/') + "/" + previousName) != null
                ? previousName : descriptiveName;

        private static IReadOnlyList<BuiltinRoleDefinition> Definitions(Dictionary<string, Color> colors, bool holoCompatible)
        {
            var result = new List<BuiltinRoleDefinition>();
            foreach (var role in DeucarianBuiltinThemeRolePresets.CreateMinimalDefaultRoleDefinitions())
                result.Add(new BuiltinRoleDefinition(role.Id, role.DisplayName, role.Category,
                    "Authored Simultria palette. See Documentation~/SIMULTRIA_PALETTES.md.", colors[role.Id]));
            DeucarianSimultriaControlColors.Add(result, colors, holoCompatible);
            return result;
        }

        private static Dictionary<string, Color> RPLight() => new Dictionary<string, Color>
        {
            { "deucarian.accent", new Color(0.5254902f, 0.6039216f, 0.41568628f, 0.5019608f) },
            { "deucarian.background", new Color(0.9529412f, 0.95686275f, 0.9647059f, 1f) },
            { "deucarian.primary", new Color(0.2509804f, 0.32156864f, 0.14117648f, 1f) },
            { "deucarian.secondary", new Color(0.5254902f, 0.6039216f, 0.41568628f, 1f) },
            { "deucarian.surface", new Color(1f, 1f, 1f, 1f) },
            { "deucarian.surface.raised", new Color(1f, 1f, 1f, 1f) },
            { "deucarian.error", new Color(0.6039216f, 0.24313726f, 0.21960784f, 1f) },
            { "deucarian.info", new Color(0.24705882f, 0.4f, 0.45882353f, 1f) },
            { "deucarian.success", new Color(0.2509804f, 0.32156864f, 0.14117648f, 1f) },
            { "deucarian.warning", new Color(0.6039216f, 0.41568628f, 0.14117648f, 1f) },
            { "deucarian.text.disabled", new Color(0.6509804f, 0.6784314f, 0.627451f, 1f) },
            { "deucarian.text.muted", new Color(0.43529412f, 0.4745098f, 0.39215687f, 1f) },
            { "deucarian.text.primary", new Color(0.2509804f, 0.32156864f, 0.14117648f, 1f) },
            { "deucarian.text.secondary", new Color(0.34901962f, 0.4f, 0.2901961f, 1f) },
            { "deucarian.ui.disabled", new Color(0.6509804f, 0.6784314f, 0.627451f, 1f) },
            { "deucarian.ui.focused", new Color(0.2509804f, 0.32156864f, 0.14117648f, 1f) },
            { "deucarian.ui.highlighted", new Color(0.5254902f, 0.6039216f, 0.41568628f, 0.2509804f) },
            { "deucarian.ui.normal", new Color(1f, 1f, 1f, 0f) },
            { "deucarian.ui.pressed", new Color(0.2509804f, 0.32156864f, 0.14117648f, 0.2f) },
            { "deucarian.ui.selected", new Color(0.5254902f, 0.6039216f, 0.41568628f, 0.5019608f) },
        };

        private static Dictionary<string, Color> DSLight() => new Dictionary<string, Color>
        {
            { "deucarian.accent", new Color(0.76862746f, 0.6313726f, 0.9764706f, 0.5019608f) },
            { "deucarian.background", new Color(0.9529412f, 0.94509804f, 0.96862745f, 1f) },
            { "deucarian.primary", new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f) },
            { "deucarian.secondary", new Color(0.49803922f, 0.32941177f, 0.7529412f, 1f) },
            { "deucarian.surface", new Color(1f, 1f, 1f, 1f) },
            { "deucarian.surface.raised", new Color(1f, 1f, 1f, 0.96862745f) },
            { "deucarian.error", new Color(0.6039216f, 0.24313726f, 0.21960784f, 1f) },
            { "deucarian.info", new Color(0.24705882f, 0.4f, 0.45882353f, 1f) },
            { "deucarian.success", new Color(0.18431373f, 0.44705883f, 0.34901962f, 1f) },
            { "deucarian.warning", new Color(0.6039216f, 0.41568628f, 0.14117648f, 1f) },
            { "deucarian.text.disabled", new Color(0.6666667f, 0.6431373f, 0.69411767f, 1f) },
            { "deucarian.text.muted", new Color(0.45490196f, 0.42352942f, 0.49019608f, 1f) },
            { "deucarian.text.primary", new Color(0.18039216f, 0.14509805f, 0.21960784f, 1f) },
            { "deucarian.text.secondary", new Color(0.33333334f, 0.29411766f, 0.38039216f, 1f) },
            { "deucarian.ui.disabled", new Color(0.6666667f, 0.6431373f, 0.69411767f, 1f) },
            { "deucarian.ui.focused", new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f) },
            { "deucarian.ui.highlighted", new Color(0.76862746f, 0.6313726f, 0.9764706f, 0.2509804f) },
            { "deucarian.ui.normal", new Color(1f, 1f, 1f, 0f) },
            { "deucarian.ui.pressed", new Color(0.3882353f, 0.25882354f, 0.5882353f, 0.2f) },
            { "deucarian.ui.selected", new Color(0.49803922f, 0.32941177f, 0.7529412f, 0.5019608f) },
        };

        private static Dictionary<string, Color> RPDark() => new Dictionary<string, Color>
        {
            { "deucarian.accent", new Color(0.6862745f, 0.77254903f, 0.5529412f, 0.5019608f) },
            { "deucarian.background", new Color(0.08235294f, 0.101960786f, 0.07058824f, 1f) },
            { "deucarian.primary", new Color(0.6862745f, 0.77254903f, 0.5529412f, 1f) },
            { "deucarian.secondary", new Color(0.5254902f, 0.6039216f, 0.41568628f, 1f) },
            { "deucarian.surface", new Color(0.11764706f, 0.14509805f, 0.101960786f, 0.9098039f) },
            { "deucarian.surface.raised", new Color(0.15294118f, 0.1882353f, 0.1254902f, 0.9411765f) },
            { "deucarian.error", new Color(0.8666667f, 0.5058824f, 0.47058824f, 1f) },
            { "deucarian.info", new Color(0.4627451f, 0.6509804f, 0.72156864f, 1f) },
            { "deucarian.success", new Color(0.6862745f, 0.77254903f, 0.5529412f, 1f) },
            { "deucarian.warning", new Color(0.84705883f, 0.68235296f, 0.38039216f, 1f) },
            { "deucarian.text.disabled", new Color(0.40784314f, 0.44313726f, 0.38039216f, 1f) },
            { "deucarian.text.muted", new Color(0.59607846f, 0.6431373f, 0.54901963f, 1f) },
            { "deucarian.text.primary", new Color(0.94509804f, 0.9607843f, 0.9254902f, 1f) },
            { "deucarian.text.secondary", new Color(0.7921569f, 0.83137256f, 0.7411765f, 1f) },
            { "deucarian.ui.disabled", new Color(0.40784314f, 0.44313726f, 0.38039216f, 1f) },
            { "deucarian.ui.focused", new Color(0.7254902f, 0.80784315f, 0.6f, 1f) },
            { "deucarian.ui.highlighted", new Color(0.5254902f, 0.6039216f, 0.41568628f, 0.2509804f) },
            { "deucarian.ui.normal", new Color(1f, 1f, 1f, 0f) },
            { "deucarian.ui.pressed", new Color(0.6862745f, 0.77254903f, 0.5529412f, 0.2f) },
            { "deucarian.ui.selected", new Color(0.5254902f, 0.6039216f, 0.41568628f, 0.5019608f) },
        };

        private static Dictionary<string, Color> DSDark() => new Dictionary<string, Color>
        {
            { "deucarian.background", new Color(0.22f, 0.23f, 0.23f, 1f) },
            { "deucarian.surface", new Color(0.22f, 0.23f, 0.23f, 1f) },
            { "deucarian.surface.raised", new Color(0.22f, 0.23f, 0.23f, 1f) },
            { "deucarian.primary", new Color(0.39200002f, 0.32200003f, 0.5f, 1f) },
            { "deucarian.secondary", new Color(0.47f, 0.39f, 0.6f, 1f) },
            { "deucarian.accent", new Color(0.7686275f, 0.6313726f, 0.97647065f, 1f) },
            { "deucarian.text.primary", new Color(1f, 1f, 1f, 1f) },
            { "deucarian.text.secondary", new Color(0.7568628f, 0.76470596f, 0.7686275f, 1f) },
            { "deucarian.text.muted", new Color(1f, 1f, 1f, 0.5019608f) },
            { "deucarian.text.disabled", new Color(0.7529412f, 0.7529412f, 0.7529412f, 0.47058824f) },
            { "deucarian.success", new Color(0.33f, 0.48f, 0.34f, 1f) },
            { "deucarian.warning", new Color(1f, 0.59f, 0f, 1f) },
            { "deucarian.error", new Color(0.62f, 0.17f, 0.27f, 1f) },
            { "deucarian.info", new Color(0.3f, 0.7f, 1f, 1f) },
            { "deucarian.ui.normal", new Color(0.22f, 0.23f, 0.23f, 1f) },
            { "deucarian.ui.highlighted", new Color(0.94f, 0.78f, 1f, 1f) },
            { "deucarian.ui.pressed", new Color(0.43120002f, 0.35420004f, 0.55f, 1f) },
            { "deucarian.ui.selected", new Color(0.58800003f, 0.48300004f, 0.75f, 1f) },
            { "deucarian.ui.disabled", new Color(0.8f, 0.8f, 0.8f, 1f) },
            { "deucarian.ui.focused", new Color(0.7686275f, 0.6313726f, 0.97647065f, 1f) },
        };

    }
}
