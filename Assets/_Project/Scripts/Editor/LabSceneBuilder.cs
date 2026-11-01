using System;
using System.IO;
using DigitalStory.Data;
using DigitalStory.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
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
        private const string HudConfigPath = Root + "/GameData/HudConfig.asset";
        private const string PanelPath = Root + "/UI/GamePanelSettings.asset";
        private const string ScenePath = Root + "/Scenes/eldad-lab-scene.unity";
        private const string ArrowPath = Root + "/Art/Placeholders/Arrow.png";
        private const int PiperOrder = 5;
        private const int ArrowPixels = 64;
        private const float ArrowShaftThickness = 0.3f;
        private const float ArrowShaftEnd = 0.45f;
        private const float ArrowStart = 0.05f;
        private const float ArrowTip = 0.95f;

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

            var units = new GameObject("Units", typeof(LabUnitsView));
            var effects = new GameObject("Effects", typeof(LabEffectsView));
            var sound = new GameObject("Sound Effects", typeof(AudioSource), typeof(LabSoundView));
            sound.GetComponent<AudioSource>().playOnAwake = false;

            var hud = new GameObject("Hud", typeof(UIDocument), typeof(LabHudView), typeof(ComboBalloonView), typeof(LabScreensView));
            hud.GetComponent<UIDocument>().panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);

            var root = new GameObject("Lab Root", typeof(LabRoot), typeof(LabRunner));
            var serialized = new SerializedObject(root.GetComponent<LabRoot>());
            SetObject(serialized, "_maze", LoadOrCreate<MazeConfig>("MazeConfig"));
            SetObject(serialized, "_player", LoadOrCreate<LabPlayerConfig>("LabPlayerConfig"));
            SetObject(serialized, "_stamina", LoadOrCreate<StaminaConfig>("StaminaConfig"));
            SetObject(serialized, "_combos", LoadOrCreate<ComboConfig>("ComboConfig"));
            SetObject(serialized, "_levels", LoadOrCreate<LevelConfig>("LevelConfig"));
            SetObject(serialized, "_battle", LoadOrCreate<BattleConfig>("BattleConfig"));
            SetObject(serialized, "_roster", LoadOrCreate<CreatureRoster>("CreatureRoster"));
            SetObject(serialized, "_population", LoadOrCreate<PopulationConfig>("PopulationConfig"));
            SetObject(serialized, "_ambush", LoadOrCreate<AmbushConfig>("AmbushConfig"));
            SetObject(serialized, "_sprites", sprites);
            SetObject(serialized, "_art", LoadArt());
            SetObject(serialized, "_hud", AssetDatabase.LoadAssetAtPath<HudConfig>(HudConfigPath));
            SetObject(serialized, "_texts", LoadOrCreate<LabTextConfig>("LabTextConfig"));
            SetObject(serialized, "_sound", LoadOrCreate<LabSoundConfig>("LabSoundConfig"));
            SetObject(serialized, "_runner", root.GetComponent<LabRunner>());
            SetObject(serialized, "_mazeView", maze.GetComponent<MazeView>());
            SetObject(serialized, "_piperView", piper.GetComponent<LabPiperView>());
            SetObject(serialized, "_camera", camera.GetComponent<LabCamera>());
            SetObject(serialized, "_unitsView", units.GetComponent<LabUnitsView>());
            SetObject(serialized, "_effectsView", effects.GetComponent<LabEffectsView>());
            SetObject(serialized, "_hudView", hud.GetComponent<LabHudView>());
            SetObject(serialized, "_balloonView", hud.GetComponent<ComboBalloonView>());
            SetObject(serialized, "_screensView", hud.GetComponent<LabScreensView>());
            SetObject(serialized, "_soundView", sound.GetComponent<LabSoundView>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LabArtConfig LoadArt()
        {
            var art = LoadOrCreate<LabArtConfig>("LabArtConfig");
            var serialized = new SerializedObject(art);
            SetObject(serialized, $"<{nameof(LabArtConfig.ArrowSprite)}>k__BackingField", SaveArrow());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return art;
        }

        private static Sprite SaveArrow()
        {
            var texture = new Texture2D(ArrowPixels, ArrowPixels, TextureFormat.RGBA32, false);

            for (int y = 0; y < ArrowPixels; y++)
            {
                for (int x = 0; x < ArrowPixels; x++)
                {
                    float across = (x + 0.5f) / ArrowPixels;
                    float up = Mathf.Abs(((y + 0.5f) / ArrowPixels) - 0.5f) * 2f;
                    texture.SetPixel(x, y, IsOnTheArrow(across, up) ? Color.white : Color.clear);
                }
            }

            File.WriteAllBytes(ArrowPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ArrowPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(ArrowPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ArrowPixels;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
        }

        // across runs from tail to tip, and up is the distance from the arrow's middle line.
        private static bool IsOnTheArrow(float across, float up)
        {
            bool isShaft = across >= ArrowStart && across < ArrowShaftEnd && up <= ArrowShaftThickness;
            bool isHead = across >= ArrowShaftEnd && across <= ArrowTip && up <= (ArrowTip - across) / (ArrowTip - ArrowShaftEnd);
            return isShaft || isHead;
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
