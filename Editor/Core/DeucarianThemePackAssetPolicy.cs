using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;


namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemePackAssetPolicy
    {
        internal static DeucarianThemeStyle FindStyleById(IReadOnlyList<DeucarianThemeStyle> styles, string styleId)
        {
            return DeucarianBuiltinThemeStyleAssets.FindStyleById(styles, styleId);
        }

        internal static void ValidateAssetFolder(string folder, string parameterName)
        {
            if (string.IsNullOrEmpty(folder)
                || (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Theme pack assets must be created under the Assets folder.", parameterName);
            }
        }
    }
}
