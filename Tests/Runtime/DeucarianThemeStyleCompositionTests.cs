using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianThemeStyleCompositionTests
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
        public void InlineStyleIsReportedAsLegacy()
        {
            DeucarianThemeStyle style = Create<DeucarianThemeStyle>();

            Assert.AreEqual(DeucarianThemeStyleCompositionKind.LegacyInline, style.CompositionKind);
            Assert.IsFalse(style.IsComposed);
            Assert.IsFalse(style.IsCustomStyle);
            Assert.IsFalse(style.IsVariant);
        }

        [Test]
        public void CompleteCompositionDistinguishesPresetAndCustomStyle()
        {
            DeucarianThemeStyle style = Create<DeucarianThemeStyle>();
            DeucarianThemeSurfaceProfile surface = Create<DeucarianThemeSurfaceProfile>();
            DeucarianThemeShapeProfile shape = Create<DeucarianThemeShapeProfile>();
            DeucarianThemeStrokeProfile stroke = Create<DeucarianThemeStrokeProfile>();

            style.SetComposition(surface, shape, stroke, DeucarianThemeDensity.Standard);

            Assert.AreEqual(DeucarianThemeStyleCompositionKind.CompletePreset, style.CompositionKind);
            Assert.IsFalse(style.IsCustomStyle);

            style.SetCustomStyleMetadata(
                "deucarian.style.custom-test",
                "Custom Test",
                "Project-authored style.");

            Assert.AreEqual(DeucarianThemeStyleCompositionKind.CompleteCustom, style.CompositionKind);
            Assert.IsTrue(style.IsCustomStyle);
            Assert.IsTrue(style.IsVariant, "The legacy alias must remain source compatible.");
            Assert.AreEqual("deucarian.style.custom-test", style.StyleId);
            Assert.AreEqual("Custom Test", style.DisplayName);
        }

        [Test]
        public void TypographyIsOptionalAndOldCompositionOverloadPreservesIt()
        {
            DeucarianThemeStyle style = Create<DeucarianThemeStyle>();
            DeucarianThemeTypographyProfile typography = Create<DeucarianThemeTypographyProfile>();
            DeucarianThemeSurfaceProfile surface = Create<DeucarianThemeSurfaceProfile>();
            DeucarianThemeShapeProfile shape = Create<DeucarianThemeShapeProfile>();
            DeucarianThemeStrokeProfile stroke = Create<DeucarianThemeStrokeProfile>();

            style.SetComposition(
                surface,
                shape,
                stroke,
                DeucarianThemeDensity.Standard,
                typography);
            Assert.IsTrue(style.IsComposed);
            Assert.AreSame(typography, style.TypographyProfile);
            Assert.IsTrue(style.UsesComponentAsset(typography));

            style.SetComposition(surface, shape, stroke, DeucarianThemeDensity.Compact);
            Assert.AreSame(
                typography,
                style.TypographyProfile,
                "The retained four-axis overload must not erase an existing optional typography profile.");
        }

        [Test]
        public void TypographyProfileSanitizesMetricsAndResolvesTmpDefault()
        {
            DeucarianThemeTypographyProfile profile = Create<DeucarianThemeTypographyProfile>();
            profile.Configure(
                null,
                new DeucarianThemeTextStyle(-20f, FontStyles.Bold, 500f, -500f),
                DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Body),
                DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Caption));

            Assert.AreEqual(1f, profile.Title.FontSize);
            Assert.AreEqual(100f, profile.Title.CharacterSpacing);
            Assert.AreEqual(-100f, profile.Title.LineSpacing);
            Assert.AreSame(
                DeucarianThemeTypographyProfile.ProjectDefaultFontAsset,
                profile.ResolvedFontAsset);
            Assert.AreEqual(20f, DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Title).FontSize);
            Assert.AreEqual(14f, DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Body).FontSize);
            Assert.AreEqual(11f, DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Caption).FontSize);
        }

        [Test]
        public void AnyMissingCompositionAxisIsReportedAsIncomplete()
        {
            DeucarianThemeStyle style = Create<DeucarianThemeStyle>();
            DeucarianThemeSurfaceProfile surface = Create<DeucarianThemeSurfaceProfile>();

            style.SetComposition(
                surface,
                null,
                null,
                DeucarianThemeDensity.Compact,
                true);

            Assert.AreEqual(DeucarianThemeStyleCompositionKind.Incomplete, style.CompositionKind);
            Assert.IsFalse(style.IsComposed);
            Assert.IsTrue(style.IsCustomStyle);
        }

        [Test]
        public void LegacyVariantMetadataApiMarksACompleteStyleAsCustom()
        {
            DeucarianThemeStyle style = Create<DeucarianThemeStyle>();
            style.SetComposition(
                Create<DeucarianThemeSurfaceProfile>(),
                Create<DeucarianThemeShapeProfile>(),
                Create<DeucarianThemeStrokeProfile>(),
                DeucarianThemeDensity.Comfortable);

            style.SetVariantMetadata(
                "deucarian.style.legacy-api",
                "Legacy API",
                "Created through the retained API.");

            Assert.IsTrue(style.IsCustomStyle);
            Assert.IsTrue(style.IsVariant);
            Assert.AreEqual(DeucarianThemeStyleCompositionKind.CompleteCustom, style.CompositionKind);
        }

        [Test]
        public void AssetChangeBatchSuppressesNotificationsUntilDisposed()
        {
            DeucarianThemeStyle style = Create<DeucarianThemeStyle>();
            int notificationCount = 0;
            Object notifiedAsset = null;
            System.Action<Object> handler = asset =>
            {
                notificationCount++;
                notifiedAsset = asset;
            };
            DeucarianThemeAssetChangeBus.AssetChanged += handler;

            try
            {
                using (DeucarianThemeAssetChangeBus.BeginBatch(style))
                {
                    style.SetCustomStyleMetadata(
                        "deucarian.style.batched",
                        "Batched",
                        "Batched custom style.");
                    style.SetComposition(
                        Create<DeucarianThemeSurfaceProfile>(),
                        Create<DeucarianThemeShapeProfile>(),
                        Create<DeucarianThemeStrokeProfile>(),
                        DeucarianThemeDensity.Standard,
                        true);

                    Assert.AreEqual(0, notificationCount);
                    Assert.IsNull(notifiedAsset);
                }

                Assert.AreEqual(1, notificationCount);
                Assert.AreSame(style, notifiedAsset);
            }
            finally
            {
                DeucarianThemeAssetChangeBus.AssetChanged -= handler;
            }
        }

        [Test]
        public void ThrowingAssetChangeSubscriberDoesNotEscapeOrBlockLaterSubscribers()
        {
            DeucarianThemeStyle style = Create<DeucarianThemeStyle>();
            int throwingSubscriberCount = 0;
            int laterSubscriberCount = 0;
            System.Action<Object> throwingHandler = _ =>
            {
                throwingSubscriberCount++;
                throw new System.InvalidOperationException("Intentional subscriber failure.");
            };
            System.Action<Object> laterHandler = _ => laterSubscriberCount++;
            DeucarianThemeAssetChangeBus.AssetChanged += throwingHandler;
            DeucarianThemeAssetChangeBus.AssetChanged += laterHandler;

            try
            {
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("theme asset change subscriber failed", RegexOptions.IgnoreCase));
                LogAssert.Expect(
                    LogType.Exception,
                    new Regex("Intentional subscriber failure", RegexOptions.IgnoreCase));

                Assert.DoesNotThrow(() => DeucarianThemeAssetChangeBus.NotifyChanged(style));

                Assert.AreEqual(1, throwingSubscriberCount);
                Assert.AreEqual(1, laterSubscriberCount);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                DeucarianThemeAssetChangeBus.AssetChanged -= throwingHandler;
                DeucarianThemeAssetChangeBus.AssetChanged -= laterHandler;
            }
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(instance);
            return instance;
        }
    }
}
