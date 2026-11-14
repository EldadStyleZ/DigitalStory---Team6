using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The trauma behind the camera shake (plan section Y, after Eiserloh, GDC 2016). Events add trauma; it is capped at 1 and
    /// falls linearly; the shake's strength is trauma raised to the exponent (2), so small events barely move the view and big
    /// ones feel big, and several events in a row build up instead of each starting a shake of its own.
    /// </summary>
    public sealed class TraumaShake
    {
        private readonly float _decayPerSecond;
        private readonly float _exponent;
        private float _trauma;

        /// <param name="decayPerSecond">How much trauma falls each second. More than 0.</param>
        /// <param name="exponent">The power the strength raises trauma to. More than 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">The decay or the exponent is not more than 0.</exception>
        public TraumaShake(float decayPerSecond, float exponent)
        {
            if (!(decayPerSecond > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(decayPerSecond), decayPerSecond, "Trauma must fall: give a decay above 0.");
            }

            if (!(exponent > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "The trauma exponent must be above 0.");
            }

            _decayPerSecond = decayPerSecond;
            _exponent = exponent;
        }

        /// <summary>The trauma now, 0 to 1.</summary>
        public float Trauma => _trauma;

        /// <summary>The shake's strength now, 0 to 1: trauma raised to the exponent.</summary>
        public float Strength => _trauma <= 0f ? 0f : (float)Math.Pow(_trauma, _exponent);

        /// <summary>True when there is no trauma left.</summary>
        public bool IsResting => _trauma <= 0f;

        /// <summary>Adds trauma, capped at 1. A negative or NaN amount changes nothing.</summary>
        public void Add(float amount)
        {
            if (!(amount > 0f))
            {
                return;
            }

            _trauma = Math.Min(1f, _trauma + amount);
        }

        /// <summary>Lets the trauma fall for the given seconds, never below 0.</summary>
        public void Step(float deltaSeconds)
        {
            if (!(deltaSeconds > 0f))
            {
                return;
            }

            _trauma = Math.Max(0f, _trauma - (_decayPerSecond * deltaSeconds));
        }

        /// <summary>Drops all trauma at once.</summary>
        public void Clear()
        {
            _trauma = 0f;
        }
    }
}
