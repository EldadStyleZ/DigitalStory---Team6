using System;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Plan section AD: how many short-lived lights may shine at once. Each pixel light draws every object in its reach again,
    /// so past the budget a new effect goes without its light.
    /// </summary>
    public sealed class LightBudget
    {
        /// <param name="most">The most lights at once; at least 1.</param>
        /// <exception cref="ArgumentOutOfRangeException">The budget is below 1.</exception>
        public LightBudget(int most)
        {
            if (most < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(most), most, "A light budget needs room for at least one light.");
            }

            Most = most;
        }

        /// <summary>The most lights at once.</summary>
        public int Most { get; }

        /// <summary>The lights shining now.</summary>
        public int InUse { get; private set; }

        /// <summary>Takes room for one light. False, and nothing taken, when the budget is full.</summary>
        public bool TryClaim()
        {
            if (InUse >= Most)
            {
                return false;
            }

            InUse++;
            return true;
        }

        /// <summary>Gives back the room of a light that went out. Never below empty.</summary>
        public void Release()
        {
            InUse = Math.Max(0, InUse - 1);
        }

        /// <summary>Forgets every light, as when the scene that held them is unloaded.</summary>
        public void Clear()
        {
            InUse = 0;
        }
    }
}
