# Deucarian Theming

## What this is

`com.deucarian.theming` is a Unity UPM package for designer-friendly runtime themes, palettes, color roles, theme assets, and runtime UI adapters.

Most users only need a **Theme Family** and its two editable palettes.

You do **not** need to manually create:

- Color Roles
- Color Role Libraries
- Theme variant and family assets

The package can create and maintain those automatically.

Current package version: `1.0.1`.

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

1. In Unity, choose `Tools > Deucarian > Theming > Create Theme Family`.
2. Edit the generated light and dark palette assets.
3. Open `Tools > Deucarian > Theming > Open Theme Manager`, choose the Theme Family, Mode, and Visual Style you want, review the live preview, then click **Activate**. Activation updates the project runtime default and synchronizes loaded scene providers together.

Done.

The simple mental model is:

```text
Light Palette -> Light Theme --+
                              +-> Theme Family -> Provider -> UI
Dark Palette  -> Dark Theme  --+
```

The two palettes are the main editable assets. Concrete theme variants, the family, and the shared role library are support assets.

Long theming lists in palette, role-library, theme-pack, and UI Toolkit inspectors include case-insensitive search and role-category filtering. Multiple search terms are combined, matching entries remain directly editable and removable, and clearing the filter restores Unity's normal add and reorder controls. Filtering is inspector-only and never changes asset ordering or values by itself.

Deucarian package tools live under `Tools/Deucarian/<PackageName>/...`.
The Theming menu is intentionally limited to quick entry points:

- `Tools/Deucarian/Theming/Open Theme Manager`
- `Tools/Deucarian/Theming/Create Theme Family`

Use Theme Manager's searchable Family and Visual Style pickers for the everyday workflow. Family, Mode, and Visual Style choices preview immediately on loaded providers without changing provider serialization, theme assets, runtime settings, scenes, or builds. Choices remain staged and receive `*` markers until **Activate** commits the family, mode, shared style, runtime default, and loaded-provider synchronization in one Undo operation. Incomplete project setup appears contextually; asset creation, repair, folders, demos, and legacy utilities stay collapsed under **Developer Tools**.

The shared responsive workbench toolbar switches between **Theme**, **Style Composer**, and **Runtime Settings** while keeping each view's current summary and primary action visible at compact and wide window sizes.

The per-user preview is restored after script reloads and after Play Mode startup has applied project settings. A later runtime call to `SetThemeFamily`, `SetThemeMode`, `SetTheme`, or `SetStyle` takes precedence and clears that provider's preview. Player builds temporarily suspend preview state and always use the activated runtime settings; leaving the Theme Manager on an unactivated choice cannot change build output.

## Samples

- Import **Basic Theming Demo** for role, palette, theme, and adapter setup notes.
- Import **UI Toolkit Theming Demo** for UIDocument selector, VisualElement, and USS variable-generation examples.

## Public API map

- `DeucarianColorRole`, `DeucarianColorRoleLibrary`, `DeucarianColorPalette`, and `DeucarianTheme`: core semantic color and concrete variant assets.
- `DeucarianThemeMode` and `DeucarianThemeFamily`: explicit light/dark selection and paired theme identity.
- `DeucarianThemeStyle`: a presentation composition referencing reusable surface, shape, and stroke profiles plus a semantic density.
- `DeucarianThemeSurfaceProfile`, `DeucarianThemeShapeProfile`, `DeucarianThemeStrokeProfile`, and `DeucarianThemeDensity`: independently reusable presentation axes.
- `DeucarianThemeProvider`, `IDeucarianThemeTarget`, and `DeucarianThemeTargetBehaviour`: runtime theme application contracts.
- `DeucarianThemeRuntimeSettings` and `DeucarianThemeRuntimeResolver`: project default theme lookup.
- `DeucarianThemePack` and `DeucarianThemePackAssetFactory`: package-owned role and palette asset import/repair.
- `DeucarianUIToolkitThemeApplier` and `DeucarianUIToolkitThemeVariables`: UI Toolkit bindings and USS text generation.
- `DeucarianTMPThemeColor`, `DeucarianGraphicThemeColor`, `DeucarianSelectableThemeColors`, `DeucarianRendererThemeColor`: runtime adapters for common Unity UI/rendering targets.
- `DeucarianThemeProvider.SetThemeFamily`, `SetThemeMode`, `SetTheme`, and `SetStyle`: paired-mode, legacy standalone-theme, and style switching.

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

