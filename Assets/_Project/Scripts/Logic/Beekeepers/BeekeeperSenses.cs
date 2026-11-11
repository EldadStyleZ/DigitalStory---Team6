using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What a beekeeper can see this tick: the core, the hives and the push away from its neighbours.</summary>
    public readonly struct BeekeeperSenses
    {
        public BeekeeperSenses(Vector3 corePosition, HiveSet hives, Vector3 separation)
        {
            CorePosition = corePosition;
            Hives = hives;
            Separation = separation;
        }

        /// <summary>Where the core stands.</summary>
        public Vector3 CorePosition { get; }

        /// <summary>The hives, standing or not.</summary>
        public HiveSet Hives { get; }

        /// <summary>A push away from neighbours, worked out by the session.</summary>
        public Vector3 Separation { get; }
    }
}
