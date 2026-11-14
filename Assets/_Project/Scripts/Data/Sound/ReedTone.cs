using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// How the buzzing reed voice sounds (plan section AC): a band-limited saw mixed with a narrow pulse, softened by a gentle
    /// low-pass, with a slight vibrato, so the wasps seem to hum the tune. It sustains while a key is held.
    /// </summary>
    [Serializable]
    public struct ReedTone
    {
        [SerializeField, Range(0f, 1f), Tooltip("The volume while a note holds.")]
        private float _volume;

        [SerializeField, Min(0.0005f), Tooltip("Seconds a note takes to swell to about two thirds of its volume.")]
        private float _attackSeconds;

        [SerializeField, Min(0.0005f), Tooltip("Seconds it takes to fade to about a third once the note ends.")]
        private float _releaseSeconds;

        [SerializeField, Range(0f, 1f), Tooltip("The volume a new note dips to when it follows a sounding one, so each note is heard as a new one.")]
        private float _articulationShare;

        [SerializeField, Range(0.05f, 0.5f), Tooltip("The pulse's width, as a share of each wave. Narrower buzzes more.")]
        private float _pulseWidth;

        [SerializeField, Range(0f, 1f), Tooltip("How much of the tone is the pulse; the rest is the saw.")]
        private float _pulseShare;

        [SerializeField, Min(1f), Tooltip("Where the low-pass softens the tone, in hertz.")]
        private float _brightnessHertz;

        [SerializeField, Min(0f), Tooltip("How fast the vibrato wobbles, in hertz.")]
        private float _vibratoHertz;

        [SerializeField, Min(0f), Tooltip("How far the vibrato bends the pitch either way, in cents (hundredths of a semitone).")]
        private float _vibratoCents;

        public ReedTone(
            float volume, float attackSeconds, float releaseSeconds, float articulationShare, float pulseWidth, float pulseShare,
            float brightnessHertz, float vibratoHertz, float vibratoCents)
        {
            _volume = volume;
            _attackSeconds = attackSeconds;
            _releaseSeconds = releaseSeconds;
            _articulationShare = articulationShare;
            _pulseWidth = pulseWidth;
            _pulseShare = pulseShare;
            _brightnessHertz = brightnessHertz;
            _vibratoHertz = vibratoHertz;
            _vibratoCents = vibratoCents;
        }

        /// <summary>The wasps' hum: a soft swell, a short release, a third of a narrow pulse in the saw, a slight vibrato.</summary>
        public static ReedTone WaspHum => new ReedTone(0.34f, 0.012f, 0.04f, 0.55f, 0.22f, 0.35f, 2200f, 5.6f, 14f);

        public float Volume => _volume;
        public float AttackSeconds => _attackSeconds;
        public float ReleaseSeconds => _releaseSeconds;
        public float ArticulationShare => _articulationShare;
        public float PulseWidth => _pulseWidth;
        public float PulseShare => _pulseShare;
        public float BrightnessHertz => _brightnessHertz;
        public float VibratoHertz => _vibratoHertz;
        public float VibratoCents => _vibratoCents;
    }
}
