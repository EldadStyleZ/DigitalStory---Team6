using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// One optional recorded sound (plan section AC): its clips, one picked each time; its volume and pitch variation; how many
    /// can sound at once and how close together; and its priority when the engine runs short of voices.
    /// </summary>
    [Serializable]
    public struct GameSoundSlot
    {
        [SerializeField, Tooltip("The clips; one is picked each time, never the same twice in a row. Empty means the moment keeps the game's own sound.")]
        private AudioClip[] _clips;

        [SerializeField, Range(0f, 1f), Tooltip("The volume, before the library's master volume.")]
        private float _volume;

        [SerializeField, Range(0f, 0.5f), Tooltip("How far the pitch varies either way, as a share.")]
        private float _pitchJitter;

        [SerializeField, Min(1), Tooltip("How many can sound at once. A new one takes the voice of the one that started longest ago.")]
        private int _mostAtOnce;

        [SerializeField, Min(0f), Tooltip("The fewest seconds between two of them, which thins out a burst. 0 lets every one play.")]
        private float _leastGapSeconds;

        [SerializeField, Range(0, 256), Tooltip("The AudioSource priority: 0 is the most important. When the engine runs out of voices, the least important are silenced first.")]
        private int _priority;

        public GameSoundSlot(AudioClip[] clips, float volume, float pitchJitter, int mostAtOnce, float leastGapSeconds, int priority)
        {
            _clips = clips;
            _volume = volume;
            _pitchJitter = pitchJitter;
            _mostAtOnce = mostAtOnce;
            _leastGapSeconds = leastGapSeconds;
            _priority = priority;
        }

        public AudioClip[] Clips => _clips;
        public float Volume => _volume;
        public float PitchJitter => _pitchJitter;
        public int MostAtOnce => _mostAtOnce;
        public float LeastGapSeconds => _leastGapSeconds;
        public int Priority => _priority;

        /// <summary>True when the slot has at least one clip.</summary>
        public bool IsSet
        {
            get
            {
                if (_clips == null)
                {
                    return false;
                }

                for (int index = 0; index < _clips.Length; index++)
                {
                    if (_clips[index] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
