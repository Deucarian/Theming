# Simple usage

Add ThemeAudioHost once to a shared scene object. It installs/reuses a DeucarianThemeAudioPlayer and a Media one-shot output, then registers that player while enabled. Configure the existing theme provider, palette, and audio experience on the player or in the project settings. ThemeAudio.Play returns whether playback was accepted; muted or unresolved roles return false. Calls require the Unity main thread. Multiple default players fail explicitly; use player references for additional independent outputs.

Import the **Simple Usage** sample from Unity Package Manager. Its caller script is:

```csharp
using UnityEngine;

namespace Deucarian.Theming.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        public void Click() => ThemeAudio.Play("deucarian.ui.audio.activate");
        public void Warning() => ThemeAudio.Play("deucarian.feedback.audio.warning");
    }
}
```
