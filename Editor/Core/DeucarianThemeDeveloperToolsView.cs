using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemeDeveloperToolsView
    {
        internal static DeucarianEditorWorkbenchDrawer Build(VisualElement container,
            Action close, Action createFamily, Action refresh)
        {
            container.Clear();
            var developerToolsDrawer = DeucarianEditorWorkbenchSurfaces.CreateDrawer(false);
            developerToolsDrawer.Root.name = "deucarian-theme-manager-developer-tools";

            VisualElement header = DeucarianEditorWorkbenchSurfaces.CreateDrawerHeader(
                "Developer Tools");
            header.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                DeucarianEditorIconIds.OpenFolder,
                "Open assets folder",
                DeucarianThemingMenuActions.OpenThemeAssetsFolder,
                "Reveal the Deucarian theme assets folder in the Project window."));
            header.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                DeucarianEditorIconIds.ChevronDown,
                "Close",
                close,
                "Close Developer Tools."));
            developerToolsDrawer.Content.Add(header);

            VisualElement columns = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumns();
            VisualElement create = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Create");
            AddDeveloperToolAction(refresh,
                create,
                DeucarianEditorIconIds.CreateFolder,
                "Theme family...",
                createFamily,
                "Opens a save dialog and creates a theme family with its palette and style references at the chosen project location.");
            AddDeveloperToolAction(refresh,
                create,
                DeucarianEditorIconIds.CreatePackage,
                "Starter assets",
                () => DeucarianThemingMenuActions.CreateMissingDefaultThemeAssets(),
                "Creates any missing default Deucarian theme assets and repairs their built-in references when needed.");
            AddDeveloperToolAction(refresh,
                create,
                DeucarianEditorIconIds.Palette,
                "Built-in theme styles",
                () => DeucarianThemingMenuActions.CreateBuiltinThemeStyleAssets(),
                "Creates or repairs the package's built-in visual-style assets in the default theming folder.");
            AddDeveloperToolAction(refresh,
                create,
                DeucarianEditorIconIds.Monitor,
                "UI Toolkit demo assets",
                () => DeucarianUIToolkitDemoAssetFactory.CreateDemoAssets(),
                "Creates or updates the UI Toolkit demo assets under the project's Deucarian theming folder.");

            VisualElement repair = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Repair");
            AddDeveloperToolAction(refresh,
                repair,
                DeucarianEditorIconIds.Wrench,
                "Selected theme family",
                () => DeucarianThemingMenuActions.RepairActiveThemeFamilySetup(),
                "Repairs missing built-in references on the currently selected theme family and its related assets.");
            AddDeveloperToolAction(refresh,
                repair,
                DeucarianEditorIconIds.Refresh,
                "Selected palette",
                () => DeucarianThemingMenuActions.RepairActivePaletteSetup(),
                "Repairs the active palette's built-in theme and visual-style setup.");

            VisualElement legacy = DeucarianEditorWorkbenchSurfaces.CreateDrawerColumn("Legacy");
            legacy.style.marginRight = 0f;
            AddDeveloperToolAction(refresh,
                legacy,
                DeucarianEditorIconIds.History,
                "Create minimal palette...",
                () => DeucarianThemingMenuActions.CreateMinimalPaletteFromSavePanel(),
                "Opens a save dialog and creates a minimal legacy palette asset at the chosen project location.");

            columns.Add(create);
            columns.Add(repair);
            columns.Add(legacy);
            developerToolsDrawer.Content.Add(columns);
            container.Add(developerToolsDrawer.Root);
            return developerToolsDrawer;
        }

        private static void AddDeveloperToolAction(Action refresh,
            VisualElement column,
            string iconId,
            string text,
            Action action,
            string confirmationDescription)
        {
            column?.Add(DeucarianEditorWorkbenchSurfaces.CreateDrawerAction(
                iconId,
                text,
                () =>
                {
                    if (!DeucarianThemeEditorConfirmations.TryExecuteDeveloperToolAction(
                            text,
                            confirmationDescription,
                            action))
                    {
                        return;
                    }

                    refresh();
                },
                confirmationDescription));
        }
    }
}
