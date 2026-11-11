using System.Collections.Generic;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// What a player's notes do. The root note calls: while it plays, a ring in the player's colour shows the call's
    /// reach, and every vermin inside it hears the call and follows. LT and RT played one after the other, quickly,
    /// make the player run faster, with a trail of paint dabs behind. The other notes only make sound.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInstrument), typeof(PlayerIdentity), typeof(PlayerMovement))]
    public sealed class PlayerAbilities : MonoBehaviour
    {
        [Header("Call (root note)")]
        [SerializeField, Tooltip("The ring shown while calling: the player's Call Area child, centred on the body. The call reaches out from its centre.")]
        private CallArea _callArea;

        [SerializeField, Min(0.1f), Tooltip("How far the call reaches from the player, in units.")]
        private float _callRadius = 2.5f;

        [SerializeField, Tooltip("Which layers hold vermin.")]
        private LayerMask _verminLayers;

        [Header("Run (LT and RT, one after the other, fast)")]
        [SerializeField, Min(0f), Tooltip("The top speed when running flat out, in units per second. Walking speed is on PlayerMovement.")]
        private float _runSpeedMax = 7f;

        [SerializeField, Min(0f), Tooltip("How much each quick switch between LT and RT adds to the top speed, in units per second.")]
        private float _speedPerSwitch = 0.4f;

        [SerializeField, Min(0.05f), Tooltip("The longest gap between LT and RT that still counts as a quick switch, in seconds.")]
        private float _switchWindow = 0.35f;

        [SerializeField, Min(0f), Tooltip("How fast the speed eases back to walking once the switches stop, in units per second, per second.")]
        private float _easeBackPerSecond = 3f;

        [SerializeField, Tooltip("The paint dabs dropped behind the player while running.")]
        private SpeedTrail _speedTrail;

        [Header("Blow (hold Left, let go when the bar is full)")]
        [SerializeField, Tooltip("The blow's numbers, shared by both players.")]
        private BlowSettings _blowSettings;

        [SerializeField, Tooltip("The charge bar under the player's feet.")]
        private BlowBar _blowBar;

        private BlowCharge _blow;

        private readonly List<Collider2D> _heard = new List<Collider2D>();
        private PlayerInstrument _instrument;
        private PlayerIdentity _identity;
        private PlayerNotes _notes;
        private PlayerMovement _movement;
        private RunRhythm _run;
        private ContactFilter2D _filter;

        /// <summary>What the player may do now; set by the round for each stage.</summary>
        public PlayerPermissions Permissions { get; private set; } = PlayerPermissions.All;

        /// <summary>Whether the player is calling now.</summary>
        // public bool IsCalling => _instrument.IsPlaying(Note.Root);
        public bool IsCalling => Permissions.CanCall && _instrument.IsPlaying(Note.Root);

        /// <summary>Sets what the player may do: walk, play notes, call, run and blow.</summary>
        public void Apply(PlayerPermissions permissions)
        {
            // Stop charging first: switching notes off below releases Left, which must not fire the blow.
            if (!permissions.CanBlow)
            {
                CancelBlow();
            }

            Permissions = permissions;
            _movement.CanMove = permissions.CanMove;
            _notes.CanPlay = permissions.CanPlay;

            if (!permissions.CanCall && _callArea != null)
            {
                _callArea.Hide();
            }
        }

        /// <summary>How far the call reaches.</summary>
        public float CallRadius => _callRadius;

        /// <summary>The run's top speed now, from walking up to the run speed.</summary>
        public float RunSpeed => _run.Speed;

        /// <summary>How hard the player is running: 0 walking, 1 flat out.</summary>
        public float RunBoost => _run.Boost;

        /// <summary>Whether the blow is charging now.</summary>
        public bool IsChargingBlow => _blow != null && _blow.IsCharging;

        /// <summary>How full the blow's charge is, 0 to 1.</summary>
        public float BlowProgress => _blow != null ? _blow.Progress(Time.time) : 0f;

        /// <summary>Raised when the blow fires, with the way the player aims.</summary>
        public event System.Action<Vector2> Blew;

        private void Awake()
        {
            _instrument = GetComponent<PlayerInstrument>();
            _identity = GetComponent<PlayerIdentity>();
            _notes = GetComponent<PlayerNotes>();
            _movement = GetComponent<PlayerMovement>();
            _run = new RunRhythm(_movement.WalkSpeed, _runSpeedMax, _speedPerSwitch, _switchWindow, _easeBackPerSecond);

            if (_blowSettings == null || _blowBar == null)
            {
                Debug.LogError($"{name}: PlayerAbilities is missing its blow settings or blow bar.", this);
            }
            else
            {
                _blow = new BlowCharge(_blowSettings.ChargeSeconds);
                _blowBar.Initialize(_blowSettings);
                _blowBar.HideNow();
            }

            if (_speedTrail == null)
            {
                Debug.LogError($"{name}: PlayerAbilities is missing its speed trail.", this);
            }
            _filter = new ContactFilter2D { useLayerMask = true, layerMask = _verminLayers, useTriggers = false };

            if (_callArea == null)
            {
                Debug.LogError($"{name}: PlayerAbilities is missing its call area child.", this);
                return;
            }

            _callArea.SetRadius(_callRadius);
            _callArea.HideNow();
        }

        // The middle of the call: the Call Area child, which sits on the player's body.
        private Vector2 CallCentre => _callArea != null ? (Vector2)_callArea.transform.position : (Vector2)transform.position;

        private void OnEnable()
        {
            _instrument.NoteStarted += HandleNoteStarted;
            _instrument.NoteEnded += HandleNoteEnded;
            _notes.NotePressed += HandleNotePressed;
            _notes.NoteReleased += HandleNoteReleased;
        }

        private void OnDisable()
        {
            _instrument.NoteStarted -= HandleNoteStarted;
            _instrument.NoteEnded -= HandleNoteEnded;
            _notes.NotePressed -= HandleNotePressed;
            _notes.NoteReleased -= HandleNoteReleased;
        }

        // Every press counts toward the run, even a quick tap of a note already playing.
        private void HandleNotePressed(Note note)
        {
            PressForBlow(note);

            if (!Permissions.CanRun)
            {
                return;
            }

            _run.Press(note, Time.time);
        }

        // Left starts the charge; any other note cancels it.
        private void PressForBlow(Note note)
        {
            if (_blow == null)
            {
                return;
            }

            if (BlowCharge.Charges(note) && Permissions.CanBlow)
            {
                _blow.Begin(Time.time);
            }
            else if (_blow.IsCharging)
            {
                CancelBlow();
            }
        }

        private void HandleNoteReleased(Note note)
        {
            if (_blow == null || !_blow.IsCharging || !BlowCharge.Charges(note))
            {
                return;
            }

            bool fires = _blow.Release(Time.time);
            _blowBar.Hide();

            if (fires)
            {
                Blew?.Invoke(_movement.AimDirection);
            }
        }

        private void CancelBlow()
        {
            if (_blow != null && _blow.IsCharging)
            {
                _blow.Cancel();
                _blowBar.Hide();
            }
        }

        private void Update()
        {
            if (_blow != null && _blow.IsCharging)
            {
                _blowBar.Show(_blow.Progress(Time.time), _identity.Colour);
            }

            _run.Tick(Time.time, Time.deltaTime);
            _movement.TopSpeed = _run.Speed;

            if (_speedTrail != null)
            {
                _speedTrail.Show(_run.Boost, _movement.Velocity);
            }
        }

        private void HandleNoteStarted(Note note)
        {
            if (note == Note.Root && _callArea != null && Permissions.CanCall)
            {
                _callArea.Show(_identity.Colour);
            }
        }

        private void HandleNoteEnded(Note note)
        {
            if (note == Note.Root && _callArea != null)
            {
                _callArea.Hide();
            }
        }

        private void FixedUpdate()
        {
            if (!IsCalling)
            {
                return;
            }

            float callStartedAt = _instrument.StartedAt(Note.Root);
            Physics2D.OverlapCircle(CallCentre, _callRadius, _filter, _heard);

            foreach (Collider2D collider in _heard)
            {
                Rigidbody2D body = collider.attachedRigidbody;
                Vermin vermin = body != null ? body.GetComponent<Vermin>() : collider.GetComponent<Vermin>();

                if (vermin != null)
                {
                    vermin.HearCall(_identity, callStartedAt);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(CallCentre, _callRadius);
        }
    }
}
