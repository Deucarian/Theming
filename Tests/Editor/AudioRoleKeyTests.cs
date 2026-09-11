using System;
using System.Linq;
using Deucarian.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor.Tests
{
    public sealed class AudioRoleKeyTests
    {
        [Test]
        public void CodeAndSerializedSelectionsRetainTheSameIdentityWithoutAssets()
        {
            var original = ScriptableObject.CreateInstance<AudioKeyCarrier>();
            var restored = ScriptableObject.CreateInstance<AudioKeyCarrier>();
            try
            {
                original.Sound = AudioRoles.Feedback.Warning;
                string json = JsonUtility.ToJson(original);
                JsonUtility.FromJsonOverwrite(json, restored);
                Assert.That(restored.Sound, Is.EqualTo(AudioRoles.Feedback.Warning));
                Assert.That(json, Does.Not.Contain("instanceID"));
                using (var serialized = new SerializedObject(restored))
                {
                    var field = serialized.FindProperty(nameof(AudioKeyCarrier.Sound));
                    Assert.That(DeucarianKeyPropertyType.Resolve(field), Is.EqualTo(typeof(AudioRoleKey)));
                    field.FindPropertyRelative("definitionId").stringValue = AudioRoles.UI.Activate.Id;
                    serialized.ApplyModifiedProperties();
                }
                Assert.That(restored.Sound, Is.EqualTo(AudioRoles.UI.Activate));
            }
            finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(restored); }
        }

        [Test]
        public void PickerContainsRealBuiltInDefinitionsAndMissingSelectionsExplainTheFix()
        {
            var choices = DeucarianKeyChoices.Read(typeof(AudioRoleKey), typeof(AudioRoleKeySetAttribute));
            Assert.That(choices.Count(c => c.Id == AudioRoles.UI.Activate.Id), Is.EqualTo(1));
            Assert.That(choices.Count(c => c.Id == AudioRoles.Feedback.Warning.Id), Is.EqualTo(1));
            string error = DeucarianKeyPicker.Validate("deleted.role", choices, "Button.Sound", "Select an existing audio role.");
            Assert.That(error, Does.Contain("Button.Sound").And.Contain("deleted.role").And.Contain("Select an existing"));
            Assert.Throws<ArgumentNullException>(() => ThemeAudio.Play(null));
        }
    }

    public sealed class AudioKeyCarrier : ScriptableObject
    {
        public AudioRoleKey Sound;
    }
}
