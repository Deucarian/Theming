using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    [CustomPropertyDrawer(typeof(DeucarianAudioEntry))]
    public sealed class DeucarianAudioEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty cueProperty = property.FindPropertyRelative("cue");
            float cueHeight = EditorGUI.GetPropertyHeight(cueProperty, true);
            return (EditorGUIUtility.singleLineHeight * 3f) + cueHeight +
                (EditorGUIUtility.standardVerticalSpacing * 3f);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty roleProperty = property.FindPropertyRelative("role");
            SerializedProperty cueProperty = property.FindPropertyRelative("cue");
            SerializedProperty noteProperty = property.FindPropertyRelative("note");

            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect firstLine = new Rect(position.x, position.y, position.width, lineHeight);
            float cueHeight = EditorGUI.GetPropertyHeight(cueProperty, true);
            Rect cueRect = new Rect(
                position.x,
                firstLine.y + lineHeight + spacing,
                position.width,
                cueHeight);
            Rect noteRect = new Rect(
                position.x,
                cueRect.yMax + spacing,
                position.width,
                lineHeight);
            Rect metadataRect = new Rect(
                position.x,
                noteRect.yMax + spacing,
                position.width,
                lineHeight);

            EditorGUI.PropertyField(firstLine, roleProperty, GUIContent.none);
            EditorGUI.PropertyField(cueRect, cueProperty, true);
            EditorGUI.PropertyField(noteRect, noteProperty);
            EditorGUI.LabelField(metadataRect, GetRoleMetadata(roleProperty), EditorStyles.miniLabel);

            EditorGUI.EndProperty();
        }

        private static string GetRoleMetadata(SerializedProperty roleProperty)
        {
            DeucarianAudioRole role = roleProperty.objectReferenceValue as DeucarianAudioRole;
            if (role == null)
            {
                return "No audio role assigned";
            }

            return $"Category: {role.Category}   ID: {role.Id}";
        }
    }
}
