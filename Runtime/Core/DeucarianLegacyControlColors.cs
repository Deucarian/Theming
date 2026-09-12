using UnityEngine;

namespace Deucarian.Theming
{
    /// <summary>Exact legacy XR appearance values for serialized compatibility. New themes use palette assets.</summary>
    public static class DeucarianLegacyControlColors
    {
        public static readonly Color Success = new Color(0.33f, 0.48f, 0.34f, 1f);
        public static readonly Color Danger = new Color(0.62f, 0.17f, 0.27f, 1f);
        public static readonly Color Warning = new Color(1f, 0.59f, 0f, 1f);
        public static readonly Color Info = new Color(0.3f, 0.7f, 1f, 1f);
        public static readonly Color Primary = new Color(0.77f, 0.63f, 0.98f, 1f);
        public static readonly Color Secondary = new Color(0.47f, 0.39f, 0.6f, 1f);
        public static readonly Color Background = new Color(0.22f, 0.23f, 0.23f, 1f);
        public static readonly Color Disabled = new Color(0.8f, 0.8f, 0.8f, 1f);
        public static readonly Color SocketGhost = Color.white;
        public static readonly Color TitleText = new Color(0.77f, 0.63f, 0.98f, 1f);
        public static readonly Color BodyText = Color.white;
        public static readonly Color SmallText = new Color(0.76f, 0.76f, 0.77f, 1f);
        public static readonly Color MutedText = new Color(1f, 1f, 1f, 0.5f);
        public static readonly Color InputText = new Color(0.75f, 0.75f, 0.75f, 1f);
        public static readonly Color PlaceholderText = new Color(0.75f, 0.75f, 0.75f, 0.47f);
        public static readonly Color Icon = Color.white;
        public static readonly Color Image = Color.white;
        public static readonly Color ImageMuted = new Color(0.75f, 0.75f, 0.75f, 1f);
        public static readonly Color ImageSubtle = new Color(1f, 1f, 1f, 0.15f);
        public static readonly Color SliderTrack = new Color(0.4f, 0.4f, 0.4f, 1f);
        public static readonly Color Outline = Color.white;
        public static readonly Color ErrorText = new Color(1f, 0.46f, 0.46f, 1f);
        public static readonly Color KeyboardAccent = new Color(0.13f, 0.59f, 0.95f, 1f);
        public static readonly Color KeyboardBackground = new Color(0.13f, 0.13f, 0.13f, 1f);
        public static readonly Color KeyboardOutline = new Color(0f, 0.6f, 1f, 1f);
        public static readonly Color KeyboardInputText = new Color(0.59f, 0.59f, 0.59f, 1f);
        public static readonly Color ControlSubtleBackground = new Color(1f, 1f, 1f, 0.05f);
        public static readonly Color ControlDarkBorder = new Color(0f, 0f, 0f, 0.48f);
        public static readonly Color SliderHandle = new Color(0.39f, 0.26f, 0.59f, 1f);
        public static readonly Color LoadingIndicator = new Color(0.47f, 0.47f, 0.47f, 1f);
        public static readonly Color DropdownInvalidState = new Color(1f, 0f, 0f, 1f);
        public static readonly Color KeyboardContentAccent = new Color(0.1f, 1f, 0f, 1f);
        public static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
    }
}
