using Deucarian.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeStyleInspectorPresentation
    {
        internal const string SurfaceLabel = "Surface";
        internal const string CornersLabel = "Corners";
        internal const string BorderLabel = "Border";
        internal const string SizeLabel = "Size";
        internal const string TypographyLabel = "Typography";
        internal const string LegacyClassificationPropertyName = "surfaceTreatment";

        private static readonly string[] CompositionProperties =
        {
            "surfaceProfile",
            "shapeProfile",
            "strokeProfile",
            "density",
            "typographyProfile"
        };

        private static readonly string[] LegacyProperties =
        {
            "styleId",
            "displayName",
            "description",
            "darkSurfaceTint",
            "lightSurfaceTint",
            "surfaceTintStrength",
            "surfaceAlphaMultiplier",
            "minimumSurfaceAlpha",
            "maximumSurfaceAlpha",
            "borderTint",
            "borderTintStrength",
            "borderAlpha",
            "borderWidth",
            "cornerRadius",
            "useGeneratedNoiseTexture",
            "textureTint",
            "generatedTextureSize",
            "generatedTextureBlurRadius",
            "generatedTextureBlurStrength"
        };

        internal static IReadOnlyList<string> CompositionPropertyNames => CompositionProperties;
        internal static IReadOnlyList<string> LegacyPropertyNames => LegacyProperties;

        internal static bool IsCompositionEditable(DeucarianThemeStyleCompositionKind kind)
        {
            return kind == DeucarianThemeStyleCompositionKind.CompleteCustom;
        }

        internal static bool ShowsLegacyCompatibility(DeucarianThemeStyleCompositionKind kind)
        {
            return kind == DeucarianThemeStyleCompositionKind.LegacyInline;
        }

        internal static IReadOnlyList<string> GetMissingComponentLabels(DeucarianThemeStyle style)
        {
            List<string> missing = new List<string>(4);
            if (style == null || style.SurfaceProfile == null)
            {
                missing.Add(SurfaceLabel);
            }

            if (style == null || style.ShapeProfile == null)
            {
                missing.Add(CornersLabel);
            }

            if (style == null || style.StrokeProfile == null)
            {
                missing.Add(BorderLabel);
            }

            if (style == null || style.Density == DeucarianThemeDensity.Unspecified)
            {
                missing.Add(SizeLabel);
            }

            return missing;
        }
    }

    internal static class DeucarianThemeSurfaceProfileInspectorPresentation
    {
        internal const string ClassificationPropertyName = "surfaceTreatment";

        private static readonly string[] EditableEffectProperties =
        {
            "darkSurfaceTint",
            "lightSurfaceTint",
            "surfaceTintStrength",
            "surfaceAlphaMultiplier",
            "minimumSurfaceAlpha",
            "maximumSurfaceAlpha",
            "useGeneratedNoiseTexture",
            "textureTint",
            "generatedTextureSize",
            "generatedTextureBlurRadius",
            "generatedTextureBlurStrength"
        };

        internal static IReadOnlyList<string> EditableEffectPropertyNames => EditableEffectProperties;
    }

    [CustomEditor(typeof(DeucarianThemeStyle))]
    public sealed class DeucarianThemeStyleEditor : UnityEditor.Editor
    {
        private bool legacyCompatibilityExpanded;
        public override VisualElement CreateInspectorGUI()
        {
            var style = (DeucarianThemeStyle)target;
            var root = DeucarianEditorInspector.CreateToolkit();
            var title = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-section-title");
            var description = DeucarianEditorWorkspaceControls.Label(string.Empty, "dw-muted");
            root.Add(title); root.Add(description);
            var body = new VisualElement(); root.Add(body);
            DeucarianThemeStyleCompositionKind? renderedKind = null;
            DeucarianEditorInspector.Observe(root, serializedObject, () =>
            {
                if (style == null) return;
                title.text = string.IsNullOrWhiteSpace(style.DisplayName) ? style.name : style.DisplayName;
                description.text = style.Description;
                if (renderedKind == style.CompositionKind)
                {
                    if (style.CompositionKind == DeucarianThemeStyleCompositionKind.Incomplete && body.Q<HelpBox>() is HelpBox warning)
                        warning.text = "Choose: " + string.Join(", ", DeucarianThemeStyleInspectorPresentation.GetMissingComponentLabels(style)) + ".";
                    return;
                }
                renderedKind = style.CompositionKind;
                body.Unbind(); body.Clear();
                if (style.CompositionKind == DeucarianThemeStyleCompositionKind.LegacyInline)
                {
                    body.Add(DeucarianEditorWorkspaceControls.Label("Legacy inline style. New styles use reusable presentation profiles.", "dw-muted"));
                    var legacy = new Foldout { text = "Legacy compatibility", value = legacyCompatibilityExpanded };
                    legacy.AddToClassList("dw-foldout"); legacy.RegisterValueChangedCallback(evt => legacyCompatibilityExpanded = evt.newValue);
                    body.Add(legacy);
                    DeucarianEditorInspector.Property(legacy, serializedObject,
                        DeucarianThemeStyleInspectorPresentation.LegacyClassificationPropertyName, "Classification").SetEnabled(false);
                    foreach (string name in DeucarianThemeStyleInspectorPresentation.LegacyPropertyNames)
                        DeucarianEditorInspector.Property(legacy, serializedObject, name);
                    return;
                }
                bool editable = DeucarianThemeStyleInspectorPresentation.IsCompositionEditable(style.CompositionKind);
                if (style.CompositionKind == DeucarianThemeStyleCompositionKind.Incomplete)
                    body.Add(new HelpBox("Choose: " + string.Join(", ", DeucarianThemeStyleInspectorPresentation.GetMissingComponentLabels(style)) + ".", HelpBoxMessageType.Error));
                else if (!editable)
                    body.Add(DeucarianEditorWorkspaceControls.Label("Curated preset. Customize a copy to keep the preset stable.", "dw-muted"));
                var composition = new VisualElement(); body.Add(composition);
                string[] labels = { "Surface", "Corners", "Border", "Size", "Typography" };
                var paths = DeucarianThemeStyleInspectorPresentation.CompositionPropertyNames;
                for (int i = 0; i < paths.Count; i++) DeucarianEditorInspector.Property(composition, serializedObject, paths[i], labels[i]);
                composition.SetEnabled(editable);
                if (!editable) body.Add(DeucarianEditorWorkspaceControls.Button(
                    style.CompositionKind == DeucarianThemeStyleCompositionKind.Incomplete ? "Complete in Theme Manager" : "Customize in Theme Manager",
                    () => DeucarianThemeManagerWindow.OpenStyleComposer(style)));
            });
            return root;
        }
    }

    [CustomEditor(typeof(DeucarianThemeTypographyProfile))]
    public sealed class DeucarianThemeTypographyProfileEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("Typography");
            DeucarianEditorInspector.Property(root, serializedObject, "fontAsset", "TMP font asset");
            foreach (string path in new[] { "title", "body", "caption", "displayName" })
                DeucarianEditorInspector.Property(root, serializedObject, path);
            return root;
        }
    }

    [CustomEditor(typeof(DeucarianThemeSurfaceProfile))]
    public sealed class DeucarianThemeSurfaceProfileEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("Surface");
            DeucarianEditorInspector.Property(root, serializedObject,
                DeucarianThemeSurfaceProfileInspectorPresentation.ClassificationPropertyName, "Classification").SetEnabled(false);
            root.Add(DeucarianEditorWorkspaceControls.Label("Color and transparency", "dw-section-title"));
            string[] paths = { "darkSurfaceTint", "lightSurfaceTint", "surfaceTintStrength", "surfaceAlphaMultiplier", "minimumSurfaceAlpha", "maximumSurfaceAlpha" };
            string[] labels = { "Dark surface tint", "Light surface tint", "Tint blend", "Opacity multiplier", "Minimum opacity", "Maximum opacity" };
            for (int i = 0; i < paths.Length; i++) DeucarianEditorInspector.Property(root, serializedObject, paths[i], labels[i]);
            root.Add(DeucarianEditorWorkspaceControls.Label("Texture", "dw-section-title"));
            DeucarianEditorInspector.Property(root, serializedObject, "useGeneratedNoiseTexture", "Generated texture");
            var texture = new VisualElement(); root.Add(texture);
            foreach (string path in new[] { "textureTint", "generatedTextureSize", "generatedTextureBlurRadius", "generatedTextureBlurStrength" })
                DeucarianEditorInspector.Property(texture, serializedObject, path);
            DeucarianEditorInspector.Observe(root, serializedObject, () =>
                texture.SetEnabled(serializedObject.FindProperty("useGeneratedNoiseTexture").boolValue));
            DeucarianThemeProfileInspectorGUI.AssetDetails(root, serializedObject, "profileId", "displayName", "description");
            return root;
        }
    }

    [CustomEditor(typeof(DeucarianThemeShapeProfile))]
    public sealed class DeucarianThemeShapeProfileEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("Corners");
            DeucarianEditorInspector.Property(root, serializedObject, "cornerRadius");
            DeucarianThemeProfileInspectorGUI.AssetDetails(root, serializedObject, "profileId", "displayName", "description");
            return root;
        }
    }

    [CustomEditor(typeof(DeucarianThemeStrokeProfile))]
    public sealed class DeucarianThemeStrokeProfileEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("Border");
            DeucarianEditorInspector.Property(root, serializedObject, "borderWidth", "Width");
            var tint = new VisualElement(); root.Add(tint);
            foreach (string path in new[] { "borderTint", "borderTintStrength", "borderAlpha" })
                DeucarianEditorInspector.Property(tint, serializedObject, path);
            var hint = DeucarianEditorWorkspaceControls.Label("Width is zero: this profile is borderless.", "dw-muted"); root.Add(hint);
            DeucarianEditorInspector.Observe(root, serializedObject, () =>
            {
                var width = serializedObject.FindProperty("borderWidth");
                bool borderless = !width.hasMultipleDifferentValues && width.floatValue <= 0;
                tint.SetEnabled(!borderless); DeucarianEditorWorkspaceControls.Show(hint, borderless);
            });
            DeucarianThemeProfileInspectorGUI.AssetDetails(root, serializedObject, "profileId", "displayName", "description");
            return root;
        }
    }

    internal static class DeucarianThemeProfileInspectorGUI
    {
        internal static void AssetDetails(VisualElement root, SerializedObject serialized, params string[] paths)
        {
            var details = new Foldout { text = "Asset details", value = false }; details.AddToClassList("dw-foldout"); root.Add(details);
            foreach (string path in paths) DeucarianEditorInspector.Property(details, serialized, path);
        }
    }
}
