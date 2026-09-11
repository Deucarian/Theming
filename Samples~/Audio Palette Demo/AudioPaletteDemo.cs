using Deucarian.Media.Unity;
using UnityEngine;

namespace Deucarian.Theming.Samples.AudioPalette
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(UnityAudioOneShotOutput))]
    [RequireComponent(typeof(DeucarianThemeAudioPlayer))]
    public sealed class AudioPaletteDemo : MonoBehaviour
    {
        [SerializeField] private DeucarianThemeAudioPlayer player;
        [SerializeField] private DeucarianAudioPaletteSet paletteSet;
        [SerializeField] private DeucarianAudioExperience experience = DeucarianAudioExperience.Default;

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

            if (player == null) player = GetComponent<DeucarianThemeAudioPlayer>();
            if (player == null)
            {
                player = gameObject.AddComponent<DeucarianThemeAudioPlayer>();
            }

            player.Output = output;
            player.PaletteSetOverride = paletteSet != null ? paletteSet : DeucarianAudioDefaults.LoadPaletteSet();
            player.UseProviderExperience = false;
            player.ExperienceOverride = experience;
        }

        private void Use(DeucarianAudioExperience experience)
        {
            this.experience = experience;
            player.ExperienceOverride = experience;
        }

        private void Play(string roleId)
        {
            player.PlayRoleById(roleId);
        }
    }
}
