using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The band behind every combo's tune. The flute's notes all belong to the first chord's scale, so
    /// they sound right in any order, and the chords all fit those notes.
    /// </summary>
    [Serializable]
    public sealed class SongBand
    {
        [SerializeField, Tooltip("The pitch of each flute note, in order, in hertz.")]
        private float[] _noteHertz = { 440f, 523.25f, 659.25f, 784f };

        [SerializeField, Tooltip("The chords, one to a bar, starting again after the last. A tune ends on the first.")]
        private SongChord[] _chords =
        {
            new SongChord(110f, 261.63f, 329.63f, 440f),
            new SongChord(87.31f, 261.63f, 349.23f, 440f),
            new SongChord(130.81f, 261.63f, 329.63f, 392f),
            new SongChord(98f, 246.94f, 293.66f, 392f),
        };

        [SerializeField, Range(0f, 1f), Tooltip("How loud the tune is.")]
        private float _leadVolume = 0.22f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the soft octave above the tune is.")]
        private float _airVolume = 0.05f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud each note of a chord is.")]
        private float _chordVolume = 0.04f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the bass is.")]
        private float _bassVolume = 0.18f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the kick drum is at full drums.")]
        private float _kickVolume = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the snare is at full drums.")]
        private float _snareVolume = 0.18f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the hi-hat is at full drums.")]
        private float _hatVolume = 0.07f;

        public IReadOnlyList<float> NoteHertz => _noteHertz;
        public IReadOnlyList<SongChord> Chords => _chords;
        public float LeadVolume => _leadVolume;
        public float AirVolume => _airVolume;
        public float ChordVolume => _chordVolume;
        public float BassVolume => _bassVolume;
        public float KickVolume => _kickVolume;
        public float SnareVolume => _snareVolume;
        public float HatVolume => _hatVolume;
    }
}
