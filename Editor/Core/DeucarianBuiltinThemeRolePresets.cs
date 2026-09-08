using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

using BuiltinRoleDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.BuiltinRoleDefinition;
using ThemeFamilyPresetDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.ThemeFamilyPresetDefinition;
using ThemePresetDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.ThemePresetDefinition;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianBuiltinThemeRolePresets
    {
        internal static IReadOnlyList<BuiltinRoleDefinition> CreateMinimalDefaultRoleDefinitions()
        {
            return CreateMinimalDefaultRoleDefinitions(DeucarianThemeMode.Dark);
        }

        internal static IReadOnlyList<BuiltinRoleDefinition> CreateMinimalDefaultRoleDefinitions(
            DeucarianThemeMode mode)
        {
            return new[]
            {
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Background, "Background", DeucarianColorRoleCategories.Semantic, "Main UI or scene background surface.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Background)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Surface, "Surface", DeucarianColorRoleCategories.Semantic, "Default panel or card surface.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Surface)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.SurfaceRaised, "Surface Raised", DeucarianColorRoleCategories.Semantic, "Elevated panel or overlay surface.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.SurfaceRaised)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Primary, "Primary", DeucarianColorRoleCategories.Semantic, "Primary action or brand emphasis.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Primary)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Secondary, "Secondary", DeucarianColorRoleCategories.Semantic, "Secondary action or brand emphasis.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Secondary)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Accent, "Accent", DeucarianColorRoleCategories.Semantic, "Subtle accent or supporting emphasis.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Core.Accent)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Primary, "Text Primary", DeucarianColorRoleCategories.Text, "Primary readable text.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Text.Primary)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Secondary, "Text Secondary", DeucarianColorRoleCategories.Text, "Secondary readable text.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Text.Secondary)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Muted, "Text Muted", DeucarianColorRoleCategories.Text, "Muted helper or supporting text.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Text.Muted)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Disabled, "Text Disabled", DeucarianColorRoleCategories.Text, "Disabled or unavailable text.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Text.Disabled)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Success, "Success", DeucarianColorRoleCategories.Status, "Positive state or confirmation.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Status.Success)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Warning, "Warning", DeucarianColorRoleCategories.Status, "Warning state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Status.Warning)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Error, "Error", DeucarianColorRoleCategories.Status, "Error or destructive state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Status.Error)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Info, "Info", DeucarianColorRoleCategories.Status, "Informational state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.Status.Info)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Normal, "UI Normal", DeucarianColorRoleCategories.UiState, "Default selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Normal)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Highlighted, "UI Highlighted", DeucarianColorRoleCategories.UiState, "Hovered or highlighted selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Highlighted)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Pressed, "UI Pressed", DeucarianColorRoleCategories.UiState, "Pressed selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Pressed)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Selected, "UI Selected", DeucarianColorRoleCategories.UiState, "Selected selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Selected)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Disabled, "UI Disabled", DeucarianColorRoleCategories.UiState, "Disabled selectable UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Disabled)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Focused, "UI Focused", DeucarianColorRoleCategories.UiState, "Keyboard or controller focused UI state.", BrandColor(mode, DeucarianBuiltinColorRoleIds.UI.Focused))
            };
        }

        internal static Color BrandColor(DeucarianThemeMode mode, string roleId)
        {
            if (DeucarianBrandThemePreset.TryGetColor(mode, roleId, out Color color))
            {
                return color;
            }

            throw new InvalidOperationException(
                $"Brand theme preset {DeucarianBrandThemePreset.Version} has no {mode} value for role '{roleId}'.");
        }

        internal static IReadOnlyList<BuiltinRoleDefinition> CreateGameRoleDefinitions()
        {
            return new[]
            {
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Health, "Health", DeucarianColorRoleCategories.Gameplay, "Health resource color.", new Color32(180, 67, 76, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Mana, "Mana", DeucarianColorRoleCategories.Gameplay, "Mana resource color.", Hex("#5A6FA0")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Stamina, "Stamina", DeucarianColorRoleCategories.Gameplay, "Stamina resource color.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Experience, "Experience", DeucarianColorRoleCategories.Gameplay, "Experience resource color.", new Color32(168, 121, 50, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Interactable, "Interactable", DeucarianColorRoleCategories.Gameplay, "Interactive game affordance color.", Hex("#276065")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Gameplay.Highlight, "Highlight", DeucarianColorRoleCategories.Gameplay, "Selected or highlighted game element color.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Faction.Ally, "Ally", DeucarianColorRoleCategories.Faction, "Friendly team or unit color.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Faction.Enemy, "Enemy", DeucarianColorRoleCategories.Faction, "Hostile team or unit color.", new Color32(180, 67, 76, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Faction.Neutral, "Neutral", DeucarianColorRoleCategories.Faction, "Neutral team or unit color.", Hex("#A8B0BA")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Common, "Item Common", DeucarianColorRoleCategories.ItemRarity, "Common item rarity color.", Hex("#C4CAD1")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Uncommon, "Item Uncommon", DeucarianColorRoleCategories.ItemRarity, "Uncommon item rarity color.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Rare, "Item Rare", DeucarianColorRoleCategories.ItemRarity, "Rare item rarity color.", Hex("#5A6FA0")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Epic, "Item Epic", DeucarianColorRoleCategories.ItemRarity, "Epic item rarity color.", new Color32(128, 103, 169, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRarity.Legendary, "Item Legendary", DeucarianColorRoleCategories.ItemRarity, "Legendary item rarity color.", new Color32(168, 121, 50, 255))
            };
        }

        internal static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                throw new ArgumentException("Invalid color value: " + hex, nameof(hex));
            }

            return color;
        }
    }
}
