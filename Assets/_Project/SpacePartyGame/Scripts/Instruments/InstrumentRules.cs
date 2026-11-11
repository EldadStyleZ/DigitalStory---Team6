using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Which of a player's six notes are playing. Each note has its own timer: it plays from its press until it is
    /// released or has been held for the limit, whichever comes first. Of the notes playing, the one pressed last is
    /// the one heard, so a player plays on without a gap only by switching notes.
    /// </summary>
    public sealed class NoteHolds
    {
        private const int NoteCount = 6;

        private readonly float _maxHoldSeconds;
        private readonly float[] _pressedAt = new float[NoteCount];
        private readonly bool[] _held = new bool[NoteCount];
        private readonly int[] _pressOrder = new int[NoteCount];
        private int _presses;

        public NoteHolds(float maxHoldSeconds)
        {
            _maxHoldSeconds = maxHoldSeconds;
        }

        /// <summary>A note's button went down; its timer starts again.</summary>
        public void Press(Note note, float now)
        {
            int index = (int)note;
            _held[index] = true;
            _pressedAt[index] = now;
            _pressOrder[index] = ++_presses;
        }

        /// <summary>A note's button came up.</summary>
        public void Release(Note note) => _held[(int)note] = false;

        /// <summary>Lets go of every note, such as when the player's controls switch off.</summary>
        public void ReleaseAll()
        {
            for (int i = 0; i < NoteCount; i++)
            {
                _held[i] = false;
            }
        }

        /// <summary>Whether a note is playing: held, and for less than the limit.</summary>
        public bool IsActive(Note note, float now)
        {
            int index = (int)note;
            return _held[index] && now - _pressedAt[index] < _maxHoldSeconds;
        }

        /// <summary>When a note was last pressed.</summary>
        public float StartedAt(Note note) => _pressedAt[(int)note];

        /// <summary>The note heard now: the last pressed of those playing, or null for silence.</summary>
        public Note? Sounding(float now)
        {
            Note? sounding = null;
            int latest = 0;

            for (int i = 0; i < NoteCount; i++)
            {
                if (IsActive((Note)i, now) && _pressOrder[i] > latest)
                {
                    latest = _pressOrder[i];
                    sounding = (Note)i;
                }
            }

            return sounding;
        }
    }

    /// <summary>
    /// The run: playing LT and RT one after the other, quickly, raises the player's top speed a step per switch up to
    /// the run speed. Once the switches stop coming, the speed eases back down to walking.
    /// </summary>
    public sealed class RunRhythm
    {
        private readonly float _walkSpeed;
        private readonly float _runSpeed;
        private readonly float _speedPerSwitch;
        private readonly float _switchWindow;
        private readonly float _easeBackPerSecond;

        private int _lastSide;
        private float _lastPressAt = float.NegativeInfinity;

        /// <param name="switchWindow">The longest gap between LT and RT that still counts as a switch, in seconds.</param>
        /// <param name="easeBackPerSecond">How fast the speed drops back to walking once the switches stop.</param>
        public RunRhythm(float walkSpeed, float runSpeed, float speedPerSwitch, float switchWindow, float easeBackPerSecond)
        {
            _walkSpeed = walkSpeed;
            _runSpeed = Mathf.Max(walkSpeed, runSpeed);
            _speedPerSwitch = speedPerSwitch;
            _switchWindow = switchWindow;
            _easeBackPerSecond = easeBackPerSecond;
            Speed = walkSpeed;
        }

        /// <summary>The player's top speed now.</summary>
        public float Speed { get; private set; }

        /// <summary>How far from walking to full run the speed is: 0 walking, 1 running flat out.</summary>
        public float Boost => _runSpeed > _walkSpeed ? (Speed - _walkSpeed) / (_runSpeed - _walkSpeed) : 0f;

        /// <summary>A note's button went down. Only LT and RT count; a switch from one to the other in time adds speed.</summary>
        public void Press(Note note, float now)
        {
            int side = note == Note.LT ? -1 : note == Note.RT ? 1 : 0;

            if (side == 0)
            {
                return;
            }

            if (_lastSide != 0 && side != _lastSide && now - _lastPressAt <= _switchWindow)
            {
                Speed = Mathf.Min(_runSpeed, Speed + _speedPerSwitch);
            }

            _lastSide = side;
            _lastPressAt = now;
        }

        /// <summary>Moves the clock on: with no switch for longer than the window, the speed eases back to walking.</summary>
        public void Tick(float now, float deltaTime)
        {
            if (now - _lastPressAt > _switchWindow)
            {
                Speed = Mathf.Max(_walkSpeed, Speed - (_easeBackPerSecond * deltaTime));
            }
        }
    }

    /// <summary>Turns a note into the instrument's key, 1 (lowest) to 6 (highest), the toy flute's holes.</summary>
    public static class InstrumentKeys
    {
        /// <summary>The key for silence.</summary>
        public const int Silent = 0;

        /// <summary>
        /// Root D, Left E, LT G, Right A, RT B, Up high D. LT and RT are a major third apart (G and B), so the run's
        /// LT-RT trill sounds sweet rather than the hollow fifth it would be with LT on E.
        /// </summary>
        public static int KeyOf(Note? note)
        {
            switch (note)
            {
                case Note.Root:
                    return 1;
                case Note.Left:
                    return 2;
                case Note.LT:
                    return 3;
                case Note.Right:
                    return 4;
                case Note.RT:
                    return 5;
                case Note.Up:
                    return 6;
                default:
                    return Silent;
            }
        }
    }
}
