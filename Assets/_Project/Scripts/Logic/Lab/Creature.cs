using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>A summoned creature fighting for the Piper.</summary>
    public sealed class Creature : Fighter
    {
        public Creature(CreatureType type, Vector2 position)
            : base(type.Stats, type.Look, position)
        {
            Type = type;
        }

        public CreatureType Type { get; }

        /// <summary>How long it has gone without a fight.</summary>
        public float QuietSeconds { get; private set; }

        public void WaitQuietly(float deltaTime) => QuietSeconds += deltaTime;

        public void KeepBusy() => QuietSeconds = 0f;
    }
}
