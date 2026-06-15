namespace Deucarian.Theming
{
    /// <summary>
    /// Convenience string IDs for the built-in Deucarian color roles.
    /// These constants are not a source-of-truth enum; designers can create more role assets without changing code.
    /// </summary>
    public static class DeucarianBuiltinColorRoleIds
    {
        public const string Background = Core.Background;
        public const string Surface = Core.Surface;
        public const string SurfaceRaised = Core.SurfaceRaised;
        public const string Primary = Core.Primary;
        public const string Secondary = Core.Secondary;
        public const string Accent = Core.Accent;
        public const string TextPrimary = Text.Primary;
        public const string TextSecondary = Text.Secondary;
        public const string TextMuted = Text.Muted;
        public const string TextDisabled = Text.Disabled;
        public const string Success = Status.Success;
        public const string Warning = Status.Warning;
        public const string Error = Status.Error;
        public const string Info = Status.Info;
        public const string UiNormal = UI.Normal;
        public const string UiHighlighted = UI.Highlighted;
        public const string UiPressed = UI.Pressed;
        public const string UiSelected = UI.Selected;
        public const string UiDisabled = UI.Disabled;
        public const string UiFocused = UI.Focused;
        public const string Health = Gameplay.Health;
        public const string Mana = Gameplay.Mana;
        public const string Stamina = Gameplay.Stamina;
        public const string Experience = Gameplay.Experience;
        public const string Interactable = Gameplay.Interactable;
        public const string Highlight = Gameplay.Highlight;
        public const string Ally = Faction.Ally;
        public const string Enemy = Faction.Enemy;
        public const string Neutral = Faction.Neutral;
        public const string ItemCommon = ItemRarity.Common;
        public const string ItemUncommon = ItemRarity.Uncommon;
        public const string ItemRare = ItemRarity.Rare;
        public const string ItemEpic = ItemRarity.Epic;
        public const string ItemLegendary = ItemRarity.Legendary;

        public static class Core
        {
            public const string Background = "deucarian.background";
            public const string Surface = "deucarian.surface";
            public const string SurfaceRaised = "deucarian.surface.raised";
            public const string Primary = "deucarian.primary";
            public const string Secondary = "deucarian.secondary";
            public const string Accent = "deucarian.accent";
        }

        public static class Text
        {
            public const string Primary = "deucarian.text.primary";
            public const string Secondary = "deucarian.text.secondary";
            public const string Muted = "deucarian.text.muted";
            public const string Disabled = "deucarian.text.disabled";
        }

        public static class Status
        {
            public const string Success = "deucarian.success";
            public const string Warning = "deucarian.warning";
            public const string Error = "deucarian.error";
            public const string Info = "deucarian.info";
        }

        public static class UI
        {
            public const string Normal = "deucarian.ui.normal";
            public const string Highlighted = "deucarian.ui.highlighted";
            public const string Pressed = "deucarian.ui.pressed";
            public const string Selected = "deucarian.ui.selected";
            public const string Disabled = "deucarian.ui.disabled";
            public const string Focused = "deucarian.ui.focused";
        }

        public static class Gameplay
        {
            public const string Health = "deucarian.game.health";
            public const string Mana = "deucarian.game.mana";
            public const string Stamina = "deucarian.game.stamina";
            public const string Experience = "deucarian.game.experience";
            public const string Interactable = "deucarian.game.interactable";
            public const string Highlight = "deucarian.game.highlight";
        }

        public static class ItemRarity
        {
            public const string Common = "deucarian.item.common";
            public const string Uncommon = "deucarian.item.uncommon";
            public const string Rare = "deucarian.item.rare";
            public const string Epic = "deucarian.item.epic";
            public const string Legendary = "deucarian.item.legendary";
        }

        public static class Faction
        {
            public const string Ally = "deucarian.game.ally";
            public const string Enemy = "deucarian.game.enemy";
            public const string Neutral = "deucarian.game.neutral";
        }
    }
}
