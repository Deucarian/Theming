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
    "com.deucarian.theming": "0.1.0"
  }
}
```

## Basic Setup

1. In Unity, use `Tools/Deucarian/Theming/Create Default Theme Assets`.
2. Add a `DeucarianThemeProvider` to a scene object and assign the generated theme.
3. Add theme color components to TMP text, uGUI graphics, sprites, or renderers.
4. Assign a `DeucarianColorRole` asset to each theme color component.

## Designer Workflow

1. Create a color role asset with `Assets/Create/Deucarian/Theming/Color Role`.
2. Add the role asset to a `DeucarianColorRoleLibrary`.
3. Add the role to a `DeucarianColorPalette` and choose the color.
4. Reference that role from a TMP, Graphic, SpriteRenderer, or Renderer theme component.

## Why Role Assets Instead of Enums

Enums make every new color role a code change. This package keeps roles as assets with stable string IDs such as `deucarian.semantic.primary` or `deucarian.game.health`. Code can use optional constants from `DeucarianBuiltinColorRoleIds`, but designers can add new roles without modifying or recompiling C#.

## TMP Usage

```csharp
using Deucarian.Theming;
using TMPro;
using UnityEngine;

public sealed class LabelSetup : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private DeucarianColorRole primaryTextRole;

    private void Reset()
    {
        label = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        var themeColor = label.gameObject.AddComponent<DeucarianTMPThemeColor>();
        themeColor.ColorRole = primaryTextRole;
    }
}
```

## uGUI Usage

Add `DeucarianGraphicThemeColor` to any object with a `UnityEngine.UI.Graphic`, then assign a role asset. The component applies the resolved palette color to `Graphic.color`.

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

`DeucarianThemeProvider.SetTheme` reapplies the theme to child components that implement `IDeucarianThemeTarget`.

## Renderer Colors

`DeucarianRendererThemeColor` uses `MaterialPropertyBlock` and does not instantiate materials. The default property is `_BaseColor`; change it to `_Color` for legacy shaders or to another shader color property when needed.
