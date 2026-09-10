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
