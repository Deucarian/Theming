# Audio Palette Demo

Open **AudioPaletteDemo.unity** and press Play. Choose an experience, then press
Button press, Keyboard key or Warning ping. Unmute Game view to hear playback.
Select **Example canvas** to inspect the serialized palette set, experience,
theme player, Media output and AudioSource. The scene includes an AudioListener.

Connect UI buttons to `UseDefault`, `UseXR`, `UseWebGL`, `UseDesktop`, or
`UseMobile`, then to `PlayActivate`, `PlayKey`, or `PlayWarning`. Changing the
experience changes resolution without changing the semantic playback call.
The sample does not change project settings and all playback remains manual.

The scene uses the built-in input module. For Input-System-only projects, replace
the Event System module with InputSystemUIInputModule. Audio must be enabled in
Theming > Project setup; this sample deliberately respects that switch.
