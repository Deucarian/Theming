using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Deucarian.Theming
{
    /// <summary>
    /// Package-level notification bus for live theme asset edits.
    /// </summary>
    public static class DeucarianThemeAssetChangeBus
    {
        /// <summary>Raised when a package-owned theme asset changes.</summary>
        public static event Action<UnityObject> AssetChanged;

        /// <summary>Notifies listeners that a package-owned theme asset changed.</summary>
        public static void NotifyChanged(UnityObject asset)
        {
            if (asset == null)
            {
                return;
            }

            AssetChanged?.Invoke(asset);
        }
    }
}
