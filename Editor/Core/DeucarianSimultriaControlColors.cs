using System.Collections.Generic;
using UnityEngine;
using BuiltinRoleDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.BuiltinRoleDefinition;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianSimultriaControlColors
    {
        internal static void Add(List<BuiltinRoleDefinition> roles, Dictionary<string, Color> colors, bool holoCompatible)
        {
            roles.Add(Role(DeucarianControlColorRoleIds.SocketGhost, "Socket Ghost",
                holoCompatible ? new Color(1f, 1f, 1f, 0.2f) : colors["deucarian.text.primary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.TitleText, "Title Text",
                holoCompatible ? new Color(0.7686275f, 0.6313726f, 0.97647065f, 1f) : colors["deucarian.primary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.InputText, "Input Text",
                holoCompatible ? new Color(0.7529412f, 0.7529412f, 0.7529412f, 1f) : colors["deucarian.text.primary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.PlaceholderText, "Placeholder Text",
                holoCompatible ? new Color(0.7529412f, 0.7529412f, 0.7529412f, 0.47058824f) : colors["deucarian.text.disabled"]));
            roles.Add(Role(DeucarianControlColorRoleIds.Icon, "Icon",
                holoCompatible ? new Color(1f, 1f, 1f, 1f) : colors["deucarian.text.primary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.Image, "Image",
                holoCompatible ? new Color(1f, 1f, 1f, 1f) : colors["deucarian.text.primary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.ImageMuted, "Image Muted",
                holoCompatible ? new Color(0.7490196f, 0.7490196f, 0.7490196f, 1f) : colors["deucarian.text.muted"]));
            roles.Add(Role(DeucarianControlColorRoleIds.ImageSubtle, "Image Subtle",
                holoCompatible ? new Color(1f, 1f, 1f, 0.15280001f) : colors["deucarian.surface.raised"]));
            roles.Add(Role(DeucarianControlColorRoleIds.SliderTrack, "Slider Track",
                holoCompatible ? new Color(0.40000004f, 0.40000004f, 0.40000004f, 1f) : colors["deucarian.secondary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.Outline, "Outline",
                holoCompatible ? new Color(1f, 1f, 1f, 1f) : colors["deucarian.text.secondary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.ErrorText, "Error Text",
                holoCompatible ? new Color(1f, 0.458f, 0.458f, 1f) : colors["deucarian.error"]));
            roles.Add(Role(DeucarianControlColorRoleIds.KeyboardAccent, "Keyboard Accent",
                holoCompatible ? new Color(0.125f, 0.588f, 0.953f, 1f) : colors["deucarian.accent"]));
            roles.Add(Role(DeucarianControlColorRoleIds.KeyboardBackground, "Keyboard Background",
                holoCompatible ? new Color(0.133f, 0.133f, 0.133f, 1f) : colors["deucarian.surface.raised"]));
            roles.Add(Role(DeucarianControlColorRoleIds.KeyboardOutline, "Keyboard Outline",
                holoCompatible ? new Color(0f, 0.6f, 1f, 1f) : colors["deucarian.primary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.KeyboardInputText, "Keyboard Input Text",
                holoCompatible ? new Color(0.588f, 0.588f, 0.588f, 1f) : colors["deucarian.text.primary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.ControlSubtleBackground, "Control Subtle Background",
                holoCompatible ? new Color(1f, 1f, 1f, 0.051f) : colors["deucarian.surface"]));
            roles.Add(Role(DeucarianControlColorRoleIds.ControlDarkBorder, "Control Dark Border",
                holoCompatible ? new Color(0f, 0f, 0f, 0.475f) : colors["deucarian.secondary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.SliderHandle, "Slider Handle",
                holoCompatible ? new Color(0.388f, 0.259f, 0.588f, 1f) : colors["deucarian.primary"]));
            roles.Add(Role(DeucarianControlColorRoleIds.LoadingIndicator, "Loading Indicator",
                holoCompatible ? new Color(0.472f, 0.472f, 0.472f, 1f) : colors["deucarian.accent"]));
            roles.Add(Role(DeucarianControlColorRoleIds.DropdownInvalidState, "Dropdown Invalid State",
                holoCompatible ? new Color(1f, 0f, 0f, 1f) : colors["deucarian.error"]));
            roles.Add(Role(DeucarianControlColorRoleIds.KeyboardContentAccent, "Keyboard Content Accent",
                holoCompatible ? new Color(0.098f, 1f, 0f, 1f) : colors["deucarian.accent"]));
            roles.Add(Role(DeucarianControlColorRoleIds.Transparent, "Transparent",
                holoCompatible ? new Color(1f, 1f, 1f, 0f) : new Color(1, 1, 1, 0)));
        }

        private static BuiltinRoleDefinition Role(string id, string label, Color color) =>
            new BuiltinRoleDefinition(id, label, DeucarianColorRoleCategories.UiState,
                "Control-specific override; falls back to the palette's standard semantic roles.", color);
    }
}
