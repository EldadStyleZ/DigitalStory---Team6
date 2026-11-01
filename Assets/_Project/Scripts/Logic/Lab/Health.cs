using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>How much more damage something can take.</summary>
    public sealed class Health
    {
        public Health(float max)
        {
            Max = max;
            Current = max;
        }

        public float Max { get; }
        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;

        /// <summary>The fraction of its health left, from 0 to 1.</summary>
        public float Share => Current / Max;

        public void Damage(float amount) => Current = Mathf.Max(0f, Current - amount);

        /// <summary>Gives health back, stopping at the maximum.</summary>
        public void Heal(float amount) => Current = Mathf.Min(Max, Current + amount);

        public void Refill() => Current = Max;
    }
}
