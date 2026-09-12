using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ui = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.Theming.Editor
{
    public sealed class DeucarianAudioCueForm : IDisposable
    {
        private readonly DeucarianAudioPalette palette;
        private readonly string roleId;
        private readonly Func<DeucarianAudioResolution> resolve;
        private readonly Action changed;
        private readonly DeucarianEditorWorkspaceForm form;
        private readonly VisualElement fields;
        private readonly DeucarianEditorSerializedForm variants;
        private readonly Label source;
        private readonly int entryIndex;
        private bool disposed;
        public bool MatchesSource => !disposed && resolve().SourcePalette == palette && FindIndex() == entryIndex;

        internal DeucarianAudioCueForm(VisualElement root, DeucarianAudioRole role,
            Func<DeucarianAudioResolution> resolve, Action changed) : this(root, role == null ? null : role.Id, resolve, changed) { }

        public DeucarianAudioCueForm(VisualElement root, string roleId,
            Func<DeucarianAudioResolution> resolve, Action changed, bool detailed = true, string clipLabel = "Audio clip")
        {
            this.roleId = roleId;
            this.resolve = resolve;
            this.changed = changed;
            palette = resolve().SourcePalette;
            fields = new VisualElement(); root.Add(fields);
            form = new DeucarianEditorWorkspaceForm(fields);
            form.AssetWithActions("audio-resolved-clip", clipLabel, typeof(AudioClip), () => Cue.Clip,
                value => Write(cue => cue.FindPropertyRelative("clip").objectReferenceValue = value));
            form.Slider("audio-cue-volume", "Volume", 0, Mathf.Max(1, Cue.Volume), () => Cue.Volume,
                value => WriteFinite(value, cue => cue.FindPropertyRelative("volume").floatValue = Mathf.Max(0, value)));
            if (detailed)
            {
            form.Slider("audio-cue-pitch", "Pitch", 0.1f, 3, () => (Cue.MinimumPitch + Cue.MaximumPitch) * 0.5f,
                value => WriteFinite(value, cue =>
                {
                    float halfRange = (Cue.MaximumPitch - Cue.MinimumPitch) * 0.5f;
                    cue.FindPropertyRelative("minimumPitch").floatValue = Mathf.Clamp(value - halfRange, 0.1f, 3);
                    cue.FindPropertyRelative("maximumPitch").floatValue = Mathf.Clamp(value + halfRange, 0.1f, 3);
                }));
            var variation = form.Section("Variation", true);
            variation.Number("audio-minimum-pitch", "Minimum pitch", () => Cue.MinimumPitch, value => WriteFinite(value, cue =>
            {
                value = Mathf.Clamp(value, 0.1f, 3);
                cue.FindPropertyRelative("minimumPitch").floatValue = value;
                cue.FindPropertyRelative("maximumPitch").floatValue = Mathf.Max(value, Cue.MaximumPitch);
            }));
            variation.Number("audio-maximum-pitch", "Maximum pitch", () => Cue.MaximumPitch, value => WriteFinite(value, cue =>
            {
                value = Mathf.Clamp(value, 0.1f, 3);
                cue.FindPropertyRelative("maximumPitch").floatValue = value;
                cue.FindPropertyRelative("minimumPitch").floatValue = Mathf.Min(value, Cue.MinimumPitch);
            }));
            variation.Toggle("audio-intentional-silence", "Intentional silence", () => Cue.IntentionalSilence,
                value => Write(cue => cue.FindPropertyRelative("intentionalSilence").boolValue = value));
            int index = FindIndex();
            if (index >= 0)
            {
                variants = new DeucarianEditorSerializedForm(variation.Root, palette);
                variants.Property("entries.Array.data[" + index + "].cue.variants", "Alternative clips");
                variants.Property("entries.Array.data[" + index + "].note", "Note");
            }
            }
            entryIndex = FindIndex();
            source = Ui.Label(string.Empty, "dw-muted"); root.Add(source);
            Refresh();
        }

        private DeucarianAudioCue Cue => resolve().Cue;
        private int FindIndex()
        {
            if (palette == null || string.IsNullOrEmpty(roleId)) return -1;
            for (int i = 0; i < palette.Entries.Count; i++)
                if (palette.Entries[i]?.Role?.Id == roleId) return i;
            return -1;
        }
        private bool CanEdit => !disposed && palette != null && FindIndex() >= 0 &&
            AssetDatabase.GetAssetPath(palette).Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal) &&
            DeucarianThemeRuntimeResolver.UseAudio;

        public void Refresh()
        {
            if (disposed) return;
            form.Refresh();
            fields.SetEnabled(CanEdit);
            source.text = CanEdit ? string.Empty : palette != null
                ? "Package palette · read only. Use a project-owned palette to edit these sounds."
                : "Role default · assign a cue in a project palette to override it.";
            Ui.Show(source, !CanEdit);
        }

        private void WriteFinite(float value, Action<SerializedProperty> change)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) { Refresh(); return; }
            Write(change);
        }

        private void Write(Action<SerializedProperty> change)
        {
            if (!CanEdit || !MatchesSource) return;
            using (var serialized = new SerializedObject(palette))
            {
                var cue = serialized.FindProperty("entries").GetArrayElementAtIndex(FindIndex()).FindPropertyRelative("cue");
                change(cue);
                serialized.ApplyModifiedProperties();
            }
            palette.RebuildCache();
            DeucarianThemeAssetChangeBus.NotifyChanged(palette);
            changed?.Invoke();
            Refresh();
        }

        public void Dispose() { if (disposed) return; disposed = true; variants?.Dispose(); fields.SetEnabled(false); }
    }
}
