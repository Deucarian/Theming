using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    [CustomPropertyDrawer(typeof(DeucarianColorEntry))]
    public sealed class DeucarianColorEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight * 3f) + (EditorGUIUtility.standardVerticalSpacing * 2f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty roleProperty = property.FindPropertyRelative("role");
            SerializedProperty colorProperty = property.FindPropertyRelative("color");
            SerializedProperty noteProperty = property.FindPropertyRelative("note");

            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect firstLine = new Rect(position.x, position.y, position.width, lineHeight);
            Rect secondLine = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
            Rect thirdLine = new Rect(position.x, secondLine.y + lineHeight + spacing, position.width, lineHeight);

            float colorWidth = 90f;
            Rect roleRect = new Rect(firstLine.x, firstLine.y, firstLine.width - colorWidth - spacing, lineHeight);
            Rect colorRect = new Rect(roleRect.xMax + spacing, firstLine.y, colorWidth, lineHeight);

            EditorGUI.PropertyField(roleRect, roleProperty, GUIContent.none);
            EditorGUI.PropertyField(colorRect, colorProperty, GUIContent.none);
            EditorGUI.PropertyField(secondLine, noteProperty);
            EditorGUI.LabelField(thirdLine, GetRoleMetadata(roleProperty), EditorStyles.miniLabel);

            EditorGUI.EndProperty();
        }

        private static string GetRoleMetadata(SerializedProperty roleProperty)
        {
            DeucarianColorRole role = roleProperty.objectReferenceValue as DeucarianColorRole;
            if (role == null)
            {
                return "No role assigned";
            }

            return $"Category: {role.Category}   ID: {role.Id}";
        }
    }
}
