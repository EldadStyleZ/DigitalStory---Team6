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
        private const float MinimumTrailMeters = 0.001f;

        // Share of the river's length behind the Piper where its light hangs: nearer the head than the middle, so the
        // Piper walks in the light too.
        private const float RiverLightShare = 0.35f;

        // Share of the way per second the attack light covers toward its target, slower than the light's own follow,
        // so it arrives with the stingers.
        private const float AttackLightFollowPerSecond = 4f;

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

        [SerializeField, Tooltip("Every song the Piper can play, including the Dome, Tornado and Meteor songs.")]
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

        [SerializeField, Tooltip("The light the glowing swarm casts on the ground, the trees and the beekeepers around it.")]
        private SwarmGlowLight _glowLight;

        [SerializeField, Tooltip("The red light of the wasps that attack in a sting or a meteor. It travels with them, while the first light stays with the rest of the swarm.")]
        private SwarmGlowLight _attackGlowLight;

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

        [Header("Sound")]
        [SerializeField, Tooltip("Sounds each note the Piper plays. May stay empty, and the flute is then silent.")]
        private SwarmFluteVoice _fluteVoice;

        [SerializeField, Tooltip("How the flute sounds. Needed when the flute voice is set; it must have a pitch for every hole.")]
        private SoundConfig _sound;

        private PathTrail _trail;
        private Transform _piperTransform;
        private Transform _colossusTransform;
        private Vector3 _lastAimPoint;
        private Color _castColor;
        private Vector3 _attackSpot;
        private float _meteorSeconds;
        private bool _isAttackSpotSet;
        private int _holeMask;
        private bool _isFollowingColossus;
        private bool _isRestarting;
        private bool _isReady;
        private float _domeRadius;
        private float _tornadoRadius;
        private float _meteorRadius;

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

            if (_fluteVoice != null)
            {
                _fluteVoice.PlayNote(hole);
            }
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
            _swarmView.SetMeteorRadius(_meteorRadius);
            _glowLight.Initialize(_swarm);
            _attackGlowLight.Initialize(_swarm);
            _castColor = _swarm.RiverColor;
            Session.Caster.SongCast += HandleSongCast;
            for (int index = 0; index < _hives.Length; index++)
            {
                _hives[index].Initialize(Session.Hives[index], _camera);
            }

            _beekeeperPool.Initialize(Session, _stickPrefab, _smokerPrefab);
            _smokePool.Initialize(Session.Smoke, _grenadePrefab, _cloudPrefab);
            _colossusMotor.Initialize(Session, _swarmView, _colossusSamples, _colossus);

            if (_fluteVoice != null)
            {
                // A held key keeps its note sounding past the shortest note length.
                _fluteVoice.Initialize(_sound, _piperMove);
                _fluteVoice.Connect(_input);
            }

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

            if (Session != null)
            {
                Session.Caster.SongCast -= HandleSongCast;
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
            FeedGlow(piper, dt);
            FeedCamera();
        }

        // Every form but the River takes the colour of the category of the song that made it.
        private void HandleSongCast(int song)
        {
            _castColor = _swarm.ColorOf(Session.Caster[song].Category);
            _swarmView.SetCastColor(_castColor);
        }

        // One light stands in for the glow of the wasps that stay with the Piper, and a second one for the wasps that attack
        // in a sting or a meteor, the share the form sends. The first hangs over the middle of the river or the ring (or
        // the giant), in the river's colour, as strong as the swarm is full and the share that stays. The second travels
        // with the stingers, or hangs over the meteor ball until the dive starts, in the attack colour. Both sit a little
        // toward the camera, so the sides it sees are lit.
        private void FeedGlow(Vector3 piper, float deltaTime)
        {
            SwarmFormMachine forms = Session.Forms;
            SwarmForm form = forms.Current;
            Vector3 middle = RiverMiddle(piper);
            Color color = _swarm.RiverColor;
            float attackShare = 0f;
            Vector3 attackAt = piper;

            if (form == SwarmForm.Sting && forms.StingTarget != null)
            {
                attackShare = forms.StingFraction;
                attackAt = forms.StingTarget.Position;
            }
            else if (form == SwarmForm.Meteor)
            {
                attackShare = forms.MeteorFraction;
                _meteorSeconds += deltaTime;
                attackAt = MeteorBallPosition(piper, forms);
            }
            else if (form == SwarmForm.Colossus)
            {
                middle = _colossusTransform.position;
                color = _castColor;
            }
            else if (form != SwarmForm.River && form != SwarmForm.Collapsed)
            {
                middle = piper;
                color = _castColor;
            }

            if (form != SwarmForm.Meteor)
            {
                _meteorSeconds = 0f;
            }

            Vector3 towardCamera = -_camera.transform.forward;
            towardCamera.y = 0f;
            if (towardCamera.sqrMagnitude > 0f)
            {
                towardCamera.Normalize();
            }

            SwarmBody swarm = Session.Swarm;
            float strength = swarm.Max > 0 ? (float)swarm.Count / swarm.Max : 0f;
            Vector3 lift = (Vector3.up * _swarm.LightHeight) + (towardCamera * _swarm.LightTowardCamera);
            _glowLight.Show(middle + lift, color, strength * (1f - attackShare));

            // The attacking light eases to a sting's target slower than the light's own follow, so it arrives with the
            // stingers. The meteor's path is known, so its light rides the ball and lands with the impact.
            if (!_isAttackSpotSet || attackShare <= 0f || form == SwarmForm.Meteor)
            {
                _attackSpot = attackAt;
                _isAttackSpotSet = true;
            }
            else
            {
                _attackSpot = Vector3.Lerp(_attackSpot, attackAt, 1f - Mathf.Exp(-AttackLightFollowPerSecond * deltaTime));
            }

            _attackGlowLight.Show(_attackSpot + lift, _castColor, strength * attackShare);
        }

        // Where the meteor's ball is: over the Piper while it climbs and hangs, then along the line down to the impact
        // point during the dive, as WaspSwarm.compute flies it (MeteorBallPoint and MeteorDiveTarget).
        private Vector3 MeteorBallPosition(Vector3 piper, SwarmFormMachine forms)
        {
            Vector3 top = piper + (Vector3.up * forms.MeteorHeight);
            float diveSeconds = _meteorSeconds - forms.MeteorRiseSeconds;
            if (diveSeconds <= 0f)
            {
                return top;
            }

            float dive = forms.MeteorDiveSeconds > 0f ? Mathf.Clamp01(diveSeconds / forms.MeteorDiveSeconds) : 1f;
            return Vector3.Lerp(top, forms.MeteorTarget, dive);
        }

        // About a third of the way along the river behind the Piper, or the Piper while the river curls into the ring around
        // them. It follows the swarm's own blend in WaspSwarm.compute (RiverTarget): a short path, or standing, makes the ring.
        private Vector3 RiverMiddle(Vector3 piper)
        {
            float length = _trail.Length;
            if (!(length > 0f))
            {
                return piper;
            }

            float back = Mathf.Min(_swarm.RiverLengthMeters, length) * RiverLightShare;
            Vector3 river = _trail.PointAt(back / length);
            river.y = piper.y;
            float trailShare = Mathf.Clamp01(length / Mathf.Max(_swarm.StandingTrailMeters, MinimumTrailMeters));
            float riverShare = trailShare * (1f - _swarmView.StandingShare);
            return Vector3.Lerp(piper, river, riverShare);
        }

        private void FeedSwarm(Vector3 piper)
        {
            SwarmFormMachine forms = Session.Forms;
            _swarmView.SetActiveCount(Session.Swarm.Count);
            _swarmView.SetForm(forms.Current);
            _swarmView.SetPiper(piper, _piperMotor.Forward);

            ISwarmTarget stingTarget = forms.StingTarget;
            _swarmView.SetSting(stingTarget != null ? stingTarget.Position : piper, forms.StingFraction);

            if (forms.Current == SwarmForm.Meteor)
            {
                _swarmView.SetMeteor(
                    forms.MeteorTarget, forms.MeteorFraction, forms.MeteorRiseSeconds, forms.MeteorDiveSeconds, forms.MeteorHeight);
            }

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
                else if (_songs[index] is MeteorSong meteor)
                {
                    _meteorRadius = meteor.Radius;
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
