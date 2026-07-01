namespace Deucarian.Theming
{
    /// <summary>
    /// Implemented by components that can apply a Deucarian visual style.
    /// </summary>
    public interface IDeucarianThemeStyleTarget
    {
        /// <summary>Applies the provided visual style to the target.</summary>
        void ApplyStyle(DeucarianThemeStyle style);
    }
}
