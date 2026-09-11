# Simple usage

Add ThemeAudioHost once to a shared scene object. It installs/reuses a DeucarianThemeAudioPlayer and a Media one-shot output, then registers that player while enabled. Configure the existing theme provider, palette, and audio experience on the player or in the project settings. ThemeAudio.Play returns whether playback was accepted; muted or unresolved roles return false. Calls require the Unity main thread. Multiple default players fail explicitly; use player references for additional independent outputs.

Import the **Simple Usage** sample from Unity Package Manager. Its caller script is:

Definition fields now use named, domain-specific keys. Select an existing definition from the Inspector dropdown or pass the same named key in code. Declare each project key once in a marked key set; ordinary caller methods do not accept raw IDs. Generated keys for asset-authored definitions require no asset reference in the caller. Owner-issued selection and row handles represent runtime instances.

```csharp
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
```
