using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DigitalStory.View
{
    /// <summary>
    /// Development tool, not game code. Switches one part of the scene off at a time and puts it back, so the benchmark
    /// can see what each part costs. It also counts which lights are on each frame. The first-cast switches (the attack
    /// light, the cast flash, the cast sound, the camera kick and the damage numbers) take away one thing that first
    /// happens on a song's first cast, so a run with one of them held shows whether it is the first cast's slow frame.
    /// </summary>
    /// <remarks>
    /// The swarm view and the feel views have no switches of their own, and this tool must not change the game, so what it
    /// switches is reached through their private fields by reflection. A renamed field fails loudly here rather than
    /// measuring nothing.
    /// </remarks>
    internal sealed class BenchmarkSwitchBoard
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string GroundName = "Ground";
        private const string SceneryName = "Scenery";
        private const string GrassName = "Grass Field";
        private const string GrassType = "GrassField";
        private const string WaterName = "Water (Crest)";
        private const string WaterType = "WaterRenderer";
        private const string SetDressingName = "Set Dressing";
        private const string MistName = "Ground Mist";
        private const string MistType = "GroundMist";
        private const string SmokeName = "Smoke Pool";
        private const string AmbienceName = "Ambience";
        private const string MinimapName = "minimap";
        private const int HalfResDivisor = 2;
        private const float ThreeQuarters = 0.75f;
        private const int OneGroup = 1;
        private const int AnimatorRescanFrames = 30;

        private readonly SwarmGpuView _swarmView;
        private readonly HitFeedbackView _feedback;
        private readonly HiveDefenseRoot _root;
        private readonly HitSoundView _sound;
        private readonly DamageNumbersView _numbers;
        private readonly Light _attackLight;
        private readonly int[] _slotFlashFrames;
        private readonly FieldInfo _castKickField;
        private readonly FieldInfo _lastPlayedField;
        private readonly int _castSoundKind;
        private readonly FieldInfo _argsField;
        private readonly FieldInfo _glowArgsField;
        private readonly FieldInfo _groupCountField;
        private readonly FieldInfo _meshField;
        private readonly FieldInfo _glowMeshField;
        private readonly FieldInfo _maxWaspsField;
        private readonly Light[] _allLights;
        private readonly Light[] _swarmLights;
        private readonly Light[] _hiveNestLights;
        private readonly bool[] _isHiveLight;
        private readonly bool[] _isNestLight;
        private readonly bool[] _isSwarmLight;
        private readonly Renderer _ground;
        private readonly Renderer[] _scenery;
        private readonly GraphicsBuffer.IndirectDrawIndexedArgs[] _args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        private readonly Behaviour[] _grass;
        private readonly GameObject _water;
        private readonly Component _waterRenderer;
        private readonly object _waterReflections;
        private readonly PropertyInfo _reflectionsEnabled;
        private readonly Renderer[] _setDressing;
        private readonly Renderer[] _mist;
        private readonly Behaviour[] _mistDepth;
        private readonly GameObject _smokePool;
        private readonly AudioSource[] _ambience;
        private readonly Terrain _terrain;
        private readonly List<Renderer> _smoke = new List<Renderer>();
        private readonly List<(object Target, MemberInfo Member, object Before)> _assigned = new List<(object, MemberInfo, object)>();
        private int _framesSinceSmokeScan;
        private int _savedWidth;
        private int _savedHeight;

        private readonly List<Light> _shadowLights = new List<Light>();
        private readonly List<LightShadows> _shadowKinds = new List<LightShadows>();
        private readonly List<bool> _savedEnabled = new List<bool>();
        private readonly List<Animator> _animators = new List<Animator>();
        private int _savedGroupCount;
        private float _savedCastKick;
        private int _framesSinceAnimatorScan;

        public BenchmarkSwitchBoard(HiveDefenseRoot root, SwarmGpuView swarmView)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _swarmView = swarmView != null ? swarmView : throw new ArgumentNullException(nameof(swarmView));
            _root = root;
            _feedback = Found(UnityEngine.Object.FindAnyObjectByType<HitFeedbackView>(), nameof(HitFeedbackView));
            _sound = Found(UnityEngine.Object.FindAnyObjectByType<HitSoundView>(), nameof(HitSoundView));
            _numbers = Found(UnityEngine.Object.FindAnyObjectByType<DamageNumbersView>(), nameof(DamageNumbersView));
            var attackGlow = (SwarmGlowLight)FieldOf(typeof(HiveDefenseRoot), "_attackGlowLight").GetValue(root);
            _attackLight = Found(attackGlow, "the root's attack light").GetComponent<Light>();
            _slotFlashFrames = (int[])PrivateField("_slotFlashFrames").GetValue(_swarmView);
            _castKickField = FieldOf(typeof(HitFeedbackView), "_castKick");
            _lastPlayedField = FieldOf(typeof(HitSoundView), "_lastPlayed");
            _castSoundKind = (int)(typeof(HitSoundView).GetField("KindCast", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("HitSoundView has no constant 'KindCast' any more. Update BenchmarkSwitchBoard."))
                .GetRawConstantValue();
            _argsField = PrivateField("_argsBuffer");
            _glowArgsField = PrivateField("_glowArgsBuffer");
            _groupCountField = PrivateField("_groupCount");
            _meshField = PrivateField("_mesh");
            _glowMeshField = PrivateField("_glowMesh");
            _maxWaspsField = PrivateField("_maxWasps");

            _allLights = SceneComponents<Light>();
            _isSwarmLight = new bool[_allLights.Length];
            _isHiveLight = new bool[_allLights.Length];
            _isNestLight = new bool[_allLights.Length];
            var swarm = new List<Light>();
            var hiveNest = new List<Light>();
            for (int index = 0; index < _allLights.Length; index++)
            {
                Light light = _allLights[index];
                _isSwarmLight[index] = light.GetComponent<SwarmGlowLight>() != null;
                _isHiveLight[index] = light.GetComponentInParent<HiveView>(true) != null;
                _isNestLight[index] = light.GetComponentInParent<NestView>(true) != null;
                if (_isSwarmLight[index])
                {
                    swarm.Add(light);
                }
                else if (_isHiveLight[index] || _isNestLight[index])
                {
                    hiveNest.Add(light);
                }
            }

            _swarmLights = swarm.ToArray();
            _hiveNestLights = hiveNest.ToArray();
            GameObject ground = GameObject.Find(GroundName);
            _ground = ground != null ? ground.GetComponent<Renderer>() : null;
            GameObject scenery = GameObject.Find(SceneryName);
            _scenery = scenery != null ? scenery.GetComponentsInChildren<Renderer>(true) : new Renderer[0];

            // Raz's parts (plan section AD), found by name: the grass and the mist live in an assembly the View does not
            // reference, and Crest is a package, so their components are reached by type name and reflection.
            _grass = BehavioursNamed(GameObject.Find(GrassName), GrassType);
            _water = GameObject.Find(WaterName);
            if (_water != null)
            {
                _waterRenderer = _water.GetComponent(WaterType);
                PropertyInfo reflections = _waterRenderer != null ? _waterRenderer.GetType().GetProperty("Reflections") : null;
                _waterReflections = reflections != null ? reflections.GetValue(_waterRenderer) : null;
                _reflectionsEnabled = _waterReflections != null ? _waterReflections.GetType().GetProperty("Enabled") : null;
            }

            GameObject setDressing = GameObject.Find(SetDressingName);
            _setDressing = setDressing != null ? setDressing.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            GameObject mist = GameObject.Find(MistName);
            _mist = mist != null ? mist.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            _mistDepth = BehavioursNamed(mist, MistType);
            _smokePool = GameObject.Find(SmokeName);
            Transform ambience = root.transform.Find(AmbienceName);
            _ambience = ambience != null ? ambience.GetComponentsInChildren<AudioSource>(true) : new AudioSource[0];
            _terrain = Terrain.activeTerrain;
        }

        /// <summary>The switch now applied.</summary>
        public BenchmarkSwitch Current { get; private set; } = BenchmarkSwitch.None;

        /// <summary>Every light in the scene, for the run's notes.</summary>
        public IReadOnlyList<Light> Lights => _allLights;

        /// <summary>The ground's renderer, or null when the scene has none.</summary>
        public Renderer Ground => _ground;

        /// <summary>The scenery's renderers.</summary>
        public IReadOnlyList<Renderer> Scenery => _scenery;

        /// <summary>One line per part of Raz's map the switches reach, for the run's notes: what was found and how much.</summary>
        public string DescribeMap()
        {
            return $"switch parts: grass fields {_grass.Length}, water {(_water != null ? "found" : "missing")}, "
                + $"water reflections {(_reflectionsEnabled != null ? "found" : "missing")}, set dressing renderers {_setDressing.Length}, "
                + $"mist renderers {_mist.Length}, mist depth {_mistDepth.Length}, smoke pool {(_smokePool != null ? "found" : "missing")}, "
                + $"ambience sources {_ambience.Length}, terrain {(_terrain != null ? _terrain.name : "missing")}";
        }

        /// <summary>Switches one thing off. The switch before it must have been restored.</summary>
        public void Apply(BenchmarkSwitch change)
        {
            if (Current != BenchmarkSwitch.None)
            {
                throw new InvalidOperationException($"Restore {Current} before applying {change}.");
            }

            Current = change;
            _savedEnabled.Clear();
            switch (change)
            {
                case BenchmarkSwitch.NoGlow:
                    WriteInstances(_glowArgsField, _glowMeshField, 0);
                    break;
                case BenchmarkSwitch.NoWaspDraw:
                    WriteInstances(_argsField, _meshField, 0);
                    break;
                case BenchmarkSwitch.NoCompute:
                    _savedGroupCount = (int)_groupCountField.GetValue(_swarmView);
                    _groupCountField.SetValue(_swarmView, OneGroup);
                    break;
                case BenchmarkSwitch.NoHiveNestLights:
                    SaveAndSwitchOff(_hiveNestLights);
                    break;
                case BenchmarkSwitch.NoShadows:
                    SwitchOffShadows();
                    break;
                case BenchmarkSwitch.NoGround:
                    if (_ground != null)
                    {
                        _savedEnabled.Add(_ground.enabled);
                        _ground.enabled = false;
                    }

                    break;
                case BenchmarkSwitch.NoScenery:
                    SaveAndSwitchOff(_scenery);
                    break;
                case BenchmarkSwitch.NoAnimators:
                    FindBeekeeperAnimators();
                    break;
                case BenchmarkSwitch.NoHud:
                    SwitchOffHud();
                    break;
                case BenchmarkSwitch.NoAttackLight:
                    _savedEnabled.Add(_attackLight.enabled);
                    break;
                case BenchmarkSwitch.NoCameraKick:
                    _savedCastKick = (float)_castKickField.GetValue(_feedback);
                    _castKickField.SetValue(_feedback, 0f);
                    break;
                case BenchmarkSwitch.NoCursorLock:
                    // Not restored, as the run holds it to the end.
                    FieldOf(typeof(HiveDefenseRoot), "_isCursorLockedInChase").SetValue(_root, false);
                    break;
                case BenchmarkSwitch.NoDamageNumbers:
                    // Its private Release stops it listening for hits. Not restored, as the run holds it to the end.
                    MethodOf(typeof(DamageNumbersView), "Release").Invoke(_numbers, null);
                    break;
                case BenchmarkSwitch.NoGrass:
                    SaveAndSwitchOff(_grass);
                    break;
                case BenchmarkSwitch.NoWater:
                    Required(_water, WaterName).SetActive(false);
                    break;
                case BenchmarkSwitch.NoWaterReflections:
                    Required(_reflectionsEnabled, "Crest's reflections").SetValue(_waterReflections, false);
                    break;
                case BenchmarkSwitch.NoForest:
                    SaveAndSwitchOff(_setDressing);
                    break;
                case BenchmarkSwitch.NoTerrain:
                    Required(_terrain, "terrain").drawHeightmap = false;
                    _terrain.drawTreesAndFoliage = false;
                    break;
                case BenchmarkSwitch.NoMist:
                    SaveAndSwitchOff(_mist);
                    break;
                case BenchmarkSwitch.NoMinimap:
                    Minimap().style.display = DisplayStyle.None;
                    break;
                case BenchmarkSwitch.NoSmoke:
                    _framesSinceSmokeScan = AnimatorRescanFrames;
                    break;
                case BenchmarkSwitch.NoAmbience:
                    foreach (AudioSource source in _ambience)
                    {
                        source.enabled = false;
                    }

                    break;
                case BenchmarkSwitch.WaterSlices6:
                    Assign(Water(), "LodLevels", 6);
                    break;
                case BenchmarkSwitch.WaterRes256:
                    Assign(Water(), "Resolution", 256);
                    break;
                case BenchmarkSwitch.WaterNoShadowSim:
                    Assign(WaterPart("ShadowLod"), "Enabled", false);
                    break;
                case BenchmarkSwitch.WaterNoFoamSim:
                    Assign(WaterPart("FoamLod"), "Enabled", false);
                    break;
                case BenchmarkSwitch.WaterNoUnderwater:
                    Assign(WaterPart("Underwater"), "Enabled", false);
                    Assign(WaterPart("Meniscus"), "Enabled", false);
                    break;
                case BenchmarkSwitch.WaterReflectHalf:
                    // Crest only skips frames for a reflection that serves one camera.
                    Assign(_waterReflections, "_RenderOnlySingleCamera", true);
                    Assign(_waterReflections, "_RefreshPerFrames", 2);
                    break;
                case BenchmarkSwitch.NoHdr:
                    Assign(Camera.main, "allowHDR", false);
                    break;
                case BenchmarkSwitch.NoMsaa:
                    Assign(null, typeof(QualitySettings), "antiAliasing", 0);
                    break;
                case BenchmarkSwitch.GrassThin:
                    Assign(Required(_grass.Length > 0 ? _grass[0] : null, "grass field"), "_farKeep", 0.35f);
                    Assign(_grass[0], "_cellsDirty", true);
                    break;
                case BenchmarkSwitch.ShadowLite:
                    Assign(null, typeof(QualitySettings), "shadowDistance", 30f);
                    Assign(null, typeof(QualitySettings), "shadowResolution", ShadowResolution.Medium);
                    break;
                case BenchmarkSwitch.Scale75:
                    _savedWidth = Screen.currentResolution.width;
                    _savedHeight = Screen.currentResolution.height;
                    Screen.SetResolution(Mathf.RoundToInt(_savedWidth * ThreeQuarters), Mathf.RoundToInt(_savedHeight * ThreeQuarters), Screen.fullScreenMode);
                    break;
                case BenchmarkSwitch.DynRes75:
                    Assign(Camera.main, "allowDynamicResolution", true);
                    ScalableBufferManager.ResizeBuffers(ThreeQuarters, ThreeQuarters);
                    break;
                case BenchmarkSwitch.HalfRes:
                    // From the display's size, not the window's, so a switch held over a fresh match never halves twice.
                    _savedWidth = Screen.currentResolution.width;
                    _savedHeight = Screen.currentResolution.height;
                    Screen.SetResolution(_savedWidth / HalfResDivisor, _savedHeight / HalfResDivisor, Screen.fullScreenMode);
                    break;
            }

            ForceAfterUpdate();
            ForceEachFrame();
        }

        /// <summary>Puts back what the current switch turned off.</summary>
        public void Restore()
        {
            switch (Current)
            {
                case BenchmarkSwitch.NoGlow:
                    WriteInstances(_glowArgsField, _glowMeshField, (int)_maxWaspsField.GetValue(_swarmView));
                    break;
                case BenchmarkSwitch.NoWaspDraw:
                    WriteInstances(_argsField, _meshField, (int)_maxWaspsField.GetValue(_swarmView));
                    break;
                case BenchmarkSwitch.NoCompute:
                    _groupCountField.SetValue(_swarmView, _savedGroupCount);
                    break;
                case BenchmarkSwitch.NoHiveNestLights:
                    RestoreEnabled(_hiveNestLights);
                    break;
                case BenchmarkSwitch.NoShadows:
                    for (int index = 0; index < _shadowLights.Count; index++)
                    {
                        _shadowLights[index].shadows = _shadowKinds[index];
                    }

                    break;
                case BenchmarkSwitch.NoGround:
                    if (_ground != null)
                    {
                        _ground.enabled = _savedEnabled[0];
                    }

                    break;
                case BenchmarkSwitch.NoScenery:
                    RestoreEnabled(_scenery);
                    break;
                case BenchmarkSwitch.NoAnimators:
                    for (int index = 0; index < _animators.Count; index++)
                    {
                        if (_animators[index] != null)
                        {
                            _animators[index].enabled = true;
                        }
                    }

                    break;
                case BenchmarkSwitch.NoAttackLight:
                    _attackLight.enabled = _savedEnabled[0];
                    break;
                case BenchmarkSwitch.NoCastSound:
                    ((float[])_lastPlayedField.GetValue(_sound))[_castSoundKind] = float.NegativeInfinity;
                    break;
                case BenchmarkSwitch.NoCameraKick:
                    _castKickField.SetValue(_feedback, _savedCastKick);
                    break;
                case BenchmarkSwitch.NoGrass:
                    RestoreEnabled(_grass);
                    break;
                case BenchmarkSwitch.NoWater:
                    _water.SetActive(true);
                    break;
                case BenchmarkSwitch.NoWaterReflections:
                    _reflectionsEnabled.SetValue(_waterReflections, true);
                    break;
                case BenchmarkSwitch.NoForest:
                    RestoreEnabled(_setDressing);
                    break;
                case BenchmarkSwitch.NoTerrain:
                    _terrain.drawHeightmap = true;
                    _terrain.drawTreesAndFoliage = true;
                    break;
                case BenchmarkSwitch.NoMist:
                    RestoreEnabled(_mist);
                    break;
                case BenchmarkSwitch.NoMinimap:
                    Minimap().style.display = DisplayStyle.Flex;
                    break;
                case BenchmarkSwitch.NoSmoke:
                    foreach (Renderer renderer in _smoke)
                    {
                        if (renderer != null)
                        {
                            renderer.enabled = true;
                        }
                    }

                    _smoke.Clear();
                    break;
                case BenchmarkSwitch.NoAmbience:
                    foreach (AudioSource source in _ambience)
                    {
                        source.enabled = true;
                    }

                    break;
                case BenchmarkSwitch.HalfRes:
                case BenchmarkSwitch.Scale75:
                    Screen.SetResolution(_savedWidth, _savedHeight, Screen.fullScreenMode);
                    break;
                case BenchmarkSwitch.DynRes75:
                    ScalableBufferManager.ResizeBuffers(1f, 1f);
                    break;
            }

            // Every value a switch assigned goes back, last first.
            for (int index = _assigned.Count - 1; index >= 0; index--)
            {
                (object target, MemberInfo member, object before) = _assigned[index];
                Write(target, member, before);
            }

            if (Current == BenchmarkSwitch.GrassThin)
            {
                Write(_grass[0], _grass[0].GetType().GetField("_cellsDirty", Private), true);
            }

            _assigned.Clear();
            Current = BenchmarkSwitch.None;
        }

        /// <summary>
        /// Takes away what a cast set off during this frame's Update before any LateUpdate shows it: the squad flash the swarm
        /// would draw, and the cast sound's next play. Call from an Update that runs after the root's.
        /// </summary>
        public void ForceAfterUpdate()
        {
            if (Current == BenchmarkSwitch.NoCastFlash)
            {
                Array.Clear(_slotFlashFrames, 0, _slotFlashFrames.Length);
            }
            else if (Current == BenchmarkSwitch.NoCastSound)
            {
                // The sound view never plays a kind sooner than its gap after the last play, so a last play at infinity
                // silences the cast sound alone.
                ((float[])_lastPlayedField.GetValue(_sound))[_castSoundKind] = float.PositiveInfinity;
            }
        }

        /// <summary>
        /// Keeps a switch off against the scripts that turn things back on every frame (the swarm's lights). Call after
        /// every other LateUpdate, before the frame renders.
        /// </summary>
        public void ForceEachFrame()
        {
            if (Current == BenchmarkSwitch.NoAttackLight)
            {
                _attackLight.enabled = false;
            }
            else if (Current == BenchmarkSwitch.NoSwarmLights)
            {
                for (int index = 0; index < _swarmLights.Length; index++)
                {
                    _swarmLights[index].enabled = false;
                }
            }
            else if (Current == BenchmarkSwitch.NoSmoke)
            {
                // The pool shows clouds made after the switch was applied, so look again now and then.
                if (++_framesSinceSmokeScan >= AnimatorRescanFrames && _smokePool != null)
                {
                    _framesSinceSmokeScan = 0;
                    _smokePool.GetComponentsInChildren(true, _smoke);
                }

                for (int index = 0; index < _smoke.Count; index++)
                {
                    if (_smoke[index] != null)
                    {
                        _smoke[index].enabled = false;
                    }
                }
            }
            else if (Current == BenchmarkSwitch.NoAnimators)
            {
                // Beekeepers shown after the switch was applied bring their own Animators, so look again now and then.
                if (++_framesSinceAnimatorScan >= AnimatorRescanFrames)
                {
                    FindBeekeeperAnimators();
                }

                for (int index = 0; index < _animators.Count; index++)
                {
                    if (_animators[index] != null)
                    {
                        _animators[index].enabled = false;
                    }
                }
            }
        }

        /// <summary>Counts the lights that will draw this frame: all, forced per-pixel, swarm, hive and nest.</summary>
        public void CountLights(out int on, out int forcedPixel, out int swarm, out int hive, out int nest)
        {
            on = 0;
            forcedPixel = 0;
            swarm = 0;
            hive = 0;
            nest = 0;
            for (int index = 0; index < _allLights.Length; index++)
            {
                Light light = _allLights[index];
                if (light == null || !light.isActiveAndEnabled || light.intensity <= 0f)
                {
                    continue;
                }

                on++;
                forcedPixel += light.renderMode == LightRenderMode.ForcePixel ? 1 : 0;
                swarm += _isSwarmLight[index] ? 1 : 0;
                hive += _isHiveLight[index] ? 1 : 0;
                nest += _isNestLight[index] ? 1 : 0;
            }
        }

        private static FieldInfo PrivateField(string name) => FieldOf(typeof(SwarmGpuView), name);

        private static FieldInfo FieldOf(Type type, string name)
        {
            return type.GetField(name, Private)
                ?? throw new InvalidOperationException($"{type.Name} has no field '{name}' any more. Update BenchmarkSwitchBoard.");
        }

        private static MethodInfo MethodOf(Type type, string name)
        {
            return type.GetMethod(name, Private)
                ?? throw new InvalidOperationException($"{type.Name} has no method '{name}' any more. Update BenchmarkSwitchBoard.");
        }

        private static Behaviour[] BehavioursNamed(GameObject holder, string typeName)
        {
            var found = new List<Behaviour>();
            if (holder != null)
            {
                foreach (Behaviour behaviour in holder.GetComponentsInChildren<Behaviour>(true))
                {
                    if (behaviour != null && behaviour.GetType().Name == typeName)
                    {
                        found.Add(behaviour);
                    }
                }
            }

            return found.ToArray();
        }

        private static T Required<T>(T found, string what) where T : class
        {
            bool isMissing = found is UnityEngine.Object unityObject ? unityObject == null : found == null;
            return isMissing ? throw new InvalidOperationException($"The scene has no {what} for this switch.") : found;
        }

        private Component Water() => Required(_waterRenderer, "Crest water renderer");

        private object WaterPart(string name)
        {
            PropertyInfo part = Water().GetType().GetProperty(name)
                ?? throw new InvalidOperationException($"Crest's water renderer has no '{name}' any more. Update BenchmarkSwitchBoard.");
            return Required(part.GetValue(_waterRenderer), name);
        }

        // Sets a property or a field (public or not) and remembers the value before, for Restore.
        private void Assign(object target, string name, object value) => Assign(target, target.GetType(), name, value);

        private void Assign(object target, Type type, string name, object value)
        {
            const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MemberInfo member = (MemberInfo)type.GetProperty(name, Any) ?? type.GetField(name, Any)
                ?? throw new InvalidOperationException($"{type.Name} has no '{name}' any more. Update BenchmarkSwitchBoard.");
            object before = member is PropertyInfo property ? property.GetValue(target) : ((FieldInfo)member).GetValue(target);
            _assigned.Add((target, member, before));
            Write(target, member, value);
        }

        private static void Write(object target, MemberInfo member, object value)
        {
            if (member is PropertyInfo property)
            {
                property.SetValue(target, value);
            }
            else
            {
                ((FieldInfo)member).SetValue(target, value);
            }
        }

        private VisualElement Minimap()
        {
            HiveDefenseHudView hud = Found(UnityEngine.Object.FindAnyObjectByType<HiveDefenseHudView>(), nameof(HiveDefenseHudView));
            VisualElement map = hud.GetComponent<UIDocument>().rootVisualElement.Q(MinimapName);
            return Required(map, "minimap");
        }

        private static T Found<T>(T found, string what) where T : UnityEngine.Object
        {
            return found != null ? found : throw new InvalidOperationException($"The hive defense scene has no {what}.");
        }

        private static T[] SceneComponents<T>() where T : Component
        {
            var found = new List<T>();
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                foreach (GameObject root in SceneManager.GetSceneAt(sceneIndex).GetRootGameObjects())
                {
                    found.AddRange(root.GetComponentsInChildren<T>(true));
                }
            }

            return found.ToArray();
        }

        // The draw keeps its call but draws this many instances; the args buffer is only written when it is made, so it
        // stays this way until restored.
        private void WriteInstances(FieldInfo argsField, FieldInfo meshField, int instances)
        {
            var args = (GraphicsBuffer)argsField.GetValue(_swarmView);
            var mesh = (Mesh)meshField.GetValue(_swarmView);
            if (args == null || mesh == null)
            {
                throw new InvalidOperationException("The swarm view has no draw to switch: is it running?");
            }

            _args[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = mesh.GetIndexCount(0),
                instanceCount = (uint)instances,
                startIndex = mesh.GetIndexStart(0),
                baseVertexIndex = mesh.GetBaseVertex(0),
                startInstance = 0,
            };
            args.SetData(_args);
        }

        private void SaveAndSwitchOff(Light[] lights)
        {
            foreach (Light light in lights)
            {
                _savedEnabled.Add(light.enabled);
                light.enabled = false;
            }
        }

        private void SaveAndSwitchOff(Renderer[] renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                _savedEnabled.Add(renderer.enabled);
                renderer.enabled = false;
            }
        }

        private void RestoreEnabled(Light[] lights)
        {
            for (int index = 0; index < lights.Length; index++)
            {
                lights[index].enabled = _savedEnabled[index];
            }
        }

        private void SaveAndSwitchOff(Behaviour[] behaviours)
        {
            foreach (Behaviour behaviour in behaviours)
            {
                _savedEnabled.Add(behaviour.enabled);
                behaviour.enabled = false;
            }
        }

        private void RestoreEnabled(Behaviour[] behaviours)
        {
            for (int index = 0; index < behaviours.Length; index++)
            {
                behaviours[index].enabled = _savedEnabled[index];
            }
        }

        private void RestoreEnabled(Renderer[] renderers)
        {
            for (int index = 0; index < renderers.Length; index++)
            {
                renderers[index].enabled = _savedEnabled[index];
            }
        }

        private void SwitchOffShadows()
        {
            _shadowLights.Clear();
            _shadowKinds.Clear();
            foreach (Light light in _allLights)
            {
                if (light != null && light.shadows != LightShadows.None)
                {
                    _shadowLights.Add(light);
                    _shadowKinds.Add(light.shadows);
                    light.shadows = LightShadows.None;
                }
            }
        }

        private void FindBeekeeperAnimators()
        {
            _framesSinceAnimatorScan = 0;
            _animators.Clear();
            foreach (BeekeeperView view in SceneComponents<BeekeeperView>())
            {
                _animators.AddRange(view.GetComponentsInChildren<Animator>(true));
            }
        }

        // The UIDocument's panel and the view that fills it. Turning the document back on would clone a fresh tree that
        // the view no longer fills, so this switch is never restored and runs last.
        private void SwitchOffHud()
        {
            HiveDefenseHudView hud = UnityEngine.Object.FindAnyObjectByType<HiveDefenseHudView>();
            if (hud == null)
            {
                return;
            }

            hud.enabled = false;
            UIDocument document = hud.GetComponent<UIDocument>();
            if (document != null)
            {
                document.enabled = false;
            }
        }
    }
}
