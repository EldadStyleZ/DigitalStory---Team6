using System;
using UnityEngine;

namespace PiperToy
{
    /// <summary>
    /// How the flute sounds. The numbers are the old game's, so the toy's flute sounds the same. It is
    /// [Serializable], so it shows in the Flute Voice's Inspector as a group of fields that can be tuned.
    /// </summary>
    [Serializable]
    public sealed class FluteSound
    {
        [SerializeField, Tooltip("The pitch of hole 1, in hertz.")]
        private float _baseHertz = 587.33f;

        [SerializeField, Tooltip("How many semitones above hole 1 each hole sounds.")]
        private int[] _semitones = { 0, 2, 5, 7, 9, 12 };

        [SerializeField, Tooltip("How quickly the pitch wobbles, in wobbles per second.")]
        private float _vibratoHertz = 5.3f;

        [SerializeField, Tooltip("How far the pitch wobbles, in hertz.")]
        private float _vibratoDepthHertz = 6f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the soft note an octave up is, next to the main one.")]
        private float _overtoneShare = 0.25f;

        [SerializeField, Tooltip("Sound above this is softened, so the flute is not harsh, in hertz.")]
        private float _brightnessHertz = 2800f;

        [SerializeField, Tooltip("The middle pitch of the breath noise, in hertz.")]
        private float _breathHertz = 1900f;

        [SerializeField, Range(0f, 1f), Tooltip("How wide the breath noise is around its pitch.")]
        private float _breathWidth = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud the breath is, next to the note.")]
        private float _breathShare = 0.12f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud a held note is.")]
        private float _volume = 0.13f;

        [SerializeField, Range(0f, 1f), Tooltip("How loud a note is for a moment as it starts, like a tongued note.")]
        private float _noteStartVolume = 0.06f;

        [SerializeField, Tooltip("How long that start lasts, in seconds.")]
        private float _noteStartSeconds = 0.035f;

        [SerializeField, Tooltip("How quickly a note dips to its start volume, in seconds.")]
        private float _noteDipSeconds = 0.006f;

        [SerializeField, Tooltip("How quickly it then grows to its full volume, in seconds.")]
        private float _noteRecoverSeconds = 0.02f;

        [SerializeField, Tooltip("How quickly the pitch slides from one note to the next, in seconds.")]
        private float _glideSeconds = 0.012f;

        [SerializeField, Tooltip("How quickly a released note fades out, in seconds. This is what stops it clicking.")]
        private float _fadeSeconds = 0.05f;

        public int HoleCount => _semitones.Length;
        public float VibratoHertz => _vibratoHertz;
        public float VibratoDepthHertz => _vibratoDepthHertz;
        public float OvertoneShare => _overtoneShare;
        public float BrightnessHertz => _brightnessHertz;
        public float BreathHertz => _breathHertz;
        public float BreathWidth => _breathWidth;
        public float BreathShare => _breathShare;
        public float Volume => _volume;
        public float NoteStartVolume => _noteStartVolume;
        public float NoteStartSeconds => _noteStartSeconds;
        public float NoteDipSeconds => _noteDipSeconds;
        public float NoteRecoverSeconds => _noteRecoverSeconds;
        public float GlideSeconds => _glideSeconds;
        public float FadeSeconds => _fadeSeconds;

        /// <summary>The pitch of a hole, with keys from 1 to 6.</summary>
        public float HertzOf(int key) => _baseHertz * Mathf.Pow(2f, _semitones[key - 1] / 12f);
    }
}
