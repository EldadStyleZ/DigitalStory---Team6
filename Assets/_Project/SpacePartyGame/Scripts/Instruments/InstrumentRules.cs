using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// Counts each note's press once until it is released. An analog trigger can report "pressed" again while it is
    /// let go (passing back under its press point), which must not count as a new press.
    /// </summary>
    public sealed class NotePressLatch
    {
        private readonly bool[] _down = new bool[Enum.GetValues(typeof(Note)).Length];

        /// <summary>The note's button went down: true if this is a new press, false if it was already down.</summary>
        public bool Press(Note note)
        {
            if (_down[(int)note])
            {
                return false;
            }

            _down[(int)note] = true;
            return true;
        }

        /// <summary>The note's button came up: true if it was down.</summary>
        public bool Release(Note note)
        {
            bool wasDown = _down[(int)note];
            _down[(int)note] = false;
            return wasDown;
        }

        /// <summary>Forgets every press, such as when playing is switched off.</summary>
        public void ReleaseAll() => Array.Clear(_down, 0, _down.Length);
    }

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
        private int _unlimited = -1;

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

        /// <summary>
        /// Lets one note play for as long as it is held, past the limit, such as Left while the blow charges; null
        /// puts the limit back on every note.
        /// </summary>
        public void HoldWithoutLimit(Note? note) => _unlimited = note.HasValue ? (int)note.Value : -1;

        /// <summary>Whether a note is playing: held, and for less than the limit (or with no limit).</summary>
        public bool IsActive(Note note, float now)
        {
            int index = (int)note;
            // return _held[index] && now - _pressedAt[index] < _maxHoldSeconds;
            return _held[index] && (index == _unlimited || now - _pressedAt[index] < _maxHoldSeconds);
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
    /// The run: while the run button is held, the player's top speed rises steadily from walking to the run speed,
    /// taking the acceleration time to get there. The moment it is let go, the speed falls back at the same rate, so
    /// slowing down takes as long as the speeding up did: the full acceleration time from flat out, less if let go
    /// early. Pressing again part way speeds up from wherever the speed is.
    /// </summary>
    public sealed class RunHold
    {
        private readonly float _walkSpeed;
        private readonly float _runSpeed;
        private readonly float _accelerationTime;

        /// <param name="accelerationTime">Seconds from walking to the run speed; 0 or less switches at once.</param>
        public RunHold(float walkSpeed, float runSpeed, float accelerationTime)
        {
            _walkSpeed = walkSpeed;
            _runSpeed = Mathf.Max(walkSpeed, runSpeed);
            _accelerationTime = accelerationTime;
            Speed = walkSpeed;
        }

        /// <summary>The player's top speed now.</summary>
        public float Speed { get; private set; }

        /// <summary>How far from walking to full run the speed is: 0 walking, 1 running flat out.</summary>
        public float Boost => _runSpeed > _walkSpeed ? (Speed - _walkSpeed) / (_runSpeed - _walkSpeed) : 0f;

        /// <summary>Moves the clock on: towards the run speed while held, back towards walking when not.</summary>
        public void Tick(bool held, float deltaTime)
        {
            float target = held ? _runSpeed : _walkSpeed;

            if (_accelerationTime <= 0f)
            {
                Speed = target;
                return;
            }

            float rate = (_runSpeed - _walkSpeed) / _accelerationTime;
            Speed = Mathf.MoveTowards(Speed, target, rate * deltaTime);
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
