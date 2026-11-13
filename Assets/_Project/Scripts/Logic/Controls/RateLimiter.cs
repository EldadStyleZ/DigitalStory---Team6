using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Lets something happen at most once per gap, such as the rumble of a hit in a stream of hits. A try that is held back
    /// does not restart the gap, so a steady stream still passes once per gap.
    /// </summary>
    public sealed class RateLimiter
    {
        private readonly float _gapSeconds;
        private float _waitLeft;

        /// <summary>Makes the limiter. The first try passes.</summary>
        /// <param name="gapSeconds">The fewest seconds between two passes; 0 lets every try pass.</param>
        /// <exception cref="ArgumentOutOfRangeException">The gap is negative or not a number.</exception>
        public RateLimiter(float gapSeconds)
        {
            if (!(gapSeconds >= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(gapSeconds), gapSeconds, "The gap must be 0 seconds or more.");
            }

            _gapSeconds = gapSeconds;
        }

        /// <summary>True, and the gap starts again, when the last pass was at least the gap ago; false otherwise.</summary>
        public bool TryPass()
        {
            if (_waitLeft > 0f)
            {
                return false;
            }

            _waitLeft = _gapSeconds;
            return true;
        }

        /// <summary>Moves time on by <paramref name="deltaTime"/> seconds.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                _waitLeft -= deltaTime;
            }
        }

        /// <summary>Forgets the last pass, so the next try passes.</summary>
        public void Reset()
        {
            _waitLeft = 0f;
        }
    }
}
