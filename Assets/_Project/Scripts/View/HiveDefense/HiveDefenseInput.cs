using System;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Turns the keyboard and mouse into what the player wants this frame.</summary>
    /// <remarks>Uses the Input Manager, as the rest of the project does. Read the properties after Update has run.</remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1100)] // Before the root (-1000), so the root reads this frame's input.
    [AddComponentMenu("DigitalStory/Hive Defense Input")]
    public sealed class HiveDefenseInput : MonoBehaviour
    {
        private const float MouseLiveSeconds = 1f;
        private const float MouseMovePixels = 4f;

        // Holes 0 to 3, lowest first: H J K L, then the top row, then the keypad.
        private static readonly KeyCode[] LetterKeys = { KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

        private static readonly KeyCode[] NumberKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };

        private static readonly KeyCode[] KeypadKeys = { KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4 };

        // What the HUD writes for each hole: the letter of its key.
        private static readonly string[] HoleNames = { "H", "J", "K", "L" };

        private Camera _camera;
        private Vector3 _lastMouse;
        private bool _hasLastMouse;
        private float _mouseLiveLeft;

        /// <summary>Raised for each hole pressed this frame, lowest hole first. The hole counts from 0.</summary>
        public event Action<int> HolePressed;

        /// <summary>
        /// The walking keys, relative to the camera and not turned by it: y forward (W) and x to the right (D), length at most 1.
        /// The root turns it onto the ground once, with <see cref="DigitalStory.Logic.HiveDefenseFrame.GroundStep"/>.
        /// </summary>
        public Vector2 LocalMove { get; private set; }

        /// <summary>True only on the frame the punch was pressed (Space or left mouse).</summary>
        public bool PunchPressed { get; private set; }

        /// <summary>True only on the frame the kick was pressed (Left Shift or right mouse).</summary>
        public bool KickPressed { get; private set; }

        /// <summary>True only on the frame restart was pressed (R).</summary>
        public bool RestartPressed { get; private set; }

        /// <summary>True while E is held: give wasps to a hive.</summary>
        public bool GiveHeld { get; private set; }

        /// <summary>True only on the frame the call-wave key was pressed (Return or keypad Enter).</summary>
        public bool CallWavePressed { get; private set; }

        /// <summary>
        /// The mouse's movement this frame, in the Input Manager's Mouse X and Mouse Y units (x to the right, y up). The root scales it
        /// by a sensitivity to orbit the chase camera.
        /// </summary>
        public Vector2 LookDelta { get; private set; }

        /// <summary>
        /// True for 1 s after the mouse moves 4 px, or turns the view while the cursor is locked (the giant's chase), and while
        /// a mouse button is held. Then the mouse aims; otherwise auto-aim chooses.
        /// </summary>
        public bool IsMouseLive { get; private set; }

        /// <summary>True while a key for a hole is held down (H J K L, top row or keypad). The hole counts from 0.</summary>
        public bool IsHoleHeld(int hole)
        {
            return hole >= 0 && hole < LetterKeys.Length
                && (Input.GetKey(LetterKeys[hole]) || Input.GetKey(NumberKeys[hole]) || Input.GetKey(KeypadKeys[hole]));
        }

        /// <summary>The letter of the key that plays the hole (H J K L), for the HUD. The hole counts from 0; any other hole is "?".</summary>
        public static string HoleName(int hole)
        {
            return hole >= 0 && hole < HoleNames.Length ? HoleNames[hole] : "?";
        }

        /// <summary>Supplies the camera that aiming is relative to. Call once, before the first Update.</summary>
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
            LocalMove = ReadMove();
            PunchPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
            KickPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftShift);
            RestartPressed = Input.GetKeyDown(KeyCode.R);
            GiveHeld = Input.GetKey(KeyCode.E);
            CallWavePressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            LookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            UpdateMouseLive();

            for (int hole = 0; hole < LetterKeys.Length; hole++)
            {
                if (Input.GetKeyDown(LetterKeys[hole]) || Input.GetKeyDown(NumberKeys[hole]) || Input.GetKeyDown(KeypadKeys[hole]))
                {
                    HolePressed?.Invoke(hole);
                }
            }
        }

        private void UpdateMouseLive()
        {
            bool hasMoved;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // A locked cursor stays put while the mouse turns the view, so a turn is the move. The reference is taken
                // afresh once the cursor is free, so its jump back is no move.
                hasMoved = LookDelta.sqrMagnitude > 0f;
                _hasLastMouse = false;
            }
            else
            {
                Vector3 mouse = Input.mousePosition;
                hasMoved = _hasLastMouse && (mouse - _lastMouse).sqrMagnitude >= MouseMovePixels * MouseMovePixels;

                // The reference moves only when the mouse has moved 4 px, so slow drags still add up.
                if (!_hasLastMouse || hasMoved)
                {
                    _lastMouse = mouse;
                    _hasLastMouse = true;
                }
            }

            _mouseLiveLeft = hasMoved ? MouseLiveSeconds : Mathf.Max(0f, _mouseLiveLeft - Time.unscaledDeltaTime);

            bool held = Input.GetMouseButton(0) || Input.GetMouseButton(1);
            if (held)
            {
                _mouseLiveLeft = MouseLiveSeconds;
            }

            IsMouseLive = held || _mouseLiveLeft > 0f;
        }

        // Only the keys, not turned by the camera: the root turns the move once, so the core and the giant agree.
        private static Vector2 ReadMove()
        {
            float x = Axis(KeyCode.RightArrow, KeyCode.D) - Axis(KeyCode.LeftArrow, KeyCode.A);
            float y = Axis(KeyCode.UpArrow, KeyCode.W) - Axis(KeyCode.DownArrow, KeyCode.S);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private static float Axis(KeyCode arrow, KeyCode letter)
        {
            return Input.GetKey(arrow) || Input.GetKey(letter) ? 1f : 0f;
        }
    }
}
