using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How a combo's tune is played, so each one sounds like itself.</summary>
    [Serializable]
    public struct SongStyle
    {
        [SerializeField, Min(30f), Tooltip("How fast it is played, in beats per minute.")]
        private float _beatsPerMinute;

        [SerializeField, Tooltip("The wave of the tune.")]
        private ToneShape _lead;

        [SerializeField, Min(0.1f), Tooltip("Multiplies every note's pitch: 2 is an octave up and 0.5 an octave down.")]
        private float _pitch;

        [SerializeField, Min(0.25f), Tooltip("How many beats each note of the combo lasts.")]
        private float _beatsPerNote;

        [SerializeField, Min(1), Tooltip("How many times the combo's notes are played.")]
        private int _repeats;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the drums are. Zero leaves them out.")]
        private float _drums;

        public SongStyle(float beatsPerMinute, ToneShape lead, float pitch, float beatsPerNote, int repeats, float drums)
        {
            _beatsPerMinute = beatsPerMinute;
            _lead = lead;
            _pitch = pitch;
            _beatsPerNote = beatsPerNote;
            _repeats = repeats;
            _drums = drums;
        }

        public float BeatsPerMinute => _beatsPerMinute;
        public ToneShape Lead => _lead;
        public float Pitch => _pitch;
        public float BeatsPerNote => _beatsPerNote;
        public int Repeats => _repeats;
        public float Drums => _drums;
        public float SecondsPerBeat => 60f / _beatsPerMinute;
    }
}
