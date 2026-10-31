using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>The six-hole flute: decides which hole sounds and what that sound does.</summary>
    public sealed class Flute
    {
        public const int NoHole = -1;

        private readonly FluteConfig _config;

        private int _lastPressedHole = NoHole;

        public Flute(FluteConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        public int CurrentHole { get; private set; } = NoHole;
        public FluteMode Mode { get; private set; } = FluteMode.None;
        public float MusicRadius { get; private set; }

        public void Press(int hole) => _lastPressedHole = hole;

        /// <summary>
        /// Picks the hole to sound from those held: the last one pressed if it is still held,
        /// otherwise the highest.
        /// </summary>
        /// <param name="heldHoles">One bit per held hole.</param>
        public int ChooseHole(int heldHoles)
        {
            if (heldHoles == 0)
            {
                return NoHole;
            }

            if (_lastPressedHole != NoHole && IsHeld(heldHoles, _lastPressedHole))
            {
                return _lastPressedHole;
            }

            return HighestHeld(heldHoles);
        }

        /// <summary>Sounds a hole, or silences the flute with <see cref="NoHole"/>.</summary>
        public void Sound(int hole)
        {
            CurrentHole = hole;
            Mode = hole == NoHole ? FluteMode.None : _config.ModeOfHole(hole);
            MusicRadius = _config.RadiusOf(Mode);
        }

        private static bool IsHeld(int heldHoles, int hole) => (heldHoles & (1 << hole)) != 0;

        private int HighestHeld(int heldHoles)
        {
            for (int hole = _config.HoleCount - 1; hole >= 0; hole--)
            {
                if (IsHeld(heldHoles, hole))
                {
                    return hole;
                }
            }

            return NoHole;
        }
    }
}
