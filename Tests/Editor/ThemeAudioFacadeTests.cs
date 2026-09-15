using System;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Tests
{
    public sealed class ThemeAudioFacadeTests
    {
        [Test]
        public void RegistrationRejectsAmbiguityAndDoesNotDestroyBorrowedPlayer()
        {
            var go = new GameObject("audio");
            try
            {
                var player = go.AddComponent<DeucarianThemeAudioPlayer>();
                var registration = ThemeAudio.Bind(player);
                try { Assert.Throws<InvalidOperationException>(() => ThemeAudio.Bind(player)); }
                finally { registration.Dispose(); }
                using (ThemeAudio.Bind(player))
                {
                    registration.Dispose();
                    Assert.That(ThemeAudio.IsConfigured, Is.True);
                }
                Assert.That(ThemeAudio.IsConfigured, Is.False);
                Assert.That(player != null, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
