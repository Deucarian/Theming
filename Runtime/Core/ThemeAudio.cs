using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Main-thread access to one explicitly configured shared audio player.</summary>
    public static class ThemeAudio
    {
        private static Registration current;
        public static bool IsConfigured => current != null && current.Player != null;
        public static IDisposable Bind(DeucarianThemeAudioPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (IsConfigured) throw new InvalidOperationException("A default theme audio player is already registered.");
            return current = new Registration(player);
        }

        public static bool Play(AudioRoleKey role)
        {
            string id = RequireRole(role);
            return Player.PlayRoleById(id);
        }
        public static bool Play(AudioRoleKey role, DeucarianAudioPlaybackModifiers modifiers)
        {
            string id = RequireRole(role);
            return Player.PlayRoleById(id, modifiers);
        }
        public static void StopAll() => Player.StopAll();
        /// <summary>Borrowed configured player for package adapters using the lower-level playback contract.</summary>
        public static DeucarianThemeAudioPlayer Player => IsConfigured ? current.Player :
            throw new InvalidOperationException("ThemeAudio.Play has no configured player. Add an enabled ThemeAudioHost to your startup scene and assign its audio player before playing sounds.");

        private static string RequireRole(AudioRoleKey role) => role != null ? role.Id :
            throw new ArgumentNullException(nameof(role), "Select an audio role in the Inspector or pass a named key such as AudioRoles.UI.Activate.");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { current = null; }

        private sealed class Registration : IDisposable
        {
            public Registration(DeucarianThemeAudioPlayer player) { Player = player; }
            public DeucarianThemeAudioPlayer Player { get; }
            public void Dispose() { if (ReferenceEquals(current, this)) current = null; }
        }
    }
}
