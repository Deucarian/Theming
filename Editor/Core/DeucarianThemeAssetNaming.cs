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
    internal static class DeucarianThemeAssetNaming
    {
        internal static void ValidateAssetPath(string assetPath, string parameterName)
        {
            if (string.IsNullOrEmpty(assetPath)
                || !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                || (assetPath != "Assets" && !assetPath.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Asset paths must be .asset files under the Assets folder.", parameterName);
            }
        }

        internal static string GetAssetFolder(string assetPath)
        {
            string normalized = NormalizeAssetPath(assetPath);
            int slashIndex = normalized.LastIndexOf('/');
            return slashIndex <= 0 ? "Assets" : normalized.Substring(0, slashIndex);
        }

        internal static string CombineAssetPath(string left, string right)
        {
            return NormalizeAssetPath(left.TrimEnd('/') + "/" + right.TrimStart('/'));
        }

        internal static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').Trim().TrimEnd('/');
        }

        internal static string SafeAssetName(string value)
        {
            string safeName = string.IsNullOrWhiteSpace(value) ? "Color Role" : value;
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            for (int i = 0; i < invalidCharacters.Length; i++)
            {
                safeName = safeName.Replace(invalidCharacters[i], '-');
            }

            return safeName;
        }

        internal static string DeriveThemeAssetName(string paletteAssetName)
        {
            if (string.IsNullOrWhiteSpace(paletteAssetName))
            {
                return "Theme";
            }

            if (paletteAssetName.EndsWith("Palette", StringComparison.Ordinal))
            {
                return paletteAssetName.Substring(0, paletteAssetName.Length - "Palette".Length) + "Theme";
            }

            return paletteAssetName + "Theme";
        }

        internal static string DeriveThemeFamilyBaseName(string familyAssetName)
        {
            if (string.IsNullOrWhiteSpace(familyAssetName))
            {
                return "Deucarian";
            }

            if (familyAssetName.EndsWith("ThemeFamily", StringComparison.Ordinal))
            {
                string withoutSuffix = familyAssetName.Substring(
                    0,
                    familyAssetName.Length - "ThemeFamily".Length);
                return string.IsNullOrWhiteSpace(withoutSuffix) ? "Deucarian" : withoutSuffix;
            }

            if (familyAssetName.EndsWith("Family", StringComparison.Ordinal))
            {
                string withoutSuffix = familyAssetName.Substring(0, familyAssetName.Length - "Family".Length);
                return string.IsNullOrWhiteSpace(withoutSuffix) ? "Deucarian" : withoutSuffix;
            }

            return familyAssetName;
        }

        internal static string BuildThemeFamilyDisplayName(string baseName)
        {
            string displayName = HumanizeAssetName(baseName);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Deucarian";
            }

            return displayName.EndsWith(" Theme", StringComparison.OrdinalIgnoreCase)
                ? displayName
                : displayName + " Theme";
        }

        internal static string HumanizeAssetName(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return string.Empty;
            }

            List<char> characters = new List<char>();
            for (int i = 0; i < assetName.Length; i++)
            {
                char character = assetName[i];
                if (i > 0 && char.IsUpper(character) && !char.IsWhiteSpace(assetName[i - 1]))
                {
                    characters.Add(' ');
                }

                characters.Add(character);
            }

            return new string(characters.ToArray()).Trim();
        }

        internal static string BuildStableId(string prefix, string assetName)
        {
            string normalizedName = string.IsNullOrWhiteSpace(assetName)
                ? "asset"
                : assetName.Trim();
            List<char> characters = new List<char>();
            bool previousWasSeparator = false;

            for (int i = 0; i < normalizedName.Length; i++)
            {
                char character = char.ToLowerInvariant(normalizedName[i]);
                if (char.IsLetterOrDigit(character))
                {
                    characters.Add(character);
                    previousWasSeparator = false;
                }
                else if (!previousWasSeparator)
                {
                    characters.Add('.');
                    previousWasSeparator = true;
                }
            }

            string suffix = new string(characters.ToArray()).Trim('.');
            return string.IsNullOrEmpty(suffix) ? prefix : prefix + "." + suffix;
        }
    }
}
