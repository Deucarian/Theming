using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Editor window for discovering, creating, selecting, and applying Deucarian theme assets.
    /// </summary>
    public sealed class DeucarianThemeManagerWindow : EditorWindow
    {
        private const string AdvancedFoldoutKey = "Deucarian.Theming.ThemeManager.AdvancedFoldout";

        private static readonly string[] DefaultRoleIds =
        {
            DeucarianBuiltinColorRoleIds.Core.Background,
            DeucarianBuiltinColorRoleIds.Core.Surface,
            DeucarianBuiltinColorRoleIds.Core.SurfaceRaised,
            DeucarianBuiltinColorRoleIds.Core.Primary,
            DeucarianBuiltinColorRoleIds.Core.Secondary,
            DeucarianBuiltinColorRoleIds.Core.Accent,
            DeucarianBuiltinColorRoleIds.Text.Primary,
            DeucarianBuiltinColorRoleIds.Text.Secondary,
            DeucarianBuiltinColorRoleIds.Text.Muted,
            DeucarianBuiltinColorRoleIds.Text.Disabled,
            DeucarianBuiltinColorRoleIds.Status.Success,
            DeucarianBuiltinColorRoleIds.Status.Warning,
            DeucarianBuiltinColorRoleIds.Status.Error,
            DeucarianBuiltinColorRoleIds.Status.Info,
            DeucarianBuiltinColorRoleIds.UI.Normal,
            DeucarianBuiltinColorRoleIds.UI.Highlighted,
            DeucarianBuiltinColorRoleIds.UI.Pressed,
            DeucarianBuiltinColorRoleIds.UI.Selected,
            DeucarianBuiltinColorRoleIds.UI.Disabled,
            DeucarianBuiltinColorRoleIds.UI.Focused
        };

        private static readonly string[] DefaultStyleIds =
        {
            DeucarianThemeStyleIds.FrostedGlass,
            DeucarianThemeStyleIds.MaterialDark,
            DeucarianThemeStyleIds.FluentAcrylic
        };

        private DeucarianThemingMenuActions.AssetSearchResult searchResult;
        private bool advancedFoldout;
        private Vector2 scrollPosition;

        public static void OpenWindow()
        {
            DeucarianThemeManagerWindow window = GetWindow<DeucarianThemeManagerWindow>("Theme Manager");
            window.minSize = new Vector2(420f, 360f);
            window.RefreshAssets(true);
            window.Show();
        }

        private void OnEnable()
        {
            advancedFoldout = EditorPrefs.GetBool(AdvancedFoldoutKey, false);
            RefreshAssets(false);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DeucarianEditorChrome.DrawPackageHeader(
                "theming",
                "Deucarian Theming",
                "Create, discover, select, and apply runtime theme assets.");

            DrawActiveAssetFields();
            DrawProjectThemeDefaultSection();
            DrawStarterAssetsSection();
            DrawAssetSummary();
            DrawAdvancedSection();
            DeucarianEditorChrome.DrawFooterVersion("com.deucarian.theming", "0.4.2");

            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveAssetFields()
        {
            DeucarianEditorChrome.DrawSectionHeader("Active Assets");
            DeucarianEditorChrome.BeginSection();

            DeucarianThemingEditorSettings.ActiveThemeFamily = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Theme Family",
                DeucarianThemingEditorSettings.ActiveThemeFamily,
                "Select",
                family => DeucarianThemingMenuActions.SetActiveThemeFamilyAndApply(family),
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveThemeFamily());

            EditorGUI.BeginChangeCheck();
            DeucarianThemeMode previewMode = (DeucarianThemeMode)EditorGUILayout.EnumPopup(
                "Preview Mode",
                DeucarianThemingEditorSettings.ActiveThemeMode);
            if (EditorGUI.EndChangeCheck())
            {
                DeucarianThemingMenuActions.SetActiveThemeModeAndApply(previewMode);
            }

            DeucarianThemingEditorSettings.ActivePalette = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Active Palette",
                DeucarianThemingEditorSettings.ActivePalette,
                "Select",
                palette => DeucarianThemingMenuActions.SetActivePaletteAndApply(palette),
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActivePaletteFirst());

            using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActiveThemeFamily != null))
            {
                DeucarianThemingEditorSettings.ActiveTheme = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                    DeucarianThemingEditorSettings.ActiveThemeFamily != null ? "Resolved Theme" : "Active Theme (Legacy)",
                    DeucarianThemingEditorSettings.ActiveTheme,
                    "Select",
                    theme => DeucarianThemingMenuActions.SetActiveThemeAndApply(theme),
                    null,
                    () => DeucarianThemingMenuActions.ResolveOrCreateActiveTheme());
            }

            DeucarianThemingEditorSettings.ActiveRoleLibrary = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Role Library",
                DeucarianThemingEditorSettings.ActiveRoleLibrary,
                "Select",
                roleLibrary => DeucarianThemingMenuActions.SetActiveRoleLibraryAndApply(roleLibrary),
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveRoleLibrary());

            DeucarianThemingEditorSettings.ActiveStyle = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Active Style",
                DeucarianThemingEditorSettings.ActiveStyle,
                "Select",
                style => DeucarianThemingMenuActions.SetActiveStyleAndApply(style),
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveStyle());

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Theme Family", GUILayout.Width(156)))
            {
                DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateThemeFamilyFromSavePanel();
                if (assets != null)
                {
                    RefreshAssets(true);
                }
            }

            using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActiveThemeFamily == null))
            {
                if (GUILayout.Button("Repair Theme Family", GUILayout.Width(180)))
                {
                    DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.RepairActiveThemeFamilySetup();
                    if (assets != null)
                    {
                        RefreshAssets(true);
                    }
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(
                       DeucarianThemingEditorSettings.ActiveThemeFamily == null
                       && DeucarianThemingEditorSettings.ActiveTheme == null))
            {
                if (GUILayout.Button("Apply Preview To Scene", GUILayout.Width(168)))
                {
                    DeucarianThemingMenuActions.ApplyActiveThemeToOpenScene();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(
                       DeucarianThemingEditorSettings.ActiveThemeFamily == null
                       || DeucarianThemingEditorSettings.ActiveStyle == null))
            {
                if (GUILayout.Button("Assign Style To Theme Family", GUILayout.Width(220)))
                {
                    DeucarianThemingMenuActions.AssignActiveStyleToActiveThemeFamily();
                    RefreshAssets(true);
                }
            }

            EditorGUILayout.EndHorizontal();
            DeucarianEditorChrome.EndSection();
        }

        private void DrawProjectThemeDefaultSection()
        {
            DeucarianEditorChrome.DrawSectionHeader("Project Theme Default");
            DeucarianEditorChrome.BeginSection();

            DeucarianThemeRuntimeSettings settings =
                DeucarianThemingMenuActions.ResolveProjectRuntimeSettings();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Runtime Settings",
                    settings,
                    typeof(DeucarianThemeRuntimeSettings),
                    false);
                EditorGUILayout.ObjectField(
                    "Default Family",
                    settings != null ? settings.DefaultThemeFamily : null,
                    typeof(DeucarianThemeFamily),
                    false);
                EditorGUILayout.EnumPopup(
                    "Default Mode",
                    settings != null ? settings.DefaultThemeMode : DeucarianThemeMode.Dark);
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "No runtime settings asset was found. Create '"
                    + DeucarianThemeRuntimeSettings.ResourceName
                    + ".asset' in a Resources folder to source-control the project default.",
                    MessageType.Warning);
            }
            else if (settings.DefaultThemeFamily == null)
            {
                EditorGUILayout.HelpBox(
                    "Runtime settings are present, but no default theme family is assigned.",
                    MessageType.Warning);
            }
            else if (!settings.DefaultThemeFamily.IsComplete)
            {
                EditorGUILayout.HelpBox(
                    "The project default family is incomplete. Assign both light and dark themes before shipping.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Status", "Project theme default ready");
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(
                       settings == null || DeucarianThemingEditorSettings.ActiveThemeFamily == null))
            {
                if (GUILayout.Button("Set Active as Project Default", GUILayout.Width(220)))
                {
                    DeucarianThemingMenuActions.SetActiveThemeFamilyAsProjectDefault(settings);
                    RefreshAssets(false);
                }
            }

            using (new EditorGUI.DisabledScope(settings == null))
            {
                if (GUILayout.Button("Select Settings", GUILayout.Width(112)))
                {
                    DeucarianThemingMenuActions.SelectAndPing(settings);
                }
            }

            EditorGUILayout.EndHorizontal();
            DeucarianEditorChrome.EndSection();
        }

        private void DrawStarterAssetsSection()
        {
            DeucarianEditorChrome.DrawSectionHeader("Starter Assets (Optional)");
            DeucarianEditorChrome.BeginSection();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            string folder = EditorGUILayout.TextField("Path", DeucarianThemingEditorSettings.DefaultAssetFolder);
            if (GUILayout.Button("Open Folder", GUILayout.Width(96)))
            {
                DeucarianThemingMenuActions.OpenThemeAssetsFolder();
            }

            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                DeucarianThemingEditorSettings.DefaultAssetFolder = folder;
            }

            bool defaultsReady = AreDefaultAssetsReady();
            EditorGUILayout.LabelField("Status", defaultsReady ? "Starter assets ready" : "Not created");

            if (!defaultsReady)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create Starter Assets", GUILayout.Width(172)))
                {
                    DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets();
                    DeucarianThemingMenuActions.SelectAndPing(assets.ThemeFamily);
                    RefreshAssets(true);
                }

                EditorGUILayout.EndHorizontal();
            }

            DeucarianEditorChrome.EndSection();
        }

        private void DrawAssetSummary()
        {
            EnsureSearchResult();

            DeucarianEditorChrome.DrawSectionHeader("Found Assets");
            DeucarianEditorChrome.BeginSection();
            DrawCountRow("Theme Families", searchResult.ThemeFamilies.Count);
            DrawCountRow("Themes", searchResult.Themes.Count);
            DrawCountRow("Palettes", searchResult.Palettes.Count);
            DrawCountRow("Role Libraries", searchResult.RoleLibraries.Count);
            DrawCountRow("Styles", searchResult.Styles.Count);

            if (searchResult.ThemeFamilies.Count == 0
                && searchResult.Themes.Count == 0
                && searchResult.Palettes.Count == 0
                && searchResult.RoleLibraries.Count == 0
                && searchResult.Styles.Count == 0)
            {
                DeucarianEditorChrome.DrawInlineHelp(
                    "No Deucarian theme assets were found in this project. Create the default assets to get started.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh Assets", GUILayout.Width(120)))
            {
                RefreshAssets(true);
            }

            EditorGUILayout.EndHorizontal();
            DeucarianEditorChrome.EndSection();
        }

        private void DrawAdvancedSection()
        {
            DeucarianEditorChrome.DrawSectionHeader("Samples & Utilities");
            DeucarianEditorChrome.BeginSection();

            bool nextFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced", true);
            if (nextFoldout != advancedFoldout)
            {
                advancedFoldout = nextFoldout;
                EditorPrefs.SetBool(AdvancedFoldoutKey, advancedFoldout);
            }

            if (advancedFoldout)
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Minimal Palette (Legacy)", GUILayout.Width(220)))
                {
                    DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMinimalPaletteFromSavePanel();
                    if (assets != null)
                    {
                        RefreshAssets(true);
                    }
                }

                using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActivePalette == null))
                {
                    if (GUILayout.Button("Theme From Active Palette", GUILayout.Width(190)))
                    {
                        DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateThemeFromActivePalette();
                        if (assets != null)
                        {
                            RefreshAssets(true);
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActivePalette == null))
                {
                    if (GUILayout.Button("Repair Palette Setup", GUILayout.Width(196)))
                    {
                        DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.RepairActivePaletteSetup();
                        if (assets != null)
                        {
                            RefreshAssets(true);
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(
                           DeucarianThemingEditorSettings.ActiveTheme == null
                           || DeucarianThemingEditorSettings.ActiveThemeFamily != null))
                {
                    if (GUILayout.Button("Wrap Theme As Light", GUILayout.Width(196)))
                    {
                        DeucarianDefaultThemeAssets assets =
                            DeucarianThemingMenuActions.WrapActiveThemeInFamilyFromSavePanel(DeucarianThemeMode.Light);
                        if (assets != null)
                        {
                            RefreshAssets(true);
                        }
                    }

                    if (GUILayout.Button("Wrap Theme As Dark", GUILayout.Width(196)))
                    {
                        DeucarianDefaultThemeAssets assets =
                            DeucarianThemingMenuActions.WrapActiveThemeInFamilyFromSavePanel(DeucarianThemeMode.Dark);
                        if (assets != null)
                        {
                            RefreshAssets(true);
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Game Theme Assets", GUILayout.Width(196)))
                {
                    DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets();
                    DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
                    RefreshAssets(true);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Built-in Theme Styles", GUILayout.Width(196)))
                {
                    DeucarianThemingMenuActions.CreateBuiltinThemeStyleAssets();
                    RefreshAssets(true);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(
                           DeucarianThemingEditorSettings.ActiveTheme == null
                           || DeucarianThemingEditorSettings.ActiveStyle == null))
                {
                    if (GUILayout.Button("Assign Style To Active Theme", GUILayout.Width(196)))
                    {
                        DeucarianThemingMenuActions.AssignActiveStyleToActiveTheme();
                        RefreshAssets(true);
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create UI Toolkit Demo Assets", GUILayout.Width(196)))
                {
                    DeucarianUIToolkitDemoAssetFactory.CreateDemoAssets();
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawCountRow(string label, int count)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.MinWidth(120));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(count.ToString(), GUILayout.Width(32));
            EditorGUILayout.EndHorizontal();
        }

        private static bool AreDefaultAssetsReady()
        {
            string folder = DeucarianThemingEditorSettings.DefaultAssetFolder;
            DeucarianColorRoleLibrary library = AssetDatabase.LoadAssetAtPath<DeucarianColorRoleLibrary>(
                CombineAssetPath(folder, "DefaultColorRoleLibrary.asset"));
            DeucarianColorPalette palette = AssetDatabase.LoadAssetAtPath<DeucarianColorPalette>(
                CombineAssetPath(folder, "DefaultDarkColorPalette.asset"));
            DeucarianTheme theme = AssetDatabase.LoadAssetAtPath<DeucarianTheme>(
                CombineAssetPath(folder, "DefaultTheme.asset"));
            DeucarianColorPalette lightPalette = AssetDatabase.LoadAssetAtPath<DeucarianColorPalette>(
                CombineAssetPath(folder, "DefaultLightColorPalette.asset"));
            DeucarianTheme lightTheme = AssetDatabase.LoadAssetAtPath<DeucarianTheme>(
                CombineAssetPath(folder, "DefaultLightTheme.asset"));
            DeucarianThemeFamily family = AssetDatabase.LoadAssetAtPath<DeucarianThemeFamily>(
                CombineAssetPath(folder, "DefaultThemeFamily.asset"));

            if (library == null
                || palette == null
                || theme == null
                || lightPalette == null
                || lightTheme == null
                || family == null
                || !family.IsComplete
                || family.LightTheme != lightTheme
                || family.DarkTheme != theme
                || theme.ColorPalette != palette
                || lightTheme.ColorPalette != lightPalette
                || palette.RoleLibrary != library
                || lightPalette.RoleLibrary != library
                || theme.VisualStyle == null
                || lightTheme.VisualStyle == null)
            {
                return false;
            }

            for (int i = 0; i < DefaultRoleIds.Length; i++)
            {
                string roleId = DefaultRoleIds[i];
                if (!library.TryGetRoleById(roleId, out DeucarianColorRole role)
                    || role == null
                    || !HasPaletteEntryForRole(palette, role)
                    || !HasPaletteEntryForRole(lightPalette, role))
                {
                    return false;
                }
            }

            for (int i = 0; i < DefaultStyleIds.Length; i++)
            {
                if (!HasStyleAssetWithId(folder, DefaultStyleIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasPaletteEntryForRole(DeucarianColorPalette palette, DeucarianColorRole role)
        {
            for (int i = 0; i < palette.Entries.Count; i++)
            {
                DeucarianColorEntry entry = palette.Entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                if (entry.Role == role || entry.Role.Id == role.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private static string CombineAssetPath(string left, string right)
        {
            string normalizedLeft = DeucarianThemingEditorSettings.NormalizeAssetPath(left);
            return string.IsNullOrEmpty(normalizedLeft)
                ? right.TrimStart('/')
                : normalizedLeft.TrimEnd('/') + "/" + right.TrimStart('/');
        }

        private static bool HasStyleAssetWithId(string defaultFolder, string styleId)
        {
            string stylesFolder = CombineAssetPath(defaultFolder, DeucarianDefaultThemeAssetFactory.BuiltinStylesFolderName);
            if (!AssetDatabase.IsValidFolder(stylesFolder))
            {
                return false;
            }

            string[] guids = AssetDatabase.FindAssets("t:" + nameof(DeucarianThemeStyle), new[] { stylesFolder });
            string normalizedId = DeucarianColorRole.NormalizeId(styleId);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                DeucarianThemeStyle style = AssetDatabase.LoadAssetAtPath<DeucarianThemeStyle>(path);
                if (style != null && style.StyleId == normalizedId)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshAssets(bool autoSelectSingles)
        {
            DeucarianThemingMenuActions.TryHydrateActiveAssetsFromProjectDefault();
            searchResult = DeucarianThemingMenuActions.FindExistingAssets(null, autoSelectSingles);
            Repaint();
        }

        private void EnsureSearchResult()
        {
            if (searchResult == null)
            {
                RefreshAssets(false);
            }
        }
    }
}
