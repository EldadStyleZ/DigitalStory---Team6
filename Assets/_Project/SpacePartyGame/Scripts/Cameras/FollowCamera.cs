using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// A player's split-screen camera: it glides after its player and never shows past the edge of the map.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField, Min(0.5f), Tooltip("How much of the world it shows: half the view's height, in units.")]
        private float _zoom = 3f;

        [SerializeField, Min(0f), Tooltip("Roughly how many seconds it takes to catch up with the player.")]
        private float _smoothTime = 0.15f;

        [SerializeField, Tooltip("Keeps the view on the map.")]
        private MapBounds _map;

        private Camera _camera;
        private Transform _target;
        private Vector2 _velocity;

        /// <summary>The camera this script moves.</summary>
        public Camera Camera => _camera;

        /// <summary>The player being followed, or null.</summary>
        public Transform Target => _target;

        /// <summary>Starts following a player from the given part of the screen, jumping straight to it.</summary>
        public void Follow(Transform target, Rect viewport)
        {
            // Switch on first: a camera that starts switched off has not run Awake yet.
            gameObject.SetActive(true);
            _target = target;
            _camera.rect = viewport;
            _camera.orthographicSize = _zoom;
            _velocity = Vector2.zero;
            MoveTo(Framed(target.position));
        }

        /// <summary>Moves the camera's view to another part of the screen, keeping its zoom.</summary>
        public void SetViewport(Rect viewport)
        {
            _camera.rect = viewport;
        }

        /// <summary>Stops following and switches the camera off.</summary>
        public void Stop()
        {
            _target = null;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_map == null)
            {
                Debug.LogError($"{name}: FollowCamera has no MapBounds.", this);
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector2 goal = Framed(_target.position);
            MoveTo(Vector2.SmoothDamp(transform.position, goal, ref _velocity, _smoothTime));
        }

        private Vector2 Framed(Vector2 point)
        {
            Vector2 half = CameraFraming.HalfExtents(_camera.orthographicSize, _camera.aspect);
            return CameraFraming.Clamp(point, half, _map.Rect);
        }

        private void MoveTo(Vector2 point)
        {
            transform.position = new Vector3(point.x, point.y, transform.position.z);
        }
    }
}
