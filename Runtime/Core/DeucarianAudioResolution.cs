namespace Deucarian.Theming
{
    public enum DeucarianAudioResolutionSource
    {
        Missing = 0,
        ExperiencePalette = 1,
        DefaultPalette = 2,
        RoleDefault = 3,
        IntentionalSilence = 4
    }

    /// <summary>Resolved cue plus provenance used by runtime and the Audio Palette Lab.</summary>
    public readonly struct DeucarianAudioResolution
    {
        public DeucarianAudioResolution(
            DeucarianAudioCue cue,
            DeucarianAudioResolutionSource source,
            DeucarianAudioPalette sourcePalette)
        {
            Cue = cue ?? DeucarianAudioCue.Empty;
            Source = Cue.IntentionalSilence
                ? DeucarianAudioResolutionSource.IntentionalSilence
                : source;
            SourcePalette = sourcePalette;
        }

        public DeucarianAudioCue Cue { get; }
        public DeucarianAudioResolutionSource Source { get; }
        public DeucarianAudioPalette SourcePalette { get; }
        public bool IsResolved => Source != DeucarianAudioResolutionSource.Missing;
        public bool IsAudible => IsResolved && Cue.HasClip && !Cue.IntentionalSilence;

        public static DeucarianAudioResolution Missing =>
            new DeucarianAudioResolution(
                DeucarianAudioCue.Empty,
                DeucarianAudioResolutionSource.Missing,
                null);
    }
}
