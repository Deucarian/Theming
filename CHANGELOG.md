# Changelog

## [1.8.0] - Unreleased

- Add editable audio-role declarations, generated project-role lookup and typed Inspector playback. Audio-only hosts can use project roles without a visual theme.
- Include a playable Definition Workflow sample with configured hosts, short callers and usage documentation.
- Align declared package dependencies with the definition-authoring development wave.

- Play processed editor auditions through a temporary 2D AudioSource with native volume/pitch instead of routing a generated AudioClip through imported-asset preview. Original audition stays unchanged; no clip reimports or scene/listener edits are needed.
- Cover audition ownership, repeated playback, modifier application and non-silent native editor output.

- Include a complete Deucarian Default light/dark visual family, canonical roles/style/Inter typography and linked bundled audio palettes without importing samples.
- Fall back to bundled visuals for unconfigured projects without replacing explicit choices or enabling disabled features.
- Replace placeholder samples with wired visual, UI Toolkit and audio scenes; add bundled-default contract tests.


## [1.7.0] - 2026-09-11

- Require Editor 1.11.0 for the integrated shared editor workspace; preserve the typed audio role APIs from develop.

- Add package-owned Simultria DS and RP palette factories with authored light/dark colors, HoloHelmet-compatible DS control roles and non-overwriting project copies.
- Show Background behind Surface in the visual specimen and use descriptive palette page captions.
- Expose a visual-styling-disabled lifecycle hook for adapters to restore authored presentation or release scoped registrations.

## [1.6.1] - 2026-09-11

- Match visual/audio palette pages and custom Inspectors to the reference compositions; use source-owned cue/color forms and live isolated specimens. Disabled capabilities show a clear inactive state with editing blocked.
- Require Editor 1.10.6 for the shared native controls, typography, responsive layouts and accessible interaction states.

## [1.6.0] - 2026-09-10

- Remove the redundant Project setup information strip. Disabled visual/audio palettes keep their assets but gate editing and audition, with a clear route to Project setup.
- Stop editor audio when project audio is disabled and use shared intensity sliders.

## [1.5.0] - 2026-09-10

- Add Theming > Project setup, with independent saved visual styling and audio switches; keep visual and audio palettes together in one package.
- Add project audio palette/experience defaults while preserving component overrides and mixed-theme audio resolution when visual styling is off.
- Gate runtime visual adapters and all themed audio playback entry points. Audio Off stops active player voices; palette assets and editor previews remain available.
- Group Project setup, Visual palettes and Audio palettes in the same-window submenu using Editor 1.8 shared feature sections.
- Report loaded button integrations and successful keyboard/warning cue observations without mistaking an assigned palette for a connection. Release observers when leaving the page.

## [1.4.4] - 2026-09-09

### Changed

- Adopt the shared Editor 1.7 workspace presentation: neutral surfaces, readable typography, consistent actions and aligned controls.
- Preserve package workflows and native serialized editing; this is an editor-only presentation update.

## [1.4.3] - 2026-09-09

- Refresh Audio Palette Lab data without rebuilding its controls. Preserve expanded sections, role selection, test-pad controls and intensity values across navigation and refresh.
- Require Editor 1.6.1 for window-owned navigation state and consistent page activation.
- Test repeated switching with an in-memory palette and real role library; no project assets or automatic audio playback are needed.

## [1.4.2] - 2026-09-09

- Register package tooling and navigation actions as shared Control Center pages. Preserve the domain workflow while using Editor-owned submenus, in-window navigation, and UI scaling.

## [1.4.1] - 2026-09-09

- Keep sidebar navigation in the current workspace and retain page drafts while switching tools.
- Support explicitly opening independent workspaces through the sidebar context menu.


## 1.4.0 - 2026-09-09

- Migrate Theme Manager and Audio Palette Lab to the shared Editor workspace. Separate preview selection from explicit activation, show role clips and source provenance, and retain composer, coverage and manual audition workflows.

## 1.3.1 - 2026-09-09

- Separate default/theme-pack asset creation, repair, naming and storage; remove duplicated asset/path operations while preserving public factory entry points.
- Compose Theme Manager toolbar controls, asset fields, summaries and developer tools independently of window workflows. Add asset identity and toolbar-state regression coverage.

## 1.3.0 - Unreleased

- Split asset discovery, authoring, selection, activation and scene application. Add original/processed audio audition, project-local editor state and compact lab workflows.

## 1.2.0 - Unreleased

- Exposed the existing editor audio preview service contract for reuse by package labs.

- Added semantic audio roles, multi-variant cues, pitch variation, and safe intentional silence.
- Added explicit Default, XR, WebGL, Desktop, and Mobile palette profiles with deterministic fallback provenance.
- Added Media-owned pooled one-shot output integration and canonical button activation semantics.
- Added the shared-editor Audio Palette Lab for per-experience validation and audition.
- Added original procedural default clips, profile assets, provenance documentation, focused inspectors, test pad, source tracing, and Theme Manager audio summary.
- Added optional per-playback intensity modifiers so interaction velocity can scale palette-authored volume and pitch without replacing platform cues.
- Unified every bundled semantic role and platform default on one canonical key-click clip, with role-specific pitch and volume variation.

## 1.1.2 - 2026-08-31

- Registered the package workflow and a bounded, sanitized local-state card with Deucarian Control Center.
- Removed normal `Tools/Deucarian` menu exposure while preserving the standalone open API.
- Updated the shared Editor dependency to 1.2.0.
- Aligned the Logging dependency to 1.0.4.

## 1.1.1 - 2026-08-26

- Incremented the package identity for the shared reference-viewer runtime so
  Unity refreshes projects that previously cached Theming 1.1.0 without it.

## 1.1.0 - 2026-08-24

- Added one reusable reference-viewer theme composition that owns the shared
  provider, persisted light/dark mode, CSS snapshot projection, and
  transport-neutral snapshot publication.
- Added consumer-neutral theme/provider/color resolution so viewer products no
  longer need copied role aliases, fallback tables, or runtime theme helpers.

## 1.0.5 - 2026-08-17

- Added a generic UI Toolkit typography adapter that applies the font from a
  Deucarian visual style and respects nearby provider style overrides.

## 1.0.4 - 2026-08-17

- Added a cached, consumer-neutral reference viewer theme family with canonical light
  and dark semantic palettes and the shared Frosted Glass chrome style.
- Kept the runtime preset on built-in Deucarian role IDs so products can share viewer
  presentation without importing product-specific roles or assets.
- Restored Style Composer as an explicit Theme Manager destination and clarified that saving creates or updates one complete reusable Custom Style asset shared by Light and Dark themes.
- Added nonserialized editor-wide preview of unsaved Style Composer compositions while preserving source assets, provider configuration, scene saves, and builds.
- Disabled runtime-settings creation once the project's single Resources-backed settings asset exists and clarified its startup family and mode role.

## 1.0.3 - 2026-07-17

- Reused the shared Editor selection-and-ping helper, completed importable sample scenes, and aligned exact dependencies.
- Aligned Theme Manager toolbar coverage with the intended staged-change discard state.

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
