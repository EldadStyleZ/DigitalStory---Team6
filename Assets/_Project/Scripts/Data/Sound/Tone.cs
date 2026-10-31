using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>One short sound: a wave that slides between two pitches and dies away.</summary>
    [Serializable]
    public struct Tone
    {
        [SerializeField, Tooltip("The wave it is made of.")]
        private ToneShape _shape;

        [SerializeField, Min(1f), Tooltip("The pitch it starts on, in hertz. For noise, the brightest it gets.")]
        private float _startHertz;

        [SerializeField, Min(1f), Tooltip("The pitch it slides to, in hertz.")]
        private float _endHertz;

        [SerializeField, Min(0.001f), Tooltip("How long it lasts, in seconds.")]
        private float _seconds;

        [SerializeField, Range(0f, 1f), Tooltip("How loud it starts.")]
        private float _volume;

        [SerializeField, Min(0f), Tooltip("Seconds after the sound begins that this tone starts.")]
        private float _delay;

        public Tone(ToneShape shape, float startHertz, float endHertz, float seconds, float volume, float delay = 0f)
        {
            _shape = shape;
            _startHertz = startHertz;
            _endHertz = endHertz;
            _seconds = seconds;
            _volume = volume;
            _delay = delay;
        }

        public ToneShape Shape => _shape;
        public float StartHertz => _startHertz;
        public float EndHertz => _endHertz;
        public float Seconds => _seconds;
        public float Volume => _volume;
        public float Delay => _delay;
        public float EndsAt => _delay + _seconds;
    }
}
