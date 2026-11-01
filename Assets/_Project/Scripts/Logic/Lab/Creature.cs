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
    }
}
