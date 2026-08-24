using System;
using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>
    /// Observes a provider and publishes canonical theme snapshots through an
    /// injected transport callback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeucarianViewerThemeSnapshotPublisher : MonoBehaviour
    {
        private DeucarianThemeProvider provider;
        private Action<string> publish;
        private bool subscribed;

        public DeucarianThemeProvider Provider => provider;
        public string LastPublishedJson { get; private set; }

        /// <summary>Binds the provider and optional consumer-owned transport.</summary>
        public void Bind(
            DeucarianThemeProvider targetProvider,
            Action<string> publishSnapshot)
        {
            if (provider != targetProvider)
            {
                Unsubscribe();
                provider = targetProvider;
            }

            publish = publishSnapshot;
            if (!isActiveAndEnabled)
            {
                return;
            }

            Subscribe();
            PublishCurrentTheme();
        }

        private void OnEnable()
        {
            Subscribe();
            PublishCurrentTheme();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            provider = null;
            publish = null;
        }

        private void Subscribe()
        {
            if (subscribed || provider == null)
            {
                return;
            }

            provider.ThemeChanged += OnThemeChanged;
            provider.StyleChanged += OnStyleChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (provider != null)
            {
                provider.ThemeChanged -= OnThemeChanged;
                provider.StyleChanged -= OnStyleChanged;
            }

            subscribed = false;
        }

        private void OnThemeChanged(DeucarianTheme theme)
        {
            PublishCurrentTheme();
        }

        private void OnStyleChanged(DeucarianThemeStyle style)
        {
            PublishCurrentTheme();
        }

        private void PublishCurrentTheme()
        {
            if (provider == null || provider.CurrentTheme == null)
            {
                return;
            }

            string json = DeucarianViewerThemeSnapshot.FromTheme(
                    provider.CurrentTheme,
                    provider.CurrentStyle)
                .ToJson();
            if (string.Equals(
                    LastPublishedJson,
                    json,
                    StringComparison.Ordinal))
            {
                return;
            }

            LastPublishedJson = json;
            publish?.Invoke(json);
        }
    }
}
