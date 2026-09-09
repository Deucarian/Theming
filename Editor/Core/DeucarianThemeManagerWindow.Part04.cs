using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianThemeManagerWindow
    {

        private void DrawComposerPreview()
        {
            DeucarianThemeManagerSelection selection =
                DeucarianThemeManagerSelection.FromEditorPrefs();
            DeucarianThemeSpecimenRenderer.DrawThemePreview(
                selection.ResolvedTheme,
                null,
                composer.Surface,
                composer.Corners,
                composer.Border,
                composer.Size,
                composer.Typography);
        }

        private void ApplyComposerPreview()
        {
            DeucarianThemePreviewCoordinator.ApplyComposerPreview(
                DeucarianThemeManagerSelection.FromEditorPrefs(),
                composer.Source,
                composer.Surface,
                composer.Corners,
                composer.Border,
                composer.Size,
                composer.Typography);
        }

        private void BuildDeveloperToolsDrawer()
        {
            if (workspace?.Drawer == null) return;
            developerToolsOpen = false;
            developerToolsDrawer = DeucarianThemeDeveloperToolsView.Build(workspace.Drawer,
                () => SetDeveloperToolsOpen(false), CreateThemeFamily, RefreshAssets);
        }

    }
}
