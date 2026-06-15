using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Creates lightweight UI Toolkit demo files in a Unity project.
    /// </summary>
    public static class DeucarianUIToolkitDemoAssetFactory
    {
        private const string DemoRoot = "Assets/Deucarian/Theming/UIToolkitDemo";

        [MenuItem("Deucarian/Theming/Create UI Toolkit Demo Assets")]
        public static void CreateDemoAssets()
        {
            EnsureFolder(DemoRoot);

            WriteTextAsset(DemoRoot + "/ReportViewerDemo.uxml", GetUxml());
            WriteTextAsset(DemoRoot + "/ReportViewerDemo.uss", GetUss());
            WriteTextAsset(DemoRoot + "/README.md", GetReadme());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Object readme = AssetDatabase.LoadMainAssetAtPath(DemoRoot + "/README.md");
            Selection.activeObject = readme;
            EditorGUIUtility.PingObject(readme);
        }

        private static void EnsureFolder(string folder)
        {
            string normalized = folder.Replace('\\', '/').Trim('/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void WriteTextAsset(string assetPath, string content)
        {
            string fullPath = Path.GetFullPath(assetPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(fullPath))
            {
                Debug.LogWarning($"UI Toolkit demo asset already exists and was left unchanged: {assetPath}");
                return;
            }

            File.WriteAllText(fullPath, content);
        }

        private static string GetUxml()
        {
            return @"<ui:UXML xmlns:ui=""UnityEngine.UIElements"">
  <Style src=""ReportViewerDemo.uss"" />
  <ui:VisualElement class=""viewer-root"">
    <ui:VisualElement class=""viewer-panel"">
      <ui:Label name=""viewer-title"" class=""viewer-title"" text=""3D Report Viewer"" />
      <ui:Label class=""viewer-body"" text=""Theme this UIDocument with DeucarianUIToolkitThemeApplier."" />
      <ui:Button name=""viewer-button"" class=""viewer-button"" text=""Open Report"" />
      <ui:Label class=""viewer-error"" text=""No report selected"" />
    </ui:VisualElement>
  </ui:VisualElement>
</ui:UXML>
";
        }

        private static string GetUss()
        {
            return @".viewer-root {
    flex-grow: 1;
    padding: 16px;
}

.viewer-panel {
    padding: 12px;
    border-top-width: 1px;
    border-right-width: 1px;
    border-bottom-width: 1px;
    border-left-width: 1px;
}

.viewer-title {
    font-size: 18px;
    -unity-font-style: bold;
}

.viewer-body {
    margin-top: 8px;
}

.viewer-button {
    margin-top: 12px;
}

.viewer-error {
    margin-top: 8px;
}
";
        }

        private static string GetReadme()
        {
            return @"# UI Toolkit Theming Demo

1. Create default theme assets with `Deucarian/Theming/Create Missing Default Theme Assets`.
2. Create a scene object with `UIDocument`.
3. Assign `ReportViewerDemo.uxml` to the UIDocument.
4. Add `DeucarianUIToolkitThemeApplier`.
5. Add bindings such as:
   - `.viewer-root` -> BackgroundColor
   - `.viewer-panel` -> BackgroundColor and BorderColor
   - `.viewer-title` -> TextColor
   - `.viewer-button` -> BackgroundColor
   - `.viewer-error` -> TextColor

Runtime USS custom variable assignment is not exposed consistently in Unity 2022.3. Use `DeucarianUIToolkitThemeVariables` to preview or generate USS text from a theme and role library.
";
        }
    }
}
