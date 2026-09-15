using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ui = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.Theming.Editor
{
    internal sealed class DeucarianThemePaletteForm
    {
        private readonly DeucarianColorPalette palette;
        private readonly DeucarianEditorWorkspaceForm form;
        private static readonly string[] PrimaryRoles = { DeucarianBuiltinColorRoleIds.Background,
            DeucarianBuiltinColorRoleIds.Surface, DeucarianBuiltinColorRoleIds.Primary,
            DeucarianBuiltinColorRoleIds.TextPrimary, DeucarianBuiltinColorRoleIds.Success };

        internal DeucarianThemePaletteForm(VisualElement root, DeucarianColorPalette palette, Action changed)
        {
            this.palette = palette;
            form = new DeucarianEditorWorkspaceForm(root);
            if (palette == null) { root.Add(Ui.Label("Choose a theme family to edit its colors.", "dw-muted")); return; }
            var roles = new List<DeucarianColorRole>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (palette.RoleLibrary != null)
                foreach (var role in palette.RoleLibrary.Roles) if (role != null && seen.Add(role.Id)) roles.Add(role);
            foreach (var entry in palette.Entries)
                if (entry?.Role != null && seen.Add(entry.Role.Id)) roles.Add(entry.Role);
            var primary = new HashSet<string>(PrimaryRoles);
            foreach (string id in PrimaryRoles)
            {
                var role = roles.Find(value => value.Id == id);
                if (role != null) Add(form, role, changed);
            }
            var more = form.Section("More colors", true);
            foreach (var role in roles) if (!primary.Contains(role.Id)) Add(more, role, changed);
            if (!IsProjectOwned(palette)) root.Add(Ui.Label("Package palette · read only. Create a project theme family to customize it.", "dw-muted"));
            Refresh();
        }

        private void Add(DeucarianEditorWorkspaceForm target, DeucarianColorRole role, Action changed)
        {
            string caption = role.Id == DeucarianBuiltinColorRoleIds.TextPrimary ? "Text" : role.DisplayName;
            var input = target.Color("theme-color-" + role.Id, caption, () => palette.GetColor(role), color =>
            {
                if (!IsProjectOwned(palette) || !DeucarianThemeRuntimeResolver.UseVisualStyling) return;
                string note = string.Empty;
                foreach (var entry in palette.Entries) if (entry?.Role?.Id == role.Id) { note = entry.Note; break; }
                Undo.RecordObject(palette, "Edit theme color");
                palette.SetColor(role, color, note);
                EditorUtility.SetDirty(palette);
                changed();
            });
            input.SetEnabled(IsProjectOwned(palette));
        }

        internal void Refresh() => form.Refresh();
        internal static bool IsProjectOwned(UnityEngine.Object asset) => asset != null &&
            AssetDatabase.GetAssetPath(asset).Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal);
    }
}
