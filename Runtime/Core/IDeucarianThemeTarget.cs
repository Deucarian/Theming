namespace Deucarian.Theming
{
    /// <summary>
    /// Implemented by components that can apply a Deucarian theme.
    /// </summary>
    public interface IDeucarianThemeTarget
    {
        /// <summary>Applies the provided theme to the target.</summary>
        void ApplyTheme(DeucarianTheme theme);
    }
}