`Create Theme Family` creates everything needed to start editing both modes:

- Independently editable light and dark `DeucarianColorPalette` assets.
- Light and dark `DeucarianTheme` variants that point to those palettes.
- A `DeucarianThemeFamily` that pairs the variants.
- One shared `DeucarianColorRoleLibrary` used by both palettes.
- Built-in role assets for common UI concepts such as background, surface, primary text, error, and button states.
- One shared adaptive visual style by default; either concrete variant may override it later.

These are support assets. Normal users mostly edit the two palettes.

Family support assets are generated together. If anything is missing later, use `Repair Theme Family`. It repairs required roles, role-library links, palette entries, theme links, and the missing mode without overwriting user-chosen colors unless an entry is missing, null, or still using the package missing-color fallback.

Fresh default families use the versioned Deucarian Brand light/dark token snapshot. Existing standalone themes and the legacy `Create Minimal Palette` API remain available for compatibility and advanced workflows. Wrapping an existing standalone theme requires explicitly identifying it as light or dark; the package never guesses or algorithmically derives its opposite.

Generic starter assets and optional game-specific roles remain available under Theme Manager's collapsed **Developer Tools** when a project actually needs them.

## Runtime default theme

Builds can resolve a project default family through a `DeucarianThemeRuntimeSettings` asset named `DeucarianThemeRuntimeSettings.asset` in any `Resources` folder. Assign its default family and explicit mode; new settings default to dark. The legacy standalone default-theme field remains supported.

The Theme Manager hydrates missing or invalid staged fields from this source-controlled project default while preserving valid machine-local Family, Mode, and Visual Style choices. **Activate** is the single commit point for the next player build: it validates the selected complete family, shares the chosen style across Light and Dark, writes the runtime default, and synchronizes loaded providers. An existing but previously unconfigured or incomplete runtime-settings asset can therefore be repaired directly by activating a valid selected family. Missing or ambiguous runtime settings expose one contextual **Configure Runtime Settings...** workflow; creation and legacy utilities remain under **Developer Tools** rather than being presented as project health requirements.

Runtime code can call:

- `DeucarianThemeRuntimeResolver.ResolveDefaultTheme(...)`
- `DeucarianThemeRuntimeResolver.ResolveTheme(...)`
- `DeucarianThemeRuntimeResolver.EnsureProviderHasTheme(...)`

`DeucarianThemeTargetBehaviour` also falls back to this runtime default when no provider theme is available.

## Theme packs

Packages that need their own semantic roles can provide a `DeucarianThemePack` instead of writing package-specific theme menus. A paired pack describes explicit light/dark role colors, both palette/theme variants, family metadata, and the shared default visual style. Editor tooling can import or repair it with `DeucarianThemePackAssetFactory.CreateOrRepairThemePackAssets(...)` and can create family-aware runtime settings with `CreateOrRepairRuntimeSettings(...)`. Legacy single-theme pack configuration remains supported.

Theme packs should keep product-specific role IDs in the owning package, for example `reportviewer.navigation.active`. Deucarian Theming owns the generic import and repair mechanism; it should not absorb every package's domain-specific role names as built-ins.

## Visual styles

Colors and styles are deliberately separate.

- A `DeucarianColorPalette` decides semantic colors such as surface, primary, error, and text.
- A `DeucarianThemeStyle` decides how chrome is treated: opacity, tinting, borders, corner radius, and optional generated texture.
- A `DeucarianTheme` can reference both a palette and a style.
- A `DeucarianThemeFamily` pairs one light and one dark concrete theme. Generated variants share a style by default, but either theme may override it.

