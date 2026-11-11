using System;
using System.Collections.Generic;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Turns flute notes into swarm songs. It wraps <see cref="SongBook"/>, adds the swarm's rules and keeps what the HUD
    /// shows: recent notes and cooldowns.
    /// </summary>
    /// <remarks>
    /// The book only matches notes: it is given the songs with no cooldown, and the caster keeps the cooldowns itself, so a
    /// song is refused before any cooldown starts. When a song's notes are completed but it cannot cast,
    /// <see cref="SongNotReady"/> is raised with the reason, in this order: the Colossus stands, the song cools down,
    /// <see cref="SwarmSongEffect.CanCast"/> is false (it needs Fury), or its price would take the swarm below its collapse
    /// line. A refused song still uses up its notes and starts no cooldown. A cast pays the song's price as it happens,
    /// unless songs are free (<see cref="SwarmSongContext.AreSongsFree"/>). Nothing is cast, or even remembered, while the
    /// swarm is collapsed.
    /// </remarks>
    public sealed class SwarmSongCaster
    {
        private readonly IReadOnlyList<SwarmSongEffect> _songs;
        private readonly SongBook _book;
        private readonly float[] _cooldownLeft;
        private readonly int[] _noteHoles;
        private readonly float[] _noteAges;
        private readonly int[] _missedHoles;
        private readonly float[] _missedAges;
        private readonly float _noteWindow;

        private int _noteCount;
        private int _missedCount;

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

                definitions.Add(WithoutCooldown(songs[i].Song));
            }

            _book = new SongBook(definitions, config, holeCount);
            _cooldownLeft = new float[songs.Count];
            _noteHoles = new int[config.NotesRemembered];
            _noteAges = new float[config.NotesRemembered];
            _missedHoles = new int[config.NotesRemembered];
            _missedAges = new float[config.NotesRemembered];
            _noteWindow = config.NoteWindowSeconds;
        }

        /// <summary>How many songs there are.</summary>
        public int Count => _songs.Count;

        /// <summary>The song at an index.</summary>
        public SwarmSongEffect this[int song] => _songs[song];

        /// <summary>How many recent notes the flute remembers right now.</summary>
        public int RecentNoteCount => _noteCount;

        /// <summary>Raised with the song's index when a song is cast, once its price is paid.</summary>
        public event Action<int> SongCast;

        /// <summary>Raised with the song's index and the reason when its notes were just completed but it could not cast.</summary>
        public event Action<int, SongRefusal> SongNotReady;

        /// <summary>Raised with the hole when a note is played, before the note can complete a song.</summary>
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
                RememberMissedNote(hole);
                int missed = LongestTailMatch(_missedHoles, _missedAges, _missedCount);

                if (missed != SongBook.NoSong)
                {
                    _missedCount = 0;
                    SongNotReady?.Invoke(missed, SongRefusal.Collapsed);
                }

                return SongBook.NoSong;
            }

            _missedCount = 0;
            ForgetExpiredNotes();
            RememberNote(hole);
            NotePlayed?.Invoke(hole);

            int song = _book.Press(hole);

            if (song == SongBook.NoSong)
            {
                return SongBook.NoSong;
            }

            _noteCount = 0;
            SwarmSongEffect effect = _songs[song];

            // The giant stands until its time is up: a song cannot replace it.
            if (context.Forms != null && context.Forms.Current == SwarmForm.Colossus)
            {
                return Refuse(song, SongRefusal.GiantStanding);
            }

            if (_cooldownLeft[song] > 0f)
            {
                return Refuse(song, SongRefusal.CoolingDown);
            }

            if (!effect.CanCast(in context))
            {
                return Refuse(song, SongRefusal.NeedsFury);
            }

            // Paid last, so a song refused for any other reason costs nothing. Paying never collapses the swarm.
            if (!context.AreSongsFree && !context.Swarm.TrySpend(effect.WaspCost))
            {
                return Refuse(song, SongRefusal.NotEnoughWasps);
            }

            _cooldownLeft[song] = effect.Song.CooldownSeconds;
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

            for (int note = 0; note < _missedCount; note++)
            {
                _missedAges[note] += deltaTime;
            }
        }

        // The book only matches notes; the caster keeps the cooldowns, so a refused song starts none.
        private static SongDefinition WithoutCooldown(SongDefinition song)
        {
            var keys = new int[song.Keys.Count];

            for (int key = 0; key < keys.Length; key++)
            {
                keys[key] = song.Keys[key];
            }

            return new SongDefinition(song.Title, song.Description, 0f, keys);
        }

        private int Refuse(int song, SongRefusal reason)
        {
            SongNotReady?.Invoke(song, reason);
            return SongBook.NoSong;
        }

        // The longest song whose keys end the given notes. Notes that are too old are ignored. It does not allocate.
        private int LongestTailMatch(int[] holes, float[] ages, int count)
        {
            int first = 0;

            while (first < count && ages[first] >= _noteWindow)
            {
                first++;
            }

            int total = count - first;
            int best = SongBook.NoSong;
            int bestLength = 0;

            for (int song = 0; song < _songs.Count; song++)
            {
                IReadOnlyList<int> keys = _songs[song].Song.Keys;
                int length = keys.Count;

                if (length > total || length <= bestLength)
                {
                    continue;
                }

                bool matches = true;

                for (int key = 0; key < length && matches; key++)
                {
                    matches = holes[count - length + key] == keys[key];
                }

                if (matches)
                {
                    best = song;
                    bestLength = length;
                }
            }

            return best;
        }

        // Notes played while the swarm is collapsed are not remembered for the HUD, but are kept apart to tell when a song was meant.
        private void RememberMissedNote(int hole)
        {
            int expired = 0;

            while (expired < _missedCount && _missedAges[expired] >= _noteWindow)
            {
                expired++;
            }

            if (expired == 0 && _missedCount == _missedHoles.Length)
            {
                expired = 1;
            }

            if (expired > 0)
            {
                int kept = _missedCount - expired;
                Array.Copy(_missedHoles, expired, _missedHoles, 0, kept);
                Array.Copy(_missedAges, expired, _missedAges, 0, kept);
                _missedCount = kept;
            }

            _missedHoles[_missedCount] = hole;
            _missedAges[_missedCount] = 0f;
            _missedCount++;
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
