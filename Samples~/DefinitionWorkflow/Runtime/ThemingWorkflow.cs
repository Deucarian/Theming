using System;
using UnityEngine;

namespace Deucarian.Theming.Samples.DefinitionWorkflow
{
    /// <summary>Small caller example. The configured scene hosts own services and resource lifetimes.</summary>
    public sealed class ThemingWorkflow : MonoBehaviour
    {
        [SerializeField] private AudioRoleKey role;
        [SerializeField] private ThemeAudioTrigger trigger;
        private string status = "Ready. Choose an action below.";
        public string Status => status;
        public void Play() { ThemeAudio.Play(role); status = "Played the selected semantic audio role."; }
        public void PlayComponent() { trigger.Play(); status = "Played through ThemeAudioTrigger."; }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, Math.Min(540, Screen.width - 48), Screen.height - 48), GUI.skin.box);
            GUILayout.Label("Theming — definition workflow");
            GUILayout.Label("The role selects reusable audio defaults. Change its clip in Audio Definitions; callers keep the same key.");
            GUILayout.Space(12);
            if (GUILayout.Button("Play with C#", GUILayout.Height(32))) { try { Play(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Play with component", GUILayout.Height(32))) { try { PlayComponent(); } catch (Exception error) { status = error.Message; } }
            GUILayout.Space(12);
            GUILayout.Label(status);
            GUILayout.EndArea();
        }
    }
}
