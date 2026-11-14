using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The ducking of plan section AC: a big moment (a slam, the Meteor's landing, a hive lost, a stinger) lowers the swarm's
    /// hum by a few decibels for a moment, so the big sound stands out, then lets it back. The dip and the return are smooth,
    /// so the hum never steps. Another big moment while ducked holds it down longer, never deeper. Feedback only.
    /// </summary>
    public sealed class SoundDuck
    {
        private readonly float _duckedGain;
        private readonly float _holdSeconds;
        private readonly float _attackSeconds;
        private readonly float _releaseSeconds;
        private float _holdLeft;

        /// <param name="decibels">How far the hum dips: 0 or less, -6 halves it.</param>
        /// <param name="holdSeconds">Seconds it stays down after the last big moment. 0 or more.</param>
        /// <param name="attackSeconds">Seconds it takes to dip, to about two thirds of the way. Above 0.</param>
        /// <param name="releaseSeconds">Seconds it takes to come back, to about two thirds of the way. Above 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">A number is out of its range.</exception>
        public SoundDuck(float decibels, float holdSeconds, float attackSeconds, float releaseSeconds)
        {
            if (!(decibels <= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(decibels), decibels, "A duck lowers the sound: 0 dB or less.");
            }

            if (!(holdSeconds >= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(holdSeconds), holdSeconds, "The hold must be 0 or more seconds.");
            }

            if (!(attackSeconds > 0f) || !(releaseSeconds > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(attackSeconds), "The dip and the return need more than 0 seconds.");
            }

            _duckedGain = (float)Math.Pow(10d, decibels / 20d);
            _holdSeconds = holdSeconds;
            _attackSeconds = attackSeconds;
            _releaseSeconds = releaseSeconds;
        }

        /// <summary>The share of the hum's volume let through now: 1 at rest, the ducked gain at the bottom of a dip.</summary>
        public float Gain { get; private set; } = 1f;

        /// <summary>True while a big moment holds the hum down.</summary>
        public bool IsHolding => _holdLeft > 0f;

        /// <summary>A big moment: the hum dips and stays down for the hold.</summary>
        public void Trigger()
        {
            _holdLeft = _holdSeconds > 0f ? _holdSeconds : float.Epsilon;
        }

        /// <summary>Moves on by <paramref name="deltaTime"/> seconds (real time) and returns the gain.</summary>
        public float Step(float deltaTime)
        {
            if (!(deltaTime > 0f))
            {
                return Gain;
            }

            bool isDown = _holdLeft > 0f;
            float target = isDown ? _duckedGain : 1f;
            float seconds = isDown ? _attackSeconds : _releaseSeconds;
            Gain += (target - Gain) * (1f - (float)Math.Exp(-deltaTime / seconds));
            _holdLeft = Math.Max(0f, _holdLeft - deltaTime);
            return Gain;
        }
    }
}
