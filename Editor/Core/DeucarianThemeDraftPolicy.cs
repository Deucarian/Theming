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
    internal static class DeucarianThemeDraftPolicy
    {
        internal static DeucarianThemeStyle ResolveSuggestedStyle(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode)
        {
            if (family == null)
            {
                return null;
            }

            if (DeucarianThemeManagerWorkflow.TryResolveSharedStyle(family, out DeucarianThemeStyle sharedStyle))
            {
                return sharedStyle;
            }

            DeucarianTheme theme = family.ResolveTheme(mode);
            return theme != null ? theme.VisualStyle : null;
        }

        internal static void SetDraft(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            DeucarianThemeStyle style)
        {
            DeucarianThemingEditorSettings.SetDraftSelection(family, mode, style);
            DeucarianThemePreviewCoordinator.ApplySelectedPreview();
        }

        internal static bool IsComposerDraftDirty(
            DeucarianThemeStyle comparison,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size,
            DeucarianThemeTypographyProfile typography)
        {
            return comparison != null
                   && (surface != comparison.SurfaceProfile
                       || corners != comparison.ShapeProfile
                       || border != comparison.StrokeProfile
                       || size != comparison.Density
                       || typography != comparison.TypographyProfile);
        }

        internal static IReadOnlyList<string> CollectPendingChangeDescriptions(
            DeucarianThemeManagerActivationStatus status,
            bool surfaceDirty,
            bool cornersDirty,
            bool borderDirty,
            bool sizeDirty,
            bool typographyDirty,
            bool runtimeSettingsDirty)
        {
            var changes = new List<string>();
            if (status.FamilyDirty) changes.Add("Theme family");
            if (status.ModeDirty) changes.Add("Mode");
            if (status.StyleDirty) changes.Add("Visual style");
            if (surfaceDirty) changes.Add("Composer surface");
            if (cornersDirty) changes.Add("Composer corners");
            if (borderDirty) changes.Add("Composer border");
            if (sizeDirty) changes.Add("Composer size");
            if (typographyDirty) changes.Add("Composer typography");
            if (runtimeSettingsDirty) changes.Add("Runtime settings candidate");
            return changes;
        }

        internal static DeucarianThemeManagerSelection ResolveProjectSelection(
            DeucarianThemeRuntimeSettings settings)
        {
            if (settings == null)
            {
                return DeucarianThemeManagerSelection.FromEditorPrefs();
            }

            DeucarianThemeFamily family = settings.DefaultThemeFamily;
            DeucarianThemeMode mode = settings.DefaultThemeMode;
            DeucarianThemeStyle style;
            if (!DeucarianThemeManagerWorkflow.TryResolveSharedStyle(family, out style))
            {
                DeucarianTheme resolvedTheme = family != null ? family.ResolveTheme(mode) : settings.DefaultTheme;
                style = resolvedTheme != null ? resolvedTheme.VisualStyle : null;
            }

            return new DeucarianThemeManagerSelection(family, mode, style);
        }
    }
}
