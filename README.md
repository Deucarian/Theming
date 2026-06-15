# Deucarian Theming

## Overview

Deucarian Theming is a Unity UPM package for designer-friendly color themes. It uses `ScriptableObject` color role assets as the source of truth, so new roles can be created in the Unity Editor without changing C#.

## Installation

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
    "com.deucarian.theming": "0.4.0"
  }
}
```

TextMesh Pro, uGUI, Unity's built-in UIElements module, and `com.deucarian.editor` are declared as package dependencies. No third-party UI Toolkit package is required.

## Deucarian Menu Workflow

The package exposes high-level Unity Editor entries under both `Tools > Deucarian > Theming` and `Deucarian > Theming`:

- `Open Theme Manager` opens `DeucarianThemeManagerWindow`.
- `Create Minimal Palette` creates a palette-first setup for normal use.
- `Repair Palette Setup` repairs support role, library, palette-entry, and theme links for the active palette.
- `Create Palette From Active Theme` copies the active theme palette into a new editable palette.
- `Create Missing Default Theme Assets` creates the minimal generic Deucarian defaults.
- `Create Game Theme Assets` creates optional gameplay, faction, and item rarity presets.
- `Repair Generated Asset Names` fixes Unity main-object names to match asset filenames.
- `Open Theme Assets Folder` selects `Assets/Deucarian/Theming/Defaults/` in the Project window.

Use the Theme Manager for package workflows:

- Create or reuse one main palette asset, then let the package create or repair required support assets.
- Set active palette, theme, and role library assets, with the palette shown first.
- Create or repair a theme from the active palette.
- Apply the active theme to open-scene `DeucarianThemeProvider` components, asking before creating one when the scene has none.
- Create optional game theme assets under `Assets/Deucarian/Theming/Game/`.
- Create the existing UI Toolkit demo files under `Assets/Deucarian/Theming/UIToolkitDemo/`.

Active theme, palette, role library, and default asset folder selections are stored by asset GUID/path in `EditorPrefs`. The editor UI uses `com.deucarian.editor` for fixed Deucarian chrome, icons, status badges, and inline asset field controls. Palette-first support assets are generated beside the palette under a `<PaletteName> Support/` folder. Default assets are created in `Assets/Deucarian/Theming/Defaults/`, under the project folder `Assets/Deucarian/Theming/`.

Editor tooling guideline: never create a separate Select button row for an asset already shown in an object field. Use `DeucarianEditorFields.DrawAssetFieldWithSelectButton<T>()` so the object field and Select button stay on the same row.

## Core Concept

- `DeucarianColorRole`: designer-authored role asset with a stable ID, display name, category string, and default color.
- `DeucarianColorRoleLibrary`: designer-maintained list of roles with duplicate/null validation.
- `DeucarianColorPalette`: maps role assets to concrete `UnityEngine.Color` values.
- `DeucarianTheme`: references the active palette for a visual style.
- `DeucarianThemeProvider`: scene component that applies themes to child `IDeucarianThemeTarget` components and notifies targets when the theme changes.

Enums make every new color role a code change. This package keeps roles as assets with stable string IDs such as `deucarian.primary`, `deucarian.ui.normal`, or `reportviewer.text.primary`. Code can use optional constants from `DeucarianBuiltinColorRoleIds`, but designers can add new roles without modifying or recompiling C#.

## Default Theme Presets

Minimal Default Theme Assets are generic and brand-aligned. They include only core, text, status, and UI state roles, so new projects do not start with gameplay assumptions.

Game Theme Assets are optional. They add gameplay resource roles, faction roles, item rarity roles, and a couple of game-specific highlight/interactable roles. Designers can still create custom roles at any time by adding role assets to their own libraries and palettes.

The minimal default palette uses the Deucarian brand palette:

| Role | Color |
| --- | --- |
| `deucarian.background` | `#0D1218` |
| `deucarian.surface` | `#1A2330` |
| `deucarian.surface.raised` | `#2C3A4D` |
| `deucarian.primary` | `#5A6FA0` |
| `deucarian.secondary` | `#3BA69A` |
| `deucarian.accent` | `#276065` |
| `deucarian.text.primary` | `#C4CAD1` |
| `deucarian.text.secondary` | `#A8B0BA` |
| `deucarian.text.muted` | `#6F7A86` |
| `deucarian.text.disabled` | `#3C444F` |
| `deucarian.success` | `#3BA69A` |
| `deucarian.warning` | `#A87932` |
| `deucarian.error` | `#A04444` |
| `deucarian.info` | `#5A6FA0` |
| `deucarian.ui.normal` | `#1A2330` |
| `deucarian.ui.highlighted` | `#2C3A4D` |
| `deucarian.ui.pressed` | `#276065` |
| `deucarian.ui.selected` | `#3BA69A` |
| `deucarian.ui.disabled` | `#3C444F` |
| `deucarian.ui.focused` | `#5A6FA0` |

## Simple Palette Workflow

Most users should start with one palette asset:

