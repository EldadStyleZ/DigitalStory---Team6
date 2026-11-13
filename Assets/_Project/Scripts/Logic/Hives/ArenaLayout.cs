using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// One match's map (plan section S), drawn by <see cref="LayoutGenerator"/>: the hives, the beekeepers' spawn points, the
    /// nest trees and the scenery. Positions are on the ground (y = 0).
    /// </summary>
    public sealed class ArenaLayout
    {
        public ArenaLayout(
            int seed,
            bool isFallback,
            IReadOnlyList<Vector3> hives,
            IReadOnlyList<Vector3> spawns,
            IReadOnlyList<Vector3> nests,
            IReadOnlyList<SceneryPlacement> scenery)
        {
            Seed = seed;
            IsFallback = isFallback;
            Hives = hives;
            Spawns = spawns;
            Nests = nests;
            Scenery = scenery;
        }

        /// <summary>The seed the map was drawn from. The same seed and config draw the same map.</summary>
        public int Seed { get; }

        /// <summary>True when no drawn map kept the rules and the config's fixed fallback map stands instead.</summary>
        public bool IsFallback { get; }

        /// <summary>The hives: the first west of the middle, the second east of it.</summary>
        public IReadOnlyList<Vector3> Hives { get; }

        /// <summary>
        /// Where beekeepers enter, in the order the waves take them: ahead, behind, beyond the east hive, beyond the west hive.
        /// </summary>
        public IReadOnlyList<Vector3> Spawns { get; }

        /// <summary>The nest trees, in the order they open.</summary>
        public IReadOnlyList<Vector3> Nests { get; }

        /// <summary>One placement for each prop the generator was given, in the same order.</summary>
        public IReadOnlyList<SceneryPlacement> Scenery { get; }
    }
}
