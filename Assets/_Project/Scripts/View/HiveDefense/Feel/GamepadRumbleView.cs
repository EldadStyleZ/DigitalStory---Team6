using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DigitalStory.View
{
    /// <summary>
    /// The felt hit on a controller. Its motors tick lightly for each note, thump when a song is cast and when the swarm's or the
    /// giant's attack hits beekeepers (at most once per gap, so a stream of hits does not buzz the whole time), rumble hard when
    /// the giant's slam or its leap lands, firmly as a giant's fist leaves its arm and as it hits, and long and low when a hive is
    /// destroyed or the swarm collapses. The pulses mix in a <see cref="RumbleMixer"/>. It rumbles only while the controller is
    /// the device in use, and stops the motors at once whenever it is disabled or destroyed (a scene unload) and the game is
    /// paused or loses focus. When the match ends no new pulse starts, and the one running plays out (the long rumble of the
    /// last hive falling ends the match on its own tick).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Gamepad Rumble View")]
    public sealed class GamepadRumbleView : MonoBehaviour
    {
        private readonly RumbleMixer _mixer = new RumbleMixer();
        private HiveDefenseSession _session;
        private HiveDefenseInput _input;
        private GamepadConfig _config;
        private RateLimiter _hitLimiter;
        private Gamepad _pad;
        private MotorSpeeds _sent;
        private bool _isReady;
        private bool _isOver;

        /// <summary>The speeds the controller's motors were last set to, for tests and debugging.</summary>
        public MotorSpeeds Speeds => _sent;

        /// <summary>Starts listening to the match and the input. Call once, before the first Update; calling again starts over.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        public void Initialize(HiveDefenseSession session, HiveDefenseInput input, GamepadConfig config)
        {
            Release();
            Stop();
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _input = input != null ? input : throw new ArgumentNullException(nameof(input));
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _hitLimiter = new RateLimiter(config.HitGapSeconds);
            _isOver = session.State != HiveDefenseState.Playing;
            _mixer.Open();

            _input.HolePressed += HandleHolePressed;
            _session.SongCastNow += HandleSongCastNow;
            _session.SongLanded += HandleSongLanded;
            _session.ColossusAttackLanded += HandleColossusAttackLanded;
            _session.ColossusFistLaunched += HandleColossusFist;
            _session.ColossusFistLanded += HandleColossusFist;
            _session.Hives.HiveDestroyed += HandleHiveDestroyed;
            _session.Swarm.Collapsed += HandleSwarmCollapsed;
            _session.StateChanged += HandleStateChanged;
            _isReady = true;
        }

        // Real seconds, so a pulse lasts as long during the slow-down as outside it.
        private void Update()
        {
            if (!_isReady)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            _hitLimiter.Tick(deltaTime);
            if (Time.timeScale <= 0f || _input.LastDevice != ControlDevice.Gamepad)
            {
                _mixer.Clear();
            }

            Apply(_mixer.Tick(deltaTime));
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Stop();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                Stop();
            }
        }

        private void OnDestroy()
        {
            Release();
            Stop();
        }

        private void Release()
        {
            _isReady = false;
            if (_input != null)
            {
                _input.HolePressed -= HandleHolePressed;
            }

            if (_session != null)
            {
                _session.SongCastNow -= HandleSongCastNow;
                _session.SongLanded -= HandleSongLanded;
                _session.ColossusAttackLanded -= HandleColossusAttackLanded;
                _session.ColossusFistLaunched -= HandleColossusFist;
                _session.ColossusFistLanded -= HandleColossusFist;
                _session.Hives.HiveDestroyed -= HandleHiveDestroyed;
                _session.Swarm.Collapsed -= HandleSwarmCollapsed;
                _session.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleHolePressed(int hole) => Pulse(_config.NotePulse);

        private void HandleSongCastNow(int song) => Pulse(_config.CastPulse);

        // A blocked hit is a beekeeper's blow on a shield, not the swarm's attack landing.
        private void HandleSongLanded(SongHit hit)
        {
            if (!hit.IsBlocked && _hitLimiter.TryPass())
            {
                Pulse(_config.HitPulse);
            }
        }

        private void HandleColossusAttackLanded(ColossusAttack attack)
        {
            if (attack == ColossusAttack.Slam || attack == ColossusAttack.LeapSlam)
            {
                Pulse(_config.HeavyPulse);
            }
        }

        private void HandleColossusFist(ColossusHand hand) => Pulse(_config.FistPulse);

        private void HandleHiveDestroyed(int hive) => Pulse(_config.LossPulse);

        private void HandleSwarmCollapsed() => Pulse(_config.LossPulse);

        private void HandleStateChanged(HiveDefenseState state)
        {
            if (state != HiveDefenseState.Playing)
            {
                _isOver = true;
                _mixer.Close();
            }
        }

        private void Pulse(in RumblePulse pulse)
        {
            if (_isOver || _input.LastDevice != ControlDevice.Gamepad)
            {
                return;
            }

            _mixer.Add(pulse);
        }

        // Sets the motors of the controller in use only when the speeds change, and stops the one before when another takes over.
        private void Apply(MotorSpeeds speeds)
        {
            Gamepad pad = Gamepad.current;
            if (pad != _pad)
            {
                StopMotors();
                _pad = pad;
            }

            if (_pad == null || (Mathf.Approximately(speeds.Low, _sent.Low) && Mathf.Approximately(speeds.High, _sent.High)))
            {
                return;
            }

            _pad.SetMotorSpeeds(speeds.Low, speeds.High);
            _sent = speeds;
        }

        private void Stop()
        {
            _mixer.Clear();
            StopMotors();
        }

        // A controller that was unplugged has no motors to stop.
        private void StopMotors()
        {
            if (_pad != null && _pad.added)
            {
                _pad.SetMotorSpeeds(0f, 0f);
            }

            _sent = default;
        }
    }
}
