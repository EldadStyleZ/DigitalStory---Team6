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

        [SerializeField, Min(0f), Tooltip("How close it walks behind the rat in front of it, in the conga line.")]
        private float _congaGap = 0.7f;

        [Header("Listening")]
        [SerializeField, Min(0f), Tooltip("How long it stays charmed after the music stops, in seconds.")]
        private float _charmSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("How long it keeps running after a scare, in seconds.")]
        private float _fleeSeconds = 1.5f;

        [SerializeField, Min(0f), Tooltip("How long it ignores the flute after a song, so the song's last note cannot undo it, in seconds.")]
        private float _afterSongSeconds = 0.5f;

        [Header("Songs")]
        [SerializeField, Min(0f), Tooltip("How fast the wind carries a floating rat.")]
        private float _windSpeed = 1.3f;

        [SerializeField, Min(0f), Tooltip("How fast a floating rat bobs up and down.")]
        private float _floatBobSpeed = 0.6f;

        [SerializeField, Min(0f), Tooltip("How fast a disco rat spins along its circle.")]
        private float _spinSpeed = 2.5f;

        [SerializeField, Min(0f), Tooltip("How many circles a disco rat spins each second.")]
        private float _spinsPerSecond = 1.2f;

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
        private float _moodSecondsLeft;
        private Vector2 _wanderDirection;
        private float _wanderSecondsLeft;
        private Transform _leader;
        private Vector2 _heartSpot;
        private Color _songColor;
        private float _phase;
        private float _ignoreTheFluteUntil;

        public RatMood Mood { get; private set; } = RatMood.Wander;

        /// <summary>True while a song is in charge of the rat, so the flute's holes are ignored.</summary>
        public bool IsInASong => Mood >= RatMood.Conga;

        private Vector2 Position => _rigidbody.position;

        private Vector2 PiperPosition => _piper.transform.position;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _phase = Random.value * FullTurn;
        }

        /// <summary>Tells the rat which flute to listen to. The RatHerd calls it for every rat.</summary>
        public void Listen(Flute flute)
        {
            _flute = flute;
            _piper = flute.GetComponent<PiperMovement>();
        }

        /// <summary>Dance and Crowd Surf: charmed, it follows the Piper.</summary>
        public void Charm(float seconds)
        {
            EndSong();
            Become(RatMood.Follow, seconds);
            _ignoreTheFluteUntil = Time.time + _afterSongSeconds;
        }

        /// <summary>Conga: walks behind its leader, which is the Piper or the rat in front.</summary>
        public void JoinConga(Transform leader, float seconds, Color color)
        {
            StartSong(RatMood.Conga, seconds, color);
            _leader = leader;
        }

        /// <summary>Heart: stands at its spot, measured from the Piper.</summary>
        public void JoinHeart(Vector2 spot, float seconds, Color color)
        {
            StartSong(RatMood.Heart, seconds, color);
            _heartSpot = spot;
        }

        public void StartDisco(float seconds, Color color) => StartSong(RatMood.Disco, seconds, color);

        /// <summary>Balloons: it floats over the other rats, so it stops bumping into them.</summary>
        public void StartFloating(float seconds, Color color)
        {
            StartSong(RatMood.Float, seconds, color);
            _collider.enabled = false;
        }

        public void StartSinging(float seconds, Color color)
        {
            StartSong(RatMood.Sing, seconds, color);
            _songNotes.Play();
        }

        public void Faint(float seconds, Color color) => StartSong(RatMood.Faint, seconds, color);

        private void Update()
        {
            _moodSecondsLeft -= Time.deltaTime;

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

        private void Become(RatMood mood, float seconds)
        {
            Mood = mood;
            _moodSecondsLeft = seconds;
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
            _collider.enabled = true;
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
                    return Toward(_leader.position, _congaGap);
                case RatMood.Heart:
                    return Toward(PiperPosition + _heartSpot, Slowest);
                case RatMood.Disco:
                    return Spin();
                case RatMood.Float:
                    return new Vector2(_windSpeed, Mathf.Sin((Time.time * 2f) + _phase) * _floatBobSpeed);
                case RatMood.Faint:
                    return _rigidbody.linearVelocity * 0.9f;
                default:
                    return Wander();
            }
        }

        // Slows down as it arrives, so it settles instead of overshooting and turning back.
        private Vector2 Toward(Vector2 target, float stopDistance)
        {
            Vector2 offset = target - Position;
            float distance = offset.magnitude;

            if (distance <= stopDistance)
            {
                return Vector2.zero;
            }

            float speed = Mathf.Min(_runSpeed, (distance - stopDistance) * 4f);
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

        private Vector2 Spin()
        {
            float angle = (Time.time * _spinsPerSecond * FullTurn) + _phase;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spinSpeed;
        }

        private void ShowMood()
        {
            Vector2 velocity = _rigidbody.linearVelocity;
            _animator.SetFloat(SpeedParameter, velocity.magnitude);
            _sprite.color = ColorOf(Mood);
            _sprite.sortingOrder = Mood == RatMood.Float ? 6 : 3;

            if (Mood == RatMood.Disco)
            {
                _body.localRotation = Quaternion.Euler(0f, 0f, Time.time * 720f);
            }
            else if (velocity.sqrMagnitude > Slowest && Mood != RatMood.Faint)
            {
                _body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            }

            float size = Mood == RatMood.Float ? _floatSize : 1f;
            _body.localScale = new Vector3(size, Mood == RatMood.Faint ? -size : size, 1f);

            float hop = Mood == RatMood.Sing ? Mathf.Abs(Mathf.Sin((Time.time * 8f) + _phase)) * _hopHeight : 0f;
            _body.localPosition = new Vector3(0f, hop, 0f);
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
                case RatMood.Disco:
                    return Color.HSVToRGB(Mathf.Repeat((Time.time * 1.5f) + _phase, 1f), 0.6f, 1f);
                case RatMood.Faint:
                    return _faintColor;
                default:
                    return _songColor;
            }
        }
    }
}
