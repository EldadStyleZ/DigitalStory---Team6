using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section S, the random map: the same seed draws the same map, and over 200 seeds every map keeps every rule of the
    /// default LayoutConfig and NestConfig, with no fallback. The rules are checked here from the config's numbers, not with
    /// the generator's own helpers. The props stand in for the scene builder's: 70 of its 13 kinds, from bushes about 1 m
    /// across to ruins about 9 m across.
    /// </summary>
    public sealed class LayoutGeneratorTests
    {
        private const int SeedCount = 200;
        private const int PropCount = 70;
        private const float PlayArea = 34f; // CoreMoveConfig.PlayAreaHalfSize's default.
        private const float Tolerance = 0.001f;
        private const float AngleTolerance = 0.01f;
        private const float WestBearing = 270f;
        private const float EastBearing = 90f;
        private const float LeastPlacedShare = 0.9f;
        private const int LeastDistinctMaps = 190;

        // The four spawn points' sides of the hives' line: ahead, behind, beyond the east hive, beyond the west hive.
        private static readonly float[] SpawnSides = { 0f, 180f, 90f, 270f };

        // Ground radii like the builder's props: two pines, a dead tree, four bushes, four ruins and two shacks.
        private static readonly float[] Kinds = { 3.2f, 3.2f, 2.5f, 1f, 0.8f, 0.7f, 0.9f, 4.5f, 3.5f, 4f, 3f, 2.5f, 2.5f };

        private LayoutConfig _layout;
        private NestConfig _nests;
        private float[] _footprints;
        private ArenaLayout[] _maps;

        [OneTimeSetUp]
        public void DrawEveryMap()
        {
            _layout = ScriptableObject.CreateInstance<LayoutConfig>();
            _nests = ScriptableObject.CreateInstance<NestConfig>();
            _footprints = new float[PropCount];
            for (int index = 0; index < PropCount; index++)
            {
                _footprints[index] = Kinds[index % Kinds.Length];
            }

            _maps = new ArenaLayout[SeedCount];
            for (int seed = 0; seed < SeedCount; seed++)
            {
                _maps[seed] = LayoutGenerator.Generate(seed, Vector3.zero, _layout, _nests, _footprints);
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_layout);
            Object.DestroyImmediate(_nests);
        }

        [Test]
        public void Generate_TheSameSeedTwice_DrawsTheSameMap()
        {
            ArenaLayout first = LayoutGenerator.Generate(77, Vector3.zero, _layout, _nests, _footprints);
            ArenaLayout second = LayoutGenerator.Generate(77, Vector3.zero, _layout, _nests, _footprints);

            Assert.That(second.Seed, Is.EqualTo(77));
            Assert.That(second.Hives, Is.EqualTo(first.Hives));
            Assert.That(second.Spawns, Is.EqualTo(first.Spawns));
            Assert.That(second.Nests, Is.EqualTo(first.Nests));
            for (int index = 0; index < PropCount; index++)
            {
                Assert.That(second.Scenery[index].IsPlaced, Is.EqualTo(first.Scenery[index].IsPlaced), $"prop {index}");
                Assert.That(second.Scenery[index].Position, Is.EqualTo(first.Scenery[index].Position), $"prop {index}");
                Assert.That(second.Scenery[index].YawDegrees, Is.EqualTo(first.Scenery[index].YawDegrees), $"prop {index}");
            }
        }

        [Test]
        public void Generate_TwoHundredSeeds_DrawAsManyDifferentMaps()
        {
            var west = new HashSet<Vector3>();
            var firstNest = new HashSet<Vector3>();
            foreach (ArenaLayout map in _maps)
            {
                west.Add(map.Hives[0]);
                firstNest.Add(map.Nests[0]);
            }

            Assert.That(west.Count, Is.GreaterThanOrEqualTo(LeastDistinctMaps), "the west hive hardly moves between seeds");
            Assert.That(firstNest.Count, Is.GreaterThanOrEqualTo(LeastDistinctMaps), "the first nest hardly moves between seeds");
        }

        [Test]
        public void Generate_TwoHundredSeeds_NeverNeedTheFallback()
        {
            foreach (ArenaLayout map in _maps)
            {
                Assert.That(map.IsFallback, Is.False, $"seed {map.Seed}");
            }
        }

        [Test]
        public void Generate_TwoHundredSeeds_HivesStayInTheirBandWestAndEastAndApart()
        {
            foreach (ArenaLayout map in _maps)
            {
                CheckHives(map, $"seed {map.Seed}");
            }
        }

        [Test]
        public void Generate_TwoHundredSeeds_SpawnPointsTurnWithTheHivesKeepTheirDistanceAndLeanWest()
        {
            foreach (ArenaLayout map in _maps)
            {
                CheckSpawns(map, $"seed {map.Seed}");
            }
        }

        [Test]
        public void Generate_TwoHundredSeeds_NestsKeepEveryRule()
        {
            foreach (ArenaLayout map in _maps)
            {
                CheckNests(map, $"seed {map.Seed}");
            }
        }

        [Test]
        public void Generate_TwoHundredSeeds_EveryPlacedPropKeepsEveryRule()
        {
            foreach (ArenaLayout map in _maps)
            {
                CheckScenery(map, $"seed {map.Seed}");
            }
        }

        [Test]
        public void Generate_TwoHundredSeeds_PlaceAtLeastNineTenthsOfTheProps()
        {
            foreach (ArenaLayout map in _maps)
            {
                Assert.That(map.Scenery.Count, Is.EqualTo(PropCount));
                int placed = 0;
                foreach (SceneryPlacement prop in map.Scenery)
                {
                    placed += prop.IsPlaced ? 1 : 0;
                }

                Assert.That(placed, Is.GreaterThanOrEqualTo(Mathf.CeilToInt(PropCount * LeastPlacedShare)), $"seed {map.Seed}");
            }
        }

        [Test]
        public void Generate_NoMapCanKeepTheRules_UsesTheFallbackMap_AndStillPlacesTheScenery()
        {
            var impossible = ScriptableObject.CreateInstance<LayoutConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"<HiveLeastSeparation>k__BackingField\":1000}", impossible);

                ArenaLayout map = LayoutGenerator.Generate(5, Vector3.zero, impossible, _nests, _footprints);

                Assert.That(map.IsFallback, Is.True);
                Assert.That(map.Hives, Is.EqualTo(impossible.FallbackHives));
                Assert.That(map.Spawns, Is.EqualTo(impossible.FallbackSpawns));
                Assert.That(map.Nests, Is.EqualTo(impossible.FallbackNests));
                CheckScenery(map, "fallback");
            }
            finally
            {
                Object.DestroyImmediate(impossible);
            }
        }

        [Test]
        public void FallbackMap_OfTheDefaultConfig_KeepsEveryRule()
        {
            var map = new ArenaLayout(
                0, true, _layout.FallbackHives, _layout.FallbackSpawns, _layout.FallbackNests, new SceneryPlacement[0]);

            CheckHives(map, "fallback");
            CheckSpawns(map, "fallback");
            CheckNests(map, "fallback");
        }

        [Test]
        public void Generate_MissingArguments_Throw()
        {
            Assert.That(() => LayoutGenerator.Generate(1, Vector3.zero, null, _nests, _footprints), Throws.ArgumentNullException);
            Assert.That(() => LayoutGenerator.Generate(1, Vector3.zero, _layout, null, _footprints), Throws.ArgumentNullException);
            Assert.That(() => LayoutGenerator.Generate(1, Vector3.zero, _layout, _nests, null), Throws.ArgumentNullException);
        }

        [Test]
        public void Generate_ANegativeFootprint_Throws()
        {
            Assert.That(
                () => LayoutGenerator.Generate(1, Vector3.zero, _layout, _nests, new[] { 1f, -0.5f }),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        private void CheckHives(ArenaLayout map, string which)
        {
            Assert.That(map.Hives.Count, Is.EqualTo(2), which);
            Vector3 west = map.Hives[0];
            Vector3 east = map.Hives[1];
            Assert.That(west.x, Is.LessThan(0f), $"{which}: the first hive is not west of the middle");
            Assert.That(east.x, Is.GreaterThan(0f), $"{which}: the second hive is not east of the middle");
            for (int index = 0; index < 2; index++)
            {
                Vector3 hive = map.Hives[index];
                Assert.That(hive.y, Is.EqualTo(0f), which);
                float radius = Radius(hive);
                Assert.That(radius, Is.InRange(_layout.HiveNearest - Tolerance, _layout.HiveFarthest + Tolerance), $"{which}: hive {index}");
                float due = index == 0 ? WestBearing : EastBearing;
                Assert.That(
                    Mathf.Abs(Mathf.DeltaAngle(due, Bearing(hive))),
                    Is.LessThanOrEqualTo(_layout.HiveTiltDegrees + AngleTolerance),
                    $"{which}: hive {index} turns too far off its side");
            }

            Assert.That(Ground(west, east), Is.GreaterThanOrEqualTo(_layout.HiveLeastSeparation - Tolerance), $"{which}: the hives are too close");
        }

        private void CheckSpawns(ArenaLayout map, string which)
        {
            Assert.That(map.Spawns.Count, Is.EqualTo(SpawnSides.Length), which);
            float turn = Turn(map);
            for (int index = 0; index < map.Spawns.Count; index++)
            {
                Vector3 spawn = map.Spawns[index];
                string name = $"{which}: spawn {index}";
                Assert.That(spawn.y, Is.EqualTo(0f), name);
                Assert.That(Radius(spawn), Is.InRange(_layout.SpawnNearest - Tolerance, _layout.SpawnFarthest + Tolerance), name);
                Assert.That(Mathf.Abs(spawn.x), Is.LessThanOrEqualTo(_layout.ArenaHalfSize + Tolerance), name);
                Assert.That(Mathf.Abs(spawn.z), Is.LessThanOrEqualTo(_layout.ArenaHalfSize + Tolerance), name);
                Assert.That(
                    Mathf.Abs(Mathf.DeltaAngle(turn + SpawnSides[index], Bearing(spawn))),
                    Is.LessThanOrEqualTo(_layout.SpawnJitterDegrees + AngleTolerance),
                    $"{name} does not turn with the hives");
                foreach (Vector3 hive in map.Hives)
                {
                    Assert.That(Ground(spawn, hive), Is.GreaterThanOrEqualTo(_layout.SpawnHiveLeastDistance - Tolerance), $"{name} is too near a hive");
                }
            }

            for (int index = 0; index < 2; index++)
            {
                float lean = Ground(map.Spawns[index], map.Hives[1]) - Ground(map.Spawns[index], map.Hives[0]);
                Assert.That(lean, Is.GreaterThanOrEqualTo(_layout.SpawnWestLeanMetres - Tolerance), $"{which}: spawn {index} does not lean to the west hive");
            }

            Assert.That(Ground(map.Spawns[2], map.Hives[1]), Is.LessThan(Ground(map.Spawns[2], map.Hives[0])), $"{which}: spawn 2 is not beyond the east hive");
            Assert.That(Ground(map.Spawns[3], map.Hives[0]), Is.LessThan(Ground(map.Spawns[3], map.Hives[1])), $"{which}: spawn 3 is not beyond the west hive");
        }

        private void CheckNests(ArenaLayout map, string which)
        {
            Assert.That(() => NestLayout.Check(map.Nests, map.Hives, _nests, PlayArea), Throws.Nothing, which);
            float turn = Turn(map);
            for (int index = 0; index < map.Nests.Count; index++)
            {
                Vector3 nest = map.Nests[index];
                string name = $"{which}: nest {index}";
                Assert.That(nest.y, Is.EqualTo(0f), name);
                Assert.That(Mathf.Abs(nest.x), Is.LessThanOrEqualTo(_layout.NestInsideHalfSize + Tolerance), name);
                Assert.That(Mathf.Abs(nest.z), Is.LessThanOrEqualTo(_layout.NestInsideHalfSize + Tolerance), name);
                Assert.That(Ground(nest, Vector3.zero), Is.GreaterThanOrEqualTo(_layout.NestStartClearance - Tolerance), $"{name} is on the start");
                foreach (Vector3 hive in map.Hives)
                {
                    Assert.That(Ground(nest, hive), Is.GreaterThanOrEqualTo(_nests.HiveClearance + _layout.NestHiveMargin - Tolerance), $"{name} is near a hive");
                }

                foreach (Vector3 spawn in map.Spawns)
                {
                    Assert.That(Ground(nest, spawn), Is.GreaterThanOrEqualTo(_layout.NestSpawnClearance - Tolerance), $"{name} is near a spawn point");
                }

                for (int other = index + 1; other < map.Nests.Count; other++)
                {
                    Assert.That(Ground(nest, map.Nests[other]), Is.GreaterThanOrEqualTo(_layout.NestLeastSpacing - Tolerance), $"{name} is near nest {other}");
                }

                if (index < _nests.OpenAtStart)
                {
                    float offAhead = Mathf.DeltaAngle(turn, Bearing(nest));
                    float side = index % 2 == 0 ? -1f : 1f;
                    Assert.That(
                        offAhead * side,
                        Is.InRange(_layout.FirstNestsNarrowestBearing - AngleTolerance, _layout.FirstNestsWidestBearing + AngleTolerance),
                        $"{name}, open at the start, is not ahead on its side");
                }
            }
        }

        private void CheckScenery(ArenaLayout map, string which)
        {
            var lanes = new List<(Vector3 From, Vector3 To)>();
            foreach (Vector3 spawn in map.Spawns)
            {
                foreach (Vector3 hive in map.Hives)
                {
                    lanes.Add((spawn, hive));
                }
            }

            for (int index = 0; index < map.Scenery.Count; index++)
            {
                SceneryPlacement prop = map.Scenery[index];
                if (!prop.IsPlaced)
                {
                    continue;
                }

                string name = $"{which}: prop {index}";
                float radius = _footprints[index];
                Vector3 at = prop.Position;
                float edge = Mathf.Max(Mathf.Abs(at.x), Mathf.Abs(at.z));
                Assert.That(at.y, Is.EqualTo(0f), name);
                Assert.That(edge, Is.InRange(_layout.SceneryInnerHalfSize - Tolerance, _layout.ArenaHalfSize + Tolerance), $"{name} is not around the edge");
                Assert.That(prop.YawDegrees, Is.InRange(0f, 360f), name);
                Assert.That(Ground(at, Vector3.zero), Is.GreaterThanOrEqualTo(_layout.SceneryStartClearance + radius - Tolerance), $"{name} is on the start");
                foreach (Vector3 hive in map.Hives)
                {
                    Assert.That(Ground(at, hive), Is.GreaterThanOrEqualTo(_layout.SceneryHiveClearance + radius - Tolerance), $"{name} is in a hive's rings");
                }

                foreach (Vector3 nest in map.Nests)
                {
                    Assert.That(Ground(at, nest), Is.GreaterThanOrEqualTo(_layout.SceneryNestClearance + radius - Tolerance), $"{name} is on a nest tree");
                }

                foreach (Vector3 spawn in map.Spawns)
                {
                    Assert.That(Ground(at, spawn), Is.GreaterThanOrEqualTo(_layout.ScenerySpawnClearance + radius - Tolerance), $"{name} is on a spawn point");
                }

                foreach ((Vector3 from, Vector3 to) in lanes)
                {
                    Assert.That(ToSegment(at, from, to), Is.GreaterThanOrEqualTo(_layout.LaneHalfWidth + radius - Tolerance), $"{name} stands in a lane");
                }

                for (int other = index + 1; other < map.Scenery.Count; other++)
                {
                    if (!map.Scenery[other].IsPlaced)
                    {
                        continue;
                    }

                    float least = Mathf.Max(_layout.SceneryLeastSpacing, _layout.SceneryFootprintShare * (radius + _footprints[other]));
                    Assert.That(Ground(at, map.Scenery[other].Position), Is.GreaterThanOrEqualTo(least - Tolerance), $"{name} stands in prop {other}");
                }
            }
        }

        // Degrees the hives' line turns off due east: the spawn points and the nests turn with it.
        private static float Turn(ArenaLayout map) => Bearing(map.Hives[1] - map.Hives[0]) - EastBearing;

        // Degrees clockwise from +Z, seen from above, from 0 to 360.
        private static float Bearing(Vector3 point) => Mathf.Repeat(Mathf.Atan2(point.x, point.z) * Mathf.Rad2Deg, 360f);

        private static float Radius(Vector3 point) => new Vector2(point.x, point.z).magnitude;

        private static float Ground(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        private static float ToSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            var p = new Vector2(point.x, point.z);
            var a = new Vector2(from.x, from.z);
            var b = new Vector2(to.x, to.z);
            Vector2 along = b - a;
            float share = along.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, along) / along.sqrMagnitude) : 0f;
            return (p - (a + (along * share))).magnitude;
        }
    }
}
