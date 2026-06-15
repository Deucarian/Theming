# Basic Theming Demo

This sample is intentionally lightweight for v0.2.1.

Create the default assets with `Deucarian/Theming/Create Missing Default Theme Assets`, add a `DeucarianThemeProvider` to your scene, and place the generated default theme on it. Add one of the theme color adapters to a TMP text, uGUI Graphic, Selectable, SpriteRenderer, or Renderer object, then assign color role assets.

## Demo Hierarchy

```text
ThemeProvider
Canvas
  Button
    DeucarianSelectableThemeColors
    TMP_Text
      DeucarianTMPThemeColor
  Panel
    Image
      DeucarianGraphicThemeColor
  Label
    TMP_Text
      DeucarianTMPThemeColor
```

Use `DeucarianSelectableThemeColors` on any `UnityEngine.UI.Selectable`, including Button, Toggle, Dropdown, InputField, Scrollbar, and Slider. Assign the UI state roles created by the default asset menu:

- `deucarian.ui.normal`
- `deucarian.ui.highlighted`
- `deucarian.ui.pressed`
- `deucarian.ui.selected`
- `deucarian.ui.disabled`

## Designer Workflow

1. Create a role with `Assets/Create/Deucarian/Theming/Color Role`.
2. Add that role to the scene's `DeucarianColorRoleLibrary`.
3. Add a palette entry for the role and choose the theme color.
4. Assign the role to a theme component such as `DeucarianTMPThemeColor` or `DeucarianSelectableThemeColors`.
5. Switch themes at runtime by calling `DeucarianThemeProvider.SetTheme`.

The package resolves colors in this order:

1. A component-level theme override, if assigned.
2. The nearest parent `DeucarianThemeProvider`.
3. `DeucarianThemeProvider.Active`, if available.

Renderer adapters use `MaterialPropertyBlock`, so assigning theme colors does not clone materials.
