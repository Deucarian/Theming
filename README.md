# Deucarian Theming

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
    "com.deucarian.theming": "0.2.1"
  }
}
```

TextMesh Pro, uGUI, and Unity's built-in UIElements module are declared as package dependencies. No third-party UI Toolkit package is required.

## Deucarian Menu

The package owns its Unity Editor tools under `Deucarian > Theming`:

- `Open Theme Manager` opens `DeucarianThemeManagerWindow` for selecting active theme, palette, and role library assets.
- `Create Missing Default Theme Assets` creates or reuses default roles, a role library, a palette, and a theme under `Assets/Deucarian/Theming/Defaults/`.
- `Select Active Theme`, `Select Active Palette`, and `Select Role Library` select the only matching asset, create defaults when none exist, or open the Theme Manager when multiple choices exist.
- `Open Theme Assets Folder` selects and pings `Assets/Deucarian/Theming/Defaults/`.
- `Apply Active Theme To Open Scene` assigns the active theme to open-scene `DeucarianThemeProvider` components, creating one provider when the scene has none.
- `Create UI Toolkit Demo Assets` creates the existing package-local UI Toolkit demo files under `Assets/Deucarian/Theming/UIToolkitDemo/`.

Active theme, palette, and role library selections are stored by asset GUID in `EditorPrefs`; no other Deucarian package is required to use these menu items.

## Core Concept

- `DeucarianColorRole`: designer-authored role asset with a stable ID, display name, category string, and default color.
- `DeucarianColorRoleLibrary`: designer-maintained list of roles with duplicate/null validation.
- `DeucarianColorPalette`: maps role assets to concrete `UnityEngine.Color` values.
- `DeucarianTheme`: references the active palette for a visual style.
- `DeucarianThemeProvider`: scene component that applies themes to child `IDeucarianThemeTarget` components and notifies targets when the theme changes.

Enums make every new color role a code change. This package keeps roles as assets with stable string IDs such as `deucarian.semantic.primary`, `deucarian.ui.normal`, or `reportviewer.text.primary`. Code can use optional constants from `DeucarianBuiltinColorRoleIds`, but designers can add new roles without modifying or recompiling C#.

## Designer Workflow

1. Create default assets with `Deucarian > Theming > Create Missing Default Theme Assets`.
2. Refine or add color role assets with `Assets/Create/Deucarian/Theming/Color Role`.
3. Add role assets to a `DeucarianColorRoleLibrary`.
4. Add roles to a `DeucarianColorPalette` and choose the colors.
5. Assign roles to uGUI, TMP, renderer, or UI Toolkit adapters.
6. Switch themes at runtime by calling `DeucarianThemeProvider.SetTheme`.

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
