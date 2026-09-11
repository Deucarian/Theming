# Basic Theming Demo

Open **BasicThemingDemo.unity** and press Play. Use Light theme / Dark theme.
The scene is fully wired to the bundled Deucarian family and distinguishes the
outer Background role from the inner Surface role. Select **Example canvas** to
inspect the family/provider and select a graphic to inspect its role adapter.

Visual styling must be enabled in Theming > Project setup. The sample respects
that switch and never changes project settings. For an Input-System-only project,
replace the Event System's built-in module with InputSystemUIInputModule.

To customize, open Control Center > Theming > Visual palettes and create a project
theme family. Assign it to the demo's Family and provider. Keep bundled assets
unchanged so future package updates remain safe.

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
