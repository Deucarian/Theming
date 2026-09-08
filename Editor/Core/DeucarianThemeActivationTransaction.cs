using System;
using System.Collections.Generic;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeActivationTransaction
    {
        private const string UndoLabel = "Activate Deucarian Theme";

        public static DeucarianThemeManagerActivationResult Activate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
        {
            return Activate(settings, selection, null, providers);
        }

        public static DeucarianThemeManagerActivationResult Activate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            DeucarianThemeManagerStyleEdit styleEdit,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
        {
            return Activate(settings, selection, (DeucarianThemeManagerStyleEdit?)styleEdit, providers);
        }

        internal static DeucarianThemeManagerActivationResult Activate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            DeucarianThemeManagerStyleEdit? styleEdit,
            IReadOnlyList<DeucarianThemeProvider> providers)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return Failure("Exit Play Mode before activating a theme for builds.");
            }

            if (settings == null)
            {
                return Failure("Runtime settings are missing. Configure them before activating a theme.");
            }

            if (!DeucarianThemeSelectionPolicy.IsFamilyReadyForRuntimeSettings(selection.Family))
            {
                return Failure(
                    "The selected theme family must include complete Light and Dark themes before activation.");
            }

            if (!DeucarianThemeSelectionPolicy.TryValidateRuntimeSettingsResource(settings, out string settingsResourceMessage))
            {
                return Failure(settingsResourceMessage);
            }

            if (styleEdit.HasValue
                && (!styleEdit.Value.IsValid || styleEdit.Value.Target != selection.Style))
            {
                return Failure(
                    "The staged custom style edit must target the selected style and include all four components.");
            }

            if (!DeucarianThemeSelectionPolicy.TryValidateSelection(
                    selection,
                    styleEdit.HasValue,
                    out string validationMessage))
            {
                return Failure(validationMessage);
            }

            IReadOnlyList<DeucarianThemeProvider> resolvedProviders = providers ?? DeucarianThemeSceneApplication.FindOpenSceneProviders();
            DeucarianTheme lightTheme = selection.Family.LightTheme;
            DeucarianTheme darkTheme = selection.Family.DarkTheme;
            bool settingsChanged = settings.DefaultThemeFamily != selection.Family
                                   || settings.DefaultThemeMode != selection.Mode;
            bool lightStyleChanged = lightTheme.VisualStyle != selection.Style;
            bool darkStyleChanged = darkTheme.VisualStyle != selection.Style;
            bool styleCompositionChanged = styleEdit.HasValue
                                           && DoesStyleCompositionDiffer(styleEdit.Value);
            var providerChanges = new bool[resolvedProviders.Count];
            int loadedProviderCount = 0;
            for (int i = 0; i < resolvedProviders.Count; i++)
            {
                DeucarianThemeProvider provider = resolvedProviders[i];
                if (!DeucarianThemeSceneApplication.IsOpenSceneProvider(provider))
                {
                    continue;
                }

                loadedProviderCount++;
                providerChanges[i] = !DeucarianThemeSceneApplication.IsProviderSynchronized(provider, selection);
            }

            int undoGroup = BeginUndoGroup();
            UnityEngine.Object consolidatedAsset = styleCompositionChanged
                ? (UnityEngine.Object)styleEdit.Value.Target
                : selection.Family;
            IDisposable notificationBatch = DeucarianThemeAssetChangeBus.BeginBatch(consolidatedAsset);

            try
            {
                if (settingsChanged)
                {
                    Undo.RecordObject(settings, UndoLabel);
                }

                if (lightStyleChanged)
                {
                    Undo.RecordObject(lightTheme, UndoLabel);
                }

                if (darkStyleChanged && darkTheme != lightTheme)
                {
                    Undo.RecordObject(darkTheme, UndoLabel);
                }

                if (styleCompositionChanged)
                {
                    Undo.RecordObject(styleEdit.Value.Target, UndoLabel);
                }

                for (int i = 0; i < resolvedProviders.Count; i++)
                {
                    DeucarianThemeProvider provider = resolvedProviders[i];
                    if (providerChanges[i])
                    {
                        Undo.RecordObject(provider, UndoLabel);
                    }
                }

                if (lightStyleChanged || darkStyleChanged)
                {
                    selection.Family.SetSharedVisualStyle(selection.Style);
                }

                if (settingsChanged)
                {
                    settings.Configure(selection.Family, selection.Mode);
                }

                if (lightStyleChanged)
                {
                    EditorUtility.SetDirty(lightTheme);
                }

                if (darkStyleChanged && darkTheme != lightTheme)
                {
                    EditorUtility.SetDirty(darkTheme);
                }

                if (settingsChanged)
                {
                    EditorUtility.SetDirty(settings);
                }

                int providerCount = 0;
                for (int i = 0; i < resolvedProviders.Count; i++)
                {
                    DeucarianThemeProvider provider = resolvedProviders[i];
                    if (!providerChanges[i])
                    {
                        continue;
                    }

                    provider.SetThemeFamily(selection.Family, selection.Mode);
                    if (provider.ConfiguredStyleOverride != null)
                    {
                        provider.ClearStyleOverride();
                    }

                    EditorUtility.SetDirty(provider);
                    EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
                    providerCount++;
                }

                if (styleCompositionChanged)
                {
                    DeucarianThemeManagerStyleEdit edit = styleEdit.Value;
                    edit.Target.SetComposition(
                        edit.Surface,
                        edit.Corners,
                        edit.Border,
                        edit.Size,
                        edit.Typography,
                        true);
                    EditorUtility.SetDirty(edit.Target);
                }

                if (lightStyleChanged)
                {
                    SaveIfDirty(lightTheme);
                }

                if (darkStyleChanged && darkTheme != lightTheme)
                {
                    SaveIfDirty(darkTheme);
                }

                if (settingsChanged)
                {
                    SaveIfDirty(settings);
                }

                if (styleCompositionChanged)
                {
                    SaveIfDirty(styleEdit.Value.Target);
                }

                Undo.CollapseUndoOperations(undoGroup);
                DeucarianThemingEditorSettings.SetDraftSelection(
                    selection.Family,
                    selection.Mode,
                    selection.Style);

                string providerNote;
                if (providerCount > 0)
                {
                    providerNote = $" Synchronized {providerCount} loaded scene provider(s).";
                }
                else if (loadedProviderCount == 0)
                {
                    providerNote = " No loaded scene provider was present; the project default remains ready.";
                }
                else
                {
                    providerNote = " Loaded scene providers already matched the staged selection.";
                }

                string successMessage = $"Activated '{selection.Family.DisplayName}' in {selection.Mode} mode.{providerNote}";
                ThemingLog.Editor.Info(successMessage, settings);
                return new DeucarianThemeManagerActivationResult(true, providerCount, successMessage);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                if (lightStyleChanged)
                {
                    SaveIfDirty(lightTheme);
                }

                if (darkStyleChanged && darkTheme != lightTheme)
                {
                    SaveIfDirty(darkTheme);
                }

                if (settingsChanged)
                {
                    SaveIfDirty(settings);
                }

                if (styleCompositionChanged)
                {
                    SaveIfDirty(styleEdit.Value.Target);
                }

                for (int i = 0; i < resolvedProviders.Count; i++)
                {
                    DeucarianThemeProvider provider = resolvedProviders[i];
                    if (DeucarianThemeSceneApplication.IsOpenSceneProvider(provider))
                    {
                        try
                        {
                            provider.RefreshThemeGraph();
                        }
                        catch (Exception refreshException)
                        {
                            ThemingLog.Editor.Exception(
                                refreshException,
                                "A provider failed while restoring its theme after activation rollback.",
                                provider);
                        }
                    }
                }

                DeucarianThemeAssetChangeBus.NotifyChanged(selection.Family);
                string failureMessage = "Theme activation was rolled back: " + exception.Message;
                ThemingLog.Editor.Error(failureMessage, settings);
                return Failure(failureMessage);
            }
            finally
            {
                notificationBatch.Dispose();
            }
        }

        internal static bool DoesStyleCompositionDiffer(
            DeucarianThemeManagerStyleEdit edit)
        {
            return edit.Target.SurfaceProfile != edit.Surface
                   || edit.Target.ShapeProfile != edit.Corners
                   || edit.Target.StrokeProfile != edit.Border
                   || edit.Target.Density != edit.Size
                   || edit.Target.TypographyProfile != edit.Typography;
        }

        internal static int BeginUndoGroup()
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            return group;
        }

        internal static void SaveIfDirty(UnityEngine.Object asset)
        {
            if (asset != null && AssetDatabase.Contains(asset))
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        internal static DeucarianThemeManagerActivationResult Failure(string message)
        {
            return new DeucarianThemeManagerActivationResult(false, 0, message);
        }

    }
}
