using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Which of a sound's clips plays next (plan section AC): any of them at random, but never the one that played last, so a
    /// run of hits never repeats one clip back to back. The randomness comes in, so a seed replays the same order.
    /// </summary>
    public sealed class ClipChoice
    {
        private readonly int _count;
        private readonly IRandomSource _random;

        /// <param name="count">How many clips there are. 1 or more.</param>
        /// <param name="random">Where the chance comes from.</param>
        /// <exception cref="ArgumentOutOfRangeException">There are no clips.</exception>
        /// <exception cref="ArgumentNullException">The random source is missing.</exception>
        public ClipChoice(int count, IRandomSource random)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "A sound needs at least one clip.");
            }

            _count = count;
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Last = -1;
        }

        /// <summary>The clip chosen last, or -1 before the first.</summary>
        public int Last { get; private set; }

        /// <summary>Chooses the next clip, from 0 to the count less 1. With one clip it is always 0.</summary>
        public int Next()
        {
            if (_count == 1)
            {
                Last = 0;
                return 0;
            }

            // Draw among the others only: every clip but the last is equally likely.
            int others = Last < 0 ? _count : _count - 1;
            int pick = Math.Min(others - 1, (int)(_random.NextDouble() * others));
            if (Last >= 0 && pick >= Last)
            {
                pick++;
            }

            Last = pick;
            return pick;
        }
    }
}
