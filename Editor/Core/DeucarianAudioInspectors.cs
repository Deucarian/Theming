using UnityEditor;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor
{
    [CustomEditor(typeof(DeucarianAudioRole))]
    public sealed class DeucarianAudioRoleEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var role = (DeucarianAudioRole)target;
            var view = new DeucarianThemingInspectorView("Audio role", serializedObject);
            view.Properties();
            view.OnRefresh(() => view.Warn(role.GetValidationWarning()));
            return view.Build();
        }
    }

    [CustomEditor(typeof(DeucarianAudioRoleLibrary))]
    public sealed class DeucarianAudioRoleLibraryEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var library = (DeucarianAudioRoleLibrary)target;
            var view = new DeucarianThemingInspectorView("Audio role library", serializedObject);
            view.Properties();
            view.OnRefresh(() => view.Warnings(library.GetValidationWarnings()));
            view.Action("Remove empty roles", () => library.RemoveNullRoles(), undo: "Remove Null Audio Roles");
            view.Action("Sort roles", library.SortRolesByCategoryAndName, undo: "Sort Audio Roles");
            return view.Build();
        }
    }

    [CustomEditor(typeof(DeucarianAudioPaletteSet))]
    public sealed class DeucarianAudioPaletteSetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var set = (DeucarianAudioPaletteSet)target;
            var view = new DeucarianThemingInspectorView("Audio palette set", serializedObject);
            view.Properties();
            view.OnRefresh(() => view.Warnings(set.GetValidationWarnings()));
            view.Action("Open audio palettes", () => DeucarianAudioPaletteLabWindow.Open(set));
            return view.Build();
        }
    }

    [CustomEditor(typeof(DeucarianAudioPalette))]
    public sealed class DeucarianAudioPaletteEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var palette = (DeucarianAudioPalette)target;
            var view = new DeucarianThemingInspectorView("Audio palette", serializedObject);
            view.Properties();
            view.OnRefresh(() => view.Warnings(palette.GetValidationWarnings()));
            view.Action("Add missing roles", () => palette.AddMissingRolesFromLibrary(),
                () => palette.RoleLibrary != null, "Add Missing Audio Roles");
            view.Action("Remove empty entries", () => palette.RemoveNullEntries(), undo: "Remove Null Audio Entries");
            view.Action("Sort entries", palette.SortEntriesByCategoryAndName, undo: "Sort Audio Palette Entries");
            return view.Build();
        }
    }
}
