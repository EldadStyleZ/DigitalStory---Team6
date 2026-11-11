using System;
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
        private const int RequiredHiveCount = 2;
        private const float MinimumTrailMeters = 0.001f;

        // Share of the river's length behind the core where its light hangs: nearer the head than the middle, so the
        // inner ring at the core is in the light too.
        private const float RiverLightShare = 0.35f;

        // Share of the way per second the attack light covers toward its target, slower than the light's own follow,
        // so it arrives with the stingers.
        private const float AttackLightFollowPerSecond = 4f;

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

        [SerializeField, Tooltip("Seed of the random source. The same seed gives the same match.")]
        private int _seed = 1010;

        [Header("Scene objects")]
        [SerializeField, Tooltip("Reads the keyboard and mouse.")]
        private HiveDefenseInput _input;

        [SerializeField, Tooltip("Moves the swarm's core, the point the swarm gathers around.")]
        private SwarmCoreMotor _coreMotor;

        [SerializeField, Tooltip("Follows the core, from further back while the giant stands.")]
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
        [SerializeField, Tooltip("Sounds each note the player plays. May stay empty, and the flute is then silent.")]
        private SwarmFluteVoice _fluteVoice;

        [SerializeField, Tooltip("How the flute sounds. Needed when the flute voice is set; it must have a pitch for every hole.")]
        private SoundConfig _sound;

        private PathTrail _trail;
        private Vector3 _lastAimPoint;
        private Vector3 _attackSpot;
        private bool _isAttackSpotSet;
        private int _holeMask;
        private bool _hasFormChangedThisTick;
        private bool _isCameraBackForColossus;
        private bool _isRestarting;
        private bool _isReady;
        private bool _isWaveCalled;
        private bool _isGiveHeld;

        /// <summary>The running match, for the views, tests and debugging.</summary>
        public HiveDefenseSession Session { get; private set; }

        /// <summary>
        /// The one entry point for a played note. Collects the hole for the next tick and sounds it. The hole counts from 0.
        /// </summary>
        public void PressHole(int hole)
        {
            if (!_isReady || hole < 0 || hole >= _holeCount)
            {
                return;
            }

            _holeMask |= HiveDefenseFrame.HoleBit(hole);

            if (_fluteVoice != null)
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
        /// Holds Give, as holding E does, until it is released with false. Beside a hurt hive the swarm then pays wasps into
        /// its health. E works either way.
        /// </summary>
        public void HoldGive(bool isHeld)
        {
            _isGiveHeld = isHeld;
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

            var rules = new HiveDefenseRules(
                _swarm, _squads, _coreMove, _fury, _colossus, _hive, _smoke, _waves, _stick, _smoker, _aim, _songConfig, _songs, _holeCount);
            var random = new Mulberry32Random(unchecked((uint)_seed));
            Session = new HiveDefenseSession(rules, positions, spawns, _coreMotor.Position, random);

            _trail = new PathTrail(_swarm.TrailPoints, _swarm.TrailSpacing);
            _trail.Reset(_coreMotor.Position);

            _input.Initialize(_camera);
            _coreMotor.Initialize(_coreMove);
            _swarmView.Initialize(_swarm, _trail);
            _glowLight.Initialize(_swarm);
            _attackGlowLight.Initialize(_swarm);
            Session.Forms.FormChanged += HandleFormChanged;
            Session.Caster.SongCast += HandleSongCast;
            for (int index = 0; index < _hives.Length; index++)
            {
                _hives[index].Initialize(Session.Hives[index], _camera, _hive);
            }

            _beekeeperPool.Initialize(Session, _stickPrefab, _smokerPrefab);
            _smokePool.Initialize(Session.Smoke, _grenadePrefab, _cloudPrefab);
            _colossusMotor.Initialize(Session, _swarmView, _colossusSamples, _colossus);

            if (_fluteVoice != null)
            {
                // A held key keeps its note sounding past the shortest note length.
                _fluteVoice.Initialize(_sound, _coreMove);
                _fluteVoice.Connect(_input);
            }

            _followCamera.SetTarget(_coreMotor.transform);
            _lastAimPoint = _coreMotor.Position;
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
                Session.Forms.FormChanged -= HandleFormChanged;
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
                // The match is over, so nothing flows between the core and a hive any more.
                StopHiveFlows();
                if (_input.RestartPressed && !_isRestarting)
                {
                    _isRestarting = true;
                    Restart();
                }

                return;
            }

            // While the giant stands the move steers the giant, which the session moves with the core inside it, so the
            // motor only settles the core on the ground.
            bool isColossus = Session.IsColossusActive;
            Vector3 move = _input.Move;
            _coreMotor.Move(
                isColossus ? Vector3.zero : move, Session.CoreSpeedMultiplier, isColossus || Session.IsCoreRooted, dt);

            if (_input.TryGetAimPoint(out Vector3 aim))
            {
                _lastAimPoint = aim;
            }

            if (_input.CallWavePressed)
            {
                CallWave();
            }

            // The mouse aims only while it is live; otherwise auto-aim chooses.
            var frame = new HiveDefenseFrame(
                _coreMotor.Position,
                _lastAimPoint,
                new Vector2(move.x, move.z),
                _holeMask,
                _input.PunchPressed,
                _input.KickPressed,
                _input.IsMouseLive,
                _input.GiveHeld || _isGiveHeld,
                _isWaveCalled);
            _holeMask = 0;
            _isWaveCalled = false;
            _hasFormChangedThisTick = false;
            Session.Tick(in frame, dt);

            // The session says where the core is: inside the giant while it stands, where the giant stood when it ends, and
            // never past the play area's edge. Once the giant is gone the core is put down clear of hives and walls, so the
            // controller has nothing to jump out of on the next move.
            _coreMotor.PlaceAt(Session.CorePosition, !Session.IsColossusActive);
            Vector3 core = _coreMotor.Position;
            _trail.Push(core);

            FeedSwarm(core);
            FeedHives(core);
            FeedGlow(core, dt);
            FeedCamera();
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

        private void HandleFormChanged(SwarmForm form)
        {
            _hasFormChangedThisTick = true;
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
        // the wasps that attack. The first hangs over the middle of the river or the ring in the river's colour, over the
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
                attackAt = StrikeLightPoint(core, strike);
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

        // Where the striking squad is: on the sting's target, or on the meteor's ball (over the core while it climbs and
        // hangs, then along the line down to the impact point, as MeteorSteer in WaspSwarm.compute flies it), or around the
        // core for a song that strikes there, such as the Tornado.
        private static Vector3 StrikeLightPoint(Vector3 core, in SlotState strike)
        {
            Vector4 a = strike.ParamA;
            Vector3 target = new Vector3(a.x, a.y, a.z);
            if (strike.Form == SwarmForm.Sting)
            {
                return target;
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

        // About a third of the way along the river behind the core, or the core while the river curls into the ring around
        // it. It follows the swarm's own blend in WaspSwarm.compute (RiverTarget): a short path, or standing, makes the ring.
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
            float trailShare = Mathf.Clamp01(length / Mathf.Max(_swarm.StandingTrailMeters, MinimumTrailMeters));
            float riverShare = trailShare * (1f - _swarmView.StandingShare);
            return Vector3.Lerp(core, river, riverShare);
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

            HiveSet hives = Session.Hives;
            _swarmView.SetHives(hives[0].Position, hives[1].Position, !hives[0].IsDestroyed, !hives[1].IsDestroyed);
            _swarmView.SetLastHit(Session.LastHitPoint);
        }

        // The camera always follows the core, which rides inside the giant while it stands; it only steps back for the giant.
        private void FeedCamera()
        {
            bool isColossus = Session.IsColossusActive;
            if (isColossus == _isCameraBackForColossus)
            {
                return;
            }

            _isCameraBackForColossus = isColossus;
            if (isColossus)
            {
                _followCamera.SetDistance(_colossus.CameraDistance);
            }
            else
            {
                _followCamera.ResetDistance();
            }
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
            Require(_smoke, nameof(_smoke));
            Require(_waves, nameof(_waves));
            Require(_stick, nameof(_stick));
            Require(_smoker, nameof(_smoker));
            Require(_songConfig, nameof(_songConfig));
            Require(_colossusSamples, nameof(_colossusSamples));
            Require(_input, nameof(_input));
            Require(_coreMotor, nameof(_coreMotor));
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
