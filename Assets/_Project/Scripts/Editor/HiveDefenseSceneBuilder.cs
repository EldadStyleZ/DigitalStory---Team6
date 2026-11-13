using System;
using System.Collections.Generic;
using System.Text;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace DigitalStory.Editor
{
    /// <summary>Creates every F-010 asset, the prefab variants and the hive defense scene from code.</summary>
    /// <remarks>
    /// Scene and asset files must not be edited by hand. Config assets are kept when they exist, so tuned values survive a
    /// rebuild. The controller, the prefab variants and the scene are made again each time. Raz's art is never changed.
    /// Works from the menu and from <c>-batchmode -executeMethod DigitalStory.Editor.HiveDefenseSceneBuilder.Build</c>.
    /// </remarks>
    public static class HiveDefenseSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string DataFolder = Root + "/GameData/F010";
        private const string ArtFolder = Root + "/Art/F010";
        private const string PrefabFolder = Root + "/Prefabs/F010";
        private const string ScenePath = Root + "/Scenes/eldad-lab-2-scene.unity";

        private const string RazPrefabFolder = Root + "/Art/prefabs";
        private const string BeekeeperSourcePath = RazPrefabFolder + "/Beekeeper_LowPoly.prefab";
        private const string HiveSourcePath = RazPrefabFolder + "/Beehive_LowPoly 1.prefab";
        private const string NestTreeSourcePath = RazPrefabFolder + "/WaspNestTree_LowPoly 1.prefab";
        private const string NestObjectName = "WaspNest"; // The nest in Raz's tree, which NestView lights and hides.
        private const string NestEmissionProperty = "_EmissionColor"; // What NestView pulses on the nest, and BeekeeperView flashes.
        private const string EmissionKeyword = "_EMISSION";
        private const string ShieldFlashShaderName = "Sprites/Default";
        private const string ShieldFlashMaterialName = "ShieldFlash";
        private const string BeekeeperFbxPath = Root + "/Art/model/Beekeeper_LowPoly.fbx";
        private const string MixamoFolder = Root + "/Art/Mixamo";
        private const string ControllerPath = ArtFolder + "/Beekeeper.controller";
        private const string ClipFolder = ArtFolder + "/Clips";
        private const string ColossusControllerPath = ArtFolder + "/Colossus.controller";
        private const string ComputePath = ArtFolder + "/Shaders/WaspSwarm.compute";
        private const string WaspShaderPath = ArtFolder + "/Shaders/WaspInstanced.shader";
        private const string GlowShaderPath = ArtFolder + "/Shaders/WaspGlow.shader";
        private const string SkyShaderPath = ArtFolder + "/Shaders/NightSky.shader";
        private const string SkyMaterialName = "NightSky";
        private const string PanelSettingsPath = Root + "/UI/GamePanelSettings.asset";
        private const string HudUxmlPath = Root + "/UI/F010/HiveDefenseHud.uxml";
        private const string SoundConfigPath = Root + "/GameData/SoundConfig.asset"; // The Town scene's, shared and never changed here.
        private const int FluteHoleCount = 4; // H J K L (songs-v2, section D).

        private const string SpeedParameter = "Speed";
        private const float MovingThreshold = 0.1f;
        private const float BlendSeconds = 0.1f;
        private const float AttackExitTime = 0.95f;
        private const string PunchTrigger = "Punch";
        private const string HeavyPunchTrigger = "HeavyPunch";
        private const string KickTrigger = "Kick";
        private const string HitTrigger = "Hit";

        private const string IdleClip = "Idle";
        private const string RunClip = "Run";

        // The core's CharacterController: a capsule the size of a person, so the core keeps to the paths a person could walk.
        private const float CoreHeight = 1.8f;
        private const float CoreRadius = 0.35f;
        private const float BeekeeperHeight = 1.8f;
        private const float HiveHeight = 1.6f;
        private const float HiveBarGap = 0.6f;

        private const float GroundSize = 160f; // Past the scenery to the horizon, so a low camera never sees its edge.
        private const float PlaneBaseSize = 10f;
        private const float ClearHalfSize = 25f;
        private const float EdgeHalfSize = 34f;
        private const float HiveSpacing = 25f;
        private const float SpawnDistance = 32f;
        private const float SpawnClearance = 4f;
        private const int SceneryCount = 70;
        private const int SceneryAttempts = 30;
        private const int SceneSeed = 1010;

        // The nest trees (plan section H). NestConfig holds the rules every layout keeps; these only choose one of the many
        // layouts that keep them. The nests stay inside the clear middle, short of the scenery around the edge, and apart
        // from each other and the spawn points, so no two collect circles touch and no beekeeper enters under a nest.
        private const float NestTreeHeight = 5f;

        // Props at least this tall block the chase camera's view of the giant; the bushes below it are looked over.
        private const float CameraBlockerLeastHeight = 2f;
        private const float NestInsideHalfSize = 24f;
        private const float NestSpacing = 8f;
        private const float NestSpawnClearance = 8f;
        private const float NestHiveMargin = 0.5f;
        // Degrees off straight ahead (+Z), one nest to each side, so both are in the opening view and neither tree stands
        // between the camera and a core walking straight ahead to the north spawn.
        private const float FirstNestsNarrowestBearing = 20f;
        private const float FirstNestsWidestBearing = 35f;
        private const float LaterNestJitterDegrees = 10f;
        private const int NestAttempts = 2000;

        private const string RightHandName = "RightHand";
        private const string FluteName = "Flute";

        // The night look. Colours are in gamma space, as the project is.
        private const float MoonlightIntensity = 0.6f;
        private const float MoonlightShadowStrength = 0.6f;
        private const float MoonSize = 0.13f;
        private const float StarDensity = 90f;
        private const float SkyHorizonWidth = 0.3f;
        private const float HiveLightHeight = HiveHeight * 0.75f;
        private const float HiveLightFront = 1.6f; // Metres toward the camera, which looks along +Z, so the hive's near side is lit.
        private const float HiveLightRange = 7f;
        private const float HiveLightIntensity = 2.2f;
        private const bool FogOn = true;
        private const float FogDensity = 0.016f;

        // From the world toward the moon. The light comes from high up, so the ground is lit and the shadows stay short;
        // the moon in the sky sits low on the same side, so a low camera sees it over the horizon, as in the pictures.
        private static readonly Vector3 MoonlightDirection = new Vector3(-0.25f, 0.75f, 0.65f).normalized;
        private static readonly Vector3 MoonDirection = new Vector3(-0.354f, 0.156f, 0.922f).normalized;
        private static readonly Color Grass = new Color(0.25f, 0.27f, 0.13f, 1f);
        private static readonly Color MoonLightColor = new Color(0.72f, 0.8f, 1f, 1f);
        private static readonly Color SkyZenith = new Color(0.04f, 0.08f, 0.24f, 1f);
        private static readonly Color SkyHorizon = new Color(0.22f, 0.15f, 0.34f, 1f);
        private static readonly Color SkyGround = new Color(0.07f, 0.08f, 0.12f, 1f);
        private static readonly Color FogColor = new Color(0.07f, 0.08f, 0.12f, 1f); // The sky below the horizon, so the far ground fades into it.
        private static readonly Color HiveLightColor = new Color(1f, 0.72f, 0.35f, 1f);
        private static readonly Color SuitWhite = new Color(0.94f, 0.94f, 0.9f, 1f);
        private static readonly Color StickWood = new Color(0.4f, 0.26f, 0.13f, 1f);
        private static readonly Color SmokerMetal = new Color(0.45f, 0.47f, 0.5f, 1f);
        private static readonly Color AmbientSky = new Color(0.16f, 0.19f, 0.32f, 1f);
        private static readonly Color AmbientEquator = new Color(0.11f, 0.12f, 0.19f, 1f);
        private static readonly Color AmbientGround = new Color(0.05f, 0.05f, 0.08f, 1f);

        // Every config and song asset BuildAll loads or creates. The baked ColossusSampleSet is not a config.
        private static readonly string[] ConfigAssetNames =
        {
            "SwarmConfig", "SquadConfig", "AimConfig", "CoreMoveConfig", "FuryConfig", "ColossusConfig", "HiveConfig", "NestConfig",
            "SwarmLevelConfig", "SmokeConfig", "WaveConfig", "F010SongConfig", "StickBeekeeper", "SmokerBeekeeper",
            "StingSong", "TornadoSong", "MeteorSong", "HydraSong", "ScytheSong", "LanceSong", "NestSong", "DomeSong", "GustSong",
            "HoneycombSong", "HiveCallSong", "ColossusSong", "GamepadConfig",
        };

        // Assets an older build made under names that no longer fit: each moves to its new path, keeping its GUID and any
        // tuned values, when nothing is there yet.
        private static readonly (string From, string To)[] RenamedAssets =
        {
            (DataFolder + "/PiperMoveConfig.asset", DataFolder + "/CoreMoveConfig.asset"),
            (ArtFolder + "/PiperSwarm.controller", ControllerPath),
        };

        // Assets an older build made that only served the Piper, who has gone: the swarm's core has no model.
        private static readonly string[] RetiredAssets =
        {
            PrefabFolder + "/F010Piper.prefab",
            ClipFolder + "/PlayFluteTwoHands.anim",
            ClipFolder + "/RunFluteTwoHands.anim",
        };

        // Prefab name, target height in metres.
        private static readonly (string Name, float Height)[] Scenery =
        {
            ("Tree_Pine", 9f),
            ("Tree_Pine", 9f),
            ("Tree_Dead", 6.5f),
            ("Bush_Cone", 1.4f),
            ("Bush_Flower", 1f),
            ("Bush_Lavender", 0.9f),
            ("Bush_Round", 1.2f),
            ("Ruin_Arcade", 4.5f),
            ("Ruin_Arch_Gate", 5f),
            ("Ruin_House_B", 4.5f),
            ("Ruin_Wall_Window", 3.5f),
            ("Shack_Abandoned_A", 3f),
            ("Shack_Abandoned_B", 3f),
        };

        /// <summary>Builds every F-010 asset, prefab and the scene.</summary>
        [MenuItem("DigitalStory/Build F-010 Scene")]
        public static void Build()
        {
            try
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                BuildAll();
                Debug.Log("[F010] Scene built: Assets/_Project/Scenes/eldad-lab-2-scene.unity");
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

        /// <summary>
        /// Deletes the F-010 config and song assets, so they are made again from the defaults in code, then builds as
        /// <see cref="Build"/> does. Use it after changing a default; tuned values in those assets are lost.
        /// </summary>
        [MenuItem("DigitalStory/Build F-010 Scene (reset configs)")]
        public static void ResetConfigsAndBuild()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // First, so a config an older build made under its old name is reset too.
            RenameOrRetireOldAssets();
            for (int index = 0; index < ConfigAssetNames.Length; index++)
            {
                string path = $"{DataFolder}/{ConfigAssetNames[index]}.asset";
                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null && !AssetDatabase.DeleteAsset(path))
                {
                    Debug.LogError($"[F010] Could not delete {path}, so it keeps its old values.");
                }
            }

            Debug.Log("[F010] Config assets deleted; they are made again from the defaults.");
            Build();
        }

        private static void BuildAll()
        {
            CreateFolders();
            RenameOrRetireOldAssets();
            Debug.Log("[F010] Folders ready.");

            // Starting a new scene unloads assets that nothing uses, so it must come before loading them.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var swarm = LoadOrCreate<SwarmConfig>("SwarmConfig");
            var squads = LoadOrCreate<SquadConfig>("SquadConfig");
            var aim = LoadOrCreate<AimConfig>("AimConfig");
            var coreMove = LoadOrCreate<CoreMoveConfig>("CoreMoveConfig");
            var fury = LoadOrCreate<FuryConfig>("FuryConfig");
            var colossus = LoadOrCreate<ColossusConfig>("ColossusConfig");
            var hive = LoadOrCreate<HiveConfig>("HiveConfig");
            var nests = LoadOrCreate<NestConfig>("NestConfig");
            var swarmLevel = LoadOrCreate<SwarmLevelConfig>("SwarmLevelConfig");
            var smoke = LoadOrCreate<SmokeConfig>("SmokeConfig");
            var waves = LoadOrCreate<WaveConfig>("WaveConfig");
            var songConfig = LoadOrCreate<SongConfig>("F010SongConfig");
            BeekeeperConfig stick = LoadOrCreate<BeekeeperConfig>("StickBeekeeper");
            BeekeeperConfig smoker = LoadOrCreateSmoker();
            SwarmSongEffect[] songs = LoadOrCreateSongs();
            var samples = LoadOrCreate<ColossusSampleSet>("ColossusSampleSet");
            var gamepad = LoadOrCreate<GamepadConfig>("GamepadConfig");
            Debug.Log("[F010] Config assets ready.");

            AnimatorController controller = BuildController();
            Debug.Log("[F010] Animator controller Beekeeper ready.");

            var beekeeperSource = AssetDatabase.LoadAssetAtPath<GameObject>(BeekeeperSourcePath);
            var hivePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HiveSourcePath);
            var nestTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NestTreeSourcePath);
            Require(beekeeperSource, BeekeeperSourcePath);
            Require(hivePrefab, HiveSourcePath);
            Require(nestTreePrefab, NestTreeSourcePath);

            BeekeeperView stickView = MakeBeekeeperPrefab("F010StickBeekeeper", beekeeperSource, controller, false);
            BeekeeperView smokerView = MakeBeekeeperPrefab("F010SmokerBeekeeper", beekeeperSource, controller, true);
            Debug.Log("[F010] Prefabs F010StickBeekeeper and F010SmokerBeekeeper saved.");
            MixamoKit mixamo = PrepareMixamo();
            GameObject colossusPrefab = MakeColossusPrefab(beekeeperSource, controller, colossus, mixamo);
            BakeSamples(colossusPrefab, swarm, samples);
            Debug.Log("[F010] Prefab F010Colossus saved and its samples baked.");
            var grenade = MakeViewPrefab<SmokeGrenadeView>("F010SmokeGrenade");
            var cloud = MakeViewPrefab<SmokeCloudView>("F010SmokeCloud");
            Debug.Log("[F010] Smoke prefabs saved.");

            AssetDatabase.SaveAssets();
            FillScene(new SceneAssets(
                swarm, squads, aim, coreMove, fury, colossus, hive, nests, swarmLevel, smoke, waves, stick, smoker, songConfig, songs,
                samples, colossusPrefab, hivePrefab, nestTreePrefab, stickView, smokerView, grenade, cloud, gamepad));
            Debug.Log("[F010] Scene filled.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void RenameOrRetireOldAssets()
        {
            for (int index = 0; index < RenamedAssets.Length; index++)
            {
                (string from, string to) = RenamedAssets[index];
                if (AssetDatabase.LoadMainAssetAtPath(from) == null || AssetDatabase.LoadMainAssetAtPath(to) != null)
                {
                    continue;
                }

                string error = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"Could not move {from} to {to}: {error}");
                }

                Debug.Log($"[F010] Moved {from} to {to}.");
            }

            for (int index = 0; index < RetiredAssets.Length; index++)
            {
                string path = RetiredAssets[index];
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                {
                    continue;
                }

                if (!AssetDatabase.DeleteAsset(path))
                {
                    throw new InvalidOperationException($"Could not delete {path}, which only served the Piper.");
                }

                Debug.Log($"[F010] Deleted {path}, which only served the Piper.");
            }
        }

        private static void CreateFolders()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(ArtFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(Root + "/Scenes");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static T LoadOrCreate<T>(string assetName) where T : ScriptableObject
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        // The smoker's numbers are set only when the asset is first made, so tuned values survive.
        private static BeekeeperConfig LoadOrCreateSmoker()
        {
            string path = $"{DataFolder}/SmokerBeekeeper.asset";
            var existing = AssetDatabase.LoadAssetAtPath<BeekeeperConfig>(path);
            if (existing != null)
            {
                return existing;
            }

            var smoker = ScriptableObject.CreateInstance<BeekeeperConfig>();
            AssetDatabase.CreateAsset(smoker, path);

            var serialized = new SerializedObject(smoker);
            Property(serialized, BackingField(nameof(BeekeeperConfig.Kind))).enumValueIndex = (int)BeekeeperKind.Smoker;
            Property(serialized, BackingField(nameof(BeekeeperConfig.Health))).floatValue = 40f;
            Property(serialized, BackingField(nameof(BeekeeperConfig.Speed))).floatValue = 2.2f;
            Property(serialized, BackingField(nameof(BeekeeperConfig.NoticeRange))).floatValue = 7f;
            Property(serialized, BackingField(nameof(BeekeeperConfig.GiveUpRange))).floatValue = 14f;
            Property(serialized, BackingField(nameof(BeekeeperConfig.AttackRange))).floatValue = 14f;
            Property(serialized, BackingField(nameof(BeekeeperConfig.WindUpSeconds))).floatValue = 0.8f;
            Property(serialized, BackingField(nameof(BeekeeperConfig.CooldownSeconds))).floatValue = 4.5f;
            Property(serialized, BackingField(nameof(BeekeeperConfig.HiveDamage))).floatValue = 0f;
            Property(serialized, BackingField(nameof(BeekeeperConfig.WaspsKnockedOff))).intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return smoker;
        }

        // The order of the song table in the HUD: the attacks (Strike, then the Post trap), the defenses (Guard, the Gust
        // pulse, then the Post shell), then the gold songs.
        private static SwarmSongEffect[] LoadOrCreateSongs()
        {
            return new SwarmSongEffect[]
            {
                LoadOrCreate<StingSong>("StingSong"),
                LoadOrCreate<TornadoSong>("TornadoSong"),
                LoadOrCreate<MeteorSong>("MeteorSong"),
                LoadOrCreate<HydraSong>("HydraSong"),
                LoadOrCreate<ScytheSong>("ScytheSong"),
                LoadOrCreate<LanceSong>("LanceSong"),
                LoadOrCreate<NestSong>("NestSong"),
                LoadOrCreate<DomeSong>("DomeSong"),
                LoadOrCreate<GustSong>("GustSong"),
                LoadOrCreate<HoneycombSong>("HoneycombSong"),
                LoadOrCreate<HiveCallSong>("HiveCallSong"),
                LoadOrCreate<ColossusSong>("ColossusSong"),
            };
        }

        // ---------------------------------------------------------------- Animator controller

        private static AnimatorController BuildController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // Recreated on each build, in the same asset, so its GUID and the prefabs' references stay.
            for (int index = controller.parameters.Length - 1; index >= 0; index--)
            {
                controller.RemoveParameter(index);
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            ChildAnimatorState[] oldStates = machine.states;
            for (int index = 0; index < oldStates.Length; index++)
            {
                machine.RemoveState(oldStates[index].state);
            }

            controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);

            // A beekeeper stands idle, and runs while Speed is above the threshold.
            AnimatorState idle = AddState(machine, IdleClip, false);
            AnimatorState run = AddState(machine, RunClip, true);
            machine.defaultState = idle;
            AddSpeedTransition(idle, run, AnimatorConditionMode.Greater);
            AddSpeedTransition(run, idle, AnimatorConditionMode.Less);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string clipName, bool isRun)
        {
            AnimationClip clip = LoopingCopy(FindClip(clipName), clipName);

            AnimatorState state = machine.AddState(clipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            if (isRun)
            {
                // The Run clip plays faster the higher Speed is. Speed is 1 at the beekeeper's walking speed.
                state.speedParameterActive = true;
                state.speedParameter = SpeedParameter;
                state.speed = 1f;
            }

            return state;
        }

        // Raz's clips are imported without Loop Time, and his importer settings are not ours to change.
        // Each state therefore plays our own copy of the clip, saved beside the controller with looping on.
        private static AnimationClip LoopingCopy(AnimationClip source, string clipName)
        {
            EnsureFolder(ClipFolder);
            string path = $"{ClipFolder}/{clipName}.anim";
            var copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (copy == null)
            {
                copy = Object.Instantiate(source);
                copy.name = clipName;
                AssetDatabase.CreateAsset(copy, path);
            }
            else
            {
                // Copying into the existing asset keeps its GUID, so nothing that points at it breaks.
                EditorUtility.CopySerialized(source, copy);
                copy.name = clipName;
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            EditorUtility.SetDirty(copy);
            return copy;
        }

        private static AnimationClip FindClip(string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BeekeeperFbxPath);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is AnimationClip clip && clip.name == clipName && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            throw new InvalidOperationException($"{BeekeeperFbxPath} has no animation clip named '{clipName}'.");
        }

        // ---------------------------------------------------------------- Prefabs

        private static BeekeeperView MakeBeekeeperPrefab(string name, GameObject source, AnimatorController controller, bool isSmoker)
        {
            var root = new GameObject(name, typeof(BeekeeperView));
            var pivot = new GameObject("LeanPivot").transform;
            pivot.SetParent(root.transform, false);

            GameObject model = InstantiateModel(source, pivot);
            FitToHeight(model.transform, BeekeeperHeight);
            Animator animator = UseController(model, controller);

            Transform flute = FindByName(model.transform, FluteName, false);
            if (flute != null)
            {
                flute.gameObject.SetActive(false);
            }

            TintSuit(model);
            Transform hand = FindByName(model.transform, RightHandName, true);
            if (hand == null)
            {
                throw new InvalidOperationException($"{source.name} has no bone named '{RightHandName}' to hold the prop.");
            }

            if (isSmoker)
            {
                AddProp(hand, model.transform, "SmokeCan", LoadOrCreateMaterial("SmokerCan", SmokerMetal), new Vector3(0.12f, 0.09f, 0.12f), Quaternion.identity, 0f);
            }
            else
            {
                AddProp(hand, model.transform, "Stick", LoadOrCreateMaterial("StickWood", StickWood), new Vector3(0.08f, 0.5f, 0.08f), Quaternion.Euler(70f, 0f, 0f), 0.4f);
            }

            var serialized = new SerializedObject(root.GetComponent<BeekeeperView>());
            SetObject(serialized, "_animator", animator);
            SetObject(serialized, "_pivot", pivot);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = SavePrefab(root, name);
            return saved.GetComponent<BeekeeperView>();
        }

        // The fallback of spec section 6, or a Mixamo model when the folder holds one.
        private static GameObject MakeColossusPrefab(
            GameObject beekeeperSource, AnimatorController controller, ColossusConfig config, MixamoKit mixamo)
        {
            GameObject source = mixamo != null ? mixamo.Character : beekeeperSource;
            var root = new GameObject("F010Colossus", typeof(ColossusMotor));
            GameObject model = InstantiateModel(source, root.transform);
            FitToHeight(model.transform, config.Height);
            Animator animator = UseController(model, mixamo != null ? mixamo.Controller : controller);
            if (mixamo != null && mixamo.Avatar != null)
            {
                animator.avatar = mixamo.Avatar;
            }

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            SkinnedMeshRenderer body = PickBody(model.transform);
            if (body == null)
            {
                throw new InvalidOperationException($"{source.name} has no SkinnedMeshRenderer for the Colossus.");
            }

            // The wasps are the body. The puppet stays active, only its renderers are off.
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].enabled = false;
            }

            var serialized = new SerializedObject(root.GetComponent<ColossusMotor>());
            SetObject(serialized, "_body", body);
            SetObject(serialized, "_animator", animator);
            SetObject(serialized, "_attackPivot", model.transform);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, "F010Colossus");
        }

        // ---------------------------------------------------------------- Mixamo Colossus (spec section 6)

        // Reads Art/Mixamo. Returns null, and leaves the beekeeper's rig as the puppet, when the folder is missing or holds no
        // character model or no idle or walking clip.
        private static MixamoKit PrepareMixamo()
        {
            if (!AssetDatabase.IsValidFolder(MixamoFolder))
            {
                Debug.Log("[F010] Colossus uses the fallback rig (no folder Art/Mixamo).");
                return null;
            }

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder });
            var paths = new List<string>(guids.Length);
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);

            string characterPath = FindCharacterPath(paths);
            var clipFiles = new Dictionary<MixamoClip, List<string>>();
            for (int index = 0; index < paths.Count; index++)
            {
                if (paths[index] == characterPath)
                {
                    continue;
                }

                MixamoClip kind = Classify(System.IO.Path.GetFileNameWithoutExtension(paths[index]));
                if (kind == MixamoClip.None)
                {
                    continue;
                }

                if (!clipFiles.TryGetValue(kind, out List<string> list))
                {
                    list = new List<string>();
                    clipFiles[kind] = list;
                }

                list.Add(paths[index]);
            }

            bool hasMove = clipFiles.ContainsKey(MixamoClip.Idle) || clipFiles.ContainsKey(MixamoClip.Walk);
            if (characterPath == null || !hasMove)
            {
                Debug.Log("[F010] Colossus uses the fallback rig (Art/Mixamo has no character model, or no idle or walking clip).");
                return null;
            }

            // The character goes first: the clip files copy the avatar it makes.
            ConfigureImporter(characterPath, false, null, null);
            Avatar avatar = LoadAvatar(characterPath);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogWarning($"[F010] {characterPath} did not make a valid Humanoid avatar; each clip file makes its own.");
                avatar = null;
            }

            var kit = new MixamoKit
            {
                CharacterPath = characterPath,
                Character = AssetDatabase.LoadAssetAtPath<GameObject>(characterPath),
                Avatar = avatar,
            };

            foreach (KeyValuePair<MixamoClip, List<string>> pair in clipFiles)
            {
                bool loops = pair.Key == MixamoClip.Idle || pair.Key == MixamoClip.Walk;
                for (int index = 0; index < pair.Value.Count; index++)
                {
                    ConfigureImporter(pair.Value[index], true, avatar, loops);
                }
            }

            AnimationClip punch = null;
            AnimationClip heavy = null;
            if (clipFiles.TryGetValue(MixamoClip.Punch, out List<string> punches))
            {
                // A cross or hook punch is the heavy one, so the plain punch comes first.
                punches.Sort(ComparePunchFiles);
                punch = LoadClip(punches[0]);
                heavy = punches.Count > 1 ? LoadClip(punches[1]) : null;
            }

            kit.Idle = FirstClip(clipFiles, MixamoClip.Idle);
            kit.Walk = FirstClip(clipFiles, MixamoClip.Walk);
            kit.Punch = punch;
            kit.HeavyPunch = heavy ?? punch;
            kit.Kick = FirstClip(clipFiles, MixamoClip.Kick);
            kit.Hit = FirstClip(clipFiles, MixamoClip.Hit);
            kit.Controller = BuildColossusController(kit);

            var names = new StringBuilder();
            AppendClipName(names, "Idle", kit.Idle);
            AppendClipName(names, "Walk", kit.Walk);
            AppendClipName(names, "Punch", kit.Punch);
            AppendClipName(names, "HeavyPunch", kit.HeavyPunch);
            AppendClipName(names, "Kick", kit.Kick);
            AppendClipName(names, "Hit", kit.Hit);
            Debug.Log($"[F010] Colossus uses the Mixamo character {kit.Character.name} with clips: {names}");
            return kit;
        }

        private static string FindCharacterPath(List<string> paths)
        {
            string firstSkinned = null;
            for (int index = 0; index < paths.Count; index++)
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(paths[index]);
                if (Classify(file) != MixamoClip.None)
                {
                    continue;
                }

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(paths[index]);
                if (model == null || model.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
                {
                    continue;
                }

                if (file.EndsWith(" Bot", StringComparison.OrdinalIgnoreCase))
                {
                    return paths[index]; // X Bot or Y Bot.
                }

                firstSkinned = firstSkinned ?? paths[index];
            }

            return firstSkinned;
        }

        private static MixamoClip Classify(string fileName)
        {
            string lower = fileName.ToLowerInvariant();
            if (lower.Contains("kick"))
            {
                return MixamoClip.Kick;
            }

            if (lower.Contains("punch"))
            {
                return MixamoClip.Punch;
            }

            if (lower.Contains("hit") || lower.Contains("reaction"))
            {
                return MixamoClip.Hit;
            }

            if (lower.Contains("idle"))
            {
                return MixamoClip.Idle;
            }

            return lower.Contains("walk") ? MixamoClip.Walk : MixamoClip.None;
        }

        private static int ComparePunchFiles(string left, string right)
        {
            int byWeight = IsHeavyPunchName(left).CompareTo(IsHeavyPunchName(right));
            return byWeight != 0 ? byWeight : string.CompareOrdinal(left, right);
        }

        private static bool IsHeavyPunchName(string path)
        {
            string lower = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            return lower.Contains("cross") || lower.Contains("hook");
        }

        // Sets the importer of one Mixamo FBX and reimports it only when something differs. These are our own files.
        // loops is null for the character, which keeps whatever clips it carries.
        private static void ConfigureImporter(string path, bool isClipFile, Avatar source, bool? loops)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[F010] {path} has no model importer, so it is left as it is.");
                return;
            }

            bool useSource = isClipFile && source != null;
            ModelImporterAvatarSetup setup = useSource ? ModelImporterAvatarSetup.CopyFromOther : ModelImporterAvatarSetup.CreateFromThisModel;
            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != setup)
            {
                importer.avatarSetup = setup;
                changed = true;
            }

            if (useSource && importer.sourceAvatar != source)
            {
                importer.sourceAvatar = source;
                changed = true;
            }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            if (loops.HasValue)
            {
                ConfigureClip(importer, path, loops.Value);
            }
        }

        // One clip named after the file. Turning and height stay in the pose, so the puppet never drifts.
        // A looping clip keeps its forward travel as root motion, which the Animator ignores (root motion is off),
        // so a walk downloaded without "In Place" still walks on the spot. An attack keeps its lunge in the pose.
        private static void ConfigureClip(ModelImporter importer, string path, bool loops)
        {
            string clipName = System.IO.Path.GetFileNameWithoutExtension(path);
            bool bakesTravel = !loops;
            ModelImporterClipAnimation[] current = importer.clipAnimations;
            if (current.Length == 1 && current[0].name == clipName && current[0].loopTime == loops
                && current[0].lockRootRotation && current[0].lockRootHeightY
                && current[0].lockRootPositionXZ == bakesTravel)
            {
                return;
            }

            ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
            if (defaults.Length == 0)
            {
                Debug.LogWarning($"[F010] {path} has no animation take, so it gives no clip.");
                return;
            }

            ModelImporterClipAnimation clip = defaults[0];
            clip.name = clipName;
            clip.loopTime = loops;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = bakesTravel;
            importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();
        }

        private static Avatar LoadAvatar(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static AnimationClip LoadClip(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            Debug.LogWarning($"[F010] {path} holds no animation clip.");
            return null;
        }

        private static AnimationClip FirstClip(Dictionary<MixamoClip, List<string>> files, MixamoClip kind)
        {
            return files.TryGetValue(kind, out List<string> list) ? LoadClip(list[0]) : null;
        }

        private static void AppendClipName(StringBuilder text, string role, AnimationClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (text.Length > 0)
            {
                text.Append(", ");
            }

            text.Append(role).Append('=').Append(clip.name);
        }

        // Speed above the threshold walks. Each attack trigger comes from Any State and returns to Idle at the clip's end.
        // A missing clip leaves its trigger without a state.
        private static AnimatorController BuildColossusController(MixamoKit kit)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ColossusControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ColossusControllerPath);
            }

            for (int index = controller.parameters.Length - 1; index >= 0; index--)
            {
                controller.RemoveParameter(index);
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            ChildAnimatorState[] oldStates = machine.states;
            for (int index = 0; index < oldStates.Length; index++)
            {
                machine.RemoveState(oldStates[index].state);
            }

            AnimatorStateTransition[] oldAny = machine.anyStateTransitions;
            for (int index = 0; index < oldAny.Length; index++)
            {
                machine.RemoveAnyStateTransition(oldAny[index]);
            }

            controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PunchTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(HeavyPunchTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(KickTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(HitTrigger, AnimatorControllerParameterType.Trigger);

            // Without an idle clip the walk clip's first pose is held while the giant stands.
            AnimatorState idle = machine.AddState("Idle");
            idle.motion = kit.Idle != null ? kit.Idle : kit.Walk;
            idle.speed = kit.Idle != null ? 1f : 0f;
            idle.writeDefaultValues = true;
            machine.defaultState = idle;

            if (kit.Walk != null)
            {
                AnimatorState walk = machine.AddState("Walk");
                walk.motion = kit.Walk;
                walk.writeDefaultValues = true;
                AddSpeedTransition(idle, walk, AnimatorConditionMode.Greater);
                AddSpeedTransition(walk, idle, AnimatorConditionMode.Less);
            }
            else
            {
                Debug.LogWarning("[F010] Mixamo folder has no walking clip: the Colossus has no Walk state.");
            }

            AddAttackState(machine, idle, "Punch", PunchTrigger, kit.Punch);
            AddAttackState(machine, idle, "HeavyPunch", HeavyPunchTrigger, kit.HeavyPunch);
            AddAttackState(machine, idle, "Kick", KickTrigger, kit.Kick);
            AddAttackState(machine, idle, "Hit", HitTrigger, kit.Hit);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddSpeedTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = BlendSeconds;
            transition.AddCondition(mode, MovingThreshold, SpeedParameter);
        }

        private static void AddAttackState(AnimatorStateMachine machine, AnimatorState idle, string stateName, string trigger, AnimationClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[F010] Mixamo folder has no clip for '{trigger}': the Colossus's {trigger} trigger has no state.");
                return;
            }

            AnimatorState state = machine.AddState(stateName);
            state.motion = clip;
            state.writeDefaultValues = true;

            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.hasFixedDuration = true;
            enter.duration = BlendSeconds;
            enter.canTransitionToSelf = true;
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);

            AnimatorStateTransition leave = state.AddTransition(idle);
            leave.hasExitTime = true;
            leave.exitTime = AttackExitTime;
            leave.hasFixedDuration = true;
            leave.duration = BlendSeconds;
        }

        private enum MixamoClip
        {
            None,
            Idle,
            Walk,
            Punch,
            Kick,
            Hit,
        }

        private sealed class MixamoKit
        {
            public string CharacterPath;
            public GameObject Character;
            public Avatar Avatar;
            public AnimationClip Idle;
            public AnimationClip Walk;
            public AnimationClip Punch;
            public AnimationClip HeavyPunch;
            public AnimationClip Kick;
            public AnimationClip Hit;
            public AnimatorController Controller;
        }

        private static SkinnedMeshRenderer PickBody(Transform model)
        {
            SkinnedMeshRenderer[] all = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer best = null;
            int bestVertices = -1;
            for (int index = 0; index < all.Length; index++)
            {
                int vertices = all[index].sharedMesh != null ? all[index].sharedMesh.vertexCount : 0;
                if (vertices > bestVertices)
                {
                    best = all[index];
                    bestVertices = vertices;
                }
            }

            return best;
        }

        private static void BakeSamples(GameObject colossusPrefab, SwarmConfig swarm, ColossusSampleSet samples)
        {
            string path = AssetDatabase.GetAssetPath(colossusPrefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SkinnedMeshRenderer body = PickBody(contents.transform);
                ColossusSampleBaker.Bake(body, swarm.MaxWasps, samples);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            EditorUtility.SetDirty(samples);
        }

        private static T MakeViewPrefab<T>(string name) where T : MonoBehaviour
        {
            var root = new GameObject(name, typeof(T));
            return SavePrefab(root, name).GetComponent<T>();
        }

        // Keeps the source prefab's own root rotation: Raz's exports are Z-up and stand up through a -90 degree turn
        // about X on that root, so resetting it to identity would lay the model on its side.
        private static GameObject InstantiateModel(GameObject source, Transform parent)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = source.transform.localRotation;
            return model;
        }

        private static Animator UseController(GameObject model, AnimatorController controller)
        {
            Transform host = AnimatorHost(model, controller);
            Animator animator = null;
            foreach (Animator found in model.GetComponentsInChildren<Animator>(true))
            {
                if (found.transform == host)
                {
                    animator = found;
                }
                else
                {
                    // Raz's prefab has its Animator on the mesh object, where the clips' bone paths lead nowhere.
                    // His prefab is not ours to change, so our prefab switches that one off and uses its own.
                    found.enabled = false;
                }
            }

            if (animator == null)
            {
                animator = host.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            return animator;
        }

        // A generic clip names each bone by its path from the Animator's object, so the Animator belongs on the
        // parent of the first object those paths name. A Humanoid clip has no paths and uses the model's own Animator.
        private static Transform AnimatorHost(GameObject model, AnimatorController controller)
        {
            foreach (AnimationClip clip in controller.animationClips)
            {
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (string.IsNullOrEmpty(binding.path))
                    {
                        continue;
                    }

                    int slash = binding.path.IndexOf('/');
                    string first = slash < 0 ? binding.path : binding.path.Substring(0, slash);
                    foreach (Transform candidate in model.GetComponentsInChildren<Transform>(true))
                    {
                        if (candidate.name == first && candidate.parent != null)
                        {
                            return candidate.parent;
                        }
                    }

                    throw new InvalidOperationException(
                        $"{model.name} has no object named '{first}', which the clip '{clip.name}' animates.");
                }
            }

            Animator existing = model.GetComponentInChildren<Animator>(true);
            return existing != null ? existing.transform : model.transform;
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
            if (saved == null)
            {
                throw new InvalidOperationException($"Could not save the prefab {name}.");
            }

            return saved;
        }

        // New materials under Art/F010, so Raz's materials are untouched.
        private static void TintSuit(GameObject model)
        {
            var made = new Dictionary<Material, Material>();
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Material[] originals = renderers[index].sharedMaterials;
                var suits = new Material[originals.Length];
                for (int slot = 0; slot < originals.Length; slot++)
                {
                    Material original = originals[slot];
                    if (original == null)
                    {
                        continue;
                    }

                    if (!made.TryGetValue(original, out Material suit))
                    {
                        suit = LoadOrCreateMaterial("Suit_" + Sanitize(original.name), SuitWhite, original);
                        AllowHitGlow(suit);
                        made[original] = suit;
                    }

                    suits[slot] = suit;
                }

                renderers[index].sharedMaterials = suits;
            }
        }

        // BeekeeperView flashes a felt hit by the emission colour, which a material only shows with its emission on. It starts
        // black, so the suit looks as before, and is marked as changed at runtime so nothing turns the emission off again.
        private static void AllowHitGlow(Material material)
        {
            if (!material.HasProperty(NestEmissionProperty))
            {
                return;
            }

            material.EnableKeyword(EmissionKeyword);
            material.SetColor(NestEmissionProperty, Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
        }

        // An unlit, see-through material for the blue flash on a shield that blocks a hit. HitFeedbackView tints it by _Color.
        private static Material LoadOrCreateShieldFlashMaterial()
        {
            var shader = Require(Shader.Find(ShieldFlashShaderName), ShieldFlashShaderName);
            string path = $"{ArtFolder}/{ShieldFlashMaterialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddProp(Transform hand, Transform model, string name, Material material, Vector3 worldSize, Quaternion tilt, float reach)
        {
            GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            prop.name = name;
            Object.DestroyImmediate(prop.GetComponent<Collider>());
            prop.GetComponent<MeshRenderer>().sharedMaterial = material;

            // The cylinder is 1 m across and 2 m tall, so the Y scale is half the wanted height. The size is in world metres.
            Quaternion rotation = model.rotation * tilt;
            prop.transform.SetPositionAndRotation(hand.position + (rotation * new Vector3(0f, reach, 0f)), rotation);
            prop.transform.localScale = worldSize;
            prop.transform.SetParent(hand, true);
        }

        private static Material LoadOrCreateMaterial(string name, Color color, Material template = null)
        {
            string path = $"{ArtFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", null);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static string Sanitize(string text)
        {
            var builder = new StringBuilder(text.Length);
            for (int index = 0; index < text.Length; index++)
            {
                builder.Append(char.IsLetterOrDigit(text[index]) ? text[index] : '_');
            }

            return builder.ToString();
        }

        // ---------------------------------------------------------------- Measuring

        private static Transform FindByName(Transform root, string boneName, bool allowSuffix)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < all.Length; index++)
            {
                if (all[index].name == boneName)
                {
                    return all[index];
                }
            }

            if (allowSuffix)
            {
                for (int index = 0; index < all.Length; index++)
                {
                    if (all[index].name.EndsWith(boneName, StringComparison.Ordinal))
                    {
                        return all[index];
                    }
                }
            }

            return null;
        }

        // The bind-pose bounds of every mesh under the root in world space. This does not depend on a renderer's cached bounds.
        private static Bounds MeasureBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasAny = false;
            var total = new Bounds();

            for (int index = 0; index < renderers.Length; index++)
            {
                Mesh mesh = null;
                if (renderers[index] is SkinnedMeshRenderer skinned)
                {
                    mesh = skinned.sharedMesh;
                }
                else if (renderers[index] is MeshRenderer)
                {
                    MeshFilter filter = renderers[index].GetComponent<MeshFilter>();
                    mesh = filter != null ? filter.sharedMesh : null;
                }

                if (mesh == null)
                {
                    continue;
                }

                Matrix4x4 toWorld = renderers[index].transform.localToWorldMatrix;
                Bounds local = mesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = local.center + Vector3.Scale(local.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    Vector3 world = toWorld.MultiplyPoint3x4(point);
                    if (hasAny)
                    {
                        total.Encapsulate(world);
                    }
                    else
                    {
                        total = new Bounds(world, Vector3.zero);
                        hasAny = true;
                    }
                }
            }

            if (!hasAny)
            {
                throw new InvalidOperationException($"{root.name} has no mesh to measure.");
            }

            return total;
        }

        // Scales the model so it stands exactly this tall, and rests its feet at the height of its parent.
        private static void FitToHeight(Transform model, float height)
        {
            float current = MeasureBounds(model).size.y;
            if (current < 1e-4f)
            {
                throw new InvalidOperationException($"{model.name} has no height to scale.");
            }

            model.localScale *= height / current;
            SitOnParent(model);
        }

        private static void SitOnParent(Transform model)
        {
            float floor = model.parent != null ? model.parent.position.y : 0f;
            Bounds bounds = MeasureBounds(model);
            model.position += new Vector3(0f, floor - bounds.min.y, 0f);
        }

        // Moves the model on the ground so the middle of its bounds is over its parent, for models whose pivot is off to one side.
        private static void CenterOnParent(Transform model)
        {
            Vector3 anchor = model.parent != null ? model.parent.position : Vector3.zero;
            Bounds bounds = MeasureBounds(model);
            model.position += new Vector3(anchor.x - bounds.center.x, 0f, anchor.z - bounds.center.z);
        }

        // ---------------------------------------------------------------- Scene

        // The swarm's core: a CharacterController and its motor at the middle of the map, and nothing to see. Nothing marks
        // it: the standing swarm's figure-eight crosses itself over it.
        private static GameObject BuildCore()
        {
            var core = new GameObject("Swarm Core", typeof(CharacterController), typeof(SwarmCoreMotor));
            var body = core.GetComponent<CharacterController>();
            body.height = CoreHeight;
            body.radius = CoreRadius;

            // A CharacterController rests its skin width above the ground, so the capsule is raised by that much and its
            // base, at the root, touches the grass.
            body.center = new Vector3(0f, (CoreHeight * 0.5f) + body.skinWidth, 0f);
            core.transform.position = Vector3.zero;
            return core;
        }

        private static void FillScene(SceneAssets assets)
        {
            BuildLighting();
            BuildGround();
            Transform spawns = BuildSpawnPoints();
            Transform scenery = BuildScenery(spawns);

            var hives = new HiveView[2];
            var hiveSource = AssetDatabase.LoadAssetAtPath<GameObject>(HiveSourcePath);
            hives[0] = BuildHive(hiveSource, "Hive A", new Vector3(-HiveSpacing * 0.5f, 0f, 0f));
            hives[1] = BuildHive(hiveSource, "Hive B", new Vector3(HiveSpacing * 0.5f, 0f, 0f));
            NestView[] nests = BuildNests(assets.NestTreePrefab, assets.Nests, assets.CoreMove, hives, spawns);

            GameObject core = BuildCore();
            var colossus = (GameObject)PrefabUtility.InstantiatePrefab(assets.ColossusPrefab);
            colossus.name = "Colossus Puppet";
            colossus.transform.position = new Vector3(0f, 0f, -ClearHalfSize * 0.5f);

            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(FollowCamera));
            camera.tag = "MainCamera";
            var cameraComponent = camera.GetComponent<Camera>();
            cameraComponent.clearFlags = CameraClearFlags.Skybox;
            cameraComponent.backgroundColor = SkyZenith; // Only seen if the skybox is ever switched off.
            cameraComponent.farClipPlane = 200f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 14f, -10f), Quaternion.Euler(55f, 0f, 0f));

            // What the chase camera follows while the giant stands (plan section I): the root puts it where the rules say the
            // giant is, facing its way, so the puppet's spin and leap never swing the view.
            var chaseAnchor = new GameObject("Giant Chase Anchor");
            chaseAnchor.transform.position = colossus.transform.position;
            SetChaseBlockers(camera.GetComponent<FollowCamera>(), scenery, nests);

            var input = new GameObject("Hive Defense Input", typeof(HiveDefenseInput));

            // The voice writes its sound in OnAudioFilterRead, so its source plays no clip and stays flat (2D).
            var fluteVoice = new GameObject("Swarm Flute Voice", typeof(AudioSource), typeof(SwarmFluteVoice));
            var fluteSource = fluteVoice.GetComponent<AudioSource>();
            fluteSource.clip = null;
            fluteSource.playOnAwake = false;
            fluteSource.spatialBlend = 0f;

            var swarmObject = new GameObject("Wasp Swarm", typeof(SwarmGpuView));
            var swarmSerialized = new SerializedObject(swarmObject.GetComponent<SwarmGpuView>());
            SetObject(swarmSerialized, "_swarmCompute", AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath));
            SetObject(swarmSerialized, "_waspShader", AssetDatabase.LoadAssetAtPath<Shader>(WaspShaderPath));
            SetObject(swarmSerialized, "_glowShader", AssetDatabase.LoadAssetAtPath<Shader>(GlowShaderPath));
            swarmSerialized.ApplyModifiedPropertiesWithoutUndo();

            // The light that follows the swarm. SwarmGlowLight sets its range, brightness and colour from the SwarmConfig.
            // Every light the scene uses exists from the first frame and stays enabled, dark while unused, so no frame of play
            // pays for a light's first use.
            var glowLight = new GameObject("Swarm Glow Light", typeof(Light), typeof(SwarmGlowLight));
            Light glow = glowLight.GetComponent<Light>();
            glow.type = LightType.Point;
            glow.shadows = LightShadows.None;
            glow.intensity = 0f;
            glow.enabled = true;

            // The second light follows the wasps that attack, in a sting or a meteor.
            var attackLight = new GameObject("Swarm Attack Light", typeof(Light), typeof(SwarmGlowLight));
            Light attackGlow = attackLight.GetComponent<Light>();
            attackGlow.type = LightType.Point;
            attackGlow.shadows = LightShadows.None;
            attackGlow.intensity = 0f;
            attackGlow.enabled = true;

            // The felt hit: its shield flashes are made from this material when the match starts.
            var hitFeedback = new GameObject("Hit Feedback", typeof(HitFeedbackView));
            var hitFeedbackSerialized = new SerializedObject(hitFeedback.GetComponent<HitFeedbackView>());
            SetObject(hitFeedbackSerialized, "_shieldFlashMaterial", LoadOrCreateShieldFlashMaterial());
            hitFeedbackSerialized.ApplyModifiedPropertiesWithoutUndo();

            // The felt hit on a controller: its motors, set from the GamepadConfig's pulses.
            var rumble = new GameObject("Gamepad Rumble", typeof(GamepadRumbleView));

            var beekeeperPool = new GameObject("Beekeeper Pool", typeof(BeekeeperViewPool));
            var smokePool = new GameObject("Smoke Pool", typeof(SmokeViewPool));

            // The giant's strip has its own view, with its lines of keys as serialized words.
            var hud = new GameObject("Hive Defense HUD", typeof(UIDocument), typeof(HiveDefenseHudView), typeof(GiantHudView));
            var document = hud.GetComponent<UIDocument>();
            document.panelSettings = Require(AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath), PanelSettingsPath);
            document.visualTreeAsset = Require(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath), HudUxmlPath);
            var hudSerialized = new SerializedObject(hud.GetComponent<HiveDefenseHudView>());
            SetObject(hudSerialized, "_camera", cameraComponent);
            SetObject(hudSerialized, "_swarmConfig", assets.Swarm);
            SetObject(hudSerialized, "_nestConfig", assets.Nests);
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            var rootObject = new GameObject("Hive Defense Root", typeof(HiveDefenseRoot));
            var serialized = new SerializedObject(rootObject.GetComponent<HiveDefenseRoot>());
            SetObject(serialized, "_swarm", assets.Swarm);
            SetObject(serialized, "_squads", assets.Squads);
            SetObject(serialized, "_aim", assets.Aim);
            SetObject(serialized, "_coreMove", assets.CoreMove);
            SetObject(serialized, "_fury", assets.Fury);
            SetObject(serialized, "_colossus", assets.Colossus);
            SetObject(serialized, "_hive", assets.Hive);
            SetObject(serialized, "_nests", assets.Nests);
            SetObject(serialized, "_swarmLevel", assets.SwarmLevel);
            SetObject(serialized, "_smoke", assets.Smoke);
            SetObject(serialized, "_waves", assets.Waves);
            SetObject(serialized, "_stick", assets.Stick);
            SetObject(serialized, "_smoker", assets.Smoker);
            SetObject(serialized, "_songConfig", assets.SongConfig);
            SetObjects(serialized, "_songs", assets.Songs);
            Property(serialized, "_holeCount").intValue = FluteHoleCount;
            SetObject(serialized, "_colossusSamples", assets.Samples);
            SetObject(serialized, "_input", input.GetComponent<HiveDefenseInput>());
            SetObject(serialized, "_coreMotor", core.GetComponent<SwarmCoreMotor>());
            SetObject(serialized, "_followCamera", camera.GetComponent<FollowCamera>());
            SetObject(serialized, "_chaseAnchor", chaseAnchor.transform);
            SetObject(serialized, "_camera", cameraComponent);
            SetObject(serialized, "_swarmView", swarmObject.GetComponent<SwarmGpuView>());
            SetObject(serialized, "_glowLight", glowLight.GetComponent<SwarmGlowLight>());
            SetObject(serialized, "_attackGlowLight", attackLight.GetComponent<SwarmGlowLight>());
            SetObjects(serialized, "_hives", hives);
            SetObjects(serialized, "_nestViews", nests);
            SetObjects(serialized, "_spawnPoints", ChildTransforms(spawns));
            SetObject(serialized, "_beekeeperPool", beekeeperPool.GetComponent<BeekeeperViewPool>());
            SetObject(serialized, "_stickPrefab", assets.StickView);
            SetObject(serialized, "_smokerPrefab", assets.SmokerView);
            SetObject(serialized, "_smokePool", smokePool.GetComponent<SmokeViewPool>());
            SetObject(serialized, "_grenadePrefab", assets.Grenade);
            SetObject(serialized, "_cloudPrefab", assets.Cloud);
            SetObject(serialized, "_colossusMotor", colossus.GetComponent<ColossusMotor>());
            SetObject(serialized, "_hud", hud.GetComponent<HiveDefenseHudView>());
            SetObject(serialized, "_hitFeedback", hitFeedback.GetComponent<HitFeedbackView>());
            SetObject(serialized, "_fluteVoice", fluteVoice.GetComponent<SwarmFluteVoice>());
            SetObject(serialized, "_sound", Require(AssetDatabase.LoadAssetAtPath<SoundConfig>(SoundConfigPath), SoundConfigPath));
            SetObject(serialized, "_gamepad", assets.Gamepad);
            SetObject(serialized, "_rumble", rumble.GetComponent<GamepadRumbleView>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // A moonlit night. The moon in the sky and the moonlight come from the same side.
        private static void BuildLighting()
        {
            var moon = new GameObject("Moonlight", typeof(Light));
            var light = moon.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = MoonLightColor;
            light.intensity = MoonlightIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = MoonlightShadowStrength;
            moon.transform.rotation = Quaternion.LookRotation(-MoonlightDirection); // The light travels away from the moon.

            RenderSettings.sun = light;
            RenderSettings.skybox = BuildSkyMaterial();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.fog = FogOn;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = FogDensity;
        }

        private static Material BuildSkyMaterial()
        {
            var shader = Require(AssetDatabase.LoadAssetAtPath<Shader>(SkyShaderPath), SkyShaderPath);
            string path = $"{ArtFolder}/{SkyMaterialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_ZenithColor", SkyZenith);
            material.SetColor("_HorizonColor", SkyHorizon);
            material.SetColor("_GroundColor", SkyGround);
            material.SetFloat("_HorizonWidth", SkyHorizonWidth);
            material.SetFloat("_StarDensity", StarDensity);
            material.SetFloat("_MoonSize", MoonSize);
            material.SetVector("_MoonDirection", new Vector4(MoonDirection.x, MoonDirection.y, MoonDirection.z, 0f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            float scale = GroundSize / PlaneBaseSize;
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            ground.GetComponent<MeshRenderer>().sharedMaterial = LoadOrCreateMaterial("Grass", Grass);
            ground.isStatic = true;
        }

        private static Transform BuildSpawnPoints()
        {
            var parent = new GameObject("Spawn Points").transform;
            CreateSpawn(parent, "Spawn North", new Vector3(0f, 0f, SpawnDistance));
            CreateSpawn(parent, "Spawn South", new Vector3(0f, 0f, -SpawnDistance));
            CreateSpawn(parent, "Spawn East", new Vector3(SpawnDistance, 0f, 0f));
            CreateSpawn(parent, "Spawn West", new Vector3(-SpawnDistance, 0f, 0f));
            return parent;
        }

        private static void CreateSpawn(Transform parent, string name, Vector3 position)
        {
            var spawn = new GameObject(name).transform;
            spawn.SetParent(parent, false);
            spawn.position = position;
        }

        private static Transform[] ChildTransforms(Transform parent)
        {
            var children = new Transform[parent.childCount];
            for (int index = 0; index < children.Length; index++)
            {
                children[index] = parent.GetChild(index);
            }

            return children;
        }

        // Around the edge only, never in the middle 50 x 50 m, and never on a spawn point.
        // Plan section I: the chase camera moves in front of what stands between it and the giant, but Raz's tree crowns and
        // ruins have no collider, so it is given each tall prop's box, the scenery's and the nest trees', instead.
        private static void SetChaseBlockers(FollowCamera follow, Transform scenery, NestView[] nests)
        {
            var blockers = new List<Bounds>();
            for (int index = 0; index < scenery.childCount; index++)
            {
                AddChaseBlocker(blockers, scenery.GetChild(index));
            }

            for (int index = 0; index < nests.Length; index++)
            {
                AddChaseBlocker(blockers, nests[index].transform);
            }

            var serialized = new SerializedObject(follow);
            SerializedProperty property = Property(serialized, "_chaseBlockers");
            property.arraySize = blockers.Count;
            for (int index = 0; index < blockers.Count; index++)
            {
                property.GetArrayElementAtIndex(index).boundsValue = blockers[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddChaseBlocker(List<Bounds> blockers, Transform prop)
        {
            Bounds bounds = MeasureBounds(prop);
            if (bounds.size.y >= CameraBlockerLeastHeight)
            {
                blockers.Add(bounds);
            }
        }

        // Returns the parent of every prop.
        private static Transform BuildScenery(Transform spawns)
        {
            var parent = new GameObject("Scenery").transform;
            var random = new System.Random(SceneSeed);
            var prefabs = new Dictionary<string, GameObject>();

            for (int count = 0; count < SceneryCount; count++)
            {
                (string name, float height) = Scenery[random.Next(Scenery.Length)];
                if (!prefabs.TryGetValue(name, out GameObject prefab))
                {
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RazPrefabFolder}/{name}.prefab");
                    Require(prefab, name);
                    prefabs[name] = prefab;
                }

                if (!TryPickEdgePoint(random, spawns, out Vector3 point))
                {
                    continue;
                }

                // The random turn goes on top of the prefab's own root rotation, which stands the Z-up model upright.
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                Quaternion turn = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                instance.transform.SetPositionAndRotation(point, turn * prefab.transform.localRotation);
                float target = height * (0.85f + ((float)random.NextDouble() * 0.3f));
                float current = MeasureBounds(instance.transform).size.y;
                if (current > 1e-4f)
                {
                    instance.transform.localScale *= target / current;
                }

                Bounds bounds = MeasureBounds(instance.transform);
                instance.transform.position += new Vector3(0f, -bounds.min.y, 0f);

                Transform[] all = instance.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < all.Length; index++)
                {
                    all[index].gameObject.isStatic = true;
                }
            }

            return parent;
        }

        private static bool TryPickEdgePoint(System.Random random, Transform spawns, out Vector3 point)
        {
            for (int attempt = 0; attempt < SceneryAttempts; attempt++)
            {
                float x = ((float)random.NextDouble() * 2f - 1f) * EdgeHalfSize;
                float z = ((float)random.NextDouble() * 2f - 1f) * EdgeHalfSize;
                if (Mathf.Abs(x) < ClearHalfSize + 0.5f && Mathf.Abs(z) < ClearHalfSize + 0.5f)
                {
                    continue;
                }

                point = new Vector3(x, 0f, z);
                bool isOnSpawn = false;
                for (int index = 0; index < spawns.childCount; index++)
                {
                    if ((spawns.GetChild(index).position - point).sqrMagnitude < SpawnClearance * SpawnClearance)
                    {
                        isOnSpawn = true;
                        break;
                    }
                }

                if (!isOnSpawn)
                {
                    return true;
                }
            }

            point = Vector3.zero;
            return false;
        }

        private static HiveView BuildHive(GameObject source, string name, Vector3 position)
        {
            var hive = new GameObject(name, typeof(HiveView));
            hive.transform.position = position;

            // HiveView flattens a destroyed hive by scaling its model's local Y. The model itself is turned -90 degrees
            // about X to stand up, so its own Y is horizontal; this unturned pivot at the base is what gets scaled.
            var pivot = new GameObject("Model").transform;
            pivot.SetParent(hive.transform, false);
            GameObject model = InstantiateModel(source, pivot);
            FitToHeight(model.transform, HiveHeight);
            CenterOnParent(model.transform);

            var serialized = new SerializedObject(hive.GetComponent<HiveView>());
            SetObject(serialized, "_model", pivot);

            // A small warm lamp beside the hive, on the camera's side, so it reads as a lit home.
            var lamp = new GameObject("Hive Light", typeof(Light));
            lamp.transform.SetParent(hive.transform, false);
            lamp.transform.localPosition = new Vector3(0f, HiveLightHeight, -HiveLightFront);
            var lampLight = lamp.GetComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.renderMode = LightRenderMode.ForcePixel; // The ground is one big plane, too coarse for a vertex light.
            lampLight.color = HiveLightColor;
            lampLight.range = HiveLightRange;
            lampLight.intensity = HiveLightIntensity;
            lampLight.shadows = LightShadows.None;

            Property(serialized, "_barHeight").floatValue = HiveHeight + HiveBarGap;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return hive.GetComponent<HiveView>();
        }

        // Plan section H: Raz's nest trees, in the order they open. The places are chosen with the scene's seeded random, so
        // every build gives the same layout, and checked against NestConfig's rules before anything is placed.
        private static NestView[] BuildNests(GameObject source, NestConfig config, CoreMoveConfig coreMove, HiveView[] hives, Transform spawns)
        {
            var hivePositions = new Vector3[hives.Length];
            for (int index = 0; index < hives.Length; index++)
            {
                hivePositions[index] = hives[index].transform.position;
            }

            Transform[] spawnPoints = ChildTransforms(spawns);
            var spawnPositions = new Vector3[spawnPoints.Length];
            for (int index = 0; index < spawnPoints.Length; index++)
            {
                spawnPositions[index] = spawnPoints[index].position;
            }

            List<Vector3> positions = PickNestPositions(config, hivePositions, spawnPositions);
            NestLayout.Check(positions, hivePositions, config, coreMove.PlayAreaHalfSize);

            var parent = new GameObject("Nests").transform;
            var views = new NestView[positions.Count];
            for (int index = 0; index < views.Length; index++)
            {
                views[index] = BuildNestTree(source, parent, $"Nest {index + 1}", positions[index]);
            }

            return views;
        }

        // The ones open at the start stand ahead of the core, where the opening camera looks, one to each side. The rest stand
        // one in each equal slice of the circle around the middle, so they are spread around it, and the nearer ones open
        // first, so each wave's nests are a longer trip than the last.
        private static List<Vector3> PickNestPositions(NestConfig config, Vector3[] hives, Vector3[] spawns)
        {
            var random = new System.Random(SceneSeed);
            var placed = new List<Vector3>(config.NestCount);
            for (int index = 0; index < config.OpenAtStart; index++)
            {
                float side = index % 2 == 0 ? -1f : 1f;
                placed.Add(PickNest(
                    random, placed, hives, spawns, config, side * FirstNestsNarrowestBearing, side * FirstNestsWidestBearing,
                    config.FirstNestsNearest, config.FirstNestsFarthest, $"nest {index}, open at the start"));
            }

            int later = config.NestCount - config.OpenAtStart;
            var laterNests = new List<Vector3>(later);
            for (int index = 0; index < later; index++)
            {
                float slice = index * 360f / later;
                Vector3 nest = PickNest(
                    random, placed, hives, spawns, config, slice - LaterNestJitterDegrees, slice + LaterNestJitterDegrees,
                    config.LaterNestsNearest, config.LaterNestsFarthest, $"nest {config.OpenAtStart + index}, opened after a wave");
                placed.Add(nest);
                laterNests.Add(nest);
            }

            laterNests.Sort((left, right) => GroundRadius(left).CompareTo(GroundRadius(right)));
            placed.RemoveRange(config.OpenAtStart, later);
            placed.AddRange(laterNests);
            return placed;
        }

        private static Vector3 PickNest(
            System.Random random,
            List<Vector3> placed,
            Vector3[] hives,
            Vector3[] spawns,
            NestConfig config,
            float fromBearing,
            float toBearing,
            float nearest,
            float farthest,
            string which)
        {
            for (int attempt = 0; attempt < NestAttempts; attempt++)
            {
                float bearing = Mathf.Lerp(fromBearing, toBearing, (float)random.NextDouble()) * Mathf.Deg2Rad;
                float radius = Mathf.Lerp(nearest, farthest, (float)random.NextDouble());
                var point = new Vector3(Mathf.Sin(bearing) * radius, 0f, Mathf.Cos(bearing) * radius);
                bool isInside = Mathf.Abs(point.x) <= NestInsideHalfSize && Mathf.Abs(point.z) <= NestInsideHalfSize;
                if (isInside
                    && IsClearOf(point, hives, config.HiveClearance + NestHiveMargin)
                    && IsClearOf(point, spawns, NestSpawnClearance)
                    && IsClearOf(point, placed, NestSpacing))
                {
                    return point;
                }
            }

            throw new InvalidOperationException($"[F010] Found no place for {which} that keeps NestConfig's rules and the builder's spacing.");
        }

        private static bool IsClearOf(Vector3 point, IReadOnlyList<Vector3> others, float distance)
        {
            for (int index = 0; index < others.Count; index++)
            {
                Vector3 offset = others[index] - point;
                offset.y = 0f;
                if (offset.sqrMagnitude < distance * distance)
                {
                    return false;
                }
            }

            return true;
        }

        private static float GroundRadius(Vector3 point) => new Vector2(point.x, point.z).magnitude;

        // NestView stands where the nest is, and Raz's tree under it keeps its own rotation, which stands the Z-up model
        // upright, with its trunk on that spot, scaled to a tree's height. The tree is static; the nest in it is not, because
        // NestView tints it and hides it once it is collected.
        private static NestView BuildNestTree(GameObject source, Transform parent, string name, Vector3 position)
        {
            var nest = new GameObject(name, typeof(NestView));
            nest.transform.SetParent(parent, false);
            nest.transform.position = position;

            var tree = (GameObject)PrefabUtility.InstantiatePrefab(source, nest.transform);
            tree.transform.localPosition = Vector3.zero;
            FitToHeight(tree.transform, NestTreeHeight);

            Transform nestObject = FindByName(tree.transform, NestObjectName, false);
            if (nestObject == null)
            {
                throw new InvalidOperationException($"[F010] {source.name} has no child named {NestObjectName}, which NestView shows.");
            }

            Transform[] all = tree.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < all.Length; index++)
            {
                all[index].gameObject.isStatic = !all[index].IsChildOf(nestObject);
            }

            Renderer[] nestRenderers = nestObject.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < nestRenderers.Length; index++)
            {
                Material[] materials = nestRenderers[index].sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    materials[slot] = NestGlowMaterial(materials[slot]);
                }

                nestRenderers[index].sharedMaterials = materials;
            }

            return nest.GetComponent<NestView>();
        }

        // A copy of the nest's own material, with its colour and texture, that can glow: NestView pulses its emission while
        // the nest is open. The emission is switched on in the asset, so the shader variant that draws it is in every build.
        // Raz's material and prefab stay as they are; only the scene's trees use the copy.
        private static Material NestGlowMaterial(Material original)
        {
            if (original == null)
            {
                throw new InvalidOperationException($"[F010] The nest in {NestTreeSourcePath} has an empty material slot.");
            }

            string path = $"{ArtFolder}/NestGlow_{Sanitize(original.name)}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(original);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = original.shader;
                material.CopyPropertiesFromMaterial(original);
            }

            if (!material.HasProperty(NestEmissionProperty))
            {
                throw new InvalidOperationException($"[F010] The nest's shader {original.shader.name} has no {NestEmissionProperty}, so it cannot glow.");
            }

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor(NestEmissionProperty, Color.black);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------------------------------------------------------------- Helpers

        private static string BackingField(string propertyName) => $"<{propertyName}>k__BackingField";

        private static T Require<T>(T asset, string path) where T : Object
        {
            if (asset == null)
            {
                throw new InvalidOperationException($"[F010] Missing {path}. The scene cannot be built without it.");
            }

            return asset;
        }

        private static SerializedProperty Property(SerializedObject target, string propertyName)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"{target.targetObject.name} has no field named {propertyName}.");
            }

            return property;
        }

        private static void SetObject(SerializedObject target, string propertyName, Object value)
        {
            SerializedProperty property = Property(target, propertyName);
            if (value == null)
            {
                throw new InvalidOperationException($"Nothing to put in {propertyName} on {target.targetObject.name}.");
            }

            property.objectReferenceValue = value;
        }

        private static void SetObjects(SerializedObject target, string propertyName, Object[] values)
        {
            SerializedProperty property = Property(target, propertyName);
            property.arraySize = values.Length;

            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == null)
                {
                    throw new InvalidOperationException($"{propertyName}[{index}] on {target.targetObject.name} has nothing to put in it.");
                }

                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private readonly struct SceneAssets
        {
            public SceneAssets(
                SwarmConfig swarm,
                SquadConfig squads,
                AimConfig aim,
                CoreMoveConfig coreMove,
                FuryConfig fury,
                ColossusConfig colossus,
                HiveConfig hive,
                NestConfig nests,
                SwarmLevelConfig swarmLevel,
                SmokeConfig smoke,
                WaveConfig waves,
                BeekeeperConfig stick,
                BeekeeperConfig smoker,
                SongConfig songConfig,
                SwarmSongEffect[] songs,
                ColossusSampleSet samples,
                GameObject colossusPrefab,
                GameObject hivePrefab,
                GameObject nestTreePrefab,
                BeekeeperView stickView,
                BeekeeperView smokerView,
                SmokeGrenadeView grenade,
                SmokeCloudView cloud,
                GamepadConfig gamepad)
            {
                Swarm = swarm;
                Squads = squads;
                Aim = aim;
                CoreMove = coreMove;
                Fury = fury;
                Colossus = colossus;
                Hive = hive;
                Nests = nests;
                SwarmLevel = swarmLevel;
                Smoke = smoke;
                Waves = waves;
                Stick = stick;
                Smoker = smoker;
                SongConfig = songConfig;
                Songs = songs;
                Samples = samples;
                ColossusPrefab = colossusPrefab;
                HivePrefab = hivePrefab;
                NestTreePrefab = nestTreePrefab;
                StickView = stickView;
                SmokerView = smokerView;
                Grenade = grenade;
                Cloud = cloud;
                Gamepad = gamepad;
            }

            public SwarmConfig Swarm { get; }
            public SquadConfig Squads { get; }
            public AimConfig Aim { get; }
            public CoreMoveConfig CoreMove { get; }
            public FuryConfig Fury { get; }
            public ColossusConfig Colossus { get; }
            public HiveConfig Hive { get; }
            public NestConfig Nests { get; }
            public SwarmLevelConfig SwarmLevel { get; }
            public SmokeConfig Smoke { get; }
            public WaveConfig Waves { get; }
            public BeekeeperConfig Stick { get; }
            public BeekeeperConfig Smoker { get; }
            public SongConfig SongConfig { get; }
            public SwarmSongEffect[] Songs { get; }
            public ColossusSampleSet Samples { get; }
            public GameObject ColossusPrefab { get; }
            public GameObject HivePrefab { get; }
            public GameObject NestTreePrefab { get; }
            public BeekeeperView StickView { get; }
            public BeekeeperView SmokerView { get; }
            public SmokeGrenadeView Grenade { get; }
            public SmokeCloudView Cloud { get; }
            public GamepadConfig Gamepad { get; }
        }
    }
}
