using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Creates project assets from package-provided theme pack definitions.
    /// </summary>
    public static class DeucarianThemePackAssetFactory
    {
        private const string RolesFolderName = "Roles";

        public static DeucarianDefaultThemeAssets CreateOrRepairThemePackAssets(
            DeucarianThemePack themePack,
            string rootFolder,
            bool overwriteExisting = false)
        {
            if (themePack == null)
            {
                throw new ArgumentNullException(nameof(themePack));
            }

            string normalizedRoot = NormalizeAssetPath(rootFolder);
            ValidateAssetFolder(normalizedRoot, nameof(rootFolder));
            EnsureFolder(normalizedRoot);

            string rolesFolder = CombineAssetPath(normalizedRoot, RolesFolderName);
            EnsureFolder(rolesFolder);

            DeucarianDefaultThemeAssets result = new DeucarianDefaultThemeAssets();
            List<DeucarianThemePackRole> createdDefinitions = new List<DeucarianThemePackRole>();
            IReadOnlyList<DeucarianThemePackRole> roleDefinitions = themePack.Roles;
            for (int i = 0; i < roleDefinitions.Count; i++)
            {
                DeucarianThemePackRole definition = roleDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                DeucarianColorRole role = CreateOrRepairRole(rolesFolder, definition, overwriteExisting);
                result.AddRole(role);
                createdDefinitions.Add(definition);
            }

            DeucarianColorRoleLibrary library = LoadOrCreateAsset(
                CombineAssetPath(normalizedRoot, themePack.RoleLibraryFileName),
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

            DeucarianColorPalette palette = LoadOrCreateAsset(
                CombineAssetPath(normalizedRoot, themePack.PaletteFileName),
                () => ScriptableObject.CreateInstance<DeucarianColorPalette>(),
                overwriteExisting,
                out bool paletteCreated);

            bool paletteChanged = paletteCreated || overwriteExisting || palette.RoleLibrary != library;
            palette.Configure(themePack.PaletteId, themePack.PaletteDisplayName, library);
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
                DeucarianThemePackRole definition = createdDefinitions[i];
                if (RepairPaletteEntry(palette, role, definition, overwriteExisting))
                {
                    paletteChanged = true;
                }
            }

            if (paletteChanged)
            {
                palette.SortEntriesByCategoryAndName();
                EditorUtility.SetDirty(palette);
            }

            string stylesFolder = CombineAssetPath(
                normalizedRoot,
                DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName);
            IReadOnlyList<DeucarianThemeStyle> styles =
                DeucarianDefaultThemeAssetFactory.CreateBuiltinThemeStyleAssets(stylesFolder, overwriteExisting);
            for (int i = 0; i < styles.Count; i++)
            {
                result.AddStyle(styles[i]);
            }

            DeucarianThemeStyle defaultStyle = FindStyleById(styles, themePack.DefaultStyleId);
            DeucarianTheme theme = LoadOrCreateAsset(
                CombineAssetPath(normalizedRoot, themePack.ThemeFileName),
                () => ScriptableObject.CreateInstance<DeucarianTheme>(),
                overwriteExisting,
                out bool themeCreated);

            if (themeCreated
                || overwriteExisting
                || theme.ColorPalette != palette
                || string.IsNullOrWhiteSpace(theme.ThemeId))
            {
                theme.Configure(themePack.ThemeId, themePack.ThemeDisplayName, palette, theme.VisualStyle);
                EditorUtility.SetDirty(theme);
            }

            if (defaultStyle != null && (overwriteExisting || theme.VisualStyle == null))
            {
                theme.SetVisualStyle(defaultStyle);
                EditorUtility.SetDirty(theme);
            }

            result.RoleLibrary = library;
            result.Palette = palette;
            result.Theme = theme;
            result.DefaultStyle = defaultStyle;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        public static DeucarianThemeRuntimeSettings CreateOrRepairRuntimeSettings(
            string resourcesFolder,
            DeucarianTheme defaultTheme,
            bool overwriteExisting = false)
        {
            string normalizedFolder = NormalizeAssetPath(resourcesFolder);
            ValidateAssetFolder(normalizedFolder, nameof(resourcesFolder));
            EnsureFolder(normalizedFolder);

            string settingsPath = CombineAssetPath(
                normalizedFolder,
                DeucarianThemeRuntimeSettings.ResourceName + ".asset");
            DeucarianThemeRuntimeSettings settings = LoadOrCreateAsset(
                settingsPath,
                () => ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>(),
                overwriteExisting,
                out bool settingsCreated);

            if (settingsCreated || overwriteExisting || settings.DefaultTheme != defaultTheme)
            {
                settings.Configure(defaultTheme);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return settings;
        }

        private static DeucarianColorRole CreateOrRepairRole(
            string rolesFolder,
            DeucarianThemePackRole definition,
            bool overwriteExisting)
        {
            string roleAssetName = string.IsNullOrWhiteSpace(definition.AssetName)
                ? definition.DisplayName
                : definition.AssetName;
            string rolePath = CombineAssetPath(rolesFolder, SafeAssetName(roleAssetName) + ".asset");
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
                    definition.IsCoreRole);
                EditorUtility.SetDirty(role);
            }

            return role;
        }

        private static bool RepairPaletteEntry(
            DeucarianColorPalette palette,
            DeucarianColorRole role,
            DeucarianThemePackRole definition,
            bool overwriteExisting)
        {
            if (palette == null || role == null || definition == null)
            {
                return false;
            }

            if (overwriteExisting
                || !TryGetPaletteEntry(palette, role.Id, out DeucarianColorRole entryRole, out Color entryColor)
                || entryRole == null
                || IsPackageMissingColor(entryColor))
            {
                palette.SetColor(role, definition.DefaultColor, definition.Description);
                return true;
            }

            if (entryRole != role)
            {
                palette.SetColor(role, entryColor, definition.Description);
                return true;
            }

            return false;
        }

        private static bool TryGetPaletteEntry(
            DeucarianColorPalette palette,
            string roleId,
            out DeucarianColorRole role,
            out Color color)
        {
            role = null;
            color = DeucarianColorPalette.MissingColor;
            if (palette == null)
            {
                return false;
            }

            string normalizedId = DeucarianColorRole.NormalizeId(roleId);
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                DeucarianColorRole entryRole = entry != null ? entry.Role : null;
                if (entryRole == null || !string.Equals(entryRole.Id, normalizedId, StringComparison.Ordinal))
                {
                    continue;
                }

                role = entryRole;
                color = entry.Color;
                return true;
            }

            return false;
        }

        private static T LoadOrCreateAsset<T>(
            string assetPath,
            Func<T> factory,
            bool overwriteExisting,
            out bool created)
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
                        EnsureAssetObjectNameMatchesPath(typedExisting, assetPath);
                        return typedExisting;
                    }

                    string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    ThemingLog.Editor.Warning($"Asset already exists at {assetPath} and is not a {typeof(T).Name}. Creating {uniquePath} instead.");
                    assetPath = uniquePath;
                }
                else
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            T asset = factory();
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            created = true;
            return asset;
        }

        private static void EnsureAssetObjectNameMatchesPath(UnityEngine.Object asset, string assetPath)
        {
            if (asset == null || string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string expectedName = Path.GetFileNameWithoutExtension(assetPath);
            if (!string.IsNullOrEmpty(expectedName) && asset.name != expectedName)
            {
                asset.name = expectedName;
                EditorUtility.SetDirty(asset);
            }
        }

        private static DeucarianThemeStyle FindStyleById(IReadOnlyList<DeucarianThemeStyle> styles, string styleId)
        {
            string normalizedId = DeucarianColorRole.NormalizeId(styleId);
            for (int i = 0; i < styles.Count; i++)
            {
                DeucarianThemeStyle style = styles[i];
                if (style != null && string.Equals(style.StyleId, normalizedId, StringComparison.Ordinal))
                {
                    return style;
                }
            }

            return null;
        }

        private static bool ShouldRepairGeneratedRole(DeucarianColorRole role, DeucarianThemePackRole definition)
        {
            if (role == null || definition == null)
            {
                return false;
            }

            return !string.Equals(role.Id, definition.Id, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(role.DisplayName)
                || string.IsNullOrWhiteSpace(role.Category)
                || IsPackageMissingColor(role.DefaultColor);
        }

        private static bool IsPackageMissingColor(Color color)
        {
            const float tolerance = 0.0001f;
            return Mathf.Abs(color.r - DeucarianColorPalette.MissingColor.r) <= tolerance
                && Mathf.Abs(color.g - DeucarianColorPalette.MissingColor.g) <= tolerance
                && Mathf.Abs(color.b - DeucarianColorPalette.MissingColor.b) <= tolerance
                && Mathf.Abs(color.a - DeucarianColorPalette.MissingColor.a) <= tolerance;
        }

        private static void ValidateAssetFolder(string folder, string parameterName)
        {
            if (string.IsNullOrEmpty(folder)
                || (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme pack assets must be created under the Assets folder.", parameterName);
            }
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
    }
}
