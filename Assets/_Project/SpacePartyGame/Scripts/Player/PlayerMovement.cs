using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpacePartyGame
{
    /// <summary>
    /// Moves the player top-down from its Move action and turns its picture to face the way it moves. The facing is
    /// public because the attack wave fires the way the player faces.
    /// </summary>
    /// <remarks>The rules live in <see cref="PlayerMotion"/> and <see cref="FacingRule"/>; this only reads input,
    /// sets the velocity and flips the sprite.</remarks>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Top speed, in units per second.")]
        private float _moveSpeed = 4f;

        [SerializeField, Tooltip("The picture to flip. The art faces right.")]
        private SpriteRenderer _body;

        [SerializeField, Tooltip("Face left at the start instead of right, such as for the player on the right side.")]
        private bool _startsFacingLeft;

        private Rigidbody2D _rigidbody;
        private InputAction _move;
        private int _facing = 1;

        /// <summary>Raised when the player turns, with the new facing.</summary>
        public event Action<int> FacingChanged;

        /// <summary>1 when facing right, -1 when facing left.</summary>
        public int Facing => _facing;

        /// <summary>The way the player faces, as a unit vector.</summary>
        public Vector2 FacingDirection => new Vector2(_facing, 0f);

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _move = GetComponent<PlayerInput>().actions.FindAction("Player/Move", true);

            if (_body == null)
            {
                Debug.LogError($"{name}: PlayerMovement has no body sprite to flip.", this);
                return;
            }

            _facing = _startsFacingLeft ? -1 : 1;
            _body.flipX = _facing < 0;
        }

        private void FixedUpdate()
        {
            Vector2 input = _move.ReadValue<Vector2>();
            _rigidbody.linearVelocity = PlayerMotion.VelocityFor(input, _moveSpeed);

            int facing = FacingRule.Next(_facing, input.x);

            if (facing != _facing)
            {
                _facing = facing;
                _body.flipX = _facing < 0;
                FacingChanged?.Invoke(_facing);
            }
        }
    }
}
