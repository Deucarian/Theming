using System;
using System.Collections.Generic;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeSelectionPolicy
    {
        public static DeucarianThemeManagerActivationStatus Evaluate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
        {
            bool resourceReady = TryValidateRuntimeSettingsResource(
                settings,
                out string resourceMessage);
            return Evaluate(settings, selection, resourceReady, resourceMessage, providers);
        }

        public static DeucarianThemeManagerActivationStatus Evaluate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            bool runtimeSettingsResourceReady,
            string runtimeSettingsResourceMessage,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
        {
            bool hasSettings = settings != null;
            bool settingsResourceReady = hasSettings && runtimeSettingsResourceReady;
            DeucarianThemeFamily canonicalFamily = hasSettings ? settings.DefaultThemeFamily : null;
            // An existing settings asset is a valid activation target even when its previous
            // family is missing or incomplete. Activation validates and writes the staged family.
            bool settingsReady = settingsResourceReady;
            DeucarianThemeMode canonicalMode = hasSettings
                ? settings.DefaultThemeMode
                : DeucarianThemeMode.Dark;
            bool sharedStyleSynchronized = TryResolveSharedStyle(canonicalFamily, out DeucarianThemeStyle canonicalStyle);

            bool familyDirty = selection.Family != canonicalFamily;
            bool modeDirty = !hasSettings || selection.Mode != canonicalMode;
            bool styleDirty = !sharedStyleSynchronized || selection.Style != canonicalStyle;
            bool selectionValid = TryValidateSelection(selection, out string validationMessage);
            bool providersSynchronized = DeucarianThemeSceneApplication.AreProvidersSynchronized(
                providers ?? DeucarianThemeSceneApplication.FindOpenSceneProviders(),
                canonicalFamily,
                canonicalMode,
                canonicalStyle,
                sharedStyleSynchronized);

            string message;
            if (!hasSettings)
            {
                message = "Runtime settings are required before this theme can be activated.";
            }
            else if (!settingsReady)
            {
                message = runtimeSettingsResourceMessage;
            }
            else if (!selectionValid)
            {
                message = validationMessage;
            }
            else if (familyDirty || modeDirty || styleDirty)
            {
                message = "Ready to activate the staged theme.";
            }
            else if (!providersSynchronized)
            {
                message = "The project default is active, but loaded scene providers need to be synchronized.";
            }
            else
            {
                message = "This theme is active everywhere.";
            }

            return new DeucarianThemeManagerActivationStatus(
                hasSettings,
                settingsReady,
                familyDirty,
                modeDirty,
                styleDirty,
                sharedStyleSynchronized,
                providersSynchronized,
                selectionValid,
                message);
        }

        public static bool TryValidateSelection(
            DeucarianThemeManagerSelection selection,
            out string message)
        {
            return TryValidateSelection(selection, false, out message);
        }

        internal static bool TryValidateSelection(
            DeucarianThemeManagerSelection selection,
            bool hasCompleteStagedStyleEdit,
            out string message)
        {
            if (selection.Family == null)
            {
                message = "Choose a theme family before activating.";
                return false;
            }

            if (!selection.Family.IsComplete)
            {
                message = "The selected family needs both a Light and Dark theme before it can be activated.";
                return false;
            }

            if (selection.Family.LightTheme.ColorPalette == null
                || selection.Family.DarkTheme.ColorPalette == null)
            {
                message = "Both family themes need a color palette before activation.";
                return false;
            }

            if (selection.Style == null)
            {
                message = "Choose a visual style before activating.";
                return false;
            }

            if (IsPartialComposition(selection.Style) && !hasCompleteStagedStyleEdit)
            {
                message = "The selected visual style has an incomplete composition. Assign Surface, Corners, Border, and Size first.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        internal static bool IsFamilyReadyForRuntimeSettings(DeucarianThemeFamily family)
        {
            return family != null
                   && family.IsComplete
                   && family.LightTheme.ColorPalette != null
                   && family.DarkTheme.ColorPalette != null;
        }

        internal static bool TryValidateRuntimeSettingsResource(
            DeucarianThemeRuntimeSettings settings,
            out string message)
        {
            if (settings == null)
            {
                message = "Runtime settings are required before this theme can be activated.";
                return false;
            }

            string path = AssetDatabase.GetAssetPath(settings);
            if (!DeucarianThemeRuntimeSettingsAssets.IsRuntimeSettingsResourcePath(path))
            {
                // Explicit settings supplied by editor tests and legacy tooling are still supported.
                // The Theme Manager itself always supplies the Resources-resolved project asset.
                message = string.Empty;
                return true;
            }

            return DeucarianThemeRuntimeSettingsAssets.TryValidateRuntimeSettingsCandidate(
                settings,
                out message);
        }

        internal static bool TryResolveSharedStyle(
            DeucarianThemeFamily family,
            out DeucarianThemeStyle style)
        {
            style = null;
            if (family == null || !family.IsComplete)
            {
                return false;
            }

            DeucarianThemeStyle lightStyle = family.LightTheme.VisualStyle;
            DeucarianThemeStyle darkStyle = family.DarkTheme.VisualStyle;
            if (lightStyle == null || lightStyle != darkStyle)
            {
                return false;
            }

            style = lightStyle;
            return true;
        }

        internal static bool IsPartialComposition(DeucarianThemeStyle style)
        {
            if (style == null || style.IsComposed)
            {
                return false;
            }

            return style.SurfaceProfile != null
                   || style.ShapeProfile != null
                   || style.StrokeProfile != null
                   || style.TypographyProfile != null
                   || style.Density != DeucarianThemeDensity.Unspecified;
        }

    }
}
