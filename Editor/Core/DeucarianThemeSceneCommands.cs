using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using AssetSearchResult = Deucarian.Theming.Editor.DeucarianThemingMenuActions.AssetSearchResult;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeSceneCommands
    {
        public static int ApplyActiveThemeToOpenScene(bool createProviderIfMissing = true, bool askBeforeCreate = true)
        {
            DeucarianThemeFamily family = DeucarianThemingEditorSettings.ActiveThemeFamily;
            if (family != null)
            {
                return ApplyThemeFamilyToOpenScene(
                    family,
                    DeucarianThemingEditorSettings.ActiveThemeMode,
                    createProviderIfMissing,
                    askBeforeCreate);
            }

            DeucarianTheme theme = DeucarianThemeSelectionActions.ResolveOrCreateActiveTheme();
            if (theme == null)
            {
                ThemingLog.Editor.Warning("No active Deucarian theme is selected. Open the Theme Manager and choose one.");
                return 0;
            }

            return ApplyThemeToOpenScene(theme, createProviderIfMissing, askBeforeCreate);
        }

        public static int ApplyThemeFamilyToOpenScene(
            DeucarianThemeFamily family,
            DeucarianThemeMode mode,
            bool createProviderIfMissing = true,
            bool askBeforeCreate = true)
        {
            if (family == null)
            {
                ThemingLog.Editor.Warning("Cannot apply a null Deucarian theme family to the open scene.");
                return 0;
            }

            DeucarianTheme resolvedTheme = family.ResolveTheme(mode);
            if (resolvedTheme == null)
            {
                ThemingLog.Editor.Warning(
                    $"Cannot apply theme family '{family.name}' because neither variant is assigned.",
                    family);
                return 0;
            }

            DeucarianThemeProvider[] providers = FindThemeProvidersInOpenScenes();
            if (providers.Length == 0)
            {
                if (!DeucarianThemeAssetCatalog.CanPersistSceneChanges
                    || !createProviderIfMissing
                    || !ShouldCreateThemeProvider(askBeforeCreate))
                {
                    ThemingLog.Editor.Warning("No DeucarianThemeProvider was found in the open scenes.");
                    return 0;
                }

                DeucarianThemeProvider createdProvider = CreateThemeFamilyProvider();
                providers = new[] { createdProvider };
                Selection.activeObject = createdProvider.gameObject;
            }

            int applied = 0;
            for (int i = 0; i < providers.Length; i++)
            {
                DeucarianThemeProvider provider = providers[i];
                if (provider == null || !provider.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (DeucarianThemeAssetCatalog.CanPersistSceneChanges)
                {
                    Undo.RecordObject(provider, "Apply Deucarian Theme Family");
                }

                provider.SetThemeFamily(family, mode);
                PersistProviderSceneChange(provider);
                applied++;
            }

            if (applied > 0)
            {
                ThemingLog.Editor.Info(
                    $"Applied Deucarian theme family '{family.name}' in {mode} mode to {applied} theme provider(s).",
                    family);
            }

            return applied;
        }

        public static int ApplyThemeToOpenScene(
            DeucarianTheme theme,
            bool createProviderIfMissing = true,
            bool askBeforeCreate = true)
        {
            if (theme == null)
            {
                ThemingLog.Editor.Warning("Cannot apply a null Deucarian theme to the open scene.");
                return 0;
            }

            DeucarianThemeProvider[] providers = FindThemeProvidersInOpenScenes();
            if (providers.Length == 0)
            {
                if (!DeucarianThemeAssetCatalog.CanPersistSceneChanges
                    || !createProviderIfMissing
                    || !ShouldCreateThemeProvider(askBeforeCreate))
                {
                    ThemingLog.Editor.Warning("No DeucarianThemeProvider was found in the open scenes.");
                    return 0;
                }

                DeucarianThemeProvider createdProvider = CreateThemeProvider(theme);
                providers = new[] { createdProvider };
                Selection.activeObject = createdProvider.gameObject;
            }

            int applied = 0;
            for (int i = 0; i < providers.Length; i++)
            {
                DeucarianThemeProvider provider = providers[i];
                if (provider == null || !provider.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (DeucarianThemeAssetCatalog.CanPersistSceneChanges)
                {
                    Undo.RecordObject(provider, "Apply Deucarian Theme");
                }

                provider.SetTheme(theme);
                provider.ApplyThemeToChildren(true);
                PersistProviderSceneChange(provider);
                applied++;
            }

            if (applied > 0)
            {
                ThemingLog.Editor.Info($"Applied Deucarian theme '{theme.name}' to {applied} theme provider(s).", theme);
            }

            return applied;
        }

        internal static bool ShouldCreateThemeProvider(bool askBeforeCreate)
        {
            return !askBeforeCreate || EditorUtility.DisplayDialog(
                "Create Deucarian Theme Provider?",
                "No DeucarianThemeProvider exists in the open scenes. Create one named 'Deucarian Theme Provider' and assign the active theme?",
                "Create",
                "Cancel");
        }

        internal static DeucarianThemeProvider[] FindThemeProvidersInOpenScenes()
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<DeucarianThemeProvider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
            return UnityEngine.Object.FindObjectsOfType<DeucarianThemeProvider>(true);
#pragma warning restore CS0618
#endif
        }

        public static int RefreshOpenSceneProvidersUsingAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return 0;
            }

            DeucarianThemeProvider[] providers = FindThemeProvidersInOpenScenes();
            int refreshed = 0;
            for (int i = 0; i < providers.Length; i++)
            {
                DeucarianThemeProvider provider = providers[i];
                if (provider == null
                    || !provider.UsesThemeAsset(asset)
                    || !provider.gameObject.scene.IsValid())
                {
                    continue;
                }

                provider.RefreshThemeGraph();
                refreshed++;
            }

            return refreshed;
        }

        internal static DeucarianThemeProvider CreateThemeProvider(DeucarianTheme theme)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistSceneChanges)
            {
                return null;
            }

            GameObject gameObject = new GameObject("Deucarian Theme Provider");
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(gameObject, activeScene);
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Deucarian Theme Provider");
            DeucarianThemeProvider provider = gameObject.AddComponent<DeucarianThemeProvider>();
            provider.SetTheme(theme);
            EditorUtility.SetDirty(provider);
            if (provider.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
            }

            return provider;
        }

        internal static DeucarianThemeProvider CreateThemeFamilyProvider()
        {
            if (!DeucarianThemeAssetCatalog.CanPersistSceneChanges)
            {
                return null;
            }

            GameObject gameObject = new GameObject("Deucarian Theme Provider");
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(gameObject, activeScene);
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Deucarian Theme Provider");
            DeucarianThemeProvider provider = gameObject.AddComponent<DeucarianThemeProvider>();
            EditorUtility.SetDirty(provider);
            if (provider.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
            }

            return provider;
        }

        internal static void PersistProviderSceneChange(DeucarianThemeProvider provider)
        {
            if (!DeucarianThemeAssetCatalog.CanPersistSceneChanges || provider == null || !provider.gameObject.scene.IsValid())
            {
                return;
            }

            EditorUtility.SetDirty(provider);
            EditorSceneManager.MarkSceneDirty(provider.gameObject.scene);
        }
    }
}
