using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DeucarianThemeStyle style = (DeucarianThemeStyle)target;
            DrawStyleHeading(style);

            switch (style.CompositionKind)
            {
                case DeucarianThemeStyleCompositionKind.CompletePreset:
                    DrawPreset(style);
                    break;
                case DeucarianThemeStyleCompositionKind.CompleteCustom:
                    DrawCustomStyle();
                    break;
                case DeucarianThemeStyleCompositionKind.Incomplete:
                    DrawIncomplete(style);
                    break;
                default:
                    DrawLegacyStyle();
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawStyleHeading(DeucarianThemeStyle style)
        {
            string heading = string.IsNullOrWhiteSpace(style.DisplayName) ? style.name : style.DisplayName;
            EditorGUILayout.LabelField(heading, EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(style.Description))
            {
                EditorGUILayout.LabelField(style.Description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawPreset(DeucarianThemeStyle style)
        {
            EditorGUILayout.HelpBox(
                "Curated preset. Its reusable presentation parts are kept read-only so the preset remains stable.",
                MessageType.Info);
            DrawComposition(false);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Customize in Theme Manager"))
            {
                DeucarianThemeManagerWindow.OpenStyleComposer(style);
            }
        }

        private void DrawCustomStyle()
        {
            DrawComposition(true);
        }

        private void DrawIncomplete(DeucarianThemeStyle style)
        {
            IReadOnlyList<string> missing =
                DeucarianThemeStyleInspectorPresentation.GetMissingComponentLabels(style);
            EditorGUILayout.HelpBox(
                "This style is incomplete. Choose: " + string.Join(", ", missing) + ".",
                MessageType.Error);
            DrawComposition(false);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Complete in Theme Manager"))
            {
                DeucarianThemeManagerWindow.OpenStyleComposer(style);
            }
        }

        private void DrawLegacyStyle()
        {
            EditorGUILayout.HelpBox(
                "Legacy inline style. It remains supported, but new styles should use composed presentation profiles.",
                MessageType.Info);

            legacyCompatibilityExpanded = EditorGUILayout.Foldout(
                legacyCompatibilityExpanded,
                "Legacy Compatibility",
                true);
            if (!legacyCompatibilityExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(true))
            {
                SerializedProperty classification = serializedObject.FindProperty(
                    DeucarianThemeStyleInspectorPresentation.LegacyClassificationPropertyName);
                if (classification != null)
                {
                    EditorGUILayout.PropertyField(classification, new GUIContent("Classification"));
                }
            }

            IReadOnlyList<string> properties = DeucarianThemeStyleInspectorPresentation.LegacyPropertyNames;
            for (int i = 0; i < properties.Count; i++)
            {
                SerializedProperty property = serializedObject.FindProperty(properties[i]);
                if (property != null)
                {
                    EditorGUILayout.PropertyField(property);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawComposition(bool editable)
        {
            using (new EditorGUI.DisabledScope(!editable))
            {
                DrawProperty("surfaceProfile", DeucarianThemeStyleInspectorPresentation.SurfaceLabel);
                DrawProperty("shapeProfile", DeucarianThemeStyleInspectorPresentation.CornersLabel);
                DrawProperty("strokeProfile", DeucarianThemeStyleInspectorPresentation.BorderLabel);
                DrawProperty("density", DeucarianThemeStyleInspectorPresentation.SizeLabel);
                DrawProperty("typographyProfile", DeucarianThemeStyleInspectorPresentation.TypographyLabel);
            }
        }

        private void DrawProperty(string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }
    }

    [CustomEditor(typeof(DeucarianThemeTypographyProfile))]
    public sealed class DeucarianThemeTypographyProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Typography", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fontAsset"), new GUIContent("TMP Font Asset"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), new GUIContent("Title"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("body"), new GUIContent("Body"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("caption"), new GUIContent("Caption"), true);
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), new GUIContent("Display Name"));
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(DeucarianThemeSurfaceProfile))]
    public sealed class DeucarianThemeSurfaceProfileEditor : UnityEditor.Editor
    {
        private bool assetDetailsExpanded;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Surface", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(
                        DeucarianThemeSurfaceProfileInspectorPresentation.ClassificationPropertyName),
                    new GUIContent("Classification"));
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Color & Transparency", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("darkSurfaceTint"), new GUIContent("Dark Surface Tint"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lightSurfaceTint"), new GUIContent("Light Surface Tint"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceTintStrength"), new GUIContent("Tint Blend"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceAlphaMultiplier"), new GUIContent("Opacity Multiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minimumSurfaceAlpha"), new GUIContent("Minimum Opacity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumSurfaceAlpha"), new GUIContent("Maximum Opacity"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Texture", EditorStyles.boldLabel);
            SerializedProperty useTexture = serializedObject.FindProperty("useGeneratedNoiseTexture");
            EditorGUILayout.PropertyField(useTexture, new GUIContent("Generated Texture"));
            using (new EditorGUI.DisabledScope(!useTexture.boolValue))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("textureTint"), new GUIContent("Texture Tint"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("generatedTextureSize"), new GUIContent("Texture Size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("generatedTextureBlurRadius"), new GUIContent("Blur Radius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("generatedTextureBlurStrength"), new GUIContent("Blur Blend"));
            }

            if (!useTexture.boolValue)
            {
                EditorGUILayout.HelpBox("Texture settings are inactive while Generated Texture is off.", MessageType.None);
            }

            DeucarianThemeProfileInspectorGUI.DrawAssetDetails(
                ref assetDetailsExpanded,
                serializedObject,
                "profileId",
                "displayName",
                "description");
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(DeucarianThemeShapeProfile))]
    public sealed class DeucarianThemeShapeProfileEditor : UnityEditor.Editor
    {
        private bool assetDetailsExpanded;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Corners", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cornerRadius"), new GUIContent("Corner Radius"));
            DeucarianThemeProfileInspectorGUI.DrawAssetDetails(
                ref assetDetailsExpanded,
                serializedObject,
                "profileId",
                "displayName",
                "description");
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(DeucarianThemeStrokeProfile))]
    public sealed class DeucarianThemeStrokeProfileEditor : UnityEditor.Editor
    {
        private bool assetDetailsExpanded;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Border", EditorStyles.boldLabel);

            SerializedProperty width = serializedObject.FindProperty("borderWidth");
            EditorGUILayout.PropertyField(width, new GUIContent("Width"));
            bool borderless = !width.hasMultipleDifferentValues && width.floatValue <= 0f;
            using (new EditorGUI.DisabledScope(borderless))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("borderTint"), new GUIContent("Tint"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("borderTintStrength"), new GUIContent("Tint Blend"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("borderAlpha"), new GUIContent("Opacity"));
            }

            if (borderless)
            {
                EditorGUILayout.HelpBox("This profile is borderless. Tint and opacity are inactive while Width is 0.", MessageType.None);
            }

            DeucarianThemeProfileInspectorGUI.DrawAssetDetails(
                ref assetDetailsExpanded,
                serializedObject,
                "profileId",
                "displayName",
                "description");
            serializedObject.ApplyModifiedProperties();
        }
    }

    internal static class DeucarianThemeProfileInspectorGUI
    {
        internal static void DrawAssetDetails(
            ref bool expanded,
            SerializedObject inspectedObject,
            params string[] propertyNames)
        {
            EditorGUILayout.Space(6f);
            expanded = EditorGUILayout.Foldout(expanded, "Asset Details", true);
            if (!expanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < propertyNames.Length; i++)
            {
                SerializedProperty property = inspectedObject.FindProperty(propertyNames[i]);
                if (property != null)
                {
                    EditorGUILayout.PropertyField(property);
                }
            }

            EditorGUI.indentLevel--;
        }
    }

}
