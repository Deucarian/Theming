# Changelog

## 1.0.2 - 2026-07-16

- Prevented Unity layout restoration from reopening Theme Manager on editor startup while preserving explicitly opened windows across domain reloads.
- Removed the opaque full-content IMGUI background so the shared Deucarian wallpaper remains visible behind Theme Manager content.
- Moved Theme Manager status, asset counts, refresh action, and package version into the shared fixed workbench footer instead of rendering a pseudo-footer inside the scroll view.

## 1.0.1 - 2026-07-15

- Added nonserialized Theme Manager live preview across loaded providers, including domain-reload and Play Mode restoration, runtime-setter precedence, scene/prefab save guards, and build-time suspension through the actual end of the player pipeline so staged choices never leak into serialized or player configuration.
- Made Theme Manager activation validate the selected family instead of the previously configured runtime family, and made editor mutations play-safe while keeping provider refreshes free of scene dirtiness.
- Made Theme Manager scroll scopes exception-safe and removed its redundant outer heading card to prevent IMGUI layout-state errors after view changes.
- Adopted the shared responsive Editor workbench for Theme Manager navigation, contextual summaries/actions, and exception-safe 24 px panels and controls without duplicating Editor-package styling constants.
- Updated the shared Editor dependency to `1.0.2` for the finalized workbench contract.
- Redesigned Theme Manager around EditorPrefs-backed staged Family, Mode, and Visual Style choices, independent dirty markers, one validated **Activate** transaction, compact cards, searchable asset pickers, contextual setup, and collapsed developer tools.
- Added a focused four-axis Custom Style composer whose existing-style edits commit atomically with theme activation and whose preview reflects surface, corner, border-width, texture, and size choices.
- Clarified composed-style authoring with explicit legacy, preset, custom, and incomplete composition states while preserving variant serialization and APIs.
- Added focused style and presentation-profile inspectors that hide overridden inline fallbacks and use the user-facing Surface, Corners, Border, and Size vocabulary.
- Made Borderless presentation profiles fully clear uGUI `Outline` effects instead of leaving a zero-offset tint pass active, while preserving caller-owned component state.
- Made visual styles composable from reusable surface, shape, and stroke profiles plus Comfortable, Standard, or Compact density intent.
- Added explicit source-controlled custom style creation in Theme Manager and live provider refresh for referenced component edits.
- Preserved legacy inline style resolution and the existing Frosted Glass, Fluent Acrylic, and Material Dark preset identities and output.
- Added searchable, category-filtered inspector lists for palettes, role libraries, theme packs, and UI Toolkit theme mappings.
- Added backward-compatible light/dark `DeucarianThemeFamily` assets, explicit provider mode switching, family-aware runtime defaults, and resilient incomplete-family fallback.
- Reworked primary authoring around paired light/dark palette and theme generation, preview, repair, migration, and paired theme packs.
- Added versioned Deucarian Brand token defaults without introducing a Brand package dependency.
- Added `DeucarianThemeStyle` visual style assets and optional `DeucarianTheme.VisualStyle` references.
- Added built-in Frosted Glass, Material Dark, and Fluent Acrylic style generation.
- Added provider style overrides and UI Toolkit/uGUI style helpers for package-specific panel chrome.
- Updated Theme Manager workflows and tests for discovering, selecting, and assigning style assets.

## 1.0.0 - 2026-06-22

- Marked the current package metadata as 1.0.0 for the stable Theming package line.
- Updated package documentation to reference the current scoped-registry version and dependency roles.

## 0.4.2

- Removed duplicate top-level Theming editor menu entries.
- Kept Theming package tools under the ecosystem standard path: `Tools/Deucarian/Theming/...`.
- Documented the current Deucarian tool menu standard as `Tools/Deucarian/<PackageName>/...`.

## 0.4.1

- Reworked README onboarding around the palette-first quick-start path.
- Made it explicit that most users do not need to manually create color roles, role libraries, or theme assets.
- Aligned Theme Manager and menu wording around `Create Minimal Palette`, `Repair Palette Setup`, and `Apply Theme To Scene`.
- Simplified the Theming top menu to the Theme Manager and minimal palette quick entries.
- Kept setup, repair, selection, folder, and scene-apply workflows available inside Theme Manager.
- Standardized package logging on com.deucarian.logging.
- Added `ThemingLog` package categories for runtime, editor, and UI Toolkit diagnostics.

