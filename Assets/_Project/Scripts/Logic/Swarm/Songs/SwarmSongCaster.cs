using System;
using System.Collections.Generic;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Turns flute notes into swarm songs. It wraps <see cref="SongBook"/>, adds the swarm's rules
    /// and keeps what the HUD shows: recent notes and cooldowns.
    /// </summary>
    /// <remarks>
    /// Nothing is cast, or even remembered, while the swarm is collapsed. A song whose
    /// <see cref="SwarmSongEffect.CanCast"/> is false still uses up the notes and starts its cooldown.
    /// </remarks>
    public sealed class SwarmSongCaster
    {
        private readonly IReadOnlyList<SwarmSongEffect> _songs;
        private readonly SongBook _book;
        private readonly float[] _cooldownLeft;
        private readonly int[] _noteHoles;
        private readonly float[] _noteAges;
        private readonly float _noteWindow;

        private int _noteCount;

        public SwarmSongCaster(IReadOnlyList<SwarmSongEffect> songs, SongConfig config, int holeCount)
        {
            _songs = songs ?? throw new ArgumentNullException(nameof(songs));

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (holeCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(holeCount), holeCount, "The flute needs at least one hole.");
            }

            var definitions = new List<SongDefinition>(songs.Count);

            for (int i = 0; i < songs.Count; i++)
            {
                if (songs[i] == null || songs[i].Song == null)
                {
                    throw new ArgumentException($"Song {i} is missing or has no definition.", nameof(songs));
                }

                definitions.Add(songs[i].Song);
            }

            _book = new SongBook(definitions, config, holeCount);
            _cooldownLeft = new float[songs.Count];
            _noteHoles = new int[config.NotesRemembered];
            _noteAges = new float[config.NotesRemembered];
            _noteWindow = config.NoteWindowSeconds;
        }

        /// <summary>How many songs there are.</summary>
        public int Count => _songs.Count;

        /// <summary>The song at an index.</summary>
        public SwarmSongEffect this[int song] => _songs[song];

        /// <summary>How many recent notes the flute remembers right now.</summary>
        public int RecentNoteCount => _noteCount;

        /// <summary>Raised with the song's index when a song is cast.</summary>
        public event Action<int> SongCast;

        /// <summary>Raised with the hole when a note is played.</summary>
        public event Action<int> NotePlayed;

        /// <summary>1 just after the song was cast, 0 when it is ready.</summary>
        public float CooldownFraction(int song)
        {
            float total = _songs[song].Song.CooldownSeconds;
            return total > 0f ? Math.Min(1f, _cooldownLeft[song] / total) : 0f;
        }

        /// <summary>The hole of a recent note, 0 being the oldest.</summary>
        public int RecentNoteHole(int index)
        {
            RequireNote(index);
            return _noteHoles[index];
        }

        /// <summary>How old a recent note is, from 0 (new) to 1 (about to be forgotten).</summary>
        public float RecentNoteAge01(int index)
        {
            RequireNote(index);
            return _noteWindow > 0f ? Math.Min(1f, _noteAges[index] / _noteWindow) : 1f;
        }

        /// <summary>Plays a note. Returns the index of the song it cast, or <see cref="SongBook.NoSong"/>.</summary>
        public int Press(int hole, in SwarmSongContext context)
        {
            if (context.Swarm == null)
            {
                throw new ArgumentException("The song context has no swarm.", nameof(context));
            }

            if (context.Swarm.IsCollapsed || (context.Forms != null && context.Forms.Current == SwarmForm.Collapsed))
            {
                return SongBook.NoSong;
            }

            ForgetExpiredNotes();
            RememberNote(hole);
            NotePlayed?.Invoke(hole);

            int song = _book.Press(hole);

            if (song == SongBook.NoSong)
            {
                return SongBook.NoSong;
            }

            _noteCount = 0;
            _cooldownLeft[song] = _songs[song].Song.CooldownSeconds;

            SwarmSongEffect effect = _songs[song];

            if (!effect.CanCast(in context))
            {
                return SongBook.NoSong;
            }

            effect.Cast(in context);
            SongCast?.Invoke(song);
            return song;
        }

        /// <summary>Advances note ages and cooldowns.</summary>
        public void Tick(float deltaTime)
        {
            _book.Tick(deltaTime);

            for (int song = 0; song < _cooldownLeft.Length; song++)
            {
                _cooldownLeft[song] = Math.Max(0f, _cooldownLeft[song] - deltaTime);
            }

            for (int note = 0; note < _noteCount; note++)
            {
                _noteAges[note] += deltaTime;
            }
        }

        private void RequireNote(int index)
        {
            if (index < 0 || index >= _noteCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "There is no recent note at that index.");
            }
        }

        private void ForgetExpiredNotes()
        {
            int expired = 0;

            while (expired < _noteCount && _noteAges[expired] >= _noteWindow)
            {
                expired++;
            }

            DropOldest(expired);
        }

        private void RememberNote(int hole)
        {
            if (_noteCount == _noteHoles.Length)
            {
                DropOldest(1);
            }

            _noteHoles[_noteCount] = hole;
            _noteAges[_noteCount] = 0f;
            _noteCount++;
        }

        private void DropOldest(int count)
        {
            if (count == 0)
            {
                return;
            }

            int kept = _noteCount - count;
            Array.Copy(_noteHoles, count, _noteHoles, 0, kept);
            Array.Copy(_noteAges, count, _noteAges, 0, kept);
            _noteCount = kept;
        }
    }
}
