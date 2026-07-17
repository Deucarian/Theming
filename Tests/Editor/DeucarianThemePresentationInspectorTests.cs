using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemePresentationInspectorTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void StyleInspectorUsesFourRequiredAxesAndOptionalTypography()
        {
            CollectionAssert.AreEqual(
                new[] { "surfaceProfile", "shapeProfile", "strokeProfile", "density", "typographyProfile" },
                DeucarianThemeStyleInspectorPresentation.CompositionPropertyNames);
            Assert.AreEqual("Surface", DeucarianThemeStyleInspectorPresentation.SurfaceLabel);
            Assert.AreEqual("Corners", DeucarianThemeStyleInspectorPresentation.CornersLabel);
            Assert.AreEqual("Border", DeucarianThemeStyleInspectorPresentation.BorderLabel);
            Assert.AreEqual("Size", DeucarianThemeStyleInspectorPresentation.SizeLabel);
            Assert.AreEqual("Typography", DeucarianThemeStyleInspectorPresentation.TypographyLabel);

            CollectionAssert.DoesNotContain(
                DeucarianThemeStyleInspectorPresentation.CompositionPropertyNames.ToArray(),
                "borderWidth");
            CollectionAssert.DoesNotContain(
                DeucarianThemeStyleInspectorPresentation.CompositionPropertyNames.ToArray(),
                "cornerRadius");
        }

        [Test]
        public void SurfaceInspectorSeparatesReadOnlyClassificationFromEditableEffects()
        {
            Assert.AreEqual(
                "surfaceTreatment",
                DeucarianThemeSurfaceProfileInspectorPresentation.ClassificationPropertyName);
            CollectionAssert.DoesNotContain(
                DeucarianThemeSurfaceProfileInspectorPresentation.EditableEffectPropertyNames.ToArray(),
                "surfaceTreatment");
            CollectionAssert.Contains(
                DeucarianThemeSurfaceProfileInspectorPresentation.EditableEffectPropertyNames.ToArray(),
                "surfaceTintStrength");
            CollectionAssert.Contains(
                DeucarianThemeSurfaceProfileInspectorPresentation.EditableEffectPropertyNames.ToArray(),
                "surfaceAlphaMultiplier");
            CollectionAssert.Contains(
                DeucarianThemeSurfaceProfileInspectorPresentation.EditableEffectPropertyNames.ToArray(),
                "useGeneratedNoiseTexture");
        }

        [Test]
        public void OnlyCompleteCustomStylesHaveEditableComposition()
        {
            Assert.IsTrue(DeucarianThemeStyleInspectorPresentation.IsCompositionEditable(
                DeucarianThemeStyleCompositionKind.CompleteCustom));
            Assert.IsFalse(DeucarianThemeStyleInspectorPresentation.IsCompositionEditable(
                DeucarianThemeStyleCompositionKind.CompletePreset));
            Assert.IsFalse(DeucarianThemeStyleInspectorPresentation.IsCompositionEditable(
                DeucarianThemeStyleCompositionKind.Incomplete));
            Assert.IsFalse(DeucarianThemeStyleInspectorPresentation.IsCompositionEditable(
                DeucarianThemeStyleCompositionKind.LegacyInline));
        }

        [Test]
        public void LegacyFallbackFieldsAreConfinedToLegacyCompatibility()
        {
            Assert.IsTrue(DeucarianThemeStyleInspectorPresentation.ShowsLegacyCompatibility(
                DeucarianThemeStyleCompositionKind.LegacyInline));
            Assert.IsFalse(DeucarianThemeStyleInspectorPresentation.ShowsLegacyCompatibility(
                DeucarianThemeStyleCompositionKind.CompletePreset));
            Assert.IsFalse(DeucarianThemeStyleInspectorPresentation.ShowsLegacyCompatibility(
                DeucarianThemeStyleCompositionKind.CompleteCustom));
            Assert.IsFalse(DeucarianThemeStyleInspectorPresentation.ShowsLegacyCompatibility(
                DeucarianThemeStyleCompositionKind.Incomplete));

            CollectionAssert.Contains(
                DeucarianThemeStyleInspectorPresentation.LegacyPropertyNames.ToArray(),
                "borderWidth");
            CollectionAssert.Contains(
                DeucarianThemeStyleInspectorPresentation.LegacyPropertyNames.ToArray(),
                "cornerRadius");
            CollectionAssert.DoesNotContain(
                DeucarianThemeStyleInspectorPresentation.LegacyPropertyNames.ToArray(),
                "surfaceProfile");
            CollectionAssert.DoesNotContain(
                DeucarianThemeStyleInspectorPresentation.LegacyPropertyNames.ToArray(),
                "surfaceTreatment");
            Assert.AreEqual(
                "surfaceTreatment",
                DeucarianThemeStyleInspectorPresentation.LegacyClassificationPropertyName);
        }

        [Test]
        public void IncompleteStyleReportsExactMissingUserFacingAxes()
        {
            DeucarianThemeStyle style = Create<DeucarianThemeStyle>();
            DeucarianThemeSurfaceProfile surface = Create<DeucarianThemeSurfaceProfile>();
            DeucarianThemeStrokeProfile border = Create<DeucarianThemeStrokeProfile>();
            style.SetComposition(surface, null, border, DeucarianThemeDensity.Unspecified, true);

            CollectionAssert.AreEqual(
                new[] { "Corners", "Size" },
                DeucarianThemeStyleInspectorPresentation.GetMissingComponentLabels(style));
        }

        [Test]
        public void PresentationAssetsUseFocusedCustomInspectors()
        {
            AssertEditorType<DeucarianThemeStyle, DeucarianThemeStyleEditor>();
            AssertEditorType<DeucarianThemeSurfaceProfile, DeucarianThemeSurfaceProfileEditor>();
            AssertEditorType<DeucarianThemeShapeProfile, DeucarianThemeShapeProfileEditor>();
            AssertEditorType<DeucarianThemeStrokeProfile, DeucarianThemeStrokeProfileEditor>();
            AssertEditorType<DeucarianThemeTypographyProfile, DeucarianThemeTypographyProfileEditor>();
        }

        private void AssertEditorType<TAsset, TEditor>()
            where TAsset : ScriptableObject
            where TEditor : UnityEditor.Editor
        {
            TAsset asset = Create<TAsset>();
            UnityEditor.Editor inspector = UnityEditor.Editor.CreateEditor(asset);
            createdObjects.Add(inspector);
            Assert.IsInstanceOf<TEditor>(inspector);
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(instance);
            return instance;
        }
    }
}
