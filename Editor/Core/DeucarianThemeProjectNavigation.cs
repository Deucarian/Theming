using System;
using UnityEditor;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeProjectNavigation
    {
        internal const string ProjectPaletteRoute = "project-palette";

        internal static bool TrySelectProjectPalette(DeucarianThemeRuntimeSettings settings,
            bool hasComposerChanges, Func<string, string, string, string, bool> confirm = null)
        {
            if (settings == null || settings.DefaultThemeFamily == null) return false;
            var current = DeucarianThemeManagerSelection.FromEditorPrefs();
            var project = DeucarianThemeDraftPolicy.ResolveProjectSelection(settings);
            bool differentSelection = current.Family != project.Family || current.Mode != project.Mode
                || current.Style != project.Style;
            bool hasPaletteChanges = current.ResolvedPalette != null && EditorUtility.IsDirty(current.ResolvedPalette);
            if (differentSelection && (hasComposerChanges || hasPaletteChanges))
            {
                var ask = confirm ?? EditorUtility.DisplayDialog;
                if (!ask("Open Project Visual Palette?",
                    "The current palette or style has unapplied work. Open " + project.Family.name
                    + " instead? Your palette edits and composer draft will be kept.",
                    "Open project palette", "Keep editing")) return false;
            }
            DeucarianThemingEditorSettings.SetDraftSelection(project.Family, project.Mode, project.Style);
            return true;
        }
    }
}
