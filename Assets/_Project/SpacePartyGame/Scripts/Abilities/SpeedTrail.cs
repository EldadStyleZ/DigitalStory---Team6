using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Small paint dabs in the player's colour, dropped behind the player while they run faster than walking. The
    /// faster the run, the more dabs; each drifts backwards, stretches a little with its motion, shrinks and fades.
    /// </summary>
    /// <remarks>The particles live in world space, so they stay where they were dropped and leave a short trail.</remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class SpeedTrail : MonoBehaviour
    {
        [SerializeField, Tooltip("Whose colour the dabs take.")]
        private PlayerIdentity _identity;

        [SerializeField, Min(0f), Tooltip("Dabs per second at full run speed.")]
        private float _dabsPerSecondAtFullRun = 45f;

        [SerializeField, Min(0f), Tooltip("Below this speed, in units per second, the player is standing still and drops nothing.")]
        private float _minMovingSpeed = 0.5f;

        [SerializeField, Tooltip("How far behind the feet the dabs appear, in units.")]
        private float _behindDistance = 0.35f;

        [SerializeField, Min(0f), Tooltip("How widely the dabs scatter around that spot, in units.")]
        private float _scatter = 0.15f;

        [SerializeField, Range(0f, 1f), Tooltip("How much a dab may drift to the side as well as backwards (0 straight back).")]
        private float _sidewaysSpread = 0.25f;

        [SerializeField, Tooltip("The slowest and fastest a dab drifts backwards, in units per second.")]
        private Vector2 _driftSpeed = new Vector2(0.4f, 1f);

        [SerializeField, Tooltip("The smallest and largest dab, in units.")]
        private Vector2 _size = new Vector2(0.1f, 0.2f);

        [SerializeField, Tooltip("The shortest and longest a dab lasts, in seconds.")]
        private Vector2 _lifetime = new Vector2(0.3f, 0.5f);

        [SerializeField, Range(0f, 1f), Tooltip("How solid a new dab is (1 is fully solid).")]
        private float _alpha = 0.85f;

        private ParticleSystem _particles;
        private float _boost;
        private Vector2 _velocity;
        private float _owed;

        /// <summary>How many dabs this trail has dropped, for tests.</summary>
        public int Dropped { get; private set; }

        /// <summary>Tells the trail how hard the player is running (0 walking, 1 full run) and how they move.</summary>
        public void Show(float boost, Vector2 velocity)
        {
            _boost = Mathf.Clamp01(boost);
            _velocity = velocity;
        }

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();

            if (_identity == null)
            {
                Debug.LogError($"{name}: SpeedTrail is missing its player identity.", this);
            }
        }

        private void Update()
        {
            float speed = _velocity.magnitude;

            if (_boost <= 0f || speed < _minMovingSpeed)
            {
                _owed = 0f;
                return;
            }

            _owed += _boost * _dabsPerSecondAtFullRun * Time.deltaTime;

            while (_owed >= 1f)
            {
                _owed -= 1f;
                Drop(_velocity / speed);
            }
        }

        private void Drop(Vector2 direction)
        {
            Vector2 behind = (Vector2)transform.position - (direction * _behindDistance) + (Random.insideUnitCircle * _scatter);
            Vector2 sideways = new Vector2(-direction.y, direction.x) * Random.Range(-_sidewaysSpread, _sidewaysSpread);
            Color colour = _identity.Colour;
            colour.a = _alpha;

            var dab = new ParticleSystem.EmitParams
            {
                position = behind,
                velocity = (-direction + sideways) * Random.Range(_driftSpeed.x, _driftSpeed.y),
                startSize = Random.Range(_size.x, _size.y),
                startLifetime = Random.Range(_lifetime.x, _lifetime.y),
                startColor = colour,
                rotation = Random.Range(0f, 360f),
                applyShapeToPosition = false,
            };

            _particles.Emit(dab, 1);
            Dropped++;
        }
    }
}
