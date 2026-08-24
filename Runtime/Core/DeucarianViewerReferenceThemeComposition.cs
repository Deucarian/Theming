using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Installed reference-theme runtime shared by viewer products.</summary>
    public sealed class DeucarianViewerReferenceThemeRuntime
    {
        internal DeucarianViewerReferenceThemeRuntime(
            DeucarianThemeProvider provider,
            DeucarianThemeModeController modeController,
            DeucarianViewerThemeSnapshotPublisher snapshotPublisher)
        {
            Provider = provider;
            ModeController = modeController;
            SnapshotPublisher = snapshotPublisher;
        }

        public DeucarianThemeProvider Provider { get; }
        public DeucarianThemeModeController ModeController { get; }
        public DeucarianViewerThemeSnapshotPublisher SnapshotPublisher { get; }
    }

    /// <summary>
    /// Installs the exact reference theme, mode persistence, and snapshot
    /// projection used by reusable viewer compositions.
    /// </summary>
    public static class DeucarianViewerReferenceThemeComposition
    {
        public static DeucarianViewerReferenceThemeRuntime Install(
            GameObject host,
            DeucarianThemeProvider preferredProvider = null,
            string preferenceKey =
                DeucarianThemeModeController.DefaultPreferenceKey,
            Action<string> publishSnapshot = null)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            DeucarianThemeProvider provider = preferredProvider ??
                host.GetComponent<DeucarianThemeProvider>();
            if (provider == null)
            {
                provider = host.AddComponent<DeucarianThemeProvider>();
            }

            DeucarianThemeModeController modeController =
                host.GetComponent<DeucarianThemeModeController>();
            if (modeController == null)
            {
                modeController =
                    host.AddComponent<DeucarianThemeModeController>();
            }

            modeController.BindReferenceTheme(provider, preferenceKey);

            DeucarianViewerThemeSnapshotPublisher snapshotPublisher =
                host.GetComponent<DeucarianViewerThemeSnapshotPublisher>();
            if (snapshotPublisher == null)
            {
                snapshotPublisher = host.AddComponent<
                    DeucarianViewerThemeSnapshotPublisher>();
            }

            snapshotPublisher.Bind(provider, publishSnapshot);
            return new DeucarianViewerReferenceThemeRuntime(
                provider,
                modeController,
                snapshotPublisher);
        }
    }
}
