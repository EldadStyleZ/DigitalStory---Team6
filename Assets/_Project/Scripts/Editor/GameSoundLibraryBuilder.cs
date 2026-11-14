using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Makes the local <see cref="GameSoundLibrary"/> of plan section AC from the recorded sounds an owner of the packs has put
    /// in <c>Assets/SoundPacksLocal</c>: it reads the folder's recipe (one line per moment: volume, pitch jitter, cap, least
    /// gap, priority and clips), sets each clip's import settings and fills the library in the folder's Resources. The folder
    /// is untracked (the repo's local exclude file lists it), because the clips come from paid packs and the repo is public.
    /// Without the folder or its recipe it does nothing, so the game keeps its synthesised sounds.
    /// </summary>
    /// <remarks>
    /// Import settings: Vorbis at quality 0.6 and 48 kHz; one-shots mono and decompressed on load, the two hum loops in stereo
    /// and compressed in memory; every clip preloaded, never in the background. Works from the menu and from
    /// <c>-batchmode -executeMethod DigitalStory.Editor.GameSoundLibraryBuilder.Build</c>.
    /// </remarks>
    public static class GameSoundLibraryBuilder
    {
        private const string LocalFolder = "Assets/SoundPacksLocal";
        private const string ClipFolder = LocalFolder + "/Clips";
        private const string ResourcesFolder = LocalFolder + "/Resources";
        private const string RecipePath = LocalFolder + "/recipe.txt";
        private const string LibraryPath = ResourcesFolder + "/" + GameSoundLibrary.ResourceName + ".asset";
        private const string MasterLine = "Master";
        private const float VorbisQuality = 0.6f;
        private const uint SampleRate = 48000;
        private const int SoundFields = 7;

        /// <summary>Makes or refreshes the local library from the recipe. Does nothing when the local folder or its recipe is absent.</summary>
        [MenuItem("DigitalStory/Sound/Make Local Sound Library")]
        public static void Build()
        {
            try
            {
                if (!AssetDatabase.IsValidFolder(LocalFolder) || !File.Exists(RecipePath))
                {
                    Debug.Log($"[F010] {RecipePath} is not in this project, so no sound library was made; the game keeps its synthesised sounds.");
                    return;
                }

                CreateFolder(ResourcesFolder);
                GameSoundLibrary library = LoadOrCreateLibrary();
                library.ClearSlots();
                var report = new StringBuilder("[F010] Sound library made at ").Append(LibraryPath).Append(':');
                int sounds = 0;
                int clips = 0;
                string[] lines = File.ReadAllLines(RecipePath);
                for (int index = 0; index < lines.Length; index++)
                {
                    string line = lines[index].Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string[] fields = line.Split('|');
                    if (fields[0].Trim() == MasterLine)
                    {
                        library.SetMasterVolume(Number(fields, 1, index));
                        report.Append("\n  master volume ").Append(library.MasterVolume.ToString(CultureInfo.InvariantCulture));
                        continue;
                    }

                    GameSoundSlot slot = MakeSlot(fields, index, out GameSoundEvent sound);
                    library.SetSlot(sound, slot);
                    sounds++;
                    clips += slot.Clips.Length;
                    report.Append("\n  ").Append(sound).Append(": ").Append(slot.Clips.Length).Append(" clips, volume ")
                        .Append(slot.Volume.ToString(CultureInfo.InvariantCulture)).Append(", at most ").Append(slot.MostAtOnce);
                }

                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                Debug.Log(report.Append("\n  ").Append(sounds).Append(" sounds, ").Append(clips).Append(" clips.").ToString());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        // Event | volume | pitch jitter | most at once | least gap seconds | priority | clips under Clips/, comma separated.
        private static GameSoundSlot MakeSlot(string[] fields, int lineIndex, out GameSoundEvent sound)
        {
            if (fields.Length != SoundFields)
            {
                throw new FormatException($"{RecipePath} line {lineIndex + 1}: expected {SoundFields} fields separated by '|', found {fields.Length}.");
            }

            string name = fields[0].Trim();
            if (!Enum.TryParse(name, false, out sound) || !Enum.IsDefined(typeof(GameSoundEvent), sound))
            {
                throw new FormatException($"{RecipePath} line {lineIndex + 1}: '{name}' is not a GameSoundEvent.");
            }

            float volume = Mathf.Clamp01(Number(fields, 1, lineIndex));
            float jitter = Mathf.Clamp(Number(fields, 2, lineIndex), 0f, 0.5f);
            int mostAtOnce = Mathf.Max(1, Mathf.RoundToInt(Number(fields, 3, lineIndex)));
            float leastGap = Mathf.Max(0f, Number(fields, 4, lineIndex));
            int priority = Mathf.Clamp(Mathf.RoundToInt(Number(fields, 5, lineIndex)), 0, 256);
            bool isLoop = GameSoundChoice.IsLoop(sound);

            var clips = new List<AudioClip>();
            foreach (string part in fields[6].Split(','))
            {
                string file = part.Trim();
                if (file.Length == 0)
                {
                    continue;
                }

                string path = ClipFolder + "/" + file;
                SetImport(path, isLoop, lineIndex);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    throw new FileNotFoundException($"{RecipePath} line {lineIndex + 1}: no audio clip at {path}.");
                }

                clips.Add(clip);
            }

            if (clips.Count == 0)
            {
                throw new FormatException($"{RecipePath} line {lineIndex + 1}: {sound} names no clip.");
            }

            return new GameSoundSlot(clips.ToArray(), volume, jitter, mostAtOnce, leastGap, priority);
        }

        private static void SetImport(string path, bool isLoop, int lineIndex)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                throw new FileNotFoundException($"{RecipePath} line {lineIndex + 1}: no audio file at {path}.");
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            AudioClipLoadType loadType = isLoop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            bool isChanged = importer.forceToMono == isLoop
                || importer.loadInBackground
                || !settings.preloadAudioData
                || settings.loadType != loadType
                || settings.compressionFormat != AudioCompressionFormat.Vorbis
                || !Mathf.Approximately(settings.quality, VorbisQuality)
                || settings.sampleRateSetting != AudioSampleRateSetting.OverrideSampleRate
                || settings.sampleRateOverride != SampleRate;
            if (!isChanged)
            {
                return;
            }

            importer.forceToMono = !isLoop;
            importer.loadInBackground = false;
            settings.preloadAudioData = true;
            settings.loadType = loadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = VorbisQuality;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = SampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static float Number(string[] fields, int field, int lineIndex)
        {
            string text = field < fields.Length ? fields[field].Trim() : string.Empty;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                throw new FormatException($"{RecipePath} line {lineIndex + 1}: field {field + 1} '{text}' is not a number.");
            }

            return value;
        }

        private static GameSoundLibrary LoadOrCreateLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<GameSoundLibrary>(LibraryPath);
            if (library != null)
            {
                return library;
            }

            library = ScriptableObject.CreateInstance<GameSoundLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        private static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
