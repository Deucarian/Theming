using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianBundledTypographyAssets
    {
        internal const string FontsRoot = "Packages/com.deucarian.theming/Runtime/Fonts";
        internal const string InterRegularFontPath = FontsRoot + "/Inter-Regular.ttf";
        internal const string InterBoldFontPath = FontsRoot + "/Inter-Bold.ttf";
        internal const string MontserratRegularFontPath = FontsRoot + "/Montserrat-Regular.ttf";
        internal const string MontserratBoldFontPath = FontsRoot + "/Montserrat-Bold.ttf";
        internal const string InterRegularAssetPath = FontsRoot + "/Inter-Regular SDF.asset";
        internal const string InterBoldAssetPath = FontsRoot + "/Inter-Bold SDF.asset";
        internal const string MontserratRegularAssetPath = FontsRoot + "/Montserrat-Regular SDF.asset";
        internal const string MontserratBoldAssetPath = FontsRoot + "/Montserrat-Bold SDF.asset";
        internal const string InterProfilePath = FontsRoot + "/InterTypography.asset";
        internal const string MontserratProfilePath = FontsRoot + "/MontserratTypography.asset";

        internal static DeucarianThemeTypographyProfile InterProfile =>
            AssetDatabase.LoadAssetAtPath<DeucarianThemeTypographyProfile>(InterProfilePath);

        internal static DeucarianThemeTypographyProfile MontserratProfile =>
            AssetDatabase.LoadAssetAtPath<DeucarianThemeTypographyProfile>(MontserratProfilePath);
    }

    /// <summary>Rebuilds the committed TMP assets for the package-bundled source fonts.</summary>
    public static class DeucarianBundledTypographyAssetGenerator
    {
        public static void Generate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureTmpEssentialResourcesForGeneration();

            TMP_FontAsset interRegular = CreateDynamicFontAsset(
                DeucarianBundledTypographyAssets.InterRegularFontPath,
                DeucarianBundledTypographyAssets.InterRegularAssetPath);
            TMP_FontAsset interBold = CreateDynamicFontAsset(
                DeucarianBundledTypographyAssets.InterBoldFontPath,
                DeucarianBundledTypographyAssets.InterBoldAssetPath);
            TMP_FontAsset montserratRegular = CreateDynamicFontAsset(
                DeucarianBundledTypographyAssets.MontserratRegularFontPath,
                DeucarianBundledTypographyAssets.MontserratRegularAssetPath);
            TMP_FontAsset montserratBold = CreateDynamicFontAsset(
                DeucarianBundledTypographyAssets.MontserratBoldFontPath,
                DeucarianBundledTypographyAssets.MontserratBoldAssetPath);

            AssignBoldTypeface(interRegular, interBold);
            AssignBoldTypeface(montserratRegular, montserratBold);
            CreateTypographyProfile(
                DeucarianBundledTypographyAssets.InterProfilePath,
                "Inter",
                interRegular);
            CreateTypographyProfile(
                DeucarianBundledTypographyAssets.MontserratProfilePath,
                "Montserrat",
                montserratRegular);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsureTmpEssentialResourcesForGeneration()
        {
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") != null)
            {
                return;
            }

            const string packagePath =
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
            string absolutePath = Path.GetFullPath(packagePath);
            if (!File.Exists(absolutePath))
            {
                throw new InvalidOperationException(
                    "TMP Essential Resources could not be found for bundled font generation.");
            }

            AssetDatabase.ImportPackage(absolutePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
            {
                throw new InvalidOperationException(
                    "TMP Essential Resources were imported but the distance-field shader is unavailable.");
            }
        }

        private static TMP_FontAsset CreateDynamicFontAsset(string sourcePath, string assetPath)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null)
            {
                throw new InvalidOperationException("Bundled source font is missing: " + sourcePath);
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(source);
            if (fontAsset == null)
            {
                throw new InvalidOperationException("TMP could not create a font asset for: " + sourcePath);
            }

            fontAsset.name = source.name + " SDF";
            fontAsset.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;
            Texture2D atlas = fontAsset.atlasTextures[0];
            atlas.name = source.name + " Atlas";
            fontAsset.material.name = source.name + " Atlas Material";

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static void AssignBoldTypeface(TMP_FontAsset regular, TMP_FontAsset bold)
        {
            TMP_FontWeightPair[] table = regular.fontWeightTable;
            TMP_FontWeightPair boldPair = table[7];
            boldPair.regularTypeface = bold;
            table[7] = boldPair;
            EditorUtility.SetDirty(regular);
        }

        private static void CreateTypographyProfile(
            string assetPath,
            string displayName,
            TMP_FontAsset fontAsset)
        {
            DeucarianThemeTypographyProfile profile =
                AssetDatabase.LoadAssetAtPath<DeucarianThemeTypographyProfile>(assetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DeucarianThemeTypographyProfile>();
                AssetDatabase.CreateAsset(profile, assetPath);
            }

            profile.Configure(
                fontAsset,
                DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Title),
                DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Body),
                DeucarianThemeTextStyle.DefaultFor(DeucarianThemeTextRole.Caption),
                displayName);
            EditorUtility.SetDirty(profile);
        }
    }
}