The built-in style presets are:

- `Frosted Glass`: translucent glass-like surfaces with cool tinting, fine texture, and soft borders.
- `Material Dark`: opaque layered dark surfaces with restrained radius and crisp dividers.
- `Fluent Acrylic`: acrylic-inspired translucent surfaces with subtle tint and texture.

These names remain curated presets, but each preset is a composition of four reusable axes: Surface, Corners, Border, and Size. The built-ins preserve their existing output exactly. Choose **Customize Style** in Theme Manager to compose a source-controlled **Custom Style**, such as Frosted Glass + Square + Compact. Composer choices remain staged until **Save & Activate**, and the resulting Custom Style stays shared by Light and Dark modes.

Existing third-party style assets continue to resolve their legacy inline fields when component references or density are absent. Providers observe edits to referenced component assets and emit the existing `StyleChanged` notification so active consumers refresh immediately.

This is a good fit for shared visual language, because packages such as a report viewer can keep their own layout and toolbar behavior while asking Theming for the surface treatment. It would be a bad fit if the style asset started owning product-specific UI structure, navigation rules, or a generic UI framework.

Use Theme Manager's collapsed **Developer Tools** for asset creation and repair. Activate a preset or Custom Style through the main workflow, or switch at runtime with `DeucarianThemeProvider.SetStyle`. If no provider style override is set, `DeucarianThemeProvider.CurrentStyle` resolves from the current theme's `VisualStyle`.

`DarkSurfaceTint` and `LightSurfaceTint` describe the tint selected for a visually dark or light incoming surface color. They are source-luminance treatments, not application modes, and they do not generate a palette. Light/dark mode always comes from the active theme family.

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

Use `DeucarianThemeProvider.SetThemeMode` to switch a paired family at runtime. Operating-system detection and preference persistence stay in application code.

```csharp
using Deucarian.Theming;
using UnityEngine;

public sealed class ThemeSwitcher : MonoBehaviour
{
    [SerializeField] private DeucarianThemeProvider provider;
    [SerializeField] private DeucarianThemeFamily themeFamily;

    private void Awake()
    {
        provider.SetThemeFamily(themeFamily);
    }

    public void UseLightTheme()
    {
        provider.SetThemeMode(DeucarianThemeMode.Light);
    }
}
```

`SetThemeFamily` and `SetThemeMode` resolve a concrete variant and reapply it to child components that implement `IDeucarianThemeTarget`. Theme targets therefore keep the existing `DeucarianTheme` contract. `SetTheme` remains available for legacy standalone themes.

## Advanced workflow

The paired family workflow is recommended, but the full asset model remains available.

Advanced users can manually create and manage:

- `DeucarianColorRole`
- `DeucarianColorRoleLibrary`
- `DeucarianColorPalette`
- `DeucarianTheme`
- `DeucarianThemeFamily`
- `DeucarianThemeStyle`
- `DeucarianThemeProvider`

Manual workflow:

1. Create or refine color role assets with `Assets/Create/Deucarian/Theming/Color Role`.
2. Add role assets to a `DeucarianColorRoleLibrary`.
3. Add roles to a `DeucarianColorPalette` and choose the colors.
4. Link a light and dark `DeucarianTheme` to their palettes and pair them in a `DeucarianThemeFamily`.
5. Assign roles to uGUI, TMP, renderer, or UI Toolkit adapters.
6. Switch modes by calling `DeucarianThemeProvider.SetThemeMode`.

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
- If generated family support assets are missing, use `Repair Theme Family` instead of recreating role libraries or variants manually.
- If a package needs domain-specific color roles, create a `DeucarianThemePack` in the owning package rather than adding those roles as Theming built-ins.

## Validation

Run the shared package validator from the repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Run the package's Runtime and EditMode tests in Unity after code or assembly definition changes. Runtime tests cover palette, family, mode, provider, and fallback behavior; editor tests cover paired creation, repair, migration, default assets, active mode settings, theme packs, and manager workflows.

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
