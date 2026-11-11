using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Moves the swarm's core with a CharacterController: the invisible point the swarm gathers around and the camera
    /// follows. The core has no model; the swarm's inner ring shows where it is.
    /// </summary>
    /// <remarks>
    /// The session decides the rest: which way the core faces, where it is while the giant stands (inside the giant) and
    /// when the giant ends (where the giant stood), and that it stays inside the play area. <see cref="PlaceAt"/> puts it
    /// there, and clear of any hive or wall when it is put down to stay.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("DigitalStory/Swarm Core Motor")]
    public sealed class SwarmCoreMotor : MonoBehaviour
    {
        private const float GroundedPush = -2f;

        // Not game tuning: a place nearer than 1 mm on the ground is where the core already is.
        private const float SamePlaceSqrMetres = 0.000001f;

        // Not game tuning: a place can be inside a hive and a wall at once, and a push out of one can reach into another.
        private const int MaxPushOutPasses = 4;
        private const int OverlapCapacity = 8;

        private readonly Collider[] _overlaps = new Collider[OverlapCapacity];

        private CharacterController _controller;
        private CoreMoveConfig _config;
        private float _verticalSpeed;
        private bool _mayBeInsideSolid;

        /// <summary>Where the core is.</summary>
        public Vector3 Position => transform.position;

        /// <summary>Supplies the numbers. Call once, before the first Move. Throws when the config is missing.</summary>
        public void Initialize(CoreMoveConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _controller = GetComponent<CharacterController>();
        }

        /// <summary>Moves the core and settles it on the ground for one frame.</summary>
        /// <param name="direction">Where to move on the ground. Length at most 1.</param>
        /// <param name="speedMultiplier">Scales the speed, for the dome and for smoke.</param>
        /// <param name="isRooted">When true the core does not move across the ground.</param>
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
            _controller.Move((horizontal + (Vector3.up * _verticalSpeed)) * deltaTime);
        }

        /// <summary>
        /// Puts the core at <paramref name="position"/> on the ground, keeping its height: where the session says it is
        /// while the giant stands, where the giant stood when it ends, and at the play area's edge when it walked past it.
        /// Does nothing when the core is already there and, if it is put down, already clear.
        /// </summary>
        /// <param name="position">Where the session says the core is.</param>
        /// <param name="isPutDown">
        /// True when the core is put down to stay, so it is pushed clear of any hive or wall it would stand inside. False
        /// while it only rides inside the giant, where standing inside a hive does not matter.
        /// </param>
        public void PlaceAt(Vector3 position, bool isPutDown)
        {
            Vector3 current = transform.position;
            Vector3 offset = position - current;
            offset.y = 0f;

            // When the giant ends the core is usually where the giant last put it, which may be inside a hive, so it is
            // cleared even without a move.
            bool isThere = offset.sqrMagnitude <= SamePlaceSqrMetres;
            if (isThere && !(isPutDown && _mayBeInsideSolid))
            {
                return;
            }

            Vector3 place = new Vector3(position.x, current.y, position.z);
            if (isPutDown)
            {
                place = ClearOfSolids(place);
            }

            _mayBeInsideSolid = !isPutDown;

            // The controller keeps its own copy of where it is, so it is off while the core is put somewhere else.
            _controller.enabled = false;
            transform.position = place;
            _controller.enabled = true;
        }

        // The controller does not push itself out of a hive or a wall it was put inside while it stands still. It jumps out
        // the shortest way on the player's next move, whichever way the player asked to go, and that jump would turn the
        // core. So the core is put down outside at once, its skin width clear, where the controller rests against things.
        private Vector3 ClearOfSolids(Vector3 place)
        {
            Quaternion rotation = transform.rotation;
            float half = Mathf.Max(0f, (_controller.height * 0.5f) - _controller.radius);
            for (int pass = 0; pass < MaxPushOutPasses; pass++)
            {
                Vector3 center = place + (rotation * _controller.center);
                int count = Physics.OverlapCapsuleNonAlloc(
                    center + (Vector3.up * half),
                    center - (Vector3.up * half),
                    _controller.radius,
                    _overlaps,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore);
                bool isClear = true;
                for (int index = 0; index < count; index++)
                {
                    Collider other = _overlaps[index];
                    if (other == _controller)
                    {
                        continue;
                    }

                    Transform otherTransform = other.transform;
                    if (Physics.ComputePenetration(
                        _controller, place, rotation, other, otherTransform.position, otherTransform.rotation, out Vector3 direction, out float distance))
                    {
                        place += direction * (distance + _controller.skinWidth);
                        isClear = false;
                    }
                }

                if (isClear)
                {
                    break;
                }
            }

            return place;
        }
    }
}
