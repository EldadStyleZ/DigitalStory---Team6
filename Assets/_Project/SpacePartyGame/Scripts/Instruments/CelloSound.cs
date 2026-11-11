using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// How the cello sounds. Its six notes are the toy flute's, one octave lower (D4 to D5). [Serializable], so it
    /// shows as a group of fields in the instrument's settings asset.
    /// </summary>
    [Serializable]
    public sealed class CelloSound
    {
        [SerializeField, Tooltip("The pitch of key 1, in hertz: the flute's D5 an octave down.")]
        private float _baseHertz = 293.665f;

        [SerializeField, Tooltip("How many semitones above key 1 each key sounds: the flute's holes.")]
        private int[] _semitones = { 0, 2, 5, 7, 9, 12 };

        [SerializeField, Tooltip("How quickly the pitch wobbles, in wobbles per second. Slower than the flute's.")]
        private float _vibratoHertz = 5f;

        [SerializeField, Tooltip("How far the pitch wobbles, in hertz.")]
        private float _vibratoDepthHertz = 2.5f;

        [SerializeField, Tooltip("Sound above this is softened, which turns the bow's buzz into a warm tone, in hertz.")]
        private float _brightnessHertz = 1600f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the bow's scratch is, next to the note.")]
        private float _bowNoiseShare = 0.03f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud a held note is.")]
        private float _volume = 0.16f;

        [SerializeField, Tooltip("How long a note takes to swell in as the bow starts, in seconds.")]
        private float _attackSeconds = 0.06f;

        [SerializeField, Tooltip("How quickly the pitch slides from one note to the next, in seconds.")]
        private float _glideSeconds = 0.02f;

        [SerializeField, Tooltip("How quickly a released note fades out, in seconds.")]
        private float _fadeSeconds = 0.08f;

        public int KeyCount => _semitones.Length;
        public float VibratoHertz => _vibratoHertz;
        public float VibratoDepthHertz => _vibratoDepthHertz;
        public float BrightnessHertz => _brightnessHertz;
        public float BowNoiseShare => _bowNoiseShare;
        public float Volume => _volume;
        public float AttackSeconds => _attackSeconds;
        public float GlideSeconds => _glideSeconds;
        public float FadeSeconds => _fadeSeconds;

        /// <summary>The pitch of a key from 1 to 6, in hertz.</summary>
        public float HertzOf(int key) => _baseHertz * Mathf.Pow(2f, _semitones[key - 1] / 12f);
    }
}