## 0.4.0

- Added a palette-first workflow that creates or repairs support role, library, and theme assets from one editable palette.
- Added `Create Minimal Palette`, `Repair Palette Setup`, `Create Palette From Active Theme`, and generated asset name repair menu tools.
- Updated Theme Manager to prioritize the active palette and expose palette setup actions near the active asset fields.
- Fixed generated ScriptableObject object names so they match their asset filenames while keeping display names separate.
- Added editor tests for minimal palette creation, palette repair, user color preservation, generated asset names, and minimal role scope.

## 0.3.0

- Split built-in theme generation into minimal default theme assets and optional game theme assets.
- Updated the minimal default palette to Deucarian brand colors with no gameplay, item rarity, or faction roles.
- Added grouped built-in role ID constants for core, text, status, UI, gameplay, item rarity, and faction roles while keeping flat constants available.
- Added a Theme Manager action and menu entry for creating game theme assets.
- Kept current theme preset tooling under `Tools > Deucarian > Theming`.
- Added editor tests for the minimal default role set, game preset role set, brand palette colors, and non-magenta defaults.

## 0.2.4

- Moved high-level Theming editor menu entries under `Tools > Deucarian > Theming`.
- Updated the shared editor helper dependency to `com.deucarian.editor` `0.1.1`.
- Updated Theme Manager package version chrome.
- Updated README and sample menu guidance, usage, tests, and license sections.

## 0.2.3

- Added a dependency on `com.deucarian.editor` for fixed Deucarian editor chrome and shared editor UI helpers.
- Updated the Theme Manager to use `DeucarianEditorChrome`, `DeucarianEditorFields`, `DeucarianEditorIcons`, and `DeucarianEditorStatusBadge`.
- Updated Theming menu placement.
- Removed the local duplicate editor asset field helper.

## 0.2.2

- Kept only high-level Theming entries under `Tools > Deucarian > Theming`.
- Moved default asset creation, UI Toolkit demo asset creation, active asset selection, and scene-apply workflows into `DeucarianThemeManagerWindow`.
- Renamed visible `Ping` editor buttons to `Select`.
- Cleaned up the Theme Manager active asset rows to use inline Select/Ping buttons beside object fields.
- Added a reusable `DrawAssetFieldWithSelectButton<T>()` editor IMGUI helper for future Deucarian tooling windows.
- Documented the Deucarian editor tooling guideline against separate Select button rows for assets already shown in object fields.

## 0.2.1

- Added top-level menu tools for finding, selecting, creating, and applying theme assets.
- Added `DeucarianThemeManagerWindow` with active theme, palette, role library, asset counts, and scene-apply actions.
- Added GUID-backed editor settings for active theme, palette, role library, and the default theme asset folder.
- Kept existing `Tools/Deucarian/Theming` compatibility menu items working.
- Moved the UI Toolkit demo asset creation action into the Theming menu.
- Added editor tests for menu settings, asset discovery, default creation, active theme creation, and provider assignment.

## 0.2.0

- Added first-class UI Toolkit theming support in `Deucarian.Theming.UIToolkit`.
- Added `DeucarianUIToolkitThemeApplier` for UIDocument and VisualElement binding support.
- Added background, text, border, image tint, selector/name/class/root bindings, and safe missing-element handling.
- Added `DeucarianUIToolkitThemeVariables` for previewing and generating USS custom variable values. Runtime USS variable assignment is documented as a Unity 2022.3 limitation.
- Organized runtime/editor code into Core, UGUI, TMP, Rendering, and UIToolkit folders while keeping existing public component names.
- Added UI Toolkit demo asset creation, sample docs, and editor inspectors.
- Added UI Toolkit utility/editor tests and additional adapter documentation.

## 0.1.0

- Initial release of `com.deucarian.theming`.
- Added ScriptableObject color roles, role libraries, color palettes, and themes.
- Added TMP, uGUI Graphic, SpriteRenderer, and Renderer color adapters.
- Added Selectable ColorBlock theming for Button, Toggle, Dropdown, InputField, Scrollbar, Slider, and custom Selectables.
- Added provider-aware theme target base behavior with automatic provider subscription.
- Added UI state and item rarity default color roles.
- Added default theme asset creation tooling and validation inspectors.
- Added runtime and editor test coverage for palette behavior and default asset creation.
