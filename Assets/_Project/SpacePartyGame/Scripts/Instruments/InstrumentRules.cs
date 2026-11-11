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

    /// <summary>Turns a note into the instrument's key, 1 (lowest) to 6 (highest), the same order as the toy flute's holes.</summary>
    public static class InstrumentKeys
    {
        /// <summary>The key for silence.</summary>
        public const int Silent = 0;

        /// <summary>Root D, LT E, Left G, Right A, RT B, Up high D.</summary>
        public static int KeyOf(Note? note)
        {
            switch (note)
            {
                case Note.Root:
                    return 1;
                case Note.LT:
                    return 2;
                case Note.Left:
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
