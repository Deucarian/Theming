using System;
using System.Text.RegularExpressions;
using Deucarian.Theming.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

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
        }

        [TearDown]
        public void TearDown()
        {
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
        public void EvaluateBlocksMissingSettingsAndIncompleteFamily()
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
            DeucarianThemeManagerActivationStatus incompleteSettings =
                DeucarianThemeManagerWorkflow.Evaluate(
                    settings,
                    completeSelection,
                    Array.Empty<DeucarianThemeProvider>());
            Assert.IsTrue(incompleteSettings.HasRuntimeSettings);
            Assert.IsFalse(incompleteSettings.RuntimeSettingsReady);
            Assert.IsFalse(incompleteSettings.CanActivate);
            Assert.IsFalse(DeucarianThemeManagerWorkflow.Activate(
                settings,
                completeSelection,
                Array.Empty<DeucarianThemeProvider>()).Succeeded);

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
                        edit,
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
    }
}
