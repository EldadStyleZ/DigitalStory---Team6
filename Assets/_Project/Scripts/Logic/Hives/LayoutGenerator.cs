using System;
using System.Collections.Generic;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Draws a match's map from a seed (plan section S): random within <see cref="LayoutConfig"/>'s and
    /// <see cref="NestConfig"/>'s rules, so every map is fair. The same seed and configs always draw the same map.
    /// </summary>
    /// <remarks>
    /// The order follows what each part must keep away from. The hives come first, one in a band west of the middle and one
    /// east of it. The line between them sets the map's turn, and the four spawn points turn with it, one ahead, one behind
    /// and one beyond each hive, so every walk to a hive stays as long as on the old fixed map; the ones ahead and behind lean
    /// to the west hive, so each hive is walked to by as many beekeepers as there. The nests follow, the ones open
    /// at the start ahead to either side and the rest one in each slice of the circle, turned the same way; then the scenery
    /// around the edge, clear of the hives' rings, the nest trees, the start, the spawn points and the lanes from each spawn
    /// point to each hive. Each part has a bounded number of tries. When a whole map fails <see cref="LayoutConfig.LayoutAttempts"/>
    /// times the config's fixed fallback map stands instead, and a prop that finds no place is left out. It runs once a
    /// match, not each frame, so it may allocate.
    /// </remarks>
    public static class LayoutGenerator
    {
        /// <summary>Hives on every map: one west of the middle, one east.</summary>
        public const int HiveCount = 2;

        /// <summary>Spawn points on every map: ahead, behind, beyond the east hive, beyond the west hive.</summary>
        public const int SpawnCount = 4;

        private const float FullTurnDegrees = 360f;
        private const float WestBearing = 270f;
        private const float EastBearing = 90f;

        // The spawn points' sides of the hives' line, in the order the waves take them: ahead, behind, beyond the east hive
        // and beyond the west hive, which on the old fixed map were north, south, east and west.
        private static readonly float[] SpawnSides = { 0f, 180f, 90f, 270f };

        // Which way each spawn point's bearing turns off its side: the one ahead turns only anticlockwise and the one behind
        // only clockwise, both toward the west hive (LayoutConfig.SpawnWestLeanMetres); 0 turns either way.
        private static readonly float[] SpawnLeans = { -1f, 1f, 0f, 0f };

        /// <summary>Draws the map for <paramref name="seed"/>.</summary>
        /// <param name="seed">Any number. The same seed draws the same map.</param>
        /// <param name="start">Where the swarm's core starts, which the nests and the scenery keep away from.</param>
        /// <param name="layout">The map's rules.</param>
        /// <param name="nests">How many nests there are, how many open at the start, and where they may stand.</param>
        /// <param name="sceneryFootprints">
        /// One entry per prop of the scenery, in the order to place them: metres on the ground from the prop's pivot to the
        /// farthest point of it, whichever way it turns.
        /// </param>
        public static ArenaLayout Generate(
            int seed, Vector3 start, LayoutConfig layout, NestConfig nests, IReadOnlyList<float> sceneryFootprints)
        {
            CheckArguments(layout, nests, sceneryFootprints);

            var random = new Mulberry32Random(unchecked((uint)seed));
            var hives = new Vector3[HiveCount];
            var spawns = new Vector3[SpawnCount];
            var nestPositions = new Vector3[nests.NestCount];
            bool isDrawn = false;
            for (int attempt = 0; attempt < layout.LayoutAttempts && !isDrawn; attempt++)
            {
                isDrawn = TryHives(random, layout, hives)
                    && TrySpawns(random, layout, hives, spawns)
                    && TryNests(random, layout, nests, start, hives, spawns, nestPositions);
            }

            if (!isDrawn)
            {
                CopyInto(layout.FallbackHives, hives);
                CopyInto(layout.FallbackSpawns, spawns);
                CopyInto(layout.FallbackNests, nestPositions);
            }

            SceneryPlacement[] scenery = PlaceScenery(random, layout, start, hives, spawns, nestPositions, sceneryFootprints);
            return new ArenaLayout(seed, !isDrawn, hives, spawns, nestPositions, scenery);
        }

        private static void CheckArguments(LayoutConfig layout, NestConfig nests, IReadOnlyList<float> sceneryFootprints)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (nests == null)
            {
                throw new ArgumentNullException(nameof(nests));
            }

            if (sceneryFootprints == null)
            {
                throw new ArgumentNullException(nameof(sceneryFootprints), "Pass an empty list for a map with no scenery.");
            }

            for (int index = 0; index < sceneryFootprints.Count; index++)
            {
                if (!(sceneryFootprints[index] >= 0f))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(sceneryFootprints), $"Prop {index}'s footprint is {sceneryFootprints[index]}; it must be 0 or more.");
                }
            }

            if (layout.FallbackHives.Count != HiveCount || layout.FallbackSpawns.Count != SpawnCount
                || layout.FallbackNests.Count != nests.NestCount)
            {
                throw new ArgumentException(
                    $"LayoutConfig's fallback map needs {HiveCount} hives, {SpawnCount} spawn points and NestConfig's {nests.NestCount} nests; "
                    + $"it has {layout.FallbackHives.Count}, {layout.FallbackSpawns.Count} and {layout.FallbackNests.Count}.",
                    nameof(layout));
            }
        }

        private static bool TryHives(IRandomSource random, LayoutConfig layout, Vector3[] hives)
        {
            for (int attempt = 0; attempt < layout.PlaceAttempts; attempt++)
            {
                Vector3 west = Polar(
                    WestBearing + Spread(random, layout.HiveTiltDegrees), Between(random, layout.HiveNearest, layout.HiveFarthest));
                Vector3 east = Polar(
                    EastBearing + Spread(random, layout.HiveTiltDegrees), Between(random, layout.HiveNearest, layout.HiveFarthest));
                if (GroundMath.DistanceSquared(west, east) >= layout.HiveLeastSeparation * layout.HiveLeastSeparation)
                {
                    hives[0] = west;
                    hives[1] = east;
                    return true;
                }
            }

            return false;
        }

        private static bool TrySpawns(IRandomSource random, LayoutConfig layout, Vector3[] hives, Vector3[] spawns)
        {
            float turn = Turn(hives);
            for (int index = 0; index < SpawnCount; index++)
            {
                bool isPlaced = false;
                float lean = SpawnLeans[index];
                for (int attempt = 0; attempt < layout.PlaceAttempts && !isPlaced; attempt++)
                {
                    float jitter = lean == 0f ? Spread(random, layout.SpawnJitterDegrees) : lean * Between(random, 0f, layout.SpawnJitterDegrees);
                    Vector3 spawn = Polar(turn + SpawnSides[index] + jitter, Between(random, layout.SpawnNearest, layout.SpawnFarthest));
                    if (IsInside(spawn, layout.ArenaHalfSize)
                        && IsClearOf(spawn, hives, layout.SpawnHiveLeastDistance)
                        && (lean == 0f || LeansWest(spawn, hives, layout.SpawnWestLeanMetres)))
                    {
                        spawns[index] = spawn;
                        isPlaced = true;
                    }
                }

                if (!isPlaced)
                {
                    return false;
                }
            }

            return true;
        }

        // The ones open at the start stand ahead, where the opening camera looks, one to each side. The rest stand one in each
        // equal slice of the circle, and the nearer ones open first, so each wave's nests are a longer trip than the last.
        private static bool TryNests(
            IRandomSource random, LayoutConfig layout, NestConfig nests, Vector3 start, Vector3[] hives, Vector3[] spawns, Vector3[] result)
        {
            float turn = Turn(hives);
            var rules = new NestRules(layout, nests, start, hives, spawns, new List<Vector3>(result.Length));
            for (int index = 0; index < nests.OpenAtStart; index++)
            {
                float side = index % 2 == 0 ? -1f : 1f;
                float from = turn + (side * layout.FirstNestsNarrowestBearing);
                float to = turn + (side * layout.FirstNestsWidestBearing);
                if (!TryNest(random, rules, from, to, nests.FirstNestsNearest, nests.FirstNestsFarthest))
                {
                    return false;
                }
            }

            int later = result.Length - nests.OpenAtStart;
            for (int index = 0; index < later; index++)
            {
                float slice = turn + (index * FullTurnDegrees / later);
                float jitter = layout.LaterNestJitterDegrees;
                if (!TryNest(random, rules, slice - jitter, slice + jitter, nests.LaterNestsNearest, nests.LaterNestsFarthest))
                {
                    return false;
                }
            }

            rules.Placed.Sort(nests.OpenAtStart, later, NearerFirst.Instance);
            rules.Placed.CopyTo(result);
            return true;
        }

        private static bool TryNest(IRandomSource random, NestRules rules, float fromBearing, float toBearing, float nearest, float farthest)
        {
            LayoutConfig layout = rules.Layout;
            float hiveClearance = rules.Nests.HiveClearance + layout.NestHiveMargin;
            for (int attempt = 0; attempt < layout.PlaceAttempts; attempt++)
            {
                Vector3 point = Polar(Between(random, fromBearing, toBearing), Between(random, nearest, farthest));

                // Rounding can put a point a hair outside the band that NestLayout.Check holds it to.
                float fromMiddle = Mathf.Sqrt(GroundMath.DistanceSquared(point, Vector3.zero));
                if (fromMiddle >= nearest && fromMiddle <= farthest
                    && IsInside(point, layout.NestInsideHalfSize)
                    && IsClearOf(point, rules.Start, layout.NestStartClearance)
                    && IsClearOf(point, rules.Hives, hiveClearance)
                    && IsClearOf(point, rules.Spawns, layout.NestSpawnClearance)
                    && IsClearOf(point, rules.Placed, layout.NestLeastSpacing))
                {
                    rules.Placed.Add(point);
                    return true;
                }
            }

            return false;
        }

        private static SceneryPlacement[] PlaceScenery(
            IRandomSource random,
            LayoutConfig layout,
            Vector3 start,
            Vector3[] hives,
            Vector3[] spawns,
            Vector3[] nests,
            IReadOnlyList<float> footprints)
        {
            var result = new SceneryPlacement[footprints.Count];
            var placed = new List<Vector3>(footprints.Count);
            var placedRadii = new List<float>(footprints.Count);
            for (int index = 0; index < footprints.Count; index++)
            {
                float radius = footprints[index];
                for (int attempt = 0; attempt < layout.SceneryAttempts; attempt++)
                {
                    var point = new Vector3(Spread(random, layout.ArenaHalfSize), 0f, Spread(random, layout.ArenaHalfSize));
                    if (IsAroundTheEdge(point, layout)
                        && IsClearOf(point, start, layout.SceneryStartClearance + radius)
                        && IsClearOf(point, hives, layout.SceneryHiveClearance + radius)
                        && IsClearOf(point, nests, layout.SceneryNestClearance + radius)
                        && IsClearOf(point, spawns, layout.ScenerySpawnClearance + radius)
                        && IsClearOfLanes(point, spawns, hives, layout.LaneHalfWidth + radius)
                        && IsClearOfProps(point, radius, placed, placedRadii, layout))
                    {
                        result[index] = new SceneryPlacement(point, Between(random, 0f, FullTurnDegrees));
                        placed.Add(point);
                        placedRadii.Add(radius);
                        break;
                    }
                }
            }

            return result;
        }

        private static bool LeansWest(Vector3 spawn, Vector3[] hives, float metres)
        {
            float toWest = Mathf.Sqrt(GroundMath.DistanceSquared(spawn, hives[0]));
            float toEast = Mathf.Sqrt(GroundMath.DistanceSquared(spawn, hives[1]));
            return toEast - toWest >= metres;
        }

        private static bool IsAroundTheEdge(Vector3 point, LayoutConfig layout)
        {
            return Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.z)) >= layout.SceneryInnerHalfSize;
        }

        private static bool IsClearOfLanes(Vector3 point, Vector3[] spawns, Vector3[] hives, float distance)
        {
            float squared = distance * distance;
            for (int spawn = 0; spawn < spawns.Length; spawn++)
            {
                for (int hive = 0; hive < hives.Length; hive++)
                {
                    if (SquaredDistanceToSegment(point, spawns[spawn], hives[hive]) < squared)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsClearOfProps(Vector3 point, float radius, List<Vector3> placed, List<float> placedRadii, LayoutConfig layout)
        {
            for (int index = 0; index < placed.Count; index++)
            {
                float least = Mathf.Max(layout.SceneryLeastSpacing, layout.SceneryFootprintShare * (radius + placedRadii[index]));
                if (GroundMath.DistanceSquared(point, placed[index]) < least * least)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsClearOf(Vector3 point, Vector3 other, float distance)
        {
            return GroundMath.DistanceSquared(point, other) >= distance * distance;
        }

        private static bool IsClearOf(Vector3 point, IReadOnlyList<Vector3> others, float distance)
        {
            for (int index = 0; index < others.Count; index++)
            {
                if (!IsClearOf(point, others[index], distance))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsInside(Vector3 point, float halfSize)
        {
            return Mathf.Abs(point.x) <= halfSize && Mathf.Abs(point.z) <= halfSize;
        }

        private static float SquaredDistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            float alongX = to.x - from.x;
            float alongZ = to.z - from.z;
            float lengthSquared = (alongX * alongX) + (alongZ * alongZ);
            float share = 0f;
            if (lengthSquared > 0f)
            {
                share = Mathf.Clamp01((((point.x - from.x) * alongX) + ((point.z - from.z) * alongZ)) / lengthSquared);
            }

            float offX = point.x - (from.x + (alongX * share));
            float offZ = point.z - (from.z + (alongZ * share));
            return (offX * offX) + (offZ * offZ);
        }

        // Degrees the line from the west hive to the east hive turns off due east.
        private static float Turn(Vector3[] hives)
        {
            Vector3 line = hives[1] - hives[0];
            return (Mathf.Atan2(line.x, line.z) * Mathf.Rad2Deg) - EastBearing;
        }

        // A point on the ground, bearingDegrees clockwise from +Z seen from above, radius metres from the middle.
        private static Vector3 Polar(float bearingDegrees, float radius)
        {
            float radians = bearingDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians) * radius, 0f, Mathf.Cos(radians) * radius);
        }

        private static float Between(IRandomSource random, float from, float to)
        {
            return from + ((to - from) * (float)random.NextDouble());
        }

        private static float Spread(IRandomSource random, float half)
        {
            return Between(random, -half, half);
        }

        private static void CopyInto(IReadOnlyList<Vector3> from, Vector3[] to)
        {
            for (int index = 0; index < to.Length; index++)
            {
                to[index] = from[index];
            }
        }

        // What every nest keeps away from, and the nests placed so far.
        private sealed class NestRules
        {
            public NestRules(LayoutConfig layout, NestConfig nests, Vector3 start, Vector3[] hives, Vector3[] spawns, List<Vector3> placed)
            {
                Layout = layout;
                Nests = nests;
                Start = start;
                Hives = hives;
                Spawns = spawns;
                Placed = placed;
            }

            public LayoutConfig Layout { get; }
            public NestConfig Nests { get; }
            public Vector3 Start { get; }
            public Vector3[] Hives { get; }
            public Vector3[] Spawns { get; }
            public List<Vector3> Placed { get; }
        }

        private sealed class NearerFirst : IComparer<Vector3>
        {
            public static readonly NearerFirst Instance = new NearerFirst();

            public int Compare(Vector3 left, Vector3 right)
            {
                return GroundMath.DistanceSquared(left, Vector3.zero).CompareTo(GroundMath.DistanceSquared(right, Vector3.zero));
            }
        }
    }
}
