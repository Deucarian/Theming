# UI Toolkit Theming Demo

Open **UIToolkitThemingDemo.unity** and press Play. Light theme / Dark theme update
the background, panel, text and buttons together. Select **Theme demo** to inspect
the provider, UIDocument, serialized selector bindings and sample controller.
The scene uses the bundled family and includes its own editable PanelSettings.
Visual styling must be enabled in Theming > Project setup.

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
    .actions
      #light.viewer-button
      #dark.viewer-button
```

Example bindings:

- `.viewer-root` -> `BackgroundColor`
- `.viewer-panel` -> `BackgroundColor`
- `.viewer-panel` -> `BorderColor`
- `.viewer-title` -> `TextColor`
- `.viewer-button` -> `BackgroundColor`
- `.viewer-body` -> `TextColor`

Open `Tools/Deucarian/Control Center`, then choose **Experience > Theme Manager** to create a theme family, preview Light/Dark, and use **Apply Preview To Scene**. Use the Theme Manager's **Create UI Toolkit Demo Assets** action to create project demo files in `Assets/Deucarian/Theming/UIToolkitDemo/`.

## Designer Workflow

1. Create a light/dark theme family from the Theme Manager.
2. Edit colors on both palette assets.
3. Add a `DeucarianUIToolkitThemeApplier` binding for a selector, element name, or class.
4. Add custom roles only when the built-in minimal roles are not enough.
5. Switch modes at runtime through `DeucarianThemeProvider.SetThemeMode`.

`DeucarianUIToolkitThemeVariables` previews or generates USS variable values. Unity 2022.3 does not expose a stable runtime API for assigning USS custom variables directly, so direct style bindings are the recommended runtime path.
