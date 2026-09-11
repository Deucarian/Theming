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

        public static bool Play(string roleId) => Player.PlayRoleById(roleId);
        public static bool Play(string roleId, DeucarianAudioPlaybackModifiers modifiers) =>
            Player.PlayRoleById(roleId, modifiers);
        public static void StopAll() => Player.StopAll();
        private static DeucarianThemeAudioPlayer Player => IsConfigured ? current.Player :
            throw new InvalidOperationException("Configure a ThemeAudioHost before using ThemeAudio.");

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
