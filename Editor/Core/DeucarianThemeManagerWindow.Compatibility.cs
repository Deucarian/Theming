using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianThemeManagerWindow
    {
        internal static Font ResolvePreviewFont(
            DeucarianThemeTypographyProfile typography,
            out string fontLabel,
            out bool usingFallback)
            => DeucarianThemeSpecimenRenderer.ResolvePreviewFont(typography, out fontLabel, out usingFallback);

        internal static Rect DrawPreviewSurface(
            Rect rect,
            Color fillColor,
            Color borderColor,
            float radius,
            float borderWidth)
            => DeucarianThemeSpecimenRenderer.DrawPreviewSurface(rect, fillColor, borderColor, radius, borderWidth);

        internal static bool TryValidateRuntimeSettingsCandidate(
            DeucarianThemeRuntimeSettings candidate,
            out string message)
            => DeucarianThemeRuntimeSettingsAssets.TryValidateRuntimeSettingsCandidate(candidate, out message);

        internal static IReadOnlyList<DeucarianThemeRuntimeSettings> FindRuntimeSettingsResourceAssets()
            => DeucarianThemeRuntimeSettingsAssets.FindRuntimeSettingsResourceAssets();

        internal static DeucarianThemeRuntimeSettings CreateRuntimeSettingsAtPath(string assetPath)
            => DeucarianThemeRuntimeSettingsAssets.CreateRuntimeSettingsAtPath(assetPath);

        internal static bool IsRuntimeSettingsResourcePath(string assetPath)
            => DeucarianThemeRuntimeSettingsAssets.IsRuntimeSettingsResourcePath(assetPath);

        internal static bool CanCreateRuntimeSettings(
            int existingRuntimeSettingsCount,
            bool isPlaying)
            => DeucarianThemeRuntimeSettingsAssets.CanCreateRuntimeSettings(existingRuntimeSettingsCount, isPlaying);

        internal static bool IsComposerDraftDirty(
            DeucarianThemeStyle comparison,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size,
            DeucarianThemeTypographyProfile typography)
            => DeucarianThemeDraftPolicy.IsComposerDraftDirty(comparison, surface, corners, border, size, typography);

        internal static IReadOnlyList<string> CollectPendingChangeDescriptions(
            DeucarianThemeManagerActivationStatus status,
            bool surfaceDirty,
            bool cornersDirty,
            bool borderDirty,
            bool sizeDirty,
            bool typographyDirty,
            bool runtimeSettingsDirty)
            => DeucarianThemeDraftPolicy.CollectPendingChangeDescriptions(status, surfaceDirty, cornersDirty, borderDirty, sizeDirty, typographyDirty, runtimeSettingsDirty);

        internal static string BuildDeveloperToolConfirmationMessage(
            string actionName,
            string description)
            => DeucarianThemeEditorConfirmations.BuildDeveloperToolConfirmationMessage(actionName, description);

        internal static bool ConfirmDeveloperToolAction(
            string actionName,
            string description,
            Func<string, string, string, string, bool> confirmation = null)
            => DeucarianThemeEditorConfirmations.ConfirmDeveloperToolAction(actionName, description, confirmation);

        internal static bool TryExecuteDeveloperToolAction(
            string actionName,
            string description,
            Action action,
            Func<string, string, string, string, bool> confirmation = null)
            => DeucarianThemeEditorConfirmations.TryExecuteDeveloperToolAction(actionName, description, action, confirmation);

        internal static bool ShouldKeepCurrentComposerDraft(
            string currentStyleName,
            string requestedStyleName,
            Func<string, string, string, string, string, int> showDialog = null)
            => DeucarianThemeEditorConfirmations.ShouldKeepCurrentComposerDraft(currentStyleName, requestedStyleName, showDialog);

    }
}
