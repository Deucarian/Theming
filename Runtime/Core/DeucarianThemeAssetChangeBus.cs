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
        private static int batchDepth;
        private static UnityObject batchConsolidatedAsset;
        private static UnityObject firstBatchedAsset;
        private static bool batchChanged;

        /// <summary>Raised when a package-owned theme asset changes.</summary>
        public static event Action<UnityObject> AssetChanged;

        /// <summary>
        /// Suppresses child notifications until the returned scope is disposed, then emits one consolidated change.
        /// Nested batches participate in the outermost batch.
        /// </summary>
        internal static IDisposable BeginBatch(UnityObject consolidatedAsset)
        {
            if (batchDepth == 0)
            {
                batchConsolidatedAsset = consolidatedAsset;
                firstBatchedAsset = null;
                batchChanged = false;
            }

            batchDepth++;
            return new BatchScope();
        }

        /// <summary>Notifies listeners that a package-owned theme asset changed.</summary>
        public static void NotifyChanged(UnityObject asset)
        {
            if (asset == null)
            {
                return;
            }

            if (batchDepth > 0)
            {
                batchChanged = true;
                if (firstBatchedAsset == null)
                {
                    firstBatchedAsset = asset;
                }

                return;
            }

            AssetChanged?.Invoke(asset);
        }

        private static void EndBatch()
        {
            if (batchDepth <= 0)
            {
                return;
            }

            batchDepth--;
            if (batchDepth > 0)
            {
                return;
            }

            UnityObject changedAsset = batchConsolidatedAsset != null
                ? batchConsolidatedAsset
                : firstBatchedAsset;
            bool shouldNotify = batchChanged && changedAsset != null;
            batchConsolidatedAsset = null;
            firstBatchedAsset = null;
            batchChanged = false;

            if (shouldNotify)
            {
                AssetChanged?.Invoke(changedAsset);
            }
        }

        private sealed class BatchScope : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                EndBatch();
            }
        }
    }
}
