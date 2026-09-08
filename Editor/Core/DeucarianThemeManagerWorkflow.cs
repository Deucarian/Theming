using System;
using System.Collections.Generic;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeManagerWorkflow
    {
        internal static int PreviewRepaintVersion => DeucarianThemeSceneApplication.PreviewRepaintVersion;

        public static DeucarianThemeManagerActivationResult Activate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
            => DeucarianThemeActivationTransaction.Activate(settings, selection, providers);

        public static DeucarianThemeManagerActivationResult Activate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            DeucarianThemeManagerStyleEdit styleEdit,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
            => DeucarianThemeActivationTransaction.Activate(settings, selection, styleEdit, providers);

        public static DeucarianThemeManagerActivationStatus Evaluate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
            => DeucarianThemeSelectionPolicy.Evaluate(settings, selection, providers);

        public static DeucarianThemeManagerActivationStatus Evaluate(
            DeucarianThemeRuntimeSettings settings,
            DeucarianThemeManagerSelection selection,
            bool runtimeSettingsResourceReady,
            string runtimeSettingsResourceMessage,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
            => DeucarianThemeSelectionPolicy.Evaluate(settings, selection, runtimeSettingsResourceReady, runtimeSettingsResourceMessage, providers);

        public static bool TryValidateSelection(
            DeucarianThemeManagerSelection selection,
            out string message)
            => DeucarianThemeSelectionPolicy.TryValidateSelection(selection, out message);

        internal static bool IsFamilyReadyForRuntimeSettings(DeucarianThemeFamily family)
            => DeucarianThemeSelectionPolicy.IsFamilyReadyForRuntimeSettings(family);

        internal static bool TryResolveSharedStyle(
            DeucarianThemeFamily family,
            out DeucarianThemeStyle style)
            => DeucarianThemeSelectionPolicy.TryResolveSharedStyle(family, out style);

        internal static bool IsPartialComposition(DeucarianThemeStyle style)
            => DeucarianThemeSelectionPolicy.IsPartialComposition(style);

        internal static int Preview(
            DeucarianThemeManagerSelection selection,
            IReadOnlyList<DeucarianThemeProvider> providers = null)
            => DeucarianThemeSceneApplication.Preview(selection, providers);

        internal static int ClearPreview(IReadOnlyList<DeucarianThemeProvider> providers = null)
            => DeucarianThemeSceneApplication.ClearPreview(providers);

        internal static bool AreProvidersSynchronized(
            IReadOnlyList<DeucarianThemeProvider> providers,
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            DeucarianThemeStyle style,
            bool sharedStyleSynchronized)
            => DeucarianThemeSceneApplication.AreProvidersSynchronized(providers, family, mode, style, sharedStyleSynchronized);

        internal static IReadOnlyList<DeucarianThemeProvider> FindOpenSceneProviders()
            => DeucarianThemeSceneApplication.FindOpenSceneProviders();

    }
}
