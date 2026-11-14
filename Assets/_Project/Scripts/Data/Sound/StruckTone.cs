using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// How a struck note voice sounds (plan section AC): the note's own pitch and three partials above it, each a sine that
    /// rings down on its own, the higher ones faster. The pluck and the bell are two sets of these numbers.
    /// </summary>
    [Serializable]
    public struct StruckTone
    {
        [SerializeField, Range(0f, 1f), Tooltip("The note's peak volume as it is struck.")]
        private float _volume;

        [SerializeField, Min(0.0005f), Tooltip("Seconds the strike takes to reach its peak, so it never clicks.")]
        private float _attackSeconds;

        [SerializeField, Min(0.01f), Tooltip("Seconds the note's own pitch takes to fall to about a third while its key is held.")]
        private float _ringSeconds;

        [SerializeField, Min(0.01f), Tooltip("The same once the key is let go: the note is damped, never cut.")]
        private float _dampSeconds;

        [SerializeField, Min(0f), Tooltip("How much faster a higher partial rings down: it rings for the note's time divided by its ratio to this power.")]
        private float _decayPower;

        [SerializeField, Min(0f), Tooltip("The second partial's pitch, as a multiple of the note's.")]
        private float _secondRatio;

        [SerializeField, Range(0f, 1f), Tooltip("The second partial's volume, as a share of the note's.")]
        private float _secondShare;

        [SerializeField, Min(0f), Tooltip("The third partial's pitch, as a multiple of the note's.")]
        private float _thirdRatio;

        [SerializeField, Range(0f, 1f), Tooltip("The third partial's volume, as a share of the note's.")]
        private float _thirdShare;

        [SerializeField, Min(0f), Tooltip("The fourth partial's pitch, as a multiple of the note's. A ratio that is not a whole number gives the metallic shimmer of a tine or a bell.")]
        private float _fourthRatio;

        [SerializeField, Range(0f, 1f), Tooltip("The fourth partial's volume, as a share of the note's.")]
        private float _fourthShare;

        public StruckTone(
            float volume, float attackSeconds, float ringSeconds, float dampSeconds, float decayPower,
            float secondRatio, float secondShare, float thirdRatio, float thirdShare, float fourthRatio, float fourthShare)
        {
            _volume = volume;
            _attackSeconds = attackSeconds;
            _ringSeconds = ringSeconds;
            _dampSeconds = dampSeconds;
            _decayPower = decayPower;
            _secondRatio = secondRatio;
            _secondShare = secondShare;
            _thirdRatio = thirdRatio;
            _thirdShare = thirdShare;
            _fourthRatio = fourthRatio;
            _fourthShare = fourthShare;
        }

        /// <summary>
        /// A kalimba-like pluck: a 2 ms strike, a ring of about half a second while held, an octave and a twelfth faintly
        /// above, and a quick inharmonic shimmer at 5.4 times the note, as a metal tine has.
        /// </summary>
        public static StruckTone Kalimba => new StruckTone(0.122f, 0.002f, 0.6f, 0.2f, 1.2f, 2f, 0.12f, 3f, 0.05f, 5.4f, 0.22f);

        /// <summary>A soft bell: a 6 ms strike, a long ring, warm whole-number partials and one slightly sharp fourth.</summary>
        public static StruckTone SoftBell => new StruckTone(0.102f, 0.006f, 1.3f, 0.4f, 0.75f, 2f, 0.4f, 3f, 0.14f, 4.2f, 0.09f);

        public float Volume => _volume;
        public float AttackSeconds => _attackSeconds;
        public float RingSeconds => _ringSeconds;
        public float DampSeconds => _dampSeconds;
        public float DecayPower => _decayPower;
        public float SecondRatio => _secondRatio;
        public float SecondShare => _secondShare;
        public float ThirdRatio => _thirdRatio;
        public float ThirdShare => _thirdShare;
        public float FourthRatio => _fourthRatio;
        public float FourthShare => _fourthShare;
    }
}
