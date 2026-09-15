using System;
using Deucarian.Editor.Definitions;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    public sealed partial class DeucarianAudioPaletteLabWindow
    {
        [SerializeField] private DeucarianDefinitionPanelState definitionState = new DeucarianDefinitionPanelState();

        public string CaptureReloadState() => JsonUtility.ToJson(new ReloadState
        {
            palette = DeucarianThemingEditorSettings.GetAssetGuid(paletteSet), theme = DeucarianThemingEditorSettings.GetAssetGuid(theme),
            role = DeucarianThemingEditorSettings.GetAssetGuid(selectedRole), experience = experience, category = categoryFilter,
            tab = selectedTab, search = search, advanced = advanced, useIntensity = useIntensity, intensity = intensity,
            definitions = definitionState
        });

        public void RestoreReloadState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            var value = JsonUtility.FromJson<ReloadState>(state);
            if (value == null) return;
            paletteSet = DeucarianThemingEditorSettings.LoadAssetByGuid<DeucarianAudioPaletteSet>(value.palette);
            theme = DeucarianThemingEditorSettings.LoadAssetByGuid<DeucarianTheme>(value.theme);
            selectedRole = DeucarianThemingEditorSettings.LoadAssetByGuid<DeucarianAudioRole>(value.role);
            experience = value.experience;
            categoryFilter = Mathf.Clamp(value.category, 0, 3);
            selectedTab = Mathf.Clamp(value.tab, 0, 1);
            search = value.search ?? string.Empty;
            advanced = value.advanced;
            useIntensity = value.useIntensity;
            intensity = Mathf.Clamp01(value.intensity);
            definitionState = value.definitions ?? new DeucarianDefinitionPanelState();
            selectionInitialized = true;
        }

        [Serializable]
        private sealed class ReloadState
        {
            public string palette, theme, role, search;
            public DeucarianAudioExperience experience;
            public int category, tab;
            public bool advanced, useIntensity;
            public float intensity;
            public DeucarianDefinitionPanelState definitions;
        }
    }
}
