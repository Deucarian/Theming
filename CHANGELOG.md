# Changelog

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
