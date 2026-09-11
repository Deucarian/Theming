using System;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.Theming.Editor
{
    [CustomPropertyDrawer(typeof(AudioRoleKey), true)]
    public sealed class AudioRoleKeyDrawer : DeucarianKeyDrawer
    {
        public override Type KeyType => typeof(AudioRoleKey);
        public override Type DefinitionSetAttribute => typeof(AudioRoleKeySetAttribute);
        public override string SetupHint => "Select an existing AudioRoleKey; declare reusable keys once in a [AudioRoleKeySet] class.";
    }
}
