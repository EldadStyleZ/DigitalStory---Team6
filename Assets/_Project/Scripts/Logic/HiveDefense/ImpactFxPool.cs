using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Which pooled instance of one impact effect plays next (plan section AB). The instances are made once, at load; a new
    /// effect takes the one that started longest ago, so a kind never shows more than its cap and nothing is made in play.
    /// A free instance is always the oldest, since it finished its play before any that still runs. A least gap between two
    /// effects of the kind thins out a burst, such as many hits in one frame. Feedback only: it changes no game rule.
    /// </summary>
    public sealed class ImpactFxPool
    {
        /// <summary>What <see cref="Claim"/> returns when the least gap refuses an effect.</summary>
        public const int None = -1;

        private readonly float[] _startedAt;
        private readonly float _leastGapSeconds;
        private float _lastStart = float.NegativeInfinity;

        /// <param name="mostAtOnce">How many instances the kind has: the most that can show at once. 1 or more.</param>
        /// <param name="leastGapSeconds">The fewest seconds between two effects of the kind. 0 or more; 0 lets every one play.</param>
        /// <exception cref="ArgumentOutOfRangeException">The cap is below 1, or the gap is negative or not a number.</exception>
        public ImpactFxPool(int mostAtOnce, float leastGapSeconds)
        {
            if (mostAtOnce < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(mostAtOnce), mostAtOnce, "An impact effect needs at least one instance.");
            }

            if (!(leastGapSeconds >= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(leastGapSeconds), leastGapSeconds, "The least gap must be 0 or more seconds.");
            }

            _startedAt = new float[mostAtOnce];
            for (int index = 0; index < _startedAt.Length; index++)
            {
                _startedAt[index] = float.NegativeInfinity;
            }

            _leastGapSeconds = leastGapSeconds;
        }

        /// <summary>How many instances the kind has.</summary>
        public int Count => _startedAt.Length;

        /// <summary>
        /// Claims an instance for an effect starting at the given time (seconds, never going back) and returns its index, the
        /// one that started longest ago; or <see cref="None"/> when the last effect started less than the least gap before.
        /// </summary>
        public int Claim(float now)
        {
            if (now - _lastStart < _leastGapSeconds)
            {
                return None;
            }

            int oldest = 0;
            for (int index = 1; index < _startedAt.Length; index++)
            {
                if (_startedAt[index] < _startedAt[oldest])
                {
                    oldest = index;
                }
            }

            _startedAt[oldest] = now;
            _lastStart = now;
            return oldest;
        }
    }
}
