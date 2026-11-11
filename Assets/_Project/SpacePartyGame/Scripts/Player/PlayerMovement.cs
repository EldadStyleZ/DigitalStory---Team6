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
        private float _topSpeed = -1f;
        private Vector2 _aim;
        // private Vector2 _push;
        // private float _pushUntil = float.NegativeInfinity;
        private Knockback _knockback;
        private float _pushControl = 1f;

        /// <summary>The normal walking top speed, set in the Inspector.</summary>
        public float WalkSpeed => _moveSpeed;

        /// <summary>The top speed now: walking, unless an ability such as the run has raised it.</summary>
        public float TopSpeed
        {
            get => _topSpeed < 0f ? _moveSpeed : _topSpeed;
            set => _topSpeed = Mathf.Max(0f, value);
        }

        /// <summary>How the player is moving now, in units per second.</summary>
        public Vector2 Velocity => _rigidbody.linearVelocity;

        /// <summary>Raised when a blow shoves the player.</summary>
        public event Action Pushed;

        /// <summary>Raised when the player turns, with the new facing.</summary>
        public event Action<int> FacingChanged;

        /// <summary>1 when facing right, -1 when facing left.</summary>
        public int Facing => _facing;

        /// <summary>The way the player faces, as a unit vector.</summary>
        public Vector2 FacingDirection => new Vector2(_facing, 0f);

        /// <summary>Whether the player may walk now. When off, they stand still but can still aim.</summary>
        public bool CanMove { get; set; } = true;

        /// <summary>
        /// The way the player aims, as a unit vector: the stick or WASD now, or the last way they pushed it, or their
        /// facing before they have pushed it at all. It works while they cannot move, too.
        /// </summary>
        public Vector2 AimDirection => _aim.sqrMagnitude > 0f ? _aim : FacingDirection;

        /// <summary>
        /// Shoves the player for a while: <paramref name="velocity"/> is added to their own movement, and fades to
        /// nothing over <paramref name="seconds"/>, while they keep only <paramref name="control"/> (0 to 1) of
        /// their own movement.
        /// </summary>
        public void Push(Vector2 velocity, float seconds, float control)
        {
            // _push = velocity;
            // _pushStartedAt = Time.time;
            // _pushSeconds = Mathf.Max(0.01f, seconds);
            // _pushUntil = Time.time + _pushSeconds;
            _knockback = new Knockback(velocity, Time.time, seconds);
            _pushControl = Mathf.Clamp01(control);
            Pushed?.Invoke();
        }

        /// <summary>Puts the player back as they started: facing their start way, standing still, not pushed.</summary>
        public void ResetToStart()
        {
            // _pushUntil = float.NegativeInfinity;
            _knockback = default;
            _aim = Vector2.zero;
            _rigidbody.linearVelocity = Vector2.zero;
            _facing = _startsFacingLeft ? -1 : 1;

            if (_body != null)
            {
                _body.flipX = _facing < 0;
            }
        }

        /// <summary>Whether a blow is shoving the player now.</summary>
        public bool IsPushed => _knockback.IsActive(Time.time);

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

            if (input.sqrMagnitude > 0.04f)
            {
                _aim = input.normalized;
            }

            // The top speed replaces the walking speed, so the run can raise it.
            // _rigidbody.linearVelocity = PlayerMotion.VelocityFor(input, _moveSpeed);
            // _rigidbody.linearVelocity = PlayerMotion.VelocityFor(input, TopSpeed);
            Vector2 own = CanMove ? PlayerMotion.VelocityFor(input, TopSpeed) : Vector2.zero;
            // bool isPushed = Time.time < _pushUntil;
            // float left = isPushed ? 1f - (Time.time - _pushStartedAt) / _pushSeconds : 0f;
            // _rigidbody.linearVelocity = isPushed ? own * _pushControl + _push * left : own;
            _rigidbody.linearVelocity = IsPushed
                ? Knockback.Combine(own, _knockback.Velocity(Time.time), _pushControl)
                : own;

            if (!CanMove)
            {
                return;
            }

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
