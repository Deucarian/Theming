using System;
using System.Collections.Generic;
using System.Text;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    internal enum DeucarianThemingInspectorListKind
    {
        ColorRoleLibraryRoles,
        ColorPaletteEntries,
        ThemePackRoles,
        UIToolkitBindings,
        UIToolkitVariableMappings
    }

    internal sealed class DeucarianThemingInspectorListFilterState
    {
        private readonly List<int> visibleIndices = new List<int>();

        public string SearchText { get; set; } = string.Empty;
        public string SelectedCategory { get; set; } = DeucarianThemingInspectorListFilter.AllCategories;
        public IReadOnlyList<int> VisibleIndices => visibleIndices;
        public bool IsFiltering => DeucarianThemingInspectorListFilter.IsFiltering(SearchText, SelectedCategory);

        internal void SetVisibleIndices(IReadOnlyList<int> indices)
        {
            visibleIndices.Clear();
            if (indices == null)
            {
                return;
            }

            for (int i = 0; i < indices.Count; i++)
            {
                visibleIndices.Add(indices[i]);
            }
        }
    }

    internal static class DeucarianThemingInspectorListFilter
    {
        private const string CategorySentinelPrefix = "\u001fDeucarian.Theming:";
        private const string AllCategoriesLabel = "All Categories";
        private const string UncategorizedCategoryLabel = "Uncategorized";
        private const string MissingCategoryLabel = "Missing";

        internal const string AllCategories = CategorySentinelPrefix + "all";
        internal const string UncategorizedCategory = CategorySentinelPrefix + "uncategorized";
        internal const string MissingCategory = CategorySentinelPrefix + "missing";

        public static void DrawInspectorProperties(
            SerializedObject serializedObject,
            string listPropertyName,
            DeucarianThemingInspectorListKind kind,
            DeucarianThemingInspectorListFilterState state,
            string placeholder)
        {
            if (serializedObject == null)
            {
                return;
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            bool drewList = false;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (string.Equals(iterator.propertyPath, listPropertyName, StringComparison.Ordinal))
                {
                    Draw(iterator.Copy(), kind, state, placeholder);
                    drewList = true;
                    continue;
                }

                using (new EditorGUI.DisabledScope(iterator.propertyPath == "m_Script"))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            if (!drewList)
            {
                state?.SetVisibleIndices(Array.Empty<int>());
                EditorGUILayout.HelpBox(
                    $"Serialized list '{listPropertyName}' could not be found.",
                    MessageType.Error);
            }
        }

        public static void Draw(
            SerializedProperty listProperty,
            DeucarianThemingInspectorListKind kind,
            DeucarianThemingInspectorListFilterState state,
            string placeholder)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (listProperty == null || !listProperty.isArray)
            {
                state.SetVisibleIndices(Array.Empty<int>());
                EditorGUILayout.HelpBox("The serialized list is unavailable.", MessageType.Error);
                return;
            }

            EditorGUILayout.Space();
            state.SearchText = DeucarianEditorSearchField.Draw(
                state.SearchText,
                string.IsNullOrWhiteSpace(placeholder) ? "Search" : placeholder);

            IReadOnlyList<string> categories = GetCategories(listProperty, kind);
            int categoryIndex = IndexOfCategory(categories, state.SelectedCategory);
            if (categoryIndex < 0)
            {
                categoryIndex = 0;
                state.SelectedCategory = AllCategories;
            }

            categoryIndex = EditorGUILayout.Popup(
                "Category",
                categoryIndex,
                ToDisplayLabels(categories));
            state.SelectedCategory = categories[categoryIndex];

            List<int> visibleIndices = GetVisibleIndices(
                listProperty,
                kind,
                state.SearchText,
                state.SelectedCategory);
            state.SetVisibleIndices(visibleIndices);

            if (!IsFiltering(state.SearchText, state.SelectedCategory))
            {
                EditorGUILayout.PropertyField(listProperty, true);
                return;
            }

            EditorGUILayout.LabelField(
                $"Showing {visibleIndices.Count} of {listProperty.arraySize}",
                EditorStyles.miniLabel);

            if (visibleIndices.Count == 0)
            {
                EditorGUILayout.HelpBox("No serialized items match the current filter.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < visibleIndices.Count; i++)
                {
                    int originalIndex = visibleIndices[i];
                    SerializedProperty element = listProperty.GetArrayElementAtIndex(originalIndex);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField($"Element {originalIndex}", EditorStyles.miniBoldLabel);
                            if (GUILayout.Button(
                                    new GUIContent("Remove", "Remove this item from the underlying serialized list."),
                                    GUILayout.Width(64f)))
                            {
                                DeleteAt(listProperty, originalIndex, "Remove Filtered Theming List Item");
                                GUIUtility.ExitGUI();
                            }
                        }

                        EditorGUILayout.PropertyField(element, GUIContent.none, true);
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Clear the search and category filter to add or reorder items.",
                MessageType.None);
        }

        internal static List<int> GetVisibleIndices(
            SerializedProperty listProperty,
            DeucarianThemingInspectorListKind kind,
            string searchText,
            string selectedCategory)
        {
            List<int> indices = new List<int>();
            if (listProperty == null || !listProperty.isArray)
            {
                return indices;
            }

            string[] tokens = Tokenize(searchText);
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                CategoryValue category = GetCategory(element, kind);
                if (!MatchesCategory(category, selectedCategory))
                {
                    continue;
                }

                string corpus = BuildSearchCorpus(element, kind, i);
                if (MatchesAllTokens(corpus, tokens))
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        internal static IReadOnlyList<string> GetCategories(
            SerializedProperty listProperty,
            DeucarianThemingInspectorListKind kind)
        {
            List<string> categories = new List<string> { AllCategories };
            if (listProperty == null || !listProperty.isArray)
            {
                return categories;
            }

            HashSet<string> namedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasUncategorized = false;
            bool hasMissing = false;
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                CategoryValue category = GetCategory(listProperty.GetArrayElementAtIndex(i), kind);
                if (category.IsMissing)
                {
                    hasMissing = true;
                }
                else if (string.IsNullOrWhiteSpace(category.Name))
                {
                    hasUncategorized = true;
                }
                else
                {
                    namedCategories.Add(category.Name.Trim());
                }
            }

            List<string> sortedCategories = new List<string>(namedCategories);
            sortedCategories.Sort(StringComparer.OrdinalIgnoreCase);
            categories.AddRange(sortedCategories);
            if (hasUncategorized)
            {
                categories.Add(UncategorizedCategory);
            }

            if (hasMissing)
            {
                categories.Add(MissingCategory);
            }

            return categories;
        }

        internal static bool DeleteAt(
            SerializedProperty listProperty,
            int index,
            string undoName = "Remove Theming List Item")
        {
            if (listProperty == null
                || !listProperty.isArray
                || index < 0
                || index >= listProperty.arraySize)
            {
                return false;
            }

            SerializedObject serializedObject = listProperty.serializedObject;
            UnityEngine.Object[] targets = serializedObject.targetObjects;
            Undo.RecordObjects(targets, undoName);

            int originalSize = listProperty.arraySize;
            listProperty.DeleteArrayElementAtIndex(index);
            if (listProperty.arraySize == originalSize)
            {
                listProperty.DeleteArrayElementAtIndex(index);
            }

            bool removed = listProperty.arraySize == originalSize - 1;
            serializedObject.ApplyModifiedProperties();
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    EditorUtility.SetDirty(targets[i]);
                }
            }

            return removed;
        }

        internal static bool IsFiltering(string searchText, string selectedCategory)
        {
            return !string.IsNullOrWhiteSpace(searchText)
                || !string.Equals(selectedCategory, AllCategories, StringComparison.Ordinal);
        }

        private static string BuildSearchCorpus(
            SerializedProperty element,
            DeucarianThemingInspectorListKind kind,
            int index)
        {
            StringBuilder builder = new StringBuilder();
            Append(builder, index.ToString());
            Append(builder, "element " + index);

            switch (kind)
            {
                case DeucarianThemingInspectorListKind.ColorRoleLibraryRoles:
                    Append(builder, "role " + index);
                    AppendRole(builder, element);
                    break;
                case DeucarianThemingInspectorListKind.ColorPaletteEntries:
                    Append(builder, "entry " + index);
                    AppendRole(builder, FindRelative(element, "role"));
                    AppendString(builder, element, "note");
                    break;
                case DeucarianThemingInspectorListKind.ThemePackRoles:
                    Append(builder, "theme pack role " + index);
                    AppendString(builder, element, "assetName");
                    AppendString(builder, element, "id");
                    AppendString(builder, element, "displayName");
                    AppendString(builder, element, "category");
                    AppendString(builder, element, "description");
                    break;
                case DeucarianThemingInspectorListKind.UIToolkitBindings:
                    Append(builder, "binding " + index);
                    AppendString(builder, element, "elementName");
                    AppendString(builder, element, "elementClass");
                    AppendString(builder, element, "ussSelector");
                    AppendRole(builder, FindRelative(element, "colorRole"));
                    AppendEnum(builder, element, "styleProperty");
                    AppendString(builder, element, "customCssVariableName");
                    break;
                case DeucarianThemingInspectorListKind.UIToolkitVariableMappings:
                    Append(builder, "variable mapping " + index);
                    AppendRole(builder, FindRelative(element, "role"));
                    AppendString(builder, element, "variableName");
                    break;
            }

            return builder.ToString();
        }

        private static CategoryValue GetCategory(
            SerializedProperty element,
            DeucarianThemingInspectorListKind kind)
        {
            if (element == null)
            {
                return CategoryValue.Missing;
            }

            if (kind == DeucarianThemingInspectorListKind.ThemePackRoles)
            {
                SerializedProperty category = FindRelative(element, "category");
                return new CategoryValue(category != null ? category.stringValue : string.Empty, false);
            }

            SerializedProperty roleProperty;
            switch (kind)
            {
                case DeucarianThemingInspectorListKind.ColorRoleLibraryRoles:
                    roleProperty = element;
                    break;
                case DeucarianThemingInspectorListKind.ColorPaletteEntries:
                case DeucarianThemingInspectorListKind.UIToolkitVariableMappings:
                    roleProperty = FindRelative(element, "role");
                    break;
                case DeucarianThemingInspectorListKind.UIToolkitBindings:
                    roleProperty = FindRelative(element, "colorRole");
                    break;
                default:
                    roleProperty = null;
                    break;
            }

            DeucarianColorRole role = GetRole(roleProperty);
            return role == null
                ? CategoryValue.Missing
                : new CategoryValue(role.Category, false);
        }

        private static bool MatchesCategory(CategoryValue category, string selectedCategory)
        {
            if (string.IsNullOrWhiteSpace(selectedCategory)
                || string.Equals(selectedCategory, AllCategories, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(selectedCategory, MissingCategory, StringComparison.Ordinal))
            {
                return category.IsMissing;
            }

            if (string.Equals(selectedCategory, UncategorizedCategory, StringComparison.Ordinal))
            {
                return !category.IsMissing && string.IsNullOrWhiteSpace(category.Name);
            }

            return !category.IsMissing
                && string.Equals(category.Name?.Trim(), selectedCategory, StringComparison.OrdinalIgnoreCase);
        }

        private static string[] Tokenize(string searchText)
        {
            return string.IsNullOrWhiteSpace(searchText)
                ? Array.Empty<string>()
                : searchText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool MatchesAllTokens(string corpus, IReadOnlyList<string> tokens)
        {
            corpus = corpus ?? string.Empty;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (corpus.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AppendRole(StringBuilder builder, SerializedProperty roleProperty)
        {
            DeucarianColorRole role = GetRole(roleProperty);
            if (role == null)
            {
                Append(builder, "missing null unassigned role");
                return;
            }

            Append(builder, role.name);
            Append(builder, role.DisplayName);
            Append(builder, role.Id);
            Append(builder, role.Category);
            Append(builder, role.Description);
        }

        private static DeucarianColorRole GetRole(SerializedProperty roleProperty)
        {
            return roleProperty != null && roleProperty.propertyType == SerializedPropertyType.ObjectReference
                ? roleProperty.objectReferenceValue as DeucarianColorRole
                : null;
        }

        private static void AppendString(
            StringBuilder builder,
            SerializedProperty element,
            string relativePropertyName)
        {
            SerializedProperty property = FindRelative(element, relativePropertyName);
            if (property != null && property.propertyType == SerializedPropertyType.String)
            {
                Append(builder, property.stringValue);
            }
        }

        private static void AppendEnum(
            StringBuilder builder,
            SerializedProperty element,
            string relativePropertyName)
        {
            SerializedProperty property = FindRelative(element, relativePropertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Enum)
            {
                return;
            }

            int index = property.enumValueIndex;
            if (index >= 0 && index < property.enumNames.Length)
            {
                Append(builder, property.enumNames[index]);
            }

            if (index >= 0 && index < property.enumDisplayNames.Length)
            {
                Append(builder, property.enumDisplayNames[index]);
            }
        }

        private static SerializedProperty FindRelative(SerializedProperty element, string name)
        {
            try
            {
                return element?.FindPropertyRelative(name);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static void Append(StringBuilder builder, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(value.Trim());
        }

        private static int IndexOfCategory(IReadOnlyList<string> categories, string selectedCategory)
        {
            for (int i = 0; i < categories.Count; i++)
            {
                if (string.Equals(categories[i], selectedCategory, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string[] ToDisplayLabels(IReadOnlyList<string> values)
        {
            string[] result = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                result[i] = GetCategoryDisplayLabel(values[i]);
            }

            return result;
        }

        private static string GetCategoryDisplayLabel(string category)
        {
            if (string.Equals(category, AllCategories, StringComparison.Ordinal))
            {
                return AllCategoriesLabel;
            }

            if (string.Equals(category, UncategorizedCategory, StringComparison.Ordinal))
            {
                return UncategorizedCategoryLabel;
            }

            if (string.Equals(category, MissingCategory, StringComparison.Ordinal))
            {
                return MissingCategoryLabel;
            }

            if (string.Equals(category, AllCategoriesLabel, StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, UncategorizedCategoryLabel, StringComparison.OrdinalIgnoreCase)
                || string.Equals(category, MissingCategoryLabel, StringComparison.OrdinalIgnoreCase))
            {
                return "Category: " + category;
            }

            return category ?? string.Empty;
        }

        private readonly struct CategoryValue
        {
            public static readonly CategoryValue Missing = new CategoryValue(string.Empty, true);

            public CategoryValue(string name, bool isMissing)
            {
                Name = name ?? string.Empty;
                IsMissing = isMissing;
            }

            public string Name { get; }
            public bool IsMissing { get; }
        }
    }
}
