using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeManagerSummary
    {
        internal static void DrawStatus(DeucarianThemeManagerActivationStatus status)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string label;
                DeucarianEditorStatus visualStatus;
                if (!status.HasRuntimeSettings)
                {
                    label = "Setup required";
                    visualStatus = DeucarianEditorStatus.Error;
                }
                else if (!status.RuntimeSettingsReady)
                {
                    label = "Setup incomplete";
                    visualStatus = DeucarianEditorStatus.Warning;
                }
                else if (!status.SelectionValid)
                {
                    label = "Incomplete";
                    visualStatus = DeucarianEditorStatus.Warning;
                }
                else if (status.IsActive)
                {
                    label = "Active";
                    visualStatus = DeucarianEditorStatus.Success;
                }
                else if (!status.HasDraftChanges)
                {
                    label = "Needs sync";
                    visualStatus = DeucarianEditorStatus.Warning;
                }
                else
                {
                    label = "Not active";
                    visualStatus = DeucarianEditorStatus.Info;
                }

                DeucarianEditorStatusBadge.Draw(label, visualStatus, GUILayout.Width(112f));
                EditorGUILayout.LabelField(status.Message, DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
            }
        }

        internal static void DrawResolvedSummary(DeucarianThemeManagerSelection selection)
        {
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Resolved Theme",
                selection.ResolvedTheme != null ? selection.ResolvedTheme.DisplayName : "Not resolved",
                "Derived from the selected family and mode.");
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Palette",
                selection.ResolvedPalette != null ? selection.ResolvedPalette.DisplayName : "Not resolved",
                "Derived from the resolved theme.");
        }

        internal static void DrawStyleSummary(DeucarianThemeStyle style)
        {
            if (style == null)
            {
                return;
            }

            GUILayout.Space(2f);
            EditorGUILayout.LabelField("Appearance", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
            const string tooltip = "This value is composed by the selected visual style. Use Style Composer to change it.";
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Surface",
                style.SurfaceProfile != null ? style.SurfaceProfile.DisplayName : "Legacy inline",
                tooltip);
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Corners",
                style.ShapeProfile != null ? style.ShapeProfile.DisplayName : "Legacy inline",
                tooltip);
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Border",
                style.StrokeProfile != null ? style.StrokeProfile.DisplayName : "Legacy inline",
                tooltip);
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Size",
                style.Density == DeucarianThemeDensity.Unspecified
                    ? "Legacy automatic"
                    : style.Density.ToString(),
                tooltip);
            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                "Typography",
                style.TypographyProfile != null
                    ? style.TypographyProfile.DisplayName
                    : "Project TMP default",
                tooltip);
        }

        internal static void DrawAudioSummary(DeucarianTheme theme)
        {
            DeucarianAudioPaletteSet set = theme != null ? theme.AudioPaletteSet : null;
            DeucarianAudioExperience previewExperience =
                DeucarianAudioPaletteLabWindow.PreviewExperience;
            DeucarianAudioPalette resolved = set != null
                ? set.GetPalette(previewExperience) ?? set.DefaultPalette
                : null;
            int warningCount = set != null ? set.GetValidationWarnings().Count : 1;

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Audio Palette", DeucarianEditorWorkbenchGUI.BoldLabelStyle);
            EditorGUILayout.LabelField(
                set != null
                    ? $"{set.name} · {previewExperience} · "
                      + (resolved != null ? resolved.DisplayName : "Missing palette")
                    : "No Audio Palette Set is linked to the resolved theme.",
                DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
            EditorGUILayout.LabelField(
                warningCount == 0 ? "Audio validation: ready" : $"Audio validation: {warningCount} issue(s)",
                DeucarianEditorWorkbenchGUI.WordWrappedMiniLabelStyle);
            using (new EditorGUI.DisabledScope(set == null))
            {
                if (GUILayout.Button("Open Audio Palette Lab"))
                {
                    DeucarianAudioPaletteLabWindow.Open(set);
                }
            }
        }
    }
}
