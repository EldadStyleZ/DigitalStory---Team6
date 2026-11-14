using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DigitalStory.Data;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.Editor
{
    /// <summary>
    /// Makes the local <see cref="GameSoundLibrary"/> of plan section AC from the clips in the untracked folder
    /// <c>Assets/SoundPacksLocal</c>, for someone who owns the sound packs. The clips and the library stay out of git (the
    /// repo's local exclude file lists the folder), because the packs are paid and the repo is public. Without the folder it
    /// does nothing, so the game keeps its own sounds.
    /// </summary>
    /// <remarks>
    /// The choice lives in the folder's <c>recipe.txt</c>, one line per moment:
    /// <c>Event | volume | pitch jitter | most at once | least gap seconds | clip, clip</c>, clips under <c>Clips/</c>. Each
    /// clip's import is set here: Vorbis, 48 kHz, preloaded, decompressed on load for a short one-shot and compressed in
    /// memory for a loop. Works from the menu and from
    /// <c>-batchmode -executeMethod DigitalStory.Editor.GameSoundLibraryBuilder.Build</c>.
    /// </remarks>
    public static class GameSoundLibraryBuilder
    {
        private const string LocalFolder = "Assets/SoundPacksLocal";
        private const string ClipFolder = LocalFolder + "/Clips";
        private const string ResourcesFolder = LocalFolder + "/Resources";
        private const string RecipePath = LocalFolder + "/recipe.txt";
        private const string LibraryPath = ResourcesFolder + "/" + GameSoundLibrary.ResourceName + ".asset";
        private const uint SampleRate = 48000;
        private const float OneShotQuality = 0.6f;
        private const float LoopQuality = 0.5f;
        private const int FieldCount = 6;

        /// <summary>Makes or refreshes the local library from the recipe. Does nothing when the local folder is absent.</summary>
        [MenuItem("DigitalStory/Sound/Make Local Sound Library")]
        public static void Build()
        {
            try
            {
                if (!AssetDatabase.IsValidFolder(LocalFolder) || !File.Exists(RecipePath))
                {
                    Debug.Log($"[F010] {RecipePath} is not in this project, so no sound library was made; the game keeps its own sounds.");
                    return;
                }

                if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                {
                    AssetDatabase.CreateFolder(LocalFolder, "Resources");
                }

                GameSoundLibrary library = LoadOrCreateLibrary();
                var report = new StringBuilder("[F010] Sound library made at ").Append(LibraryPath).Append(':');
                foreach (string line in File.ReadAllLines(RecipePath))
                {
                    ReadLine(line, library, report);
                }

                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                Debug.Log(report.ToString());
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

        private static void ReadLine(string line, GameSoundLibrary library, StringBuilder report)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return;
            }

            string[] fields = trimmed.Split('|');
            if (fields.Length != FieldCount || !Enum.TryParse(fields[0].Trim(), out GameSoundEvent kind))
            {
                throw new FormatException($"{RecipePath}: cannot read the line \"{trimmed}\".");
            }

            float volume = Number(fields[1]);
            float jitter = Number(fields[2]);
            int mostAtOnce = (int)Number(fields[3]);
            float leastGap = Number(fields[4]);
            bool isLoop = kind == GameSoundEvent.SwarmLoop;
            var clips = new List<AudioClip>();
            foreach (string name in fields[5].Split(','))
            {
                string path = ClipFolder + "/" + name.Trim();
                if (!SetImport(path, isLoop))
                {
                    Debug.LogWarning($"[F010] {path} is missing, so {kind} leaves it out.");
                    continue;
                }

                clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }

            library.SetSlot(kind, new GameSoundSlot(clips.ToArray(), volume, jitter, Math.Max(1, mostAtOnce), leastGap));
            report.Append("\n  ").Append(kind).Append(": ").Append(clips.Count).Append(" clips, volume ").Append(volume);
        }

        private static float Number(string field) => float.Parse(field.Trim(), CultureInfo.InvariantCulture);

        private static bool SetImport(string path, bool isLoop)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                return false;
            }

            importer.forceToMono = !isLoop;
            importer.loadInBackground = false;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = isLoop ? LoopQuality : OneShotQuality;
            settings.loadType = isLoop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = SampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            return true;
        }
    }
}
