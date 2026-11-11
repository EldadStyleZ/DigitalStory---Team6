using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section L, on the default WaveConfig: five waves of sticks and then smokers; each round a farm phase and then its
    /// wave; the first farm phase lasts FirstFarmSeconds, later ones FarmSeconds; beekeepers SpawnIntervalSeconds apart.
    /// Every count and time is read from the config, whose numbers the playtest bot sets and OwnerDecidedDefaultsTests pins.
    /// </summary>
    public sealed class WaveDirectorTests
    {
        private const float Step = 0.1f;
        private const int SpawnPoints = 4;
        private static readonly int[] Sticks = ReadWaves(entry => entry.Sticks);
        private static readonly int[] Smokers = ReadWaves(entry => entry.Smokers);
        private static readonly float FirstFarm = ReadDefault(config => config.FirstFarmSeconds);
        private static readonly float Farm = ReadDefault(config => config.FarmSeconds);
        private static readonly float Interval = ReadDefault(config => config.SpawnIntervalSeconds);

        private sealed class Recorder
        {
            public readonly List<int> Started = new List<int>();
            public readonly List<MatchPhase> Phases = new List<MatchPhase>();
            public readonly List<float> CalledEarly = new List<float>();
            public readonly List<(BeekeeperKind Kind, int Point)> Spawns = new List<(BeekeeperKind, int)>();
            public readonly List<int> WavesCleared = new List<int>();
            public int ClearedCount;

            public Recorder(WaveDirector director)
            {
                director.PhaseChanged += phase =>
                {
                    Phases.Add(phase);
                    if (phase == MatchPhase.Survive)
                    {
                        Started.Add(director.Round);
                    }
                };
                director.WaveCalledEarly += CalledEarly.Add;
                director.SpawnRequested += (kind, point) => Spawns.Add((kind, point));
                director.Cleared += () => ClearedCount++;
                director.WaveCleared += WavesCleared.Add;
            }

            public int Count(BeekeeperKind kind)
            {
                int count = 0;
                foreach (var spawn in Spawns)
                {
                    if (spawn.Kind == kind)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private static float ReadDefault(Func<WaveConfig, float> read)
        {
            var config = ScriptableObject.CreateInstance<WaveConfig>();
            float value = read(config);
            Object.DestroyImmediate(config);
            return value;
        }

        private static int[] ReadWaves(Func<WaveEntry, int> read)
        {
            var config = ScriptableObject.CreateInstance<WaveConfig>();
            var counts = new int[config.Waves.Count];
            for (int wave = 0; wave < counts.Length; wave++)
            {
                counts[wave] = read(config.Waves[wave]);
            }

            Object.DestroyImmediate(config);
            return counts;
        }

        private static int Total(int round)
        {
            return Sticks[round - 1] + Smokers[round - 1];
        }

        // Seconds from a wave's start until all of it has spawned, and a little more.
        private static float AllSpawnedSeconds(int round)
        {
            return (Interval * Total(round)) + 0.5f;
        }

        private static int Sum(int[] counts)
        {
            int sum = 0;
            foreach (int count in counts)
            {
                sum += count;
            }

            return sum;
        }

        private static WaveDirector MakeDirector()
        {
            return new WaveDirector(ScriptableObject.CreateInstance<WaveConfig>(), SpawnPoints);
        }

        private static void Run(WaveDirector director, float seconds, int alive)
        {
            int steps = Mathf.RoundToInt(seconds / Step);
            for (int i = 0; i < steps; i++)
            {
                director.Tick(Step, alive);
            }
        }

        /// <summary>Runs until the wave of a round has started.</summary>
        private static void RunUntilWaveStarts(WaveDirector director, int round, int alive)
        {
            int guard = 0;
            while (!(director.Round == round && director.Phase == MatchPhase.Survive) && guard++ < 10000)
            {
                director.Tick(Step, alive);
            }

            Assert.That(director.Round, Is.EqualTo(round), "the wave never started");
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Survive), "the wave never started");
        }

        /// <summary>Plays one wave through: waits for its start, lets everything spawn while all of it lives, then kills everyone.</summary>
        private static void PlayWave(WaveDirector director, Recorder recorder, int round)
        {
            RunUntilWaveStarts(director, round, 0);

            int expected = Total(round);
            int before = recorder.Spawns.Count - 1;
            int guard = 0;
            while (recorder.Spawns.Count - before < expected && guard++ < 10000)
            {
                director.Tick(Step, 1);
            }

            director.Tick(Step, 0);
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.That(() => new WaveDirector(null, SpawnPoints), Throws.ArgumentNullException);
        }

        [Test]
        public void New_StartsInTheFirstFarmPhaseOfRoundOne()
        {
            var director = MakeDirector();

            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(director.Round, Is.EqualTo(1));
            Assert.That(director.RoundCount, Is.EqualTo(5));
            Assert.That(director.AllWavesCleared, Is.False);
            Assert.That(director.FarmSecondsLeft, Is.EqualTo(FirstFarm).Within(0.001f));
        }

        [Test]
        public void Tick_BeforeTheFirstFarmPhaseEnds_SpawnsNobody()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            Run(director, FirstFarm - 0.5f, 0);

            Assert.That(recorder.Started, Is.Empty);
            Assert.That(recorder.Phases, Is.Empty);
            Assert.That(recorder.Spawns, Is.Empty, "no beekeeper comes in a farm phase");
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(director.FarmSecondsLeft, Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void Tick_FirstFarmPhaseEnds_StartsWaveOneInASurvivePhase()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            Run(director, FirstFarm + 0.2f, 0);

            Assert.That(recorder.Started, Is.EqualTo(new[] { 1 }));
            Assert.That(recorder.Phases, Is.EqualTo(new[] { MatchPhase.Survive }));
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(director.Round, Is.EqualTo(1));
            Assert.That(director.FarmSecondsLeft, Is.EqualTo(0f));
            Assert.That(recorder.Spawns.Count, Is.EqualTo(1), "the first beekeeper comes with the wave");
        }

        [Test]
        public void WaveOne_SpawnsItsSticksAndItsSmokers()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            Run(director, FirstFarm + AllSpawnedSeconds(1), 1);

            Assert.That(recorder.Count(BeekeeperKind.Stick), Is.EqualTo(Sticks[0]));
            Assert.That(recorder.Count(BeekeeperKind.Smoker), Is.EqualTo(Smokers[0]));
        }

        [Test]
        public void Spawns_AreSpacedBySpawnIntervalSeconds()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            Assert.That(Total(1), Is.GreaterThanOrEqualTo(3), "the test needs three beekeepers in wave one");

            // Wave one starts when the first farm phase ends. A little later only the first has spawned, one interval more
            // and two have, another and three have.
            Run(director, FirstFarm + (Interval * 0.4f), 1);
            Assert.That(recorder.Spawns.Count, Is.EqualTo(1));

            Run(director, Interval, 1);
            Assert.That(recorder.Spawns.Count, Is.EqualTo(2));

            Run(director, Interval, 1);
            Assert.That(recorder.Spawns.Count, Is.EqualTo(3));
        }

        [Test]
        public void Spawns_UseSpawnPointsInRoundRobin()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            RunUntilWaveStarts(director, 1, 0);

            Run(director, AllSpawnedSeconds(1), 1);

            // The whole of wave one, and the next farm phase only starts after a clear, which never happens with one alive.
            Assert.That(recorder.Spawns.Count, Is.EqualTo(Total(1)));
            for (int spawn = 0; spawn < recorder.Spawns.Count; spawn++)
            {
                Assert.That(recorder.Spawns[spawn].Point, Is.EqualTo(spawn % SpawnPoints));
            }
        }

        [Test]
        public void Spawns_PointIndexAlwaysStaysInRange()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            for (int round = 1; round <= 3; round++)
            {
                PlayWave(director, recorder, round);
            }

            Assert.That(recorder.Spawns.Count, Is.GreaterThan(SpawnPoints));
            foreach (var spawn in recorder.Spawns)
            {
                Assert.That(spawn.Point, Is.InRange(0, SpawnPoints - 1));
            }
        }

        [Test]
        public void Spawns_WithTwoPoints_WrapsAroundToTheFirst()
        {
            var director = new WaveDirector(ScriptableObject.CreateInstance<WaveConfig>(), 2);
            var recorder = new Recorder(director);
            Assert.That(Total(1), Is.GreaterThanOrEqualTo(3), "the test needs three beekeepers in wave one");

            Run(director, FirstFarm + AllSpawnedSeconds(1), 1);

            Assert.That(recorder.Spawns[0].Point, Is.EqualTo(0));
            Assert.That(recorder.Spawns[1].Point, Is.EqualTo(1));
            Assert.That(recorder.Spawns[2].Point, Is.EqualTo(0));
        }

        [Test]
        public void WaveTwo_SpawnsSticksFirstThenTheSmokers()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            PlayWave(director, recorder, 1);
            recorder.Spawns.Clear();
            Assert.That(Smokers[1], Is.GreaterThan(0), "the test needs a smoker in wave two");

            Run(director, Farm + 0.5f, 0);
            int guard = 0;
            while (recorder.Spawns.Count < Total(2) && guard++ < 10000)
            {
                director.Tick(Step, recorder.Spawns.Count);
            }

            Assert.That(recorder.Started, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(recorder.Spawns.Count, Is.EqualTo(Total(2)));
            for (int i = 0; i < recorder.Spawns.Count; i++)
            {
                BeekeeperKind expected = i < Sticks[1] ? BeekeeperKind.Stick : BeekeeperKind.Smoker;
                Assert.That(recorder.Spawns[i].Kind, Is.EqualTo(expected), "spawn " + i);
            }
        }

        [Test]
        public void Wave_NotClearedWhileBeekeepersAreAlive()
        {
            var director = MakeDirector();
            RunUntilWaveStarts(director, 1, 1);

            Run(director, 60f, 1);

            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(director.Round, Is.EqualTo(1));
            Assert.That(director.FarmSecondsLeft, Is.EqualTo(0f));
        }

        [Test]
        public void Wave_NoneAliveButNotAllSpawned_IsNotCleared()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            // The first stick spawned and was killed at once; the rest of the wave is still to come.
            Run(director, FirstFarm + (Interval * 0.4f), 0);

            Assert.That(recorder.Spawns.Count, Is.EqualTo(1));
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Survive));
        }

        [Test]
        public void Wave_AllSpawnedAndNoneAlive_ReturnsToAFarmPhaseOfTheNextRound()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            RunUntilWaveStarts(director, 1, 0);
            Run(director, AllSpawnedSeconds(1), 1);

            director.Tick(Step, 0);

            Assert.That(recorder.Spawns.Count, Is.EqualTo(Total(1)));
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(director.Round, Is.EqualTo(2));
            Assert.That(director.FarmSecondsLeft, Is.EqualTo(Farm).Within(0.001f));
            Assert.That(recorder.Phases, Is.EqualTo(new[] { MatchPhase.Survive, MatchPhase.Farm }));
        }

        [Test]
        public void FarmPhase_AfterAWave_LastsFarmSecondsWithNobodyAndThenStartsTheNextWave()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            PlayWave(director, recorder, 1);
            int spawned = recorder.Spawns.Count;

            Assert.That(director.FarmSecondsLeft, Is.EqualTo(Farm).Within(0.001f));

            Run(director, Farm - 1f, 0);
            Assert.That(director.Round, Is.EqualTo(2));
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(recorder.Spawns.Count, Is.EqualTo(spawned), "no beekeeper comes in a farm phase");

            Run(director, 1.5f, 0);
            Assert.That(director.Round, Is.EqualTo(2));
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(recorder.Started, Is.EqualTo(new[] { 1, 2 }));
        }

        // Plan section H: nests open when a wave is cleared, so the clear itself is an event, with the wave's number.
        [Test]
        public void WaveCleared_IsRaisedWithTheWavesNumberOnTheTickItIsCleared()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            RunUntilWaveStarts(director, 1, 0);
            Run(director, AllSpawnedSeconds(1), 1);
            Assert.That(recorder.WavesCleared, Is.Empty, "not while beekeepers live");
            int roundSeen = -1;
            MatchPhase phaseSeen = MatchPhase.Farm;
            director.WaveCleared += wave =>
            {
                roundSeen = director.Round;
                phaseSeen = director.Phase;
            };

            director.Tick(Step, 0);

            Assert.That(recorder.WavesCleared, Is.EqualTo(new[] { 1 }));
            Assert.That(roundSeen, Is.EqualTo(1), "raised before the next round's farm phase starts");
            Assert.That(phaseSeen, Is.EqualTo(MatchPhase.Survive));
        }

        [Test]
        public void WaveCleared_EveryWaveInOrder_TheLastBeforeCleared()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            int clearedWhenTheLastWaveWas = -1;
            director.WaveCleared += wave =>
            {
                if (wave == 5)
                {
                    clearedWhenTheLastWaveWas = recorder.ClearedCount;
                }
            };

            for (int round = 1; round <= 5; round++)
            {
                PlayWave(director, recorder, round);
            }

            Assert.That(recorder.WavesCleared, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(clearedWhenTheLastWaveWas, Is.EqualTo(0));
            Assert.That(recorder.ClearedCount, Is.EqualTo(1));
        }

        [Test]
        public void WaveCleared_NotRaisedByCallingAWaveEarly()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            director.CallWave();

            Assert.That(recorder.WavesCleared, Is.Empty);
        }

        [Test]
        public void Cleared_NotRaisedBeforeTheLastWave()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            PlayWave(director, recorder, 1);

            Assert.That(recorder.ClearedCount, Is.EqualTo(0));
            Assert.That(director.AllWavesCleared, Is.False);
        }

        [Test]
        public void AllFiveWaves_SpawnTheConfiguredCountsAndRaiseClearedOnce()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            for (int round = 1; round <= 5; round++)
            {
                PlayWave(director, recorder, round);
                if (round < 5)
                {
                    Assert.That(director.Round, Is.EqualTo(round + 1));
                    Assert.That(director.Phase, Is.EqualTo(MatchPhase.Farm));
                }
            }

            Assert.That(recorder.Count(BeekeeperKind.Stick), Is.EqualTo(Sum(Sticks)));
            Assert.That(recorder.Count(BeekeeperKind.Smoker), Is.EqualTo(Sum(Smokers)));
            Assert.That(recorder.Started, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(recorder.ClearedCount, Is.EqualTo(1));
            Assert.That(director.AllWavesCleared, Is.True);
            Assert.That(director.Round, Is.EqualTo(5));
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Survive), "no farm phase follows the last wave: the match is won");
            Assert.That(recorder.Phases.Count, Is.EqualTo(9), "five waves and the four farm phases between them");
            Assert.That(director.FarmSecondsLeft, Is.EqualTo(0f));
        }

        [Test]
        public void AfterTheLastWave_FurtherTicksStartNothingAndDoNotRaiseClearedAgain()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            for (int round = 1; round <= 5; round++)
            {
                PlayWave(director, recorder, round);
            }

            int spawnsBefore = recorder.Spawns.Count;
            int phasesBefore = recorder.Phases.Count;

            Run(director, 60f, 0);

            Assert.That(recorder.Spawns.Count, Is.EqualTo(spawnsBefore));
            Assert.That(recorder.Phases.Count, Is.EqualTo(phasesBefore));
            Assert.That(recorder.ClearedCount, Is.EqualTo(1));
            Assert.That(director.Round, Is.EqualTo(5));
        }

        [Test]
        public void PhaseChanged_ToSurvive_IsRaisedBeforeThatWavesFirstSpawn()
        {
            var director = MakeDirector();
            var order = new List<string>();
            director.PhaseChanged += phase => order.Add(phase + "" + director.Round);
            director.SpawnRequested += (_, __) => order.Add("spawn");

            Run(director, FirstFarm + 1f, 1);

            Assert.That(order[0], Is.EqualTo("Survive1"));
            Assert.That(order[1], Is.EqualTo("spawn"));
        }

        // ---- Calling the wave early ----

        [Test]
        public void CallWave_InTheFirstFarmPhase_StartsTheWaveAtOnceAndReturnsTheSecondsSkipped()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            Run(director, 10f, 0);

            float skipped = director.CallWave();

            Assert.That(skipped, Is.EqualTo(FirstFarm - 10f).Within(0.01f));
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(director.Round, Is.EqualTo(1));
            Assert.That(director.FarmSecondsLeft, Is.EqualTo(0f));
            Assert.That(recorder.Started, Is.EqualTo(new[] { 1 }));
            Assert.That(recorder.Spawns.Count, Is.EqualTo(1), "the first beekeeper comes at once");
        }

        [Test]
        public void CallWave_InAFarmPhase_RaisesWaveCalledEarlyWithTheSecondsSkipped()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            Run(director, 10f, 0);

            float skipped = director.CallWave();

            Assert.That(recorder.CalledEarly, Is.EqualTo(new[] { skipped }));
        }

        [Test]
        public void CallWave_ThenTicks_SpawnsTheRestOfTheWaveOnTime()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            director.CallWave();

            Run(director, Interval - 0.1f, 1);
            Assert.That(recorder.Spawns.Count, Is.EqualTo(1));

            Run(director, 0.2f, 1);
            Assert.That(recorder.Spawns.Count, Is.EqualTo(2));
        }

        [Test]
        public void CallWave_InALaterFarmPhase_StartsThatRoundsWave()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            PlayWave(director, recorder, 1);
            Run(director, 5f, 0);

            float skipped = director.CallWave();

            Assert.That(skipped, Is.EqualTo(Farm - 5f).Within(0.01f));
            Assert.That(director.Round, Is.EqualTo(2));
            Assert.That(director.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(recorder.Started, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void CallWave_InASurvivePhase_DoesNothing()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            RunUntilWaveStarts(director, 1, 1);
            int spawned = recorder.Spawns.Count;

            float skipped = director.CallWave();

            Assert.That(skipped, Is.EqualTo(0f));
            Assert.That(recorder.CalledEarly, Is.Empty);
            Assert.That(recorder.Phases, Is.EqualTo(new[] { MatchPhase.Survive }));
            Assert.That(recorder.Spawns.Count, Is.EqualTo(spawned));
            Assert.That(director.Round, Is.EqualTo(1));
        }

        [Test]
        public void CallWave_AfterTheLastWave_DoesNothing()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            for (int round = 1; round <= 5; round++)
            {
                PlayWave(director, recorder, round);
            }

            float skipped = director.CallWave();

            Assert.That(skipped, Is.EqualTo(0f));
            Assert.That(recorder.CalledEarly, Is.Empty);
            Assert.That(recorder.Started.Count, Is.EqualTo(5));
        }
    }
}
