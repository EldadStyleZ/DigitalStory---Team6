using UnityEngine;

namespace PiperToy
{
    /// <summary>Walks the Piper in the direction it is given, using physics so it cannot leave the field.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PiperMovement : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("How fast the Piper walks, in units per second.")]
        private float _speed = 4f;

        [SerializeField, Min(1f), Tooltip("How many times faster the Piper goes while the Crowd Surf crowd under it carries it.")]
        private float _carriedSpeedFactor = 2f;

        private Rigidbody2D _body;

        /// <summary>Where the player wants to go. PiperInput sets it every frame.</summary>
        public Vector2 Direction { get; set; }

        /// <summary>True while the Crowd Surf crowd is under the Piper and carrying it. The RatHerd sets it.</summary>
        public bool IsCarried { get; set; }

        /// <summary>The way the Piper last walked, so marching rats know which way is ahead.</summary>
        public Vector2 Facing { get; private set; } = Vector2.right;

        /// <summary>How fast and which way the Piper is moving, so a crowd carrying it can keep up.</summary>
        public Vector2 Velocity => _body.linearVelocity;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        // Physics runs in FixedUpdate, so moving the Rigidbody2D here lets it bump into the edge and the rats.
        private void FixedUpdate()
        {
            Vector2 direction = Vector2.ClampMagnitude(Direction, 1f);
            float speed = IsCarried ? _speed * _carriedSpeedFactor : _speed;
            _body.linearVelocity = direction * speed;

            if (direction != Vector2.zero)
            {
                Facing = direction.normalized;
            }
        }
    }
}
