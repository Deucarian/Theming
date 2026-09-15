using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.Theming.Editor
{
    /// <summary>Copies editable theme content together, while keeping shared roles and media references.</summary>
    public static class DeucarianThemeAssetCustomization
    {
        public static Object CreateFamily() => DeucarianThemeAssetDialogs.CreateThemeFamilyFromSavePanel()?.ThemeFamily;
        public static Object CreateAudio() => Customize(DeucarianAudioDefaults.LoadPaletteSet());

        public static Object Customize(Object source)
        {
            if (source == null || !IsContent(source)) return null;
            string path = EditorUtility.SaveFilePanelInProject("Customize " + source.name, source.name + " Custom", "asset",
                "Create an editable project copy, including its palettes and profiles.");
            if (string.IsNullOrEmpty(path)) return null;
            return Copy(source, path);
        }

        internal static Object Copy(Object source, string path)
        {
            if (!IsContent(source) || !DeucarianEditorAssetCatalog.IsUnusedProjectPath(path))
                throw new ArgumentException("Choose a new project .asset path and theme content.");
            var copies = new Dictionary<Object, Object>();
            Object root = null;
            bool saved = false;
            try
            {
                root = Object.Instantiate(source); root.name = System.IO.Path.GetFileNameWithoutExtension(path);
                copies.Add(source, root);
                AssetDatabase.CreateAsset(root, path); saved = true;
                CopyReferences(root, root, copies);
                AssetDatabase.SaveAssetIfDirty(root);
                return root;
            }
            catch
            {
                if (saved) AssetDatabase.DeleteAsset(path);
                foreach (Object copy in copies.Values)
                    if (copy != null && !AssetDatabase.Contains(copy)) Undo.DestroyObjectImmediate(copy);
                throw;
            }
        }

        private static void CopyReferences(Object copy, Object root, Dictionary<Object, Object> copies)
        {
            using (var serialized = new SerializedObject(copy))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Object source = property.objectReferenceValue;
                    if (!IsContent(source)) continue;
                    if (!copies.TryGetValue(source, out Object child))
                    {
                        child = Object.Instantiate(source); child.name = source.name;
                        copies.Add(source, child);
                        AssetDatabase.AddObjectToAsset(child, root);
                        CopyReferences(child, root, copies);
                    }
                    property.objectReferenceValue = child;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(copy);
        }

        private static bool IsContent(Object value) => value is DeucarianThemeFamily || value is DeucarianTheme ||
            value is DeucarianColorPalette || value is DeucarianAudioPaletteSet || value is DeucarianAudioPalette ||
            value is DeucarianThemeStyle || value is DeucarianThemeTypographyProfile || value is DeucarianThemeSurfaceProfile ||
            value is DeucarianThemeShapeProfile || value is DeucarianThemeStrokeProfile;
    }
}
