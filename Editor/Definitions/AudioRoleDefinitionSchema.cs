using System;
using System.Linq;
using Deucarian.Editor.Definitions;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Definitions
{
    [Serializable]
    public sealed class AudioRoleDefinitionSpec : DeucarianDefinitionSpec
    {
        [DefinitionField("category")] public string Category = "Project";
        [DefinitionField("description")] public string Description = "Project sound";
        [DefinitionField("defaultCue.clip")] public AudioClip Clip;
        [DefinitionField("defaultCue.variants")] public AudioClip[] Variants = Array.Empty<AudioClip>();
        [DefinitionField("defaultCue.volume")] public float Volume = 1;
        [DefinitionField("defaultCue.minimumPitch")] public float MinimumPitch = 1;
        [DefinitionField("defaultCue.maximumPitch")] public float MaximumPitch = 1;
        [DefinitionField("defaultCue.intentionalSilence")] public bool IntentionalSilence = true;
    }

    public sealed class AudioRoleDefinitionSchema : DeucarianSerializedDefinitionSchema<DeucarianAudioRole, AudioRoleDefinitionSpec>
    {
        public override string Id => "audio";
        public override string DisplayName => "Audio roles";
        public override bool CanPreview => true;
        public override void Preview(ScriptableObject asset) => DeucarianAudioPaletteLabWindow.PreviewDefinition((DeucarianAudioRole)asset);
        public override void Validate(DeucarianDefinitionSpec value)
        {
            base.Validate(value);
            var spec = (AudioRoleDefinitionSpec)value;
            if (!DeucarianAudioRole.IsValidId(spec.Id)) throw new ArgumentException("Use a valid stable audio role ID.");
            if (float.IsNaN(spec.Volume) || spec.Volume < 0 || spec.Volume > 1) throw new ArgumentException("Audio volume must be between zero and one.");
            if (float.IsNaN(spec.MinimumPitch) || float.IsNaN(spec.MaximumPitch) || spec.MinimumPitch <= 0 || spec.MaximumPitch > 3 || spec.MinimumPitch > spec.MaximumPitch) throw new ArgumentException("Audio pitch must be greater than zero and at most three, with minimum no greater than maximum.");
            if (!spec.IntentionalSilence && spec.Clip == null && (spec.Variants == null || !spec.Variants.Any(x => x != null))) throw new ArgumentException("Assign a Clip or Variants, or enable Intentional Silence for this audio role.");
        }
        public override void RefreshCatalog(bool validateOnly = false)
        {
            var roles = AssetDatabase.FindAssets("t:DeucarianAudioRole", new[] { "Assets" })
                .Select(x => AssetDatabase.LoadAssetAtPath<DeucarianAudioRole>(AssetDatabase.GUIDToAssetPath(x)))
                .Where(x => x != null && !x.IsCoreRole).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (roles.GroupBy(x => x.Id).Any(x => x.Count() > 1)) throw new InvalidOperationException("Project audio role IDs must be unique.");
            DeucarianDefinitionCatalog.Update<DeucarianAudioRoleLibrary>("Assets/DeucarianDefinitions/Resources/Deucarian/Theming/ProjectAudioRoles.asset", "roles", roles, validateOnly);
        }
        [MenuItem("Assets/Create/Deucarian/Theming/Audio Role")]
        private static void CreateDefinition() { Selection.activeObject = DeucarianDefinitionSync.Create(new AudioRoleDefinitionSchema(), "NewAudioRole"); }
    }
}
