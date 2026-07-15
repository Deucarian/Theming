using System;
using System.IO;
using System.Text.RegularExpressions;
using Deucarian.Editor;
using Deucarian.Theming.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemeManagerWorkflowTests
    {
        private const string TestRootBase = "Assets/DeucarianThemeManagerWorkflowTests";

        private string testRoot;
        private string previousThemeGuid;
        private string previousFamilyGuid;
        private DeucarianThemeMode previousMode;
        private string previousPaletteGuid;
        private string previousLibraryGuid;
        private string previousStyleGuid;

        [SetUp]
        public void SetUp()
        {
            previousThemeGuid = DeucarianThemingEditorSettings.ActiveThemeGuid;
            previousFamilyGuid = DeucarianThemingEditorSettings.ActiveThemeFamilyGuid;
            previousMode = DeucarianThemingEditorSettings.ActiveThemeMode;
            previousPaletteGuid = DeucarianThemingEditorSettings.ActivePaletteGuid;
            previousLibraryGuid = DeucarianThemingEditorSettings.ActiveRoleLibraryGuid;
            previousStyleGuid = DeucarianThemingEditorSettings.ActiveStyleGuid;
            testRoot = TestRootBase + "/" + Guid.NewGuid().ToString("N");
            DeucarianThemingEditorSettings.ClearActiveAssets();
            DeucarianThemingEditorSettings.ActiveThemeMode = DeucarianThemeMode.Dark;
            DeucarianThemePreviewCoordinator.ApplySelectedPreview();
        }

        [TearDown]
        public void TearDown()
        {
            DeucarianThemePreviewCoordinator.ResumeAfterSave();
            DeucarianThemePreviewCoordinator.ResumeAfterBuild();
            DeucarianThemeManagerWorkflow.ClearPreview();
            DeucarianThemeProvider[] providers = Resources.FindObjectsOfTypeAll<DeucarianThemeProvider>();
            for (int i = 0; i < providers.Length; i++)
            {
                if (providers[i] != null && providers[i].name.StartsWith("Workflow Test", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(providers[i].gameObject);
                }
            }

            AssetDatabase.DeleteAsset(testRoot);
            DeucarianThemingEditorSettings.ActiveThemeGuid = previousThemeGuid;
            DeucarianThemingEditorSettings.ActiveThemeFamilyGuid = previousFamilyGuid;
            DeucarianThemingEditorSettings.ActiveThemeMode = previousMode;
            DeucarianThemingEditorSettings.ActivePaletteGuid = previousPaletteGuid;
            DeucarianThemingEditorSettings.ActiveRoleLibraryGuid = previousLibraryGuid;
            DeucarianThemingEditorSettings.ActiveStyleGuid = previousStyleGuid;
            DeucarianThemePreviewCoordinator.ApplySelectedPreview();
        }

        [Test]
        public void DraftSelectionChangesOnlyEditorPrefs()
        {
            DeucarianDefaultThemeAssets canonical = CreateFamily("Canonical");
            DeucarianDefaultThemeAssets draft = CreateFamily("Draft");
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            settings.Configure(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            GameObject providerObject = new GameObject("Workflow Test Draft Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            DeucarianThemeStyle originalLightStyle = draft.LightTheme.VisualStyle;
            DeucarianThemeStyle originalDarkStyle = draft.DarkTheme.VisualStyle;

            DeucarianThemingEditorSettings.SetDraftSelection(
                draft.ThemeFamily,
                DeucarianThemeMode.Light,
                draft.DefaultStyle);

            Assert.AreSame(canonical.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);
            Assert.AreSame(canonical.ThemeFamily, provider.CurrentThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, provider.ThemeMode);
            Assert.AreSame(originalLightStyle, draft.LightTheme.VisualStyle);
            Assert.AreSame(originalDarkStyle, draft.DarkTheme.VisualStyle);
            Assert.AreSame(draft.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreSame(draft.LightTheme, DeucarianThemingEditorSettings.ActiveTheme);
        }

        [Test]
        public void PreviewAppliesDraftWithoutChangingSettingsAssetsOrProviderConfiguration()
        {
            DeucarianDefaultThemeAssets canonical = CreateFamily("PreviewCanonical");
            DeucarianDefaultThemeAssets draft = CreateFamily("PreviewDraft");
            DeucarianThemeRuntimeSettings settings = CreateSettings("PreviewSettings.asset");
            settings.Configure(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            AssetDatabase.SaveAssetIfDirty(settings);
            GameObject providerObject = new GameObject("Workflow Test Preview Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            string serializedBeforePreview = JsonUtility.ToJson(provider);
            bool providerDirtyBeforePreview = EditorUtility.IsDirty(provider);
            bool sceneDirtyBeforePreview = provider.gameObject.scene.isDirty;
            int repaintVersionBeforePreview = DeucarianThemeManagerWorkflow.PreviewRepaintVersion;

            int previewed = DeucarianThemeManagerWorkflow.Preview(
                new DeucarianThemeManagerSelection(
                    draft.ThemeFamily,
                    DeucarianThemeMode.Light,
                    draft.DefaultStyle),
                new[] { provider });

            Assert.AreEqual(1, previewed);
            Assert.IsTrue(provider.HasEditorPreview);
            Assert.AreSame(draft.ThemeFamily, provider.CurrentThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, provider.ThemeMode);
            Assert.AreSame(draft.LightTheme, provider.CurrentTheme);
            Assert.AreSame(canonical.ThemeFamily, provider.ConfiguredThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, provider.ConfiguredThemeMode);
            Assert.AreEqual(serializedBeforePreview, JsonUtility.ToJson(provider));
            Assert.AreSame(canonical.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);
            Assert.AreEqual(providerDirtyBeforePreview, EditorUtility.IsDirty(provider));
            Assert.AreEqual(sceneDirtyBeforePreview, provider.gameObject.scene.isDirty);
            Assert.Greater(
                DeucarianThemeManagerWorkflow.PreviewRepaintVersion,
                repaintVersionBeforePreview);

            int repaintVersionBeforeClear = DeucarianThemeManagerWorkflow.PreviewRepaintVersion;
            Assert.AreEqual(1, DeucarianThemeManagerWorkflow.ClearPreview(new[] { provider }));
            Assert.AreSame(canonical.ThemeFamily, provider.CurrentThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, provider.ThemeMode);
            Assert.AreEqual(serializedBeforePreview, JsonUtility.ToJson(provider));
            Assert.AreEqual(providerDirtyBeforePreview, EditorUtility.IsDirty(provider));
            Assert.AreEqual(sceneDirtyBeforePreview, provider.gameObject.scene.isDirty);
            Assert.Greater(
                DeucarianThemeManagerWorkflow.PreviewRepaintVersion,
                repaintVersionBeforeClear);
        }

        [Test]
        public void PreviewDoesNotChangeTheActiveProviderIdentity()
        {
            DeucarianDefaultThemeAssets runtime = CreateFamily("ActiveRuntime");
            DeucarianDefaultThemeAssets preview = CreateFamily("ActivePreview");
            GameObject previewedObject = new GameObject("Workflow Test Previewed Provider");
            DeucarianThemeProvider previewedProvider =
                previewedObject.AddComponent<DeucarianThemeProvider>();
            previewedProvider.SetThemeFamily(runtime.ThemeFamily, DeucarianThemeMode.Dark);
            GameObject activeObject = new GameObject("Workflow Test Active Provider");
            DeucarianThemeProvider activeProvider =
                activeObject.AddComponent<DeucarianThemeProvider>();
            activeProvider.SetThemeFamily(runtime.ThemeFamily, DeucarianThemeMode.Dark);

            Assert.AreSame(activeProvider, DeucarianThemeProvider.Active);

            Assert.AreEqual(
                1,
                DeucarianThemeManagerWorkflow.Preview(
                    new DeucarianThemeManagerSelection(
                        preview.ThemeFamily,
                        DeucarianThemeMode.Light,
                        preview.DefaultStyle),
                    new[] { previewedProvider }));

            Assert.IsTrue(previewedProvider.HasEditorPreview);
            Assert.AreSame(activeProvider, DeucarianThemeProvider.Active);
        }

        [Test]
        public void EnteredPlayModeReappliesPreviewAfterStartupAndLaterRuntimeChangesWin()
        {
            DeucarianDefaultThemeAssets runtime = CreateFamily("PlayRuntime");
            DeucarianDefaultThemeAssets preview = CreateFamily("PlayPreview");
            GameObject providerObject = new GameObject("Workflow Test Play Preview Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(runtime.ThemeFamily, DeucarianThemeMode.Dark);
            DeucarianThemingEditorSettings.SetDraftSelection(
                preview.ThemeFamily,
                DeucarianThemeMode.Light,
                preview.DefaultStyle);

            DeucarianThemePreviewCoordinator.HandlePlayModeStateChanged(
                PlayModeStateChange.EnteredPlayMode);

            Assert.IsTrue(DeucarianThemePreviewCoordinator.IsPlayStartupApplyQueued);
            Assert.IsFalse(provider.HasEditorPreview);
            Assert.AreSame(runtime.ThemeFamily, provider.CurrentThemeFamily);

            DeucarianThemePreviewCoordinator.ApplyPlayStartupPreview();

            Assert.IsFalse(DeucarianThemePreviewCoordinator.IsPlayStartupApplyQueued);
            Assert.IsTrue(provider.HasEditorPreview);
            Assert.AreSame(preview.ThemeFamily, provider.CurrentThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, provider.ThemeMode);

            provider.SetThemeFamily(runtime.ThemeFamily, DeucarianThemeMode.Light);

            Assert.IsFalse(provider.HasEditorPreview);
            Assert.AreSame(runtime.ThemeFamily, provider.CurrentThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, provider.ThemeMode);
        }

        [Test]
        public void BuildSuspensionClearsAndRestoresSelectedPreview()
        {
            DeucarianDefaultThemeAssets runtime = CreateFamily("BuildRuntime");
            DeucarianDefaultThemeAssets preview = CreateFamily("BuildPreview");
            GameObject providerObject = new GameObject("Workflow Test Build Preview Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(runtime.ThemeFamily, DeucarianThemeMode.Dark);
            DeucarianThemingEditorSettings.SetDraftSelection(
                preview.ThemeFamily,
                DeucarianThemeMode.Light,
                preview.DefaultStyle);
            Assert.GreaterOrEqual(DeucarianThemePreviewCoordinator.ApplySelectedPreview(), 1);
            Assert.IsTrue(provider.HasEditorPreview);

            DeucarianThemePreviewCoordinator.SuspendForBuild();

            Assert.IsTrue(DeucarianThemePreviewCoordinator.IsBuildSuspended);
            Assert.IsFalse(provider.HasEditorPreview);
            Assert.AreSame(runtime.ThemeFamily, provider.CurrentThemeFamily);

            DeucarianThemePreviewCoordinator.ResumeAfterBuild();
            Assert.IsFalse(DeucarianThemePreviewCoordinator.IsBuildSuspended);
            Assert.GreaterOrEqual(DeucarianThemePreviewCoordinator.ApplySelectedPreview(), 1);
            Assert.IsTrue(provider.HasEditorPreview);
            Assert.AreSame(preview.ThemeFamily, provider.CurrentThemeFamily);
        }

        [Test]
        public void BuildPostprocessWaitsUntilTheBuildPipelineIsIdleBeforeRestoringPreview()
        {
            DeucarianDefaultThemeAssets runtime = CreateFamily("BusyBuildRuntime");
            DeucarianDefaultThemeAssets preview = CreateFamily("BusyBuildPreview");
            GameObject providerObject = new GameObject("Workflow Test Busy Build Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(runtime.ThemeFamily, DeucarianThemeMode.Dark);
            DeucarianThemingEditorSettings.SetDraftSelection(
                preview.ThemeFamily,
                DeucarianThemeMode.Light,
                preview.DefaultStyle);
            Assert.GreaterOrEqual(DeucarianThemePreviewCoordinator.ApplySelectedPreview(), 1);

            DeucarianThemePreviewCoordinator.SuspendForBuild();
            DeucarianThemePreviewCoordinator.ResumeAfterBuildForTests(true);

            Assert.IsTrue(DeucarianThemePreviewCoordinator.IsBuildSuspended);
            Assert.IsFalse(provider.HasEditorPreview);
            Assert.AreEqual(0, DeucarianThemePreviewCoordinator.ApplySelectedPreview());

            DeucarianThemePreviewCoordinator.ResumeAfterBuildForTests(false);

            Assert.IsFalse(DeucarianThemePreviewCoordinator.IsBuildSuspended);
            Assert.GreaterOrEqual(DeucarianThemePreviewCoordinator.ApplySelectedPreview(), 1);
            Assert.IsTrue(provider.HasEditorPreview);
            Assert.AreSame(preview.ThemeFamily, provider.CurrentThemeFamily);
        }

        [TestCase("PreviewScene.unity")]
        [TestCase("PreviewPrefab.prefab")]
        public void SceneAndPrefabSavePreparationRestoresConfiguredSerializedTargetState(
            string assetName)
        {
            DeucarianDefaultThemeAssets runtime = CreateFamily("SaveRuntime");
            DeucarianDefaultThemeAssets preview = CreateFamily("SavePreview");
            GameObject providerObject = new GameObject("Workflow Test Save Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            GameObject targetObject = new GameObject("Workflow Test Serialized Theme Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            SerializedPreviewThemeTarget target =
                targetObject.AddComponent<SerializedPreviewThemeTarget>();
            provider.SetThemeFamily(runtime.ThemeFamily, DeucarianThemeMode.Dark);
            DeucarianThemingEditorSettings.SetDraftSelection(
                preview.ThemeFamily,
                DeucarianThemeMode.Light,
                preview.DefaultStyle);
            Assert.GreaterOrEqual(DeucarianThemePreviewCoordinator.ApplySelectedPreview(), 1);
            Assert.AreSame(preview.LightTheme, target.AppliedTheme);

            DeucarianThemePreviewSaveGuard.PrepareForSave(
                new[] { testRoot + "/" + assetName });

            Assert.IsTrue(DeucarianThemePreviewCoordinator.IsSaveSuspended);
            Assert.IsFalse(provider.HasEditorPreview);
            Assert.AreSame(runtime.DarkTheme, target.AppliedTheme);
            SerializedObject serializedTarget = new SerializedObject(target);
            serializedTarget.Update();
            SerializedProperty appliedThemeProperty =
                serializedTarget.FindProperty("appliedTheme");
            Assert.IsNotNull(appliedThemeProperty);
            Assert.AreSame(
                runtime.DarkTheme,
                appliedThemeProperty.objectReferenceValue);

            DeucarianThemePreviewCoordinator.ResumeAfterSave();
            Assert.IsFalse(DeucarianThemePreviewCoordinator.IsSaveSuspended);
            Assert.GreaterOrEqual(DeucarianThemePreviewCoordinator.ApplySelectedPreview(), 1);
            Assert.IsTrue(provider.HasEditorPreview);
            Assert.AreSame(preview.LightTheme, target.AppliedTheme);
        }

        [Test]
        public void PreviewCoordinatorCallbackRegistrationIsIdempotent()
        {
            int registrationCount = DeucarianThemePreviewCoordinator.CallbackRegistrationCount;

            DeucarianThemePreviewCoordinator.RegisterCallbacks();
            DeucarianThemePreviewCoordinator.RegisterCallbacks();

            Assert.IsTrue(DeucarianThemePreviewCoordinator.CallbacksRegistered);
            Assert.AreEqual(registrationCount, DeucarianThemePreviewCoordinator.CallbackRegistrationCount);
        }

        [Test]
        public void EvaluateMarksFamilyModeAndStyleIndependently()
        {
            DeucarianDefaultThemeAssets canonical = CreateFamily("Canonical");
            DeucarianDefaultThemeAssets other = CreateFamily("Other");
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            settings.Configure(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            other.ThemeFamily.SetSharedVisualStyle(canonical.DefaultStyle);

            DeucarianThemeManagerActivationStatus familyStatus =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    new DeucarianThemeManagerSelection(
                        other.ThemeFamily,
                        DeucarianThemeMode.Dark,
                        canonical.DefaultStyle),
                    Array.Empty<DeucarianThemeProvider>());
            Assert.IsTrue(familyStatus.FamilyDirty);
            Assert.IsFalse(familyStatus.ModeDirty);
            Assert.IsFalse(familyStatus.StyleDirty);

            DeucarianThemeManagerActivationStatus modeStatus =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    new DeucarianThemeManagerSelection(
                        canonical.ThemeFamily,
                        DeucarianThemeMode.Light,
                        canonical.DefaultStyle),
                    Array.Empty<DeucarianThemeProvider>());
            Assert.IsFalse(modeStatus.FamilyDirty);
            Assert.IsTrue(modeStatus.ModeDirty);
            Assert.IsFalse(modeStatus.StyleDirty);

            DeucarianThemeManagerActivationStatus styleStatus =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    new DeucarianThemeManagerSelection(
                        canonical.ThemeFamily,
                        DeucarianThemeMode.Dark,
                        other.DefaultStyle),
                    Array.Empty<DeucarianThemeProvider>());
            Assert.IsFalse(styleStatus.FamilyDirty);
            Assert.IsFalse(styleStatus.ModeDirty);
            Assert.IsTrue(styleStatus.StyleDirty);
        }

        [Test]
        public void EvaluateSeparatesProviderDriftFromFieldDirtyState()
        {
            DeucarianDefaultThemeAssets canonical = CreateFamily("Canonical");
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            settings.Configure(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            GameObject providerObject = new GameObject("Workflow Test Drift Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(canonical.ThemeFamily, DeucarianThemeMode.Light);

            DeucarianThemeManagerActivationStatus status =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    new DeucarianThemeManagerSelection(
                        canonical.ThemeFamily,
                        DeucarianThemeMode.Dark,
                        canonical.DefaultStyle),
                    new[] { provider });

            Assert.IsFalse(status.FamilyDirty);
            Assert.IsFalse(status.ModeDirty);
            Assert.IsFalse(status.StyleDirty);
            Assert.IsFalse(status.ProvidersSynchronized);
            Assert.IsFalse(status.IsActive);
            Assert.IsTrue(status.CanActivate);
            StringAssert.Contains("synchronized", status.Message);
        }

        [Test]
        public void ProviderOnlySynchronizationDoesNotDirtyOrPublishProjectAssets()
        {
            DeucarianDefaultThemeAssets canonical = CreateFamily("ProviderOnly");
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            settings.Configure(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            AssetDatabase.SaveAssetIfDirty(canonical.LightTheme);
            AssetDatabase.SaveAssetIfDirty(canonical.DarkTheme);
            GameObject providerObject = new GameObject("Workflow Test Provider Only");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(canonical.ThemeFamily, DeucarianThemeMode.Light);
            int assetNotifications = 0;
            Action<UnityEngine.Object> listener = _ => assetNotifications++;
            DeucarianThemeAssetChangeBus.AssetChanged += listener;

            try
            {
                Assert.IsFalse(EditorUtility.IsDirty(settings));
                Assert.IsFalse(EditorUtility.IsDirty(canonical.LightTheme));
                Assert.IsFalse(EditorUtility.IsDirty(canonical.DarkTheme));

                DeucarianThemeManagerActivationResult result =
                    DeucarianThemeManagerWorkflow.Activate(
                        settings,
                        new DeucarianThemeManagerSelection(
                            canonical.ThemeFamily,
                            DeucarianThemeMode.Dark,
                            canonical.DefaultStyle),
                        new[] { provider });

                Assert.IsTrue(result.Succeeded, result.Message);
                Assert.AreEqual(1, result.ProviderCount);
                Assert.AreEqual(DeucarianThemeMode.Dark, provider.ThemeMode);
                Assert.AreEqual(0, assetNotifications);
                Assert.IsFalse(EditorUtility.IsDirty(settings));
                Assert.IsFalse(EditorUtility.IsDirty(canonical.LightTheme));
                Assert.IsFalse(EditorUtility.IsDirty(canonical.DarkTheme));
            }
            finally
            {
                DeucarianThemeAssetChangeBus.AssetChanged -= listener;
            }
        }

        [Test]
        public void EvaluateBlocksMissingSettingsAndIncompleteSelectedFamily()
        {
            DeucarianDefaultThemeAssets complete = CreateFamily("Complete");
            DeucarianThemeManagerSelection completeSelection = new DeucarianThemeManagerSelection(
                complete.ThemeFamily,
                DeucarianThemeMode.Dark,
                complete.DefaultStyle);

            DeucarianThemeManagerActivationStatus missingSettings =
                DeucarianThemeManagerWorkflow.Evaluate(
                    null,
                    completeSelection,
                    Array.Empty<DeucarianThemeProvider>());
            Assert.IsFalse(missingSettings.HasRuntimeSettings);
            Assert.IsFalse(missingSettings.CanActivate);

            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            DeucarianThemeManagerActivationStatus unconfiguredSettings =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    completeSelection,
                    Array.Empty<DeucarianThemeProvider>());
            Assert.IsTrue(unconfiguredSettings.HasRuntimeSettings);
            Assert.IsTrue(unconfiguredSettings.RuntimeSettingsReady);
            Assert.IsTrue(unconfiguredSettings.CanActivate);
            Assert.IsTrue(DeucarianThemeManagerWorkflow.Activate(
                settings,
                completeSelection,
                Array.Empty<DeucarianThemeProvider>()).Succeeded);
            Assert.AreSame(complete.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);

            DeucarianThemeFamily incomplete = CreateAsset<DeucarianThemeFamily>("IncompleteFamily.asset");
            incomplete.Configure(
                "deucarian.test.incomplete",
                "Incomplete",
                complete.LightTheme,
                null);
            settings.Configure(complete.ThemeFamily, DeucarianThemeMode.Dark);
            DeucarianThemeManagerActivationStatus incompleteStatus =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    new DeucarianThemeManagerSelection(
                        incomplete,
                        DeucarianThemeMode.Light,
                        complete.DefaultStyle),
                    Array.Empty<DeucarianThemeProvider>());
            Assert.IsFalse(incompleteStatus.SelectionValid);
            Assert.IsFalse(incompleteStatus.CanActivate);
            StringAssert.Contains("Light and Dark", incompleteStatus.Message);
        }

        [Test]
        public void ActivateUpdatesBothModesSettingsAndLoadedProviders()
        {
            DeucarianDefaultThemeAssets canonical = CreateFamily("Canonical");
            DeucarianDefaultThemeAssets selected = CreateFamily("Selected");
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            settings.Configure(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            GameObject providerObject = new GameObject("Workflow Test Activate Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            provider.SetStyle(canonical.DefaultStyle);

            DeucarianThemeManagerSelection selection = new DeucarianThemeManagerSelection(
                selected.ThemeFamily,
                DeucarianThemeMode.Light,
                canonical.DefaultStyle);
            DeucarianThemeManagerActivationResult result =
                DeucarianThemeManagerWorkflow.Activate(settings, selection, new[] { provider });

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(1, result.ProviderCount);
            Assert.AreSame(selected.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, settings.DefaultThemeMode);
            Assert.AreSame(canonical.DefaultStyle, selected.LightTheme.VisualStyle);
            Assert.AreSame(canonical.DefaultStyle, selected.DarkTheme.VisualStyle);
            Assert.AreSame(selected.ThemeFamily, provider.CurrentThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, provider.ThemeMode);
            Assert.IsNull(provider.StyleOverride);
            Assert.AreSame(canonical.DefaultStyle, provider.CurrentStyle);
            Assert.AreSame(selected.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreSame(selected.LightTheme, DeucarianThemingEditorSettings.ActiveTheme);
        }

        [Test]
        public void ActivationFailureReturnsOriginalErrorWhenRollbackRefreshAlsoThrows()
        {
            DeucarianDefaultThemeAssets canonical = CreateFamily("RollbackCanonical");
            DeucarianDefaultThemeAssets selected = CreateFamily("RollbackSelected");
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            settings.Configure(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            GameObject providerObject = new GameObject("Workflow Test Rollback Provider");
            DeucarianThemeProvider provider = providerObject.AddComponent<DeucarianThemeProvider>();
            provider.SetThemeFamily(canonical.ThemeFamily, DeucarianThemeMode.Dark);
            GameObject targetObject = new GameObject("Workflow Test Throwing Target");
            targetObject.transform.SetParent(providerObject.transform, false);
            targetObject.AddComponent<ThrowingThemeTarget>();
            DeucarianThemeStyle originalLightStyle = selected.LightTheme.VisualStyle;
            DeucarianThemeStyle originalDarkStyle = selected.DarkTheme.VisualStyle;

            LogAssert.Expect(
                LogType.Exception,
                new Regex("Workflow test target failure", RegexOptions.IgnoreCase));
            LogAssert.Expect(
                LogType.Error,
                new Regex("provider failed while restoring", RegexOptions.IgnoreCase));
            LogAssert.Expect(
                LogType.Exception,
                new Regex("Workflow test target failure", RegexOptions.IgnoreCase));
            LogAssert.Expect(
                LogType.Error,
                new Regex("Theme activation was rolled back", RegexOptions.IgnoreCase));
            DeucarianThemeManagerActivationResult result =
                DeucarianThemeManagerWorkflow.Activate(
                    settings,
                    new DeucarianThemeManagerSelection(
                        selected.ThemeFamily,
                        DeucarianThemeMode.Light,
                        canonical.DefaultStyle),
                    new[] { provider });

            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains("rolled back", result.Message);
            Assert.AreSame(canonical.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);
            Assert.AreSame(originalLightStyle, selected.LightTheme.VisualStyle);
            Assert.AreSame(originalDarkStyle, selected.DarkTheme.VisualStyle);
            Assert.AreSame(canonical.ThemeFamily, provider.CurrentThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, provider.ThemeMode);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ActivateWithoutProviderStillUpdatesProjectDefault()
        {
            DeucarianDefaultThemeAssets selected = CreateFamily("Selected");
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            settings.Configure(selected.ThemeFamily, DeucarianThemeMode.Light);
            DeucarianThemeManagerSelection selection = new DeucarianThemeManagerSelection(
                selected.ThemeFamily,
                DeucarianThemeMode.Dark,
                selected.DefaultStyle);

            DeucarianThemeManagerActivationResult result =
                DeucarianThemeManagerWorkflow.Activate(
                    settings,
                    selection,
                    Array.Empty<DeucarianThemeProvider>());

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(0, result.ProviderCount);
            Assert.AreSame(selected.ThemeFamily, settings.DefaultThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Dark, settings.DefaultThemeMode);
            StringAssert.Contains("No loaded scene provider", result.Message);
        }

        [Test]
        public void CreatedCustomStyleRollbackDeletesAssetAndRestoresPreviousDraft()
        {
            DeucarianDefaultThemeAssets previous = CreateFamily("PreviousDraft");
            DeucarianDefaultThemeAssets sourceAssets = CreateFamily("SourceDraft");
            DeucarianThemingEditorSettings.SetDraftSelection(
                previous.ThemeFamily,
                DeucarianThemeMode.Light,
                previous.DefaultStyle);
            DeucarianThemeManagerSelection previousDraft =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            string customPath = testRoot + "/CreatedThenFailed.asset";

            DeucarianThemeStyle created = DeucarianThemingMenuActions.CreateCustomStyle(
                sourceAssets.DefaultStyle,
                customPath,
                sourceAssets.DefaultStyle.SurfaceProfile,
                sourceAssets.DefaultStyle.ShapeProfile,
                sourceAssets.DefaultStyle.StrokeProfile,
                sourceAssets.DefaultStyle.Density);

            Assert.IsNotNull(created);
            Assert.AreSame(created, DeucarianThemingEditorSettings.ActiveStyle);
            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(customPath));

            Assert.IsTrue(DeucarianThemeManagerWindow.RollbackCreatedCustomStyle(
                customPath,
                previousDraft));
            Assert.IsNull(AssetDatabase.LoadMainAssetAtPath(customPath));
            Assert.AreSame(previous.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, DeucarianThemingEditorSettings.ActiveThemeMode);
            Assert.AreSame(previous.DefaultStyle, DeucarianThemingEditorSettings.ActiveStyle);
            Assert.AreSame(previous.LightTheme, DeucarianThemingEditorSettings.ActiveTheme);
            Assert.AreSame(previous.LightPalette, DeucarianThemingEditorSettings.ActivePalette);
            Assert.AreSame(
                previous.LightPalette.RoleLibrary,
                DeucarianThemingEditorSettings.ActiveRoleLibrary);
        }

        [Test]
        public void CustomStyleEditRemainsStagedUntilAtomicActivation()
        {
            DeucarianDefaultThemeAssets assets = CreateFamily("CustomStyle");
            DeucarianThemeStyle source = assets.DefaultStyle;
            DeucarianThemeStyle custom = DeucarianThemingMenuActions.CreateCustomStyle(
                source,
                testRoot + "/CustomStyle.asset",
                source.SurfaceProfile,
                source.ShapeProfile,
                source.StrokeProfile,
                source.Density);
            custom.SetComposition(
                source.SurfaceProfile,
                null,
                source.StrokeProfile,
                source.Density,
                true);
            EditorUtility.SetDirty(custom);
            AssetDatabase.SaveAssetIfDirty(custom);
            Assert.AreEqual(DeucarianThemeStyleCompositionKind.Incomplete, custom.CompositionKind);
            DeucarianThemeShapeProfile square = CreateAsset<DeucarianThemeShapeProfile>("SquareShape.asset");
            square.Configure(
                DeucarianThemePresentationProfileIds.Shape.Square,
                "Square",
                "Square workflow test shape.",
                0f);
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            DeucarianThemeManagerSelection selection = new DeucarianThemeManagerSelection(
                assets.ThemeFamily,
                DeucarianThemeMode.Dark,
                custom);
            DeucarianThemeManagerStyleEdit edit = new DeucarianThemeManagerStyleEdit(
                custom,
                source.SurfaceProfile,
                square,
                source.StrokeProfile,
                DeucarianThemeDensity.Compact);
            DeucarianThemeManagerStyleEdit incompleteEdit = new DeucarianThemeManagerStyleEdit(
                custom,
                source.SurfaceProfile,
                square,
                source.StrokeProfile,
                DeucarianThemeDensity.Unspecified);
            DeucarianThemeShapeProfile originalShape = custom.ShapeProfile;
            int notifications = 0;
            UnityEngine.Object notifiedAsset = null;
            Action<UnityEngine.Object> listener = asset =>
            {
                notifications++;
                notifiedAsset = asset;
            };
            DeucarianThemeAssetChangeBus.AssetChanged += listener;

            try
            {
                DeucarianThemeManagerActivationResult blocked =
                    DeucarianThemeManagerWorkflow.Activate(
                        settings,
                        selection,
                        incompleteEdit,
                        Array.Empty<DeucarianThemeProvider>());
                Assert.IsFalse(blocked.Succeeded);
                Assert.AreSame(originalShape, custom.ShapeProfile);
                Assert.AreSame(source, assets.LightTheme.VisualStyle);
                Assert.AreSame(source, assets.DarkTheme.VisualStyle);
                Assert.AreEqual(0, notifications);

                AssetDatabase.ImportAsset(
                    AssetDatabase.GetAssetPath(custom),
                    ImportAssetOptions.ForceUpdate);
                Assert.IsNull(custom.ShapeProfile);

                settings.Configure(assets.ThemeFamily, DeucarianThemeMode.Dark);
                notifications = 0;
                notifiedAsset = null;
                DeucarianThemeManagerActivationResult activated =
                    DeucarianThemeManagerWorkflow.Activate(
                        settings,
                        selection,
                        edit,
                        Array.Empty<DeucarianThemeProvider>());

                Assert.IsTrue(activated.Succeeded, activated.Message);
                Assert.AreSame(square, custom.ShapeProfile);
                Assert.AreEqual(DeucarianThemeDensity.Compact, custom.Density);
                Assert.AreSame(custom, assets.LightTheme.VisualStyle);
                Assert.AreSame(custom, assets.DarkTheme.VisualStyle);
                Assert.AreEqual(1, notifications);
                Assert.AreSame(custom, notifiedAsset);
            }
            finally
            {
                DeucarianThemeAssetChangeBus.AssetChanged -= listener;
            }
        }

        [Test]
        public void HydrationDerivesMissingStyleWithoutReplacingValidFamilyAndMode()
        {
            DeucarianDefaultThemeAssets projectDefault = CreateFamily("ProjectDefault");
            DeucarianDefaultThemeAssets local = CreateFamily("Local");
            DeucarianThemeRuntimeSettings settings = CreateSettings("Settings.asset");
            settings.Configure(projectDefault.ThemeFamily, DeucarianThemeMode.Dark);
            DeucarianThemingEditorSettings.ActiveThemeFamily = local.ThemeFamily;
            DeucarianThemingEditorSettings.ActiveThemeMode = DeucarianThemeMode.Light;
            DeucarianThemingEditorSettings.ActiveStyle = null;

            Assert.IsTrue(DeucarianThemingMenuActions.TryHydrateActiveAssetsFromProjectDefault(settings));
            Assert.AreSame(local.ThemeFamily, DeucarianThemingEditorSettings.ActiveThemeFamily);
            Assert.AreEqual(DeucarianThemeMode.Light, DeucarianThemingEditorSettings.ActiveThemeMode);
            Assert.AreSame(local.LightTheme.VisualStyle, DeucarianThemingEditorSettings.ActiveStyle);
            Assert.AreSame(local.LightTheme, DeucarianThemingEditorSettings.ActiveTheme);
        }

        [Test]
        public void RuntimeSettingsPathMustUseExactResourcesContract()
        {
            Assert.IsTrue(DeucarianThemeManagerWindow.IsRuntimeSettingsResourcePath(
                "Assets/Config/Resources/DeucarianThemeRuntimeSettings.asset"));
            Assert.IsFalse(DeucarianThemeManagerWindow.IsRuntimeSettingsResourcePath(
                "Assets/Config/DeucarianThemeRuntimeSettings.asset"));
            Assert.IsFalse(DeucarianThemeManagerWindow.IsRuntimeSettingsResourcePath(
                "Assets/Resources/OtherSettings.asset"));
        }

        [Test]
        public void ThemeManagerUsesSharedResponsiveWorkbenchAndToolbar()
        {
            DeucarianThemeManagerWindow window =
                ScriptableObject.CreateInstance<DeucarianThemeManagerWindow>();
            try
            {
                window.CreateGUI();
                DeucarianEditorWorkbench workbench = window.WorkbenchForTests;

                Assert.IsNotNull(workbench);
                Assert.IsNotNull(workbench.Toolbar);
                Assert.IsNotNull(workbench.Content);
                Button themeButton = workbench.Toolbar.Q<Button>(
                    "deucarian-theme-manager-view-theme");
                Button styleButton = workbench.Toolbar.Q<Button>(
                    "deucarian-theme-manager-view-style");
                Button settingsButton = workbench.Toolbar.Q<Button>(
                    "deucarian-theme-manager-view-runtime-settings");
                Label summary = workbench.Toolbar.Q<Label>(
                    "deucarian-theme-manager-toolbar-summary");
                Button secondary = workbench.Toolbar.Q<Button>(
                    "deucarian-theme-manager-toolbar-secondary");
                Button primary = workbench.Toolbar.Q<Button>(
                    "deucarian-theme-manager-toolbar-primary");
                Assert.IsNotNull(themeButton);
                Assert.IsNotNull(styleButton);
                Assert.IsNotNull(settingsButton);
                Assert.IsNotNull(summary);
                Assert.IsNotNull(secondary);
                Assert.IsNotNull(primary);
                IMGUIContainer content = workbench.Content.Q<IMGUIContainer>(
                    "deucarian-theme-manager-content");
                Assert.IsNotNull(content);
                Assert.AreEqual(1f, content.style.flexGrow.value);
                Assert.AreEqual(0f, content.style.minHeight.value.value);
                Assert.AreSame(themeButton, workbench.Toolbar.ElementAt(0));
                Assert.AreSame(styleButton, workbench.Toolbar.ElementAt(1));
                Assert.AreSame(settingsButton, workbench.Toolbar.ElementAt(2));
                Assert.AreSame(summary, workbench.Toolbar.ElementAt(3));
                Assert.IsTrue(workbench.Toolbar.ElementAt(4).ClassListContains(
                    DeucarianEditorWorkbenchToolbar.SpacerClass));
                Assert.AreSame(secondary, workbench.Toolbar.ElementAt(5));
                Assert.AreSame(primary, workbench.Toolbar.ElementAt(6));
                Assert.IsTrue(primary
                    .ClassListContains(DeucarianEditorWorkbenchToolbar.EmphasizedActionClass));

                Assert.AreEqual(
                    DeucarianEditorLayoutMode.Narrow,
                    workbench.ApplyResponsiveLayout(899f));
                Assert.IsTrue(workbench.ShellContent.ClassListContains(
                    DeucarianEditorResponsiveLayout.NarrowClass));
                Assert.AreEqual(
                    DeucarianEditorLayoutMode.Compact,
                    workbench.ApplyResponsiveLayout(900f));
                Assert.IsTrue(workbench.ShellContent.ClassListContains(
                    DeucarianEditorResponsiveLayout.CompactClass));
                Assert.AreEqual(
                    DeucarianEditorLayoutMode.Wide,
                    workbench.ApplyResponsiveLayout(1180f));
                Assert.IsTrue(workbench.ShellContent.ClassListContains(
                    DeucarianEditorResponsiveLayout.WideClass));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ThemeManagerSourceUsesWorkbenchWithoutLegacyLargeHeader()
        {
            DeucarianThemeManagerWindow window =
                ScriptableObject.CreateInstance<DeucarianThemeManagerWindow>();
            try
            {
                MonoScript script = MonoScript.FromScriptableObject(window);
                string assetPath = AssetDatabase.GetAssetPath(script);
                string absolutePath = ResolveAbsoluteAssetPath(assetPath);
                string source = File.ReadAllText(absolutePath);

                StringAssert.Contains("DeucarianEditorWorkbench.Create", source);
                StringAssert.Contains("DeucarianEditorWorkbenchGUI.BeginSurface", source);
                StringAssert.Contains("DeucarianEditorWorkbenchGUI.DrawPanel", source);
                StringAssert.DoesNotContain("CreateWindowShell", source);
                StringAssert.DoesNotContain("DrawHeaderCard", source);
                StringAssert.DoesNotContain("BeginScrollView", source);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        [Test]
        public void ComposerPreviewInsetsContentByConfiguredBorderWidth()
        {
            Rect rect = new Rect(0f, 0f, 120f, 64f);

            Rect borderless = DeucarianThemeManagerWindow.DrawPreviewSurface(
                rect,
                Color.gray,
                Color.white,
                8f,
                0f);
            Rect onePixel = DeucarianThemeManagerWindow.DrawPreviewSurface(
                rect,
                Color.gray,
                Color.white,
                8f,
                1f);
            Rect fourPixels = DeucarianThemeManagerWindow.DrawPreviewSurface(
                rect,
                Color.gray,
                Color.white,
                8f,
                4f);

            Assert.AreEqual(rect, borderless);
            Assert.AreEqual(1f, onePixel.x);
            Assert.AreEqual(118f, onePixel.width);
            Assert.AreEqual(4f, fourPixels.x);
            Assert.AreEqual(112f, fourPixels.width);
        }

        [Test]
        public void RuntimeSettingsCandidateRejectsDuplicateResources()
        {
            int baselineCount =
                DeucarianThemeManagerWindow.FindRuntimeSettingsResourceAssets().Count;
            DeucarianDefaultThemeAssets assets = CreateFamily("DuplicateSettings");
            DeucarianThemeRuntimeSettings first =
                CreateAsset<DeucarianThemeRuntimeSettings>(
                    "One/Resources/DeucarianThemeRuntimeSettings.asset");
            first.Configure(assets.ThemeFamily, DeucarianThemeMode.Dark);
            CreateAsset<DeucarianThemeRuntimeSettings>(
                "Two/Resources/DeucarianThemeRuntimeSettings.asset");
            AssetDatabase.Refresh();

            int expectedCount = baselineCount + 2;
            Assert.AreEqual(
                expectedCount,
                DeucarianThemeManagerWindow.FindRuntimeSettingsResourceAssets().Count);
            bool candidateValid = DeucarianThemeManagerWindow.TryValidateRuntimeSettingsCandidate(
                first,
                out string message);
            Assert.IsFalse(candidateValid);
            StringAssert.Contains("Found " + expectedCount, message);
            Assert.IsNull(DeucarianThemeManagerWindow.CreateRuntimeSettingsAtPath(
                testRoot + "/Three/Resources/DeucarianThemeRuntimeSettings.asset"));

            DeucarianThemeManagerSelection selection = new DeucarianThemeManagerSelection(
                assets.ThemeFamily,
                DeucarianThemeMode.Dark,
                assets.DefaultStyle);
            DeucarianThemeManagerActivationStatus status =
                DeucarianThemeManagerWorkflow.Evaluate(
                    first,
                    selection,
                    candidateValid,
                    message,
                    Array.Empty<DeucarianThemeProvider>());
            Assert.IsFalse(status.RuntimeSettingsReady);
            Assert.IsFalse(status.CanActivate);
            StringAssert.Contains("Found " + expectedCount, status.Message);
            Assert.IsFalse(DeucarianThemeManagerWorkflow.Activate(
                first,
                selection,
                Array.Empty<DeucarianThemeProvider>()).Succeeded);
        }

        private DeucarianDefaultThemeAssets CreateFamily(string name)
        {
            return DeucarianDefaultThemeAssetFactory.CreateThemeFamily(
                testRoot + "/" + name + "/" + name + "ThemeFamily.asset");
        }

        private static string ResolveAbsoluteAssetPath(string assetPath)
        {
            string projectRelativePath = Path.GetFullPath(assetPath);
            if (File.Exists(projectRelativePath))
            {
                return projectRelativePath;
            }

            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            Assert.IsNotNull(package, "Could not resolve the theming package for its manager source.");
            string prefix = "Packages/" + package.name + "/";
            Assert.IsTrue(assetPath.StartsWith(prefix, StringComparison.Ordinal));
            return Path.Combine(package.resolvedPath, assetPath.Substring(prefix.Length));
        }

        private DeucarianThemeRuntimeSettings CreateSettings(string name)
        {
            return CreateAsset<DeucarianThemeRuntimeSettings>(name);
        }

        private T CreateAsset<T>(string name)
            where T : ScriptableObject
        {
            string path = testRoot + "/" + name;
            string folder = path.Substring(0, path.LastIndexOf('/'));
            DeucarianThemingMenuActions.EnsureAssetFolder(folder);
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        private sealed class ThrowingThemeTarget : MonoBehaviour, IDeucarianThemeTarget
        {
            public void ApplyTheme(DeucarianTheme theme)
            {
                throw new InvalidOperationException("Workflow test target failure.");
            }
        }

        private sealed class SerializedPreviewThemeTarget : MonoBehaviour, IDeucarianThemeTarget
        {
            [SerializeField] private DeucarianTheme appliedTheme;

            internal DeucarianTheme AppliedTheme => appliedTheme;

            public void ApplyTheme(DeucarianTheme theme)
            {
                appliedTheme = theme;
            }
        }
    }
}
