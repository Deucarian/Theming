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
            DrawDefaultAssetsSection();
            DrawAssetSummary();
            DrawAdvancedSection();
            DeucarianEditorChrome.DrawFooterVersion("com.deucarian.theming", "0.4.2");

            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveAssetFields()
        {
            DeucarianEditorChrome.DrawSectionHeader("Active Assets");
            DeucarianEditorChrome.BeginSection();

            DeucarianThemingEditorSettings.ActivePalette = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Active Palette",
                DeucarianThemingEditorSettings.ActivePalette,
                "Select",
                palette => DeucarianThemingEditorSettings.ActivePalette = palette,
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActivePaletteFirst());

            DeucarianThemingEditorSettings.ActiveTheme = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Active Theme",
                DeucarianThemingEditorSettings.ActiveTheme,
                "Select",
                theme => DeucarianThemingEditorSettings.ActiveTheme = theme,
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveTheme());

            DeucarianThemingEditorSettings.ActiveRoleLibrary = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Role Library",
                DeucarianThemingEditorSettings.ActiveRoleLibrary,
                "Select",
                roleLibrary => DeucarianThemingEditorSettings.ActiveRoleLibrary = roleLibrary,
                null,
                () => DeucarianThemingMenuActions.ResolveOrCreateActiveRoleLibrary());

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Minimal Palette", GUILayout.Width(156)))
            {
                DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMinimalPaletteFromSavePanel();
                if (assets != null)
                {
                    RefreshAssets(true);
                }
            }

            using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActivePalette == null))
            {
                if (GUILayout.Button("Create Theme From Active Palette", GUILayout.Width(220)))
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
            if (GUILayout.Button("Repair Palette Setup", GUILayout.Width(156)))
            {
                DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.RepairActivePaletteSetup();
                if (assets != null)
                {
                    RefreshAssets(true);
                }
            }

            using (new EditorGUI.DisabledScope(DeucarianThemingEditorSettings.ActiveTheme == null))
            {
                if (GUILayout.Button("Apply Theme To Scene", GUILayout.Width(168)))
                {
                    DeucarianThemingMenuActions.ApplyActiveThemeToOpenScene();
                }
            }

            EditorGUILayout.EndHorizontal();
            DeucarianEditorChrome.EndSection();
        }

        private void DrawDefaultAssetsSection()
        {
            DeucarianEditorChrome.DrawSectionHeader("Default Assets");
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
            EditorGUILayout.LabelField("Status", defaultsReady ? "Default assets ready" : "Missing defaults");

            if (!defaultsReady)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create Missing Defaults", GUILayout.Width(172)))
                {
                    DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets();
                    DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
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
            DrawCountRow("Themes", searchResult.Themes.Count);
            DrawCountRow("Palettes", searchResult.Palettes.Count);
            DrawCountRow("Role Libraries", searchResult.RoleLibraries.Count);

            if (searchResult.Themes.Count == 0 && searchResult.Palettes.Count == 0 && searchResult.RoleLibraries.Count == 0)
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
                if (GUILayout.Button("Create Game Theme Assets", GUILayout.Width(196)))
                {
                    DeucarianDefaultThemeAssets assets = DeucarianThemingMenuActions.CreateGameThemeAssets();
                    DeucarianThemingMenuActions.SelectAndPing(assets.Theme);
                    RefreshAssets(true);
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

            if (library == null || palette == null || theme == null || theme.ColorPalette != palette || palette.RoleLibrary != library)
            {
                return false;
            }

            for (int i = 0; i < DefaultRoleIds.Length; i++)
            {
                string roleId = DefaultRoleIds[i];
                if (!library.TryGetRoleById(roleId, out DeucarianColorRole role)
                    || role == null
                    || !HasPaletteEntryForRole(palette, role))
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

        private void RefreshAssets(bool autoSelectSingles)
        {
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
