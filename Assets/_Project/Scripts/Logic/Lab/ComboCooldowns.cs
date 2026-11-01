using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>Keeps each combo waiting after it is used.</summary>
    public sealed class ComboCooldowns
    {
        private readonly List<IComboSong> _songs;
        private readonly float[] _secondsLeft;

        public ComboCooldowns(IReadOnlyList<IComboSong> songs)
        {
            _songs = new List<IComboSong>(songs ?? throw new ArgumentNullException(nameof(songs)));
            _secondsLeft = new float[songs.Count];
        }

        public bool IsReady(IComboSong song) => SecondsLeft(song) <= 0f;

        public float SecondsLeft(IComboSong song) => _secondsLeft[_songs.IndexOf(song)];

        public void Start(IComboSong song)
        {
            _secondsLeft[_songs.IndexOf(song)] = song.CooldownSeconds;
        }

        public void Tick(float deltaTime)
        {
            for (int index = 0; index < _secondsLeft.Length; index++)
            {
                _secondsLeft[index] = Mathf.Max(0f, _secondsLeft[index] - deltaTime);
            }
        }

        public void Reset() => Array.Clear(_secondsLeft, 0, _secondsLeft.Length);
    }
}
