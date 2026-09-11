namespace Deucarian.Theming
{
    /// <summary>Typed access to the built-in audio roles. The active palette supplies their sounds.</summary>
    public static class AudioRoles
    {
        [AudioRoleKeySet]
        public static class UI
        {
            public static AudioRoleKey Hover => new Role(DeucarianBuiltinAudioRoleIds.UI.Hover);
            public static AudioRoleKey Press => new Role(DeucarianBuiltinAudioRoleIds.UI.Press);
            public static AudioRoleKey Activate => new Role(DeucarianBuiltinAudioRoleIds.UI.Activate);
            public static AudioRoleKey Select => new Role(DeucarianBuiltinAudioRoleIds.UI.Select);
            public static AudioRoleKey Submit => new Role(DeucarianBuiltinAudioRoleIds.UI.Submit);
            public static AudioRoleKey Cancel => new Role(DeucarianBuiltinAudioRoleIds.UI.Cancel);
        }

        [AudioRoleKeySet]
        public static class Input
        {
            public static AudioRoleKey Key => new Role(DeucarianBuiltinAudioRoleIds.Input.Key);
            public static AudioRoleKey SpecialKey => new Role(DeucarianBuiltinAudioRoleIds.Input.SpecialKey);
        }

        [AudioRoleKeySet]
        public static class Feedback
        {
            public static AudioRoleKey Info => new Role(DeucarianBuiltinAudioRoleIds.Feedback.Info);
            public static AudioRoleKey Success => new Role(DeucarianBuiltinAudioRoleIds.Feedback.Success);
            public static AudioRoleKey Warning => new Role(DeucarianBuiltinAudioRoleIds.Feedback.Warning);
            public static AudioRoleKey Error => new Role(DeucarianBuiltinAudioRoleIds.Feedback.Error);
            public static AudioRoleKey Invalid => new Role(DeucarianBuiltinAudioRoleIds.Feedback.Invalid);
        }

        private sealed class Role : AudioRoleKey
        {
            public Role(string id) : base(id) { }
        }
    }
}
