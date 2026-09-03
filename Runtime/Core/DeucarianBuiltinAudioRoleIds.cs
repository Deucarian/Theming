namespace Deucarian.Theming
{
    /// <summary>
    /// Convenience string IDs for built-in Deucarian audio roles.
    /// These constants are not a source-of-truth enum; designers can create more role assets without changing code.
    /// </summary>
    public static class DeucarianBuiltinAudioRoleIds
    {
        public const string Hover = UI.Hover;
        public const string Press = UI.Press;
        public const string Activate = UI.Activate;
        [System.Obsolete("Use Activate. Click is a compatibility alias for the committed action.")]
        public const string Click = UI.Activate;
        public const string Select = UI.Select;
        public const string Submit = UI.Submit;
        public const string Cancel = UI.Cancel;
        public const string Key = Input.Key;
        public const string SpecialKey = Input.SpecialKey;
        public const string Info = Feedback.Info;
        public const string Success = Feedback.Success;
        public const string Warning = Feedback.Warning;
        public const string Error = Feedback.Error;
        public const string Invalid = Feedback.Invalid;

        public static class UI
        {
            public const string Hover = "deucarian.ui.audio.hover";
            public const string Press = "deucarian.ui.audio.press";
            public const string Activate = "deucarian.ui.audio.activate";
            [System.Obsolete("Use Activate.")]
            public const string Click = Activate;
            public const string Select = "deucarian.ui.audio.select";
            public const string Submit = "deucarian.ui.audio.submit";
            public const string Cancel = "deucarian.ui.audio.cancel";
        }

        public static class Input
        {
            public const string Key = "deucarian.input.audio.key";
            public const string SpecialKey = "deucarian.input.audio.key-special";
        }

        public static class Feedback
        {
            public const string Info = "deucarian.feedback.audio.info";
            public const string Success = "deucarian.feedback.audio.success";
            public const string Warning = "deucarian.feedback.audio.warning";
            public const string Error = "deucarian.feedback.audio.error";
            public const string Invalid = "deucarian.feedback.audio.invalid";
        }
    }
}
