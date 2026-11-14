using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>One optional recorded sound: its clips, volume, pitch variation, how many at once and how they are spaced.</summary>
    [Serializable]
    public struct GameSoundSlot
    {
        [SerializeField, Tooltip("The clips; one is picked each time. Empty means the moment keeps the game's own sound.")]
        private AudioClip[] _clips;

        [SerializeField, Range(0f, 1f), Tooltip("The volume, before the library's master volume.")]
        private float _volume;

        [SerializeField, Range(0f, 0.5f), Tooltip("How far the pitch varies either way, as a share.")]
        private float _pitchJitter;

        [SerializeField, Min(1), Tooltip("How many can sound at once. A new one takes the oldest's voice.")]
        private int _mostAtOnce;

        [SerializeField, Min(0f), Tooltip("The fewest seconds between two of them, which thins out a burst.")]
        private float _leastGapSeconds;

        public GameSoundSlot(AudioClip[] clips, float volume, float pitchJitter, int mostAtOnce, float leastGapSeconds)
        {
            _clips = clips;
            _volume = volume;
            _pitchJitter = pitchJitter;
            _mostAtOnce = mostAtOnce;
            _leastGapSeconds = leastGapSeconds;
        }

        public AudioClip[] Clips => _clips;
        public float Volume => _volume;
        public float PitchJitter => _pitchJitter;
        public int MostAtOnce => _mostAtOnce;
        public float LeastGapSeconds => _leastGapSeconds;

        /// <summary>True when the slot has at least one clip.</summary>
        public bool IsSet => _clips != null && _clips.Length > 0 && _clips[0] != null;
    }
}
