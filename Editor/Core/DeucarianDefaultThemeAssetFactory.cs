using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Result object returned by theme preset asset creation.
    /// </summary>
    public sealed class DeucarianDefaultThemeAssets
    {
        private readonly List<DeucarianColorRole> roles = new List<DeucarianColorRole>();

        public DeucarianColorRoleLibrary RoleLibrary { get; internal set; }
        public DeucarianColorPalette Palette { get; internal set; }
        public DeucarianTheme Theme { get; internal set; }
        public IReadOnlyList<DeucarianColorRole> Roles => roles;

        internal void AddRole(DeucarianColorRole role)
        {
            if (role != null)
            {
                roles.Add(role);
            }
        }
    }

    /// <summary>
    /// Creates built-in Deucarian theme preset assets.
    /// </summary>
    public static class DeucarianDefaultThemeAssetFactory
    {
        public const string DefaultRootFolder = "Assets/Deucarian/Theming/Defaults";
        public const string GameRootFolder = "Assets/Deucarian/Theming/Game";

        public static void CreateDefaultThemeAssetsFromMenu()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets(DefaultRootFolder);
            if (assets.Theme != null)
            {
                DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
            }
        }

        public static void CreateGameThemeAssetsFromMenu()
        {
            DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets(GameRootFolder);
            if (assets.Theme != null)
            {
                DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
            }
        }

        /// <summary>
        /// Creates minimal generic role, library, palette, and theme assets under the requested Assets folder.
        /// Existing assets are reused, with missing role references and palette entries filled in.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateDefaultThemeAssets(string rootFolder, bool overwriteExisting = false)
        {
            return CreateThemeAssets(
                rootFolder,
                overwriteExisting,
                CreateMinimalDefaultRoleDefinitions(),
                new ThemePresetDefinition(
                    "Default Color Role Library.asset",
                    "Default Dark Color Palette.asset",
                    "Default Theme.asset",
                    "deucarian.palette.default",
                    "Deucarian Default",
                    "deucarian.theme.default",
                    "Default"));
        }

        /// <summary>
        /// Creates optional gameplay, faction, and item rarity role assets under the requested Assets folder.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateGameThemeAssets(string rootFolder, bool overwriteExisting = false)
        {
            return CreateThemeAssets(
                rootFolder,
                overwriteExisting,
                CreateGameRoleDefinitions(),
                new ThemePresetDefinition(
                    "Game Color Role Library.asset",
                    "Game Color Palette.asset",
                    "Game Theme.asset",
                    "deucarian.palette.game",
                    "Game Default",
                    "deucarian.theme.game",
                    "Game Theme"));
        }

        private static DeucarianDefaultThemeAssets CreateThemeAssets(
            string rootFolder,
            bool overwriteExisting,
            IReadOnlyList<BuiltinRoleDefinition> definitions,
            ThemePresetDefinition preset)
        {
            string normalizedRoot = NormalizeAssetPath(rootFolder);
            if (string.IsNullOrEmpty(normalizedRoot) || (normalizedRoot != "Assets" && !normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme assets must be created under the Assets folder.", nameof(rootFolder));
            }

            string rolesFolder = CombineAssetPath(normalizedRoot, "Roles");
            EnsureFolder(rolesFolder);

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets();

            for (int i = 0; i < definitions.Count; i++)
            {
                BuiltinRoleDefinition definition = definitions[i];
                string rolePath = CombineAssetPath(rolesFolder, SafeAssetName(definition.DisplayName) + ".asset");
                DeucarianColorRole role = LoadOrCreateAsset(
                    rolePath,
                    () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                    overwriteExisting,
                    out bool roleCreated);

                if (roleCreated || overwriteExisting || ShouldRepairGeneratedRole(role, definition))
                {
                    role.Configure(
                        definition.Id,
                        definition.DisplayName,
                        definition.Category,
                        definition.Description,
                        definition.DefaultColor,
                        true);
                    EditorUtility.SetDirty(role);
                }

                result.AddRole(role);
            }

            string libraryPath = CombineAssetPath(normalizedRoot, preset.RoleLibraryFileName);
            DeucarianColorRoleLibrary library = LoadOrCreateAsset(
                libraryPath,
                () => ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>(),
                overwriteExisting,
                out bool libraryCreated);

            bool libraryChanged = library.RemoveNullRoles() > 0;
            for (int i = 0; i < result.Roles.Count; i++)
            {
                libraryChanged |= library.AddRole(result.Roles[i]);
            }

            if (libraryChanged || libraryCreated || overwriteExisting)
            {
                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }

            string palettePath = CombineAssetPath(normalizedRoot, preset.PaletteFileName);
            DeucarianColorPalette palette = LoadOrCreateAsset(
                palettePath,
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            bool paletteChanged = paletteCreated || overwriteExisting || palette.RoleLibrary != library;
            palette.Configure(preset.PaletteId, preset.PaletteDisplayName, library);
            if (paletteCreated || overwriteExisting)
            {
                palette.ClearEntries();
            }
            else
            {
                paletteChanged |= palette.RemoveNullEntries() > 0;
            }

            for (int i = 0; i < result.Roles.Count; i++)
            {
                DeucarianColorRole role = result.Roles[i];
                Color defaultColor = definitions[i].DefaultColor;
                if (role == null)
                {
                    continue;
                }

                if (paletteCreated
                    || overwriteExisting
                    || !PaletteHasEntryForRole(palette, role)
                    || PaletteRoleColorIsMissing(palette, role))
                {
                    palette.SetColor(role, defaultColor, definitions[i].Description);
                    paletteChanged = true;
                }
            }

            if (paletteChanged)
            {
                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }

            string themePath = CombineAssetPath(normalizedRoot, preset.ThemeFileName);
            DeucarianTheme theme = LoadOrCreateAsset(
                themePath,
                () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                overwriteExisting,
                out bool themeCreated);

            if (themeCreated || overwriteExisting || theme.ColorPalette != palette || string.IsNullOrWhiteSpace(theme.ThemeId))
            {
                theme.Configure(preset.ThemeId, preset.ThemeDisplayName, palette);
                EditorUtility.SetDirty(theme);
            }

            result.RoleLibrary = library;
            result.Palette = palette;
            result.Theme = theme;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return result;
        }

        private static T LoadOrCreateAsset<T>(string assetPath, Func<T> factory, bool overwriteExisting, out bool created)
            where T : ScriptableObject
        {
            created = false;
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (existing != null)
            {
                if (!overwriteExisting)
                {
                    T typedExisting = existing as T;
                    if (typedExisting != null)
                    {
                        return typedExisting;
                    }

                    string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    Debug.LogWarning($"Asset already exists at {assetPath} and is not a {typeof(T).Name}. Creating {uniquePath} instead.");
                    assetPath = uniquePath;
                }
                else
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            T asset = factory();
            AssetDatabase.CreateAsset(asset, assetPath);
            created = true;
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            string normalized = NormalizeAssetPath(folder);
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string[] parts = normalized.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string CombineAssetPath(string left, string right)
        {
            return NormalizeAssetPath(left.TrimEnd('/') + "/" + right.TrimStart('/'));
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').Trim().TrimEnd('/');
        }

        private static string SafeAssetName(string value)
        {
            string safeName = string.IsNullOrWhiteSpace(value) ? "Color Role" : value;
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                safeName = safeName.Replace(invalidCharacters[i], '-');
            }

            return safeName;
        }

        private static bool ShouldRepairGeneratedRole(DeucarianColorRole role, BuiltinRoleDefinition definition)
        {
            if (role == null)
            {
                return false;
            }

            return !string.Equals(role.Id, definition.Id, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(role.DisplayName)
                || string.IsNullOrWhiteSpace(role.Category)
                || IsPackageMissingColor(role.DefaultColor);
        }

        private static bool PaletteHasEntryForRole(DeucarianColorPalette palette, DeucarianColorRole role)
        {
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                if (entry.Role == role || string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PaletteRoleColorIsMissing(DeucarianColorPalette palette, DeucarianColorRole role)
        {
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                if ((entry.Role == role || string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal))
                    && IsPackageMissingColor(entry.Color))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPackageMissingColor(Color color)
        {
            const float tolerance = 0.0001f;
            return Mathf.Abs(color.r - Color.magenta.r) <= tolerance
                && Mathf.Abs(color.g - Color.magenta.g) <= tolerance
                && Mathf.Abs(color.b - Color.magenta.b) <= tolerance
                && Mathf.Abs(color.a - Color.magenta.a) <= tolerance;
        }

        private static IReadOnlyList<BuiltinRoleDefinition> CreateMinimalDefaultRoleDefinitions()
        {
            return new[]
            {
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Background, "Background", DeucarianColorRoleCategories.Semantic, "Main UI or scene background surface.", Hex("#0D1218")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Surface, "Surface", DeucarianColorRoleCategories.Semantic, "Default panel or card surface.", Hex("#1A2330")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.SurfaceRaised, "Surface Raised", DeucarianColorRoleCategories.Semantic, "Elevated panel or overlay surface.", Hex("#2C3A4D")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Primary, "Primary", DeucarianColorRoleCategories.Semantic, "Primary action or brand emphasis.", Hex("#5A6FA0")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Secondary, "Secondary", DeucarianColorRoleCategories.Semantic, "Secondary action or brand emphasis.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Core.Accent, "Accent", DeucarianColorRoleCategories.Semantic, "Subtle accent or supporting emphasis.", Hex("#276065")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Primary, "Text Primary", DeucarianColorRoleCategories.Text, "Primary readable text.", Hex("#C4CAD1")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Secondary, "Text Secondary", DeucarianColorRoleCategories.Text, "Secondary readable text.", Hex("#A8B0BA")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Muted, "Text Muted", DeucarianColorRoleCategories.Text, "Muted helper or supporting text.", Hex("#6F7A86")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Text.Disabled, "Text Disabled", DeucarianColorRoleCategories.Text, "Disabled or unavailable text.", Hex("#3C444F")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Success, "Success", DeucarianColorRoleCategories.Status, "Positive state or confirmation.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Warning, "Warning", DeucarianColorRoleCategories.Status, "Warning state.", Hex("#A87932")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Error, "Error", DeucarianColorRoleCategories.Status, "Error or destructive state.", Hex("#A04444")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Status.Info, "Info", DeucarianColorRoleCategories.Status, "Informational state.", Hex("#5A6FA0")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Normal, "UI Normal", DeucarianColorRoleCategories.UiState, "Default selectable UI state.", Hex("#1A2330")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Highlighted, "UI Highlighted", DeucarianColorRoleCategories.UiState, "Hovered or highlighted selectable UI state.", Hex("#2C3A4D")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Pressed, "UI Pressed", DeucarianColorRoleCategories.UiState, "Pressed selectable UI state.", Hex("#276065")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Selected, "UI Selected", DeucarianColorRoleCategories.UiState, "Selected selectable UI state.", Hex("#3BA69A")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Disabled, "UI Disabled", DeucarianColorRoleCategories.UiState, "Disabled selectable UI state.", Hex("#3C444F")),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UI.Focused, "UI Focused", DeucarianColorRoleCategories.UiState, "Keyboard or controller focused UI state.", Hex("#5A6FA0"))
            };
        }

        private static IReadOnlyList<BuiltinRoleDefinition> CreateGameRoleDefinitions()
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

        private static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                throw new ArgumentException("Invalid color value: " + hex, nameof(hex));
            }

            return color;
        }

        private readonly struct ThemePresetDefinition
        {
            public ThemePresetDefinition(
                string roleLibraryFileName,
                string paletteFileName,
                string themeFileName,
                string paletteId,
                string paletteDisplayName,
                string themeId,
                string themeDisplayName)
            {
                RoleLibraryFileName = roleLibraryFileName;
                PaletteFileName = paletteFileName;
                ThemeFileName = themeFileName;
                PaletteId = paletteId;
                PaletteDisplayName = paletteDisplayName;
                ThemeId = themeId;
                ThemeDisplayName = themeDisplayName;
            }

            public string RoleLibraryFileName { get; }
            public string PaletteFileName { get; }
            public string ThemeFileName { get; }
            public string PaletteId { get; }
            public string PaletteDisplayName { get; }
            public string ThemeId { get; }
            public string ThemeDisplayName { get; }
        }

        private readonly struct BuiltinRoleDefinition
        {
            public BuiltinRoleDefinition(string id, string displayName, string category, string description, Color defaultColor)
            {
                Id = id;
                DisplayName = displayName;
                Category = category;
                Description = description;
                DefaultColor = defaultColor;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Category { get; }
            public string Description { get; }
            public Color DefaultColor { get; }
        }
    }
}
