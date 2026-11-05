using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// One rat. It wanders until it hears the flute: holes 1 and 2 make it follow the Piper, 3 and 4 march it ahead
    /// and 5 and 6 scare it away. A song gives it a mood of its own for a few seconds, which the flute's holes cannot
    /// interrupt.
    /// </summary>
    /// <remarks>
    /// Update decides what the rat feels and how it looks; FixedUpdate moves it, because the rat moves with physics.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Rat : MonoBehaviour
    {
        private const float FullTurn = 2f * Mathf.PI;
        private const float Slowest = 0.05f;

        // Getting closer to its place by less than this does not count, so jostling on the spot is not progress.
        private const float CloserStep = 0.05f;

        // Turning the parameter's name into a number once is quicker than looking up the name every frame.
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        [Header("Moving")]
        [SerializeField, Min(0f), Tooltip("How fast it strolls on its own, in units per second.")]
        private float _wanderSpeed = 0.7f;

        [SerializeField, Min(0f), Tooltip("How fast it runs after or away from the Piper, in units per second.")]
        private float _runSpeed = 3.6f;

        [SerializeField, Min(0f), Tooltip("How close it walks up to the Piper.")]
        private float _followDistance = 1.1f;

        [SerializeField, Min(0f), Tooltip("How far ahead of the Piper it marches.")]
        private float _marchDistance = 3f;

        [SerializeField, Min(0f), Tooltip("How fast it runs to its place in the conga line or the heart. Faster than the Piper walks, so the line keeps up.")]
        private float _formationSpeed = 6f;

        [SerializeField, Min(0f), Tooltip("How close to its place in the conga line or the heart it must be to count as in place.")]
        private float _inPlaceDistance = 0.4f;

        [SerializeField, Min(0f), Tooltip("How long it may go without getting any closer to its place in the conga line or the heart before the others stop waiting for it, in seconds.")]
        private float _stuckSeconds = 1f;

        [Header("Listening")]
        [SerializeField, Min(0f), Tooltip("How long it stays charmed after the music stops, in seconds.")]
        private float _charmSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("How long it keeps running after a scare, in seconds.")]
        private float _fleeSeconds = 1.5f;

        [SerializeField, Min(0f), Tooltip("How long it ignores the flute after a song, so the song's last note cannot undo it, in seconds.")]
        private float _afterSongSeconds = 0.5f;

        [Header("Songs")]
        [SerializeField, Min(0f), Tooltip("How fast a balloon rat rises up the screen.")]
        private float _floatRiseSpeed = 0.45f;

        [SerializeField, Min(0f), Tooltip("How fast the breeze pushes a balloon rat to the right.")]
        private float _floatDriftSpeed = 0.15f;

        [SerializeField, Min(0f), Tooltip("How fast a balloon rat sways from side to side.")]
        private float _floatSwaySpeed = 0.1f;

        [SerializeField, Min(0f), Tooltip("How strongly a rat in the Crowd Surf crowd closes the gap to its spot under the Piper.")]
        private float _carryCatchUp = 6f;

        [Header("Disco")]
        [SerializeField, Min(0f), Tooltip("How many times a disco rat turns round on the spot each second.")]
        private float _discoTurnsPerSecond = 1.5f;

        [SerializeField, Tooltip("The soft light under the rat during Disco. It does not turn with the body.")]
        private SpriteRenderer _discoLight;

        [SerializeField, Tooltip("The colours the disco light steps through, one each beat: the flute's hole colours.")]
        private Color[] _discoColors =
        {
            new Color32(0xF2, 0xB5, 0x44, 0xFF),
            new Color32(0xF2, 0x9A, 0x44, 0xFF),
            new Color32(0x8F, 0xD3, 0x6A, 0xFF),
            new Color32(0x5F, 0xC9, 0xA8, 0xFF),
            new Color32(0x8F, 0xB8, 0xFF, 0xFF),
            new Color32(0xD9, 0xD2, 0xFF, 0xFF),
        };

        [SerializeField, Min(0.05f), Tooltip("How long the disco light holds each colour, in seconds.")]
        private float _discoBeatSeconds = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("How strong the disco light is.")]
        private float _discoLightStrength = 0.85f;

        [SerializeField, Range(0f, 1f), Tooltip("How much of the light's colour the rat itself takes on.")]
        private float _discoTint = 0.35f;

        [SerializeField, Min(0f), Tooltip("How much the light swells on each beat, as a share of its size.")]
        private float _discoPulse = 0.15f;

        [Header("Look")]
        [SerializeField, Tooltip("The child that turns, swells and hops. The Animator waddles its Sprite child.")]
        private Transform _body;

        [SerializeField]
        private SpriteRenderer _sprite;

        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip("The notes that float up while the rat sings.")]
        private ParticleSystem _songNotes;

        [SerializeField]
        private Color _calmColor = new Color(0.65f, 0.56f, 0.5f);

        [SerializeField]
        private Color _charmedColor = new Color(1f, 0.77f, 0.32f);

        [SerializeField]
        private Color _faintColor = new Color(0.55f, 0.55f, 0.6f);

        [SerializeField, Min(1f), Tooltip("How big a floating rat swells.")]
        private float _floatSize = 1.7f;

        [SerializeField, Min(0f), Tooltip("How high a singing rat hops.")]
        private float _hopHeight = 0.15f;

        [SerializeField, Tooltip("The sound when a floating rat pops.")]
        private AudioClip _popSound;

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private Flute _flute;
        private PiperMovement _piper;
        private Vector2 _crowdSpot;
        private float _moodSecondsLeft;
        private Vector2 _wanderDirection;
        private float _wanderSecondsLeft;
        private CongaLine _congaLine;
        private float _congaDistance;
        private bool _waitsForTheOthers;
        private float _closestToItsPlace;
        private float _lastGotCloser;
        private Vector2 _heartSpot;
        private Color _songColor;
        private float _phase;
        private float _ignoreTheFluteUntil;
        private int _discoFirstColor;
        private float _spinDirection;
        private float _discoShown;
        private Vector3 _discoLightSize;

        public RatMood Mood { get; private set; } = RatMood.Wander;

        /// <summary>True while a song is in charge of the rat, so the flute's holes are ignored.</summary>
        public bool IsInASong => Mood >= RatMood.Conga;

        /// <summary>
        /// True while it is in the conga line or the heart and the others are still getting into place. Its song's
        /// seconds do not count down until the RatHerd calls <see cref="StartTheClock"/>.
        /// </summary>
        public bool IsWaitingForTheOthers => _waitsForTheOthers;

        /// <summary>True when it stands at its place in the conga line, the heart or the Crowd Surf crowd.</summary>
        public bool IsInPlace => Vector2.Distance(Position, FormationSpot()) <= _inPlaceDistance;

        /// <summary>
        /// True when it has not got any closer to its place for a while, for example because an obstacle stands on
        /// its place or between it and its place, so it is as close as it can get.
        /// </summary>
        public bool IsStuck => Time.time - _lastGotCloser >= _stuckSeconds;

        private bool IsInAFormation => Mood == RatMood.Conga || Mood == RatMood.Heart || Mood == RatMood.Carry;

        private Vector2 Position => _rigidbody.position;

        private Vector2 PiperPosition => _piper.transform.position;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _phase = Random.value * FullTurn;

            // Each rat starts the disco on its own colour and turns its own way, so neighbours do not match.
            _discoFirstColor = Random.Range(0, _discoColors.Length);
            _spinDirection = Random.value < 0.5f ? 1f : -1f;
            _discoLightSize = _discoLight.transform.localScale;
        }

        /// <summary>Tells the rat which flute to listen to. The RatHerd calls it for every rat.</summary>
        public void Listen(Flute flute)
        {
            _flute = flute;
            _piper = flute.GetComponent<PiperMovement>();

            // The Piper walks through the rats instead of shoving them; the rats still bump into each other.
            Physics2D.IgnoreCollision(_collider, flute.GetComponent<Collider2D>());
        }

        /// <summary>Dance: charmed, it follows the Piper.</summary>
        public void Charm(float seconds)
        {
            EndSong();
            Become(RatMood.Follow, seconds);
            _ignoreTheFluteUntil = Time.time + _afterSongSeconds;
        }

        /// <summary>
        /// Conga: runs at once to its place on the line, this far back along the Piper's footsteps, and then keeps
        /// to it, following the rat in front wherever the Piper goes. It waits for the others before dancing.
        /// </summary>
        public void JoinConga(CongaLine line, float distanceBehind, float seconds, Color color)
        {
            StartSong(RatMood.Conga, seconds, color);
            _congaLine = line;
            _congaDistance = distanceBehind;
            WaitForTheOthers();
        }

        /// <summary>Heart: stands at its spot, measured from the Piper. It waits for the others before dancing.</summary>
        public void JoinHeart(Vector2 spot, float seconds, Color color)
        {
            StartSong(RatMood.Heart, seconds, color);
            _heartSpot = spot;
            WaitForTheOthers();
        }

        /// <summary>Everyone in its formation is in place, so its song's seconds start counting.</summary>
        public void StartTheClock() => _waitsForTheOthers = false;

        private void WaitForTheOthers()
        {
            _waitsForTheOthers = true;
            StartHeadingForItsPlace();
        }

        private void StartHeadingForItsPlace()
        {
            _closestToItsPlace = float.MaxValue;
            _lastGotCloser = Time.time;
        }

        public void StartDisco(float seconds, Color color) => StartSong(RatMood.Disco, seconds, color);

        /// <summary>Balloons: it floats slowly up the screen. It still bumps into the edge, so it stays on the field.</summary>
        public void StartFloating(float seconds, Color color) => StartSong(RatMood.Float, seconds, color);

        public void StartSinging(float seconds, Color color)
        {
            StartSong(RatMood.Sing, seconds, color);
            _songNotes.Play();
        }

        public void Faint(float seconds, Color color) => StartSong(RatMood.Faint, seconds, color);

        /// <summary>
        /// Crowd Surf: joins the crowd under the Piper, at its spot measured from the Piper. The song's seconds count
        /// from the start, as before; the RatHerd only watches for the crowd to be under the Piper.
        /// </summary>
        public void JoinCrowd(Vector2 spot, float seconds, Color color)
        {
            StartSong(RatMood.Carry, seconds, color);
            _crowdSpot = spot;
            StartHeadingForItsPlace();
        }

        private void Update()
        {
            if (IsInAFormation)
            {
                NoteHowCloseItIs();
            }

            if (!_waitsForTheOthers)
            {
                _moodSecondsLeft -= Time.deltaTime;
            }

            if (IsInASong && _moodSecondsLeft <= 0f)
            {
                EndSong();
            }

            if (!IsInASong && Time.time >= _ignoreTheFluteUntil)
            {
                ListenToTheFlute();
            }

            ShowMood();
        }

        private void FixedUpdate()
        {
            if (_piper == null)
            {
                return;
            }

            _rigidbody.linearVelocity = Velocity();
        }

        private void ListenToTheFlute()
        {
            bool hears = _flute != null && Vector2.Distance(Position, PiperPosition) <= _flute.HearingRadius;

            switch (hears ? _flute.Mode : FluteMode.Silent)
            {
                case FluteMode.Call:
                    Become(RatMood.Follow, _charmSeconds);
                    break;
                case FluteMode.March:
                    Become(RatMood.March, _charmSeconds);
                    break;
                case FluteMode.Scare:
                    Become(RatMood.Flee, _fleeSeconds);
                    break;
                default:
                    WhenTheMusicStops();
                    break;
            }
        }

        // A march needs the music, but the charm lasts a little longer, so the rat follows until it wears off.
        private void WhenTheMusicStops()
        {
            if (_moodSecondsLeft <= 0f)
            {
                Become(RatMood.Wander, 0f);
            }
            else if (Mood == RatMood.March)
            {
                Mood = RatMood.Follow;
            }
        }

        // Any change of mood takes the rat out of a formation it was waiting in.
        private void Become(RatMood mood, float seconds)
        {
            Mood = mood;
            _moodSecondsLeft = seconds;
            _waitsForTheOthers = false;
        }

        private void StartSong(RatMood mood, float seconds, Color color)
        {
            EndSong();
            Become(mood, seconds);
            _songColor = color;
        }

        // A popped balloon or a rat waking up goes back to its own business; the others stay charmed a while.
        private void EndSong()
        {
            if (!IsInASong)
            {
                return;
            }

            RatMood finished = Mood;
            _songNotes.Stop();

            if (finished == RatMood.Float && _popSound != null)
            {
                AudioSource.PlayClipAtPoint(_popSound, transform.position);
            }

            bool staysCharmed = finished != RatMood.Float && finished != RatMood.Faint;
            Become(staysCharmed ? RatMood.Follow : RatMood.Wander, staysCharmed ? _charmSeconds : 0f);
        }

        private Vector2 Velocity()
        {
            switch (Mood)
            {
                case RatMood.Follow:
                case RatMood.Sing:
                    return Toward(PiperPosition, _followDistance);
                case RatMood.March:
                    return Toward(PiperPosition + (_piper.Facing * _marchDistance), Slowest);
                case RatMood.Flee:
                    return (Position - PiperPosition).normalized * _runSpeed;
                case RatMood.Conga:
                case RatMood.Heart:
                    return Toward(FormationSpot(), Slowest, _formationSpeed);
                case RatMood.Disco:
                    return Vector2.zero;
                case RatMood.Float:
                    return Float();
                case RatMood.Carry:
                    return Carry();
                case RatMood.Faint:
                    return _rigidbody.linearVelocity * 0.9f;
                default:
                    return Wander();
            }
        }

        // Remembers when it last got closer to its place, so it can tell when it is stuck.
        private void NoteHowCloseItIs()
        {
            float distance = Vector2.Distance(Position, FormationSpot());

            if (distance < _closestToItsPlace - CloserStep)
            {
                _closestToItsPlace = distance;
                _lastGotCloser = Time.time;
            }
        }

        // Its place in the conga line, the heart or the Crowd Surf crowd, which moves with the Piper.
        private Vector2 FormationSpot()
        {
            switch (Mood)
            {
                case RatMood.Conga:
                    return _congaLine.SpotBehind(_congaDistance);
                case RatMood.Carry:
                    return PiperPosition + _crowdSpot;
                default:
                    return PiperPosition + _heartSpot;
            }
        }

        private Vector2 Toward(Vector2 target, float stopDistance) => Toward(target, stopDistance, _runSpeed);

        // Slows down as it arrives, so it settles instead of overshooting and turning back.
        private Vector2 Toward(Vector2 target, float stopDistance, float topSpeed)
        {
            Vector2 offset = target - Position;
            float distance = offset.magnitude;

            if (distance <= stopDistance)
            {
                return Vector2.zero;
            }

            float speed = Mathf.Min(topSpeed, (distance - stopDistance) * 4f);
            return offset / distance * speed;
        }

        private Vector2 Wander()
        {
            _wanderSecondsLeft -= Time.fixedDeltaTime;

            if (_wanderSecondsLeft <= 0f)
            {
                _wanderSecondsLeft = Random.Range(1f, 3f);
                _wanderDirection = Random.value < 0.3f ? Vector2.zero : Random.insideUnitCircle.normalized;
            }

            return _wanderDirection * _wanderSpeed;
        }

        // A balloon rises slowly up the screen and the breeze nudges it right; the edge of the field stops it.
        private Vector2 Float()
        {
            float sway = Mathf.Sin((Time.time * 2f) + _phase) * _floatSwaySpeed;
            return new Vector2(_floatDriftSpeed + sway, _floatRiseSpeed);
        }

        // The rat moves exactly as the Piper does, plus a pull back to its spot, so the whole crowd goes wherever
        // the player steers. Its top speed is the Piper's plus its own, so it keeps up when the Piper rides fast.
        private Vector2 Carry()
        {
            Vector2 spot = PiperPosition + _crowdSpot;
            Vector2 velocity = _piper.Velocity + ((spot - Position) * _carryCatchUp);
            return Vector2.ClampMagnitude(velocity, _piper.Velocity.magnitude + _runSpeed);
        }

        private void ShowMood()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            bool isFloating = Mood == RatMood.Float;
            Color discoColor = DiscoPalette.ColorAt(_discoColors, (Time.time / _discoBeatSeconds) + _discoFirstColor);

            // A balloon rat neither waddles nor turns: it just drifts.
            _animator.SetFloat(SpeedParameter, isFloating ? 0f : velocity.magnitude);
            _sprite.color = Mood == RatMood.Disco ? Color.Lerp(_calmColor, discoColor, _discoTint) : ColorOf(Mood);
            _sprite.sortingOrder = Mood == RatMood.Float ? 6 : 3;
            ShowTheDiscoLight(discoColor);

            // A disco rat turns its body round its own middle; the rat itself stays on its spot.
            if (Mood == RatMood.Disco)
            {
                _body.localRotation = Quaternion.Euler(0f, 0f, (Time.time * _discoTurnsPerSecond * 360f * _spinDirection) + (_phase * Mathf.Rad2Deg));
            }
            else if (velocity.sqrMagnitude > Slowest && Mood != RatMood.Faint && !isFloating)
            {
                _body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            }

            float size = Mood == RatMood.Float ? _floatSize : 1f;
            _body.localScale = new Vector3(size, Mood == RatMood.Faint ? -size : size, 1f);

            float hop = Mood == RatMood.Sing ? Mathf.Abs(Mathf.Sin((Time.time * 8f) + _phase)) * _hopHeight : 0f;
            _body.localPosition = new Vector3(0f, hop, 0f);
        }

        // The light fades in for the disco and out after it, and swells a little on each beat.
        private void ShowTheDiscoLight(Color color)
        {
            const float FadeSeconds = 0.25f;
            _discoShown = Mathf.MoveTowards(_discoShown, Mood == RatMood.Disco ? 1f : 0f, Time.deltaTime / FadeSeconds);

            float intoTheBeat = Mathf.Repeat(Time.time / _discoBeatSeconds, 1f);
            float swell = 1f + (_discoPulse * (1f - intoTheBeat) * (1f - intoTheBeat));
            _discoLight.transform.localScale = _discoLightSize * swell;
            _discoLight.color = new Color(color.r, color.g, color.b, _discoLightStrength * _discoShown);
        }

        private Color ColorOf(RatMood mood)
        {
            switch (mood)
            {
                case RatMood.Wander:
                case RatMood.Flee:
                    return _calmColor;
                case RatMood.Follow:
                case RatMood.March:
                    return _charmedColor;
                case RatMood.Faint:
                    return _faintColor;
                default:
                    return _songColor;
            }
        }
    }
}
