# Basic Theming Demo

This sample is intentionally lightweight for v0.4.2.

Open `Tools/Deucarian/Experience and Interaction/UI and Presentation/Theming/Open Theme Manager` and use **Create Theme Family**, then preview Light/Dark and choose **Apply Preview To Scene**. Add one of the theme color adapters to a TMP text, uGUI Graphic, Selectable, SpriteRenderer, or Renderer object, then assign color role assets.

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

1. Create a light/dark theme family from the Theme Manager.
2. Edit colors on both palette assets and use the preview mode to compare them.
3. Assign roles to theme components such as `DeucarianTMPThemeColor` or `DeucarianSelectableThemeColors`.
4. Add custom roles only when the built-in minimal roles are not enough.
5. Switch modes at runtime by calling `DeucarianThemeProvider.SetThemeMode`.

The package resolves colors in this order:

1. A component-level theme override, if assigned.
2. The nearest parent `DeucarianThemeProvider`.
3. `DeucarianThemeProvider.Active`, if available.

Renderer adapters use `MaterialPropertyBlock`, so assigning theme colors does not clone materials.
