using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    [CustomEditor(typeof(DeucarianColorRole))]
    public sealed class DeucarianColorRoleEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var role = (DeucarianColorRole)target;
            var view = new DeucarianThemingInspectorView("Color role", serializedObject);
            view.Properties(); view.OnRefresh(() => view.Warn(role.GetValidationWarning()));
            return view.Build();
        }
    }

    [CustomEditor(typeof(DeucarianColorRoleLibrary))]
    public sealed class DeucarianColorRoleLibraryEditor : UnityEditor.Editor
    {
        private readonly DeucarianThemingInspectorListFilterState rolesFilter = new DeucarianThemingInspectorListFilterState();
        public override VisualElement CreateInspectorGUI()
        {
            var library = (DeucarianColorRoleLibrary)target;
            var view = new DeucarianThemingInspectorView("Color role library", serializedObject);
            view.Properties("roles");
            var list = new DeucarianThemingInspectorList(serializedObject, "roles",
                DeucarianThemingInspectorListKind.ColorRoleLibraryRoles, rolesFilter, "Search roles", view.Refresh);
            view.Fields.Add(list.Root);
            view.OnRefresh(() =>
            {
                list.Refresh();
                var duplicateIds = library.GetDuplicateRoleIds();
                if (duplicateIds.Count > 0) view.Warn("Duplicate role IDs:\n" + string.Join("\n", duplicateIds), HelpBoxMessageType.Error);
                view.Warnings(library.GetValidationWarnings());
            });
            view.Action("Remove empty roles", () => library.RemoveNullRoles(), undo: "Remove Null Color Roles");
            view.Action("Sort roles", library.SortRolesByCategoryAndName, () => !rolesFilter.IsFiltering, "Sort Color Roles");
            return view.Build();
        }
    }

    [CustomEditor(typeof(DeucarianColorPalette))]
    public sealed class DeucarianColorPaletteEditor : UnityEditor.Editor
    {
        private readonly DeucarianThemingInspectorListFilterState entriesFilter = new DeucarianThemingInspectorListFilterState();
        public override VisualElement CreateInspectorGUI()
        {
            var palette = (DeucarianColorPalette)target;
            var view = new DeucarianThemingInspectorView("Color palette", serializedObject);
            view.Properties("entries");
            var list = new DeucarianThemingInspectorList(serializedObject, "entries",
                DeucarianThemingInspectorListKind.ColorPaletteEntries, entriesFilter, "Search entries", view.Refresh);
            view.Fields.Add(list.Root);
            view.OnRefresh(() => { list.Refresh(); view.Warnings(palette.GetValidationWarnings()); });
            view.Action("Add missing roles", () =>
            {
                int added = palette.AddMissingRolesFromLibrary();
                ThemingLog.Editor.Info($"Added {added} missing role entries to {palette.name}.", palette);
            }, () => palette.RoleLibrary != null && !entriesFilter.IsFiltering, "Add Missing Palette Roles");
            view.Action("Remove empty entries", () =>
            {
                int removed = palette.RemoveNullEntries();
                ThemingLog.Editor.Info($"Removed {removed} null entries from {palette.name}.", palette);
            }, undo: "Remove Null Palette Entries");
            view.Action("Sort entries", palette.SortEntriesByCategoryAndName, () => !entriesFilter.IsFiltering, "Sort Palette Entries");
            view.Action("Reset entry to role default", () => ShowResetEntryMenu(palette), () => palette.Entries.Count > 0);
            return view.Build();
        }

        private static void ShowResetEntryMenu(DeucarianColorPalette palette)
        {
            GenericMenu menu = new GenericMenu();
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                DeucarianColorRole role = entry != null ? entry.Role : null;

                if (role == null)
                {
                    menu.AddDisabledItem(new GUIContent($"Entry {i}: Missing Role"));
                    continue;
                }

                string label = $"{role.Category}/{role.DisplayName} ({role.Id})";
                menu.AddItem(new GUIContent(label), false, () => ResetEntry(palette, entry, role));
            }

            menu.ShowAsContext();
        }

        internal static bool ResetEntry(DeucarianColorPalette palette, DeucarianColorEntry entry, DeucarianColorRole role)
        {
            if (palette == null || entry == null || role == null || entry.Role != role) return false;
            for (int index = 0; index < palette.Entries.Count; index++)
            {
                if (!ReferenceEquals(palette.Entries[index], entry)) continue;
                Undo.RecordObject(palette, "Reset Palette Entry To Role Default");
                palette.ResetEntryToRoleDefault(index);
                EditorUtility.SetDirty(palette);
                return true;
            }
            return false;
        }

    }

    [CustomEditor(typeof(DeucarianThemePack))]
    [CanEditMultipleObjects]
    public sealed class DeucarianThemePackEditor : UnityEditor.Editor
    {
        private readonly DeucarianThemingInspectorListFilterState rolesFilter = new DeucarianThemingInspectorListFilterState();
        public override VisualElement CreateInspectorGUI()
        {
            var view = new DeucarianThemingInspectorView("Theme pack", serializedObject);
            if (UsesNativeUnfilteredInspector(serializedObject)) view.Properties();
            else
            {
                view.Properties("roles");
                view.Fields.Add(new DeucarianThemingInspectorList(serializedObject, "roles",
                    DeucarianThemingInspectorListKind.ThemePackRoles, rolesFilter, "Search roles").Root);
            }
            return view.Build();
        }
        internal static bool UsesNativeUnfilteredInspector(SerializedObject inspectedObject) =>
            inspectedObject != null && inspectedObject.isEditingMultipleObjects;
    }

    [CustomEditor(typeof(DeucarianTheme))]
    public sealed class DeucarianThemeEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var theme = (DeucarianTheme)target;
            var view = new DeucarianThemingInspectorView("Theme", serializedObject);
            view.Properties();
            view.OnRefresh(() => { if (theme.ColorPalette == null) view.Warn("Assign a color palette."); });
            view.Action("Select palette", () => DeucarianEditorSelection.SelectAndPing(theme.ColorPalette), () => theme.ColorPalette != null);
            return view.Build();
        }
    }

    [CustomEditor(typeof(DeucarianThemeFamily))]
    public sealed class DeucarianThemeFamilyEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var family = (DeucarianThemeFamily)target;
            var view = new DeucarianThemingInspectorView("Theme family", serializedObject);
            view.Properties();
            view.OnRefresh(() =>
            {
                if (string.IsNullOrWhiteSpace(family.FamilyId)) view.Warn("Assign a stable family ID.");
                if (string.IsNullOrWhiteSpace(family.DisplayName)) view.Warn("Assign a display name.");
                if (!family.IsComplete) view.Warn("Author both a light and a dark theme. Runtime falls back to the available variant.", HelpBoxMessageType.Error);
            });
            view.Action("Select light theme", () => DeucarianEditorSelection.SelectAndPing(family.LightTheme), () => family.LightTheme != null);
            view.Action("Select dark theme", () => DeucarianEditorSelection.SelectAndPing(family.DarkTheme), () => family.DarkTheme != null);
            view.Action("Repair theme family", () =>
            {
                var assets = DeucarianDefaultThemeAssetFactory.RepairThemeFamilySetup(family);
                DeucarianThemingEditorSettings.ActiveThemeFamily = assets.ThemeFamily;
            });
            return view.Build();
        }
    }

    [CustomEditor(typeof(DeucarianThemeProvider))]
    public sealed class DeucarianThemeProviderEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() =>
            DeucarianThemeSourceInspector.Build("Theme provider", serializedObject, "currentTheme", "currentThemeFamily");
    }

    [CustomEditor(typeof(DeucarianThemeRuntimeSettings))]
    public sealed class DeucarianThemeRuntimeSettingsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI() =>
            DeucarianThemeSourceInspector.Build("Runtime theme settings", serializedObject, "defaultTheme", "defaultThemeFamily");
    }

    internal static class DeucarianThemeSourceInspector
    {
        internal static VisualElement Build(string title, SerializedObject serialized, string themePath, string familyPath)
        {
            var view = new DeucarianThemingInspectorView(title, serialized); view.Properties();
            view.OnRefresh(() =>
            {
                var family = serialized.FindProperty(familyPath)?.objectReferenceValue as DeucarianThemeFamily;
                if (serialized.FindProperty(themePath)?.objectReferenceValue != null && family != null)
                    view.Warn("Choose a standalone theme or a theme family, not both.", HelpBoxMessageType.Error);
                if (family != null && !family.IsComplete)
                    view.Warn("The family is incomplete. Author both variants; runtime falls back to the available one.", HelpBoxMessageType.Error);
            });
            return view.Build();
        }
    }
}
