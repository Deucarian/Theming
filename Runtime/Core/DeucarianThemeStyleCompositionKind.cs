namespace Deucarian.Theming
{
    /// <summary>Describes how a theme style resolves its presentation values.</summary>
    public enum DeucarianThemeStyleCompositionKind
    {
        /// <summary>The style resolves every value from its backward-compatible inline fields.</summary>
        LegacyInline = 0,

        /// <summary>The style is a complete curated composition.</summary>
        CompletePreset = 1,

        /// <summary>The style is a complete project-authored custom composition.</summary>
        CompleteCustom = 2,

        /// <summary>The style mixes assigned and missing composition values and must be completed before authoring.</summary>
        Incomplete = 3
    }
}
