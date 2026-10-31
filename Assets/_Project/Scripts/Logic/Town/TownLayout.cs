using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One generated town: its houses, street junctions and lamps.</summary>
    public sealed class TownLayout
    {
        public TownLayout(
            int columns,
            int rows,
            IReadOnlyList<House> houses,
            IReadOnlyList<Junction> junctions,
            IReadOnlyList<Vector2> lamps)
        {
            Columns = columns;
            Rows = rows;
            Houses = houses;
            Junctions = junctions;
            Lamps = lamps;
        }

        public int Columns { get; }
        public int Rows { get; }
        public IReadOnlyList<House> Houses { get; }
        public IReadOnlyList<Junction> Junctions { get; }
        public IReadOnlyList<Vector2> Lamps { get; }
    }
}
