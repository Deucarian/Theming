using Deucarian.Media.Unity;
using UnityEngine;

namespace Deucarian.Theming.Samples.AudioPalette
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(UnityAudioOneShotOutput))]
    [RequireComponent(typeof(DeucarianThemeAudioPlayer))]
    public sealed class AudioPaletteDemo : MonoBehaviour
    {
        private DeucarianThemeAudioPlayer player;

        public void UseDefault() => Use(DeucarianAudioExperience.Default);
        public void UseXR() => Use(DeucarianAudioExperience.XR);
        public void UseWebGL() => Use(DeucarianAudioExperience.WebGL);
        public void UseDesktop() => Use(DeucarianAudioExperience.Desktop);
        public void UseMobile() => Use(DeucarianAudioExperience.Mobile);

        public void PlayActivate() => Play(DeucarianBuiltinAudioRoleIds.Activate);
        public void PlayKey() => Play(DeucarianBuiltinAudioRoleIds.Key);
        public void PlayWarning() => Play(DeucarianBuiltinAudioRoleIds.Warning);

        private void Awake()
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            UnityAudioOneShotOutput output = GetComponent<UnityAudioOneShotOutput>();
            if (output == null)
            {
                output = gameObject.AddComponent<UnityAudioOneShotOutput>();
            }

            output.Template = source;

            player = GetComponent<DeucarianThemeAudioPlayer>();
            if (player == null)
            {
                player = gameObject.AddComponent<DeucarianThemeAudioPlayer>();
            }

            player.Output = output;
            player.PaletteSetOverride = DeucarianAudioDefaults.LoadPaletteSet();
            player.UseProviderExperience = false;
        }

        private void Use(DeucarianAudioExperience experience)
        {
            player.ExperienceOverride = experience;
        }

        private void Play(string roleId)
        {
            player.PlayRoleById(roleId);
        }
    }
}
