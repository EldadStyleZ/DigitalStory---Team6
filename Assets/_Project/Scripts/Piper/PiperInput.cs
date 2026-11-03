using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// Reads the keyboard and passes it on: the walk keys move the Piper and the six flute keys are its holes.
    /// Every key can be changed in the Inspector. It is the only script that reads input, so the rest can be
    /// tested without pressing keys.
    /// </summary>
    [RequireComponent(typeof(PiperMovement), typeof(Flute))]
    public sealed class PiperInput : MonoBehaviour
    {
        [Header("Walking")]
        [SerializeField, Tooltip("Any of these walks up.")]
        private KeyCode[] _upKeys = { KeyCode.W, KeyCode.UpArrow };

        [SerializeField, Tooltip("Any of these walks down.")]
        private KeyCode[] _downKeys = { KeyCode.S, KeyCode.DownArrow };

        [SerializeField, Tooltip("Any of these walks left.")]
        private KeyCode[] _leftKeys = { KeyCode.A, KeyCode.LeftArrow };

        [SerializeField, Tooltip("Any of these walks right.")]
        private KeyCode[] _rightKeys = { KeyCode.D, KeyCode.RightArrow };

        [Header("Flute")]
        [SerializeField, Tooltip("The key for each hole, from hole 1 to hole 6.")]
        private KeyCode[] _holeKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        };

        private PiperMovement _movement;
        private Flute _flute;

        // Awake runs once, before any Update, so it is the place to find the components this one works with.
        private void Awake()
        {
            _movement = GetComponent<PiperMovement>();
            _flute = GetComponent<Flute>();
        }

        private void Update()
        {
            float across = Held(_rightKeys) - Held(_leftKeys);
            float up = Held(_upKeys) - Held(_downKeys);
            _movement.Direction = new Vector2(across, up);

            for (int hole = 0; hole < _holeKeys.Length && hole < Flute.HoleCount; hole++)
            {
                if (Input.GetKeyDown(_holeKeys[hole]))
                {
                    _flute.Press(hole + 1);
                }

                if (Input.GetKeyUp(_holeKeys[hole]))
                {
                    _flute.Release(hole + 1);
                }
            }
        }

        private static float Held(KeyCode[] keys)
        {
            foreach (KeyCode key in keys)
            {
                if (Input.GetKey(key))
                {
                    return 1f;
                }
            }

            return 0f;
        }
    }
}
