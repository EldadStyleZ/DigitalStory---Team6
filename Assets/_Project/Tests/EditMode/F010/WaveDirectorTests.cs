using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Uses the default WaveConfig: waves 3/0, 4/1, 4/3, 6/4, 8/5, first wave after FirstWaveDelaySeconds (20 s), 15 s breaks, 1.2 s spawn gaps.</summary>
    public sealed class WaveDirectorTests
    {
        private const float Step = 0.1f;
        private const int SpawnPoints = 4;
        private static readonly int[] Sticks = { 3, 4, 4, 6, 8 };
        private static readonly int[] Smokers = { 0, 1, 3, 4, 5 };
        private static readonly float FirstDelay = ReadFirstDelay();

        private sealed class Recorder
        {
            public readonly List<int> Started = new List<int>();
            public readonly List<(BeekeeperKind Kind, int Point)> Spawns = new List<(BeekeeperKind, int)>();
            public int ClearedCount;

            public Recorder(WaveDirector director)
            {
                director.WaveStarted += Started.Add;
                director.SpawnRequested += (kind, point) => Spawns.Add((kind, point));
                director.Cleared += () => ClearedCount++;
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

        private static float ReadFirstDelay()
        {
            var config = ScriptableObject.CreateInstance<WaveConfig>();
            float delay = config.FirstWaveDelaySeconds;
            Object.DestroyImmediate(config);
            return delay;
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

        /// <summary>Runs until a wave has started.</summary>
        private static void RunUntilStarted(WaveDirector director, int waveNumber, int alive)
        {
            int guard = 0;
            while (director.WaveNumber < waveNumber && guard++ < 10000)
            {
                director.Tick(Step, alive);
            }

            Assert.That(director.WaveNumber, Is.EqualTo(waveNumber), "the wave never started");
        }

        /// <summary>Plays one wave through: waits for its start, lets everything spawn while all of it lives, then kills everyone.</summary>
        private static void PlayWave(WaveDirector director, Recorder recorder, int waveNumber)
        {
            int guard = 0;
            while (director.WaveNumber < waveNumber && guard++ < 10000)
            {
                director.Tick(Step, 0);
            }

            int expected = Sticks[waveNumber - 1] + Smokers[waveNumber - 1];
            int before = recorder.Spawns.Count;
            guard = 0;
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
        public void New_WaitsForTheFirstWave()
        {
            var director = MakeDirector();

            Assert.That(director.WaveNumber, Is.EqualTo(0));
            Assert.That(director.WaveCount, Is.EqualTo(5));
            Assert.That(director.IsWaveRunning, Is.False);
            Assert.That(director.AllWavesCleared, Is.False);
            Assert.That(director.SecondsToNextWave, Is.EqualTo(FirstDelay).Within(0.001f));
        }

        [Test]
        public void Tick_BeforeTheFirstDelay_StartsNothing()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            Run(director, FirstDelay - 0.5f, 0);

            Assert.That(recorder.Started, Is.Empty);
            Assert.That(recorder.Spawns, Is.Empty);
            Assert.That(director.SecondsToNextWave, Is.EqualTo(0.5f).Within(0.01f));
        }

        [Test]
        public void Tick_AfterTheFirstDelay_StartsWaveOne()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            Run(director, FirstDelay + 0.2f, 0);

            Assert.That(recorder.Started, Is.EqualTo(new[] { 1 }));
            Assert.That(director.WaveNumber, Is.EqualTo(1));
            Assert.That(director.IsWaveRunning, Is.True);
            Assert.That(director.SecondsToNextWave, Is.EqualTo(0f));
        }

        [Test]
        public void WaveOne_SpawnsThreeSticksAndNoSmokers()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            Run(director, FirstDelay + 10f, 3);

            Assert.That(recorder.Count(BeekeeperKind.Stick), Is.EqualTo(3));
            Assert.That(recorder.Count(BeekeeperKind.Smoker), Is.EqualTo(0));
        }

        [Test]
        public void Spawns_AreSpacedBySpawnIntervalSeconds()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            // Wave one starts at the first delay. 0.5 s later only the first has spawned, 1.6 s later two, 2.9 s later three.
            Run(director, FirstDelay + 0.5f, 1);
            Assert.That(recorder.Spawns.Count, Is.EqualTo(1));

            Run(director, 1.1f, 1);
            Assert.That(recorder.Spawns.Count, Is.EqualTo(2));

            Run(director, 1.3f, 1);
            Assert.That(recorder.Spawns.Count, Is.EqualTo(3));
        }

        [Test]
        public void Spawns_UseSpawnPointsInRoundRobin()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            RunUntilStarted(director, 1, 0);

            Run(director, 40f, 1);
            // Wave one has three, and the next wave only starts after a clear, which never happens with one alive.
            Assert.That(recorder.Spawns.Count, Is.EqualTo(3));
            Assert.That(recorder.Spawns[0].Point, Is.EqualTo(0));
            Assert.That(recorder.Spawns[1].Point, Is.EqualTo(1));
            Assert.That(recorder.Spawns[2].Point, Is.EqualTo(2));
        }

        [Test]
        public void Spawns_PointIndexAlwaysStaysInRange()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            for (int wave = 1; wave <= 3; wave++)
            {
                PlayWave(director, recorder, wave);
                Run(director, 16f, 0);
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

            Run(director, FirstDelay + 10f, 1);

            Assert.That(recorder.Spawns[0].Point, Is.EqualTo(0));
            Assert.That(recorder.Spawns[1].Point, Is.EqualTo(1));
            Assert.That(recorder.Spawns[2].Point, Is.EqualTo(0));
        }

        [Test]
        public void WaveTwo_SpawnsSticksFirstThenTheSmoker()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            PlayWave(director, recorder, 1);
            recorder.Spawns.Clear();

            Run(director, 15.5f, 0);
            int guard = 0;
            while (recorder.Spawns.Count < 5 && guard++ < 1000)
            {
                director.Tick(Step, recorder.Spawns.Count);
            }

            Assert.That(recorder.Started, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(recorder.Spawns.Count, Is.EqualTo(5));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(recorder.Spawns[i].Kind, Is.EqualTo(BeekeeperKind.Stick));
            }

            Assert.That(recorder.Spawns[4].Kind, Is.EqualTo(BeekeeperKind.Smoker));
        }

        [Test]
        public void Wave_NotClearedWhileBeekeepersAreAlive()
        {
            var director = MakeDirector();
            RunUntilStarted(director, 1, 1);

            Run(director, 60f, 1);

            Assert.That(director.IsWaveRunning, Is.True);
            Assert.That(director.WaveNumber, Is.EqualTo(1));
            Assert.That(director.SecondsToNextWave, Is.EqualTo(0f));
        }

        [Test]
        public void Wave_NoneAliveButNotAllSpawned_IsNotCleared()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);

            // The first stick spawned and was killed at once; two sticks are still to come.
            Run(director, FirstDelay + 0.5f, 0);

            Assert.That(recorder.Spawns.Count, Is.EqualTo(1));
            Assert.That(director.IsWaveRunning, Is.True);
        }

        [Test]
        public void Wave_AllSpawnedAndNoneAlive_IsCleared()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            RunUntilStarted(director, 1, 0);
            Run(director, 5f, 3);

            director.Tick(Step, 0);

            Assert.That(recorder.Spawns.Count, Is.EqualTo(3));
            Assert.That(director.IsWaveRunning, Is.False);
            Assert.That(director.SecondsToNextWave, Is.GreaterThan(0f));
        }

        [Test]
        public void Break_LastsBreakSecondsAndThenStartsTheNextWave()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            PlayWave(director, recorder, 1);

            Assert.That(director.SecondsToNextWave, Is.EqualTo(15f).Within(0.3f));

            Run(director, 14f, 0);
            Assert.That(director.WaveNumber, Is.EqualTo(1));
            Assert.That(director.IsWaveRunning, Is.False);

            Run(director, 1.5f, 0);
            Assert.That(director.WaveNumber, Is.EqualTo(2));
            Assert.That(director.IsWaveRunning, Is.True);
            Assert.That(recorder.Started, Is.EqualTo(new[] { 1, 2 }));
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

            for (int wave = 1; wave <= 5; wave++)
            {
                PlayWave(director, recorder, wave);
                Assert.That(director.WaveNumber, Is.EqualTo(wave));
            }

            Assert.That(recorder.Count(BeekeeperKind.Stick), Is.EqualTo(25));
            Assert.That(recorder.Count(BeekeeperKind.Smoker), Is.EqualTo(13));
            Assert.That(recorder.Started, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(recorder.ClearedCount, Is.EqualTo(1));
            Assert.That(director.AllWavesCleared, Is.True);
            Assert.That(director.IsWaveRunning, Is.False);
            Assert.That(director.SecondsToNextWave, Is.EqualTo(0f));
        }

        [Test]
        public void AfterTheLastWave_FurtherTicksStartNothingAndDoNotRaiseClearedAgain()
        {
            var director = MakeDirector();
            var recorder = new Recorder(director);
            for (int wave = 1; wave <= 5; wave++)
            {
                PlayWave(director, recorder, wave);
            }
            int spawnsBefore = recorder.Spawns.Count;

            Run(director, 60f, 0);

            Assert.That(recorder.Spawns.Count, Is.EqualTo(spawnsBefore));
            Assert.That(recorder.Started.Count, Is.EqualTo(5));
            Assert.That(recorder.ClearedCount, Is.EqualTo(1));
            Assert.That(director.WaveNumber, Is.EqualTo(5));
        }

        [Test]
        public void WaveStarted_IsRaisedBeforeThatWavesFirstSpawn()
        {
            var director = MakeDirector();
            var order = new List<string>();
            director.WaveStarted += n => order.Add("start" + n);
            director.SpawnRequested += (_, __) => order.Add("spawn");

            Run(director, FirstDelay + 1f, 1);

            Assert.That(order[0], Is.EqualTo("start1"));
            Assert.That(order[1], Is.EqualTo("spawn"));
        }
    }
}
