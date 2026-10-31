using System;
using System.IO;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DigitalStory.Editor
{
    /// <summary>Creates the config assets, placeholder art and Town scene from code.</summary>
    /// <remarks>
    /// Scene and asset files must not be edited by hand, and this rebuilds them the same way for everyone.
    /// It keeps existing assets, so tuned values and swapped art survive a rebuild of the scene.
    /// </remarks>
    public static class TownSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string GameData = Root + "/GameData";
        private const string SongsFolder = "Songs";
        private const string Placeholders = Root + "/Art/Placeholders";
        private const string UiFolder = Root + "/UI";
        private const string ScenePath = Root + "/Scenes/Town.unity";

        private const int BlockSize = 4;
        private const int RatWidth = 48;
        private const int RatHeight = 24;
        private const int PiperSize = 72;
        private const int CatWidth = 92;
        private const int CatHeight = 40;
        private const int PiperOrder = 4;
        private const int DetailedPixelsPerUnit = 200;
        private const float PanelWidthHeightBalance = 0.5f;

        private static readonly Vector2Int PanelReferenceSize = new Vector2Int(960, 540);

        private static readonly Color CameraBackground = new Color32(0x0D, 0x0F, 0x18, 0xFF);
        private static readonly Color Cloak = new Color32(0xC8, 0x35, 0x2E, 0xFF);
        private static readonly Color Trim = new Color32(0xE8, 0xC5, 0x47, 0xFF);
        private static readonly Color Flute = new Color32(0xC9, 0x94, 0x4F, 0xFF);

        /// <summary>Builds everything the Town scene needs.</summary>
        [MenuItem("DigitalStory/Build Town Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            CreateFolders();

            // Starting a new scene unloads assets that nothing uses, so it must come before loading them.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var night = LoadOrCreate<NightConfig>("NightConfig");
            var town = LoadOrCreate<TownConfig>("TownConfig");
            var piper = LoadOrCreate<PiperConfig>("PiperConfig");
            var breath = LoadOrCreate<BreathConfig>("BreathConfig");
            var flute = LoadOrCreate<FluteConfig>("FluteConfig");
            var rats = LoadOrCreate<RatConfig>("RatConfig");
            var cats = LoadOrCreate<CatConfig>("CatConfig");
            var songConfig = LoadOrCreate<SongConfig>("SongConfig");
            SongEffect[] songs = LoadOrCreateSongs();
            var endings = LoadOrCreate<EndingTextConfig>("EndingTextConfig");
            TownArtConfig art = LoadOrCreateArt();
            PanelSettings panel = LoadOrCreatePanelSettings();

            FillScene(new SceneAssets(night, town, piper, breath, flute, rats, cats, songConfig, songs, art, endings, panel));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
            AssetDatabase.SaveAssets();
        }

        private static void CreateFolders()
        {
            Directory.CreateDirectory($"{GameData}/{SongsFolder}");
            Directory.CreateDirectory(Placeholders);
            Directory.CreateDirectory(UiFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            AssetDatabase.Refresh();
        }

        private static T LoadOrCreate<T>(string assetName) where T : ScriptableObject
        {
            string path = $"{GameData}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        // The order here is the order on the combos row.
        private static SongEffect[] LoadOrCreateSongs()
        {
            return new SongEffect[]
            {
                LoadOrCreate<LullabyEffect>($"{SongsFolder}/LullabySong"),
                LoadOrCreate<DanceEffect>($"{SongsFolder}/DanceSong"),
                LoadOrCreate<CongaEffect>($"{SongsFolder}/CongaSong"),
                LoadOrCreate<BalloonsEffect>($"{SongsFolder}/BalloonsSong"),
                LoadOrCreate<DiscoEffect>($"{SongsFolder}/DiscoSong"),
                LoadOrCreate<HeartEffect>($"{SongsFolder}/HeartSong"),
                LoadOrCreate<FaintEffect>($"{SongsFolder}/FaintSong"),
                LoadOrCreate<CrowdSurfEffect>($"{SongsFolder}/CrowdSurfSong"),
                LoadOrCreate<ChoirEffect>($"{SongsFolder}/ChoirSong"),
            };
        }

        // Only fills in what is missing, so art swapped into the asset survives a rebuild.
        private static TownArtConfig LoadOrCreateArt()
        {
            var art = LoadOrCreate<TownArtConfig>("TownArtConfig");
            var serialized = new SerializedObject(art);

            if (art.BlockSprite == null)
            {
                Sprite block = SavePlaceholder("Block", BlockSize, BlockSize, PaintBlock, BlockSize);
                SetObject(serialized, BackingField(nameof(TownArtConfig.BlockSprite)), block);
            }

            if (art.RatSprite == null)
            {
                Sprite rat = SavePlaceholder("Rat", RatWidth, RatHeight, PaintRat, DetailedPixelsPerUnit);
                SetObject(serialized, BackingField(nameof(TownArtConfig.RatSprite)), rat);
            }

            if (art.PiperSprite == null)
            {
                Sprite piper = SavePlaceholder("Piper", PiperSize, PiperSize, PaintPiper, DetailedPixelsPerUnit);
                SetObject(serialized, BackingField(nameof(TownArtConfig.PiperSprite)), piper);
            }

            if (art.CatSprite == null)
            {
                Sprite cat = SavePlaceholder("Cat", CatWidth, CatHeight, PaintCat, DetailedPixelsPerUnit);
                SetObject(serialized, BackingField(nameof(TownArtConfig.CatSprite)), cat);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return art;
        }

        private static Sprite SavePlaceholder(
            string spriteName,
            int width,
            int height,
            Func<int, int, int, int, Color> paint,
            int pixelsPerUnit)
        {
            string path = $"{Placeholders}/{spriteName}.png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, paint(x, y, width, height));
                }
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color PaintBlock(int x, int y, int width, int height) => Color.white;

        private static Color PaintRat(int x, int y, int width, int height)
        {
            bool isBody = IsInEllipse(x, y, width * 0.42f, height * 0.5f, width * 0.32f, height * 0.42f);
            bool isHead = IsInEllipse(x, y, width * 0.78f, height * 0.5f, width * 0.17f, height * 0.3f);
            bool isTail = y == height / 2 && x < width * 0.14f;
            return isBody || isHead || isTail ? Color.white : Color.clear;
        }

        private static Color PaintCat(int x, int y, int width, int height)
        {
            bool isBody = IsInEllipse(x, y, width * 0.46f, height * 0.5f, width * 0.27f, height * 0.36f);
            bool isHead = IsInEllipse(x, y, width * 0.78f, height * 0.5f, width * 0.13f, height * 0.3f);
            bool isEar = IsInEllipse(x, y, width * 0.84f, height * 0.5f, width * 0.05f, height * 0.46f);
            bool isTail = Mathf.Abs(y - (height * 0.5f)) < 2f && x < width * 0.24f;
            return isBody || isHead || isEar || isTail ? Color.white : Color.clear;
        }

        private static Color PaintPiper(int x, int y, int width, int height)
        {
            bool isCloak = IsInEllipse(x, y, width * 0.42f, height * 0.5f, width * 0.3f, height * 0.4f);
            bool isHat = IsInEllipse(x, y, width * 0.46f, height * 0.5f, width * 0.14f, height * 0.14f);
            bool isFlute = x > width * 0.55f && x < width * 0.98f && Mathf.Abs(y - (height * 0.42f)) < 3f;

            if (isFlute)
            {
                return Flute;
            }

            if (isHat)
            {
                return Trim;
            }

            return isCloak ? Cloak : Color.clear;
        }

        private static bool IsInEllipse(int x, int y, float centreX, float centreY, float radiusX, float radiusY)
        {
            float offsetX = (x - centreX) / radiusX;
            float offsetY = (y - centreY) / radiusY;
            return (offsetX * offsetX) + (offsetY * offsetY) <= 1f;
        }

        private static PanelSettings LoadOrCreatePanelSettings()
        {
            string path = $"{UiFolder}/GamePanelSettings.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);

            if (panel != null)
            {
                return panel;
            }

            string themePath = $"{UiFolder}/GameTheme.tss";
            File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
            AssetDatabase.ImportAsset(themePath);

            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = PanelReferenceSize;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = PanelWidthHeightBalance;
            AssetDatabase.CreateAsset(panel, path);
            return panel;
        }

        private static void FillScene(SceneAssets assets)
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(TownCamera));
            camera.tag = "MainCamera";
            var cameraComponent = camera.GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = CameraBackground;

            var piperObject = new GameObject("Piper", typeof(SpriteRenderer), typeof(PiperView));
            var piperRenderer = piperObject.GetComponent<SpriteRenderer>();
            piperRenderer.sprite = assets.Art.PiperSprite;
            piperRenderer.sortingOrder = PiperOrder;

            var townObject = new GameObject("Town", typeof(TownView));
            var ratsObject = new GameObject("Rats", typeof(RatSwarmView));
            var catsObject = new GameObject("Cats", typeof(CatView));

            var hud = new GameObject("Night Status", typeof(UIDocument), typeof(NightStatusView), typeof(ComboRowView));
            hud.GetComponent<UIDocument>().panelSettings = assets.Panel;

            var root = new GameObject("Game Root", typeof(GameRoot), typeof(PiperInput), typeof(NightRunner));
            var serialized = new SerializedObject(root.GetComponent<GameRoot>());
            SetObject(serialized, "_night", assets.Night);
            SetObject(serialized, "_town", assets.Town);
            SetObject(serialized, "_piper", assets.Piper);
            SetObject(serialized, "_breath", assets.Breath);
            SetObject(serialized, "_flute", assets.Flute);
            SetObject(serialized, "_rats", assets.Rats);
            SetObject(serialized, "_cats", assets.Cats);
            SetObject(serialized, "_songConfig", assets.SongConfig);
            SetObjects(serialized, "_songs", assets.Songs);
            SetObject(serialized, "_art", assets.Art);
            SetObject(serialized, "_endingTexts", assets.Endings);
            SetObject(serialized, "_input", root.GetComponent<PiperInput>());
            SetObject(serialized, "_runner", root.GetComponent<NightRunner>());
            SetObject(serialized, "_townView", townObject.GetComponent<TownView>());
            SetObject(serialized, "_piperView", piperObject.GetComponent<PiperView>());
            SetObject(serialized, "_swarmView", ratsObject.GetComponent<RatSwarmView>());
            SetObject(serialized, "_catView", catsObject.GetComponent<CatView>());
            SetObject(serialized, "_townCamera", camera.GetComponent<TownCamera>());
            SetObject(serialized, "_statusView", hud.GetComponent<NightStatusView>());
            SetObject(serialized, "_comboRow", hud.GetComponent<ComboRowView>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static string BackingField(string propertyName) => $"<{propertyName}>k__BackingField";

        private static void SetObject(SerializedObject target, string propertyName, Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException($"{target.targetObject.name} has no field named {propertyName}.");
            }

            if (value == null)
            {
                throw new InvalidOperationException($"Nothing to put in {propertyName} on {target.targetObject.name}.");
            }

            property.objectReferenceValue = value;
        }

        private static void SetObjects(SerializedObject target, string propertyName, Object[] values)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            property.arraySize = values.Length;

            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private readonly struct SceneAssets
        {
            public SceneAssets(
                NightConfig night,
                TownConfig town,
                PiperConfig piper,
                BreathConfig breath,
                FluteConfig flute,
                RatConfig rats,
                CatConfig cats,
                SongConfig songConfig,
                SongEffect[] songs,
                TownArtConfig art,
                EndingTextConfig endings,
                PanelSettings panel)
            {
                Night = night;
                Town = town;
                Piper = piper;
                Breath = breath;
                Flute = flute;
                Rats = rats;
                Cats = cats;
                SongConfig = songConfig;
                Songs = songs;
                Art = art;
                Endings = endings;
                Panel = panel;
            }

            public NightConfig Night { get; }
            public TownConfig Town { get; }
            public PiperConfig Piper { get; }
            public BreathConfig Breath { get; }
            public FluteConfig Flute { get; }
            public RatConfig Rats { get; }
            public CatConfig Cats { get; }
            public SongConfig SongConfig { get; }
            public SongEffect[] Songs { get; }
            public TownArtConfig Art { get; }
            public EndingTextConfig Endings { get; }
            public PanelSettings Panel { get; }
        }
    }
}
