using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Fills a maze with minions and gangs, stronger the further in they are. Depth is measured by walking from the
    /// entrance, because a cell can sit right beside the entrance and still be the far end of the maze.
    /// </summary>
    public static class MinionPlacer
    {
        /// <returns>The hidden groups, which do not exist in the battle until they spring.</returns>
        public static IReadOnlyList<Ambush> Populate(Battle battle, Maze maze, PopulationPlan plan, IRandomSource random)
        {
            var depth = new StepField(maze);
            depth.Fill(maze.Entrance);
            var isTaken = new bool[maze.CellCount];
            var ambushes = new List<Ambush>();

            PlaceBosses(battle, maze, plan, depth, isTaken);
            FillCells(battle, maze, plan, depth, isTaken, random, ambushes);
            return ambushes;
        }

        private static void PlaceBosses(Battle battle, Maze maze, PopulationPlan plan, StepField depth, bool[] isTaken)
        {
            for (int index = 0; index < plan.Bosses.Count; index++)
            {
                BossPlacement placement = plan.Bosses[index];
                int cell = CellNearest(Mathf.RoundToInt(placement.Depth * depth.Furthest), maze, depth, isTaken);
                MinionRank rank = index == plan.Bosses.Count - 1 ? MinionRank.FinalBoss : MinionRank.Boss;
                MinionGang gang = placement.Guards.Count > 0 ? new MinionGang() : null;

                isTaken[cell] = true;
                battle.AddMinion(placement.Boss, maze.CenterOf(cell), rank, gang);
                PlaceMembers(battle, maze, plan, placement.Boss.Name, placement.Guards, placement.Guards.Count, cell, gang);
            }
        }

        private static void PlaceMembers(Battle battle, Maze maze, PopulationPlan plan, string owner, IReadOnlyList<string> names, int count, int cell, MinionGang gang)
        {
            for (int index = 0; index < count; index++)
            {
                battle.AddMinion(Find(plan, names[index], owner), maze.SpotAround(cell, index, count), MinionRank.Ordinary, gang);
            }
        }

        private static MinionType Find(PopulationPlan plan, string name, string owner)
        {
            foreach (MinionType minion in plan.Minions)
            {
                if (minion.Name == name)
                {
                    return minion;
                }
            }

            throw new InvalidOperationException($"{owner} names {name}, which is not one of the population's minions.");
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

        private static void FillCells(Battle battle, Maze maze, PopulationPlan plan, StepField depth, bool[] isTaken, IRandomSource random, List<Ambush> ambushes)
        {
            var fittingMinions = new List<MinionType>();
            var fittingGangs = new List<GangType>();

            for (int cell = 0; cell < maze.CellCount; cell++)
            {
                int steps = depth.StepsAt(cell);

                if (isTaken[cell] || steps < plan.SafeCells)
                {
                    continue;
                }

                double roll = random.NextDouble();
                float cellDepth = steps / (float)depth.Furthest;

                if (roll < plan.GangChancePerCell)
                {
                    CollectFitting(plan, cellDepth, fittingGangs);
                    PlaceAGang(battle, maze, plan, cell, fittingGangs, random, ambushes);
                }
                else if (roll < plan.GangChancePerCell + plan.ChancePerCell)
                {
                    CollectFitting(plan, cellDepth, fittingMinions);
                    PlaceAMinion(battle, maze, cell, fittingMinions, random);
                }
            }
        }

        private static void PlaceAMinion(Battle battle, Maze maze, int cell, List<MinionType> fitting, IRandomSource random)
        {
            if (fitting.Count > 0)
            {
                battle.AddMinion(fitting[(int)(random.NextDouble() * fitting.Count)], maze.CenterOf(cell));
            }
        }

        private static void PlaceAGang(Battle battle, Maze maze, PopulationPlan plan, int cell, List<GangType> fitting, IRandomSource random, List<Ambush> ambushes)
        {
            if (fitting.Count == 0)
            {
                return;
            }

            GangType type = fitting[(int)(random.NextDouble() * fitting.Count)];
            int visible = type.Members.Count - type.HiddenCount;

            PlaceMembers(battle, maze, plan, type.Name, type.Members, visible, cell, new MinionGang());

            if (type.HiddenCount > 0)
            {
                ambushes.Add(new Ambush(cell, HiddenMembers(plan, type, visible)));
            }
        }

        private static MinionType[] HiddenMembers(PopulationPlan plan, GangType type, int firstHidden)
        {
            var members = new MinionType[type.HiddenCount];

            for (int index = 0; index < members.Length; index++)
            {
                members[index] = Find(plan, type.Members[firstHidden + index], type.Name);
            }

            return members;
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

        private static void CollectFitting(PopulationPlan plan, float depth, List<GangType> fitting)
        {
            fitting.Clear();

            foreach (GangType gang in plan.Gangs)
            {
                if (depth >= gang.FirstDepth && depth <= gang.LastDepth)
                {
                    fitting.Add(gang);
                }
            }
        }
    }
}
