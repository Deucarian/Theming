# Deucarian Theming

## What this is

`com.deucarian.theming` is a Unity UPM package for designer-friendly runtime themes, palettes, color roles, theme assets, and runtime UI adapters.

Most users only need a **Palette**.

You do **not** need to manually create:

- Color Roles
- Color Role Libraries
- Theme assets

The package can create and maintain those automatically.

Current package version: `1.0.0`.

## When to use it

- You need reusable runtime color palettes and semantic color roles.
- You need runtime adapters for UI Toolkit, uGUI, TextMesh Pro, renderers, or selectable controls.
- You want designers to edit palette assets while code binds to stable semantic roles.
- You need package-specific theme packs that can create or repair their own role assets.

## When not to use it

- Do not use Theming for Deucarian editor chrome; `com.deucarian.editor` owns editor shell styling.
- Do not use Theming as a generic UI framework, layout system, package installer, diagnostics package, or registry source.
- Do not add package-specific gameplay roles as built-ins unless they belong to the generic theming domain.

## Install

Stable:

```json
"com.deucarian.theming": "https://github.com/Deucarian/Theming.git#main"
```

Development:

```json
"com.deucarian.theming": "https://github.com/Deucarian/Theming.git#develop"
```

Dependencies:

- `com.deucarian.editor` for editor tooling.
- `com.deucarian.logging` for runtime and editor diagnostics.
- `com.unity.modules.uielements` for UI Toolkit adapters and editor UI.
- `com.unity.textmeshpro` for TextMesh Pro adapters.
- `com.unity.ugui` for uGUI adapters.

npm/scoped-registry distribution is deferred for now. Use Git URLs until the manual release process is finalized.

## Unity compatibility

Requires Unity 2022.3 or newer.

## 60-second quick start

Recommended workflow:

1. In Unity, choose `Tools > Deucarian > Theming > Create Minimal Palette`.
2. Edit the colors in the generated palette asset.
3. Open `Tools > Deucarian > Theming > Open Theme Manager` and click `Apply Theme To Scene` when you are ready to apply the active theme.

Done.

The simple mental model is:

```text
Palette
  -> Theme
      -> UI
```

The palette is the main asset. Theme and role library assets are support assets.

Deucarian package tools live under `Tools/Deucarian/<PackageName>/...`.
The Theming menu is intentionally limited to quick entry points:

- `Tools/Deucarian/Theming/Open Theme Manager`
- `Tools/Deucarian/Theming/Create Minimal Palette`

Use the Theme Manager for the full workflow and actions:

- Find, select, and ping theme assets.
- Create missing defaults.
- Repair palette setup.
- Create built-in visual styles.
- Assign the active style to the active theme.
- Create game theme assets.
- Open theme folders.
- Apply the active theme to the open scene.

## Samples

- Import **Basic Theming Demo** for role, palette, theme, and adapter setup notes.
- Import **UI Toolkit Theming Demo** for UIDocument selector, VisualElement, and USS variable-generation examples.

## Public API map

- `DeucarianColorRole`, `DeucarianColorRoleLibrary`, `DeucarianColorPalette`, and `DeucarianTheme`: core semantic color assets.
- `DeucarianThemeStyle`: visual surface treatment assets such as frosted glass and material dark.
- `DeucarianThemeProvider`, `IDeucarianThemeTarget`, and `DeucarianThemeTargetBehaviour`: runtime theme application contracts.
- `DeucarianThemeRuntimeSettings` and `DeucarianThemeRuntimeResolver`: project default theme lookup.
- `DeucarianThemePack` and `DeucarianThemePackAssetFactory`: package-owned role and palette asset import/repair.
- `DeucarianUIToolkitThemeApplier` and `DeucarianUIToolkitThemeVariables`: UI Toolkit bindings and USS text generation.
- `DeucarianTMPThemeColor`, `DeucarianGraphicThemeColor`, `DeucarianSelectableThemeColors`, `DeucarianRendererThemeColor`: runtime adapters for common Unity UI/rendering targets.
- `DeucarianThemeProvider.SetTheme` and `DeucarianThemeProvider.SetStyle`: runtime switching for theme and style.

## Integrations

Works with:

- `com.deucarian.editor` for the Theme Manager shell.
- `com.deucarian.logging` for stable package categories: `Theming`, `Theming.Editor`, and `Theming.UIToolkit`.
- Unity UI Toolkit, TextMesh Pro, and uGUI adapters.

Does not own:

- Deucarian editor chrome styling,
- generic UI layout frameworks,
- package installation or registry governance,
- diagnostics ownership or telemetry.

## Logging

This package uses `com.deucarian.logging`.

Theming diagnostics use stable package categories: `Theming`, `Theming.Editor`, and `Theming.UIToolkit`. Configure Deucarian Logging filters by category and level to isolate runtime theme-target warnings, editor workflow messages, or UI Toolkit adapter output. Entries flow through the shared ring buffer for recent-diagnostic inspection and remain compatible with future telemetry sinks.

## What gets created automatically

`Create Minimal Palette` creates everything needed to start editing colors:

- A `DeucarianColorPalette` asset for the colors you edit.
- A `DeucarianTheme` asset that points to the palette.
- A `DeucarianColorRoleLibrary` asset used by the palette.
- Built-in role assets for common UI concepts such as background, surface, primary text, error, and button states.

These are support assets. Normal users mostly edit the palette.

Palette-first support assets are generated beside the palette under a `<PaletteName> Support/` folder. If anything is missing later, use `Repair Palette Setup`. It repairs required roles, role library links, palette entries, and theme links without overwriting user-chosen colors unless an entry is missing, null, or still using the package missing-color fallback.

