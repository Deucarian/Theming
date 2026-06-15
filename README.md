# Quick Start (Recommended)

Deucarian Theming is a Unity UPM package for designer-friendly color themes.

Most users only need a **Palette**.

You do **not** need to manually create:

- Color Roles
- Color Role Libraries
- Theme assets

The package can create and maintain those automatically.

Recommended workflow:

1. In Unity, choose `Deucarian > Theming > Create Minimal Palette`.
2. Edit the colors in the generated palette asset.
3. Open `Deucarian > Theming > Open Theme Manager` and click `Apply Theme To Scene` when you are ready to apply the active theme.

Done.

The simple mental model is:

```text
Palette
  -> Theme
      -> UI
```

The palette is the main asset. Theme and role library assets are support assets.

The top menu is intentionally limited to quick entry points:

- `Deucarian/Theming/Open Theme Manager`
- `Deucarian/Theming/Create Minimal Palette`

`Tools > Deucarian > Theming` exposes the same two entries. Use the Theme Manager for the full workflow and actions:

- Find, select, and ping theme assets.
- Create missing defaults.
- Repair palette setup.
- Create game theme assets.
- Open theme folders.
- Apply the active theme to the open scene.

Install from Git URL in Unity Package Manager:

```text
https://github.com/Deucarian/Theming.git
```

For a scoped registry, add a Deucarian registry entry to `Packages/manifest.json` once your registry is available:

```json
{
  "scopedRegistries": [
    {
      "name": "Deucarian",
      "url": "https://registry.example.com",
      "scopes": ["com.deucarian"]
    }
  ],
  "dependencies": {
    "com.deucarian.theming": "0.4.1"
  }
}
```

Deucarian Logging, TextMesh Pro, uGUI, Unity's built-in UIElements module, and `com.deucarian.editor` are declared as package dependencies. No third-party UI Toolkit package is required.

## Logging

This package uses `com.deucarian.logging`.

Theming diagnostics use stable package categories: `Theming`, `Theming.Editor`, and `Theming.UIToolkit`. Configure Deucarian Logging filters by category and level to isolate runtime theme-target warnings, editor workflow messages, or UI Toolkit adapter output. Entries flow through the shared ring buffer for recent-diagnostic inspection and remain compatible with future telemetry sinks.

# What Gets Created Automatically

`Create Minimal Palette` creates everything needed to start editing colors:

- A `DeucarianColorPalette` asset for the colors you edit.
- A `DeucarianTheme` asset that points to the palette.
- A `DeucarianColorRoleLibrary` asset used by the palette.
- Built-in role assets for common UI concepts such as background, surface, primary text, error, and button states.

These are support assets. Normal users mostly edit the palette.

Palette-first support assets are generated beside the palette under a `<PaletteName> Support/` folder. If anything is missing later, use `Repair Palette Setup`. It repairs required roles, role library links, palette entries, and theme links without overwriting user-chosen colors unless an entry is missing, null, or still using the package missing-color fallback.

Default generic assets can still be created from the Theme Manager. Game-specific roles are optional and live in the Theme Manager's advanced utilities.

# Why Color Roles Exist

Color roles let UI code and prefabs ask for meaning instead of exact colors.

Examples:

- `deucarian.background`
- `deucarian.surface`
- `deucarian.text.primary`
- `deucarian.error`
- `deucarian.ui.normal`
- `deucarian.ui.pressed`

This makes themes reusable across scenes, packages, and projects. A button can bind to the `deucarian.ui.normal` role once, while each palette decides what that color actually is.

Advanced users can create custom concepts such as:

- `reportviewer.navigation.active`
- `inventory.item.legendary`
- `dialogue.speaker.npc`

Most projects can start with the built-in minimal roles and add custom roles only when a new semantic concept appears.

# UI Toolkit

Add `DeucarianUIToolkitThemeApplier` to the same GameObject as a `UIDocument`, or assign a UIDocument explicitly.

Each binding resolves elements in this priority:

1. `ussSelector`
2. `elementName`
3. `elementClass`
4. UIDocument root when all selector fields are empty

Supported style targets include background color, text color, all border colors, individual border side colors, and Unity background image tint. Simple USS selectors such as `.viewer-root`, `#viewer-title`, `Button.viewer-button`, and type names are supported.

Example bindings:

- `.viewer-panel` -> `BackgroundColor`
- `.viewer-title` -> `TextColor`
- `.viewer-button` -> `BackgroundColor`
- `.viewer-error` -> `TextColor`

