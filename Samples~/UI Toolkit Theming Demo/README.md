# UI Toolkit Theming Demo

This sample documents the recommended UIDocument setup for UI Toolkit.

## Demo Hierarchy

```text
ThemeProvider
UIDocument
  DeucarianUIToolkitThemeApplier
  DeucarianUIToolkitThemeVariables
```

Example VisualElement structure:

```text
.viewer-root
  .viewer-panel
    #viewer-title.viewer-title
    .viewer-body
    #viewer-button.viewer-button
    .viewer-error
```

Example bindings:

- `.viewer-root` -> `BackgroundColor`
- `.viewer-panel` -> `BackgroundColor`
- `.viewer-panel` -> `BorderColor`
- `.viewer-title` -> `TextColor`
- `.viewer-button` -> `BackgroundColor`
- `.viewer-error` -> `TextColor`

Open `Tools/Deucarian/Theming/Open Theme Manager` to create a minimal palette and apply the active theme. Use the Theme Manager's **Create UI Toolkit Demo Assets** action to create project demo files in `Assets/Deucarian/Theming/UIToolkitDemo/`.

## Designer Workflow

1. Create a minimal palette from the Theme Manager.
2. Edit colors on the palette asset.
3. Add a `DeucarianUIToolkitThemeApplier` binding for a selector, element name, or class.
4. Add custom roles only when the built-in minimal roles are not enough.
5. Switch themes at runtime through `DeucarianThemeProvider.SetTheme`.

`DeucarianUIToolkitThemeVariables` previews or generates USS variable values. Unity 2022.3 does not expose a stable runtime API for assigning USS custom variables directly, so direct style bindings are the recommended runtime path.
