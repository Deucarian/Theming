using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Result object returned by default theme asset creation.
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
    /// Creates a complete default set of Deucarian theming assets.
    /// </summary>
    public static class DeucarianDefaultThemeAssetFactory
    {
        public const string DefaultRootFolder = "Assets/Deucarian/Theming/Defaults";

        [MenuItem("Deucarian/Theming/Create Missing Default Theme Assets")]
        public static void CreateDefaultThemeAssetsFromMenu()
        {
            DeucarianDefaultThemeAssets assets =
                DeucarianThemingEditorAssetUtility.CreateMissingDefaultThemeAssets(DefaultRootFolder);
            if (assets.Theme != null)
            {
                DeucarianThemingEditorAssetUtility.SelectAndPing(assets.Theme);
            }
        }

        /// <summary>
        /// Creates default role, library, palette, and theme assets under the requested Assets folder.
        /// Existing assets are reused and warned about unless overwriteExisting is true.
        /// </summary>
        public static DeucarianDefaultThemeAssets CreateDefaultThemeAssets(string rootFolder, bool overwriteExisting = false)
        {
            string normalizedRoot = NormalizeAssetPath(rootFolder);
            if (string.IsNullOrEmpty(normalizedRoot) || (normalizedRoot != "Assets" && !normalizedRoot.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Default theme assets must be created under the Assets folder.", nameof(rootFolder));
            }

            string rolesFolder = CombineAssetPath(normalizedRoot, "Roles");
            EnsureFolder(rolesFolder);

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets();
            IReadOnlyList<BuiltinRoleDefinition> definitions = GetDefaultRoleDefinitions();

            for (int i = 0; i < definitions.Count; i++)
            {
                BuiltinRoleDefinition definition = definitions[i];
                string rolePath = CombineAssetPath(rolesFolder, SafeAssetName(definition.DisplayName) + ".asset");
                DeucarianColorRole role = LoadOrCreateAsset(
                    rolePath,
                    () => ScriptableObject.CreateInstance<DeucarianColorRole>(),
                    overwriteExisting,
                    out bool roleCreated);

                if (roleCreated || overwriteExisting)
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

            string libraryPath = CombineAssetPath(normalizedRoot, "Default Color Role Library.asset");
            DeucarianColorRoleLibrary library = LoadOrCreateAsset(
                libraryPath,
                () => ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>(),
                overwriteExisting,
                out bool libraryCreated);

            if (libraryCreated || overwriteExisting)
            {
                for (int i = 0; i < result.Roles.Count; i++)
                {
                    library.AddRole(result.Roles[i]);
                }

                library.SortRolesByCategoryAndName();
                EditorUtility.SetDirty(library);
            }

            string palettePath = CombineAssetPath(normalizedRoot, "Default Dark Color Palette.asset");
            DeucarianColorPalette palette = LoadOrCreateAsset(
                palettePath,
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            if (paletteCreated || overwriteExisting)
            {
                palette.Configure("deucarian.palette.dark-default", "Dark Default", library);
                palette.ClearEntries();

                for (int i = 0; i < result.Roles.Count; i++)
                {
                    palette.SetColor(result.Roles[i], definitions[i].DefaultColor);
                }

                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }

            string themePath = CombineAssetPath(normalizedRoot, "Default Theme.asset");
            DeucarianTheme theme = LoadOrCreateAsset(
                themePath,
                () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                overwriteExisting,
                out bool themeCreated);

            if (themeCreated || overwriteExisting)
            {
                theme.Configure("deucarian.theme.default", "Default", palette);
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
                        Debug.LogWarning($"Using existing asset without overwriting it: {assetPath}", typedExisting);
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

        private static IReadOnlyList<BuiltinRoleDefinition> GetDefaultRoleDefinitions()
        {
            return new[]
            {
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Background, "Background", DeucarianColorRoleCategories.Semantic, "Main scene or UI background.", new Color32(16, 17, 20, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Surface, "Surface", DeucarianColorRoleCategories.Semantic, "Default panel or card surface.", new Color32(24, 27, 32, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.SurfaceRaised, "Surface Raised", DeucarianColorRoleCategories.Semantic, "Elevated panel or overlay surface.", new Color32(35, 39, 49, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.TextPrimary, "Text Primary", DeucarianColorRoleCategories.Text, "Primary readable text.", new Color32(245, 247, 250, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.TextSecondary, "Text Secondary", DeucarianColorRoleCategories.Text, "Secondary readable text.", new Color32(184, 192, 204, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.TextDisabled, "Text Disabled", DeucarianColorRoleCategories.Text, "Disabled or unavailable text.", new Color32(112, 119, 132, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Primary, "Primary", DeucarianColorRoleCategories.Semantic, "Primary action or brand emphasis.", new Color32(91, 167, 255, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Secondary, "Secondary", DeucarianColorRoleCategories.Semantic, "Secondary action or emphasis.", new Color32(177, 124, 255, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Accent, "Accent", DeucarianColorRoleCategories.Semantic, "Decorative accent or contrast.", new Color32(255, 209, 102, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Success, "Success", DeucarianColorRoleCategories.Semantic, "Positive state or confirmation.", new Color32(87, 214, 141, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Warning, "Warning", DeucarianColorRoleCategories.Semantic, "Warning state.", new Color32(255, 184, 77, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Error, "Error", DeucarianColorRoleCategories.Semantic, "Error or destructive state.", new Color32(255, 107, 107, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UiNormal, "UI Normal", DeucarianColorRoleCategories.UiState, "Default selectable UI state.", new Color32(245, 247, 250, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UiHighlighted, "UI Highlighted", DeucarianColorRoleCategories.UiState, "Hovered or highlighted selectable UI state.", new Color32(123, 223, 242, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UiPressed, "UI Pressed", DeucarianColorRoleCategories.UiState, "Pressed selectable UI state.", new Color32(91, 167, 255, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UiSelected, "UI Selected", DeucarianColorRoleCategories.UiState, "Selected selectable UI state.", new Color32(255, 230, 109, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UiDisabled, "UI Disabled", DeucarianColorRoleCategories.UiState, "Disabled selectable UI state.", new Color32(112, 119, 132, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.UiFocused, "UI Focused", DeucarianColorRoleCategories.UiState, "Keyboard or controller focused UI state.", new Color32(177, 124, 255, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Health, "Health", DeucarianColorRoleCategories.Gameplay, "Health resource color.", new Color32(233, 77, 95, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Mana, "Mana", DeucarianColorRoleCategories.Gameplay, "Mana resource color.", new Color32(78, 155, 255, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Stamina, "Stamina", DeucarianColorRoleCategories.Gameplay, "Stamina resource color.", new Color32(97, 211, 148, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Experience, "Experience", DeucarianColorRoleCategories.Gameplay, "Experience resource color.", new Color32(201, 162, 39, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Ally, "Ally", DeucarianColorRoleCategories.Faction, "Friendly team or unit color.", new Color32(75, 192, 200, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Enemy, "Enemy", DeucarianColorRoleCategories.Faction, "Hostile team or unit color.", new Color32(242, 92, 84, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Neutral, "Neutral", DeucarianColorRoleCategories.Faction, "Neutral team or unit color.", new Color32(164, 169, 182, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemCommon, "Item Common", DeucarianColorRoleCategories.ItemRarity, "Common item rarity color.", new Color32(205, 210, 218, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemUncommon, "Item Uncommon", DeucarianColorRoleCategories.ItemRarity, "Uncommon item rarity color.", new Color32(87, 214, 141, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemRare, "Item Rare", DeucarianColorRoleCategories.ItemRarity, "Rare item rarity color.", new Color32(78, 155, 255, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemEpic, "Item Epic", DeucarianColorRoleCategories.ItemRarity, "Epic item rarity color.", new Color32(177, 124, 255, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.ItemLegendary, "Item Legendary", DeucarianColorRoleCategories.ItemRarity, "Legendary item rarity color.", new Color32(255, 184, 77, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Interactable, "Interactable", DeucarianColorRoleCategories.Semantic, "Interactive affordance color.", new Color32(123, 223, 242, 255)),
                new BuiltinRoleDefinition(DeucarianBuiltinColorRoleIds.Highlight, "Highlight", DeucarianColorRoleCategories.Semantic, "Selected or highlighted element color.", new Color32(255, 230, 109, 255))
            };
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
