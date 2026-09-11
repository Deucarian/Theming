using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemingNativeInspectorTests
    {
        private sealed class Host : EditorWindow { }

        [Test] public void PaletteResetMenuTracksTheOriginalEntryAndRejectsStaleSelections()
        {
            var palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            var first = ScriptableObject.CreateInstance<DeucarianColorRole>();
            var second = ScriptableObject.CreateInstance<DeucarianColorRole>();
            try
            {
                first.Configure("first", "First", "Test", "", Color.red, false);
                second.Configure("second", "Second", "Test", "", Color.green, false);
                palette.SetColor(first, Color.blue); palette.SetColor(second, Color.black);
                var entries = (System.Collections.Generic.List<DeucarianColorEntry>)palette.Entries;
                var selected = entries[0];
                entries.Reverse();
                Assert.That(DeucarianColorPaletteEditor.ResetEntry(palette, selected, first), Is.True);
                Assert.That(selected.Color, Is.EqualTo(Color.red));
                Assert.That(entries[0].Color, Is.EqualTo(Color.black), "Reordering must not redirect a queued menu command.");
                selected.Configure(second, Color.blue);
                Assert.That(DeucarianColorPaletteEditor.ResetEntry(palette, selected, first), Is.False);
                entries.Remove(selected);
                Assert.That(DeucarianColorPaletteEditor.ResetEntry(palette, selected, second), Is.False);
                Assert.That(DeucarianColorPaletteEditor.ResetEntry(null, selected, first), Is.False);
            }
            finally { Object.DestroyImmediate(palette); Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }

        [UnityTest] public IEnumerator AssetInspectorsUseNativeControlsWithoutWritingOnOpen()
        {
            var types = new[] { typeof(DeucarianAudioRole), typeof(DeucarianAudioRoleLibrary), typeof(DeucarianAudioPalette),
                typeof(DeucarianAudioPaletteSet), typeof(DeucarianColorRole), typeof(DeucarianColorRoleLibrary),
                typeof(DeucarianColorPalette), typeof(DeucarianThemePack), typeof(DeucarianTheme), typeof(DeucarianThemeFamily),
                typeof(DeucarianThemeRuntimeSettings), typeof(DeucarianThemeStyle), typeof(DeucarianThemeSurfaceProfile),
                typeof(DeucarianThemeShapeProfile), typeof(DeucarianThemeStrokeProfile), typeof(DeucarianThemeTypographyProfile) };
            foreach (var type in types)
            {
                var asset = ScriptableObject.CreateInstance(type);
                var inspector = UnityEditor.Editor.CreateEditor(asset);
                var host = ScriptableObject.CreateInstance<Host>(); host.position = new Rect(100, 100, 500, 800); host.Show();
                try
                {
                    string before = JsonUtility.ToJson(asset);
                    var root = inspector.CreateInspectorGUI(); host.rootVisualElement.Add(root);
                    for (int frame = 0; frame < 8; frame++) yield return null;
                    Assert.That(root.ClassListContains("dw-inspector"), Is.True, type.Name);
                    Assert.That(root.Q<IMGUIContainer>(), Is.Null, type.Name + " retains an old inspector renderer");
                    Assert.That(JsonUtility.ToJson(asset), Is.EqualTo(before), type.Name + " writes while opening");
                }
                finally { host.Close(); Object.DestroyImmediate(inspector); Object.DestroyImmediate(asset); }
            }
        }

        [UnityTest] public IEnumerator NativeListSearchPreservesOriginalIndicesAndNeverResizesTheSource()
        {
            var library = ScriptableObject.CreateInstance<DeucarianColorRoleLibrary>();
            var role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            using var serialized = new SerializedObject(library);
            var roles = serialized.FindProperty("roles"); roles.arraySize = 3;
            roles.GetArrayElementAtIndex(0).objectReferenceValue = null;
            roles.GetArrayElementAtIndex(1).objectReferenceValue = role;
            roles.GetArrayElementAtIndex(2).objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var state = new DeucarianThemingInspectorListFilterState();
            var host = ScriptableObject.CreateInstance<Host>(); host.Show();
            try
            {
                var root = Deucarian.Editor.DeucarianEditorInspector.CreateToolkit(); host.rootVisualElement.Add(root);
                var list = new DeucarianThemingInspectorList(serialized, "roles", DeucarianThemingInspectorListKind.ColorRoleLibraryRoles, state, "Search roles");
                root.Add(list.Root);
                for (int frame = 0; frame < 8; frame++) yield return null;
                root.Q<TextField>("search-roles").value = "missing";
                for (int frame = 0; frame < 4; frame++) yield return null;
                Assert.That(state.VisibleIndices, Is.EqualTo(new[] { 0, 2 }));
                Assert.That(serialized.FindProperty("roles").arraySize, Is.EqualTo(3));
                Assert.That(serialized.FindProperty("roles").GetArrayElementAtIndex(1).objectReferenceValue, Is.SameAs(role));
                Assert.That(root.Q<IMGUIContainer>(), Is.Null);
                root.Q<TextField>("search-roles").value = string.Empty;
                for (int frame = 0; frame < 4; frame++) yield return null;
                Assert.That(state.VisibleIndices, Is.EqualTo(new[] { 0, 1, 2 }));
            }
            finally { host.Close(); Object.DestroyImmediate(library); Object.DestroyImmediate(role); }
        }
    }
}
