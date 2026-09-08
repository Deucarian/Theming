using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using AssetSearchResult = Deucarian.Theming.Editor.DeucarianThemingMenuActions.AssetSearchResult;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeAssetCatalog
    {
        public static IReadOnlyList<T> FindAssets<T>(string[] searchFolders = null)
            where T : UnityEngine.Object
        {
            string[] normalizedFolders = NormalizeSearchFolders(searchFolders);
            if (searchFolders != null && normalizedFolders.Length == 0)
            {
                return Array.Empty<T>();
            }

            string filter = "t:" + typeof(T).Name;
            string[] guids = normalizedFolders == null
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, normalizedFolders);

            List<T> assets = new List<T>();
            HashSet<string> seenGuids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < guids.Length; i++)
            {
                if (!seenGuids.Add(guids[i]))
                {
                    continue;
                }

                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            assets.Sort((left, right) =>
                string.Compare(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right), StringComparison.OrdinalIgnoreCase));
            return assets;
        }

        public static int RepairGeneratedAssetNames(string[] searchFolders = null)
        {
            if (!CanPersistEditorChanges("repairing generated asset names"))
            {
                return 0;
            }

            string[] folders = searchFolders == null
                ? NormalizeSearchFolders(new[] { DeucarianThemingEditorSettings.DefaultProjectFolder })
                : NormalizeSearchFolders(searchFolders);
            if (folders == null || folders.Length == 0)
            {
                ThemingLog.Editor.Info("No Deucarian generated asset folders were found to repair.");
                return 0;
            }

            int repaired = 0;
            repaired += RepairAssetNames<DeucarianThemeFamily>(folders);
            repaired += RepairAssetNames<DeucarianColorRole>(folders);
            repaired += RepairAssetNames<DeucarianColorRoleLibrary>(folders);
            repaired += RepairAssetNames<DeucarianColorPalette>(folders);
            repaired += RepairAssetNames<DeucarianTheme>(folders);
            repaired += RepairAssetNames<DeucarianThemeStyle>(folders);

            if (repaired > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            ThemingLog.Editor.Info($"Repaired {repaired} Deucarian generated asset name(s).");
            return repaired;
        }

        internal static string BuildVariantStyleId(string fileName)
        {
            string source = string.IsNullOrWhiteSpace(fileName) ? "custom" : fileName.Trim();
            System.Text.StringBuilder slug = new System.Text.StringBuilder(source.Length);
            bool pendingSeparator = false;
            for (int i = 0; i < source.Length; i++)
            {
                char character = char.ToLowerInvariant(source[i]);
                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSeparator && slug.Length > 0)
                    {
                        slug.Append('-');
                    }

                    slug.Append(character);
                    pendingSeparator = false;
                }
                else
                {
                    pendingSeparator = slug.Length > 0;
                }
            }

            return "deucarian.style.variant." + (slug.Length > 0 ? slug.ToString() : "custom");
        }

        public static string EnsureAssetFolder(string folder)
        {
            string normalized = DeucarianThemingEditorSettings.NormalizeAssetPath(folder);
            if (string.IsNullOrEmpty(normalized))
            {
                normalized = DeucarianThemingEditorSettings.DefaultThemeAssetFolder;
            }

            if (normalized != "Assets" && !normalized.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Theme asset folders must be under the Assets folder.", nameof(folder));
            }

            if (AssetDatabase.IsValidFolder(normalized))
            {
                return normalized;
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

            AssetDatabase.Refresh();
            return normalized;
        }

        internal static int RepairAssetNames<T>(string[] searchFolders)
            where T : UnityEngine.Object
        {
            string[] guids = searchFolders == null
                ? AssetDatabase.FindAssets("t:" + typeof(T).Name)
                : AssetDatabase.FindAssets("t:" + typeof(T).Name, searchFolders);

            int repaired = 0;
            HashSet<string> seenGuids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                if (!seenGuids.Add(guids[i]))
                {
                    continue;
                }

                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                string expectedName = PathWithoutExtension(path);
                if (!string.Equals(asset.name, expectedName, StringComparison.Ordinal))
                {
                    asset.name = expectedName;
                    EditorUtility.SetDirty(asset);
                    repaired++;
                }
            }

            return repaired;
        }

        internal static string CombineAssetPath(string left, string right)
        {
            return DeucarianThemingEditorSettings.NormalizeAssetPath(left.TrimEnd('/') + "/" + right.TrimStart('/'));
        }

        internal static string PathWithoutExtension(string path)
        {
            string fileName = path;
            int slashIndex = fileName.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                fileName = fileName.Substring(slashIndex + 1);
            }

            return fileName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - ".asset".Length)
                : fileName;
        }

        internal static DeucarianThemeStyle FindStyleById(IReadOnlyList<DeucarianThemeStyle> styles, string styleId)
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

        internal static string[] NormalizeSearchFolders(string[] searchFolders)
        {
            if (searchFolders == null)
            {
                return null;
            }

            List<string> validFolders = new List<string>();
            for (int i = 0; i < searchFolders.Length; i++)
            {
                string folder = DeucarianThemingEditorSettings.NormalizeAssetPath(searchFolders[i]);
                if (!string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
                {
                    validFolders.Add(folder);
                }
            }

            return validFolders.ToArray();
        }

        internal static bool CanPersistEditorChanges(string operation)
        {
            if (CanPersistSceneChanges)
            {
                return true;
            }

            ThemingLog.Editor.Warning(
                $"Exit Play Mode before {operation}. Theme preview changes remain available while playing.");
            return false;
        }

        internal static bool CanPersistSceneChanges => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
