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

        private Transform _target;
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
            _focus = Vector3.SmoothDamp(_focus, FocusOf(_target), ref _focusVelocity, _followSmoothSeconds, Mathf.Infinity, deltaTime);
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _wantedDistance, ref _distanceVelocity, _distanceSmoothSeconds, Mathf.Infinity, deltaTime);
            Apply();
        }

        private Vector3 FocusOf(Transform target)
        {
            return target.position + (Vector3.up * _focusHeight);
        }

        private void Apply()
        {
            Quaternion rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            transform.SetPositionAndRotation(_focus - (rotation * Vector3.forward * _currentDistance), rotation);
        }
    }
}
