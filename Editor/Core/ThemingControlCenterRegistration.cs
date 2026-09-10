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
        private static readonly IDisposable ProjectRegistration;

        static ThemingControlCenterRegistration()
        {
            ProjectRegistration = DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                DeucarianThemingProjectPage.ToolId, "Project setup", "Choose visual styling and audio for this project.",
                DeucarianControlCenterArea.Experience, OpenProjectSetup, PackageId,
                iconKey: DeucarianEditorIconIds.Palette, searchTerms: new[] { "theming", "audio", "visual", "enable", "disable" },
                order: 129, createPage: DeucarianThemingProjectPage.Create, navigationPath: "Theming",
                navigationGroupIcon: DeucarianEditorIconIds.Palette, showNavigationIcon: false));
            ToolRegistration = DeucarianToolRegistry.Register(
                new DeucarianToolDescriptor(
                    DeucarianToolIds.ThemeManager,
                    "Theme Manager",
                    "Create, inspect, and activate project theme families.",
                    DeucarianControlCenterArea.Experience,
                    DeucarianThemingMenu.OpenThemeManager,
                    PackageId,
                    searchTerms: new[] { "theme", "palette", "style", "colors" },
                    order: 130, createPage: DeucarianThemeManagerWindow.CreatePage, navigationPath: "Theming",
                    navigationGroupIcon: DeucarianEditorIconIds.Palette, navigationLabel: "Visual palettes", showNavigationIcon: false));

            AudioRegistration = DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                DeucarianEditorWorkspaceNavigation.AudioToolId, "Audio Palette Lab",
                "Audition project audio by semantic role and experience.", DeucarianControlCenterArea.Experience,
                DeucarianAudioPaletteLabWindow.OpenWindow, PackageId,
                searchTerms: new[] { "audio", "sound", "preview", "palette" }, order: 135, createPage: DeucarianAudioPaletteLabWindow.CreatePage, navigationPath: "Theming",
                navigationGroupIcon: DeucarianEditorIconIds.Palette, navigationLabel: "Audio palettes", showNavigationIcon: false));

            CardRegistration = DeucarianControlCenterRegistry.RegisterCardProvider(
                new ThemingCardProvider());
        }

        private static void OpenProjectSetup() => DeucarianEditorToolWindow.Open(DeucarianThemingProjectPage.ToolId);

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
                var settings = DeucarianThemeRuntimeResolver.LoadSettings();
                bool visualEnabled = settings == null || settings.UseVisualStyling;
                bool audioEnabled = settings == null || settings.UseAudio;

                return new[]
                {
                    new DeucarianControlCenterCard(
                        PackageId + ".active-theme",
                        DeucarianControlCenterArea.Experience,
                        "Theming",
                        "Visual styling and audio, independently configured.",
                        PackageId,
                        configured || !visualEnabled
                            ? DeucarianControlCenterStatus.Success
                            : DeucarianControlCenterStatus.Warning,
                        "Visual " + (visualEnabled ? "on" : "off") + " · Audio " + (audioEnabled ? "on" : "off"),
                        order: 130,
                        details: new[]
                        {
                            !visualEnabled ? "Existing app styling stays in charge."
                                : configured ? "Mode: " + DeucarianThemingEditorSettings.ActiveThemeMode
                                : "Select a visual theme, or turn visual styling off in Project setup."
                        },
                        actions: new[]
                        {
                            new DeucarianControlCenterAction(
                                PackageId + ".open",
                                "Project setup",
                                OpenProjectSetup, navigationToolId: DeucarianThemingProjectPage.ToolId),
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
