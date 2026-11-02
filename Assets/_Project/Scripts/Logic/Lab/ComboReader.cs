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
        private readonly List<IComboSong> _songs;
        private readonly List<int> _notes = new List<int>();
        private readonly List<IComboSong> _candidates = new List<IComboSong>();

        private float _secondsLeft;

        public ComboReader(ComboConfig config, IReadOnlyList<IComboSong> songs)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _songs = new List<IComboSong>(songs ?? throw new ArgumentNullException(nameof(songs)));
            CheckCombos(songs);
        }

        /// <summary>The notes of the combo being played.</summary>
        public IReadOnlyList<int> Notes => _notes;

        /// <summary>Every known combo the notes so far could still become.</summary>
        public IReadOnlyList<IComboSong> Candidates => _candidates;

        /// <summary>Adds a combo the Piper can play.</summary>
        public void Learn(IComboSong song)
        {
            CheckNotes(song);

            foreach (IComboSong known in _songs)
            {
                CheckClash(song, known, nameof(song));
                CheckClash(known, song, nameof(song));
            }

            _songs.Add(song);
        }

        /// <summary>Adds a note to the combo being played.</summary>
        /// <param name="level">The Piper's level, which decides which combos are known.</param>
        public ComboResult Press(int note, int level)
        {
            _notes.Add(note);
            _candidates.Clear();

            foreach (IComboSong song in _songs)
            {
                if (song.UnlockLevel > level || !Starts(_notes, song.Combo))
                {
                    continue;
                }

                if (song.Combo.Count == _notes.Count)
                {
                    Clear();
                    return new ComboResult(ComboOutcome.Completed, song);
                }

                _candidates.Add(song);
            }

            return _candidates.Count > 0 ? KeepBuilding() : Fail();
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
        public void Clear()
        {
            _notes.Clear();
            _candidates.Clear();
        }

        private static void CheckCombos(IReadOnlyList<IComboSong> songs)
        {
            foreach (IComboSong song in songs)
            {
                CheckNotes(song);

                foreach (IComboSong other in songs)
                {
                    if (!ReferenceEquals(song, other))
                    {
                        CheckClash(song, other, nameof(songs));
                    }
                }
            }
        }

        private static void CheckClash(IComboSong shorter, IComboSong longer, string parameter)
        {
            if (Starts(shorter.Combo, longer.Combo))
            {
                throw new ArgumentException(
                    $"{shorter.Name}'s combo is the start of {longer.Name}'s, so {longer.Name} could never be played.",
                    parameter);
            }
        }

        private static void CheckNotes(IComboSong song)
        {
            if (song.Combo.Count == 0)
            {
                throw new ArgumentException($"{song.Name} has no combo.", nameof(song));
            }

            foreach (int note in song.Combo)
            {
                if (note < 0 || note >= NoteCount)
                {
                    throw new ArgumentException($"{song.Name}'s combo has note {note}, which is not on the flute.", nameof(song));
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
