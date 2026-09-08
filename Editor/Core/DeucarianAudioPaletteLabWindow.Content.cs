using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianAudioPaletteLabWindow
    {
        private void DrawEmptyPaletteActions()
        {
            EditorGUILayout.LabelField("Choose your project's palette, browse assets, or audition the package defaults.", EditorStyles.wordWrappedLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Project palettes…"))
                {
                    var menu = new GenericMenu();
                    string[] guids = AssetDatabase.FindAssets("t:DeucarianAudioPaletteSet", new[] { "Assets" });
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        menu.AddItem(new GUIContent(path), false, () => HandlePaletteSetChanged(AssetDatabase.LoadAssetAtPath<DeucarianAudioPaletteSet>(path)));
                    }
                    if (guids.Length == 0) menu.AddDisabledItem(new GUIContent("No project palette; try Package defaults"));
                    menu.ShowAsContext();
                }
                if (DeucarianEditorButtons.Secondary("Package defaults")) HandlePaletteSetChanged(DeucarianAudioDefaults.LoadPaletteSet());
            }
        }

        private void DrawPlaybackActions()
        {
            bool resolved = TryResolve(out DeucarianAudioResolution resolution);
            bool canPlay = resolved && resolution.IsAudible && preview != null && preview.IsAvailable;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Primary("Play processed", canPlay)) Play(resolution.Cue);
                if (DeucarianEditorButtons.Secondary("Original", canPlay)) Play(resolution.Cue, false);
                if (DeucarianEditorButtons.Secondary("Stop", preview != null && preview.IsAvailable)) StopPreview();
            }
        }

        private static readonly string[] TestPadRoleIds =
        {
            DeucarianBuiltinAudioRoleIds.Hover,
            DeucarianBuiltinAudioRoleIds.Press,
            DeucarianBuiltinAudioRoleIds.Activate,
            DeucarianBuiltinAudioRoleIds.Key,
            DeucarianBuiltinAudioRoleIds.SpecialKey,
            DeucarianBuiltinAudioRoleIds.Info,
            DeucarianBuiltinAudioRoleIds.Success,
            DeucarianBuiltinAudioRoleIds.Warning,
            DeucarianBuiltinAudioRoleIds.Error,
            DeucarianBuiltinAudioRoleIds.Invalid
        };

        private static readonly string[] TestPadLabels =
        {
            "Hover", "Press", "Activate", "Key", "Special Key",
            "Info", "Success", "Warning", "Error", "Invalid"
        };

        private void DrawContextFields()
        {
            theme = DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                "Theme (optional)",
                theme,
                onValueChanged: HandleThemeChanged);

            DeucarianAudioPalette relevantPalette = ResolveRelevantPalette();
            DeucarianAudioRoleLibrary library = relevantPalette != null
                ? relevantPalette.RoleLibrary
                : paletteSet != null && paletteSet.DefaultPalette != null
                    ? paletteSet.DefaultPalette.RoleLibrary
                    : null;

            using (new EditorGUI.DisabledScope(true))
            {
                DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                    "Resolved palette",
                    relevantPalette);
                DeucarianEditorFields.DrawAssetFieldWithSelectButton(
                    "Role library",
                    library);
            }
        }

        private void DrawTestPad()
        {
            DeucarianEditorChrome.BeginSection();
            DeucarianEditorChrome.DrawSectionHeader("Test pad");
            EditorGUILayout.LabelField(
                "Manual semantic requests for the selected experience.",
                EditorStyles.wordWrappedMiniLabel);

            int columns = position.width >= 720f ? 5 : 2;
            for (int i = 0; i < TestPadRoleIds.Length; i++)
            {
                if (i % columns == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                DeucarianAudioRole role = FindRole(TestPadRoleIds[i]);
                bool canPlay = role != null && paletteSet != null &&
                    paletteSet.TryResolve(role, experience, out DeucarianAudioResolution resolution) &&
                    resolution.IsAudible && preview != null && preview.IsAvailable;
                using (new EditorGUI.DisabledScope(!canPlay))
                {
                    if (GUILayout.Button(new GUIContent(
                            TestPadLabels[i],
                            TestPadRoleIds[i]),
                            GUILayout.MinWidth(78f)))
                    {
                        SelectRole(role);
                        if (paletteSet.TryResolve(role, experience, out resolution))
                        {
                            Play(resolution.Cue);
                        }
                    }
                }

                if (i % columns == columns - 1 || i == TestPadRoleIds.Length - 1)
                {
                    EditorGUILayout.EndHorizontal();
                }
            }

            DeucarianEditorChrome.EndSection();
        }

        private void DrawValidationSummary()
        {
            if (paletteSet == null)
            {
                return;
            }

            List<string> warnings = paletteSet.GetValidationWarnings();
            DeucarianAudioPalette relevant = ResolveRelevantPalette();
            if (relevant != null)
            {
                warnings.AddRange(relevant.GetValidationWarnings());
            }

            DeucarianEditorStatusPanel.DrawValidationCard(
                "Audio coverage",
                warnings,
                warnings.Count == 0
                    ? DeucarianEditorStatus.Success
                    : DeucarianEditorStatus.Warning);
        }

        private DeucarianAudioPalette ResolveRelevantPalette()
        {
            if (paletteSet == null)
            {
                return null;
            }

            return paletteSet.GetPalette(experience) ?? paletteSet.DefaultPalette;
        }

        private DeucarianAudioRole FindRole(string id) => DeucarianAudioRoleBrowserModel.Find(paletteSet, experience, id);

        private string DescribeRoleRow(DeucarianAudioRole role) => DeucarianAudioRoleBrowserModel.Describe(paletteSet, experience, role);

        private void HandleThemeChanged(DeucarianTheme value)
        {
            StopPreview();
            theme = value;
            if (theme != null)
            {
                paletteSet = theme.AudioPaletteSet;
            }

            selectedRole = null;
            feedback = theme != null
                ? $"Loaded {theme.DisplayName}."
                : "Select a Theme or Audio Palette Set.";
        }
    }
}
