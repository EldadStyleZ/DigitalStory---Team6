using UnityEngine;

namespace PiperToy
{
    /// <summary>Walks the Piper in the direction it is given, using physics so it cannot leave the field.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PiperMovement : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("How fast the Piper walks, in units per second.")]
        private float _speed = 4f;

        private Rigidbody2D _body;

        /// <summary>Where the player wants to go. PiperInput sets it every frame.</summary>
        public Vector2 Direction { get; set; }

        /// <summary>The way the Piper last walked, so marching rats know which way is ahead.</summary>
        public Vector2 Facing { get; private set; } = Vector2.right;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        // Physics runs in FixedUpdate, so moving the Rigidbody2D here lets it bump into the edge and the rats.
        private void FixedUpdate()
        {
            Vector2 direction = Vector2.ClampMagnitude(Direction, 1f);
            _body.linearVelocity = direction * _speed;

            if (direction != Vector2.zero)
            {
                Facing = direction.normalized;
            }
        }
    }
}
