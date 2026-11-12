using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// What a beekeeper can see this tick: the core, whether there is a swarm there to see, the hives and the push away from
    /// its neighbours.
    /// </summary>
    public readonly struct BeekeeperSenses
    {
        /// <summary>Senses with the swarm there to see.</summary>
        public BeekeeperSenses(Vector3 corePosition, HiveSet hives, Vector3 separation)
            : this(corePosition, hives, separation, true)
        {
        }

        /// <summary>Senses that say whether the swarm is there to see (<paramref name="canSeeCore"/>).</summary>
        public BeekeeperSenses(Vector3 corePosition, HiveSet hives, Vector3 separation, bool canSeeCore)
        {
            CorePosition = corePosition;
            Hives = hives;
            Separation = separation;
            CanSeeCore = canSeeCore;
        }

        /// <summary>Where the core stands.</summary>
        public Vector3 CorePosition { get; }

        /// <summary>
        /// False while the swarm is collapsed (plan section O): there is nothing at the core to see, chase, hit or smoke until
        /// it respawns.
        /// </summary>
        public bool CanSeeCore { get; }

        /// <summary>The hives, standing or not.</summary>
        public HiveSet Hives { get; }

        /// <summary>A push away from neighbours, worked out by the session.</summary>
        public Vector3 Separation { get; }
    }
}
