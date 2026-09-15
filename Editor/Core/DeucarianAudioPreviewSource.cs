using System;
using Deucarian.Media;
using Deucarian.Media.Unity;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>Owns a temporary editor audition source; never edits the clip, scene listener or importer.</summary>
    internal sealed class DeucarianAudioPreviewSource : IDisposable
    {
        private IMediaResourceLease<GameObject> owner;
        private AudioSource source;
        private double startedAt;
        internal AudioSource Source => source;
        internal bool IsPlaying => source != null && source.isPlaying;

        internal void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            Dispose();
            var instance = new GameObject("Deucarian audio audition") { hideFlags = HideFlags.HideAndDontSave };
            owner = UnityMediaResourceLease.CreateOwned(instance);
            try
            {
                source = instance.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0;
                source.dopplerLevel = 0;
                source.ignoreListenerPause = true;
                source.ignoreListenerVolume = true;
                source.bypassEffects = true;
                source.bypassListenerEffects = true;
                source.bypassReverbZones = true;
                source.clip = clip;
                source.volume = DeucarianAudioPlaybackModifiers.Identity.ApplyVolume(volume);
                source.pitch = DeucarianAudioPlaybackModifiers.Identity.ApplyPitch(pitch);
                source.Play();
                startedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update += Update;
                AssemblyReloadEvents.beforeAssemblyReload += Dispose;
                EditorApplication.quitting += Dispose;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }
            catch { Dispose(); throw; }
        }

        private void Update()
        {
            // The audio thread can report false during the first editor frame.
            if (!IsPlaying && EditorApplication.timeSinceStartup - startedAt > .1) Dispose();
        }

        private void OnPlayModeChanged(PlayModeStateChange state) => Dispose();

        public void Dispose()
        {
            EditorApplication.update -= Update;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            EditorApplication.quitting -= Dispose;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (source != null) { source.Stop(); source.clip = null; }
            source = null;
            owner?.Dispose();
            owner = null;
        }
    }
}
