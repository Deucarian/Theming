# Audio Palette Demo

Add `AudioPaletteDemo` to an empty GameObject. Its required components compose
the bundled default palette set with the Media one-shot output automatically.

Connect UI buttons to `UseDefault`, `UseXR`, `UseWebGL`, `UseDesktop`, or
`UseMobile`, then to `PlayActivate`, `PlayKey`, or `PlayWarning`. Changing the
experience changes resolution without changing the semantic playback call.
The sample does not change project settings and all playback remains manual.
