using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// What a blow looks like: a burst of watercolour dabs streaking out along the cone in the player's colour, and
    /// a wide painted arc sweeping out to the edge of the reach and fading. It plays when
    /// <see cref="PlayerAbilities.Blew"/> fires; every size and time is in <see cref="BlowSettings"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlowEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("The player whose blow this shows.")]
        private PlayerAbilities _abilities;

        [SerializeField, Tooltip("Whose colour the effect takes.")]
        private PlayerIdentity _identity;

        [SerializeField, Tooltip("The blow's numbers.")]
        private BlowSettings _settings;

        [SerializeField, Tooltip("The particle system that throws the dabs (world space, no emission of its own).")]
        private ParticleSystem _burst;

        [SerializeField, Tooltip("The painted arc; its sprite's pivot is the cone's tip and it points right.")]
        private SpriteRenderer _arc;

        private float _arcStartedAt = float.NegativeInfinity;
        private Color _arcColour;

        /// <summary>How many dabs the last blow threw, for checking.</summary>
        public int LastDabCount { get; private set; }

        /// <summary>Whether the arc is on screen now.</summary>
        public bool IsArcShowing => _arc != null && _arc.enabled;

        private void Awake()
        {
            if (_abilities == null || _identity == null || _settings == null || _burst == null || _arc == null)
            {
                Debug.LogError($"{name}: BlowEffect is missing a reference.", this);
                return;
            }

            _arc.enabled = false;
        }

        private void OnEnable()
        {
            if (_abilities != null)
            {
                _abilities.Blew += Play;
            }
        }

        private void OnDisable()
        {
            if (_abilities != null)
            {
                _abilities.Blew -= Play;
            }

            if (_arc != null)
            {
                _arc.enabled = false;
            }
        }

        /// <summary>Plays the effect toward <paramref name="aim"/>.</summary>
        public void Play(Vector2 aim)
        {
            if (_settings == null || _burst == null || _arc == null)
            {
                return;
            }

            Vector2 direction = aim.sqrMagnitude > 1e-6f ? aim.normalized : Vector2.right;
            // Starts in front of the player, not inside them.
            // Vector2 origin = (Vector2)_abilities.transform.position + _settings.EffectOffset;
            Vector2 origin = (Vector2)_abilities.transform.position + _settings.EffectOffset + direction * _settings.EffectForward;
            ThrowDabs(origin, direction);
            StartArc(origin, direction);
        }

        private void ThrowDabs(Vector2 origin, Vector2 direction)
        {
            float half = _settings.ConeAngle * 0.5f;
            Color colour = _identity.Colour;
            int count = _settings.DabCount;

            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(-half, half);
                Vector2 heading = Quaternion.Euler(0f, 0f, angle) * direction;
                float lifetime = Random.Range(_settings.DabLifetime.x, _settings.DabLifetime.y);

                // Fast enough to reach the edge of the blow (some a little short, some a little past) in its lifetime.
                float travel = _settings.EffectReach * Random.Range(_settings.DabTravel.x, _settings.DabTravel.y);
                Color dabColour = Color.Lerp(colour, Color.white, Random.Range(0f, _settings.DabWhiteMix));
                dabColour.a = _settings.DabAlpha;

                var dab = new ParticleSystem.EmitParams
                {
                    position = origin + Random.insideUnitCircle * _settings.DabScatter,
                    velocity = heading * (travel / lifetime),
                    startSize = Random.Range(_settings.DabSize.x, _settings.DabSize.y),
                    startLifetime = lifetime,
                    startColor = dabColour,
                    rotation = Random.Range(0f, 360f),
                };

                _burst.Emit(dab, 1);
            }

            LastDabCount = count;
        }

        private void StartArc(Vector2 origin, Vector2 direction)
        {
            _arc.transform.position = new Vector3(origin.x, origin.y, _arc.transform.position.z);
            _arc.transform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, direction));
            _arcColour = _identity.Colour;
            _arcStartedAt = Time.time;
            _arc.enabled = true;
            DrawArc(0f);
        }

        private void Update()
        {
            if (_arc == null || !_arc.enabled)
            {
                return;
            }

            float t = (Time.time - _arcStartedAt) / Mathf.Max(0.01f, _settings.ArcSeconds);

            if (t >= 1f)
            {
                _arc.enabled = false;
                return;
            }

            DrawArc(t);
        }

        // The arc grows fast then slows (ease out) to the full reach, fading as it goes.
        private void DrawArc(float t)
        {
            float grow = 1f - (1f - t) * (1f - t);
            float radius = Mathf.Lerp(_settings.ArcStartFraction, 1f, grow) * _settings.EffectReach;
            // The arc picture is drawn for a 90° cone; other cone angles widen or narrow it to match.
            float widen = Mathf.Tan(Mathf.Clamp(_settings.ConeAngle, 10f, 170f) * 0.5f * Mathf.Deg2Rad);
            _arc.transform.localScale = new Vector3(radius, radius * widen, 1f);
            Color colour = _arcColour;
            colour.a = _settings.ArcAlpha * (1f - t * t);
            _arc.color = colour;
        }
    }
}
