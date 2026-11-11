using System;
using System.Collections.Generic;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Which swarm songs the player knows. A song is known from the start, or the first time it is really cast.
    /// A song can also be revealed: its notes may be shown, but it is not known until it is cast.
    /// The song table fills in (plan section N): for each song it keeps how many notes of its beginning the player has
    /// played in order at least once, for the whole match, even when the song itself was refused. A collected nest hums
    /// (plan section H): it fills in the first notes of a song the player has not found, never the whole song.
    /// Nothing is saved: every match starts again.
    /// </summary>
    public sealed class SongDiscovery
    {
        private readonly IReadOnlyList<SwarmSongEffect> _songs;
        private readonly bool[] _known;
        private readonly bool[] _revealed;
        private readonly int[] _heardNotes;

        private int _knownCount;

        public SongDiscovery(IReadOnlyList<SwarmSongEffect> songs)
        {
            _songs = songs ?? throw new ArgumentNullException(nameof(songs));
            _known = new bool[songs.Count];
            _revealed = new bool[songs.Count];
            _heardNotes = new int[songs.Count];

            for (int song = 0; song < songs.Count; song++)
            {
                if (songs[song] == null)
                {
                    throw new ArgumentException($"Song {song} is missing.", nameof(songs));
                }

                if (songs[song].KnownFromStart)
                {
                    _known[song] = true;
                    _revealed[song] = true;
                    _knownCount++;
                }
            }
        }

        /// <summary>How many songs there are.</summary>
        public int Count => _songs.Count;

        /// <summary>How many songs the player knows.</summary>
        public int KnownCount => _knownCount;

        /// <summary>Raised with the song's index the first time the song is cast.</summary>
        public event Action<int> Discovered;

        /// <summary>Raised with the song's index when its notes are revealed before it is known.</summary>
        public event Action<int> Revealed;

        /// <summary>Raised with the song's index when a hum fills in more of its beginning on the song table.</summary>
        public event Action<int> Hummed;

        /// <summary>True when the player knows the song.</summary>
        public bool IsKnown(int song)
        {
            RequireSong(song);
            return _known[song];
        }

        /// <summary>True when the song is known or revealed, so its notes may be shown.</summary>
        public bool IsRevealed(int song)
        {
            RequireSong(song);
            return _revealed[song];
        }

        /// <summary>
        /// How many notes of the song's beginning the player has played in order at least once, for the song table: the rest
        /// show as dots. It never falls. A known song returns its whole length.
        /// </summary>
        public int KnownNotes(int song)
        {
            RequireSong(song);
            return _known[song] ? _songs[song].Song.Keys.Count : _heardNotes[song];
        }

        /// <summary>A song was really cast. Makes it known, and raises <see cref="Discovered"/> the first time.</summary>
        public void NoteCast(int song)
        {
            RequireSong(song);

            if (_known[song])
            {
                return;
            }

            _known[song] = true;
            _revealed[song] = true;
            _knownCount++;
            Discovered?.Invoke(song);
        }

        /// <summary>Shows the song's notes without making it known. Raises <see cref="Revealed"/> the first time.</summary>
        public void Reveal(int song)
        {
            RequireSong(song);

            if (_revealed[song])
            {
                return;
            }

            _revealed[song] = true;
            Revealed?.Invoke(song);
        }

        /// <summary>
        /// A nest hums the song: the first <paramref name="notes"/> notes of its beginning fill in on the song table, but
        /// never the whole song, which only a cast makes known. Raises <see cref="Hummed"/> when that fills in more than
        /// the table showed; a known song, or one whose table already shows as much, changes nothing. It never falls.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">There is no such song, or the notes are negative.</exception>
        public void Hum(int song, int notes)
        {
            RequireSong(song);
            if (notes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(notes), notes, "A hum cannot take notes away.");
            }

            if (!WouldHumTeach(song, notes))
            {
                return;
            }

            _heardNotes[song] = HummedLength(song, notes);
            Hummed?.Invoke(song);
        }

        /// <summary>
        /// A nest hums one song the player has not found, chosen by <paramref name="random"/> among those whose table the hum
        /// would fill in further, and returns its index. Returns <see cref="SongBook.NoSong"/> and hums nothing when no song
        /// would learn from it, as when every song is found. It never allocates.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The notes are negative.</exception>
        public int HumUnfound(IRandomSource random, int notes)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (notes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(notes), notes, "A hum cannot take notes away.");
            }

            int candidates = 0;
            for (int song = 0; song < _songs.Count; song++)
            {
                if (WouldHumTeach(song, notes))
                {
                    candidates++;
                }
            }

            if (candidates == 0)
            {
                return SongBook.NoSong;
            }

            int pick = Math.Min(candidates - 1, (int)(random.NextDouble() * candidates));
            for (int song = 0; song < _songs.Count; song++)
            {
                if (!WouldHumTeach(song, notes))
                {
                    continue;
                }

                if (pick == 0)
                {
                    Hum(song, notes);
                    return song;
                }

                pick--;
            }

            throw new InvalidOperationException("The hum's candidates changed while it chose.");
        }

        /// <summary>
        /// Fills in the song table from the caster's recent notes. Call it after every note the caster hears
        /// (<see cref="SwarmSongCaster.NotePlayed"/>), before the note can cast a song and use the notes up: for each song, the
        /// longest beginning those notes end with is kept, so a beginning reached inside a longer run counts too. It never
        /// allocates.
        /// </summary>
        public void RecordNotes(SwarmSongCaster caster)
        {
            if (caster == null)
            {
                throw new ArgumentNullException(nameof(caster));
            }

            int first = FirstFreshNote(caster);
            int fresh = caster.RecentNoteCount - first;

            for (int song = 0; song < _songs.Count; song++)
            {
                IReadOnlyList<int> keys = _songs[song].Song.Keys;
                int longest = Math.Min(fresh, keys.Count);

                for (int length = longest; length > _heardNotes[song]; length--)
                {
                    if (EndsWithPrefix(caster, first + fresh - length, keys, length))
                    {
                        _heardNotes[song] = length;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// How many of the caster's most recent notes are the beginning of some song, from 1 to one less than the song's length.
        /// 0 when none are. Notes that are about to be forgotten do not count. It never allocates.
        /// </summary>
        public int HintNotes(SwarmSongCaster caster)
        {
            if (caster == null)
            {
                throw new ArgumentNullException(nameof(caster));
            }

            int first = FirstFreshNote(caster);
            int valid = caster.RecentNoteCount - first;
            int best = 0;

            for (int song = 0; song < _songs.Count; song++)
            {
                IReadOnlyList<int> keys = _songs[song].Song.Keys;
                int longest = Math.Min(valid, keys.Count - 1);

                for (int length = longest; length > best; length--)
                {
                    if (EndsWithPrefix(caster, first + valid - length, keys, length))
                    {
                        best = length;
                        break;
                    }
                }
            }

            return best;
        }

        // The notes a hum may show: never the whole song, so the cast is still the player's to find.
        private int HummedLength(int song, int notes)
        {
            return Math.Min(notes, _songs[song].Song.Keys.Count - 1);
        }

        private bool WouldHumTeach(int song, int notes)
        {
            return !_known[song] && HummedLength(song, notes) > _heardNotes[song];
        }

        // The oldest of the caster's notes that is not about to be forgotten.
        private static int FirstFreshNote(SwarmSongCaster caster)
        {
            int first = 0;
            while (first < caster.RecentNoteCount && caster.RecentNoteAge01(first) >= 1f)
            {
                first++;
            }

            return first;
        }

        private static bool EndsWithPrefix(SwarmSongCaster caster, int start, IReadOnlyList<int> keys, int length)
        {
            for (int note = 0; note < length; note++)
            {
                if (caster.RecentNoteHole(start + note) != keys[note])
                {
                    return false;
                }
            }

            return true;
        }

        private void RequireSong(int song)
        {
            if (song < 0 || song >= _songs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(song), song, "There is no song at that index.");
            }
        }
    }
}
