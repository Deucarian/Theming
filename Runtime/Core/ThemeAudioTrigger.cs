using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Reusable UnityEvent entry point using the same typed role as C# callers.</summary>
    [AddComponentMenu("Deucarian/Theming/Theme Audio Trigger")]
    public sealed class ThemeAudioTrigger : MonoBehaviour
    {
        [SerializeField] private AudioRoleKey role;
        public AudioRoleKey Role { get => role; set => role = value; }
        public void Play()
        {
            if (role == null) throw new InvalidOperationException("Select an audio role on ThemeAudioTrigger '" + name + "', or create one in the Audio Palette Lab.");
            ThemeAudio.Play(role);
        }
    }
}
