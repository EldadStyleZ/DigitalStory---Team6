using System;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Turns the keyboard and mouse into what the Wasp Piper's player wants this frame.</summary>
    /// <remarks>Uses the Input Manager, as the rest of the project does. Read the properties after Update has run.</remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1100)] // Before the root (-1000), so the root reads this frame's input.
    [AddComponentMenu("DigitalStory/Hive Defense Input")]
    public sealed class HiveDefenseInput : MonoBehaviour
    {
        private static readonly KeyCode[] NumberKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        };

        private static readonly KeyCode[] KeypadKeys =
        {
            KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6,
        };

        private Camera _camera;

        /// <summary>Raised for each hole pressed this frame, lowest hole first. The hole counts from 0.</summary>
        public event Action<int> HolePressed;

        /// <summary>The walking direction on the ground, relative to the camera's yaw. Length at most 1.</summary>
        public Vector3 Move { get; private set; }

        /// <summary>True only on the frame the punch was pressed (left mouse or J).</summary>
        public bool PunchPressed { get; private set; }

        /// <summary>True only on the frame the kick was pressed (right mouse or K).</summary>
        public bool KickPressed { get; private set; }

        /// <summary>True only on the frame restart was pressed (R).</summary>
        public bool RestartPressed { get; private set; }

        /// <summary>Supplies the camera that walking and aiming are relative to. Call once, before the first Update.</summary>
        public void Initialize(Camera camera)
        {
            _camera = camera;
        }

        /// <summary>Finds where the mouse points on the ground plane (y = 0).</summary>
        /// <returns>False when there is no camera or the ray does not reach the ground.</returns>
        public bool TryGetAimPoint(out Vector3 point)
        {
            point = Vector3.zero;

            if (_camera == null)
            {
                return false;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);

            if (!ground.Raycast(ray, out float distance))
            {
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }

        private void Update()
        {
            Move = ReadMove();
            PunchPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
            KickPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.K);
            RestartPressed = Input.GetKeyDown(KeyCode.R);

            for (int hole = 0; hole < NumberKeys.Length; hole++)
            {
                if (Input.GetKeyDown(NumberKeys[hole]) || Input.GetKeyDown(KeypadKeys[hole]))
                {
                    HolePressed?.Invoke(hole);
                }
            }
        }

        private Vector3 ReadMove()
        {
            float x = Axis(KeyCode.RightArrow, KeyCode.D) - Axis(KeyCode.LeftArrow, KeyCode.A);
            float z = Axis(KeyCode.UpArrow, KeyCode.W) - Axis(KeyCode.DownArrow, KeyCode.S);

            if (x == 0f && z == 0f)
            {
                return Vector3.zero;
            }

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (_camera != null)
            {
                Transform view = _camera.transform;
                forward = Flatten(view.forward, view.up);
                right = Flatten(view.right, Vector3.right);
            }

            return Vector3.ClampMagnitude((right * x) + (forward * z), 1f);
        }

        // A camera looking straight down has no horizontal forward, so the up vector stands in for it.
        private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 1e-6f)
            {
                fallback.y = 0f;
                direction = fallback;
            }

            return direction.normalized;
        }

        private static float Axis(KeyCode arrow, KeyCode letter)
        {
            return Input.GetKey(arrow) || Input.GetKey(letter) ? 1f : 0f;
        }
    }
}