Use palette roles for binding colors. The UI Toolkit applier updates when the active provider theme changes.

`DeucarianUIToolkitThemeVariables` can preview variable names and generate USS text from a role library and theme. Unity 2022.3 does not expose a stable runtime API for assigning USS custom variables directly, so direct style bindings are the recommended runtime path.

# TMP

Use `DeucarianTMPThemeColor` with any `TMP_Text`, including TextMeshProUGUI and world-space TextMesh Pro.

```csharp
using Deucarian.Theming;
using TMPro;
using UnityEngine;

public sealed class LabelSetup : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private DeucarianColorRole primaryTextRole;

    private void Awake()
    {
        var themeColor = label.gameObject.AddComponent<DeucarianTMPThemeColor>();
        themeColor.ColorRole = primaryTextRole;
    }
}
```

# uGUI

Use `DeucarianGraphicThemeColor` with `UnityEngine.UI.Graphic` components such as `Image`, `RawImage`, and legacy `Text`.

Use `DeucarianSelectableThemeColors` with any `UnityEngine.UI.Selectable`, including Button, Toggle, Dropdown, InputField, Scrollbar, Slider, and custom Selectables. It applies normal, highlighted, pressed, selected, and disabled role colors while preserving `ColorBlock.colorMultiplier` and `ColorBlock.fadeDuration`.

Typical selectable bindings:

- Normal -> `deucarian.ui.normal`
- Highlighted -> `deucarian.ui.highlighted`
- Pressed -> `deucarian.ui.pressed`
- Selected -> `deucarian.ui.selected`
- Disabled -> `deucarian.ui.disabled`

# Runtime Theme Switching

Use `DeucarianThemeProvider.SetTheme` to switch themes at runtime.

```csharp
using Deucarian.Theming;
using UnityEngine;

public sealed class ThemeSwitcher : MonoBehaviour
{
    [SerializeField] private DeucarianThemeProvider provider;
    [SerializeField] private DeucarianTheme lightTheme;

    public void UseLightTheme()
    {
        provider.SetTheme(lightTheme);
    }
}
```

`DeucarianThemeProvider.SetTheme` reapplies the theme to child components that implement `IDeucarianThemeTarget`. Theme target components listen to their nearest provider while enabled, so they reapply automatically when the provider theme changes.

# Advanced Workflow

The palette-first workflow is recommended, but the full asset model remains available.

Advanced users can manually create and manage:

- `DeucarianColorRole`
- `DeucarianColorRoleLibrary`
- `DeucarianColorPalette`
- `DeucarianTheme`
- `DeucarianThemeProvider`

Manual workflow:

1. Create or refine color role assets with `Assets/Create/Deucarian/Theming/Color Role`.
2. Add role assets to a `DeucarianColorRoleLibrary`.
3. Add roles to a `DeucarianColorPalette` and choose the colors.
4. Link a `DeucarianTheme` to the palette.
5. Assign roles to uGUI, TMP, renderer, or UI Toolkit adapters.
6. Switch themes at runtime by calling `DeucarianThemeProvider.SetTheme`.

Game-specific roles are optional. Use the Theme Manager's `Create Game Theme Assets` action only when gameplay, faction, and item rarity roles are useful for the project.

Renderer adapters are also available. `DeucarianRendererThemeColor` uses `MaterialPropertyBlock` and does not instantiate materials. The default property is `_BaseColor`; change it to `_Color` for legacy shaders or to another shader color property when needed.

Runtime code is organized so adapters can later move into separate packages without rewriting core data APIs:

- `Runtime/Core`: role assets, libraries, palettes, themes, providers, and target contracts.
- `Runtime/UGUI`: uGUI Graphic and Selectable adapters.
- `Runtime/TMP`: TextMesh Pro adapter.
- `Runtime/Rendering`: SpriteRenderer and Renderer adapters.
- `Runtime/UIToolkit`: UIDocument and VisualElement adapters.

Future adapter packages could be:

- `com.deucarian.theming.ugui`
- `com.deucarian.theming.uitoolkit`

Editor tooling guideline: never create a separate Select button row for an asset already shown in an object field. Use `DeucarianEditorFields.DrawAssetFieldWithSelectButton<T>()` so the object field and Select button stay on the same row.

Run the package's EditMode tests in Unity. Runtime tests cover palette/theme behavior, and editor tests cover palette-first creation, repair, default asset creation, active asset settings, and manager workflows.

License: see [LICENSE.md](LICENSE.md).
