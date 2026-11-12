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
        private const int OneGroup = 1;
        private const int AnimatorRescanFrames = 30;

        private readonly SwarmGpuView _swarmView;
        private readonly HitFeedbackView _feedback;
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
        }

        /// <summary>The switch now applied.</summary>
        public BenchmarkSwitch Current { get; private set; } = BenchmarkSwitch.None;

        /// <summary>Every light in the scene, for the run's notes.</summary>
        public IReadOnlyList<Light> Lights => _allLights;

        /// <summary>The ground's renderer, or null when the scene has none.</summary>
        public Renderer Ground => _ground;

        /// <summary>The scenery's renderers.</summary>
        public IReadOnlyList<Renderer> Scenery => _scenery;

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
                case BenchmarkSwitch.NoDamageNumbers:
                    // Its private Release stops it listening for hits. Not restored, as the run holds it to the end.
                    MethodOf(typeof(DamageNumbersView), "Release").Invoke(_numbers, null);
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
            }

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
