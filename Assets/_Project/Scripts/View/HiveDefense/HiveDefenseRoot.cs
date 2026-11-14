using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DigitalStory.View
{
    /// <summary>
    /// The hive defense scene's single composition root. It builds the rules and the session, connects every view to them,
    /// feeds the session each frame and shows what the session decided. It holds no game rule.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)] // Runs before other scripts, so they have what they need when they start.
    [AddComponentMenu("DigitalStory/Hive Defense Root")]
    public sealed class HiveDefenseRoot : MonoBehaviour
    {
        private const int RequiredHiveCount = LayoutGenerator.HiveCount;

        // How long after a nest is collected the wasps that join come out of it. Long enough for the whole gain to fly out.
        private const float NestSpawnSeconds = 1.5f;

        // Over a river the swarm flies this far above the water, instead of following the core down into the bed.
        private const float AboveWater = 0.25f;
        private const string WaterObjectName = "Water (Crest)";

        // A sting's target as the swarm view reads it: w is 1 while the sting wraps a living beekeeper. The rules give each
        // sting a new target when its own dies, and end them all at once, letting the song go, when none is left; so every
        // sting the running Sting sent is alive until the slot lets go and the whole squad flies home.
        private const float LivingSting = 1f;

        // Share of the river's length behind the core where its light hangs: nearer the head than the middle, so the
        // core, at the river's head, is in the light too.
        private const float RiverLightShare = 0.35f;

        // Share of the way per second the attack light covers toward its target, slower than the light's own follow,
        // so it arrives with the stingers.
        private const float AttackLightFollowPerSecond = 4f;

        // Share of the lance's line from the core where the attack light settles once the spear fires, so the whole streak
        // is lit.
        private const float LanceLightShare = 0.5f;

        // Every slot, in the order the view numbers them.
        private static readonly SongSlot[] Slots = { SongSlot.Post, SongSlot.Guard, SongSlot.Strike };

        [Header("Rules")]
        [SerializeField, Tooltip("The wasp swarm's numbers.")]
        private SwarmConfig _swarm;

        [SerializeField, Tooltip("The song slots' squads of wasps and the return beat after a song lets go.")]
        private SquadConfig _squads;

        [SerializeField, Tooltip("How auto-aim chooses what an aimed song strikes.")]
        private AimConfig _aim;

        [SerializeField, Tooltip("How fast the core moves and what slows it.")]
        private CoreMoveConfig _coreMove;

        [SerializeField, Tooltip("The Fury meter's numbers.")]
        private FuryConfig _fury;

        [SerializeField, Tooltip("The giant's numbers.")]
        private ColossusConfig _colossus;

        [SerializeField, Tooltip("Each hive's numbers.")]
        private HiveConfig _hive;

        [SerializeField, Tooltip("The wild nests' numbers: when they open, how the core collects one, what one gives and where they may stand.")]
        private NestConfig _nests;

        [SerializeField, Tooltip("Where each match's map puts the hives, the spawn points, the nest trees and the scenery. Each match draws a new map (MatchSeed).")]
        private LayoutConfig _layout;

        [SerializeField, Tooltip("What each swarm level multiplies the songs' sizes and damage by.")]
        private SwarmLevelConfig _swarmLevel;

        [SerializeField, Tooltip("The smoke grenade and cloud numbers.")]
        private SmokeConfig _smoke;

        [SerializeField, Tooltip("The rounds: the farm phases and the waves.")]
        private WaveConfig _waves;

        [SerializeField, Tooltip("The stick beekeeper's numbers.")]
        private BeekeeperConfig _stick;

        [SerializeField, Tooltip("The smoker beekeeper's numbers.")]
        private BeekeeperConfig _smoker;

        [SerializeField, Tooltip("How long the song window is and how many notes it remembers.")]
        private SongConfig _songConfig;

        [SerializeField, Tooltip("Every song the player can play, including the Dome, Tornado and Meteor songs.")]
        private SwarmSongEffect[] _songs = new SwarmSongEffect[0];

        [SerializeField, Min(1), Tooltip("How many holes the flute has. The input has four keys: H J K L.")]
        private int _holeCount = 4;

        [SerializeField, Tooltip("The baked body samples the swarm takes when it forms the giant.")]
        private ColossusSampleSet _colossusSamples;

        [Header("Scene objects")]
        [SerializeField, Tooltip("Reads the keyboard, the mouse and a controller.")]
        private HiveDefenseInput _input;

        [SerializeField, Tooltip("Moves the swarm's core, the point the swarm gathers around.")]
        private SwarmCoreMotor _coreMotor;

        [SerializeField, Tooltip("Follows the core from above, and chases the giant from behind while it stands.")]
        private FollowCamera _followCamera;

        [SerializeField, Tooltip("An empty object the root puts where the rules say the giant stands, facing its way. The chase camera follows it rather than the puppet, so the spin kick's turn and the leap's arc never swing the view.")]
        private Transform _chaseAnchor;

        [SerializeField, Tooltip("The camera that the mouse aim is relative to.")]
        private Camera _camera;

        [SerializeField, Tooltip("Draws the wasps.")]
        private SwarmGpuView _swarmView;

        [SerializeField, Tooltip("The light the glowing swarm casts on the ground, the trees and the beekeepers around it.")]
        private SwarmGlowLight _glowLight;

        [SerializeField, Tooltip("The red light of the wasps that attack in a sting or a meteor. It travels with them, while the first light stays with the rest of the swarm.")]
        private SwarmGlowLight _attackGlowLight;

        [SerializeField, Tooltip("Draws the two hives, in the same order as the hives' positions.")]
        private HiveView[] _hives = new HiveView[0];

        [SerializeField, Tooltip("Draws the wild nests, in the order they open. Each stands where its nest is, so the first ones are those open at the start.")]
        private NestView[] _nestViews = new NestView[0];

        [SerializeField, Tooltip("Where beekeepers enter: four, which the root moves to the match's map when it starts.")]
        private Transform[] _spawnPoints = new Transform[0];

        [SerializeField, Tooltip("The props around the arena's edge, which the root places on the match's map when it starts.")]
        private SceneryView _scenery;

        [SerializeField, Tooltip("Shows the beekeepers.")]
        private BeekeeperViewPool _beekeeperPool;

        [SerializeField, Tooltip("The stick beekeeper's prefab.")]
        private BeekeeperView _stickPrefab;

        [SerializeField, Tooltip("The smoker beekeeper's prefab.")]
        private BeekeeperView _smokerPrefab;

        [SerializeField, Tooltip("Shows the smoke grenades and clouds.")]
        private SmokeViewPool _smokePool;

        [SerializeField, Tooltip("The smoke grenade's prefab.")]
        private SmokeGrenadeView _grenadePrefab;

        [SerializeField, Tooltip("The smoke cloud's prefab.")]
        private SmokeCloudView _cloudPrefab;

        [SerializeField, Tooltip("Shows the giant.")]
        private ColossusMotor _colossusMotor;

        [SerializeField, Tooltip("The HUD.")]
        private HiveDefenseHudView _hud;

        private HiveMinimapView _minimap;
        private HiveKeysView _keys;
        private HiveBadgesView _badges;
        private ScreenFadeView _fadeIn;
        private Transform _water;
        private const float AmbienceFullSwarm = 2000f;
        private const float AmbienceFullSpeed = 7f;
        private const float StreamReach = 18f;
        private const float RiverMargin = 2.5f;
        private AmbienceVoice _ambience;
        private Vector3 _lastCore;
        private float _coreSpeed;

        [SerializeField, Tooltip("The felt hit: the camera's shake and kick, the beekeeper's flash and recoil, the swarm's freeze and the shield's blue flash.")]
        private HitFeedbackView _hitFeedback;

        [Header("Shadows")]
        [SerializeField, Min(1f), Tooltip("Metres the moon's shadows reach while this scene is loaded. Set at runtime for this scene only, and the quality level's own value is put back when the scene unloads.")]
        private float _shadowDistance = 45f;

        [SerializeField, Range(1, 4), Tooltip("Shadow cascades while this scene is loaded: 1, 2 or 4. Put back when the scene unloads, like the distance.")]
        private int _shadowCascades = 2;

        [Header("Giant camera")]
        [SerializeField, Min(0f), Tooltip("Degrees the chase camera turns for each unit of the Input Manager's Mouse X and Mouse Y while the giant stands.")]
        private float _lookDegreesPerUnit = 2.5f;

        [SerializeField, Tooltip("Hides and locks the cursor while the giant stands, so the mouse turns the chase camera as far as it likes, as in a third-person game.")]
        private bool _isCursorLockedInChase = true;

        [SerializeField, Min(0f), Tooltip("The nearest, in metres ahead of the giant, that the chase view's aim may be. The aim is where the camera looks on the ground; the leap slam goes there.")]
        private float _chaseAimLeastMetres = 2f;

        [SerializeField, Min(0f), Tooltip("The furthest, in metres ahead of the giant, that the chase view's aim may be, for a camera that looks nearly level.")]
        private float _chaseAimMostMetres = 20f;

        [Header("Liveliness")]
        [SerializeField, Range(0.05f, 1f), Tooltip("The time scale of the brief slow-down when the giant forms and when a wave's last beekeeper dies. The camera's Liveliness scales how deep it goes; 0 turns it off.")]
        private float _slowMotionScale = 0.4f;

        [SerializeField, Min(0f), Tooltip("Real seconds the slow-down lasts.")]
        private float _slowMotionSeconds = 0.25f;

        [SerializeField, Range(0f, 0.5f), Tooltip("How far from the middle of the screen, as a share of its size, a hive under attack must be before the camera leans toward it.")]
        private float _leanStartShare = 0.2f;

        [Header("Load")]
        [SerializeField, Min(0f), Tooltip("Milliseconds of each frame, from the scene's start until it is done, spent compiling the game's code ahead of its first call (ScriptWarmup), so the first cast of a song is not a slow frame. 0 turns it off.")]
        private float _warmupMillisecondsPerFrame = 2f;

        [Header("Sound")]
        [SerializeField, Tooltip("Sounds each note the player plays. May stay empty, and the flute is then silent.")]
        private SwarmFluteVoice _fluteVoice;

        [SerializeField, Tooltip("How the flute sounds. Needed when the flute voice is set; it must have a pitch for every hole.")]
        private SoundConfig _sound;

        [Header("Controller")]
        [SerializeField, Tooltip("The controller's numbers: the sticks' dead zone, the right stick's aim and turn, and the rumble.")]
        private GamepadConfig _gamepad;

        [SerializeField, Tooltip("Rumbles the controller for notes, casts, hits, the giant's heavy blows and losses.")]
        private GamepadRumbleView _rumble;

        // The quality level's shadow values from before the first root of this scene set its own (see SetSceneShadows).
        private static int s_rootsHoldingShadows;
        private static float s_savedShadowDistance;
        private static int s_savedShadowCascades;

        private readonly Vector4[] _stingTargets = new Vector4[StingSong.MostStings];
        private ScriptWarmup _scriptWarmup;
        private PathTrail _trail;
        private Vector3 _lastAimPoint;
        private bool _hasSetShadows;
        private Vector3 _attackSpot;
        private bool _isAttackSpotSet;
        private int _holeMask;
        private bool _hasFormChangedThisTick;
        private bool _isChasing;
        private bool _hasLockedCursor;
        private CursorLockMode _savedLockMode;
        private bool _wasCursorVisible;
        private bool _wasWaveRunning;
        private bool _isSlowingTime;
        private float _slowSecondsLeft;
        private float _timeScaleBeforeSlow;
        private float _slowTimeScale;
        private bool _isRestarting;
        private List<Bounds> _mapBoxes = new List<Bounds>();
        private bool _isReady;
        private bool _isWaveCalled;
        private bool _isGiveHeld;
        private bool _isRightHandPressed;
        private bool _isLeftHandPressed;
        private Vector2 _heldMove;

        /// <summary>The running match, for the views, tests and debugging.</summary>
        public HiveDefenseSession Session { get; private set; }

        /// <summary>The match's map (plan section S), drawn when the match started, for tests and debugging.</summary>
        public ArenaLayout Layout { get; private set; }

        /// <summary>
        /// The one entry point for a played note. Collects the hole for the next tick and sounds it. The hole counts from 0.
        /// While the giant stands the holes are its moves (plan section I), so they reach the session but the flute is silent.
        /// </summary>
        public void PressHole(int hole)
        {
            if (!_isReady || hole < 0 || hole >= _holeCount)
            {
                return;
            }

            _holeMask |= HiveDefenseFrame.HoleBit(hole);

            if (_fluteVoice != null && !Session.IsColossusActive)
            {
                _fluteVoice.PlayNote(hole);
            }
        }

        /// <summary>
        /// The one entry point for calling the wave early, as Enter does. The call is read on the next tick; outside a farm
        /// phase the session ignores it.
        /// </summary>
        public void CallWave()
        {
            if (_isReady)
            {
                _isWaveCalled = true;
            }
        }

        /// <summary>
        /// Presses one of the giant's hand launches, as its trigger, mouse button or key does (songs-v2 R). Read on the next
        /// tick; it does nothing while no giant stands. For tests and the benchmark; the buttons work either way.
        /// </summary>
        public void PressHand(ColossusHand hand)
        {
            if (!_isReady)
            {
                return;
            }

            if (hand == ColossusHand.Right)
            {
                _isRightHandPressed = true;
            }
            else
            {
                _isLeftHandPressed = true;
            }
        }

        /// <summary>
        /// Holds Give, as holding E does, until it is released with false. Beside a hurt hive the swarm then pays wasps into
        /// its health. E works either way.
        /// </summary>
        public void HoldGive(bool isHeld)
        {
            _isGiveHeld = isHeld;
        }

        /// <summary>
        /// Holds a move, as holding W A S D does, until it is set back to zero: y forward and x to the right, length 0 to 1.
        /// It goes the keys' way: relative to the camera, so (0, 1) walks the core or the giant away from the camera (north
        /// in the top view). For tests and the benchmark; the keys work either way.
        /// </summary>
        public void HoldMove(Vector2 move)
        {
            _heldMove = Vector2.ClampMagnitude(move, 1f);
        }

        /// <summary>True while the root has the camera chasing the giant, from the tick it forms until the tick it ends.</summary>
        public bool IsChasingGiant => _isChasing;

        /// <summary>True while the brief slow-down of time runs, for tests and debugging.</summary>
        public bool IsSlowingTime => _isSlowingTime;

        /// <summary>The compiling of the game's code ahead of its first call, for tests and the benchmark.</summary>
        public ScriptWarmup ScriptWarmup => _scriptWarmup;

        private void Awake()
        {
            CheckReferences();
            _ambience = new GameObject("Ambience", typeof(AudioSource), typeof(AmbienceVoice)).GetComponent<AmbienceVoice>();
            _ambience.transform.SetParent(transform, false);

            Layout = DrawMap();

            var rules = new HiveDefenseRules(
                _swarm, _squads, _coreMove, _fury, _colossus, _hive, _nests, _swarmLevel, _smoke, _waves, _stick, _smoker, _aim,
                _songConfig, _songs, _holeCount);
            // The session's chance (the nests' combos, the hummed notes, where beekeepers step in) comes from the match's map
            // seed, as in the playtest bot, so each match is new and a seed replays all of it, not only the map.
            var random = new Mulberry32Random(unchecked((uint)Layout.Seed));
            Session = new HiveDefenseSession(rules, Layout.Hives, Layout.Nests, Layout.Spawns, _coreMotor.Position, random);

            _trail = new PathTrail(_swarm.TrailPoints, _swarm.TrailSpacing);
            _trail.Reset(_coreMotor.Position);

            _input.Initialize(_camera, _gamepad);
            _coreMotor.Initialize(_coreMove);
            _swarmView.Initialize(_swarm, _trail);
            _glowLight.Initialize(_swarm);
            _attackGlowLight.Initialize(_swarm);
            Session.Forms.FormChanged += HandleFormChanged;
            Session.Caster.SongCast += HandleSongCast;
            Session.Nests.NestCollected += HandleNestCollected;
            Session.Nests.ComboNote += HandleComboNote;
            for (int index = 0; index < _hives.Length; index++)
            {
                _hives[index].Initialize(Session.Hives[index], _camera, _hive);
            }

            SendObstacles();

            for (int index = 0; index < _nestViews.Length; index++)
            {
                _nestViews[index].Initialize(Session.Nests[index], _nests);
            }

            _beekeeperPool.Initialize(Session, _stickPrefab, _smokerPrefab);
            _smokePool.Initialize(Session.Smoke, _grenadePrefab, _cloudPrefab);
            _colossusMotor.Initialize(Session, _swarmView, _colossusSamples, _colossus);
            _hitFeedback.Initialize(Session, _followCamera, _swarmView);

            // Plan section AB: the optional impact effects, made from a paid Asset Store pack that the public repo may not
            // publish. Their library lives only in an untracked local folder, so no committed file may refer to it, not even the
            // scene; loading it by name from a Resources folder is the one way to keep every committed file the same with or
            // without the pack. Without it (a teammate without the pack) this is null and the game's own effects run alone.
            _hitFeedback.UseImpactFx(Resources.Load<ImpactFxLibrary>(ImpactFxLibrary.ResourceName));
            _rumble.Initialize(Session, _input, _gamepad);
            SetSceneShadows();

            if (_fluteVoice != null)
            {
                // A held key keeps its note sounding past the shortest note length.
                _fluteVoice.Initialize(_sound, _coreMove);
                _fluteVoice.Connect(_input);
            }

            _followCamera.SetTarget(_coreMotor.transform);
            GameObject water = GameObject.Find(WaterObjectName);
            _water = water != null ? water.transform : null;
            _lastAimPoint = _coreMotor.Position;
            _input.HolePressed += PressHole;

            // The Data, Logic and View assemblies: every rule a cast runs and every view that answers it.
            _scriptWarmup = new ScriptWarmup(typeof(SwarmConfig).Assembly, typeof(HiveDefenseSession).Assembly, typeof(HiveDefenseRoot).Assembly);
            _isReady = true;
        }

        private void Start()
        {
            if (!_isReady)
            {
                return; // Awake already logged the real cause.
            }

            // The HUD reads the UIDocument's tree, which exists only once every object has been enabled.
            _hud.Initialize(Session);
            _hud.ListenToKillChain(_hitFeedback);
            _hud.ShowControls(_input.LastDevice);
            _minimap = new HiveMinimapView(
                Session, _hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement, _coreMove.PlayAreaHalfSize, () => _followCamera.GroundForward);
            UnityEngine.UIElements.VisualElement hudRoot = _hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
            UnityEngine.UIElements.VisualElement bottom = UnityEngine.UIElements.UQueryExtensions.Q(hudRoot, "bottom");
            _keys = new HiveKeysView(_input, bottom ?? hudRoot);
            _badges = new HiveBadgesView(Session, hudRoot, _camera);
            _fadeIn = new ScreenFadeView(hudRoot);
            _input.LastDeviceChanged += HandleLastDeviceChanged;
        }

        // The wasps a nest gives pour out of that nest in its tree, not out of a hive.
        private void HandleNestCollected(int nest)
        {
            if (nest >= 0 && nest < _nestViews.Length)
            {
                _swarmView.SetSpawnSource(_nestViews[nest].NestPoint, NestSpawnSeconds);
            }

            _ambience.PlayCollect();
        }

        private void Restart()
        {
            HiveDefenseInput.CarryOverRestart(_input.LastDevice);
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }

#if UNITY_EDITOR
            // The scene is not in the build settings, so load it by path, as Play Mode allows.
            if (!string.IsNullOrEmpty(scene.path))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            Debug.LogError($"Cannot restart: scene '{scene.name}' is not in the build settings.", this);
            _isRestarting = false;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.HolePressed -= PressHole;
                _input.LastDeviceChanged -= HandleLastDeviceChanged;
            }

            _minimap?.Dispose();
            _minimap = null;
            _keys?.Dispose();
            _keys = null;
            _badges?.Dispose();
            _badges = null;
            _fadeIn?.Dispose();
            _fadeIn = null;

            if (Session != null)
            {
                Session.Forms.FormChanged -= HandleFormChanged;
                Session.Caster.SongCast -= HandleSongCast;
                Session.Nests.NestCollected -= HandleNestCollected;
                Session.Nests.ComboNote -= HandleComboNote;
            }

            // A scene unloaded during the slow-down or the chase leaves time and the cursor as it found them.
            EndSlowMotion();
            UnlockCursor();
            RestoreShadows();
        }

        // The moon's shadows reach only as far as this scene's camera sees, in fewer cascades, which cuts their cost here.
        // The quality level's own values are kept and put back when the scene unloads, so no other scene and no shared
        // setting changes. A restart loads the scene again, and the old root may go after the new one has woken, so the
        // values are kept once for every root alive (a static count) rather than by each root.
        private void SetSceneShadows()
        {
            if (s_rootsHoldingShadows == 0)
            {
                s_savedShadowDistance = QualitySettings.shadowDistance;
                s_savedShadowCascades = QualitySettings.shadowCascades;
            }

            s_rootsHoldingShadows++;
            _hasSetShadows = true;
            QualitySettings.shadowDistance = _shadowDistance;
            QualitySettings.shadowCascades = _shadowCascades;
        }

        private void RestoreShadows()
        {
            if (!_hasSetShadows)
            {
                return;
            }

            _hasSetShadows = false;
            s_rootsHoldingShadows--;
            if (s_rootsHoldingShadows == 0)
            {
                QualitySettings.shadowDistance = s_savedShadowDistance;
                QualitySettings.shadowCascades = s_savedShadowCascades;
            }
        }

        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            _minimap?.Tick();
            _keys?.Tick(Time.unscaledDeltaTime);
            _badges?.Tick(Time.unscaledDeltaTime);
            _fadeIn?.Tick(Time.unscaledDeltaTime);

            StepSlowMotion();
            if (_warmupMillisecondsPerFrame > 0f)
            {
                _scriptWarmup.Step(_warmupMillisecondsPerFrame);
            }

            float dt = Mathf.Min(Time.deltaTime, _swarm.MaxDeltaTime);

            if (Session.State != HiveDefenseState.Playing)
            {
                // The match is over, so nothing flows between the core and a hive any more, and the camera comes back up.
                StopHiveFlows();
                FeedChase();
                FeedWave();
                if (_input.RestartPressed && !_isRestarting)
                {
                    _isRestarting = true;
                    Restart();
                }

                return;
            }

            // The keys, or a held move, are relative to the camera: y is the camera's way on the ground and x its right. They
            // are turned onto the ground once (GroundStep): here for the core, and in the session for the giant, which gets
            // them unturned. While the giant stands it carries the core, so the motor only settles the core on the ground.
            bool isColossus = Session.IsColossusActive;
            Vector2 localMove = _heldMove.sqrMagnitude > 0f ? _heldMove : _input.LocalMove;
            Vector3 cameraForward = _followCamera.GroundForward;
            Vector3 move = HiveDefenseFrame.GroundStep(localMove, cameraForward);

            // Shift sprints the swarm. The giant has its own speed, and there Shift kicks.
            float sprint = _input.SprintHeld && !isColossus ? _coreMove.SprintSpeedMultiplier : 1f;
            _coreMotor.Move(
                isColossus ? Vector3.zero : move, Session.CoreSpeedMultiplier * sprint, isColossus || Session.IsCoreRooted, dt);

            if (_input.TryGetAimPoint(_coreMotor.Position, cameraForward, out Vector3 aim))
            {
                _lastAimPoint = aim;
            }

            if (_input.CallWavePressed)
            {
                CallWave();
            }

            // The mouse aims only while it is live, and the right stick while it leans; otherwise auto-aim chooses (plan section
            // I). In the chase view the mouse and the stick turn the camera, so there their aim is where the camera looks on the
            // ground, ahead of the giant, live for a moment after the mouse turns the view, while a button is held or while the
            // stick leans, as anywhere else.
            Vector3 aimPoint = _isChasing ? ChaseAimPoint() : _lastAimPoint;

            var frame = new HiveDefenseFrame(
                _coreMotor.Position,
                aimPoint,
                localMove,
                _holeMask,
                _input.RightHandPressed || _isRightHandPressed,
                _input.LeftHandPressed || _isLeftHandPressed,
                _input.IsMouseLive,
                _input.GiveHeld || _isGiveHeld,
                _isWaveCalled,
                cameraForward);
            _holeMask = 0;
            _isRightHandPressed = false;
            _isLeftHandPressed = false;
            _isWaveCalled = false;
            _hasFormChangedThisTick = false;
            Session.Tick(in frame, dt);

            // The session says where the core is: inside the giant while it stands, where the giant stood when it ends, and
            // never past the play area's edge. Once the giant is gone the core is put down clear of hives and walls, so the
            // controller has nothing to jump out of on the next move.
            _coreMotor.PlaceAt(Session.CorePosition, !Session.IsColossusActive);
            Vector3 core = _coreMotor.Position;

            // The swarm and its lights stay above any water the core walks into; the hives still see the core itself.
            Vector3 flying = OverWater(core);
            _trail.Push(flying);

            FeedSwarm(flying);
            FeedHives(core);
            FeedGlow(flying, dt);
            FeedAmbience(core, dt);
            FeedChase();
            FeedWave();
            FeedLean();
        }

        // Lifts a point to just above the water's surface when it is below it, as in a riverbed. Unchanged without water.
        // The field's noise and the swarm's buzz: louder with more wasps, higher as the swarm speeds along.
        private void FeedAmbience(Vector3 core, float dt)
        {
            if (dt > 0f)
            {
                float speed = new Vector2(core.x - _lastCore.x, core.z - _lastCore.z).magnitude / dt;
                _coreSpeed = Mathf.Lerp(_coreSpeed, Mathf.Min(speed, AmbienceFullSpeed * 2f), 1f - Mathf.Exp(-6f * dt));
            }

            _lastCore = core;
            float water = RiverBridge.Nearness(core, StreamReach);
            float charge = 0f;
            for (int index = 0; index < Session.Nests.Count; index++)
            {
                Nest nest = Session.Nests[index];
                if (nest.IsOpen && !nest.IsCollected)
                {
                    charge = Mathf.Max(charge, nest.Progress01);
                }
            }

            _ambience.SetCharge(charge);
            _ambience.SetFluteSounding(_fluteVoice != null && _fluteVoice.HolePlaying != FluteSynth.Silent);
            _ambience.Feed(Session.Swarm.Count / AmbienceFullSwarm, _coreSpeed / AmbienceFullSpeed, water * water);
        }

        private Vector3 OverWater(Vector3 point)
        {
            if (_water == null)
            {
                return point;
            }

            point.y = Mathf.Max(point.y, _water.position.y + AboveWater);
            return point;
        }

        // Only the hive the core stands at shows the core near, and its store flowing in or the swarm's wasps flowing out.
        private void FeedHives(Vector3 core)
        {
            int near = Session.NearHive;
            bool isCollecting = Session.IsCollecting;
            bool isGiving = Session.IsGiving;
            for (int index = 0; index < _hives.Length; index++)
            {
                bool isNear = index == near;
                HiveView view = _hives[index];
                view.SetCorePosition(core);
                view.ShowCoreNear(isNear);
                view.ShowCollecting(isNear && isCollecting);
                view.ShowGiving(isNear && isGiving);
            }
        }

        private void StopHiveFlows()
        {
            for (int index = 0; index < _hives.Length; index++)
            {
                _hives[index].ShowCoreNear(false);
                _hives[index].ShowCollecting(false);
                _hives[index].ShowGiving(false);
            }
        }

        // The HUD names the keys, or the controller's buttons, of the device the player picked up last.
        private void HandleLastDeviceChanged(ControlDevice device)
        {
            _hud.ShowControls(device);
        }

        private void HandleFormChanged(SwarmForm form)
        {
            _hasFormChangedThisTick = true;
        }

        // Plan section U: each right note of a nest's combo sounds higher than the last, so the combo climbs as it fills. The
        // note was sounded as it was pressed, earlier this frame, so the voice glides it up before it is heard for long.
        private void HandleComboNote(int nest, NestComboStep step)
        {
            if (_fluteVoice != null && step != NestComboStep.Broken)
            {
                _fluteVoice.LiftNote((Session.Nests[nest].NotesPlayed - 1) * _nests.LiftSemitonesPerNote);
            }
        }

        // A whole-swarm form (Hive Call, the Colossus) takes the colour of the category of the song whose cast started it.
        // A slot song's squad shows its own colour through SetSlot, and a Gust changes no form, so neither recolours it.
        private void HandleSongCast(int song)
        {
            if (_hasFormChangedThisTick)
            {
                _swarmView.SetCastColor(_swarm.ColorOf(Session.Caster[song].Category));
            }
        }

        // One light stands in for the glow of the wasps that stay with the core, and a second one for the Strike squad,
        // the wasps that attack. The first hangs over the middle of the river or the eight in the river's colour, over the
        // core in the Guard song's colour while one shields it, or over the giant; it is as strong as the swarm is full
        // and the share that stays. The second travels with the striking squad in its colour, and fades as the squad flies
        // home. Both sit a little toward the camera, so the sides it sees are lit.
        private void FeedGlow(Vector3 core, float deltaTime)
        {
            SwarmFormMachine forms = Session.Forms;
            SwarmForm form = forms.Current;
            SlotState guard = forms.Slot(SongSlot.Guard);
            SlotState strike = forms.Slot(SongSlot.Strike);
            Vector3 middle = RiverMiddle(core);
            Color color = _swarm.RiverColor;

            if (form == SwarmForm.Colossus)
            {
                middle = Session.ColossusPosition;
                color = _swarmView.CastColor;
            }
            else if (form == SwarmForm.HiveCall)
            {
                middle = core;
                color = _swarmView.CastColor;
            }
            else if (guard.IsActive && guard.ReleasedSeconds < 0f)
            {
                middle = core;
                color = _swarm.ColorOf(guard.Category);
            }

            float attackShare = 0f;
            Vector3 attackAt = core;
            if (strike.IsActive)
            {
                attackShare = (strike.High - strike.Low) * StrikeLightLeft(strike);
                attackAt = strike.Form == SwarmForm.Sting && forms.StingCount > 0
                    ? StingsMiddle(forms)
                    : StrikeLightPoint(core, strike);
            }

            Vector3 towardCamera = -_camera.transform.forward;
            towardCamera.y = 0f;
            if (towardCamera.sqrMagnitude > 0f)
            {
                towardCamera.Normalize();
            }

            // Full is the capacity, which grows with each nest, so a full swarm of 800 at the start glows as bright as a
            // full one later, and a hurt swarm dims.
            SwarmBody swarm = Session.Swarm;
            // The light grows with the swarm itself, not with how full it is: a young swarm of 800 glows gently,
            // and a full-grown one lights the ground around it.
            float grown = swarm.Max > 0 ? (float)swarm.Count / swarm.Max : 0f;
            float strength = swarm.Count > 0 ? Mathf.Lerp(_swarm.LightLeastStrength, 1f, grown) : 0f;
            Vector3 lift = (Vector3.up * _swarm.LightHeight) + (towardCamera * _swarm.LightTowardCamera);
            _glowLight.Show(middle + lift, color, strength * (1f - attackShare));

            // The attacking light eases to a sting's target slower than the light's own follow, so it arrives with the
            // stingers. The meteor's path is known, so its light rides the ball and lands with the impact.
            if (!_isAttackSpotSet || attackShare <= 0f || strike.Form == SwarmForm.Meteor)
            {
                _attackSpot = attackAt;
                _isAttackSpotSet = true;
            }
            else
            {
                _attackSpot = Vector3.Lerp(_attackSpot, attackAt, 1f - Mathf.Exp(-AttackLightFollowPerSecond * deltaTime));
            }

            Color attackColor = strike.IsActive ? _swarm.ColorOf(strike.Category) : _swarm.RiverColor;
            _attackGlowLight.Show(_attackSpot + lift, attackColor, strength * attackShare);
        }

        // 1 while the Strike song runs, easing to 0 over its return beat as its squad flies home.
        private float StrikeLightLeft(in SlotState strike)
        {
            if (strike.ReleasedSeconds < 0f)
            {
                return 1f;
            }

            float beat = _squads.ReturnSeconds;
            return beat > 0f ? 1f - Mathf.Clamp01(strike.ReleasedSeconds / beat) : 0f;
        }

        // The middle of the stings' targets: one light stands for every rope of a Sting that sent several.
        private static Vector3 StingsMiddle(SwarmFormMachine forms)
        {
            int count = forms.StingCount;
            Vector3 sum = Vector3.zero;
            for (int sting = 0; sting < count; sting++)
            {
                sum += forms.StingTarget(sting);
            }

            return sum / count;
        }

        // Where the striking squad is: on the sting's target, or on the meteor's ball (over the core while it climbs and
        // hangs, then along the line down to the impact point, as MeteorSteer in WaspSwarm.compute flies it), or halfway down
        // the lance's line once it fires (LanceRule's ParamA and ParamB), or around the core for a song that strikes there,
        // such as the Tornado.
        private static Vector3 StrikeLightPoint(Vector3 core, in SlotState strike)
        {
            Vector4 a = strike.ParamA;
            Vector3 target = new Vector3(a.x, a.y, a.z);
            if (strike.Form == SwarmForm.Sting)
            {
                return target;
            }

            if (strike.Form == SwarmForm.Lance)
            {
                Vector4 line = strike.ParamB;
                return strike.Seconds < line.y ? core : new Vector3(a.x, core.y, a.y) + (new Vector3(a.z, 0f, a.w) * (line.x * LanceLightShare));
            }

            if (strike.Form != SwarmForm.Meteor)
            {
                return core;
            }

            Vector4 b = strike.ParamB;
            Vector3 top = core + (Vector3.up * a.w);
            float diveSeconds = strike.Seconds - b.x;
            if (diveSeconds <= 0f)
            {
                return top;
            }

            float dive = b.y > 0f ? Mathf.Clamp01(diveSeconds / b.y) : 1f;
            return Vector3.Lerp(top, target, dive);
        }

        // About a third of the way along the river behind the core, or the core, the eight's crossing, while the swarm is
        // wound into the figure-eight round it. It follows the swarm's own blend (SwarmGpuView.EightShare).
        private Vector3 RiverMiddle(Vector3 core)
        {
            float length = _trail.Length;
            if (!(length > 0f))
            {
                return core;
            }

            float back = Mathf.Min(_swarm.RiverLengthMeters, length) * RiverLightShare;
            Vector3 river = _trail.PointAt(back / length);
            river.y = core.y;
            return Vector3.Lerp(core, river, 1f - _swarmView.EightShare);
        }

        private void FeedSwarm(Vector3 core)
        {
            SwarmFormMachine forms = Session.Forms;
            _swarmView.SetActiveCount(Session.Swarm.Count);
            _swarmView.SetForm(forms.Current);
            _swarmView.SetCore(core, Session.CoreFacing);

            for (int index = 0; index < Slots.Length; index++)
            {
                SlotState state = forms.Slot(Slots[index]);
                _swarmView.SetSlot((int)Slots[index], in state, _swarm.ColorOf(state.Category));
            }

            _swarmView.SetGust(forms.GustCenter, forms.GustRadius, forms.GustSeconds);
            FeedStingTargets(forms);
            _swarmView.SetSizeScale(Session.SizeScale);

            HiveSet hives = Session.Hives;
            _swarmView.SetHives(hives[0].Position, hives[1].Position, !hives[0].IsDestroyed, !hives[1].IsDestroyed);
            _swarmView.SetLastHit(Session.LastHitPoint);
        }

        // One target for each sting the running Sting sent, followed live, so the squad is shared between them and each rope
        // wraps its own beekeeper; none while no Sting holds the Strike slot.
        private void FeedStingTargets(SwarmFormMachine forms)
        {
            int count = forms.StingCount;
            for (int sting = 0; sting < count; sting++)
            {
                Vector3 target = forms.StingTarget(sting);
                _stingTargets[sting] = new Vector4(target.x, target.y, target.z, LivingSting);
            }

            _swarmView.SetStingTargets(_stingTargets, count);
        }

        // Plan section I: while the giant stands the camera chases it from behind and above, and the mouse turns the view; it
        // swoops back to the top view over the core when the giant ends. The anchor carries the rules' place and facing, so
        // the camera never follows the puppet's spin or leap. The giant's forming slows time for a moment.
        private void FeedChase()
        {
            bool isColossus = Session.IsColossusActive && Session.State == HiveDefenseState.Playing;
            if (isColossus)
            {
                Vector3 facing = Session.ColossusFacing;
                facing.y = 0f;
                Quaternion rotation = facing.sqrMagnitude > 0f ? Quaternion.LookRotation(facing, Vector3.up) : _chaseAnchor.rotation;
                _chaseAnchor.SetPositionAndRotation(Session.ColossusPosition, rotation);
            }

            if (isColossus != _isChasing)
            {
                _isChasing = isColossus;
                if (isColossus)
                {
                    _followCamera.EnterChase(_chaseAnchor, _colossus.Height);
                    LockCursor();
                    StartSlowMotion();
                }
                else
                {
                    _followCamera.ExitChase();
                    UnlockCursor();
                }
            }

            if (!_isChasing)
            {
                return;
            }

            // Mouse right turns the view right; mouse up looks up, which lowers the camera. The right stick turns it the same
            // way, by degrees per second. Only a moved mouse or a leaning stick orbits, so the camera drifts back behind the
            // giant once both rest.
            Vector2 look = (_input.LookDelta * _lookDegreesPerUnit) + _input.StickLookDegrees;
            if (look.sqrMagnitude > 0f)
            {
                _followCamera.Orbit(look.x, -look.y);
            }
        }

        // Where the chase camera looks on the ground, ahead of the giant and no nearer or further than the root allows. A
        // camera that looks level or up aims the furthest ahead.
        private Vector3 ChaseAimPoint()
        {
            Vector3 giant = Session.ColossusPosition;
            Vector3 ahead = _followCamera.GroundForward;
            Transform view = _camera.transform;
            Vector3 forward = view.forward;
            float metres = _chaseAimMostMetres;
            if (forward.y < 0f)
            {
                Vector3 onGround = view.position + (forward * ((giant.y - view.position.y) / forward.y));
                Vector3 offset = onGround - giant;
                offset.y = 0f;
                metres = Vector3.Dot(offset, ahead);
            }

            metres = Mathf.Clamp(metres, _chaseAimLeastMetres, Mathf.Max(_chaseAimLeastMetres, _chaseAimMostMetres));
            return giant + (ahead * metres);
        }

        // The camera backs away a little while a wave runs. The wave is over the tick its last beekeeper dies, and time
        // slows for a moment then, also when that wave was the last.
        private void FeedWave()
        {
            bool isWaveRunning = Session.State == HiveDefenseState.Playing && Session.Phase == MatchPhase.Survive;
            if (isWaveRunning == _wasWaveRunning)
            {
                return;
            }

            bool isCleared = _wasWaveRunning && Session.State != HiveDefenseState.Lost;
            _wasWaveRunning = isWaveRunning;
            _followCamera.SetWavePullBack(isWaveRunning);
            if (isCleared)
            {
                StartSlowMotion();
            }
        }

        // In the top view the camera leans a little toward a hive under attack that stands away from the middle of the
        // screen, the further away the more, and comes back once it is not called.
        private void FeedLean()
        {
            if (_isChasing)
            {
                return;
            }

            HiveSet hives = Session.Hives;
            float strongest = 0f;
            Vector3 toward = Vector3.zero;
            for (int index = 0; index < hives.Count; index++)
            {
                Hive hive = hives[index];
                if (hive.IsDestroyed || !hive.IsUnderAttack)
                {
                    continue;
                }

                Vector3 viewport = _camera.WorldToViewportPoint(hive.Position);
                float off = viewport.z <= 0f
                    ? 1f
                    : Mathf.Max(Mathf.Abs(viewport.x - 0.5f), Mathf.Abs(viewport.y - 0.5f));
                float strength = Mathf.Clamp01((off - _leanStartShare) / Mathf.Max(0.01f, 0.5f - _leanStartShare));
                if (strength > strongest)
                {
                    strongest = strength;
                    toward = hive.Position;
                }
            }

            if (strongest > 0f)
            {
                _followCamera.LeanToward(toward, strongest);
            }
        }

        // Time runs at the slow-down's scale for its real seconds and then goes back to what it was. When something else
        // changed the time scale meanwhile (a test, a pause), that change is kept. The camera's Liveliness scales how deep
        // it goes, and 0 turns it off.
        private void StartSlowMotion()
        {
            float depth = Mathf.Clamp01(_followCamera.Liveliness);
            if (!(_slowMotionSeconds > 0f) || !(depth > 0f))
            {
                return;
            }

            if (!_isSlowingTime)
            {
                _timeScaleBeforeSlow = Time.timeScale;
                _isSlowingTime = true;
            }

            _slowSecondsLeft = _slowMotionSeconds;
            _slowTimeScale = _timeScaleBeforeSlow * Mathf.Lerp(1f, _slowMotionScale, depth);
            Time.timeScale = _slowTimeScale;
        }

        private void StepSlowMotion()
        {
            if (!_isSlowingTime)
            {
                return;
            }

            _slowSecondsLeft -= Time.unscaledDeltaTime;
            if (_slowSecondsLeft <= 0f)
            {
                EndSlowMotion();
            }
        }

        private void EndSlowMotion()
        {
            if (!_isSlowingTime)
            {
                return;
            }

            _isSlowingTime = false;
            if (Mathf.Approximately(Time.timeScale, _slowTimeScale))
            {
                Time.timeScale = _timeScaleBeforeSlow;
            }
        }

        private void LockCursor()
        {
            if (!_isCursorLockedInChase || _hasLockedCursor)
            {
                return;
            }

            _hasLockedCursor = true;
            _savedLockMode = Cursor.lockState;
            _wasCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UnlockCursor()
        {
            if (!_hasLockedCursor)
            {
                return;
            }

            _hasLockedCursor = false;
            Cursor.lockState = _savedLockMode;
            Cursor.visible = _wasCursorVisible;
        }

        // Plan section S: a new map each match. The scenery, the spawn points and the nest trees move here, once, and the
        // hives and nests stand where their views put them in Initialize, from the session. The chase camera's blocker boxes
        // are measured again from where the props now stand, the nest trees before their rings exist.
        private ArenaLayout DrawMap()
        {
            int seed = MatchSeed.Next();
            // Nothing stands in or beside a river: the map keeps a few metres off the water.
            LayoutGenerator.IsBlocked = (point, radius) => RiverBridge.Nearness(point, radius + RiverMargin) > 0f;
            ArenaLayout layout = LayoutGenerator.Generate(seed, _coreMotor.Position, _layout, _nests, _scenery.Footprints);
            string fallback = layout.IsFallback ? " No map kept the rules, so the fallback map stands." : string.Empty;
            Debug.Log($"[F010] Map seed {seed}: launch with {MatchSeed.CommandLineOption} {seed} to play this map again.{fallback}");

            // The rules every map keeps are checked here too, so a broken config stops the match before it starts.
            NestLayout.Check(layout.Nests, layout.Hives, _nests, _coreMove.PlayAreaHalfSize);

            _scenery.Place(layout.Scenery);
            for (int index = 0; index < _spawnPoints.Length; index++)
            {
                _spawnPoints[index].position = layout.Spawns[index];
            }

            // The bridges follow the map: one wherever a spawn point's way to its own hive crosses a river.
            RuntimeBridges.Build(layout.Spawns, layout.Hives);

            var blockers = new List<Bounds>();
            _scenery.AddBlockers(blockers);
            for (int index = 0; index < _nestViews.Length; index++)
            {
                _nestViews[index].transform.position = layout.Nests[index];
                _scenery.AddBlocker(blockers, _nestViews[index].transform);
            }

            _followCamera.SetChaseBlockers(blockers.ToArray());
            _mapBoxes = blockers;
            return layout;
        }

        // Plan section X: the free swarm flows round each tall prop and nest tree, from the boxes DrawMap measured for the
        // chase camera before the nests' rings existed, and round each hive's model, once its view stands where the session
        // put it. Sent once a match; a restart loads the scene again and sends the new map's.
        private void SendObstacles()
        {
            var boxes = new List<Bounds>(_mapBoxes);
            for (int index = 0; index < _hives.Length; index++)
            {
                if (_hives[index].TryMeasureModel(out Bounds box))
                {
                    boxes.Add(box);
                }
            }

            var obstacles = new Vector4[SwarmObstacles.MostObstacles];
            int count = SwarmObstacles.Fill(boxes, _swarm.ObstacleRadiusShare, obstacles);
            if (count == SwarmObstacles.MostObstacles && boxes.Count > count)
            {
                Debug.LogWarning($"[F010] The swarm flows round {count} of {boxes.Count} obstacles; raise SwarmObstacles.MostObstacles to take them all.", this);
            }

            _swarmView.SetObstacles(obstacles, count);
        }

        private void CheckReferences()
        {
            Require(_swarm, nameof(_swarm));
            Require(_squads, nameof(_squads));
            Require(_aim, nameof(_aim));
            Require(_coreMove, nameof(_coreMove));
            Require(_fury, nameof(_fury));
            Require(_colossus, nameof(_colossus));
            Require(_hive, nameof(_hive));
            Require(_nests, nameof(_nests));
            Require(_layout, nameof(_layout));
            Require(_swarmLevel, nameof(_swarmLevel));
            Require(_smoke, nameof(_smoke));
            Require(_waves, nameof(_waves));
            Require(_stick, nameof(_stick));
            Require(_smoker, nameof(_smoker));
            Require(_songConfig, nameof(_songConfig));
            Require(_colossusSamples, nameof(_colossusSamples));
            Require(_input, nameof(_input));
            Require(_coreMotor, nameof(_coreMotor));
            Require(_followCamera, nameof(_followCamera));
            Require(_chaseAnchor, nameof(_chaseAnchor));
            Require(_camera, nameof(_camera));
            Require(_swarmView, nameof(_swarmView));
            Require(_glowLight, nameof(_glowLight));
            Require(_attackGlowLight, nameof(_attackGlowLight));
            Require(_beekeeperPool, nameof(_beekeeperPool));
            Require(_stickPrefab, nameof(_stickPrefab));
            Require(_smokerPrefab, nameof(_smokerPrefab));
            Require(_smokePool, nameof(_smokePool));
            Require(_grenadePrefab, nameof(_grenadePrefab));
            Require(_cloudPrefab, nameof(_cloudPrefab));
            Require(_colossusMotor, nameof(_colossusMotor));
            Require(_hud, nameof(_hud));
            Require(_hitFeedback, nameof(_hitFeedback));
            Require(_gamepad, nameof(_gamepad));
            Require(_rumble, nameof(_rumble));
            Require(_scenery, nameof(_scenery));

            if (_scenery.Footprints.Count != _scenery.PropCount)
            {
                throw new InvalidOperationException($"{name}: the scenery has {_scenery.PropCount} props but {_scenery.Footprints.Count} footprints. Rebuild the scene.");
            }

            if (_songs == null || _songs.Length == 0)
            {
                throw new InvalidOperationException($"{name}: the songs list is empty. Add the swarm songs to HiveDefenseRoot.");
            }

            for (int index = 0; index < _songs.Length; index++)
            {
                Require(_songs[index], $"{nameof(_songs)}[{index}]");
            }

            if (_hives == null || _hives.Length != RequiredHiveCount)
            {
                throw new InvalidOperationException($"{name}: HiveDefenseRoot needs exactly {RequiredHiveCount} hive views.");
            }

            for (int index = 0; index < _hives.Length; index++)
            {
                Require(_hives[index], $"{nameof(_hives)}[{index}]");
            }

            if (_nestViews == null || _nestViews.Length != _nests.NestCount)
            {
                throw new InvalidOperationException(
                    $"{name}: HiveDefenseRoot needs {_nests.NestCount} nest views, as many as '{_nests.name}' opens, in the order they open.");
            }

            for (int index = 0; index < _nestViews.Length; index++)
            {
                Require(_nestViews[index], $"{nameof(_nestViews)}[{index}]");
            }

            if (_spawnPoints == null || _spawnPoints.Length != LayoutGenerator.SpawnCount)
            {
                throw new InvalidOperationException(
                    $"{name}: HiveDefenseRoot needs {LayoutGenerator.SpawnCount} spawn points, as every map has: two for each hive.");
            }

            for (int index = 0; index < _spawnPoints.Length; index++)
            {
                Require(_spawnPoints[index], $"{nameof(_spawnPoints)}[{index}]");
            }

            if (_fluteVoice != null)
            {
                Require(_sound, nameof(_sound));

                // The synth looks a hole's pitch up on the audio thread, where a missing pitch cannot be reported.
                if (_holeCount > _sound.HoleCount)
                {
                    throw new InvalidOperationException(
                        $"{name}: the flute has {_holeCount} holes, but '{_sound.name}' only has pitches for {_sound.HoleCount}.");
                }
            }
        }

        private void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null)
            {
                throw new InvalidOperationException($"{name}: HiveDefenseRoot is missing '{field}'. Fill it in or rebuild the scene.");
            }
        }
    }
}
