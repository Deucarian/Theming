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

}
