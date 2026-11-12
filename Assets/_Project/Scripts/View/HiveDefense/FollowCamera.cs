using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>A three-quarter view from above that follows a target with smoothing.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Follow Camera")]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField, Range(10f, 80f), Tooltip("How far the camera looks down, in degrees.")]
        private float _pitchDegrees = 55f;

        [SerializeField, Tooltip("The compass heading of the view, in degrees. 0 looks along +Z.")]
        private float _yawDegrees;

        [SerializeField, Min(1f), Tooltip("How far back the camera sits from the point it looks at, in metres.")]
        private float _distance = 14f;

        [SerializeField, Min(0f), Tooltip("How high above the target's feet the camera looks, in metres.")]
        private float _focusHeight = 1f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the camera takes to catch up with the target.")]
        private float _followSmoothSeconds = 0.25f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the camera takes to blend to another distance.")]
        private float _distanceSmoothSeconds = 0.6f;

        [Header("Shake and kick")]
        [SerializeField, Min(0f), Tooltip("The most a shake moves the camera, in metres, however hard the hit.")]
        private float _maxShakeMetres = 0.6f;

        [SerializeField, Min(1f), Tooltip("How fast a shake wobbles, in cycles per second.")]
        private float _shakeHertz = 28f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a shake takes to die away.")]
        private float _shakeSeconds = 0.25f;

        [SerializeField, Min(0f), Tooltip("The most a kick pushes the camera along its view, in metres.")]
        private float _maxKickMetres = 0.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a kick takes to settle.")]
        private float _kickSeconds = 0.18f;

        private Transform _target;
        private float _shake;
        private float _kick;
        private float _shakeClock;
        private Vector3 _offset;
        private Vector3 _focus;
        private Vector3 _focusVelocity;
        private float _wantedDistance;
        private float _currentDistance;
        private float _distanceVelocity;

        /// <summary>The distance the camera returns to after SetDistance.</summary>
        public float DefaultDistance => _distance;

        /// <summary>What the camera follows, or null before <see cref="SetTarget"/>.</summary>
        public Transform Target => _target;

        /// <summary>The distance the camera is blending to: the default, or the last one SetDistance asked for.</summary>
        public float WantedDistance => _wantedDistance;

        /// <summary>The shake's current strength, 0 to 1, falling to 0 as it dies away. A test reads it to see a shake happened.</summary>
        public float ShakeStrength => _shake;

        /// <summary>The kick's current strength, 0 to 1, falling to 0 as it settles.</summary>
        public float KickStrength => _kick;

        /// <summary>The offset the shake and the kick add to the followed position this frame, in metres.</summary>
        public Vector3 CurrentOffset => _offset;

        /// <summary>
        /// Shakes the camera: strength 0 to 1 (more is clamped). A new shake never lowers one still running, so several hits in a frame
        /// give one shake of the hardest.
        /// </summary>
        public void Shake(float strength)
        {
            _shake = Mathf.Max(_shake, Mathf.Clamp01(strength));
        }

        /// <summary>Kicks the camera forward along its view and lets it settle back: strength 0 to 1. Used the frame a song is cast.</summary>
        public void Kick(float strength)
        {
            _kick = Mathf.Max(_kick, Mathf.Clamp01(strength));
        }

        /// <summary>Chooses what to follow and jumps to it at once.</summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            _focusVelocity = Vector3.zero;

            if (target != null)
            {
                _focus = FocusOf(target);
                Apply();
            }
        }

        /// <summary>Blends to another distance, for example the Colossus's camera distance.</summary>
        public void SetDistance(float distance)
        {
            _wantedDistance = Mathf.Max(1f, distance);
        }

        /// <summary>Blends back to the default distance.</summary>
        public void ResetDistance()
        {
            _wantedDistance = _distance;
        }

        private void Awake()
        {
            _wantedDistance = _distance;
            _currentDistance = _distance;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            StepOffset(deltaTime);
            _focus = Vector3.SmoothDamp(_focus, FocusOf(_target), ref _focusVelocity, _followSmoothSeconds, Mathf.Infinity, deltaTime);
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _wantedDistance, ref _distanceVelocity, _distanceSmoothSeconds, Mathf.Infinity, deltaTime);
            Apply();
        }

        // The shake and the kick are only an offset added when the camera is placed, never to the followed focus, so they cannot fight it.
        private void StepOffset(float deltaTime)
        {
            _shake = Mathf.Max(0f, _shake - (deltaTime / _shakeSeconds));
            _kick = Mathf.Max(0f, _kick - (deltaTime / _kickSeconds));
            _shakeClock += deltaTime * _shakeHertz;

            Quaternion rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            float amount = _shake * _shake * _maxShakeMetres;
            float x = Mathf.Sin(_shakeClock * 2.1f) * amount;
            float y = Mathf.Sin((_shakeClock * 1.7f) + 1.3f) * amount;
            float push = _kick * _kick * _maxKickMetres;
            _offset = (rotation * new Vector3(x, y, push));
        }

        private Vector3 FocusOf(Transform target)
        {
            return target.position + (Vector3.up * _focusHeight);
        }

        private void Apply()
        {
            Quaternion rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            transform.SetPositionAndRotation(_focus - (rotation * Vector3.forward * _currentDistance) + _offset, rotation);
        }
    }
}
