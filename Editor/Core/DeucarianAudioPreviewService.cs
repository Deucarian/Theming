using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal interface IDeucarianAudioPreviewService
    {
        bool IsAvailable { get; }
        bool IsPlaying { get; }
        bool Play(AudioClip clip);
        void Stop();
    }

    /// <summary>Contains Unity's editor-only preview reflection behind one testable boundary.</summary>
    internal sealed class DeucarianAudioPreviewService : IDeucarianAudioPreviewService
    {
        private readonly MethodInfo playMethod;
        private readonly MethodInfo stopMethod;
        private readonly MethodInfo isPlayingMethod;

        public DeucarianAudioPreviewService()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null)
            {
                return;
            }

            playMethod = FindPlayMethod(audioUtil, "PlayPreviewClip", "PlayClip");
            stopMethod = FindParameterlessMethod(audioUtil, "StopAllPreviewClips", "StopAllClips");
            isPlayingMethod = FindParameterlessMethod(audioUtil, "IsPreviewClipPlaying", "IsClipPlaying");
        }

        public bool IsAvailable => playMethod != null && stopMethod != null;

        public bool IsPlaying
        {
            get
            {
                if (isPlayingMethod == null)
                {
                    return false;
                }

                try
                {
                    return isPlayingMethod.Invoke(null, Array.Empty<object>()) is bool playing && playing;
                }
                catch (TargetInvocationException)
                {
                    return false;
                }
            }
        }

        public bool Play(AudioClip clip)
        {
            if (!IsAvailable || clip == null)
            {
                return false;
            }

            try
            {
                Stop();
                ParameterInfo[] parameters = playMethod.GetParameters();
                object[] arguments = parameters.Length == 1
                    ? new object[] { clip }
                    : parameters.Length == 2
                        ? new object[] { clip, 0 }
                        : new object[] { clip, 0, false };
                playMethod.Invoke(null, arguments);
                return true;
            }
            catch (TargetInvocationException)
            {
                return false;
            }
            catch (TargetParameterCountException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public void Stop()
        {
            if (stopMethod == null)
            {
                return;
            }

            try
            {
                stopMethod.Invoke(null, Array.Empty<object>());
            }
            catch (TargetInvocationException)
            {
                // Preview is an optional editor convenience; runtime audio is unaffected.
            }
        }

        private static MethodInfo FindPlayMethod(Type type, params string[] names)
        {
            MethodInfo[] candidates = type.GetMethods(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < names.Length; i++)
            {
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    ParameterInfo[] parameters = candidates[candidateIndex].GetParameters();
                    if (string.Equals(candidates[candidateIndex].Name, names[i], StringComparison.Ordinal) &&
                        parameters.Length >= 1 && parameters.Length <= 3 &&
                        parameters[0].ParameterType == typeof(AudioClip))
                    {
                        return candidates[candidateIndex];
                    }
                }
            }

            return null;
        }

        private static MethodInfo FindParameterlessMethod(Type type, params string[] names)
        {
            MethodInfo[] candidates = type.GetMethods(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < names.Length; i++)
            {
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    if (string.Equals(candidates[candidateIndex].Name, names[i], StringComparison.Ordinal) &&
                        candidates[candidateIndex].GetParameters().Length == 0)
                    {
                        return candidates[candidateIndex];
                    }
                }
            }

            return null;
        }
    }
}
