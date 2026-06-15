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

Create default theme assets with `Deucarian/Theming/Create Missing Default Theme Assets`. Create project demo files in `Assets/Deucarian/Theming/UIToolkitDemo/` with `Tools/Deucarian/Theming/Create UI Toolkit Demo Assets`.

## Designer Workflow

1. Create a role asset.
2. Add it to a `DeucarianColorRoleLibrary`.
3. Add a palette entry for that role.
4. Add a `DeucarianUIToolkitThemeApplier` binding for a selector, element name, or class.
5. Switch themes at runtime through `DeucarianThemeProvider.SetTheme`.

`DeucarianUIToolkitThemeVariables` previews or generates USS variable values. Unity 2022.3 does not expose a stable runtime API for assigning USS custom variables directly, so direct style bindings are the recommended runtime path.
