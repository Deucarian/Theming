using System;
using NUnit.Framework;
using UnityEditor;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianInteractionAudioDefaultsTests
    {
        [Test]
        public void EveryBundledExperienceAndRoleFallbackUsesTheSamePackageOwnedSoundStyle()
        {
            var set = DeucarianAudioDefaults.LoadPaletteSet();
            Assert.That(set, Is.Not.Null);
            Assert.That(set.DefaultPalette.RoleLibrary.Roles.Count, Is.EqualTo(13));
            foreach (var role in set.DefaultPalette.RoleLibrary.Roles)
            {
                Assert.That(set.TryResolve(role, DeucarianAudioExperience.Default, out var expected), Is.True);
                Assert.That(expected.Cue.TrySelectVariant(0, -1, out var expectedClip, out _), Is.True);
                string name = role.Id == DeucarianBuiltinAudioRoleIds.Hover ? "interaction-hover.wav" :
                    role.Id == DeucarianBuiltinAudioRoleIds.Warning ? "notification-warning.ogg" : "interaction-click.wav";
                Assert.That(AssetDatabase.GetAssetPath(expectedClip), Is.EqualTo("Packages/com.deucarian.theming/Runtime/Resources/Deucarian/Theming/Audio/Defaults/" + name));
                Assert.That(role.DefaultCue.TrySelectVariant(0, -1, out var fallback, out _), Is.True);
                Assert.That(fallback, Is.SameAs(expectedClip));
                foreach (DeucarianAudioExperience experience in Enum.GetValues(typeof(DeucarianAudioExperience)))
                {
                    Assert.That(set.TryResolve(role, experience, out var actual), Is.True);
                    Assert.That(actual.Cue.TrySelectVariant(0, -1, out var clip, out _), Is.True);
                    Assert.That(clip, Is.SameAs(expectedClip), role.Id + " / " + experience);
                    Assert.That(actual.Cue.Volume, Is.EqualTo(expected.Cue.Volume));
                    Assert.That(actual.Cue.MinimumPitch, Is.EqualTo(expected.Cue.MinimumPitch));
                    Assert.That(actual.Cue.MaximumPitch, Is.EqualTo(expected.Cue.MaximumPitch));
                }
            }
        }
    }
}
