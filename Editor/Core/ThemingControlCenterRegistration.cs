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
                    order: 130));

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
                                DeucarianThemingMenu.OpenThemeManager),
                            new DeucarianControlCenterAction(
                                PackageId + ".open-audio-palette-lab",
                                "Open Audio Palette Lab",
                                DeucarianThemingMenu.OpenAudioPaletteLab),
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
