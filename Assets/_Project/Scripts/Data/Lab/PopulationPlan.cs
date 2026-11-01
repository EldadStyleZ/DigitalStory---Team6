using System.Collections.Generic;

namespace DigitalStory.Data
{
    /// <summary>Who lives in a maze and how crowded it is.</summary>
    public readonly struct PopulationPlan
    {
        public PopulationPlan(IReadOnlyList<MinionType> minions, IReadOnlyList<BossPlacement> bosses, float chancePerCell, int safeCells)
        {
            Minions = minions;
            Bosses = bosses;
            ChancePerCell = chancePerCell;
            SafeCells = safeCells;
        }

        public IReadOnlyList<MinionType> Minions { get; }

        /// <summary>The bosses from shallowest to deepest. Beating the last one wins the game.</summary>
        public IReadOnlyList<BossPlacement> Bosses { get; }

        public float ChancePerCell { get; }

        /// <summary>How many cells of walking from the entrance stay empty.</summary>
        public int SafeCells { get; }
    }
}
