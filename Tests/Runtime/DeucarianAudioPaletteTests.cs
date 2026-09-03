using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianAudioPaletteTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void AudioRoleNormalizesIdAndReportsInvalidUppercaseId()
        {
            DeucarianAudioRole role = CreateRole(" deucarian.test.audio ", null);

            Assert.AreEqual("deucarian.test.audio", role.Id);
            Assert.IsTrue(role.HasValidId);

            role.Configure(
                "Deucarian.Test.Audio",
                "Audio",
                DeucarianAudioRoleCategories.UI,
                string.Empty,
                DeucarianAudioCue.Empty,
                false);

            Assert.IsFalse(role.HasValidId);
            Assert.AreEqual("Role ID should use lowercase-friendly stable identifiers.", role.GetValidationWarning());
        }

        [Test]
        public void AudioRoleLibraryLookupAndDuplicateWarningsUseFirstRole()
        {
            DeucarianAudioRole first = CreateRole("deucarian.test.duplicate", null);
            DeucarianAudioRole second = CreateRole("deucarian.test.duplicate", null);
            DeucarianAudioRoleLibrary library = CreateAsset<DeucarianAudioRoleLibrary>();

            library.AddRole(first);
            library.AddRole(second);

            Assert.IsTrue(library.TryGetRoleById("deucarian.test.duplicate", out DeucarianAudioRole resolved));
            Assert.AreSame(first, resolved);
            CollectionAssert.Contains(library.GetDuplicateRoleIds(), "deucarian.test.duplicate");
            CollectionAssert.Contains(library.GetValidationWarnings(), "Duplicate role ID: deucarian.test.duplicate");
        }

        [Test]
        public void AudioPaletteLookupByRoleAndIdUsesExplicitCue()
        {
            AudioClip defaultClip = CreateClip("Default");
            AudioClip overrideClip = CreateClip("Override");
            DeucarianAudioRole role = CreateRole("deucarian.test.click", defaultClip);
            DeucarianAudioPalette palette = CreateAsset<DeucarianAudioPalette>();
            DeucarianAudioCue overrideCue = new DeucarianAudioCue(overrideClip, 0.45f);

            palette.SetCue(role, overrideCue, "Override");

            Assert.IsTrue(palette.TryGetCue(role, out DeucarianAudioCue roleCue));
            Assert.AreSame(overrideClip, roleCue.Clip);
            Assert.AreEqual(0.45f, roleCue.Volume);

            Assert.IsTrue(palette.TryGetCueById("deucarian.test.click", out DeucarianAudioCue idCue));
            Assert.AreSame(overrideClip, idCue.Clip);
        }

        [Test]
        public void AudioPaletteFallsBackToRoleDefaultCueByRoleAndLibraryId()
        {
            AudioClip defaultClip = CreateClip("Default");
            DeucarianAudioRole role = CreateRole("deucarian.test.default", defaultClip);
            DeucarianAudioRoleLibrary library = CreateAsset<DeucarianAudioRoleLibrary>();
            DeucarianAudioPalette palette = CreateAsset<DeucarianAudioPalette>();

            library.AddRole(role);
            palette.Configure("deucarian.audio-palette.test", "Audio", library);

            Assert.IsTrue(palette.TryGetCue(role, out DeucarianAudioCue roleCue));
            Assert.AreSame(defaultClip, roleCue.Clip);

            Assert.IsTrue(palette.TryGetCueById("deucarian.test.default", out DeucarianAudioCue idCue));
            Assert.AreSame(defaultClip, idCue.Clip);
        }

        [Test]
        public void MissingPaletteOrRoleReturnsFalseAndSafeEmptyCue()
        {
            DeucarianAudioPalette palette = CreateAsset<DeucarianAudioPalette>();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();

            Assert.IsFalse(palette.TryGetCue(null, out DeucarianAudioCue paletteCue));
            Assert.NotNull(paletteCue);
            Assert.IsNull(paletteCue.Clip);

            Assert.IsFalse(theme.TryGetAudioCue(null, out DeucarianAudioCue themeCue));
            Assert.NotNull(themeCue);
            Assert.IsNull(themeCue.Clip);
        }

        [Test]
        public void FirstDuplicateAudioEntryWins()
        {
            AudioClip firstClip = CreateClip("First");
            AudioClip secondClip = CreateClip("Second");
            DeucarianAudioRole role = CreateRole("deucarian.test.duplicate-entry", null);
            DeucarianAudioPalette palette = CreateAsset<DeucarianAudioPalette>();

            palette.AddEntry(role, new DeucarianAudioCue(firstClip));
            palette.AddEntry(role, new DeucarianAudioCue(secondClip));

            Assert.AreSame(firstClip, palette.GetCue(role).Clip);
            CollectionAssert.Contains(palette.GetDuplicateEntryRoleIds(), "deucarian.test.duplicate-entry");
        }

        [Test]
        public void ThemeDelegatesAudioCueLookupToPalette()
        {
            AudioClip clip = CreateClip("Theme Clip");
            DeucarianAudioRole role = CreateRole("deucarian.test.theme-audio", null);
            DeucarianAudioPalette palette = CreateAsset<DeucarianAudioPalette>();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();

            palette.SetCue(role, new DeucarianAudioCue(clip, 0.8f));
            theme.Configure("deucarian.theme.audio", "Audio Theme", null, null, palette);

            Assert.IsTrue(theme.TryGetAudioCue(role, out DeucarianAudioCue cue));
            Assert.AreSame(clip, cue.Clip);
        }

        [Test]
        public void ThemeProviderUsesActiveAudioPaletteLibraryAndRoles()
        {
            DeucarianAudioRole role = CreateRole("deucarian.test.provider-audio", null);
            DeucarianAudioRoleLibrary library = CreateAsset<DeucarianAudioRoleLibrary>();
            DeucarianAudioPalette palette = CreateAsset<DeucarianAudioPalette>();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            GameObject gameObject = CreateGameObject("Provider");
            DeucarianThemeProvider provider = gameObject.AddComponent<DeucarianThemeProvider>();

            library.AddRole(role);
            palette.Configure("deucarian.audio-palette.provider", "Provider", library);
            theme.Configure("deucarian.theme.provider", "Provider", null, null, palette);
            provider.SetTheme(theme);

            Assert.IsTrue(provider.UsesThemeAsset(palette));
            Assert.IsTrue(provider.UsesThemeAsset(library));
            Assert.IsTrue(provider.UsesThemeAsset(role));
        }

        [Test]
        public void SelectableThemeAudioNoOpsSafelyWhenCueHasNoClip()
        {
            DeucarianAudioRole role = CreateRole("deucarian.test.selectable-audio", null);
            DeucarianAudioPalette palette = CreateAsset<DeucarianAudioPalette>();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();
            GameObject gameObject = CreateGameObject("Selectable");
            gameObject.SetActive(false);
            gameObject.AddComponent<Button>();
            DeucarianSelectableThemeAudio adapter = gameObject.AddComponent<DeucarianSelectableThemeAudio>();

            palette.SetCue(role, DeucarianAudioCue.Empty);
            theme.Configure("deucarian.theme.selectable-audio", "Selectable", null, null, palette);
            adapter.ThemeOverride = theme;
            adapter.ClickRole = role;

            Assert.DoesNotThrow(() => adapter.OnPointerClick(null));
        }

        [Test]
        public void PaletteSetUsesExplicitExperienceBeforeDefaultPalette()
        {
            DeucarianAudioRole role = CreateRole("deucarian.test.profile", null);
            AudioClip fallbackClip = CreateClip("Fallback");
            AudioClip xrClip = CreateClip("XR");
            DeucarianAudioPalette fallback = CreateAsset<DeucarianAudioPalette>();
            DeucarianAudioPalette xr = CreateAsset<DeucarianAudioPalette>();
            DeucarianAudioPaletteSet set = CreateAsset<DeucarianAudioPaletteSet>();
            fallback.SetCue(role, new DeucarianAudioCue(fallbackClip));
            xr.SetCue(role, new DeucarianAudioCue(xrClip));
            set.Configure(fallback, new[] { CreateProfile(DeucarianAudioExperience.XR, xr) });

            Assert.IsTrue(set.TryResolve(role, DeucarianAudioExperience.XR, out DeucarianAudioResolution xrResult));
            Assert.AreSame(xrClip, xrResult.Cue.Clip);
            Assert.AreEqual(DeucarianAudioResolutionSource.ExperiencePalette, xrResult.Source);

            Assert.IsTrue(set.TryResolve(role, DeucarianAudioExperience.WebGL, out DeucarianAudioResolution webResult));
            Assert.AreSame(fallbackClip, webResult.Cue.Clip);
            Assert.AreEqual(DeucarianAudioResolutionSource.DefaultPalette, webResult.Source);
        }

        [Test]
        public void PaletteSetFallsBackToRoleDefaultAndPreservesIntentionalSilence()
        {
            AudioClip roleClip = CreateClip("Role Default");
            DeucarianAudioRole role = CreateRole("deucarian.test.role-default", roleClip);
            DeucarianAudioPalette fallback = CreateAsset<DeucarianAudioPalette>();
            DeucarianAudioPalette xr = CreateAsset<DeucarianAudioPalette>();
            DeucarianAudioPaletteSet set = CreateAsset<DeucarianAudioPaletteSet>();
            set.Configure(fallback, new[] { CreateProfile(DeucarianAudioExperience.XR, xr) });

            Assert.IsTrue(set.TryResolve(role, DeucarianAudioExperience.WebGL, out DeucarianAudioResolution roleResult));
            Assert.AreSame(roleClip, roleResult.Cue.Clip);
            Assert.AreEqual(DeucarianAudioResolutionSource.RoleDefault, roleResult.Source);

            xr.SetCue(role, DeucarianAudioCue.Silent());
            Assert.IsTrue(set.TryResolve(role, DeucarianAudioExperience.XR, out DeucarianAudioResolution silentResult));
            Assert.AreEqual(DeucarianAudioResolutionSource.IntentionalSilence, silentResult.Source);
            Assert.IsFalse(silentResult.IsAudible);
        }

        [Test]
        public void CueVariantSelectionAvoidsImmediateRepeatWhenPossible()
        {
            AudioClip first = CreateClip("First");
            AudioClip second = CreateClip("Second");
            DeucarianAudioCue cue = new DeucarianAudioCue(new[] { first, second }, 1f);

            Assert.IsTrue(cue.TrySelectVariant(0, -1, out AudioClip firstSelected, out int firstIndex));
            Assert.IsTrue(cue.TrySelectVariant(0, firstIndex, out AudioClip secondSelected, out int secondIndex));
            Assert.AreNotSame(firstSelected, secondSelected);
            Assert.AreNotEqual(firstIndex, secondIndex);
        }

        [Test]
        public void CuePitchResolutionStaysInsideConfiguredBounds()
        {
            DeucarianAudioCue cue = new DeucarianAudioCue(
                new[] { CreateClip("Pitch") },
                0.5f,
                0.8f,
                1.2f);

            Assert.AreEqual(0.8f, cue.ResolvePitch(-10f), 0.0001f);
            Assert.AreEqual(1.0f, cue.ResolvePitch(0.5f), 0.0001f);
            Assert.AreEqual(1.2f, cue.ResolvePitch(10f), 0.0001f);
        }

        [Test]
        public void PlaybackModifiersMapInteractionIntensityWithoutReplacingPaletteValues()
        {
            DeucarianAudioPlaybackModifiers soft =
                DeucarianAudioPlaybackModifiers.FromIntensity(0f);
            DeucarianAudioPlaybackModifiers firm =
                DeucarianAudioPlaybackModifiers.FromIntensity(1f);

            Assert.AreEqual(0.35f, soft.ApplyVolume(0.5f), 0.0001f);
            Assert.AreEqual(0.5f, firm.ApplyVolume(0.5f), 0.0001f);
            Assert.AreEqual(0.97f, soft.ApplyPitch(1f), 0.0001f);
            Assert.AreEqual(1.03f, firm.ApplyPitch(1f), 0.0001f);
        }

        [Test]
        public void DefaultPlaybackModifiersAreSafeIdentity()
        {
            DeucarianAudioPlaybackModifiers modifiers = default;

            Assert.AreEqual(0.4f, modifiers.ApplyVolume(0.4f), 0.0001f);
            Assert.AreEqual(1.1f, modifiers.ApplyPitch(1.1f), 0.0001f);
        }

        [Test]
        public void BuiltinClickIsCompatibilityAliasForActivate()
        {
#pragma warning disable CS0618
            Assert.AreEqual(DeucarianBuiltinAudioRoleIds.Activate, DeucarianBuiltinAudioRoleIds.Click);
#pragma warning restore CS0618
        }

        [Test]
        public void BuiltinRoleCatalogueIsCompleteAndUnique()
        {
            string[] ids =
            {
                DeucarianBuiltinAudioRoleIds.Hover,
                DeucarianBuiltinAudioRoleIds.Press,
                DeucarianBuiltinAudioRoleIds.Activate,
                DeucarianBuiltinAudioRoleIds.Select,
                DeucarianBuiltinAudioRoleIds.Submit,
                DeucarianBuiltinAudioRoleIds.Cancel,
                DeucarianBuiltinAudioRoleIds.Key,
                DeucarianBuiltinAudioRoleIds.SpecialKey,
                DeucarianBuiltinAudioRoleIds.Info,
                DeucarianBuiltinAudioRoleIds.Success,
                DeucarianBuiltinAudioRoleIds.Warning,
                DeucarianBuiltinAudioRoleIds.Error,
                DeucarianBuiltinAudioRoleIds.Invalid
            };

            Assert.AreEqual(13, new HashSet<string>(ids).Count);
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.IsTrue(DeucarianAudioRole.IsValidId(ids[i]), ids[i]);
            }
        }

        [Test]
        public void BundledDefaultsContainDistinctXrAndWebGlWarningCues()
        {
            DeucarianAudioPaletteSet set = DeucarianAudioDefaults.LoadPaletteSet();
            Assert.NotNull(set);
            Assert.NotNull(set.GetPalette(DeucarianAudioExperience.XR));
            Assert.NotNull(set.GetPalette(DeucarianAudioExperience.WebGL));
            Assert.NotNull(set.GetPalette(DeucarianAudioExperience.Desktop));
            Assert.NotNull(set.GetPalette(DeucarianAudioExperience.Mobile));

            Assert.IsTrue(set.TryResolveById(
                DeucarianBuiltinAudioRoleIds.Warning,
                DeucarianAudioExperience.XR,
                out DeucarianAudioResolution xr));
            Assert.IsTrue(set.TryResolveById(
                DeucarianBuiltinAudioRoleIds.Warning,
                DeucarianAudioExperience.WebGL,
                out DeucarianAudioResolution webGl));
            Assert.IsTrue(xr.IsAudible);
            Assert.IsTrue(webGl.IsAudible);
            Assert.AreNotSame(xr.Cue.Clip, webGl.Cue.Clip);
        }

        private static DeucarianAudioPaletteProfile CreateProfile(
            DeucarianAudioExperience experience,
            DeucarianAudioPalette palette)
        {
            DeucarianAudioPaletteProfile profile = new DeucarianAudioPaletteProfile();
            profile.Configure(experience, palette);
            return profile;
        }

        private DeucarianAudioRole CreateRole(string id, AudioClip defaultClip)
        {
            DeucarianAudioRole role = CreateAsset<DeucarianAudioRole>();
            role.name = id;
            role.Configure(
                id,
                id,
                DeucarianAudioRoleCategories.UI,
                string.Empty,
                new DeucarianAudioCue(defaultClip),
                false);
            return role;
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 32, 1, 8000, false);
            createdObjects.Add(clip);
            return clip;
        }

        private T CreateAsset<T>()
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}
