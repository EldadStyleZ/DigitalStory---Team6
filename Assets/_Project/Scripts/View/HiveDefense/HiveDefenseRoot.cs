using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DigitalStory.View
{
    /// <summary>
    /// The Wasp Piper scene's single composition root. It builds the rules and the session, connects every view to them,
    /// feeds the session each frame and shows what the session decided. It holds no game rule.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)] // Runs before other scripts, so they have what they need when they start.
    [AddComponentMenu("DigitalStory/Hive Defense Root")]
    public sealed class HiveDefenseRoot : MonoBehaviour
    {
        private const int RequiredHiveCount = 2;

        [Header("Rules")]
        [SerializeField, Tooltip("The wasp swarm's numbers.")]
        private SwarmConfig _swarm;

        [SerializeField, Tooltip("How fast the Piper moves and what slows it.")]
        private PiperMoveConfig _piperMove;

        [SerializeField, Tooltip("The Fury meter's numbers.")]
        private FuryConfig _fury;

        [SerializeField, Tooltip("The giant's numbers.")]
        private ColossusConfig _colossus;

        [SerializeField, Tooltip("Each hive's numbers.")]
        private HiveConfig _hive;

        [SerializeField, Tooltip("The smoke grenade and cloud numbers.")]
        private SmokeConfig _smoke;

        [SerializeField, Tooltip("The waves and the pauses between them.")]
        private WaveConfig _waves;

        [SerializeField, Tooltip("The stick beekeeper's numbers.")]
        private BeekeeperConfig _stick;

        [SerializeField, Tooltip("The smoker beekeeper's numbers.")]
        private BeekeeperConfig _smoker;

        [SerializeField, Tooltip("How long the song window is and how many notes it remembers.")]
        private SongConfig _songConfig;

        [SerializeField, Tooltip("Every song the Piper can play, including the Dome and Tornado songs.")]
        private SwarmSongEffect[] _songs = new SwarmSongEffect[0];

        [SerializeField, Min(1), Tooltip("How many holes the flute has. The input has six keys.")]
        private int _holeCount = 6;

        [SerializeField, Tooltip("The baked body samples the swarm takes when it forms the giant.")]
        private ColossusSampleSet _colossusSamples;

        [SerializeField, Tooltip("Seed of the random source. The same seed gives the same match.")]
        private int _seed = 1010;

        [Header("Scene objects")]
        [SerializeField, Tooltip("Reads the keyboard and mouse.")]
        private HiveDefenseInput _input;

        [SerializeField, Tooltip("Walks the Piper.")]
        private PiperMotor _piperMotor;

        [SerializeField, Tooltip("The Piper's Animator. May stay empty.")]
        private Animator _piperAnimator;

        [SerializeField, Tooltip("Follows the Piper, or the giant while it stands.")]
        private FollowCamera _followCamera;

        [SerializeField, Tooltip("The camera that the mouse aim is relative to.")]
        private Camera _camera;

        [SerializeField, Tooltip("Draws the wasps.")]
        private SwarmGpuView _swarmView;

        [SerializeField, Tooltip("Draws the two hives, in the same order as the hives' positions.")]
        private HiveView[] _hives = new HiveView[0];

        [SerializeField, Tooltip("Where beekeepers enter. At least one.")]
        private Transform[] _spawnPoints = new Transform[0];

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

        private PathTrail _trail;
        private Transform _piperTransform;
        private Transform _colossusTransform;
        private Vector3 _lastAimPoint;
        private int _holeMask;
        private bool _isFollowingColossus;
        private bool _isRestarting;
        private bool _isReady;
        private float _domeRadius;
        private float _tornadoRadius;

        /// <summary>The running match, for the views, tests and debugging.</summary>
        public HiveDefenseSession Session { get; private set; }

        /// <summary>
        /// The one entry point for a played note. Collects the hole for the next tick and shows the playing pose.
        /// The hole counts from 0.
        /// </summary>
        public void PressHole(int hole)
        {
            if (!_isReady || hole < 0 || hole >= _holeCount)
            {
                return;
            }

            _holeMask |= HiveDefenseFrame.HoleBit(hole);
            _piperMotor.NotifyNotePlayed();
        }

        private void Awake()
        {
            CheckReferences();

            var positions = new Vector3[_hives.Length];
            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = _hives[index].transform.position;
            }

            var spawns = new Vector3[_spawnPoints.Length];
            for (int index = 0; index < spawns.Length; index++)
            {
                spawns[index] = _spawnPoints[index].position;
            }

            _piperTransform = _piperMotor.transform;
            _colossusTransform = _colossusMotor.transform;

            var rules = new HiveDefenseRules(
                _swarm, _piperMove, _fury, _colossus, _hive, _smoke, _waves, _stick, _smoker, _songConfig, _songs, _holeCount);
            var random = new Mulberry32Random(unchecked((uint)_seed));
            Session = new HiveDefenseSession(rules, positions, spawns, _piperMotor.Position, random);

            _trail = new PathTrail(_swarm.TrailPoints, _swarm.TrailSpacing);
            _trail.Reset(_piperMotor.Position);
            ReadSongRadii();

            _input.Initialize(_camera);
            _piperMotor.Initialize(_piperMove, _piperAnimator);
            _swarmView.Initialize(_swarm, _trail);
            _swarmView.SetFormRadii(_domeRadius, _tornadoRadius);
            for (int index = 0; index < _hives.Length; index++)
            {
                _hives[index].Initialize(Session.Hives[index], _camera);
            }

            _beekeeperPool.Initialize(Session, _stickPrefab, _smokerPrefab);
            _smokePool.Initialize(Session.Smoke, _grenadePrefab, _cloudPrefab);
            _colossusMotor.Initialize(Session, _swarmView, _colossusSamples, _colossus);

            _followCamera.SetTarget(_piperTransform);
            _lastAimPoint = _piperMotor.Position;
            _input.HolePressed += PressHole;
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
        }

        private void Restart()
        {
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
            }
        }

        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, _swarm.MaxDeltaTime);

            if (Session.State != HiveDefenseState.Playing)
            {
                if (_input.RestartPressed && !_isRestarting)
                {
                    _isRestarting = true;
                    Restart();
                }

                return;
            }

            bool isColossus = Session.IsColossusActive;
            Vector3 move = _input.Move;
            // While the giant stands the Piper stays put but still animates, plays and settles.
            _piperMotor.Move(
                isColossus ? Vector3.zero : move, Session.PiperSpeedMultiplier, isColossus || Session.IsPiperRooted, dt);

            _piperMotor.SetChannelling(Session.IsPiperRooted);

            Vector3 piper = _piperMotor.Position;
            _trail.Push(piper);

            if (_input.TryGetAimPoint(out Vector3 aim))
            {
                _lastAimPoint = aim;
            }

            // While the giant stands, the move steers the giant and the Piper stays where it is.
            var frame = new HiveDefenseFrame(
                piper, _lastAimPoint, new Vector2(move.x, move.z), _holeMask, _input.PunchPressed, _input.KickPressed);
            _holeMask = 0;
            Session.Tick(in frame, dt);

            FeedSwarm(piper);
            FeedCamera();
        }

        private void FeedSwarm(Vector3 piper)
        {
            SwarmFormMachine forms = Session.Forms;
            _swarmView.SetActiveCount(Session.Swarm.Count);
            _swarmView.SetForm(forms.Current);
            _swarmView.SetPiper(piper, _piperMotor.Forward);

            ISwarmTarget stingTarget = forms.StingTarget;
            _swarmView.SetSting(stingTarget != null ? stingTarget.Position : piper, forms.StingFraction);

            HiveSet hives = Session.Hives;
            _swarmView.SetHives(hives[0].Position, hives[1].Position, !hives[0].IsDestroyed, !hives[1].IsDestroyed);
            _swarmView.SetLastHit(Session.LastHitPoint);
        }

        private void FeedCamera()
        {
            bool isColossus = Session.IsColossusActive;
            if (isColossus == _isFollowingColossus)
            {
                return;
            }

            _isFollowingColossus = isColossus;
            if (isColossus)
            {
                _followCamera.SetTarget(_colossusTransform);
                _followCamera.SetDistance(_colossus.CameraDistance);
            }
            else
            {
                _followCamera.SetTarget(_piperTransform);
                _followCamera.ResetDistance();
            }
        }

        private void ReadSongRadii()
        {
            for (int index = 0; index < _songs.Length; index++)
            {
                if (_songs[index] is DomeSong dome)
                {
                    _domeRadius = dome.Radius;
                }
                else if (_songs[index] is TornadoSong tornado)
                {
                    _tornadoRadius = tornado.Radius;
                }
            }
        }

        private void CheckReferences()
        {
            Require(_swarm, nameof(_swarm));
            Require(_piperMove, nameof(_piperMove));
            Require(_fury, nameof(_fury));
            Require(_colossus, nameof(_colossus));
            Require(_hive, nameof(_hive));
            Require(_smoke, nameof(_smoke));
            Require(_waves, nameof(_waves));
            Require(_stick, nameof(_stick));
            Require(_smoker, nameof(_smoker));
            Require(_songConfig, nameof(_songConfig));
            Require(_colossusSamples, nameof(_colossusSamples));
            Require(_input, nameof(_input));
            Require(_piperMotor, nameof(_piperMotor));
            Require(_followCamera, nameof(_followCamera));
            Require(_camera, nameof(_camera));
            Require(_swarmView, nameof(_swarmView));
            Require(_beekeeperPool, nameof(_beekeeperPool));
            Require(_stickPrefab, nameof(_stickPrefab));
            Require(_smokerPrefab, nameof(_smokerPrefab));
            Require(_smokePool, nameof(_smokePool));
            Require(_grenadePrefab, nameof(_grenadePrefab));
            Require(_cloudPrefab, nameof(_cloudPrefab));
            Require(_colossusMotor, nameof(_colossusMotor));
            Require(_hud, nameof(_hud));

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

            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                throw new InvalidOperationException($"{name}: HiveDefenseRoot needs at least one spawn point.");
            }

            for (int index = 0; index < _spawnPoints.Length; index++)
            {
                Require(_spawnPoints[index], $"{nameof(_spawnPoints)}[{index}]");
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