Default generic assets can still be created from the Theme Manager. Game-specific roles are optional and live in the Theme Manager's advanced utilities.

## Runtime default theme

Builds can resolve a project default theme through a `DeucarianThemeRuntimeSettings` asset named `DeucarianThemeRuntimeSettings.asset` in any `Resources` folder. Assign its `DefaultTheme` field to the theme that runtime-created providers should use.

Runtime code can call:

- `DeucarianThemeRuntimeResolver.ResolveDefaultTheme(...)`
- `DeucarianThemeRuntimeResolver.ResolveTheme(...)`
- `DeucarianThemeRuntimeResolver.EnsureProviderHasTheme(...)`

`DeucarianThemeTargetBehaviour` also falls back to this runtime default when no provider theme is available.

## Theme packs

Packages that need their own semantic roles can provide a `DeucarianThemePack` instead of writing package-specific theme menus. A theme pack describes role assets, palette defaults, theme metadata, and the default visual style. Editor tooling can import or repair it with `DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(...)` and can create the runtime settings asset with `CreateOrRepairRuntimeSettings(...)`.

Theme packs should keep product-specific role IDs in the owning package, for example `reportviewer.navigation.active`. Deucarian Theming owns the generic import and repair mechanism; it should not absorb every package's domain-specific role names as built-ins.

## Visual styles

Colors and styles are deliberately separate.

- A `DeucarianColorPalette` decides semantic colors such as surface, primary, error, and text.
- A `DeucarianThemeStyle` decides how chrome is treated: opacity, tinting, borders, corner radius, and optional generated texture.
- A `DeucarianTheme` can reference both a palette and a style.

The built-in style presets are:

- `Frosted Glass`: translucent glass-like surfaces with cool tinting, fine texture, and soft borders.
- `Material Dark`: opaque layered dark surfaces with restrained radius and crisp dividers.
- `Fluent Acrylic`: acrylic-inspired translucent surfaces with subtle tint and texture.

This is a good fit for shared visual language, because packages such as a report viewer can keep their own layout and toolbar behavior while asking Theming for the surface treatment. It would be a bad fit if the style asset started owning product-specific UI structure, navigation rules, or a generic UI framework.

Use the Theme Manager's advanced actions to create the built-in style assets under the default theme folder. Assign one to the active theme, or switch at runtime with `DeucarianThemeProvider.SetStyle`. If no provider style override is set, `DeucarianThemeProvider.CurrentStyle` resolves from the current theme's `VisualStyle`.

UI Toolkit and uGUI helpers are available for package-specific UI code that wants to apply a style without copying preset math:

- `Deucarian.Theming.UIToolkit.DeucarianUIToolkitThemeStyleUtility.ApplyPanel(...)`
- `Deucarian.Theming.DeucarianUGUIThemeStyleUtility.ApplyPanel(...)`
- `Deucarian.Theming.DeucarianUGUIThemeStyleUtility.ApplyOutline(...)`

## Why color roles exist

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

## UI Toolkit

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

## TMP

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

## uGUI

Use `DeucarianGraphicThemeColor` with `UnityEngine.UI.Graphic` components such as `Image`, `RawImage`, and legacy `Text`.

Use `DeucarianSelectableThemeColors` with any `UnityEngine.UI.Selectable`, including Button, Toggle, Dropdown, InputField, Scrollbar, Slider, and custom Selectables. It applies normal, highlighted, pressed, selected, and disabled role colors while preserving `ColorBlock.colorMultiplier` and `ColorBlock.fadeDuration`.

Typical selectable bindings:

- Normal -> `deucarian.ui.normal`
- Highlighted -> `deucarian.ui.highlighted`
- Pressed -> `deucarian.ui.pressed`
- Selected -> `deucarian.ui.selected`
- Disabled -> `deucarian.ui.disabled`

## Runtime theme switching

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

## Advanced workflow

The palette-first workflow is recommended, but the full asset model remains available.

Advanced users can manually create and manage:

- `DeucarianColorRole`
- `DeucarianColorRoleLibrary`
- `DeucarianColorPalette`
- `DeucarianTheme`
- `DeucarianThemeStyle`
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

## Troubleshooting

- If UI does not update, confirm a `DeucarianThemeProvider` is present and the target implements or uses a supported theme adapter.
- If a UI Toolkit binding does not resolve, check selector priority: `ussSelector`, then `elementName`, then `elementClass`, then UIDocument root.
- If generated palette support assets are missing, use `Repair Palette Setup` instead of recreating role libraries manually.
- If a package needs domain-specific color roles, create a `DeucarianThemePack` in the owning package rather than adding those roles as Theming built-ins.

## Validation

Run the shared package validator from the repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Run the package's Runtime and EditMode tests in Unity after code or assembly definition changes. Runtime tests cover palette/theme behavior, and editor tests cover palette-first creation, repair, default asset creation, active asset settings, and manager workflows.

Documentation-only updates should still pass:

```powershell
git diff --check
```

## Architecture / Contributor Notes

- [AGENTS.md](AGENTS.md) contains repository-specific ownership and Codex guidance.
- Deucarian architecture rules live in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/develop/ARCHITECTURE.md).
- Capability ownership is tracked in [CAPABILITY_OWNERSHIP.md](https://github.com/Deucarian/Package-Registry/blob/develop/CAPABILITY_OWNERSHIP.md).

## License

See [LICENSE.md](LICENSE.md).
