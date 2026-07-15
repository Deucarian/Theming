using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Owns the per-user Theme Manager preview lifecycle. Preview state is kept entirely outside
    /// serialized provider configuration and is suspended while Unity produces a player build.
    /// </summary>
    [InitializeOnLoad]
    internal static class DeucarianThemePreviewCoordinator
    {
        private static bool callbacksRegistered;
        private static bool applyQueued;
        private static bool playStartupApplyQueued;
        private static bool buildSuspended;
        private static bool saveSuspended;
        private static int callbackRegistrationCount;

        static DeucarianThemePreviewCoordinator()
        {
            RegisterCallbacks();
            if (BuildPipeline.isBuildingPlayer)
            {
                buildSuspended = true;
                ScheduleBuildCompletionRestore();
            }
            else
            {
                QueuePreviewApplication();
            }
        }

        internal static bool IsBuildSuspended => buildSuspended;

        internal static bool IsSaveSuspended => saveSuspended;

        internal static bool CallbacksRegistered => callbacksRegistered;

        internal static int CallbackRegistrationCount => callbackRegistrationCount;

        internal static bool IsPlayStartupApplyQueued => playStartupApplyQueued;

        internal static void RegisterCallbacks()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            if (!callbacksRegistered)
            {
                callbacksRegistered = true;
                callbackRegistrationCount++;
            }
        }

        internal static int ApplySelectedPreview()
        {
            CancelQueuedPreviewApplication();
            CancelPlayStartupPreviewApplication();
            if (buildSuspended || saveSuspended || BuildPipeline.isBuildingPlayer)
            {
                return 0;
            }

            DeucarianThemeManagerSelection selection = DeucarianThemeManagerSelection.FromEditorPrefs();
            return selection.Family == null
                ? DeucarianThemeManagerWorkflow.ClearPreview()
                : DeucarianThemeManagerWorkflow.Preview(selection);
        }

        internal static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (buildSuspended || saveSuspended)
            {
                return;
            }

            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    // Defer until startup Awake/Start configuration has run. The queue is
                    // idempotent, and public runtime setters clear the preview afterward.
                    QueuePlayStartupPreviewApplication();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    CancelPlayStartupPreviewApplication();
                    QueuePreviewApplication();
                    break;
            }
        }

        internal static void SuspendForBuild()
        {
            buildSuspended = true;
            CancelQueuedPreviewApplication();
            CancelPlayStartupPreviewApplication();
            DeucarianThemeManagerWorkflow.ClearPreview();
            ScheduleBuildCompletionRestore();
        }

        internal static void ResumeAfterBuild()
        {
            TryResumeAfterBuild(BuildPipeline.isBuildingPlayer);
        }

        internal static void ResumeAfterBuildForTests(bool isBuildingPlayer)
        {
            TryResumeAfterBuild(isBuildingPlayer);
        }

        internal static void SuspendForSave()
        {
            if (buildSuspended)
            {
                return;
            }

            if (!saveSuspended)
            {
                saveSuspended = true;
                CancelQueuedPreviewApplication();
                CancelPlayStartupPreviewApplication();
                DeucarianThemeManagerWorkflow.ClearPreview();
            }

            ScheduleSaveCompletionRestore();
        }

        internal static void ResumeAfterSave()
        {
            EditorApplication.delayCall -= RestoreAfterSave;
            if (!saveSuspended)
            {
                return;
            }

            saveSuspended = false;
            if (!buildSuspended && !BuildPipeline.isBuildingPlayer)
            {
                QueuePreviewApplication();
            }
        }

        private static void TryResumeAfterBuild(bool isBuildingPlayer)
        {
            if (!buildSuspended)
            {
                EditorApplication.update -= RestoreAfterBuildWhenIdle;
                return;
            }

            if (isBuildingPlayer)
            {
                ScheduleBuildCompletionRestore();
                return;
            }

            EditorApplication.update -= RestoreAfterBuildWhenIdle;
            buildSuspended = false;
            if (!saveSuspended)
            {
                QueuePreviewApplication();
            }
        }

        internal static void QueuePreviewApplication()
        {
            if (applyQueued || buildSuspended || saveSuspended || BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            applyQueued = true;
            EditorApplication.delayCall -= ApplyQueuedPreview;
            EditorApplication.delayCall += ApplyQueuedPreview;
        }

        private static void QueuePlayStartupPreviewApplication()
        {
            if (playStartupApplyQueued
                || buildSuspended
                || saveSuspended
                || BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            // The first editor update after EnteredPlayMode occurs after the player's startup
            // lifecycle. Keeping this queue separate from delayCall prevents an Awake/Start
            // settings assignment from overwriting the selected editor preview.
            CancelQueuedPreviewApplication();
            playStartupApplyQueued = true;
            EditorApplication.update -= ApplyPlayStartupPreview;
            EditorApplication.update += ApplyPlayStartupPreview;
        }

        private static void ApplyQueuedPreview()
        {
            ApplySelectedPreview();
        }

        internal static void ApplyPlayStartupPreview()
        {
            if (!playStartupApplyQueued)
            {
                return;
            }

            playStartupApplyQueued = false;
            EditorApplication.update -= ApplyPlayStartupPreview;
            ApplySelectedPreview();
        }

        private static void CancelQueuedPreviewApplication()
        {
            if (!applyQueued)
            {
                return;
            }

            applyQueued = false;
            EditorApplication.delayCall -= ApplyQueuedPreview;
        }

        private static void CancelPlayStartupPreviewApplication()
        {
            if (!playStartupApplyQueued)
            {
                return;
            }

            playStartupApplyQueued = false;
            EditorApplication.update -= ApplyPlayStartupPreview;
        }

        private static void ScheduleBuildCompletionRestore()
        {
            EditorApplication.update -= RestoreAfterBuildWhenIdle;
            EditorApplication.update += RestoreAfterBuildWhenIdle;
        }

        private static void ScheduleSaveCompletionRestore()
        {
            EditorApplication.delayCall -= RestoreAfterSave;
            EditorApplication.delayCall += RestoreAfterSave;
        }

        private static void RestoreAfterSave()
        {
            ResumeAfterSave();
        }

        private static void RestoreAfterBuildWhenIdle()
        {
            if (!BuildPipeline.isBuildingPlayer)
            {
                TryResumeAfterBuild(false);
            }
        }
    }

    /// <summary>
    /// Restores configured target values before Unity serializes a scene or prefab, then lets the
    /// coordinator reapply the per-user preview after the save operation has completed.
    /// </summary>
    internal sealed class DeucarianThemePreviewSaveGuard : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            PrepareForSave(paths);
            return paths;
        }

        internal static void PrepareForSave(string[] paths)
        {
            if (!ContainsSceneOrPrefab(paths))
            {
                return;
            }

            DeucarianThemePreviewCoordinator.SuspendForSave();
        }

        private static bool ContainsSceneOrPrefab(string[] paths)
        {
            if (paths == null)
            {
                return false;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (!string.IsNullOrEmpty(path)
                    && (path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class DeucarianThemePreviewBuildGuard :
        IPreprocessBuildWithReport,
        IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            DeucarianThemePreviewCoordinator.SuspendForBuild();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            DeucarianThemePreviewCoordinator.ResumeAfterBuild();
        }
    }
}
