# Deucarian Theming Agent Notes

Package ID: `com.deucarian.theming`
Repository: `Deucarian/Theming`

Follow the canonical Deucarian governance docs in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/main/ARCHITECTURE.md), especially capability ownership and dependency rules.

## Ownership

This package owns:

- Runtime themes, palettes, color roles, theme assets, and runtime UI adapters.

Registered capabilities:
- `runtime-theming`

This package must not own:

- Deucarian editor chrome theming, package installation, or diagnostics ownership.

## Dependencies

Allowed dependency shape:

- May depend on Editor for editor manager UI, Logging for diagnostics, and UI modules it directly adapts.

Required dependencies and why:

- `com.deucarian.editor`: shared editor shell/resources.
- `com.deucarian.logging`: package logging facade and diagnostics output.
- `com.unity.modules.uielements`: Unity UIElements module used by theming adapters/editor UI.
- `com.unity.textmeshpro`: TextMeshPro package used by theming adapters.
- `com.unity.ugui`: UGUI package used by UI adapters.

Optional/version-defined dependencies:

- None.

Architecture exceptions:

- None.

## Policies

- Logging: Use Logging; no direct Unity Debug calls.
- Common: Do not add Common unless runtime theming directly owns transient Unity cleanup.
- Editor UI: Editor manager may use Editor shell; do not move editor chrome ownership here.
- Diagnostics: No diagnostics ownership.
- Testing: Tests should cover runtime theme and editor manager behavior without redefining editor shell tokens.

## Validation

Run the shared validator before committing:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Also run existing repository tests when changing code or asmdefs. Documentation-only updates should still run `git diff --check`.

## Codex Guidance

- Inspect current files before changing anything.
- Work on `develop`; do not edit or merge `main` unless the task is promotion-only.
- Do not edit `Library/PackageCache`.
- Do not guess package versions or dependency versions.
- Do not add package dependencies casually; update asmdefs, `package.json`, `deucarian-package.json`, Package Registry, and fallback catalogs together when a dependency is truly required.
- Do not create local copies of shared helpers.
- Keep commits focused and report exactly what changed and what was validated.

## Before Adding Code

- Confirm the change fits this package's ownership boundary.
- Reuse existing local patterns and helpers.
- Avoid broad refactors without audit support.
- Preserve runtime/editor behavior unless the task explicitly asks to change it.

## Before Adding A Dependency

- Is the capability already owned by that package?
- Is it used by production code, editor code, sample code, or tests?
- Does the asmdef reference match `package.json`?
- Does `deucarian-package.json` need updating?
- Does Package Registry need updating?
- Does Package Installer fallback catalog need updating?
- Does Bootstrap fallback catalog need updating?
- Are exact versions propagated without guessing?

## Before Adding A Helper

- Is this package the capability owner?
- Is this behavior repeated in at least three production packages?
- Is there an existing owner package?
- Should this remain local?
- Has the audit been updated?

## Debug And Unity Object Lifetime

- Use Deucarian Logging for package diagnostics; direct Unity Debug calls are forbidden.
- Do not copy Common lifetime helpers. Add Common only if production code directly owns transient Unity object cleanup.
- Test fixture teardown may use `DestroyImmediate` directly.
