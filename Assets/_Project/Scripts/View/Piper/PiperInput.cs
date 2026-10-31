using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Turns the keyboard and mouse into what the player wants the Piper to do.</summary>
    /// <remarks>
    /// Uses the Input Manager, which Unity marks for deprecation. Moving to the Input System is a
    /// package decision for the team, and this is the only class that would change.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Piper Input")]
    public sealed class PiperInput : MonoBehaviour
    {
        private const int FirstHole = 1 << 0;

        private static readonly KeyCode[] NumberKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        };

        private static readonly KeyCode[] KeypadKeys =
        {
            KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6,
        };

        private Piper _piper;
        private Camera _camera;
        private PiperConfig _config;
        private FluteView _fluteView;
        private HudLayout _layout;

        /// <summary>Supplies what pointer walking needs. Call once, before the first read.</summary>
        public void Initialize(Piper piper, Camera sceneCamera, PiperConfig config, FluteView fluteView, HudLayout layout)
        {
            _piper = piper;
            _camera = sceneCamera;
            _config = config;
            _fluteView = fluteView;
            _layout = layout;
        }

        /// <summary>Reads this frame's input.</summary>
        public PiperIntent ReadIntent() => new PiperIntent(ReadMove(), ReadHeldHoles(), ReadPressedHoles());

        private int ReadHeldHoles()
        {
            int holes = _fluteView.HeldHoles | (Input.GetKey(KeyCode.Space) ? FirstHole : 0);

            for (int hole = 0; hole < NumberKeys.Length; hole++)
            {
                if (Input.GetKey(NumberKeys[hole]) || Input.GetKey(KeypadKeys[hole]))
                {
                    holes |= 1 << hole;
                }
            }

            return holes;
        }

        private int ReadPressedHoles()
        {
            int holes = _fluteView.TakePressedHoles() | (Input.GetKeyDown(KeyCode.Space) ? FirstHole : 0);

            for (int hole = 0; hole < NumberKeys.Length; hole++)
            {
                if (Input.GetKeyDown(NumberKeys[hole]) || Input.GetKeyDown(KeypadKeys[hole]))
                {
                    holes |= 1 << hole;
                }
            }

            return holes;
        }

        private static Vector2 ReadKeys()
        {
            float x = Axis(KeyCode.RightArrow, KeyCode.D) - Axis(KeyCode.LeftArrow, KeyCode.A);
            float y = Axis(KeyCode.DownArrow, KeyCode.S) - Axis(KeyCode.UpArrow, KeyCode.W);
            return new Vector2(x, y);
        }

        private static float Axis(KeyCode arrow, KeyCode letter)
        {
            return Input.GetKey(arrow) || Input.GetKey(letter) ? 1f : 0f;
        }

        private bool IsMouseOnAControl()
        {
            return _fluteView.IsBeingPlayed || _layout.IsOverControl(Input.mousePosition);
        }

        private Vector2 ReadMove()
        {
            Vector2 keys = ReadKeys();

            if (keys != Vector2.zero || !Input.GetMouseButton(0) || IsMouseOnAControl())
            {
                return keys;
            }

            Vector2 pointer = TownSpace.ToTown(_camera.ScreenToWorldPoint(Input.mousePosition));
            Vector2 toPointer = pointer - _piper.Position;
            return toPointer.magnitude > _config.PointerDeadZone ? toPointer : Vector2.zero;
        }
    }
}
