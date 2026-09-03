using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Deucarian.Theming
{
    /// <summary>
    /// Plays theme-defined semantic UI feedback sounds for Unity UI Selectable events.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class DeucarianSelectableThemeAudio : MonoBehaviour,
        IPointerEnterHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        ISelectHandler,
        ISubmitHandler,
        ICancelHandler
    {
        [SerializeField] private DeucarianTheme themeOverride;
        [SerializeField] private DeucarianThemeAudioPlayer audioPlayer;
        [SerializeField] private DeucarianAudioRole hoverRole;
        [SerializeField] private DeucarianAudioRole pressRole;
        [FormerlySerializedAs("clickRole")]
        [SerializeField] private DeucarianAudioRole activateRole;
        [SerializeField] private DeucarianAudioRole selectRole;
        [SerializeField] private DeucarianAudioRole submitRole;
        [SerializeField] private DeucarianAudioRole cancelRole;
        [SerializeField] private bool playPressFeedback;

        private Selectable target;
        private bool pointerSequenceActive;

        /// <summary>Optional theme override used before provider lookup.</summary>
        public DeucarianTheme ThemeOverride
        {
            get => themeOverride;
            set => themeOverride = value;
        }

        /// <summary>Optional themed audio player used before parent lookup.</summary>
        public DeucarianThemeAudioPlayer AudioPlayer
        {
            get => audioPlayer;
            set => audioPlayer = value;
        }

        public DeucarianAudioRole HoverRole
        {
            get => hoverRole;
            set => hoverRole = value;
        }

        public DeucarianAudioRole PressRole
        {
            get => pressRole;
            set => pressRole = value;
        }

        public DeucarianAudioRole ActivateRole
        {
            get => activateRole;
            set => activateRole = value;
        }

        [System.Obsolete("Use ActivateRole.")]
        public DeucarianAudioRole ClickRole
        {
            get => activateRole;
            set => activateRole = value;
        }

        public DeucarianAudioRole SelectRole
        {
            get => selectRole;
            set => selectRole = value;
        }

        public DeucarianAudioRole SubmitRole
        {
            get => submitRole;
            set => submitRole = value;
        }

        public DeucarianAudioRole CancelRole
        {
            get => cancelRole;
            set => cancelRole = value;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayRole(hoverRole);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerSequenceActive = true;
            if (playPressFeedback)
            {
                PlayRole(pressRole);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerSequenceActive = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PlayRole(activateRole);
            pointerSequenceActive = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!pointerSequenceActive)
            {
                PlayRole(selectRole);
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            PlayRole(submitRole);
        }

        public void OnCancel(BaseEventData eventData)
        {
            PlayRole(cancelRole);
        }

        private void PlayRole(DeucarianAudioRole role)
        {
            if (role == null || !IsSelectableInteractable())
            {
                return;
            }

            DeucarianThemeAudioPlayer player = ResolvePlayer();
            if (player == null)
            {
                return;
            }

            if (themeOverride == null)
            {
                player.PlayRole(role);
                return;
            }

            if (themeOverride.TryResolveAudio(
                    role,
                    player.CurrentExperience,
                    out DeucarianAudioResolution resolution))
            {
                player.PlayResolved(role.Id, resolution.Cue);
            }
        }

        private DeucarianThemeAudioPlayer ResolvePlayer()
        {
            if (audioPlayer == null)
            {
                audioPlayer = GetComponentInParent<DeucarianThemeAudioPlayer>(true);
            }

            return audioPlayer;
        }

        private bool IsSelectableInteractable()
        {
            CacheTarget();
            return target == null || target.IsInteractable();
        }

        private void CacheTarget()
        {
            if (target == null)
            {
                target = GetComponent<Selectable>();
            }
        }

        private void Awake()
        {
            CacheTarget();
        }

        private void Reset()
        {
            CacheTarget();
            audioPlayer = GetComponentInParent<DeucarianThemeAudioPlayer>(true);
        }
    }
}
