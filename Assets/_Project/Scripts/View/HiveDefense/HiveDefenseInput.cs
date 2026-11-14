using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DigitalStory.View
{
    /// <summary>Turns the keyboard, the mouse and a controller into what the player wants this frame.</summary>
    /// <remarks>
    /// The keyboard and mouse go through the Input Manager, as the rest of the project does. A controller goes through the Input
    /// System package (<see cref="Gamepad.current"/>), which can also rumble it. Both always work, and their values are merged.
    /// Read the properties after Update has run.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1100)] // Before the root (-1000), so the root reads this frame's input.
    [AddComponentMenu("DigitalStory/Hive Defense Input")]
    public sealed class HiveDefenseInput : MonoBehaviour
    {
        private const float MouseLiveSeconds = 1f;
        private const float MouseMovePixels = 4f;

        // The controller's buttons as bits: the four face buttons first, in the holes' order, then the others.
        private const int HoleButtonMask = 0b1111;
        private const int GiveBit = 1 << 4;
        private const int StartBit = 1 << 5;
        private const int PunchBit = 1 << 6;
        private const int KickBit = 1 << 7;

        // Holes 0 to 3, lowest first: H J K L, then the top row, then the keypad.
        private static readonly KeyCode[] LetterKeys = { KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

        private static readonly KeyCode[] NumberKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };

        private static readonly KeyCode[] KeypadKeys = { KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4 };

        // What the HUD writes for each hole: the letter of its key.
        private static readonly string[] HoleNames = { "H", "J", "K", "L" };

        // What a controller player reads instead of each key word in the HUD's prompts. The holes are drawn as button symbols.
        private static readonly (string Keys, string Pad)[] PadWords =
        {
            ("W A S D", "the left stick"),
            ("E", "RB"),
            ("Enter", "Start"),
            ("R", "Start"),
        };

        private Camera _camera;
        private GamepadConfig _gamepad;
        private Vector3 _lastMouse;
        private bool _hasLastMouse;
        private float _mouseLiveLeft;
        private bool _isMouseLive;
        private Gamepad _pad;
        private int _padHeld;
        private Vector2 _padMove;
        private Vector2 _aimStick;
        private bool _isStickAiming;

        /// <summary>Raised for each hole pressed this frame, by a key or a face button, lowest hole first. The hole counts from 0.</summary>
        public event Action<int> HolePressed;

        /// <summary>Raised when the player switches between the keyboard and mouse and a controller, with the one now in use.</summary>
        public event Action<ControlDevice> LastDeviceChanged;

        /// <summary>
        /// The walking keys and the left stick, relative to the camera and not turned by it: y forward (W) and x to the right (D),
        /// length at most 1. The root turns it onto the ground once, with <see cref="DigitalStory.Logic.HiveDefenseFrame.GroundStep"/>.
        /// </summary>
        public Vector2 LocalMove { get; private set; }

        /// <summary>True only on the frame the punch was pressed (Space, left mouse or the right trigger).</summary>
        public bool PunchPressed { get; private set; }

        /// <summary>True only on the frame the kick was pressed (Left Shift, right mouse or the left trigger).</summary>
        public bool KickPressed { get; private set; }

        /// <summary>True while either Shift is held: the swarm sprints. The giant ignores it; a Shift press still kicks.</summary>
        public bool SprintHeld { get; private set; }

        /// <summary>True only on the frame restart was pressed (R or Start).</summary>
        public bool RestartPressed { get; private set; }

        /// <summary>True while E or the right shoulder is held: give wasps to a hive.</summary>
        public bool GiveHeld { get; private set; }

        /// <summary>True only on the frame the call-wave key was pressed (Return, keypad Enter or Start).</summary>
        public bool CallWavePressed { get; private set; }

        /// <summary>
        /// The mouse's movement this frame, in the Input Manager's Mouse X and Mouse Y units (x to the right, y up). The root scales it
        /// by a sensitivity to orbit the chase camera.
        /// </summary>
        public Vector2 LookDelta { get; private set; }

        /// <summary>
        /// The right stick's turn of the view this frame, in degrees (x to the right, y up): its lean times the GamepadConfig's
        /// orbit speed, over real time. The root adds it to the mouse's turn while the camera chases the giant.
        /// </summary>
        public Vector2 StickLookDegrees { get; private set; }

        /// <summary>
        /// True for 1 s after the mouse moves 4 px, or turns the view while the cursor is locked (the giant's chase), while a mouse
        /// button is held, and while the right stick leans. Then the mouse or the stick aims; otherwise auto-aim chooses.
        /// </summary>
        public bool IsMouseLive { get; private set; }

        /// <summary>The device the player used last, the keyboard and mouse until a controller is used. The HUD names its controls.</summary>
        public ControlDevice LastDevice { get; private set; }

        /// <summary>The key word of each hole (H J K L), lowest first, for the HUD.</summary>
        public static IReadOnlyList<string> HoleKeyNames => HoleNames;

        /// <summary>Each key word in the HUD's prompts and the controller's name for it, such as ("E", "RB").</summary>
        public static IReadOnlyList<(string Keys, string Pad)> PadNames => PadWords;

        /// <summary>
        /// True while a key for a hole is held down (H J K L, top row or keypad), or its face button (West, South, East, North).
        /// The hole counts from 0.
        /// </summary>
        public bool IsHoleHeld(int hole)
        {
            return hole >= 0 && hole < LetterKeys.Length
                && (Input.GetKey(LetterKeys[hole]) || Input.GetKey(NumberKeys[hole]) || Input.GetKey(KeypadKeys[hole])
                    || (_padHeld & (1 << hole)) != 0);
        }

        /// <summary>The letter of the key that plays the hole (H J K L), for the HUD. The hole counts from 0; any other hole is "?".</summary>
        public static string HoleName(int hole)
        {
            return hole >= 0 && hole < HoleNames.Length ? HoleNames[hole] : "?";
        }

        /// <summary>Supplies the camera that aiming is relative to and the controller's numbers. Call once, before the first Update.</summary>
        /// <exception cref="ArgumentNullException">The controller's numbers are missing.</exception>
        public void Initialize(Camera camera, GamepadConfig gamepad)
        {
            _camera = camera;
            _gamepad = gamepad != null ? gamepad : throw new ArgumentNullException(nameof(gamepad));
        }

        /// <summary>
        /// Finds what the player aims at on the ground: while the right stick leans, the core plus the stick's lean times the
        /// GamepadConfig's reach, the camera's way; otherwise where the mouse points on the ground plane (y = 0).
        /// </summary>
        /// <param name="core">The swarm's core, which the stick aims from.</param>
        /// <param name="cameraForward">The camera's forward; the stick turns with it, as the walking keys do.</param>
        /// <param name="point">The aim point.</param>
        /// <returns>False when the stick rests and there is no camera or the mouse's ray does not reach the ground.</returns>
        public bool TryGetAimPoint(Vector3 core, Vector3 cameraForward, out Vector3 point)
        {
            if (_isStickAiming)
            {
                point = GamepadStick.AimPoint(core, _aimStick, cameraForward, _gamepad.AimReachMetres);
                return true;
            }

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

        /// <summary>
        /// Notes what the player touched this frame and switches <see cref="LastDevice"/> by <see cref="LastDeviceRule"/>, raising
        /// <see cref="LastDeviceChanged"/> when it changes. Update calls it each frame; a test may call it to stand in for a key,
        /// which the Input Manager cannot fake.
        /// </summary>
        public void RecordUse(bool isKeyboardUsed, bool isGamepadUsed)
        {
            ControlDevice next = LastDeviceRule.Next(LastDevice, isKeyboardUsed, isGamepadUsed);
            if (next == LastDevice)
            {
                return;
            }

            LastDevice = next;
            LastDeviceChanged?.Invoke(next);
        }

        private void Update()
        {
            Vector2 keysMove = ReadMove();
            bool isPunchKey = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
            bool isKickKey = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftShift);
            bool isRestartKey = Input.GetKeyDown(KeyCode.R);
            bool isGiveKey = Input.GetKey(KeyCode.E);
            bool isCallWaveKey = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            bool isMouseHeld = Input.GetMouseButton(0) || Input.GetMouseButton(1);
            LookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            bool hasMouseMoved = UpdateMouseLive(isMouseHeld);
            int holeKeys = ReadHoleKeys();
            int padPressed = ReadPad();

            LocalMove = Vector2.ClampMagnitude(keysMove + _padMove, 1f);
            PunchPressed = isPunchKey || (padPressed & PunchBit) != 0;
            KickPressed = isKickKey || (padPressed & KickBit) != 0;
            SprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            RestartPressed = isRestartKey || (padPressed & StartBit) != 0;
            GiveHeld = isGiveKey || (_padHeld & GiveBit) != 0;
            CallWavePressed = isCallWaveKey || (padPressed & StartBit) != 0;
            IsMouseLive = _isMouseLive || _isStickAiming;

            bool isKeyboardUsed = keysMove.sqrMagnitude > 0f || isPunchKey || isKickKey || isRestartKey || isGiveKey
                || isCallWaveKey || isMouseHeld || hasMouseMoved || holeKeys != 0;
            bool isGamepadUsed = _padHeld != 0 || _padMove.sqrMagnitude > 0f || _isStickAiming;
            RecordUse(isKeyboardUsed, isGamepadUsed);

            int holes = holeKeys | (padPressed & HoleButtonMask);
            for (int hole = 0; hole < LetterKeys.Length; hole++)
            {
                if ((holes & (1 << hole)) != 0)
                {
                    HolePressed?.Invoke(hole);
                }
            }
        }

        // Returns true when the mouse moved this frame.
        private bool UpdateMouseLive(bool isHeld)
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
            if (isHeld)
            {
                _mouseLiveLeft = MouseLiveSeconds;
            }

            _isMouseLive = isHeld || _mouseLiveLeft > 0f;
            return hasMoved;
        }

        // The holes whose key went down this frame, as bits.
        private static int ReadHoleKeys()
        {
            int holes = 0;
            for (int hole = 0; hole < LetterKeys.Length; hole++)
            {
                if (Input.GetKeyDown(LetterKeys[hole]) || Input.GetKeyDown(NumberKeys[hole]) || Input.GetKeyDown(KeypadKeys[hole]))
                {
                    holes |= 1 << hole;
                }
            }

            return holes;
        }

        // Reads the controller in use, if there is one: its sticks, and its buttons as bits. Returns the buttons that went down
        // this frame, found against last frame's, so a press counts once however the Input System updates.
        private int ReadPad()
        {
            Gamepad pad = _gamepad != null ? Gamepad.current : null;
            if (pad != _pad)
            {
                // A controller that appears, or takes over, starts with what is already held: Start held from the restart that
                // loaded this scene is no new press.
                _pad = pad;
                _padHeld = pad != null ? PadButtons(pad) : 0;
            }

            if (pad == null)
            {
                _padMove = Vector2.zero;
                _aimStick = Vector2.zero;
                _isStickAiming = false;
                StickLookDegrees = Vector2.zero;
                return 0;
            }

            int held = PadButtons(pad);
            int pressed = held & ~_padHeld;
            _padHeld = held;

            float deadZone = _gamepad.StickDeadZone;
            _padMove = GamepadStick.DeadZone(pad.leftStick.ReadValue(), deadZone);
            _aimStick = GamepadStick.DeadZone(pad.rightStick.ReadValue(), deadZone);
            _isStickAiming = _aimStick.sqrMagnitude > 0f;
            StickLookDegrees = _aimStick * (_gamepad.OrbitDegreesPerSecond * Time.unscaledDeltaTime);
            return pressed;
        }

        // Brand-neutral positions: West, South, East and North are X A B Y on one pad and Square Cross Circle Triangle on another.
        private static int PadButtons(Gamepad pad)
        {
            int bits = 0;
            bits |= pad.buttonWest.isPressed ? 1 << 0 : 0;
            bits |= pad.buttonSouth.isPressed ? 1 << 1 : 0;
            bits |= pad.buttonEast.isPressed ? 1 << 2 : 0;
            bits |= pad.buttonNorth.isPressed ? 1 << 3 : 0;
            bits |= pad.rightShoulder.isPressed ? GiveBit : 0;
            bits |= pad.startButton.isPressed ? StartBit : 0;
            bits |= pad.rightTrigger.isPressed ? PunchBit : 0;
            bits |= pad.leftTrigger.isPressed ? KickBit : 0;
            return bits;
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
