using System;
using Deucarian.Media.Unity;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Owns registration of a scene's shared semantic audio output.</summary>
    [DefaultExecutionOrder(-1100), DisallowMultipleComponent]
    [RequireComponent(typeof(DeucarianThemeAudioPlayer), typeof(UnityAudioOneShotOutput))]
    public sealed class ThemeAudioHost : MonoBehaviour
    {
        private IDisposable registration;
        private void OnEnable()
        {
            var player = GetComponent<DeucarianThemeAudioPlayer>();
            if (player.Output == null) player.Output = GetComponent<UnityAudioOneShotOutput>();
            registration = ThemeAudio.Bind(player);
        }
        private void OnDisable() { registration?.Dispose(); registration = null; }
    }
}
