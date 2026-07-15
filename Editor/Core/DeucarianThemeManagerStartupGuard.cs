using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Prevents Unity layout restoration from reopening Theme Manager at the start of
    /// every editor session. Explicitly opened windows remain available across domain reloads.
    /// </summary>
    [InitializeOnLoad]
    internal static class DeucarianThemeManagerStartupGuard
    {
        private const string ExplicitOpenSessionKey =
            "Deucarian.Theming.ThemeManager.ExplicitlyOpened";

        static DeucarianThemeManagerStartupGuard()
        {
            EditorApplication.delayCall -= CloseRestoredWindows;
            EditorApplication.delayCall += CloseRestoredWindows;
        }

        internal static void MarkExplicitOpen()
        {
            SessionState.SetBool(ExplicitOpenSessionKey, true);
        }

        internal static bool ShouldCloseRestoredWindows(bool explicitlyOpenedThisSession)
        {
            return !explicitlyOpenedThisSession;
        }

        private static void CloseRestoredWindows()
        {
            EditorApplication.delayCall -= CloseRestoredWindows;
            if (!ShouldCloseRestoredWindows(
                    SessionState.GetBool(ExplicitOpenSessionKey, false)))
            {
                return;
            }

            DeucarianThemeManagerWindow[] windows =
                Resources.FindObjectsOfTypeAll<DeucarianThemeManagerWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] != null)
                {
                    windows[i].Close();
                }
            }
        }
    }
}
