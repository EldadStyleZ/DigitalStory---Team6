using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>
    /// What a player's notes do. The root note calls: while it plays, a ring in the player's colour shows the call's
    /// reach, and every vermin inside it hears the call and follows. Holding the run button (A or Down arrow) makes the
    /// player run faster, with a trail of paint dabs behind. RT (hold, let go when full) blows, in red; LT, charged the
    /// same way, plays the earworm in the player's colour: the same wave and chord, but free vermin it reaches follow
    /// the player for a while, deaf to the other player's calls. The other notes only make sound.
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

        [Header("Run (hold A or Down arrow)")]
        [SerializeField, Min(0f), Tooltip("The top speed when running flat out, in units per second. Walking speed is on PlayerMovement.")]
        private float _runSpeedMax = 7f;

        [SerializeField, Min(0f), Tooltip("Seconds from walking to the run speed while the run button is held. Letting go slows back at the same rate, starting at once, so it takes as long as the speeding up did.")]
        private float _accelerationTime = 0.1f;

        [SerializeField, Tooltip("The paint dabs dropped behind the player while running.")]
        private SpeedTrail _speedTrail;

        [Header("Blow (hold RT) and earworm (hold LT): let go when the bar is full")]
        [SerializeField, Tooltip("The blow's numbers, shared by both players.")]
        private BlowSettings _blowSettings;

        [SerializeField, Tooltip("The charge bar under the player's feet.")]
        private BlowBar _blowBar;

        private BlowCharge _blow;
        private BlowCharge _earworm;
        private readonly List<Collider2D> _blowHits = new List<Collider2D>();
        private readonly HashSet<Rigidbody2D> _blowBodies = new HashSet<Rigidbody2D>();

        private readonly List<Collider2D> _heard = new List<Collider2D>();
        private PlayerInstrument _instrument;
        private PlayerIdentity _identity;
        private PlayerNotes _notes;
        private PlayerMovement _movement;
        private RunHold _run;
        private InputAction _runAction;
        private ContactFilter2D _filter;

        /// <summary>What the player may do now; set by the round for each stage.</summary>
        public PlayerPermissions Permissions { get; private set; } = PlayerPermissions.All;

        /// <summary>Whether the player is calling now.</summary>
        // public bool IsCalling => _instrument.IsPlaying(Note.Root);
        public bool IsCalling => Permissions.CanCall && _instrument.IsPlaying(Note.Root);

        /// <summary>Sets what the player may do: walk, play notes, call, run and blow.</summary>
        public void Apply(PlayerPermissions permissions)
        {
            // Stop charging first: switching notes off below releases Left or RT, which must not fire anything.
            if (!permissions.CanBlow)
            {
                CancelCharge(_blow);
            }

            if (!CanEarworm(permissions))
            {
                CancelCharge(_earworm);
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

        /// <summary>Whether the blow or the earworm is charging now.</summary>
        public bool IsChargingBlow => ChargingNow != null;

        /// <summary>How full the blow's (or earworm's) charge is, 0 to 1.</summary>
        public float BlowProgress => ChargingNow != null ? ChargingNow.Progress(Time.time) : 0f;

        /// <summary>
        /// The colour of the charge filling now, or of the last one fired: the blow's own colour (red) or, for the
        /// earworm, the player's. The bar, area, wave and dabs all take it.
        /// </summary>
        public Color EffectColour => BlowCharge.EffectColour(
            (ChargingNow ?? _lastFired) != _earworm, _identity.Colour, _blowSettings != null ? _blowSettings.BlowColour : Color.red);

        private BlowCharge _lastFired;
        private float _chargeLockedUntil = float.NegativeInfinity;

        // The charge filling now, the blow's or the earworm's, or null. Only one charges at a time.
        private BlowCharge ChargingNow =>
            _blow != null && _blow.IsCharging ? _blow : _earworm != null && _earworm.IsCharging ? _earworm : null;

        // The earworm changes who vermin follow, so it needs the call allowed as well as the blow.
        private static bool CanEarworm(PlayerPermissions permissions) => permissions.CanBlow && permissions.CanCall;

        /// <summary>Raised when the blow or the earworm fires (they look the same), with the way the player aims.</summary>
        public event System.Action<Vector2> Blew;

        private void Awake()
        {
            _instrument = GetComponent<PlayerInstrument>();
            _identity = GetComponent<PlayerIdentity>();
            _notes = GetComponent<PlayerNotes>();
            _movement = GetComponent<PlayerMovement>();
            _run = new RunHold(_movement.WalkSpeed, _runSpeedMax, _accelerationTime);
            _runAction = GetComponent<PlayerInput>().actions.FindAction("Player/Run", true);

            if (_blowSettings == null || _blowBar == null)
            {
                Debug.LogError($"{name}: PlayerAbilities is missing its blow settings or blow bar.", this);
            }
            else
            {
                _blow = new BlowCharge(_blowSettings.ChargeSeconds);
                _earworm = new BlowCharge(_blowSettings.ChargeSeconds);
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

        private void HandleNotePressed(Note note)
        {
            PressForCharges(note);
        }

        // RT starts the blow's charge and Left (LT) the earworm's; any other press, the other one's included, cancels it.
        private void PressForCharges(Note note)
        {
            if (_blow == null)
            {
                return;
            }

            BlowCharge wanted = IsChargeInterrupted ? null : ChargeStartedBy(note);
            BlowCharge charging = ChargingNow;

            if (charging != null && charging != wanted)
            {
                CancelCharge(charging);
            }

            if (wanted != null && Time.time >= _chargeLockedUntil)
            {
                wanted.Begin(Time.time);
                _instrument.HoldWithoutLimit(note);
                _instrument.Gain = _blowSettings.ChargeStartVolume;
            }
        }

        // Running (A or Down arrow) or calling (X or Left arrow) stops any charge, and none can start while either is held.
        private bool IsChargeInterrupted => BlowCharge.IsInterrupted(
            Permissions.CanRun && Permissions.CanMove && _runAction.IsPressed(),
            Permissions.CanCall && _notes.IsHeld(Note.Root));

        // The charge a note starts now, if the player may use it.
        private BlowCharge ChargeStartedBy(Note note)
        {
            if (BlowCharge.Charges(note))
            {
                return Permissions.CanBlow ? _blow : null;
            }

            if (EarwormRules.Charges(note))
            {
                return CanEarworm(Permissions) ? _earworm : null;
            }

            return null;
        }

        private void HandleNoteReleased(Note note)
        {
            BlowCharge charge = BlowCharge.Charges(note) ? _blow : EarwormRules.Charges(note) ? _earworm : null;

            if (charge == null || !charge.IsCharging)
            {
                return;
            }

            bool fires = charge.Release(Time.time);
            _blowBar.Hide();
            _instrument.HoldWithoutLimit(null);
            _instrument.Gain = 1f;

            if (!fires)
            {
                return;
            }

            _lastFired = charge;

            // No new charge until the picture is done (the chord may ring on longer).
            float sectorSeconds = _blowSettings.SectorFadeInSeconds + _blowSettings.SectorHoldSeconds + _blowSettings.SectorFadeOutSeconds;
            _chargeLockedUntil = Time.time + BlowCharge.LockoutSeconds(_blowSettings.ChordSeconds, _blowSettings.ArcSeconds, sectorSeconds);

            // Both play the same chord, on Left, so they sound the same.
            _instrument.PlayChord(Note.Left, _blowSettings.ChordSemitones, _blowSettings.ChordSeconds, _blowSettings.ChordVolume);
            Vector2 aim = _movement.AimDirection;

            if (charge == _blow)
            {
                BlowAway(aim);
            }
            else
            {
                PlayEarworm(aim);
            }

            Blew?.Invoke(aim);
        }

        /// <summary>Where the blow's area starts: the middle of the player's body.</summary>
        public Vector2 BlowOrigin => (Vector2)transform.position + (_blowSettings != null ? _blowSettings.EffectOffset : Vector2.zero);

        /// <summary>The way the player aims now (or faces, before Play Mode).</summary>
        public Vector2 AimDirection => _movement != null ? _movement.AimDirection : Vector2.right;

        // Pushes every vermin and other player whose body touches the blow's area, along the aim.
        private void BlowAway(Vector2 aim)
        {
            // The blow's area is a ring sector from the middle of the body, the same shape the sector shows.
            Vector2 origin = BlowOrigin;
            float leeway = _blowSettings.HitLeeway;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = _blowSettings.PushLayers, useTriggers = false };
            Physics2D.OverlapCircle(origin, _blowSettings.Reach + leeway, filter, _blowHits);
            _blowBodies.Clear();

            foreach (Collider2D hit in _blowHits)
            {
                Rigidbody2D body = hit.attachedRigidbody;

                if (body == null || body.gameObject == gameObject || !_blowBodies.Add(body))
                {
                    continue;
                }

                // Judged at the middle of the target's body (not its feet), with some leeway, so what looks
                // inside the sector is hit.
                Vector2 target = hit.bounds.center;

                if (!BlowCone.Overlaps(origin, aim, target, leeway, _blowSettings.ConeAngle, _blowSettings.InnerRadius, _blowSettings.Reach))
                {
                    continue;
                }

                if (body.TryGetComponent(out Vermin vermin))
                {
                    Vector2 push = BlowCone.Push(origin, aim, target, _blowSettings.Reach, _blowSettings.VerminPushSpeed, _blowSettings.EdgeStrength);
                    vermin.Blow(push, _blowSettings.PushSeconds);
                }
                else if (body.TryGetComponent(out PlayerMovement other))
                {
                    Vector2 push = BlowCone.Push(origin, aim, target, _blowSettings.Reach, _blowSettings.PlayerPushSpeed, _blowSettings.EdgeStrength);
                    other.Push(push, _blowSettings.PushSeconds, _blowSettings.PushedPlayerControl);
                }
            }
        }

        // Every free vermin whose body touches the area (the blow's ring sector) follows this player for a while.
        private void PlayEarworm(Vector2 aim)
        {
            Vector2 origin = BlowOrigin;
            float leeway = _blowSettings.HitLeeway;
            Physics2D.OverlapCircle(origin, _blowSettings.Reach + leeway, _filter, _blowHits);
            _blowBodies.Clear();

            foreach (Collider2D hit in _blowHits)
            {
                Rigidbody2D body = hit.attachedRigidbody;

                if (body == null || !_blowBodies.Add(body) || !body.TryGetComponent(out Vermin vermin))
                {
                    continue;
                }

                if (BlowCone.Overlaps(origin, aim, hit.bounds.center, leeway, _blowSettings.ConeAngle, _blowSettings.InnerRadius, _blowSettings.Reach))
                {
                    vermin.Earworm(_identity);
                }
            }
        }

        private void CancelCharge(BlowCharge charge)
        {
            if (charge != null && charge.IsCharging)
            {
                charge.Cancel();
                _blowBar.Hide();
                _instrument.HoldWithoutLimit(null);
                _instrument.Gain = 1f;
            }
        }

        private void Update()
        {
            if (ChargingNow != null && IsChargeInterrupted)
            {
                CancelCharge(ChargingNow);
            }

            if (ChargingNow != null)
            {
                float progress = ChargingNow.Progress(Time.time);
                _blowBar.Show(progress, EffectColour);
                _instrument.Gain = BlowSound.ChargeGain(progress, _blowSettings.ChargeStartVolume, _blowSettings.ChargeEndVolume);
            }

            bool runHeld = Permissions.CanRun && Permissions.CanMove && _runAction.IsPressed();
            _run.Tick(runHeld, Time.deltaTime);
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

            if (_blowSettings != null)
            {
                // The blow's real area: the ring sector, and (fainter) how far the leeway lets a target's middle be.
                Vector2 aim = AimDirection;
                DrawSectorGizmo(BlowOrigin, aim, _blowSettings.ConeAngle, _blowSettings.InnerRadius, _blowSettings.Reach, Color.magenta);

                if (_blowSettings.HitLeeway > 0f)
                {
                    DrawSectorGizmo(BlowOrigin, aim, _blowSettings.ConeAngle, Mathf.Max(0f, _blowSettings.InnerRadius - _blowSettings.HitLeeway),
                        _blowSettings.Reach + _blowSettings.HitLeeway, new Color(1f, 0f, 1f, 0.35f));
                }
            }
        }

        private static void DrawSectorGizmo(Vector2 origin, Vector2 aim, float angle, float inner, float reach, Color colour)
        {
            const int steps = 24;
            Gizmos.color = colour;
            Vector2 forward = aim.sqrMagnitude > 1e-6f ? aim.normalized : Vector2.right;
            Vector3 previousInner = default, previousOuter = default;

            for (int i = 0; i <= steps; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, 0f, Mathf.Lerp(-angle * 0.5f, angle * 0.5f, i / (float)steps)) * forward;
                Vector3 innerPoint = (Vector3)origin + direction * inner;
                Vector3 outerPoint = (Vector3)origin + direction * reach;

                if (i == 0 || i == steps)
                {
                    Gizmos.DrawLine(innerPoint, outerPoint);
                }

                if (i > 0)
                {
                    Gizmos.DrawLine(previousInner, innerPoint);
                    Gizmos.DrawLine(previousOuter, outerPoint);
                }

                previousInner = innerPoint;
                previousOuter = outerPoint;
            }
        }
    }
}
