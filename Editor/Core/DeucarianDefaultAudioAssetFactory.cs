using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>Deterministically generates the original Deucarian default UI feedback clips and assets.</summary>
    public static class DeucarianDefaultAudioAssetFactory
    {
        private const string VirtualRoot =
            "Packages/com.deucarian.theming/Runtime/Resources/Deucarian/Theming/Audio/Defaults";

        private readonly struct RoleSpec
        {
            public RoleSpec(string id, string name, string category)
            {
                Id = id;
                Name = name;
                Category = category;
            }

            public string Id { get; }
            public string Name { get; }
            public string Category { get; }
        }

        private readonly struct ToneSpec
        {
            public ToneSpec(string name, float frequency, float duration, int pulses, float harmonic)
            {
                Name = name;
                Frequency = frequency;
                Duration = duration;
                Pulses = pulses;
                Harmonic = harmonic;
            }

            public string Name { get; }
            public float Frequency { get; }
            public float Duration { get; }
            public int Pulses { get; }
            public float Harmonic { get; }
        }

        private static readonly RoleSpec[] Roles =
        {
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Hover, "Hover", DeucarianAudioRoleCategories.UI),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Press, "Press", DeucarianAudioRoleCategories.UI),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Activate, "Activate", DeucarianAudioRoleCategories.UI),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Select, "Select", DeucarianAudioRoleCategories.UI),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Submit, "Submit", DeucarianAudioRoleCategories.UI),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Cancel, "Cancel", DeucarianAudioRoleCategories.UI),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Key, "Key", DeucarianAudioRoleCategories.Input),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.SpecialKey, "Special Key", DeucarianAudioRoleCategories.Input),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Info, "Info", DeucarianAudioRoleCategories.Feedback),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Success, "Success", DeucarianAudioRoleCategories.Feedback),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Warning, "Warning", DeucarianAudioRoleCategories.Feedback),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Error, "Error", DeucarianAudioRoleCategories.Feedback),
            new RoleSpec(DeucarianBuiltinAudioRoleIds.Invalid, "Invalid", DeucarianAudioRoleCategories.Feedback)
        };

        private static readonly ToneSpec[] Tones =
        {
            new ToneSpec("default-tap", 660f, 0.07f, 1, 0.10f),
            new ToneSpec("default-confirm", 880f, 0.13f, 1, 0.24f),
            new ToneSpec("default-cancel", 480f, 0.11f, 1, 0.08f),
            new ToneSpec("default-warning", 740f, 0.20f, 2, 0.18f),
            new ToneSpec("default-error", 320f, 0.16f, 2, 0.12f),
            new ToneSpec("default-key-a", 930f, 0.045f, 1, 0.05f),
            new ToneSpec("default-key-b", 1030f, 0.043f, 1, 0.05f),
            new ToneSpec("xr-activate", 560f, 0.15f, 1, 0.38f),
            new ToneSpec("xr-warning", 630f, 0.24f, 2, 0.34f),
            new ToneSpec("webgl-activate", 790f, 0.10f, 1, 0.14f),
            new ToneSpec("webgl-warning", 710f, 0.18f, 2, 0.10f),
            new ToneSpec("desktop-key-a", 1080f, 0.038f, 1, 0.03f),
            new ToneSpec("desktop-key-b", 1170f, 0.036f, 1, 0.03f),
            new ToneSpec("mobile-tap", 1240f, 0.048f, 1, 0.04f),
            new ToneSpec("mobile-warning", 820f, 0.17f, 2, 0.12f)
        };

        public static void RegenerateBundledDefaults()
        {
            EnsurePhysicalDirectory();
            Dictionary<string, AudioClip> clips = GenerateClips();
            DeucarianAudioRoleLibrary library = LoadOrCreate<DeucarianAudioRoleLibrary>(
                VirtualRoot + "/DefaultAudioRoleLibrary.asset");
            Dictionary<string, DeucarianAudioRole> roles = ConfigureRoles(library, clips);

            DeucarianAudioPalette fallback = ConfigureDefaultPalette(roles, clips);
            DeucarianAudioPalette xr = ConfigureExperiencePalette(
                "XR", library, roles, clips, DeucarianAudioExperience.XR);
            DeucarianAudioPalette webgl = ConfigureExperiencePalette(
                "WebGL", library, roles, clips, DeucarianAudioExperience.WebGL);
            DeucarianAudioPalette desktop = ConfigureExperiencePalette(
                "Desktop", library, roles, clips, DeucarianAudioExperience.Desktop);
            DeucarianAudioPalette mobile = ConfigureExperiencePalette(
                "Mobile", library, roles, clips, DeucarianAudioExperience.Mobile);

            DeucarianAudioPaletteSet set = LoadOrCreate<DeucarianAudioPaletteSet>(
                VirtualRoot + "/DefaultAudioPaletteSet.asset");
            set.Configure(fallback, new[]
            {
                Profile(DeucarianAudioExperience.XR, xr),
                Profile(DeucarianAudioExperience.WebGL, webgl),
                Profile(DeucarianAudioExperience.Desktop, desktop),
                Profile(DeucarianAudioExperience.Mobile, mobile)
            });
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Dictionary<string, AudioClip> GenerateClips()
        {
            Dictionary<string, AudioClip> result = new Dictionary<string, AudioClip>();
            for (int i = 0; i < Tones.Length; i++)
            {
                ToneSpec tone = Tones[i];
                string virtualPath = VirtualRoot + "/" + tone.Name + ".wav";
                string physicalPath = ResolvePhysicalPath(tone.Name + ".wav");
                File.WriteAllBytes(physicalPath, BuildWave(tone));
                AssetDatabase.ImportAsset(virtualPath, ImportAssetOptions.ForceSynchronousImport);
                result[tone.Name] = AssetDatabase.LoadAssetAtPath<AudioClip>(virtualPath);
            }

            return result;
        }

        private static Dictionary<string, DeucarianAudioRole> ConfigureRoles(
            DeucarianAudioRoleLibrary library,
            IReadOnlyDictionary<string, AudioClip> clips)
        {
            Dictionary<string, DeucarianAudioRole> result =
                new Dictionary<string, DeucarianAudioRole>(StringComparer.Ordinal);
            for (int i = 0; i < Roles.Length; i++)
            {
                RoleSpec spec = Roles[i];
                DeucarianAudioRole role = LoadOrCreate<DeucarianAudioRole>(
                    VirtualRoot + "/Role-" + spec.Name.Replace(" ", string.Empty) + ".asset");
                role.Configure(
                    spec.Id,
                    spec.Name,
                    spec.Category,
                    "Built-in semantic feedback role.",
                    DefaultCueFor(spec.Id, clips),
                    true);
                EditorUtility.SetDirty(role);
                library.AddRole(role);
                result[spec.Id] = role;
            }

            library.SortRolesByCategoryAndName();
            EditorUtility.SetDirty(library);
            return result;
        }

        private static DeucarianAudioPalette ConfigureDefaultPalette(
            IReadOnlyDictionary<string, DeucarianAudioRole> roles,
            IReadOnlyDictionary<string, AudioClip> clips)
        {
            DeucarianAudioPalette palette = LoadOrCreate<DeucarianAudioPalette>(
                VirtualRoot + "/DefaultAudioPalette.asset");
            DeucarianAudioRoleLibrary library = LoadOrCreate<DeucarianAudioRoleLibrary>(
                VirtualRoot + "/DefaultAudioRoleLibrary.asset");
            palette.Configure("deucarian.audio-palette.default", "Default", library);
            palette.ClearEntries();
            for (int i = 0; i < Roles.Length; i++)
            {
                RoleSpec spec = Roles[i];
                palette.SetCue(roles[spec.Id], DefaultCueFor(spec.Id, clips));
            }
            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static DeucarianAudioPalette ConfigureExperiencePalette(
            string name,
            DeucarianAudioRoleLibrary library,
            IReadOnlyDictionary<string, DeucarianAudioRole> roles,
            IReadOnlyDictionary<string, AudioClip> clips,
            DeucarianAudioExperience experience)
        {
            DeucarianAudioPalette palette = LoadOrCreate<DeucarianAudioPalette>(
                VirtualRoot + "/" + name + "AudioPalette.asset");
            palette.Configure("deucarian.audio-palette." + name.ToLowerInvariant(), name, library);
            palette.ClearEntries();

            if (experience == DeucarianAudioExperience.XR)
            {
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Activate], Cue(clips["xr-activate"], 0.34f));
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Warning], Cue(clips["xr-warning"], 0.30f));
            }
            else if (experience == DeucarianAudioExperience.WebGL)
            {
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Activate], Cue(clips["webgl-activate"], 0.38f));
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Warning], Cue(clips["webgl-warning"], 0.36f));
            }
            else if (experience == DeucarianAudioExperience.Desktop)
            {
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Key], new DeucarianAudioCue(
                    new[] { clips["desktop-key-a"], clips["desktop-key-b"] }, 0.25f, 0.97f, 1.03f));
            }
            else if (experience == DeucarianAudioExperience.Mobile)
            {
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Hover], DeucarianAudioCue.Silent());
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Press], Cue(clips["mobile-tap"], 0.28f));
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Activate], Cue(clips["mobile-tap"], 0.30f));
                palette.SetCue(roles[DeucarianBuiltinAudioRoleIds.Warning], Cue(clips["mobile-warning"], 0.34f));
            }

            EditorUtility.SetDirty(palette);
            return palette;
        }

        private static DeucarianAudioCue DefaultCueFor(
            string roleId,
            IReadOnlyDictionary<string, AudioClip> clips)
        {
            if (roleId == DeucarianBuiltinAudioRoleIds.Key)
            {
                return new DeucarianAudioCue(
                    new[] { clips["default-key-a"], clips["default-key-b"] },
                    0.25f,
                    0.97f,
                    1.03f);
            }

            if (roleId == DeucarianBuiltinAudioRoleIds.Warning) return Cue(clips["default-warning"], 0.36f);
            if (roleId == DeucarianBuiltinAudioRoleIds.Error || roleId == DeucarianBuiltinAudioRoleIds.Invalid)
                return Cue(clips["default-error"], 0.34f);
            if (roleId == DeucarianBuiltinAudioRoleIds.Cancel) return Cue(clips["default-cancel"], 0.30f);
            if (roleId == DeucarianBuiltinAudioRoleIds.Activate || roleId == DeucarianBuiltinAudioRoleIds.Submit ||
                roleId == DeucarianBuiltinAudioRoleIds.Success)
                return Cue(clips["default-confirm"], 0.34f);
            return Cue(clips["default-tap"], 0.28f);
        }

        private static DeucarianAudioCue Cue(AudioClip clip, float volume) =>
            new DeucarianAudioCue(clip, volume);

        private static DeucarianAudioPaletteProfile Profile(
            DeucarianAudioExperience experience,
            DeucarianAudioPalette palette)
        {
            DeucarianAudioPaletteProfile profile = new DeucarianAudioPaletteProfile();
            profile.Configure(experience, palette);
            return profile;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsurePhysicalDirectory()
        {
            Directory.CreateDirectory(ResolvePhysicalPath(string.Empty));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static string ResolvePhysicalPath(string fileName)
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(DeucarianAudioPalette).Assembly);
            if (package == null) throw new InvalidOperationException("Could not resolve the Theming package path.");
            return Path.Combine(
                package.resolvedPath,
                "Runtime", "Resources", "Deucarian", "Theming", "Audio", "Defaults", fileName);
        }

        private static byte[] BuildWave(ToneSpec tone)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(tone.Duration * sampleRate);
            byte[] bytes = new byte[44 + sampleCount * 2];
            WriteAscii(bytes, 0, "RIFF");
            WriteInt(bytes, 4, 36 + sampleCount * 2);
            WriteAscii(bytes, 8, "WAVEfmt ");
            WriteInt(bytes, 16, 16);
            WriteShort(bytes, 20, 1);
            WriteShort(bytes, 22, 1);
            WriteInt(bytes, 24, sampleRate);
            WriteInt(bytes, 28, sampleRate * 2);
            WriteShort(bytes, 32, 2);
            WriteShort(bytes, 34, 16);
            WriteAscii(bytes, 36, "data");
            WriteInt(bytes, 40, sampleCount * 2);

            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float normalized = i / (float)Mathf.Max(1, sampleCount - 1);
                float pulsePhase = normalized * Mathf.Max(1, tone.Pulses);
                float local = pulsePhase - Mathf.Floor(pulsePhase);
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(local));
                envelope *= envelope * (1f - normalized * 0.35f);
                float wave = Mathf.Sin(2f * Mathf.PI * tone.Frequency * time);
                wave += tone.Harmonic * Mathf.Sin(4f * Mathf.PI * tone.Frequency * time);
                short sample = (short)Mathf.RoundToInt(Mathf.Clamp(wave * envelope * 0.32f, -1f, 1f) * short.MaxValue);
                WriteShort(bytes, 44 + i * 2, sample);
            }

            return bytes;
        }

        private static void WriteAscii(byte[] target, int offset, string value)
        {
            for (int i = 0; i < value.Length; i++) target[offset + i] = (byte)value[i];
        }

        private static void WriteInt(byte[] target, int offset, int value)
        {
            target[offset] = (byte)value;
            target[offset + 1] = (byte)(value >> 8);
            target[offset + 2] = (byte)(value >> 16);
            target[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteShort(byte[] target, int offset, int value)
        {
            target[offset] = (byte)value;
            target[offset + 1] = (byte)(value >> 8);
        }
    }
}
