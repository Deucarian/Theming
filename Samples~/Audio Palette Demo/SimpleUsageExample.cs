using UnityEngine;

namespace Deucarian.Theming.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        [SerializeField] private AudioRoleKey clickSound = AudioRoles.UI.Activate;

        public void Click() => ThemeAudio.Play(clickSound);
        public void Warning() => ThemeAudio.Play(AudioRoles.Feedback.Warning);
    }
}
