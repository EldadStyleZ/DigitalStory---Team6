using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>What a beekeeper can see this tick: the Piper, the hives and the push away from its neighbours.</summary>
    public readonly struct BeekeeperSenses
    {
        public BeekeeperSenses(Vector3 piperPosition, HiveSet hives, Vector3 separation)
        {
            PiperPosition = piperPosition;
            Hives = hives;
            Separation = separation;
        }

        /// <summary>Where the Piper stands.</summary>
        public Vector3 PiperPosition { get; }

        /// <summary>The hives, standing or not.</summary>
        public HiveSet Hives { get; }

        /// <summary>A push away from neighbours, worked out by the session.</summary>
        public Vector3 Separation { get; }
    }
}
