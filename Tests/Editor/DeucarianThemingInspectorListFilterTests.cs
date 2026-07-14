using System;
using System.Collections.Generic;
using System.Linq;
using Deucarian.Theming.UIToolkit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class DeucarianThemingInspectorListFilterTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void RoleLibraryFilterSearchesRoleMetadataAndExposesSpecialCategories()
        {
            DeucarianColorRole surface = CreateRole(
                "deucarian.report.canvas",
                "Raised Canvas",
                "Surface",
                "Behind report panels",
                "Canvas Role Asset");
            DeucarianColorRole uncategorized = CreateRole(
                "deucarian.report.annotation",
                "Annotation Ink",
                "   ",
                "Used for markup notes",
                "Annotation Role Asset");
            DeucarianColorRole authoredMissing = CreateRole(
                "deucarian.report.authored-missing",
                "Authored Missing Category",
                "Missing",
                "A real category whose name matches the missing-reference label",
                "Authored Missing Role Asset");
            DeucarianColorRoleLibrary library = CreateObject<DeucarianColorRoleLibrary>();
            library.AddRole(surface);
            library.AddRole(uncategorized);
            library.AddRole(authoredMissing);

            SerializedObject serializedLibrary = new SerializedObject(library);
            SerializedProperty roles = serializedLibrary.FindProperty("roles");
            roles.arraySize = 4;
            roles.GetArrayElementAtIndex(3).objectReferenceValue = null;
            serializedLibrary.ApplyModifiedProperties();
            serializedLibrary.Update();

            AssertFilterResultAndNoMutation(
                library,
                roles,
                DeucarianThemingInspectorListKind.ColorRoleLibraryRoles,
                "cAnVaS behind",
                DeucarianThemingInspectorListFilter.AllCategories,
                0);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ColorRoleLibraryRoles, "RAISED surface", null, 0);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ColorRoleLibraryRoles, "deucarian.report.canvas", null, 0);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ColorRoleLibraryRoles, "annotation markup", null, 1);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ColorRoleLibraryRoles, "missing role", null, 2, 3);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ColorRoleLibraryRoles, "   ", null, 0, 1, 2, 3);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ColorRoleLibraryRoles, "not-present", null);

            IReadOnlyList<string> categories = DeucarianThemingInspectorListFilter.GetCategories(
                roles,
                DeucarianThemingInspectorListKind.ColorRoleLibraryRoles);
            CollectionAssert.AreEqual(
                new[]
                {
                    DeucarianThemingInspectorListFilter.AllCategories,
                    "Missing",
                    "Surface",
                    DeucarianThemingInspectorListFilter.UncategorizedCategory,
                    DeucarianThemingInspectorListFilter.MissingCategory
                },
                categories);
            AssertIndices(
                roles,
                DeucarianThemingInspectorListKind.ColorRoleLibraryRoles,
                string.Empty,
                DeucarianThemingInspectorListFilter.UncategorizedCategory,
                1);
            AssertIndices(
                roles,
                DeucarianThemingInspectorListKind.ColorRoleLibraryRoles,
                string.Empty,
                DeucarianThemingInspectorListFilter.MissingCategory,
                3);
            AssertIndices(
                roles,
                DeucarianThemingInspectorListKind.ColorRoleLibraryRoles,
                string.Empty,
                "Missing",
                2);
        }

        [Test]
        public void PaletteFilterSearchesNotesAndRoleMetadataWithoutChangingOriginalOrder()
        {
            DeucarianColorRole raised = CreateRole(
                "deucarian.report.surface.raised",
                "Raised Surface",
                "Surface",
                "Report panel background",
                "Raised Surface Role");
            DeucarianColorRole warning = CreateRole(
                "deucarian.report.status.warning",
                "Warning Text",
                "Status",
                "Calls out unsafe values",
                "Warning Role");
            DeucarianColorRole baseSurface = CreateRole(
                "deucarian.report.surface.base",
                "Base Surface",
                "Surface",
                "Report viewer foundation",
                "Base Surface Role");
            DeucarianColorPalette palette = CreateObject<DeucarianColorPalette>();
            palette.AddEntry(raised, Color.gray, "Viewer shell midnight treatment");
            palette.AddEntry(warning, Color.yellow, "Alert foreground");
            palette.AddEntry(baseSurface, Color.black, "Secondary report layout");

            SerializedObject serializedPalette = new SerializedObject(palette);
            SerializedProperty entries = serializedPalette.FindProperty("entries");

            AssertFilterResultAndNoMutation(
                palette,
                entries,
                DeucarianThemingInspectorListKind.ColorPaletteEntries,
                "VIEWER midnight",
                DeucarianThemingInspectorListFilter.AllCategories,
                0);
            AssertIndices(entries, DeucarianThemingInspectorListKind.ColorPaletteEntries, "raised background", null, 0);
            AssertIndices(entries, DeucarianThemingInspectorListKind.ColorPaletteEntries, "report surface", null, 0, 2);
            AssertIndices(entries, DeucarianThemingInspectorListKind.ColorPaletteEntries, "secondary layout", null, 2);
            AssertIndices(entries, DeucarianThemingInspectorListKind.ColorPaletteEntries, "\t \r\n", null, 0, 1, 2);
            AssertIndices(entries, DeucarianThemingInspectorListKind.ColorPaletteEntries, "no palette match", null);
            AssertIndices(entries, DeucarianThemingInspectorListKind.ColorPaletteEntries, string.Empty, "surface", 0, 2);

            CollectionAssert.AreEqual(new[] { raised, warning, baseSurface }, palette.Entries.Select(entry => entry.Role));
            CollectionAssert.AreEqual(
                new[] { "Viewer shell midnight treatment", "Alert foreground", "Secondary report layout" },
                palette.Entries.Select(entry => entry.Note));
        }

        [Test]
        public void ThemePackFilterSearchesEveryInlineRoleFieldAndUncategorizedRoles()
        {
            DeucarianThemePackRole canvas = new DeucarianThemePackRole(
                "Canvas Role Asset",
                "deucarian.report.canvas",
                "Report Canvas",
                "Surface",
                "Behind all report cards",
                Color.white,
                true);
            DeucarianThemePackRole annotation = new DeucarianThemePackRole(
                "Annotation Ink Asset",
                "deucarian.report.annotation",
                "Markup Annotation",
                " ",
                "Notes drawn over the model",
                Color.cyan,
                false);
            DeucarianThemePack pack = CreateObject<DeucarianThemePack>();
            pack.SetRoles(new[] { canvas, annotation });

            SerializedObject serializedPack = new SerializedObject(pack);
            SerializedProperty roles = serializedPack.FindProperty("roles");

            AssertFilterResultAndNoMutation(
                pack,
                roles,
                DeucarianThemingInspectorListKind.ThemePackRoles,
                "CANVAS cards",
                DeucarianThemingInspectorListFilter.AllCategories,
                0);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ThemePackRoles, "deucarian.report.canvas surface", null, 0);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ThemePackRoles, "report canvas", null, 0);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ThemePackRoles, "annotation model", null, 1);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ThemePackRoles, string.Empty, null, 0, 1);
            AssertIndices(roles, DeucarianThemingInspectorListKind.ThemePackRoles, "no pack match", null);
            AssertIndices(
                roles,
                DeucarianThemingInspectorListKind.ThemePackRoles,
                string.Empty,
                DeucarianThemingInspectorListFilter.UncategorizedCategory,
                1);

            IReadOnlyList<string> categories = DeucarianThemingInspectorListFilter.GetCategories(
                roles,
                DeucarianThemingInspectorListKind.ThemePackRoles);
            CollectionAssert.AreEqual(
                new[]
                {
                    DeucarianThemingInspectorListFilter.AllCategories,
                    "Surface",
                    DeucarianThemingInspectorListFilter.UncategorizedCategory
                },
                categories);
            CollectionAssert.AreEqual(
                new[] { "deucarian.report.canvas", "deucarian.report.annotation" },
                pack.Roles.Select(role => role.Id));
        }

        [Test]
        public void UIToolkitBindingFilterSearchesTargetsEnumsVariablesAndRoleMetadata()
        {
            DeucarianColorRole surface = CreateRole(
                "deucarian.report.surface",
                "Report Surface",
                "Surface",
                "Card chrome color",
                "Report Surface Role");
            DeucarianColorRole status = CreateRole(
                "deucarian.report.status",
                "Status Copy",
                "Status",
                "Health status label",
                "Status Role");
            DeucarianUIToolkitThemeApplier applier = CreateInactiveComponent<DeucarianUIToolkitThemeApplier>();
            SerializedObject serializedApplier = new SerializedObject(applier);
            SerializedProperty bindings = serializedApplier.FindProperty("bindings");
            bindings.arraySize = 3;
            ConfigureBinding(
                bindings.GetArrayElementAtIndex(0),
                surface,
                DeucarianUIToolkitStyleProperty.CustomCssVariable,
                ".report-card",
                "summary-panel",
                "viewer-shell",
                "--report-card-accent");
            ConfigureBinding(
                bindings.GetArrayElementAtIndex(1),
                status,
                DeucarianUIToolkitStyleProperty.TextColor,
                "#alert-label",
                "health-status",
                "status-copy",
                string.Empty);
            ConfigureBinding(
                bindings.GetArrayElementAtIndex(2),
                null,
                DeucarianUIToolkitStyleProperty.BorderColor,
                ".missing-role",
                string.Empty,
                string.Empty,
                string.Empty);
            serializedApplier.ApplyModifiedProperties();
            serializedApplier.Update();

            AssertFilterResultAndNoMutation(
                applier,
                bindings,
                DeucarianThemingInspectorListKind.UIToolkitBindings,
                "REPORT chrome",
                DeucarianThemingInspectorListFilter.AllCategories,
                0);
            AssertIndices(bindings, DeucarianThemingInspectorListKind.UIToolkitBindings, "summary viewer-shell", null, 0);
            AssertIndices(bindings, DeucarianThemingInspectorListKind.UIToolkitBindings, ".report-card", null, 0);
            AssertIndices(bindings, DeucarianThemingInspectorListKind.UIToolkitBindings, "CUSTOM variable", null, 0);
            AssertIndices(bindings, DeucarianThemingInspectorListKind.UIToolkitBindings, "report-card-accent", null, 0);
            AssertIndices(bindings, DeucarianThemingInspectorListKind.UIToolkitBindings, "text color health", null, 1);
            AssertIndices(bindings, DeucarianThemingInspectorListKind.UIToolkitBindings, string.Empty, null, 0, 1, 2);
            AssertIndices(bindings, DeucarianThemingInspectorListKind.UIToolkitBindings, "no binding match", null);
            AssertIndices(
                bindings,
                DeucarianThemingInspectorListKind.UIToolkitBindings,
                string.Empty,
                DeucarianThemingInspectorListFilter.MissingCategory,
                2);

            CollectionAssert.Contains(
                DeucarianThemingInspectorListFilter.GetCategories(
                    bindings,
                    DeucarianThemingInspectorListKind.UIToolkitBindings),
                DeucarianThemingInspectorListFilter.MissingCategory);
            Assert.AreEqual(".report-card", applier.Bindings[0].UssSelector);
            Assert.AreEqual("#alert-label", applier.Bindings[1].UssSelector);
            Assert.IsNull(applier.Bindings[2].ColorRole);
        }

        [Test]
        public void UIToolkitVariableMappingFilterSearchesVariableAndRoleMetadata()
        {
            DeucarianColorRole surface = CreateRole(
                "deucarian.report.surface.raised",
                "Raised Surface",
                "Surface",
                "Elevated report cards",
                "Raised Surface Role");
            DeucarianUIToolkitThemeVariables variables = CreateInactiveComponent<DeucarianUIToolkitThemeVariables>();
            SerializedObject serializedVariables = new SerializedObject(variables);
            SerializedProperty mappings = serializedVariables.FindProperty("explicitVariableMappings");
            mappings.arraySize = 2;
            ConfigureMapping(mappings.GetArrayElementAtIndex(0), surface, "--viewer-surface-raised");
            ConfigureMapping(mappings.GetArrayElementAtIndex(1), null, "--viewer-missing-role");
            serializedVariables.ApplyModifiedProperties();
            serializedVariables.Update();

            AssertFilterResultAndNoMutation(
                variables,
                mappings,
                DeucarianThemingInspectorListKind.UIToolkitVariableMappings,
                "VIEWER raised",
                DeucarianThemingInspectorListFilter.AllCategories,
                0);
            AssertIndices(mappings, DeucarianThemingInspectorListKind.UIToolkitVariableMappings, "elevated surface", null, 0);
            AssertIndices(mappings, DeucarianThemingInspectorListKind.UIToolkitVariableMappings, "missing role", null, 1);
            AssertIndices(mappings, DeucarianThemingInspectorListKind.UIToolkitVariableMappings, " ", null, 0, 1);
            AssertIndices(mappings, DeucarianThemingInspectorListKind.UIToolkitVariableMappings, "no variable match", null);
            AssertIndices(
                mappings,
                DeucarianThemingInspectorListKind.UIToolkitVariableMappings,
                string.Empty,
                DeucarianThemingInspectorListFilter.MissingCategory,
                1);

            CollectionAssert.Contains(
                DeucarianThemingInspectorListFilter.GetCategories(
                    mappings,
                    DeucarianThemingInspectorListKind.UIToolkitVariableMappings),
                DeucarianThemingInspectorListFilter.MissingCategory);
            Assert.AreEqual("--viewer-surface-raised", mappings.GetArrayElementAtIndex(0)
                .FindPropertyRelative("variableName").stringValue);
            Assert.AreEqual("--viewer-missing-role", mappings.GetArrayElementAtIndex(1)
                .FindPropertyRelative("variableName").stringValue);
        }

        [Test]
        public void FilteredObjectReferenceDeletionPreservesOriginalIndexAndUndoRestoresIt()
        {
            DeucarianColorRole first = CreateRole("deucarian.first", "First", "Surface", string.Empty, "First");
            DeucarianColorRole remove = CreateRole(
                "deucarian.remove",
                "Delete Target",
                "Surface",
                string.Empty,
                "Remove");
            DeucarianColorRole last = CreateRole("deucarian.last", "Last", "Surface", string.Empty, "Last");
            DeucarianColorRoleLibrary library = CreateObject<DeucarianColorRoleLibrary>();
            library.AddRole(first);
            library.AddRole(remove);
            library.AddRole(last);
            SerializedObject serializedLibrary = new SerializedObject(library);
            SerializedProperty roles = serializedLibrary.FindProperty("roles");
            List<int> filtered = DeucarianThemingInspectorListFilter.GetVisibleIndices(
                roles,
                DeucarianThemingInspectorListKind.ColorRoleLibraryRoles,
                "delete target",
                DeucarianThemingInspectorListFilter.AllCategories);

            CollectionAssert.AreEqual(new[] { 1 }, filtered);
            Assert.IsTrue(DeucarianThemingInspectorListFilter.DeleteAt(roles, filtered[0], "Delete Filtered Role"));
            CollectionAssert.AreEqual(new[] { first, last }, library.Roles);

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            serializedLibrary.Update();

            CollectionAssert.AreEqual(new[] { first, remove, last }, library.Roles);
        }

        [Test]
        public void FilteredSerializableObjectDeletionPreservesOriginalIndexAndUndoRestoresIt()
        {
            DeucarianColorRole first = CreateRole("deucarian.first", "First", "Surface", string.Empty, "First");
            DeucarianColorRole remove = CreateRole(
                "deucarian.remove",
                "Delete Target",
                "Status",
                string.Empty,
                "Remove");
            DeucarianColorRole last = CreateRole("deucarian.last", "Last", "Surface", string.Empty, "Last");
            DeucarianColorPalette palette = CreateObject<DeucarianColorPalette>();
            palette.AddEntry(first, Color.red, "Keep first");
            palette.AddEntry(remove, Color.green, "Remove serialized entry");
            palette.AddEntry(last, Color.blue, "Keep last");
            SerializedObject serializedPalette = new SerializedObject(palette);
            SerializedProperty entries = serializedPalette.FindProperty("entries");
            List<int> filtered = DeucarianThemingInspectorListFilter.GetVisibleIndices(
                entries,
                DeucarianThemingInspectorListKind.ColorPaletteEntries,
                "remove serialized",
                DeucarianThemingInspectorListFilter.AllCategories);

            CollectionAssert.AreEqual(new[] { 1 }, filtered);
            Assert.IsTrue(DeucarianThemingInspectorListFilter.DeleteAt(entries, filtered[0], "Delete Filtered Entry"));
            CollectionAssert.AreEqual(new[] { first, last }, palette.Entries.Select(entry => entry.Role));

            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            serializedPalette.Update();

            CollectionAssert.AreEqual(new[] { first, remove, last }, palette.Entries.Select(entry => entry.Role));
            CollectionAssert.AreEqual(
                new[] { "Keep first", "Remove serialized entry", "Keep last" },
                palette.Entries.Select(entry => entry.Note));
        }

        [Test]
        public void ThemePackCustomEditorSupportsSingleAndMultiSelection()
        {
            DeucarianThemePack first = CreateObject<DeucarianThemePack>();
            DeucarianThemePack second = CreateObject<DeucarianThemePack>();

            UnityEditor.Editor singleEditor = UnityEditor.Editor.CreateEditor(first);
            createdObjects.Add(singleEditor);
            UnityEditor.Editor multiEditor = UnityEditor.Editor.CreateEditor(new Object[] { first, second });
            createdObjects.Add(multiEditor);

            Assert.IsInstanceOf<DeucarianThemePackEditor>(singleEditor);
            Assert.IsInstanceOf<DeucarianThemePackEditor>(multiEditor);
            Assert.IsTrue(Attribute.IsDefined(
                typeof(DeucarianThemePackEditor),
                typeof(CanEditMultipleObjects),
                false));
            Assert.IsTrue(multiEditor.serializedObject.isEditingMultipleObjects);
            Assert.AreEqual(2, multiEditor.targets.Length);
        }

        private T CreateObject<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(instance);
            return instance;
        }

        private DeucarianColorRole CreateRole(
            string id,
            string displayName,
            string category,
            string description,
            string assetName)
        {
            DeucarianColorRole role = CreateObject<DeucarianColorRole>();
            role.name = assetName;
            role.Configure(id, displayName, category, description, Color.white, false);
            return role;
        }

        private T CreateInactiveComponent<T>() where T : Component
        {
            GameObject gameObject = new GameObject(typeof(T).Name + " Filter Tests");
            gameObject.SetActive(false);
            createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private static void ConfigureBinding(
            SerializedProperty binding,
            DeucarianColorRole role,
            DeucarianUIToolkitStyleProperty styleProperty,
            string selector,
            string elementName,
            string elementClass,
            string variableName)
        {
            binding.FindPropertyRelative("colorRole").objectReferenceValue = role;
            binding.FindPropertyRelative("styleProperty").enumValueIndex = (int)styleProperty;
            binding.FindPropertyRelative("ussSelector").stringValue = selector;
            binding.FindPropertyRelative("elementName").stringValue = elementName;
            binding.FindPropertyRelative("elementClass").stringValue = elementClass;
            binding.FindPropertyRelative("customCssVariableName").stringValue = variableName;
        }

        private static void ConfigureMapping(
            SerializedProperty mapping,
            DeucarianColorRole role,
            string variableName)
        {
            mapping.FindPropertyRelative("role").objectReferenceValue = role;
            mapping.FindPropertyRelative("variableName").stringValue = variableName;
        }

        private static void AssertFilterResultAndNoMutation(
            Object owner,
            SerializedProperty list,
            DeucarianThemingInspectorListKind kind,
            string searchText,
            string selectedCategory,
            params int[] expectedIndices)
        {
            string before = EditorJsonUtility.ToJson(owner);

            AssertIndices(list, kind, searchText, selectedCategory, expectedIndices);

            Assert.AreEqual(before, EditorJsonUtility.ToJson(owner), "Filtering must not mutate serialized data.");
        }

        private static void AssertIndices(
            SerializedProperty list,
            DeucarianThemingInspectorListKind kind,
            string searchText,
            string selectedCategory,
            params int[] expectedIndices)
        {
            List<int> actual = DeucarianThemingInspectorListFilter.GetVisibleIndices(
                list,
                kind,
                searchText,
                selectedCategory ?? DeucarianThemingInspectorListFilter.AllCategories);

            CollectionAssert.AreEqual(expectedIndices, actual);
        }
    }
}
