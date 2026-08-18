using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianViewerReferenceThemePresetTests
    {
        private const float Tolerance = 0.000001f;

        private static readonly string[] ExpectedRoleIds =
        {
            DeucarianBuiltinColorRoleIds.Background,
            DeucarianBuiltinColorRoleIds.Surface,
            DeucarianBuiltinColorRoleIds.SurfaceRaised,
            DeucarianBuiltinColorRoleIds.Primary,
            DeucarianBuiltinColorRoleIds.Secondary,
            DeucarianBuiltinColorRoleIds.Accent,
            DeucarianBuiltinColorRoleIds.TextPrimary,
            DeucarianBuiltinColorRoleIds.TextSecondary,
            DeucarianBuiltinColorRoleIds.TextMuted,
            DeucarianBuiltinColorRoleIds.TextDisabled,
            DeucarianBuiltinColorRoleIds.Success,
            DeucarianBuiltinColorRoleIds.Warning,
            DeucarianBuiltinColorRoleIds.Error,
            DeucarianBuiltinColorRoleIds.Info,
            DeucarianBuiltinColorRoleIds.UiNormal,
            DeucarianBuiltinColorRoleIds.UiHighlighted,
            DeucarianBuiltinColorRoleIds.UiPressed,
            DeucarianBuiltinColorRoleIds.UiSelected,
            DeucarianBuiltinColorRoleIds.UiDisabled,
            DeucarianBuiltinColorRoleIds.UiFocused
        };

        [Test]
        public void ResolveReturnsOneCompleteConsumerNeutralRuntimeFamily()
        {
            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();
            DeucarianViewerReferenceThemeProfile resolvedAgain =
                DeucarianViewerReferenceThemePreset.Resolve();

            Assert.That(resolvedAgain, Is.SameAs(profile));
            Assert.That(profile.ThemeFamily.IsComplete, Is.True);
            Assert.That(
                profile.ThemeFamily.FamilyId,
                Is.EqualTo(DeucarianViewerReferenceThemePreset.FamilyId));
            Assert.That(
                profile.ThemeFamily.LightTheme,
                Is.SameAs(profile.LightTheme));
            Assert.That(
                profile.ThemeFamily.DarkTheme,
                Is.SameAs(profile.DarkTheme));
            Assert.That(profile.DefaultTheme, Is.SameAs(profile.DarkTheme));
            Assert.That(
                DeucarianViewerReferenceThemePreset.DefaultMode,
                Is.EqualTo(DeucarianThemeMode.Dark));
            Assert.That(
                profile.LightTheme.ColorPalette,
                Is.SameAs(profile.LightPalette));
            Assert.That(
                profile.DarkTheme.ColorPalette,
                Is.SameAs(profile.DarkPalette));
            Assert.That(
                profile.LightTheme.VisualStyle,
                Is.SameAs(profile.VisualStyle));
            Assert.That(
                profile.DarkTheme.VisualStyle,
                Is.SameAs(profile.VisualStyle));
            Assert.That(
                profile.VisualStyle.StyleId,
                Is.EqualTo(DeucarianThemeStyleIds.FrostedGlass));
            Assert.That(profile.VisualStyle.IsComposed, Is.True);
            Assert.That(
                profile.VisualStyle.Density,
                Is.EqualTo(DeucarianThemeDensity.Comfortable));
            Assert.That(profile.VisualStyle.SurfaceProfile, Is.Not.Null);
            Assert.That(profile.VisualStyle.ShapeProfile, Is.Not.Null);
            Assert.That(profile.VisualStyle.StrokeProfile, Is.Not.Null);
            Assert.That(profile.VisualStyle.TypographyProfile, Is.Not.Null);
            Assert.That(
                profile.VisualStyle.TypographyProfile,
                Is.SameAs(
                    Resources.Load<DeucarianThemeTypographyProfile>(
                        DeucarianViewerReferenceThemePreset
                            .TypographyResourcePath)));
            Assert.That(
                profile.VisualStyle.TypographyProfile.ResolvedFontAsset,
                Is.Not.Null);
            Assert.That(
                profile.VisualStyle.TypographyProfile
                    .ResolvedFontAsset.sourceFontFile,
                Is.Not.Null);
            Assert.That(profile.LightPalette.ThemeMode, Is.EqualTo(DeucarianThemeMode.Light));
            Assert.That(profile.DarkPalette.ThemeMode, Is.EqualTo(DeucarianThemeMode.Dark));
            Assert.That(profile.RoleLibrary.Roles.Count, Is.EqualTo(ExpectedRoleIds.Length));
            Assert.That(profile.LightPalette.Entries.Count, Is.EqualTo(ExpectedRoleIds.Length));
            Assert.That(profile.DarkPalette.Entries.Count, Is.EqualTo(ExpectedRoleIds.Length));
            AssertRuntimeAsset(profile.ThemeFamily);
            AssertRuntimeAsset(profile.LightTheme);
            AssertRuntimeAsset(profile.DarkTheme);
            AssertRuntimeAsset(profile.LightPalette);
            AssertRuntimeAsset(profile.DarkPalette);
            AssertRuntimeAsset(profile.VisualStyle);
            AssertRuntimeAsset(profile.VisualStyle.SurfaceProfile);
            AssertRuntimeAsset(profile.VisualStyle.ShapeProfile);
            AssertRuntimeAsset(profile.VisualStyle.StrokeProfile);
            AssertRuntimeAsset(profile.RoleLibrary);
        }

        [Test]
        public void BothVariantsResolveEveryViewerSemanticRole()
        {
            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();
            var actualRoleIds = new HashSet<string>();

            for (int i = 0; i < profile.RoleLibrary.Roles.Count; i++)
            {
                DeucarianColorRole role = profile.RoleLibrary.Roles[i];
                Assert.That(role, Is.Not.Null);
                Assert.That(role.IsCoreRole, Is.True, role.Id);
                Assert.That(role.Id, Does.StartWith("deucarian."));
                Assert.That(actualRoleIds.Add(role.Id), Is.True, role.Id);
                AssertRuntimeAsset(role);
            }

            CollectionAssert.AreEquivalent(ExpectedRoleIds, actualRoleIds);
            for (int i = 0; i < ExpectedRoleIds.Length; i++)
            {
                string roleId = ExpectedRoleIds[i];
                Assert.That(
                    profile.TryGetColor(
                        DeucarianThemeMode.Light,
                        roleId,
                        out Color lightColor),
                    Is.True,
                    roleId);
                Assert.That(
                    profile.TryGetColor(
                        DeucarianThemeMode.Dark,
                        roleId,
                        out Color darkColor),
                    Is.True,
                    roleId);
                Assert.That(lightColor, Is.Not.EqualTo(DeucarianColorPalette.MissingColor));
                Assert.That(darkColor, Is.Not.EqualTo(DeucarianColorPalette.MissingColor));
            }
        }

        [Test]
        public void DarkVariantMatchesCanonicalViewerChromeColors()
        {
            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();

            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.Background,
                new Color(0.07f, 0.08f, 0.09f, 0.88f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.Surface,
                new Color(0.11f, 0.14f, 0.18f, 0.88f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.SurfaceRaised,
                new Color(0.11f, 0.14f, 0.18f, 0.94f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.Primary,
                new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.Accent,
                new Color(0.76862746f, 0.6313726f, 0.9764706f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.TextPrimary,
                new Color(0.95f, 0.97f, 0.96f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.TextSecondary,
                new Color(0.82f, 0.9f, 0.86f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.TextMuted,
                new Color(0.78f, 0.82f, 0.8f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.UiHighlighted,
                new Color(0.76862746f, 0.6313726f, 0.9764706f, 0.35f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.UiPressed,
                new Color(0.49803922f, 0.32941177f, 0.7529412f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.UiSelected,
                new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Dark,
                DeucarianBuiltinColorRoleIds.Error,
                new Color(0.8666667f, 0.5058824f, 0.47058824f, 1f));
        }

        [Test]
        public void LightVariantRetainsTheSameReferenceInteractionIdentity()
        {
            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();

            AssertColor(
                profile,
                DeucarianThemeMode.Light,
                DeucarianBuiltinColorRoleIds.Background,
                new Color(0.9529412f, 0.94509804f, 0.96862745f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Light,
                DeucarianBuiltinColorRoleIds.Primary,
                new Color(0.3882353f, 0.25882354f, 0.5882353f, 1f));
            AssertColor(
                profile,
                DeucarianThemeMode.Light,
                DeucarianBuiltinColorRoleIds.UiSelected,
                new Color(0.49803922f, 0.32941177f, 0.7529412f, 0.5019608f));
            AssertColor(
                profile,
                DeucarianThemeMode.Light,
                DeucarianBuiltinColorRoleIds.TextPrimary,
                new Color(0.18039216f, 0.14509805f, 0.21960784f, 1f));
        }

        [Test]
        public void ProviderConsumesTheReferenceFamilyWithoutProductAdapters()
        {
            DeucarianViewerReferenceThemeProfile profile =
                DeucarianViewerReferenceThemePreset.Resolve();
            GameObject gameObject = new GameObject("Viewer Reference Theme Provider");
            try
            {
                DeucarianThemeProvider provider =
                    gameObject.AddComponent<DeucarianThemeProvider>();

                provider.SetThemeFamily(
                    profile.ThemeFamily,
                    DeucarianThemeMode.Dark);

                Assert.That(provider.CurrentThemeFamily, Is.SameAs(profile.ThemeFamily));
                Assert.That(provider.CurrentTheme, Is.SameAs(profile.DarkTheme));
                Assert.That(provider.CurrentStyle, Is.SameAs(profile.VisualStyle));

                provider.SetThemeMode(DeucarianThemeMode.Light);

                Assert.That(provider.CurrentTheme, Is.SameAs(profile.LightTheme));
                Assert.That(provider.CurrentStyle, Is.SameAs(profile.VisualStyle));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void AssertColor(
            DeucarianViewerReferenceThemeProfile profile,
            DeucarianThemeMode mode,
            string roleId,
            Color expected)
        {
            Assert.That(profile.TryGetColor(mode, roleId, out Color actual), Is.True, roleId);
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(Tolerance), roleId + " r");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(Tolerance), roleId + " g");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(Tolerance), roleId + " b");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(Tolerance), roleId + " a");
        }

        private static void AssertRuntimeAsset(Object asset)
        {
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
        }
    }
}
