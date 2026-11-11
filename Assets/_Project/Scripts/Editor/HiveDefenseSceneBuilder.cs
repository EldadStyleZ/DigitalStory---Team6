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
    /// <summary>Creates every F-010 asset, the prefab variants and the Wasp Piper scene from code.</summary>
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
        private const string PiperSourcePath = RazPrefabFolder + "/Beekeeper_LowPoly.prefab";
        private const string HiveSourcePath = RazPrefabFolder + "/Beehive_LowPoly 1.prefab";
        private const string PiperFbxPath = Root + "/Art/model/Beekeeper_LowPoly.fbx";
        private const string MixamoFolder = Root + "/Art/Mixamo";
        private const string ControllerPath = ArtFolder + "/PiperSwarm.controller";
        private const string ComputePath = ArtFolder + "/Shaders/WaspSwarm.compute";
        private const string WaspShaderPath = ArtFolder + "/Shaders/WaspInstanced.shader";
        private const string PanelSettingsPath = Root + "/UI/GamePanelSettings.asset";
        private const string HudUxmlPath = Root + "/UI/F010/HiveDefenseHud.uxml";

        private const string SpeedParameter = "Speed";
        private const string IsPlayingParameter = "IsPlaying";
        private const float MovingThreshold = 0.1f;
        private const float BlendSeconds = 0.1f;

        private const string IdleClip = "Idle";
        private const string RunClip = "Run";
        private const string PlayClip = "PlayFluteTwoHands";
        private const string RunPlayClip = "RunFluteTwoHands";

        private const float PiperHeight = 1.8f;
        private const float BeekeeperHeight = 1.8f;
        private const float HiveHeight = 1.6f;
        private const float HiveBarGap = 0.6f;
        private const float PiperRadius = 0.35f;

        private const float GroundSize = 70f;
        private const float PlaneBaseSize = 10f;
        private const float ClearHalfSize = 25f;
        private const float EdgeHalfSize = 34f;
        private const float HiveSpacing = 25f;
        private const float SpawnDistance = 32f;
        private const float SpawnClearance = 4f;
        private const int SceneryCount = 70;
        private const int SceneryAttempts = 30;
        private const int SceneSeed = 1010;

        private const string RightHandName = "RightHand";
        private const string FluteName = "Flute";

        private static readonly Color Grass = new Color(0.36f, 0.55f, 0.24f, 1f);
        private static readonly Color SuitWhite = new Color(0.94f, 0.94f, 0.9f, 1f);
        private static readonly Color StickWood = new Color(0.4f, 0.26f, 0.13f, 1f);
        private static readonly Color SmokerMetal = new Color(0.45f, 0.47f, 0.5f, 1f);
        private static readonly Color SunColor = new Color(1f, 0.9f, 0.72f, 1f);
        private static readonly Color SkyColor = new Color(0.62f, 0.78f, 0.95f, 1f);
        private static readonly Color AmbientSky = new Color(0.72f, 0.8f, 0.92f, 1f);
        private static readonly Color AmbientEquator = new Color(0.6f, 0.58f, 0.5f, 1f);
        private static readonly Color AmbientGround = new Color(0.3f, 0.27f, 0.2f, 1f);

        // Every config and song asset BuildAll loads or creates. The baked ColossusSampleSet is not a config.
        private static readonly string[] ConfigAssetNames =
        {
            "SwarmConfig", "PiperMoveConfig", "FuryConfig", "ColossusConfig", "HiveConfig", "SmokeConfig", "WaveConfig",
            "F010SongConfig", "StickBeekeeper", "SmokerBeekeeper",
            "DomeSong", "StingSong", "TornadoSong", "GustSong", "HiveCallSong", "ColossusSong",
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
            Debug.Log("[F010] Folders ready.");

            // Starting a new scene unloads assets that nothing uses, so it must come before loading them.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var swarm = LoadOrCreate<SwarmConfig>("SwarmConfig");
            var piperMove = LoadOrCreate<PiperMoveConfig>("PiperMoveConfig");
            var fury = LoadOrCreate<FuryConfig>("FuryConfig");
            var colossus = LoadOrCreate<ColossusConfig>("ColossusConfig");
            var hive = LoadOrCreate<HiveConfig>("HiveConfig");
            var smoke = LoadOrCreate<SmokeConfig>("SmokeConfig");
            var waves = LoadOrCreate<WaveConfig>("WaveConfig");
            var songConfig = LoadOrCreate<SongConfig>("F010SongConfig");
            BeekeeperConfig stick = LoadOrCreate<BeekeeperConfig>("StickBeekeeper");
            BeekeeperConfig smoker = LoadOrCreateSmoker();
            SwarmSongEffect[] songs = LoadOrCreateSongs();
            var samples = LoadOrCreate<ColossusSampleSet>("ColossusSampleSet");
            Debug.Log("[F010] Config assets ready.");

            AnimatorController controller = BuildController();
            Debug.Log("[F010] Animator controller PiperSwarm ready.");

            var piperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PiperSourcePath);
            var hivePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HiveSourcePath);
            Require(piperPrefab, PiperSourcePath);
            Require(hivePrefab, HiveSourcePath);

            GameObject piper = MakePiperPrefab(piperPrefab, controller);
            Debug.Log("[F010] Prefab F010Piper saved.");
            BeekeeperView stickView = MakeBeekeeperPrefab("F010StickBeekeeper", piperPrefab, controller, false);
            BeekeeperView smokerView = MakeBeekeeperPrefab("F010SmokerBeekeeper", piperPrefab, controller, true);
            Debug.Log("[F010] Prefabs F010StickBeekeeper and F010SmokerBeekeeper saved.");
            GameObject colossusPrefab = MakeColossusPrefab(piperPrefab, controller, colossus);
            BakeSamples(colossusPrefab, swarm, samples);
            Debug.Log("[F010] Prefab F010Colossus saved and its samples baked.");
            var grenade = MakeViewPrefab<SmokeGrenadeView>("F010SmokeGrenade");
            var cloud = MakeViewPrefab<SmokeCloudView>("F010SmokeCloud");
            Debug.Log("[F010] Smoke prefabs saved.");

            AssetDatabase.SaveAssets();
            FillScene(new SceneAssets(
                swarm, piperMove, fury, colossus, hive, smoke, waves, stick, smoker, songConfig, songs, samples,
                piper, colossusPrefab, hivePrefab, stickView, smokerView, grenade, cloud));
            Debug.Log("[F010] Scene filled.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
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

        // The order is the spec's table: Dome, Sting, Tornado, Gust, Hive Call, Colossus.
        private static SwarmSongEffect[] LoadOrCreateSongs()
        {
            return new SwarmSongEffect[]
            {
                LoadOrCreate<DomeSong>("DomeSong"),
                LoadOrCreate<StingSong>("StingSong"),
                LoadOrCreate<TornadoSong>("TornadoSong"),
                LoadOrCreate<GustSong>("GustSong"),
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
            controller.AddParameter(IsPlayingParameter, AnimatorControllerParameterType.Bool);

            AnimatorState idle = AddState(machine, IdleClip, false);
            AnimatorState run = AddState(machine, RunClip, true);
            AnimatorState play = AddState(machine, PlayClip, false);
            AnimatorState runPlay = AddState(machine, RunPlayClip, true);
            machine.defaultState = idle;

            // Each state can go to the state that matches the two conditions: moving, and playing.
            // The index is (moving ? 1 : 0) + (playing ? 2 : 0).
            AnimatorState[] byCondition = { idle, run, play, runPlay };
            for (int from = 0; from < byCondition.Length; from++)
            {
                for (int to = 0; to < byCondition.Length; to++)
                {
                    if (from == to)
                    {
                        continue;
                    }

                    AnimatorStateTransition transition = byCondition[from].AddTransition(byCondition[to]);
                    transition.hasExitTime = false;
                    transition.hasFixedDuration = true;
                    transition.duration = BlendSeconds;
                    transition.AddCondition((to & 1) != 0 ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, MovingThreshold, SpeedParameter);
                    transition.AddCondition((to & 2) != 0 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, IsPlayingParameter);
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string clipName, bool isRun)
        {
            AnimationClip clip = FindClip(clipName);
            if (!clip.isLooping)
            {
                Debug.LogWarning($"[F010] The clip '{clipName}' in {PiperFbxPath} is not set to loop. The importer is left as it is; ask Raz to tick Loop Time.");
            }

            AnimatorState state = machine.AddState(clipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            if (isRun)
            {
                // The Run clip plays faster the faster the Piper moves. Speed is 1 at the full walking speed.
                state.speedParameterActive = true;
                state.speedParameter = SpeedParameter;
                state.speed = 1f;
            }

            return state;
        }

        private static AnimationClip FindClip(string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PiperFbxPath);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is AnimationClip clip && clip.name == clipName && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            throw new InvalidOperationException($"{PiperFbxPath} has no animation clip named '{clipName}'.");
        }

        // ---------------------------------------------------------------- Prefabs

        private static GameObject MakePiperPrefab(GameObject source, AnimatorController controller)
        {
            var root = new GameObject("F010Piper", typeof(CharacterController), typeof(PiperMotor));
            var body = root.GetComponent<CharacterController>();
            body.height = PiperHeight;
            body.radius = PiperRadius;

            // A CharacterController rests its skin width above the ground, so the capsule is raised by that much
            // and the feet, at the root, touch the grass.
            body.center = new Vector3(0f, (PiperHeight * 0.5f) + body.skinWidth, 0f);

            GameObject model = InstantiateModel(source, root.transform);
            FitToHeight(model.transform, PiperHeight);
            UseController(model, controller);
            return SavePrefab(root, "F010Piper");
        }

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
        private static GameObject MakeColossusPrefab(GameObject piperSource, AnimatorController controller, ColossusConfig config)
        {
            GameObject source = FindMixamoModel() ?? piperSource;
            var root = new GameObject("F010Colossus", typeof(ColossusMotor));
            GameObject model = InstantiateModel(source, root.transform);
            FitToHeight(model.transform, config.Height);
            Animator animator = UseController(model, controller);
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

        private static GameObject FindMixamoModel()
        {
            if (!AssetDatabase.IsValidFolder(MixamoFolder))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder });
            var paths = new List<string>(guids.Length);
            for (int index = 0; index < guids.Length; index++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[index]));
            }

            paths.Sort(StringComparer.Ordinal);
            for (int index = 0; index < paths.Count; index++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(paths[index]);
                if (model != null && model.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                {
                    Debug.Log($"[F010] The Colossus uses the Mixamo model {paths[index]}.");
                    Debug.LogWarning("[F010] The Colossus controller is made from the Piper's clips. A Mixamo Humanoid needs its own clips to move.");
                    return model;
                }
            }

            return null;
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
            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            return animator;
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
                        made[original] = suit;
                    }

                    suits[slot] = suit;
                }

                renderers[index].sharedMaterials = suits;
            }
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

        private static void FillScene(SceneAssets assets)
        {
            BuildLighting();
            BuildGround();
            Transform spawns = BuildSpawnPoints();
            BuildScenery(spawns);

            var hives = new HiveView[2];
            var hiveSource = AssetDatabase.LoadAssetAtPath<GameObject>(HiveSourcePath);
            hives[0] = BuildHive(hiveSource, "Hive A", new Vector3(-HiveSpacing * 0.5f, 0f, 0f));
            hives[1] = BuildHive(hiveSource, "Hive B", new Vector3(HiveSpacing * 0.5f, 0f, 0f));

            var piper = (GameObject)PrefabUtility.InstantiatePrefab(assets.PiperPrefab);
            piper.name = "Piper";
            piper.transform.position = Vector3.zero;
            var colossus = (GameObject)PrefabUtility.InstantiatePrefab(assets.ColossusPrefab);
            colossus.name = "Colossus Puppet";
            colossus.transform.position = new Vector3(0f, 0f, -ClearHalfSize * 0.5f);

            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(FollowCamera));
            camera.tag = "MainCamera";
            var cameraComponent = camera.GetComponent<Camera>();
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = SkyColor;
            cameraComponent.farClipPlane = 200f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 14f, -10f), Quaternion.Euler(55f, 0f, 0f));

            var input = new GameObject("Hive Defense Input", typeof(HiveDefenseInput));

            var swarmObject = new GameObject("Wasp Swarm", typeof(SwarmGpuView));
            var swarmSerialized = new SerializedObject(swarmObject.GetComponent<SwarmGpuView>());
            SetObject(swarmSerialized, "_swarmCompute", AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath));
            SetObject(swarmSerialized, "_waspShader", AssetDatabase.LoadAssetAtPath<Shader>(WaspShaderPath));
            swarmSerialized.ApplyModifiedPropertiesWithoutUndo();

            var beekeeperPool = new GameObject("Beekeeper Pool", typeof(BeekeeperViewPool));
            var smokePool = new GameObject("Smoke Pool", typeof(SmokeViewPool));

            var hud = new GameObject("Hive Defense HUD", typeof(UIDocument), typeof(HiveDefenseHudView));
            var document = hud.GetComponent<UIDocument>();
            document.panelSettings = Require(AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath), PanelSettingsPath);
            document.visualTreeAsset = Require(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath), HudUxmlPath);
            var hudSerialized = new SerializedObject(hud.GetComponent<HiveDefenseHudView>());
            SetObject(hudSerialized, "_camera", cameraComponent);
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            var rootObject = new GameObject("Hive Defense Root", typeof(HiveDefenseRoot));
            var serialized = new SerializedObject(rootObject.GetComponent<HiveDefenseRoot>());
            SetObject(serialized, "_swarm", assets.Swarm);
            SetObject(serialized, "_piperMove", assets.PiperMove);
            SetObject(serialized, "_fury", assets.Fury);
            SetObject(serialized, "_colossus", assets.Colossus);
            SetObject(serialized, "_hive", assets.Hive);
            SetObject(serialized, "_smoke", assets.Smoke);
            SetObject(serialized, "_waves", assets.Waves);
            SetObject(serialized, "_stick", assets.Stick);
            SetObject(serialized, "_smoker", assets.Smoker);
            SetObject(serialized, "_songConfig", assets.SongConfig);
            SetObjects(serialized, "_songs", assets.Songs);
            SetObject(serialized, "_colossusSamples", assets.Samples);
            SetObject(serialized, "_input", input.GetComponent<HiveDefenseInput>());
            SetObject(serialized, "_piperMotor", piper.GetComponent<PiperMotor>());
            SetObject(serialized, "_piperAnimator", piper.GetComponentInChildren<Animator>(true));
            SetObject(serialized, "_followCamera", camera.GetComponent<FollowCamera>());
            SetObject(serialized, "_camera", cameraComponent);
            SetObject(serialized, "_swarmView", swarmObject.GetComponent<SwarmGpuView>());
            SetObjects(serialized, "_hives", hives);
            SetObjects(serialized, "_spawnPoints", ChildTransforms(spawns));
            SetObject(serialized, "_beekeeperPool", beekeeperPool.GetComponent<BeekeeperViewPool>());
            SetObject(serialized, "_stickPrefab", assets.StickView);
            SetObject(serialized, "_smokerPrefab", assets.SmokerView);
            SetObject(serialized, "_smokePool", smokePool.GetComponent<SmokeViewPool>());
            SetObject(serialized, "_grenadePrefab", assets.Grenade);
            SetObject(serialized, "_cloudPrefab", assets.Cloud);
            SetObject(serialized, "_colossusMotor", colossus.GetComponent<ColossusMotor>());
            SetObject(serialized, "_hud", hud.GetComponent<HiveDefenseHudView>());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildLighting()
        {
            var sun = new GameObject("Sun", typeof(Light));
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.fog = false;
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
        private static void BuildScenery(Transform spawns)
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
            Property(serialized, "_barHeight").floatValue = HiveHeight + HiveBarGap;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return hive.GetComponent<HiveView>();
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
                PiperMoveConfig piperMove,
                FuryConfig fury,
                ColossusConfig colossus,
                HiveConfig hive,
                SmokeConfig smoke,
                WaveConfig waves,
                BeekeeperConfig stick,
                BeekeeperConfig smoker,
                SongConfig songConfig,
                SwarmSongEffect[] songs,
                ColossusSampleSet samples,
                GameObject piperPrefab,
                GameObject colossusPrefab,
                GameObject hivePrefab,
                BeekeeperView stickView,
                BeekeeperView smokerView,
                SmokeGrenadeView grenade,
                SmokeCloudView cloud)
            {
                Swarm = swarm;
                PiperMove = piperMove;
                Fury = fury;
                Colossus = colossus;
                Hive = hive;
                Smoke = smoke;
                Waves = waves;
                Stick = stick;
                Smoker = smoker;
                SongConfig = songConfig;
                Songs = songs;
                Samples = samples;
                PiperPrefab = piperPrefab;
                ColossusPrefab = colossusPrefab;
                HivePrefab = hivePrefab;
                StickView = stickView;
                SmokerView = smokerView;
                Grenade = grenade;
                Cloud = cloud;
            }

            public SwarmConfig Swarm { get; }
            public PiperMoveConfig PiperMove { get; }
            public FuryConfig Fury { get; }
            public ColossusConfig Colossus { get; }
            public HiveConfig Hive { get; }
            public SmokeConfig Smoke { get; }
            public WaveConfig Waves { get; }
            public BeekeeperConfig Stick { get; }
            public BeekeeperConfig Smoker { get; }
            public SongConfig SongConfig { get; }
            public SwarmSongEffect[] Songs { get; }
            public ColossusSampleSet Samples { get; }
            public GameObject PiperPrefab { get; }
            public GameObject ColossusPrefab { get; }
            public GameObject HivePrefab { get; }
            public BeekeeperView StickView { get; }
            public BeekeeperView SmokerView { get; }
            public SmokeGrenadeView Grenade { get; }
            public SmokeCloudView Cloud { get; }
        }
    }
}
