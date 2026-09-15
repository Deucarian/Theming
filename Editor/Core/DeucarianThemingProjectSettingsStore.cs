using System;
using UnityEditor;

namespace Deucarian.Theming.Editor
{
    internal interface IDeucarianThemingProjectSettingsStore
    {
        DeucarianThemeRuntimeSettings Read();
        bool CanWrite { get; }
        string Problem { get; }
        void Write(Action<DeucarianThemeRuntimeSettings> change);
    }

    internal sealed class DeucarianThemingProjectSettingsStore : IDeucarianThemingProjectSettingsStore
    {
        private const string DefaultPath = "Assets/Deucarian/Resources/DeucarianThemeRuntimeSettings.asset";
        public DeucarianThemeRuntimeSettings Read()
        {
            var assets = DeucarianThemeRuntimeSettingsAssets.FindRuntimeSettingsResourceAssets();
            return assets.Count == 1 ? assets[0] : null;
        }
        public bool CanWrite => !EditorApplication.isPlayingOrWillChangePlaymode && string.IsNullOrEmpty(Problem);
        public string Problem => DeucarianThemeRuntimeSettingsAssets.FindRuntimeSettingsResourceAssets().Count > 1
            ? "More than one runtime settings resource exists. Keep one before changing project settings." : null;

        public void Write(Action<DeucarianThemeRuntimeSettings> change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            if (!CanWrite) return;
            var settings = Read();
            if (settings == null)
            {
                settings = DeucarianThemeRuntimeSettingsAssets.CreateRuntimeSettingsAtPath(DefaultPath);
                if (settings == null) throw new InvalidOperationException("Could not create unique project Theming settings.");
                Undo.RegisterCreatedObjectUndo(settings, "Configure project Theming");
            }
            Undo.RecordObject(settings, "Change project Theming");
            change(settings);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
        }
    }
}
