using UnityEngine;

namespace Deucarian.Theming.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        public void Click() => ThemeAudio.Play("deucarian.ui.audio.activate");
        public void Warning() => ThemeAudio.Play("deucarian.feedback.audio.warning");
    }
}
