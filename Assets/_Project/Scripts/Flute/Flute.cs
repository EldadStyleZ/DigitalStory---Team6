using System;
using System.Collections.Generic;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// The Piper's flute with six holes. Holding a hole sounds its note, and what the rats do depends on the hole:
    /// 1 and 2 call them, 3 and 4 march them, 5 and 6 scare them. A few notes in a row can spell a song.
    /// </summary>
    /// <remarks>
    /// The flute does not read the keyboard; PiperInput does that and calls <see cref="Press"/> and
    /// <see cref="Release"/>. It does not make the note's sound either: the FluteVoice plays
    /// <see cref="SoundingKey"/>. Other objects learn what happened through the two events, without the flute
    /// knowing who listens.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Flute : MonoBehaviour
    {
        public const int HoleCount = 6;

        [Header("Sound")]
        [SerializeField, Tooltip("Plays each song's tune once.")]
        private AudioSource _songSpeaker;

        [Header("Music")]
        [SerializeField, Min(0f), Tooltip("How far the rats hear the held notes, in units.")]
        private float _hearingRadius = 4f;

        [SerializeField, Tooltip("The ring that shows how far the music reaches. It shows while a hole is held.")]
        private GameObject _musicRing;

        [Header("Songs")]
        [SerializeField, Tooltip("Every song the flute can play.")]
        private Song[] _songs = Array.Empty<Song>();

        [SerializeField, Min(0.1f), Tooltip("How quickly a song's notes must follow each other, in seconds.")]
        private float _comboSeconds = 3f;

        private readonly bool[] _isHeld = new bool[HoleCount];
        private readonly Dictionary<Song, float> _restingUntil = new Dictionary<Song, float>();
        private ComboReader _combo;
        private int _soundingKey;

        /// <summary>Raised with the key, from 1 to 6, each time a hole is pressed.</summary>
        public event Action<int> NotePlayed;

        /// <summary>Raised when the notes spell a song that is not resting.</summary>
        public event Action<Song> SongPlayed;

        public float HearingRadius => _hearingRadius;
        public IReadOnlyList<Song> Songs => _songs;

        /// <summary>The notes that could still become a song, oldest first.</summary>
        public IReadOnlyList<int> RecentKeys => _combo.Keys;

        /// <summary>The key, from 1 to 6, whose note sounds now, or 0 for silence. The note pressed last wins.</summary>
        public int SoundingKey => _soundingKey;

        /// <summary>What the held note does to the rats.</summary>
        public FluteMode Mode => _soundingKey == 0 ? FluteMode.Silent : ModeOf(_soundingKey);

        private void Awake()
        {
            _combo = new ComboReader(_comboSeconds);
            FitTheRing();
        }

        // Unity calls OnValidate when a value changes in the Inspector, so the ring follows the range as you edit it.
        private void OnValidate()
        {
            FitTheRing();
        }

        private void FitTheRing()
        {
            if (_musicRing != null && _musicRing.TryGetComponent(out SpriteRenderer ring) && ring.sprite != null)
            {
                float across = _hearingRadius * 2f / ring.sprite.bounds.size.x;
                _musicRing.transform.localScale = new Vector3(across, across, 1f);
            }
        }

        private void Update()
        {
            if (_musicRing != null)
            {
                _musicRing.SetActive(Mode != FluteMode.Silent);
            }
        }

        /// <summary>Opens a hole: plays its note, and the song it finishes if there is one.</summary>
        public void Press(int key)
        {
            if (key < 1 || key > HoleCount || _isHeld[key - 1])
            {
                return;
            }

            _isHeld[key - 1] = true;
            _soundingKey = key;
            NotePlayed?.Invoke(key);

            _combo.Add(key, Time.time);
            Song song = _combo.FindSong(_songs);

            if (song != null && IsReady(song))
            {
                Cast(song);
            }
        }

        /// <summary>Closes a hole. Another hole still held takes over the sound.</summary>
        public void Release(int key)
        {
            if (key < 1 || key > HoleCount)
            {
                return;
            }

            _isHeld[key - 1] = false;

            if (key == _soundingKey)
            {
                _soundingKey = HighestHeldKey();
            }
        }

        public bool IsHeld(int key) => key >= 1 && key <= HoleCount && _isHeld[key - 1];

        /// <summary>True unless the song was played moments ago and is still resting.</summary>
        public bool IsReady(Song song) => SecondsUntilReady(song) <= 0f;

        public float SecondsUntilReady(Song song)
        {
            return _restingUntil.TryGetValue(song, out float until) ? Mathf.Max(0f, until - Time.time) : 0f;
        }

        private static FluteMode ModeOf(int key)
        {
            if (key <= 2)
            {
                return FluteMode.Call;
            }

            return key <= 4 ? FluteMode.March : FluteMode.Scare;
        }

        private void Cast(Song song)
        {
            _restingUntil[song] = Time.time + song.RestSeconds;
            _combo.Clear();

            if (song.Sound != null)
            {
                _songSpeaker.PlayOneShot(song.Sound);
            }

            SongPlayed?.Invoke(song);
        }

        private int HighestHeldKey()
        {
            for (int key = HoleCount; key >= 1; key--)
            {
                if (_isHeld[key - 1])
                {
                    return key;
                }
            }

            return 0;
        }
    }
}