1. Open `Tools > Deucarian > Theming > Open Theme Manager`.
2. Click `Create Minimal Palette`.
3. Edit colors on the generated `DeucarianColorPalette` asset.
4. Click `Create Theme From Active Palette` or `Repair Palette Setup` if support assets need repair.
5. Click `Apply To Scene` to assign the active theme to open-scene `DeucarianThemeProvider` components.

The generated roles, role library, and theme exist to support semantic theming. Normal palette editing usually happens on the palette asset; users do not need to manage every generated role asset directly.

## Advanced Workflow

Advanced users can still work directly with the full asset model:

1. Create or refine color role assets with `Assets/Create/Deucarian/Theming/Color Role`.
2. Add role assets to a `DeucarianColorRoleLibrary`.
3. Add roles to a `DeucarianColorPalette` and choose the colors.
4. Link a `DeucarianTheme` to the palette.
5. Assign roles to uGUI, TMP, renderer, or UI Toolkit adapters.
6. Switch themes at runtime by calling `DeucarianThemeProvider.SetTheme`.

Game-specific roles are optional. Use `Create Game Theme Assets` only when gameplay, faction, and item rarity roles are useful for the project.

## uGUI Usage

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

Use `DeucarianGraphicThemeColor` with `UnityEngine.UI.Graphic` components such as `Image`, `RawImage`, and legacy `Text`.

Use `DeucarianSelectableThemeColors` with any `UnityEngine.UI.Selectable`, including Button, Toggle, Dropdown, InputField, Scrollbar, Slider, and custom Selectables. It applies normal, highlighted, pressed, selected, and disabled role colors while preserving `ColorBlock.colorMultiplier` and `ColorBlock.fadeDuration`.

## UI Toolkit Usage

Add `DeucarianUIToolkitThemeApplier` to the same GameObject as a `UIDocument`, or assign a UIDocument explicitly. Each binding resolves elements in this priority:

1. `ussSelector`
2. `elementName`
3. `elementClass`
4. UIDocument root when all selector fields are empty

Supported style targets include background color, text color, all border colors, individual border side colors, and Unity background image tint. Simple USS selectors such as `.viewer-root`, `#viewer-title`, `Button.viewer-button`, and type names are supported.

Example bindings:

- `.viewer-root` -> `BackgroundColor`
- `.viewer-panel` -> `BackgroundColor`
- `.viewer-title` -> `TextColor`
- `.viewer-button` -> `BackgroundColor`
- `.viewer-error` -> `TextColor`

`DeucarianUIToolkitThemeVariables` can preview variable names and generate USS text from a role library and theme. Unity 2022.3 does not expose a stable runtime API for assigning USS custom variables directly, so this package does not silently fake that behavior. Use generated USS text or bind concrete style properties with `DeucarianUIToolkitThemeApplier`.

UXML factories are intentionally not part of this pass. The supported v0 path is MonoBehaviour appliers on a `UIDocument`.

## Runtime Theme Switching

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

## Renderer Colors

`DeucarianRendererThemeColor` uses `MaterialPropertyBlock` and does not instantiate materials. The default property is `_BaseColor`; change it to `_Color` for legacy shaders or to another shader color property when needed.

## Recommended Package Architecture

Adapters currently live in this same repo/package to keep setup simple. Runtime code is organized so adapters can later move into separate packages without rewriting core data APIs:

- `Runtime/Core`: role assets, libraries, palettes, themes, providers, and target contracts.
- `Runtime/UGUI`: uGUI Graphic and Selectable adapters.
- `Runtime/TMP`: TextMesh Pro adapter.
- `Runtime/Rendering`: SpriteRenderer and Renderer adapters.
- `Runtime/UIToolkit`: UIDocument and VisualElement adapters.

Future adapter packages could be:

- `com.deucarian.theming.ugui`
- `com.deucarian.theming.uitoolkit`

The core role, palette, and theme types do not depend on adapter-specific behavior.

## Report Viewer Example

For the Simultria 3D Report Viewer, create project-specific roles such as:

- `reportviewer.background`
- `reportviewer.panel`
- `reportviewer.text.primary`
- `reportviewer.text.secondary`
- `reportviewer.button.normal`
- `reportviewer.button.hover`
- `reportviewer.button.pressed`
- `reportviewer.error`
- `reportviewer.loading`
- `reportviewer.navigation.active`

Example UI Toolkit bindings:

- `.viewer-root` -> `BackgroundColor`
- `.viewer-panel` -> `BackgroundColor`
- `.viewer-title` -> `TextColor`
- `.viewer-button` -> `BackgroundColor`
- `.viewer-error` -> `TextColor`

Use a `DeucarianThemeProvider` at the viewer root so uGUI, TMP, renderer, and UI Toolkit theme targets all update when the report viewer switches theme.

## Tests

Run the package's EditMode tests in Unity. Runtime tests cover palette/theme behavior, and editor tests cover default asset creation, active asset settings, and manager workflows.

## License

See [LICENSE.md](LICENSE.md).
