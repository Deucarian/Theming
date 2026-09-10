using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemingConnectionObserver : IDisposable
    {
        private readonly List<DeucarianThemeAudioPlayer> players = new List<DeucarianThemeAudioPlayer>();
        private bool observing;
        public event Action Changed;
        public bool Buttons { get; private set; }
        public bool Keyboard { get; private set; }
        public bool Warnings { get; private set; }

        public void Start()
        {
            if (observing) return;
            observing = true;
            EditorApplication.hierarchyChanged += Refresh;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Refresh();
        }

        public void Refresh()
        {
            if (!observing) return;
            ReleasePlayers();
            Buttons = Keyboard = Warnings = false;
            foreach (var player in Resources.FindObjectsOfTypeAll<DeucarianThemeAudioPlayer>())
            {
                if (!IsLive(player)) continue;
                players.Add(player);
                player.RolePlayed += OnRolePlayed;
                foreach (string role in player.PlayedRoleIds) Record(role);
            }
            foreach (var adapter in Resources.FindObjectsOfTypeAll<Deucarian.Theming.DeucarianSelectableThemeAudio>())
            {
                if (IsLive(adapter) && IsLive(adapter.AudioPlayer) &&
                    (adapter.HoverRole != null || adapter.PressRole != null || adapter.ActivateRole != null ||
                     adapter.SelectRole != null || adapter.SubmitRole != null || adapter.CancelRole != null))
                    Buttons = true;
            }
            Changed?.Invoke();
        }

        private static bool IsLive(Behaviour component) => component != null && component.isActiveAndEnabled
            && !EditorUtility.IsPersistent(component) && component.gameObject.scene.IsValid();

        private void OnRolePlayed(string role)
        {
            bool beforeButtons = Buttons, beforeKeyboard = Keyboard, beforeWarnings = Warnings;
            Record(role);
            if (Buttons != beforeButtons || Keyboard != beforeKeyboard || Warnings != beforeWarnings) Changed?.Invoke();
        }

        private void Record(string role)
        {
            if (string.IsNullOrEmpty(role)) return;
            if (role.StartsWith("deucarian.ui.audio.", StringComparison.Ordinal)) Buttons = true;
            if (role.StartsWith("deucarian.input.audio.key", StringComparison.Ordinal)) Keyboard = true;
            if (role == DeucarianBuiltinAudioRoleIds.Warning || role == DeucarianBuiltinAudioRoleIds.Error) Warnings = true;
        }

        private void OnPlayModeChanged(PlayModeStateChange state) => Refresh();
        private void ReleasePlayers()
        {
            foreach (var player in players) if (player != null) player.RolePlayed -= OnRolePlayed;
            players.Clear();
        }
        public void Dispose()
        {
            if (!observing) return;
            observing = false;
            EditorApplication.hierarchyChanged -= Refresh;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            ReleasePlayers();
        }
    }
}
