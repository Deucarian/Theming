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
    internal static class DeucarianThemeAssetDialogs
    {
        public static DeucarianDefaultThemeAssets CreateThemeFamilyFromSavePanel()
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a theme family"))
            {
                return null;
            }

            string folder = DeucarianThemeAssetCatalog.EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string familyPath = EditorUtility.SaveFilePanelInProject(
                "Create Theme Family",
                DeucarianThemeAssetCatalog.PathWithoutExtension(DeucarianDefaultThemeAssetFactory.ThemeFamilyFileName),
                "asset",
                "Choose where to create the paired light and dark Deucarian theme family.",
                folder);

            if (string.IsNullOrEmpty(familyPath))
            {
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianThemeAssetSetup.CreateThemeFamily(familyPath);
            if (assets == null)
            {
                return null;
            }

            DeucarianEditorSelection.SelectAndPing(assets.ThemeFamily);
            return assets;
        }

        public static DeucarianDefaultThemeAssets WrapActiveThemeInFamilyFromSavePanel(
            DeucarianThemeMode existingThemeMode)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("wrapping a theme in a family"))
            {
                return null;
            }

            if (DeucarianThemingEditorSettings.ActiveThemeFamily != null)
            {
                ThemingLog.Editor.Warning(
                    "The active theme already belongs to a theme family. Select a standalone legacy theme before migration.");
                return null;
            }

            DeucarianTheme theme = DeucarianThemingEditorSettings.ActiveTheme;
            if (theme == null)
            {
                ThemingLog.Editor.Warning("Choose an active standalone theme before wrapping it in a family.");
                return null;
            }

            string folder = DeucarianThemeAssetCatalog.EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string familyPath = EditorUtility.SaveFilePanelInProject(
                $"Wrap Theme As {existingThemeMode}",
                theme.name + "Family",
                "asset",
                $"Choose the family asset that will reference '{theme.name}' as its {existingThemeMode} variant.",
                folder);
            return string.IsNullOrEmpty(familyPath)
                ? null
                : DeucarianThemeAssetSetup.WrapActiveThemeInFamily(existingThemeMode, familyPath);
        }

        public static DeucarianDefaultThemeAssets CreateMinimalPaletteFromSavePanel()
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a minimal palette"))
            {
                return null;
            }

            string folder = DeucarianThemeAssetCatalog.EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string palettePath = EditorUtility.SaveFilePanelInProject(
                "Create Minimal Palette",
                DeucarianThemeAssetCatalog.PathWithoutExtension(DeucarianDefaultThemeAssetFactory.MinimalPaletteFileName),
                "asset",
                "Choose where to create the minimal Deucarian palette.",
                folder);

            if (string.IsNullOrEmpty(palettePath))
            {
                return null;
            }

            DeucarianDefaultThemeAssets assets = DeucarianThemeAssetSetup.CreateMinimalPalette(palettePath);
            if (assets == null)
            {
                return null;
            }

            DeucarianEditorSelection.SelectAndPing(assets.Palette);
            return assets;
        }

        public static DeucarianColorPalette CreatePaletteFromActiveThemeFromSavePanel()
        {
            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("creating a palette from a theme"))
            {
                return null;
            }

            DeucarianTheme theme = DeucarianThemeSelectionActions.ResolveOrCreateActiveTheme();
            if (theme == null || theme.ColorPalette == null)
            {
                ThemingLog.Editor.Warning("No active Deucarian theme with a palette is selected.");
                return null;
            }

            string folder = DeucarianThemeAssetCatalog.EnsureAssetFolder(DeucarianDefaultThemeAssetFactory.MinimalPaletteRootFolder);
            string palettePath = EditorUtility.SaveFilePanelInProject(
                "Create Palette From Active Theme",
                theme.name + "Palette",
                "asset",
                "Choose where to create or update the palette copy.",
                folder);

            if (string.IsNullOrEmpty(palettePath))
            {
                return null;
            }

            DeucarianColorPalette palette = DeucarianThemeAssetSetup.CreatePaletteFromTheme(theme, palettePath);
            DeucarianEditorSelection.SelectAndPing(palette);
            return palette;
        }

        public static DeucarianThemeStyle CreateStyleVariantFromActiveFromSavePanel()
        {
            DeucarianThemeStyle source = DeucarianThemingEditorSettings.ActiveStyle;
            if (source == null)
            {
                ThemingLog.Editor.Warning("Creating a custom style requires an active Deucarian style.");
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string defaultFolder = string.IsNullOrEmpty(sourcePath)
                ? DeucarianThemingEditorSettings.DefaultAssetFolder
                : sourcePath.Substring(0, sourcePath.LastIndexOf('/'));
            string suggestedName = string.IsNullOrWhiteSpace(source.DisplayName)
                ? "Custom Theme Style"
                : source.DisplayName + " Custom";
            string variantPath = EditorUtility.SaveFilePanelInProject(
                "Create Deucarian Custom Style",
                suggestedName,
                "asset",
                "Choose a source-controlled location for the reusable custom presentation style.",
                defaultFolder);
            return string.IsNullOrWhiteSpace(variantPath)
                ? null
                : DeucarianThemeStyleAssets.CreateStyleVariant(source, variantPath);
        }

        public static void SelectAndPing(UnityEngine.Object asset)
        {
            DeucarianEditorSelection.SelectAndPing(asset);
        }

        public static void OpenThemeAssetsFolder()
        {
            string folder = DeucarianThemeAssetCatalog.EnsureAssetFolder(DeucarianThemingEditorSettings.DefaultAssetFolder);
            UnityEngine.Object folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
            if (folderAsset != null)
            {
                DeucarianEditorSelection.SelectAndPing(folderAsset);
                return;
            }

            ThemingLog.Editor.Info($"Deucarian theme assets folder: {folder}");
        }
    }
}
