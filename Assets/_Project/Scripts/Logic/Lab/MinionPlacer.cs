using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Fills a maze with minions, stronger the further in they are. Depth is measured by walking from the
    /// entrance, because a cell can sit right beside the entrance and still be the far end of the maze.
    /// </summary>
    public static class MinionPlacer
    {
        public static void Populate(Battle battle, Maze maze, PopulationPlan plan, IRandomSource random)
        {
            var depth = new StepField(maze);
            depth.Fill(maze.Entrance);
            var isTaken = new bool[maze.CellCount];

            PlaceBosses(battle, maze, plan, depth, isTaken);
            PlaceMinions(battle, maze, plan, depth, isTaken, random);
        }

        private static void PlaceBosses(Battle battle, Maze maze, PopulationPlan plan, StepField depth, bool[] isTaken)
        {
            for (int index = 0; index < plan.Bosses.Count; index++)
            {
                BossPlacement placement = plan.Bosses[index];
                int cell = CellNearest(Mathf.RoundToInt(placement.Depth * depth.Furthest), maze, depth, isTaken);
                MinionRank rank = index == plan.Bosses.Count - 1 ? MinionRank.FinalBoss : MinionRank.Boss;

                isTaken[cell] = true;
                battle.AddMinion(placement.Boss, maze.CenterOf(cell), rank);
            }
        }

        private static int CellNearest(int steps, Maze maze, StepField depth, bool[] isTaken)
        {
            int best = Maze.NoCell;
            int bestGap = int.MaxValue;

            for (int cell = 0; cell < maze.CellCount; cell++)
            {
                int gap = Mathf.Abs(depth.StepsAt(cell) - steps);

                if (!isTaken[cell] && gap < bestGap)
                {
                    best = cell;
                    bestGap = gap;
                }
            }

            return best;
        }

        private static void PlaceMinions(Battle battle, Maze maze, PopulationPlan plan, StepField depth, bool[] isTaken, IRandomSource random)
        {
            var fitting = new List<MinionType>();

            for (int cell = 0; cell < maze.CellCount; cell++)
            {
                int steps = depth.StepsAt(cell);

                if (isTaken[cell] || steps < plan.SafeCells || random.NextDouble() >= plan.ChancePerCell)
                {
                    continue;
                }

                CollectFitting(plan, steps / (float)depth.Furthest, fitting);

                if (fitting.Count > 0)
                {
                    battle.AddMinion(fitting[(int)(random.NextDouble() * fitting.Count)], maze.CenterOf(cell));
                }
            }
        }

        private static void CollectFitting(PopulationPlan plan, float depth, List<MinionType> fitting)
        {
            fitting.Clear();

            foreach (MinionType minion in plan.Minions)
            {
                if (depth >= minion.FirstDepth && depth <= minion.LastDepth)
                {
                    fitting.Add(minion);
                }
            }
        }
    }
}
