using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.Theming.Editor
{
    [InitializeOnLoad]
    internal static class ThemingControlCenterRegistration
    {
        private const string PackageId = "com.deucarian.theming";
        private static readonly IDisposable ToolRegistration;
        private static readonly IDisposable CardRegistration;
        private static readonly IDisposable AudioRegistration;

        static ThemingControlCenterRegistration()
        {
            ToolRegistration = DeucarianToolRegistry.Register(
                new DeucarianToolDescriptor(
                    DeucarianToolIds.ThemeManager,
                    "Theme Manager",
                    "Create, inspect, and activate project theme families.",
                    DeucarianControlCenterArea.Experience,
                    DeucarianThemingMenu.OpenThemeManager,
                    PackageId,
                    searchTerms: new[] { "theme", "palette", "style", "colors" },
                    order: 130, createPage: DeucarianThemeManagerWindow.CreatePage, navigationPath: "Appearance"));

            AudioRegistration = DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                DeucarianEditorWorkspaceNavigation.AudioToolId, "Audio Palette Lab",
                "Audition project audio by semantic role and experience.", DeucarianControlCenterArea.Experience,
                DeucarianAudioPaletteLabWindow.OpenWindow, PackageId,
                searchTerms: new[] { "audio", "sound", "preview", "palette" }, order: 135, createPage: DeucarianAudioPaletteLabWindow.CreatePage, navigationPath: "Audio"));

            CardRegistration = DeucarianControlCenterRegistry.RegisterCardProvider(
                new ThemingCardProvider());
        }

        private sealed class ThemingCardProvider :
            IDeucarianControlCenterCardProvider
        {
            public string Id => PackageId + ".control-center";

            public IEnumerable<DeucarianControlCenterCard> Capture(
                DeucarianControlCenterContext context)
            {
                DeucarianTheme activeTheme =
                    DeucarianThemingEditorSettings.ActiveTheme;
                bool configured = activeTheme != null;

                return new[]
                {
                    new DeucarianControlCenterCard(
                        PackageId + ".active-theme",
                        DeucarianControlCenterArea.Experience,
                        "Theming",
                        "Project-local active theme selection and authoring workflow.",
                        PackageId,
                        configured
                            ? DeucarianControlCenterStatus.Success
                            : DeucarianControlCenterStatus.Warning,
                        configured ? "Active theme selected" : "No active theme",
                        order: 130,
                        details: new[]
                        {
                            configured
                                ? "Mode: " + DeucarianThemingEditorSettings.ActiveThemeMode
                                : "Create or select a theme family to begin."
                        },
                        actions: new[]
                        {
                            new DeucarianControlCenterAction(
                                PackageId + ".open",
                                "Open Theme Manager",
                                DeucarianThemingMenu.OpenThemeManager, navigationToolId: DeucarianToolIds.ThemeManager),
                            new DeucarianControlCenterAction(
                                PackageId + ".open-audio-palette-lab",
                                "Open Audio Palette Lab",
                                DeucarianThemingMenu.OpenAudioPaletteLab, navigationToolId: DeucarianEditorWorkspaceNavigation.AudioToolId),
                            new DeucarianControlCenterAction(
                                PackageId + ".create-family",
                                "Create Theme Family",
                                DeucarianThemingMenu.CreateThemeFamily)
                        },
                        searchTerms: new[] { "theme", "palette", "style", "appearance" })
                };
            }
        }
    }
}
