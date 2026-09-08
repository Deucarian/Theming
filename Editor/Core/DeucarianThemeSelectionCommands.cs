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
    internal static class DeucarianThemeSelectionCommands
    {
        public static DeucarianThemeRuntimeSettings ResolveProjectRuntimeSettings()
        {
            return DeucarianThemeRuntimeResolver.LoadSettings();
        }

        public static bool TryHydrateActiveAssetsFromProjectDefault()
        {
            return TryHydrateActiveAssetsFromProjectDefault(ResolveProjectRuntimeSettings());
        }

        public static bool TryHydrateActiveAssetsFromProjectDefault(
            DeucarianThemeRuntimeSettings settings)
        {
            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            DeucarianThemeMode mode = DeucarianThemingEditorSettings.ActiveThemeMode;
            DeucarianThemeStyle style = DeucarianThemingEditorSettings.ActiveStyle;
            bool changed = false;

            if (family == null)
            {
                if (settings == null || settings.DefaultThemeFamily == null)
                {
                    return false;
                }

                family = settings.DefaultThemeFamily;
                mode = settings.DefaultThemeMode;
                changed = true;
            }

            DeucarianTheme resolvedTheme = family.ResolveTheme(mode);
            DeucarianColorPalette resolvedPalette =
                resolvedTheme != null ? resolvedTheme.ColorPalette : null;
            DeucarianColorRoleLibrary resolvedLibrary =
                resolvedPalette != null ? resolvedPalette.RoleLibrary : null;
            if (style == null && resolvedTheme != null && resolvedTheme.VisualStyle != null)
            {
                style = resolvedTheme.VisualStyle;
                changed = true;
            }

            if (DeucarianThemingEditorSettings.ActiveTheme != resolvedTheme
                || DeucarianThemingEditorSettings.ActivePalette != resolvedPalette
                || DeucarianThemingEditorSettings.ActiveRoleLibrary != resolvedLibrary)
            {
                changed = true;
            }

            DeucarianThemingEditorSettings.SetDraftSelection(family, mode, style);
            return changed;
        }

        public static bool SetActiveThemeFamilyAsProjectDefault()
        {
            return SetActiveThemeFamilyAsProjectDefault(ResolveProjectRuntimeSettings());
        }

        public static bool SetActiveThemeFamilyAsProjectDefault(
            DeucarianThemeRuntimeSettings settings)
        {
            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (settings == null)
            {
                ThemingLog.Editor.Warning(
                    "No Deucarian runtime theme settings were found. Create an asset named '"
                    + DeucarianThemeRuntimeSettings.ResourceName
                    + ".asset' in a Resources folder before setting the project default.");
                return false;
            }

            if (family == null)
            {
                ThemingLog.Editor.Warning("Choose an active Deucarian theme family before setting the project default.");
                return false;
            }

            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("setting the project theme default"))
            {
                return false;
            }

            Undo.RecordObject(settings, "Set Deucarian Project Theme Default");
            settings.Configure(family, DeucarianThemingEditorSettings.ActiveThemeMode);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            ThemingLog.Editor.Info(
                $"Set '{family.name}' ({DeucarianThemingEditorSettings.ActiveThemeMode}) as the Deucarian project theme default.",
                settings);
            return true;
        }

        public static int SetActiveThemeFamilyAndApply(DeucarianThemeFamily family)
        {
            SetActiveThemeFamilySelection(family, DeucarianThemingEditorSettings.ActiveThemeMode);
            if (family == null)
            {
                return 0;
            }

            return DeucarianThemeSceneCommands.ApplyThemeFamilyToOpenScene(
                family,
                DeucarianThemingEditorSettings.ActiveThemeMode,
                false,
                false);
        }

        public static int SetActiveThemeModeAndApply(DeucarianThemeMode mode)
        {
            DeucarianThemingEditorSettings.ActiveThemeMode = mode;
            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (family == null)
            {
                return 0;
            }

            SetActiveThemeFamilySelection(family, mode);
            return DeucarianThemeSceneCommands.ApplyThemeFamilyToOpenScene(family, mode, false, false);
        }

        public static int SetActiveThemeAndApply(DeucarianTheme theme)
        {
            DeucarianThemingEditorSettings.ActiveThemeFamily = null;
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            if (theme == null)
            {
                return 0;
            }

            return DeucarianThemeSceneCommands.ApplyThemeToOpenScene(theme, false, false);
        }

        public static bool SetActivePaletteAndApply(DeucarianColorPalette palette)
        {
            DeucarianThemingEditorSettings.ActivePalette = palette;
            if (palette == null)
            {
                return false;
            }

            DeucarianTheme theme = DeucarianThemingEditorSettings.ActiveTheme
                ?? DeucarianThemeSelectionActions.ResolveOrCreateActiveTheme(false);
            if (theme == null)
            {
                return false;
            }

            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("assigning a theme palette"))
            {
                return false;
            }

            Undo.RecordObject(theme, "Assign Deucarian Theme Palette");
            theme.SetColorPalette(palette);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            DeucarianThemeSceneCommands.RefreshOpenSceneProvidersUsingAsset(theme);
            return true;
        }

        public static bool SetActiveRoleLibraryAndApply(DeucarianColorRoleLibrary roleLibrary)
        {
            DeucarianThemingEditorSettings.ActiveRoleLibrary = roleLibrary;
            if (roleLibrary == null)
            {
                return false;
            }

            DeucarianColorPalette palette = DeucarianThemingEditorSettings.ActivePalette
                ?? DeucarianThemeSelectionActions.ResolveOrCreateActivePalette(false);
            if (palette == null)
            {
                return false;
            }

            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("assigning a palette role library"))
            {
                return false;
            }

            Undo.RecordObject(palette, "Assign Deucarian Palette Role Library");
            palette.SetRoleLibrary(roleLibrary);
            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            DeucarianThemeSceneCommands.RefreshOpenSceneProvidersUsingAsset(palette);
            return true;
        }

        public static bool SetActiveStyleAndApply(DeucarianThemeStyle style)
        {
            DeucarianThemingEditorSettings.ActiveStyle = style;
            if (style == null)
            {
                return false;
            }

            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (family != null)
            {
                return AssignStyleToThemeFamily(family, style);
            }

            DeucarianTheme theme = DeucarianThemingEditorSettings.ActiveTheme
                ?? DeucarianThemeSelectionActions.ResolveOrCreateActiveTheme(false);
            if (theme == null)
            {
                return false;
            }

            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("assigning a theme style"))
            {
                return false;
            }

            Undo.RecordObject(theme, "Assign Deucarian Theme Style");
            theme.SetVisualStyle(style);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            DeucarianThemeSceneCommands.RefreshOpenSceneProvidersUsingAsset(theme);
            return true;
        }

        public static bool AssignActiveStyleToActiveTheme()
        {
            DeucarianTheme theme = DeucarianThemeSelectionActions.ResolveOrCreateActiveTheme();
            DeucarianThemeStyle style = DeucarianThemeSelectionActions.ResolveOrCreateActiveStyle();
            if (theme == null || style == null)
            {
                ThemingLog.Editor.Warning("Assigning a Deucarian style requires both an active theme and an active style.");
                return false;
            }

            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("assigning an active theme style"))
            {
                return false;
            }

            Undo.RecordObject(theme, "Assign Deucarian Theme Style");
            theme.SetVisualStyle(style);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            int refreshed = DeucarianThemeSceneCommands.RefreshOpenSceneProvidersUsingAsset(theme);
            string providerNote = refreshed > 0 ? $" Refreshed {refreshed} open scene provider(s)." : string.Empty;
            ThemingLog.Editor.Info(
                $"Assigned Deucarian style '{style.name}' to theme '{theme.name}'.{providerNote}",
                theme);
            return true;
        }

        public static bool AssignActiveStyleToActiveThemeFamily()
        {
            DeucarianThemeFamily family = DeucarianThemeSelectionActions.ResolveOrCreateActiveThemeFamily();
            DeucarianThemeStyle style = DeucarianThemeSelectionActions.ResolveOrCreateActiveStyle();
            if (family == null || style == null)
            {
                ThemingLog.Editor.Warning("Assigning a shared Deucarian style requires an active theme family and style.");
                return false;
            }

            return AssignStyleToThemeFamily(family, style);
        }

        internal static void StoreDefaultAssetSelections(DeucarianDefaultThemeAssets assets)
        {
            if (assets == null)
            {
                return;
            }

            if (assets.ThemeFamily != null)
            {
                SetActiveThemeFamilySelection(
                    assets.ThemeFamily,
                    DeucarianThemingEditorSettings.ActiveThemeMode);
            }
            else
            {
                DeucarianThemingEditorSettings.ActiveThemeFamily = null;
            }

            if (assets.ThemeFamily == null && assets.Theme != null)
            {
                DeucarianThemingEditorSettings.ActiveTheme = assets.Theme;
            }

            if (assets.ThemeFamily == null && assets.Palette != null)
            {
                DeucarianThemingEditorSettings.ActivePalette = assets.Palette;
            }

            if (assets.RoleLibrary != null)
            {
                DeucarianThemingEditorSettings.ActiveRoleLibrary = assets.RoleLibrary;
            }

            if (assets.DefaultStyle != null)
            {
                DeucarianThemingEditorSettings.ActiveStyle = assets.DefaultStyle;
            }
            else if (assets.Styles.Count > 0)
            {
                DeucarianThemingEditorSettings.ActiveStyle = assets.Styles[0];
            }
        }

        internal static void SetActiveThemeFamilySelection(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode)
        {
            DeucarianThemingEditorSettings.ActiveThemeFamily = family;
            DeucarianThemingEditorSettings.ActiveThemeMode = mode;
            if (family == null)
            {
                return;
            }

            DeucarianTheme theme = family.ResolveTheme(mode);
            DeucarianColorPalette palette = theme != null ? theme.ColorPalette : null;
            DeucarianThemingEditorSettings.ActiveTheme = theme;
            DeucarianThemingEditorSettings.ActivePalette = palette;
            DeucarianThemingEditorSettings.ActiveRoleLibrary = palette != null ? palette.RoleLibrary : null;
            if (theme != null && theme.VisualStyle != null)
            {
                DeucarianThemingEditorSettings.ActiveStyle = theme.VisualStyle;
            }
        }

        internal static bool AssignStyleToThemeFamily(
            DeucarianThemeFamily family,
            DeucarianThemeStyle style)
        {
            if (family == null || style == null)
            {
                return false;
            }

            if (!DeucarianThemeAssetCatalog.CanPersistEditorChanges("assigning a shared family style"))
            {
                return false;
            }

            DeucarianTheme lightTheme = family.LightTheme;
            DeucarianTheme darkTheme = family.DarkTheme;
            if (lightTheme != null)
            {
                Undo.RecordObject(lightTheme, "Assign Shared Deucarian Theme Style");
            }

            if (darkTheme != null && darkTheme != lightTheme)
            {
                Undo.RecordObject(darkTheme, "Assign Shared Deucarian Theme Style");
            }

            bool changed = family.SetSharedVisualStyle(style);

            if (changed)
            {
                if (lightTheme != null)
                {
                    EditorUtility.SetDirty(lightTheme);
                }

                if (darkTheme != null)
                {
                    EditorUtility.SetDirty(darkTheme);
                }

                AssetDatabase.SaveAssets();
                DeucarianThemeSceneCommands.RefreshOpenSceneProvidersUsingAsset(family);
            }

            ThemingLog.Editor.Info(
                $"Assigned Deucarian style '{style.name}' to both variants of family '{family.name}'.",
                family);
            return true;
        }
    }
}
