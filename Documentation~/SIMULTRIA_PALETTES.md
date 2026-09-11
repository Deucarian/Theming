# Simultria visual palettes

Theming owns the reusable palette definitions. XR UI and Notifications consume
these colors; they do not maintain another editable brand palette.

In **Theming → Project setup → Visual styling → Simultria presets**, choose:

- **Design & Sales (DS)**: purple/pink. The dark preset preserves HoloHelmet's
  existing authored control colors, including its distinct title, keyboard,
  slider, hover, pressed and selected colors.
- **Realisation & Progress (RP)**: green. Both variants use the existing
  Report Viewer palette values.

Choosing a preset creates editable project assets, selects the resulting family,
and sets its initial mode to Dark. Running the same creation action again reuses
the assets without replacing your color edits. It does not enable visual styling,
change audio, install an integration, or attach components to a scene.

The same public editor factories are available as
`DeucarianSimultriaThemeAssets.CreateDesignAndSales(rootFolder)` and
`CreateRealisationAndProgress(rootFolder)`.

## Source and compatibility

The source was inspected on 11 September 2026:

| Variant | Existing authored source |
| --- | --- |
| DS Dark | HoloHelmet: `Assets/Simultria/Generic/UI/CustomButtons/Resources/SimultriaColorPalette.asset` |
| DS Light | Report Viewer: `Assets/Simultria/ReportViewer/Theming/Themes/DS/DSLightPalette.asset` |
| RP Light / Dark | Report Viewer: `Assets/Simultria/ReportViewer/Theming/Themes/RP/RPLightPalette.asset` and `RPDarkPalette.asset` |

DS Dark intentionally preserves the actual HoloHelmet primary
`(0.39200002, 0.32200003, 0.5, 1)`, not the brighter generic XR fallback.
Its title/accent remains `(0.7686275, 0.6313726, 0.97647065, 1)`.
The authored interaction multipliers are baked once into UI state roles: hover
2× secondary (clamped), pressed 1.1× primary and selected 1.5× primary.
Adapters must not multiply these already resolved state colors again.

Optional `DeucarianControlColorRoleIds` preserve finer control colors for DS
Dark. DS Light and RP derive those control values from their authored standard
semantic roles, because those source assets do not define separate keyboard,
slider or image overrides. Transparent remains transparent. No application,
model-rendering, skybox, report category, or lighting behavior is imported.

## XR UI and notifications

Keep the optional XR UI Theming Integration when using XR UI with Theming.
Add its bridge alongside an explicitly assigned `XrUiPaletteScope` at the UI
root, and use a nearby theme provider or the project default theme. The bridge
creates a temporary output palette and never rewrites a source palette asset.
Legacy XR palette assets are retained as fallback inputs for existing projects.

Notifications already depends on Theming. Its runtime rows and editor preview
resolve the same semantic surface, title, body and severity roles. Turning off
Visual styling restores authored runtime row colors/typography and releases XR
bridge palette registrations; it does not clear the configured theme family.

The visual palette preview shows the Background behind the Surface panel.
Background is the app canvas; Surface and Surface Raised are panels above it.
