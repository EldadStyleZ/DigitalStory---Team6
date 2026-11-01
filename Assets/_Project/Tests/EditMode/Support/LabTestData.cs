using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Builds the small mazes and fighters the lab's tests use.</summary>
    internal static class LabTestData
    {
        public const int H = 0;
        public const int J = 1;
        public const int K = 2;
        public const int L = 3;

        private static readonly FighterLook Plain = new FighterLook(Color.white, 1f, FighterShape.Rat);

        public static readonly FighterStats Sturdy = new FighterStats(1000f, 5f, 1f, 1f, 1f);

        /// <summary>A straight east-west corridor of cells.</summary>
        public static Maze Corridor(int cells)
        {
            var openEast = new bool[cells];
            var openNorth = new bool[cells];

            for (int cell = 0; cell < cells - 1; cell++)
            {
                openEast[cell] = true;
            }

            return new Maze(new MazeShape(cells, 1, 3, 1, 0f), openEast, openNorth);
        }

        /// <summary>
        /// Cells 0 and 1 sit side by side with a wall between them, so the only way round is 0, 2, 3, 1.
        /// </summary>
        public static Maze Horseshoe()
        {
            return new Maze(
                new MazeShape(2, 2, 3, 1, 0f),
                new[] { false, false, true, false },
                new[] { true, true, false, false });
        }

        public static CreatureType Creature(FighterStats stats, string name = "Creature", int unlockLevel = 1, float cost = 10f, float cooldown = 2f, params int[] combo)
        {
            return new CreatureType(name, unlockLevel, combo.Length > 0 ? combo : new[] { H, J }, cost, cooldown, stats, Plain);
        }

        public static CreatureType Creature(string name, int unlockLevel, params int[] combo)
        {
            return Creature(Sturdy, name, unlockLevel, 10f, 2f, combo);
        }

        public static MinionType Minion(FighterStats stats, float detectTiles = 8f, int xp = 10, float firstDepth = 0f, float lastDepth = 1f)
        {
            return new MinionType("Minion", detectTiles, xp, firstDepth, lastDepth, stats, Plain);
        }
    }
}
