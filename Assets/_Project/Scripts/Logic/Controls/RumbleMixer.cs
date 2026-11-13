using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Mixes rumble pulses into the two motors' speeds. Pulses that overlap combine by the stronger of each motor, never by
    /// adding, so a stream of small ticks cannot add up to a heavy rumble. It holds a fixed number of pulses and never
    /// allocates after it is made.
    /// </summary>
    public sealed class RumbleMixer
    {
        /// <summary>How many pulses can run at once. A new pulse beyond it replaces the one nearest its end.</summary>
        public const int Capacity = 8;

        private readonly float[] _low = new float[Capacity];
        private readonly float[] _high = new float[Capacity];
        private readonly float[] _secondsLeft = new float[Capacity];

        /// <summary>Starts a pulse. A pulse with no speed or no length is ignored.</summary>
        public void Add(in RumblePulse pulse)
        {
            if (!(pulse.Seconds > 0f) || (!(pulse.Low > 0f) && !(pulse.High > 0f)))
            {
                return;
            }

            int slot = 0;
            for (int index = 0; index < Capacity; index++)
            {
                if (_secondsLeft[index] < _secondsLeft[slot])
                {
                    slot = index;
                }
            }

            _low[slot] = pulse.Low;
            _high[slot] = pulse.High;
            _secondsLeft[slot] = pulse.Seconds;
        }

        /// <summary>
        /// The speeds for this frame, then moves every pulse on by <paramref name="deltaTime"/>. A pulse is felt from the frame it
        /// is added, even one shorter than a frame, until its seconds have passed.
        /// </summary>
        /// <param name="deltaTime">Real seconds since the last tick.</param>
        public MotorSpeeds Tick(float deltaTime)
        {
            float low = 0f;
            float high = 0f;
            float step = Mathf.Max(0f, deltaTime);
            for (int index = 0; index < Capacity; index++)
            {
                if (!(_secondsLeft[index] > 0f))
                {
                    continue;
                }

                low = Mathf.Max(low, _low[index]);
                high = Mathf.Max(high, _high[index]);
                _secondsLeft[index] -= step;
            }

            return new MotorSpeeds(low, high);
        }

        /// <summary>Stops every pulse at once, so the next tick is still.</summary>
        public void Clear()
        {
            for (int index = 0; index < Capacity; index++)
            {
                _secondsLeft[index] = 0f;
            }
        }
    }
}
