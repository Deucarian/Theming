using System;
using Deucarian.Editor;

namespace Deucarian.Theming.Editor
{
    /// <summary>Existing project audio role assets are the source of truth for custom role keys.</summary>
    public sealed class AudioRoleKeySource : DeucarianAssetKeySource<DeucarianAudioRole>
    {
        public override Type KeyType => typeof(AudioRoleKey);
        public override Type DefinitionSetAttribute => typeof(AudioRoleKeySetAttribute);
        public override string GeneratedClassName => "ProjectAudioRoles";
        protected override DeucarianKeyChoice ReadDefinition(DeucarianAudioRole asset) =>
            asset.IsCoreRole ? null : new DeucarianKeyChoice(asset.Id, asset.DisplayName);
    }
}
