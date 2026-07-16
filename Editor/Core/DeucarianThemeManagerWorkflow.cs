using System;
using System.Collections.Generic;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Immutable staged selection used by the Theme Manager. Creating a selection never changes
    /// theme assets or scene providers.
    /// </summary>
    internal readonly struct DeucarianThemeManagerSelection
    {
        public DeucarianThemeManagerSelection(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            DeucarianThemeStyle style)
        {
            Family = family;
            Mode = mode == DeucarianThemeMode.Light
                ? DeucarianThemeMode.Light
                : DeucarianThemeMode.Dark;
            Style = style;
        }

        public DeucarianThemeFamily Family { get; }

        public DeucarianThemeMode Mode { get; }

        public DeucarianThemeStyle Style { get; }

        public DeucarianTheme ResolvedTheme => Family != null ? Family.ResolveTheme(Mode) : null;

        public DeucarianColorPalette ResolvedPalette =>
            ResolvedTheme != null ? ResolvedTheme.ColorPalette : null;

        public DeucarianColorRoleLibrary ResolvedRoleLibrary =>
            ResolvedPalette != null ? ResolvedPalette.RoleLibrary : null;

        public static DeucarianThemeManagerSelection FromEditorPrefs()
        {
            return new DeucarianThemeManagerSelection(
                DeucarianThemingEditorSettings.ActiveThemeFamily,
                DeucarianThemingEditorSettings.ActiveThemeMode,
                DeucarianThemingEditorSettings.ActiveStyle);
        }
    }

    /// <summary>Comparison between the staged selection and the source-controlled project state.</summary>
    internal readonly struct DeucarianThemeManagerActivationStatus
    {
        public DeucarianThemeManagerActivationStatus(
            bool hasRuntimeSettings,
            bool runtimeSettingsReady,
            bool familyDirty,
            bool modeDirty,
            bool styleDirty,
            bool sharedStyleSynchronized,
            bool providersSynchronized,
            bool selectionValid,
            string message)
        {
            HasRuntimeSettings = hasRuntimeSettings;
            RuntimeSettingsReady = runtimeSettingsReady;
            FamilyDirty = familyDirty;
            ModeDirty = modeDirty;
            StyleDirty = styleDirty;
            SharedStyleSynchronized = sharedStyleSynchronized;
            ProvidersSynchronized = providersSynchronized;
            SelectionValid = selectionValid;
            Message = message ?? string.Empty;
        }

        public bool HasRuntimeSettings { get; }

        public bool RuntimeSettingsReady { get; }

        public bool FamilyDirty { get; }

        public bool ModeDirty { get; }

        public bool StyleDirty { get; }

        public bool SharedStyleSynchronized { get; }

        public bool ProvidersSynchronized { get; }

        public bool SelectionValid { get; }

        public string Message { get; }

        public bool HasDraftChanges => FamilyDirty || ModeDirty || StyleDirty;

        public bool IsActive => RuntimeSettingsReady
                                && SelectionValid
                                && !HasDraftChanges
                                && SharedStyleSynchronized
                                && ProvidersSynchronized;

        public bool CanActivate => RuntimeSettingsReady && SelectionValid && !IsActive;
    }

    internal readonly struct DeucarianThemeManagerActivationResult
    {
        public DeucarianThemeManagerActivationResult(bool succeeded, int providerCount, string message)
        {
            Succeeded = succeeded;
            ProviderCount = providerCount;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }

        public int ProviderCount { get; }

        public string Message { get; }
    }

    internal readonly struct DeucarianThemeManagerStyleEdit
    {
        public DeucarianThemeManagerStyleEdit(
            DeucarianThemeStyle target,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size)
            : this(
                target,
                surface,
                corners,
                border,
                size,
                target != null ? target.TypographyProfile : null)
        {
        }

        public DeucarianThemeManagerStyleEdit(
            DeucarianThemeStyle target,
            DeucarianThemeSurfaceProfile surface,
            DeucarianThemeShapeProfile corners,
            DeucarianThemeStrokeProfile border,
            DeucarianThemeDensity size,
            DeucarianThemeTypographyProfile typography)
        {
            Target = target;
            Surface = surface;
            Corners = corners;
            Border = border;
            Size = size;
            Typography = typography;
        }

        public DeucarianThemeStyle Target { get; }

        public DeucarianThemeSurfaceProfile Surface { get; }

        public DeucarianThemeShapeProfile Corners { get; }

        public DeucarianThemeStrokeProfile Border { get; }

        public DeucarianThemeDensity Size { get; }

        public DeucarianThemeTypographyProfile Typography { get; }

        public bool IsValid => Target != null
                               && Target.IsCustomStyle
                               && Surface != null
                               && Corners != null
                               && Border != null
                               && Size != DeucarianThemeDensity.Unspecified;
    }

    /// <summary>
    /// Testable Theme Manager state comparison and the single transactional activation path.
    /// </summary>
    internal static class DeucarianThemeManagerWorkflow
    {
        private const string UndoLabel = "Activate Deucarian Theme";

        internal static int PreviewRepaintVersion { get; private set; }

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
            bool providersSynchronized = AreProvidersSynchronized(
                providers ?? FindOpenSceneProviders(),
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

        public static DeucarianThemeManagerActivationResult Activate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
        {
            return Activate(settings, selection, null, providers);
        }

        /// <summary>
        /// Applies the staged editor presentation without changing provider serialization,
        /// project runtime settings, or source-controlled theme assets.
        /// </summary>
        internal static int Preview(
            DeucarianThemeManagerSelection selection,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
        {
            IReadOnlyList<DeucarianThemeProvider> resolvedProviders = providers ?? FindOpenSceneProviders();
            int previewed = 0;
            for (int i = 0; i < resolvedProviders.Count; i++)
            {
                DeucarianThemeProvider provider = resolvedProviders[i];
                if (!IsOpenSceneProvider(provider))
                {
                    continue;
                }

                if (selection.Family == null)
                {
                    provider.ClearEditorPreview();
                }
                else
                {
                    provider.SetEditorPreview(selection.Family, selection.Mode, selection.Style);
                }

                previewed++;
            }

            if (previewed > 0)
            {
                RequestPreviewRepaint();
            }

            return previewed;
        }

        internal static int ClearPreview(IReadOnlyList<DeucarianThemeProvider> providers = null)
        {
            IReadOnlyList<DeucarianThemeProvider> resolvedProviders = providers ?? FindOpenSceneProviders();
            int cleared = 0;
            for (int i = 0; i < resolvedProviders.Count; i++)
            {
                DeucarianThemeProvider provider = resolvedProviders[i];
                if (IsOpenSceneProvider(provider) && provider.ClearEditorPreview())
                {
                    cleared++;
                }
            }

            if (cleared > 0)
            {
                RequestPreviewRepaint();
            }

            return cleared;
        }

        private static void RequestPreviewRepaint()
        {
            PreviewRepaintVersion++;
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            InternalEditorUtility.RepaintAllViews();
        }

        public static DeucarianThemeManagerActivationResult Activate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            DeucarianThemeManagerStyleEdit styleEdit,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
        {
            return Activate(settings, selection, (DeucarianThemeManagerStyleEdit?)styleEdit, providers);
        }

        private static DeucarianThemeManagerActivationResult Activate(
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

            if (!IsFamilyReadyForRuntimeSettings(selection.Family))
            {
                return Failure(
                    "The selected theme family must include complete Light and Dark themes before activation.");
            }

            if (!TryValidateRuntimeSettingsResource(settings, out string settingsResourceMessage))
            {
                return Failure(settingsResourceMessage);
            }

            if (styleEdit.HasValue
                && (!styleEdit.Value.IsValid || styleEdit.Value.Target != selection.Style))
            {
                return Failure(
                    "The staged custom style edit must target the selected style and include all four components.");
            }

            if (!TryValidateSelection(
                    selection,
                    styleEdit.HasValue,
                    out string validationMessage))
            {
                return Failure(validationMessage);
            }

            IReadOnlyList<DeucarianThemeProvider> resolvedProviders = providers ?? FindOpenSceneProviders();
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
                if (!IsOpenSceneProvider(provider))
                {
                    continue;
                }

                loadedProviderCount++;
                providerChanges[i] = !IsProviderSynchronized(provider, selection);
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
                    if (IsOpenSceneProvider(provider))
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

        public static bool TryValidateSelection(
            DeucarianThemeManagerSelection selection,
            out string message)
        {
            return TryValidateSelection(selection, false, out message);
        }

        private static bool TryValidateSelection(
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

        private static bool TryValidateRuntimeSettingsResource(
            DeucarianThemeRuntimeSettings settings,
            out string message)
        {
            if (settings == null)
            {
                message = "Runtime settings are required before this theme can be activated.";
                return false;
            }

            string path = AssetDatabase.GetAssetPath(settings);
            if (!DeucarianThemeManagerWindow.IsRuntimeSettingsResourcePath(path))
            {
                // Explicit settings supplied by editor tests and legacy tooling are still supported.
                // The Theme Manager itself always supplies the Resources-resolved project asset.
                message = string.Empty;
                return true;
            }

            return DeucarianThemeManagerWindow.TryValidateRuntimeSettingsCandidate(
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

        internal static bool AreProvidersSynchronized(
            IReadOnlyList<DeucarianThemeProvider> providers,
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            DeucarianThemeStyle style,
            bool sharedStyleSynchronized)
        {
            if (!sharedStyleSynchronized || family == null)
            {
                return false;
            }

            if (providers == null)
            {
                return true;
            }

            for (int i = 0; i < providers.Count; i++)
            {
                DeucarianThemeProvider provider = providers[i];
                if (!IsOpenSceneProvider(provider))
                {
                    continue;
                }

                if (!IsProviderSynchronized(
                        provider,
                        new DeucarianThemeManagerSelection(family, mode, style)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsProviderSynchronized(
            DeucarianThemeProvider provider,
            DeucarianThemeManagerSelection selection)
        {
            return provider != null
                   && provider.ConfiguredThemeFamily == selection.Family
                   && provider.ConfiguredThemeMode == selection.Mode
                   && provider.ConfiguredStyleOverride == null
                   && provider.ConfiguredStyle == selection.Style;
        }

        private static bool DoesStyleCompositionDiffer(
            DeucarianThemeManagerStyleEdit edit)
        {
            return edit.Target.SurfaceProfile != edit.Surface
                   || edit.Target.ShapeProfile != edit.Corners
                   || edit.Target.StrokeProfile != edit.Border
                   || edit.Target.Density != edit.Size
                   || edit.Target.TypographyProfile != edit.Typography;
        }

        internal static IReadOnlyList<DeucarianThemeProvider> FindOpenSceneProviders()
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            DeucarianThemeProvider[] providers = UnityEngine.Object.FindObjectsByType<DeucarianThemeProvider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            DeucarianThemeProvider[] providers = UnityEngine.Object.FindObjectsOfType<DeucarianThemeProvider>(true);
#pragma warning restore CS0618
#endif
            return providers;
        }

        private static bool IsOpenSceneProvider(DeucarianThemeProvider provider)
        {
            return provider != null
                   && provider.gameObject != null
                   && provider.gameObject.scene.IsValid()
                   && provider.gameObject.scene.isLoaded;
        }

        private static int BeginUndoGroup()
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            return group;
        }

        private static void SaveIfDirty(UnityEngine.Object asset)
        {
            if (asset != null && AssetDatabase.Contains(asset))
            {
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }

        private static DeucarianThemeManagerActivationResult Failure(string message)
        {
            return new DeucarianThemeManagerActivationResult(false, 0, message);
        }
    }
}
