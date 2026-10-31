using System;
using System.Collections.Generic;
using System.Linq;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>Remembers recent notes and works out when they cast a song.</summary>
    public sealed class SongBook
    {
        public const int NoSong = -1;

        private readonly SongDefinition[] _songs;
        private readonly int[] _longestFirst;
        private readonly float[] _cooldowns;
        private readonly int[] _noteHoles;
        private readonly float[] _noteAges;
        private readonly float _noteWindow;

        private int _noteCount;

        public SongBook(IReadOnlyList<SongDefinition> songs, SongConfig config, int holeCount)
        {
            if (songs == null)
            {
                throw new ArgumentNullException(nameof(songs));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            foreach (SongDefinition song in songs)
            {
                Validate(song, config.NotesRemembered, holeCount);
            }

            _songs = songs.ToArray();
            _longestFirst = Enumerable.Range(0, _songs.Length).OrderByDescending(song => _songs[song].Keys.Count).ToArray();
            _cooldowns = new float[_songs.Length];
            _noteHoles = new int[config.NotesRemembered];
            _noteAges = new float[config.NotesRemembered];
            _noteWindow = config.NoteWindowSeconds;
        }

        public int Count => _songs.Length;

        public SongDefinition this[int song] => _songs[song];

        public bool IsCoolingDown(int song) => _cooldowns[song] > 0f;

        /// <summary>Advances note ages and cooldowns by one step of game time.</summary>
        public void Tick(float deltaTime)
        {
            for (int song = 0; song < _cooldowns.Length; song++)
            {
                _cooldowns[song] = Math.Max(0f, _cooldowns[song] - deltaTime);
            }

            for (int note = 0; note < _noteCount; note++)
            {
                _noteAges[note] += deltaTime;
            }
        }

        /// <summary>Records a played note and returns the song it cast, or <see cref="NoSong"/>.</summary>
        public int Press(int hole)
        {
            ForgetExpiredNotes();
            Remember(hole);

            foreach (int song in _longestFirst)
            {
                if (!IsCoolingDown(song) && IsPlayed(_songs[song]))
                {
                    _cooldowns[song] = _songs[song].CooldownSeconds;
                    _noteCount = 0;
                    return song;
                }
            }

            return NoSong;
        }

        private static void Validate(SongDefinition song, int notesRemembered, int holeCount)
        {
            int keyCount = song.Keys.Count;

            if (keyCount == 0 || keyCount > notesRemembered)
            {
                throw new ArgumentException($"{song.Title} has {keyCount} keys, but a combo needs 1 to {notesRemembered}.");
            }

            foreach (int key in song.Keys)
            {
                if (key < 0 || key >= holeCount)
                {
                    throw new ArgumentException($"{song.Title} uses hole {key}, but the flute has holes 0 to {holeCount - 1}.");
                }
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

        private void Remember(int hole)
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

        private bool IsPlayed(SongDefinition song)
        {
            int keyCount = song.Keys.Count;

            if (_noteCount < keyCount)
            {
                return false;
            }

            int firstNote = _noteCount - keyCount;

            for (int key = 0; key < keyCount; key++)
            {
                if (_noteHoles[firstNote + key] != song.Keys[key])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
