using System;
using UnityEditor;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemeStyleSaveResult
    {
        internal DeucarianThemeStyleSaveResult(bool succeeded, DeucarianThemeStyle style, string message)
        {
            Succeeded = succeeded;
            Style = style;
            Message = message;
        }
        public bool Succeeded { get; }
        public DeucarianThemeStyle Style { get; }
        public string Message { get; }
    }

    internal static class DeucarianThemeStyleAuthoring
    {
        internal static DeucarianThemeStyleSaveResult SaveAndActivate(
            DeucarianThemeStyleDraft draft, DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection, bool saveAsNew, string newAssetPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return Failure("Exit Play Mode before saving or activating a custom style.");
            if (draft == null || !draft.IsComplete)
                return Failure("Choose Surface, Corners, Border and Size before saving.");
            if (!DeucarianThemeRuntimeSettingsAssets.TryValidateRuntimeSettingsCandidate(settings, out string reason))
                return Failure(reason);
            if (!DeucarianThemeManagerWorkflow.IsFamilyReadyForRuntimeSettings(selection.Family))
                return Failure("Complete the staged theme family before saving a custom style.");

            bool creating = draft.EditingStyle == null || saveAsNew;
            string path = DeucarianThemingEditorSettings.NormalizeAssetPath(newAssetPath);
            if (creating && (string.IsNullOrWhiteSpace(path) || AssetDatabase.LoadMainAssetAtPath(path) != null))
                return Failure("Choose an unused project asset path. Existing assets are never overwritten.");
            DeucarianThemeStyle created = null;
            try
            {
                DeucarianThemeStyle style = creating
                    ? created = DeucarianThemingMenuActions.CreateCustomStyle(
                        draft.Source, path, draft.Surface, draft.Corners, draft.Border, draft.Size, draft.Typography)
                    : draft.EditingStyle;
                if (style == null) return Failure("The custom style could not be saved.");
                var target = new DeucarianThemeManagerSelection(selection.Family, selection.Mode, style);
                DeucarianThemeManagerActivationResult result = creating
                    ? DeucarianThemeManagerWorkflow.Activate(settings, target)
                    : DeucarianThemeManagerWorkflow.Activate(settings, target,
                        new DeucarianThemeManagerStyleEdit(style, draft.Surface, draft.Corners,
                            draft.Border, draft.Size, draft.Typography));
                if (!result.Succeeded) return CleanupFailure(result.Message, created, path, selection);
                return new DeucarianThemeStyleSaveResult(true, style,
                    "Saved and activated the complete Custom Style '" + style.DisplayName +
                    "' for both Light and Dark themes. " + result.Message);
            }
            catch (Exception exception)
            {
                return CleanupFailure("Saving the style failed: " + exception.Message, created, path, selection);
            }
        }

        private static DeucarianThemeStyleSaveResult CleanupFailure(string message, DeucarianThemeStyle created,
            string path, DeucarianThemeManagerSelection previous)
        {
            if (created != null)
            {
                try
                {
                    bool owned = AssetDatabase.LoadMainAssetAtPath(path) == created;
                    bool removed = owned && AssetDatabase.DeleteAsset(path);
                    message += removed
                        ? " The newly created style was removed."
                        : " The new style could not be removed automatically. Inspect it before retrying.";
                }
                catch (Exception cleanupError)
                {
                    message += " Cleanup also failed: " + cleanupError.Message + " Inspect the new style before retrying.";
                }
            }
            try { DeucarianThemingEditorSettings.SetDraftSelection(previous.Family, previous.Mode, previous.Style); }
            catch (Exception restoreError) { message += " Restore the previous selection manually: " + restoreError.Message; }
            return Failure(message);
        }

        private static DeucarianThemeStyleSaveResult Failure(string message)
            => new DeucarianThemeStyleSaveResult(false, null, message);
    }
}
