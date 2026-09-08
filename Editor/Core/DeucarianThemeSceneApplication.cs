using System;
using System.Collections.Generic;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeSceneApplication
    {
        internal static int PreviewRepaintVersion { get; private set; }

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

        internal static void RequestPreviewRepaint()
        {
            PreviewRepaintVersion++;
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            InternalEditorUtility.RepaintAllViews();
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

        internal static bool IsProviderSynchronized(
            DeucarianThemeProvider provider,
            DeucarianThemeManagerSelection selection)
        {
            return provider != null
                   && provider.ConfiguredThemeFamily == selection.Family
                   && provider.ConfiguredThemeMode == selection.Mode
                   && provider.ConfiguredStyleOverride == null
                   && provider.ConfiguredStyle == selection.Style;
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

        internal static bool IsOpenSceneProvider(DeucarianThemeProvider provider)
        {
            return provider != null
                   && provider.gameObject != null
                   && provider.gameObject.scene.IsValid()
                   && provider.gameObject.scene.isLoaded;
        }

    }
}
