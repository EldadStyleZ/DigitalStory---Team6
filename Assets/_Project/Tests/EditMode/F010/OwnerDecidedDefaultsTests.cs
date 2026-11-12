using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Pins the numbers the owner decided: a wasp twice as long, the growing swarm, nests and levels of plan section H, and
    /// the farm phases, prices and hive economy of plan sections L and M, as the playtest bot settled them
    /// (<see cref="EconomyPlaytestTests"/>). Other tests read the config, so only this one catches a reverted default.
    /// </summary>
    public sealed class OwnerDecidedDefaultsTests
    {
        // Plan section H: 800 wasps of a capacity of 800, which nests raise to 4,000 at most; the collapse below 12% of the
        // capacity, the respawn 50% and the low warning 30%. The floor of 15% is the old 300 of the old 2,000 start.
        [Test]
        public void SwarmConfig_HoldsTheGrowingSwarmOfPlanSectionH()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            Assert.AreEqual(800, config.StartingWasps);
            Assert.AreEqual(800, config.StartingCapacity);
            Assert.AreEqual(4000, config.MaxWasps);
            Assert.AreEqual(0.12f, config.CollapseShare, 0.0001f);
            Assert.AreEqual(0.5f, config.RespawnShare, 0.0001f);
            Assert.AreEqual(0.15f, config.MinRespawnShare, 0.0001f);
            Assert.AreEqual(0.3f, config.LowShare, 0.0001f);
            Assert.AreEqual(3f, config.CollapseBlockSeconds);
            Assert.AreEqual(0.24f, config.WaspLength, 0.0001f);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void NestConfig_HoldsTheNestsOfPlanSectionH()
        {
            var config = ScriptableObject.CreateInstance<NestConfig>();

            Assert.AreEqual(2.5f, config.CollectRadius);
            Assert.AreEqual(2f, config.CollectSeconds);
            Assert.AreEqual(400, config.CapacityPerNest);
            Assert.AreEqual(400, config.WaspsPerNest);
            Assert.AreEqual(2, config.HummedNotes);
            Assert.AreEqual(2, config.OpenAtStart);
            Assert.AreEqual(new[] { 2, 2, 1, 1 }, config.OpenAfterWave);
            Assert.AreEqual(8, config.NestCount);
            Assert.AreEqual(12f, config.HiveClearance);
            Assert.AreEqual(10f, config.FirstNestsNearest);
            Assert.AreEqual(14f, config.FirstNestsFarthest);
            Assert.AreEqual(16f, config.LaterNestsNearest);
            Assert.AreEqual(30f, config.LaterNestsFarthest);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void SwarmLevelConfig_HoldsTheTwelvePercentPerLevelOfPlanSectionH()
        {
            var config = ScriptableObject.CreateInstance<SwarmLevelConfig>();

            Assert.AreEqual(0.12f, config.SizePerLevel, 0.0001f);
            Assert.AreEqual(0.12f, config.DamagePerLevel, 0.0001f);

            Object.DestroyImmediate(config);
        }

        // The stick took 90 health from the playtest bot, so a lone Sting at the first level no longer kills one.
        [Test]
        public void BeekeeperAndSmokeConfigs_HoldTheScaledWaspLosses()
        {
            var beekeeper = ScriptableObject.CreateInstance<BeekeeperConfig>();
            var smoke = ScriptableObject.CreateInstance<SmokeConfig>();

            Assert.AreEqual(72, beekeeper.WaspsKnockedOff);
            Assert.AreEqual(90f, beekeeper.Health);
            Assert.AreEqual(6f, beekeeper.HiveDamage);
            Assert.AreEqual(4f, beekeeper.ProvokedSeconds, "plan section O: a stick a song hurt chases the core for 4 s");
            Assert.AreEqual(48f, smoke.WaspLossPerSecond);

            Object.DestroyImmediate(beekeeper);
            Object.DestroyImmediate(smoke);
        }

        // The playtest bot's waves: later ones bigger, because the swarm grows, and waves 4 and 5 most, so a player without
        // nests loses (plan section H), even with the towers and the rules of plan section O; a beekeeper every 2.5 s, so a
        // wave comes in over time instead of all at once.
        [Test]
        public void WaveConfig_HoldsTheFarmPhasesAndWavesOfPlanSectionL()
        {
            var config = ScriptableObject.CreateInstance<WaveConfig>();
            int[] sticks = { 4, 5, 5, 18, 20 };
            int[] smokers = { 0, 1, 3, 13, 13 };

            Assert.AreEqual(45f, config.FirstFarmSeconds);
            Assert.AreEqual(40f, config.FarmSeconds);
            Assert.AreEqual(5f, config.EarlyBonusPerSecond);
            Assert.AreEqual(2.5f, config.SpawnIntervalSeconds);
            Assert.AreEqual(5, config.Waves.Count);
            for (int wave = 0; wave < sticks.Length; wave++)
            {
                Assert.AreEqual(sticks[wave], config.Waves[wave].Sticks, "sticks in wave " + (wave + 1));
                Assert.AreEqual(smokers[wave], config.Waves[wave].Smokers, "smokers in wave " + (wave + 1));
            }

            Object.DestroyImmediate(config);
        }

        // Plan section I, the giant version 2: it stands 20 s, plus 2 s for each beekeeper it kills, 30 s at most.
        [Test]
        public void ColossusSong_StandsTheTimesOfPlanSectionI()
        {
            var song = ScriptableObject.CreateInstance<ColossusSong>();

            Assert.AreEqual(20f, song.Seconds);
            Assert.AreEqual(2f, song.SecondsPerKill);
            Assert.AreEqual(30f, song.MostSeconds);

            Object.DestroyImmediate(song);
        }

        // Plan section I: the slam, the roar and the two phrases, at level 1. The hit delays and the leap's throw are ours.
        [Test]
        public void ColossusConfig_HoldsTheMovesOfPlanSectionI()
        {
            var config = ScriptableObject.CreateInstance<ColossusConfig>();

            Assert.AreEqual(0.6f, config.PhraseWindowSeconds, 0.0001f);
            Assert.AreEqual(40f, config.SlamDamage);
            Assert.AreEqual(5f, config.SlamRadius);
            Assert.AreEqual(4f, config.SlamKnockback);
            Assert.AreEqual(3f, config.SlamCooldownSeconds);
            Assert.AreEqual(0.3f, config.SlamHitDelay, 0.0001f);
            Assert.AreEqual(6f, config.RoarRadius);
            Assert.AreEqual(1.2f, config.RoarStunSeconds, 0.0001f);
            Assert.AreEqual(6f, config.RoarCooldownSeconds);
            Assert.AreEqual(0.2f, config.RoarHitDelay, 0.0001f);
            Assert.AreEqual(55f, config.SpinKickDamage);
            Assert.AreEqual(3.5f, config.SpinKickRadius, 0.0001f);
            Assert.AreEqual(0.3f, config.SpinKickHitDelay, 0.0001f);
            Assert.AreEqual(10f, config.LeapRange);
            Assert.AreEqual(0.6f, config.LeapSeconds, 0.0001f);
            Assert.AreEqual(70f, config.LeapDamage);
            Assert.AreEqual(4f, config.LeapRadius);
            Assert.AreEqual(6f, config.LeapKnockback);

            Object.DestroyImmediate(config);
        }

        // The giant stays a highlight: 1 Fury point per 4 damage, 100 points to fill.
        [Test]
        public void FuryConfig_HoldsTheGiantsPriceThePlaytestBotSet()
        {
            var config = ScriptableObject.CreateInstance<FuryConfig>();

            Assert.AreEqual(100f, config.Max);
            Assert.AreEqual(4f, config.DamagePerFuryPoint);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void HiveConfig_HoldsTheEconomyOfPlanSectionM()
        {
            var config = ScriptableObject.CreateInstance<HiveConfig>();

            Assert.AreEqual(450f, config.Health);
            Assert.AreEqual(6f, config.ProductionPerSecond);
            Assert.AreEqual(0.5f, config.ProductionHealthFraction);
            Assert.AreEqual(300f, config.StoreCapacity);
            Assert.AreEqual(100f, config.StartStore);
            Assert.AreEqual(3f, config.CollectRadius);
            Assert.AreEqual(150f, config.CollectPerSecond);
            Assert.AreEqual(60f, config.HealWaspsPerSecond);
            Assert.AreEqual(3f, config.WaspsPerHealthPoint, "the playtest bot raised it from 2.5 once the Sting hit from its arrival");
            Assert.AreEqual(0.8f, config.DamagedStartFraction, 0.0001f);

            Object.DestroyImmediate(config);
        }

        // The Sting's is for each sting a cast sends (plan section O), and the playtest bot halved it and doubled the Nest's
        // after that section, so a cast at the starting 800 wasps still costs 10.
        [TestCase(typeof(StingSong), 5)]
        [TestCase(typeof(GustSong), 20)]
        [TestCase(typeof(DomeSong), 40)]
        [TestCase(typeof(TornadoSong), 60)]
        [TestCase(typeof(MeteorSong), 90)]
        [TestCase(typeof(HydraSong), 60)]
        [TestCase(typeof(ScytheSong), 30)]
        [TestCase(typeof(LanceSong), 70)]
        [TestCase(typeof(NestSong), 200)]
        [TestCase(typeof(HoneycombSong), 150)]
        [TestCase(typeof(ColossusSong), 0)]
        [TestCase(typeof(HiveCallSong), 0)]
        public void SongAssets_CostThePricesOfPlanSectionM(System.Type songType, int cost)
        {
            var song = (SwarmSongEffect)ScriptableObject.CreateInstance(songType);

            Assert.AreEqual(cost, song.WaspCost);

            Object.DestroyImmediate(song);
        }

        // Plan section H: the Hydra has 3 heads, then 4 from level 4 and 5 from level 7.
        [Test]
        public void HydraSong_GrowsTheHeadsOfPlanSectionH()
        {
            var hydra = ScriptableObject.CreateInstance<HydraSong>();

            Assert.AreEqual(3, hydra.HeadCount(1));
            Assert.AreEqual(3, hydra.HeadCount(3));
            Assert.AreEqual(4, hydra.HeadCount(4));
            Assert.AreEqual(4, hydra.HeadCount(6));
            Assert.AreEqual(5, hydra.HeadCount(7));

            Object.DestroyImmediate(hydra);
        }

        // Auto-aim's ThreeNearest rule holds as many beekeepers as the Hydra's most heads, so a fifth head has one to bite.
        [Test]
        public void AimConfig_HoldsANearestBeekeeperForEachOfTheHydrasMostHeads()
        {
            var config = ScriptableObject.CreateInstance<AimConfig>();
            var hydra = ScriptableObject.CreateInstance<HydraSong>();

            Assert.AreEqual(5, config.NearestCount);
            Assert.That(config.NearestCount, Is.GreaterThanOrEqualTo(hydra.HeadCount(int.MaxValue)));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(hydra);
        }

        // Plan section O: the Threat rule lists as many beekeepers as the Sting's most stings, so each can take its own.
        [Test]
        public void AimConfig_HoldsAThreatForEachOfTheStingsMostStings()
        {
            var config = ScriptableObject.CreateInstance<AimConfig>();
            var sting = ScriptableObject.CreateInstance<StingSong>();

            Assert.AreEqual(10, config.ThreatCount);
            Assert.That(config.ThreatCount, Is.GreaterThanOrEqualTo(sting.MaxStings));

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(sting);
        }
    }
}
