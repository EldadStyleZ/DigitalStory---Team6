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

        public void Damage(float amount) => Current = Mathf.Max(0f, Current - amount);

        public void Refill() => Current = Max;
    }
}
