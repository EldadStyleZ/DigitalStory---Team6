using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Moves the Wasp Piper with a CharacterController and drives the Animator's Speed and IsPlaying.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("DigitalStory/Piper Motor")]
    public sealed class PiperMotor : MonoBehaviour
    {
        private const float GroundedPush = -2f;
        private const float MovedEpsilonSquared = 1e-8f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsPlayingHash = Animator.StringToHash("IsPlaying");

        private CharacterController _controller;
        private PiperMoveConfig _config;
        private Animator _animator;
        private float _verticalSpeed;
        private float _playingSecondsLeft;
        private bool _isChannelling;
        private bool _lastIsPlaying;
        private bool _hasLastIsPlaying;

        /// <summary>Where the Piper stands.</summary>
        public Vector3 Position => transform.position;

        /// <summary>The way the Piper faces, on the ground.</summary>
        public Vector3 Forward => transform.forward;

        /// <summary>True when the last Move call changed the Piper's horizontal position.</summary>
        public bool MovedThisFrame { get; private set; }

        /// <summary>Supplies the numbers and the Animator. The Animator may be null. Call once, before the first Move.</summary>
        public void Initialize(PiperMoveConfig config, Animator animator)
        {
            _config = config;
            _animator = animator;
            _controller = GetComponent<CharacterController>();
        }

        /// <summary>Walks, turns and settles on the ground for one frame, and updates the animation.</summary>
        /// <param name="direction">Where to walk on the ground. Length at most 1.</param>
        /// <param name="speedMultiplier">Scales the walking speed, for the dome and for smoke.</param>
        /// <param name="isRooted">When true the Piper does not walk.</param>
        /// <param name="deltaTime">Seconds since the last call.</param>
        public void Move(Vector3 direction, float speedMultiplier, bool isRooted, float deltaTime)
        {
            direction.y = 0f;
            Vector3 horizontal = isRooted ? Vector3.zero : direction * (_config.Speed * speedMultiplier);

            if (_controller.isGrounded && _verticalSpeed < 0f)
            {
                _verticalSpeed = GroundedPush;
            }

            _verticalSpeed += Physics.gravity.y * deltaTime;

            Vector3 before = transform.position;
            _controller.Move((horizontal + (Vector3.up * _verticalSpeed)) * deltaTime);
            Vector3 moved = transform.position - before;
            moved.y = 0f;
            MovedThisFrame = moved.sqrMagnitude > MovedEpsilonSquared;

            if (!isRooted && direction.sqrMagnitude > 1e-6f)
            {
                Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, _config.TurnDegreesPerSecond * deltaTime);
            }

            _playingSecondsLeft = Mathf.Max(0f, _playingSecondsLeft - deltaTime);
            float speed = deltaTime > 0f ? moved.magnitude / deltaTime : 0f;
            Animate(speed);
        }

        /// <summary>Holds the playing pose for PlayingHoldSeconds. Call on every hole press.</summary>
        public void NotifyNotePlayed()
        {
            _playingSecondsLeft = _config.PlayingHoldSeconds;
        }

        /// <summary>Keeps the playing pose while a channelled song runs.</summary>
        public void SetChannelling(bool isChannelling)
        {
            _isChannelling = isChannelling;
        }

        private void Animate(float horizontalSpeed)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetFloat(SpeedHash, horizontalSpeed / _config.RunClipSpeed);

            bool isPlaying = _isChannelling || _playingSecondsLeft > 0f;

            if (!_hasLastIsPlaying || isPlaying != _lastIsPlaying)
            {
                _animator.SetBool(IsPlayingHash, isPlaying);
                _lastIsPlaying = isPlaying;
                _hasLastIsPlaying = true;
            }
        }
    }
}
