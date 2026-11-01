using System;
using System.Collections.Generic;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Reads notes as they are played and decides which combo they make.</summary>
    public sealed class ComboReader
    {
        /// <summary>How many notes the flute has: H, J, K and L.</summary>
        public const int NoteCount = 4;

        private readonly ComboConfig _config;
        private readonly IReadOnlyList<CreatureType> _creatures;
        private readonly List<int> _notes = new List<int>();

        private float _secondsLeft;

        public ComboReader(ComboConfig config, IReadOnlyList<CreatureType> creatures)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _creatures = creatures ?? throw new ArgumentNullException(nameof(creatures));
            CheckCombos(creatures);
        }

        /// <summary>The notes of the combo being played.</summary>
        public IReadOnlyList<int> Notes => _notes;

        /// <summary>Adds a note to the combo being played.</summary>
        /// <param name="level">The Piper's level, which decides which combos are known.</param>
        public ComboResult Press(int note, int level)
        {
            _notes.Add(note);
            bool canStillBecomeOne = false;

            foreach (CreatureType creature in _creatures)
            {
                if (creature.UnlockLevel > level || !Starts(_notes, creature.Combo))
                {
                    continue;
                }

                if (creature.Combo.Count == _notes.Count)
                {
                    Clear();
                    return new ComboResult(ComboOutcome.Completed, creature);
                }

                canStillBecomeOne = true;
            }

            return canStillBecomeOne ? KeepBuilding() : Fail();
        }

        /// <summary>Counts down the time allowed for the next note.</summary>
        public ComboResult Tick(float deltaTime)
        {
            if (_notes.Count == 0)
            {
                return default;
            }

            _secondsLeft -= deltaTime;
            return _secondsLeft > 0f ? default : Fail();
        }

        /// <summary>Drops the combo being played, without it counting as a failure.</summary>
        public void Clear() => _notes.Clear();

        private static void CheckCombos(IReadOnlyList<CreatureType> creatures)
        {
            foreach (CreatureType creature in creatures)
            {
                CheckNotes(creature);

                foreach (CreatureType other in creatures)
                {
                    if (creature != other && Starts(creature.Combo, other.Combo))
                    {
                        throw new ArgumentException(
                            $"{creature.Name}'s combo is the start of {other.Name}'s, so {other.Name} could never be summoned.",
                            nameof(creatures));
                    }
                }
            }
        }

        private static void CheckNotes(CreatureType creature)
        {
            if (creature.Combo.Count == 0)
            {
                throw new ArgumentException($"{creature.Name} has no combo.", nameof(creature));
            }

            foreach (int note in creature.Combo)
            {
                if (note < 0 || note >= NoteCount)
                {
                    throw new ArgumentException($"{creature.Name}'s combo has note {note}, which is not on the flute.", nameof(creature));
                }
            }
        }

        private static bool Starts(IReadOnlyList<int> start, IReadOnlyList<int> whole)
        {
            if (start.Count > whole.Count)
            {
                return false;
            }

            for (int index = 0; index < start.Count; index++)
            {
                if (start[index] != whole[index])
                {
                    return false;
                }
            }

            return true;
        }

        private ComboResult KeepBuilding()
        {
            _secondsLeft = _config.SecondsToNextNote;
            return new ComboResult(ComboOutcome.Building);
        }

        private ComboResult Fail()
        {
            Clear();
            return new ComboResult(ComboOutcome.Failed);
        }
    }
}
