namespace Deucarian.Theming
{
    /// <summary>
    /// Semantic UI density intent. Presentation packages translate this intent into control-specific metrics.
    /// </summary>
    public enum DeucarianThemeDensity
    {
        /// <summary>Legacy style with no explicit density selection.</summary>
        Unspecified = 0,

        /// <summary>Largest built-in controls and icons.</summary>
        Comfortable = 1,

        /// <summary>Balanced built-in control dimensions.</summary>
        Standard = 2,

        /// <summary>Smallest built-in controls and icons.</summary>
        Compact = 3
    }
}
