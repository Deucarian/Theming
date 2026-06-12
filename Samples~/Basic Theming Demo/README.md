# Basic Theming Demo

This sample is intentionally lightweight for v0.1.0.

Create the default assets with `Tools/Deucarian/Theming/Create Default Theme Assets`, add a `DeucarianThemeProvider` to your scene, and place the generated default theme on it. Add one of the theme color adapters to a TMP text, uGUI Graphic, SpriteRenderer, or Renderer object, then assign a color role asset.

The package resolves colors in this order:

1. A component-level theme override, if assigned.
2. The nearest parent `DeucarianThemeProvider`.
3. `DeucarianThemeProvider.Active`, if available.

Renderer adapters use `MaterialPropertyBlock`, so assigning theme colors does not clone materials.
