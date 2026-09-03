using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianAudioPaletteLabWindow
    {
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
                "Theme",
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

        private DeucarianAudioRole FindRole(string id)
        {
            IReadOnlyList<DeucarianAudioRole> roles = CollectRoles();
            for (int i = 0; i < roles.Count; i++)
            {
                if (roles[i] != null && roles[i].Id == id)
                {
                    return roles[i];
                }
            }

            return null;
        }

        private string DescribeRoleRow(DeucarianAudioRole role)
        {
            if (paletteSet == null || role == null ||
                !paletteSet.TryResolve(role, experience, out DeucarianAudioResolution resolution))
            {
                return role != null ? $"{role.DisplayName}  ·  missing" : "Missing role";
            }

            string state = resolution.Cue.IntentionalSilence
                ? "muted"
                : resolution.IsAudible ? $"{resolution.Cue.UsableVariantCount} clip(s)" : "missing";
            return $"{role.DisplayName}  ·  {resolution.Source}  ·  {state}";
        }

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
