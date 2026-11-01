using System;
using System.IO;
using DigitalStory.Data;
using DigitalStory.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DigitalStory.Editor
{
    /// <summary>Creates the lab's config assets and scene from code.</summary>
    /// <remarks>Scene and asset files must not be edited by hand, and it keeps existing assets, so tuned values survive.</remarks>
    public static class LabSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string LabData = Root + "/GameData/Lab";
        private const string TownArtPath = Root + "/GameData/TownArtConfig.asset";
        private const string ScenePath = Root + "/Scenes/eldad-lab-scene.unity";
        private const int PiperOrder = 5;

        /// <summary>Builds everything the lab's scene needs.</summary>
        [MenuItem("DigitalStory/Build Lab Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Directory.CreateDirectory(LabData);
            AssetDatabase.Refresh();

            // Starting a new scene unloads assets that nothing uses, so it must come before loading them.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            FillScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
            AssetDatabase.SaveAssets();
        }

        private static void FillScene()
        {
            var sprites = AssetDatabase.LoadAssetAtPath<TownArtConfig>(TownArtPath);

            if (sprites == null)
            {
                throw new InvalidOperationException($"Build the Town scene first: {TownArtPath} is missing.");
            }

            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(LabCamera));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;

            var maze = new GameObject("Maze", typeof(Grid), typeof(MazeView));

            var piper = new GameObject("Piper", typeof(SpriteRenderer), typeof(LabPiperView));
            var piperRenderer = piper.GetComponent<SpriteRenderer>();
            piperRenderer.sprite = sprites.PiperSprite;
            piperRenderer.sortingOrder = PiperOrder;

            var root = new GameObject("Lab Root", typeof(LabRoot), typeof(LabRunner));
            var serialized = new SerializedObject(root.GetComponent<LabRoot>());
            SetObject(serialized, "_maze", LoadOrCreate<MazeConfig>("MazeConfig"));
            SetObject(serialized, "_player", LoadOrCreate<LabPlayerConfig>("LabPlayerConfig"));
            SetObject(serialized, "_sprites", sprites);
            SetObject(serialized, "_art", LoadOrCreate<LabArtConfig>("LabArtConfig"));
            SetObject(serialized, "_runner", root.GetComponent<LabRunner>());
            SetObject(serialized, "_mazeView", maze.GetComponent<MazeView>());
            SetObject(serialized, "_piperView", piper.GetComponent<LabPiperView>());
            SetObject(serialized, "_camera", camera.GetComponent<LabCamera>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadOrCreate<T>(string assetName) where T : ScriptableObject
        {
            string path = $"{LabData}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void SetObject(SerializedObject target, string propertyName, Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException($"{target.targetObject.name} has no field named {propertyName}.");
            }

            property.objectReferenceValue = value != null
                ? value
                : throw new InvalidOperationException($"Nothing to put in {propertyName} on {target.targetObject.name}.");
        }

        private static void AddSceneToBuild()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (Array.Exists(scenes, scene => scene.path == ScenePath))
            {
                return;
            }

            Array.Resize(ref scenes, scenes.Length + 1);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = scenes;
        }
    }
}
