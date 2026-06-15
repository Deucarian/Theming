using Deucarian.Logging;

namespace Deucarian.Theming
{
    /// <summary>
    /// Package-level log categories for Deucarian Theming.
    /// </summary>
    public static class ThemingLog
    {
        public static readonly DLog General = DLog.For("Theming");
        public static readonly DLog Editor = DLog.For("Theming.Editor");
        public static readonly DLog UIToolkit = DLog.For("Theming.UIToolkit");
    }
}
