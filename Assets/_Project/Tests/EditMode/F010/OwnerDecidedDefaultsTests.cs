using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Pins the numbers the owner decided: wasp counts scaled by 0.4 with a wasp twice as long, and the farm phases, prices
    /// and hive economy of plan sections L and M. Other tests read the config, so only this one catches a reverted default.
    /// </summary>
    public sealed class OwnerDecidedDefaultsTests
    {
        [Test]
        public void SwarmConfig_HoldsTheScaledWaspCounts()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            Assert.AreEqual(2000, config.StartingWasps);
            Assert.AreEqual(4000, config.MaxWasps);
            Assert.AreEqual(240, config.CollapseBelow);
            Assert.AreEqual(1000, config.RespawnWasps);
            Assert.AreEqual(config.CollapseBelow + 60, config.MinRespawnWasps, "the respawn floor is the collapse line plus 60");
            Assert.AreEqual(600, config.LowWasps);
            Assert.AreEqual(0.24f, config.WaspLength, 0.0001f);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BeekeeperAndSmokeConfigs_HoldTheScaledWaspLosses()
        {
            var beekeeper = ScriptableObject.CreateInstance<BeekeeperConfig>();
            var smoke = ScriptableObject.CreateInstance<SmokeConfig>();

            Assert.AreEqual(72, beekeeper.WaspsKnockedOff);
            Assert.AreEqual(48f, smoke.WaspLossPerSecond);

            Object.DestroyImmediate(beekeeper);
            Object.DestroyImmediate(smoke);
        }

        [Test]
        public void WaveConfig_HoldsTheFarmPhasesOfPlanSectionL()
        {
            var config = ScriptableObject.CreateInstance<WaveConfig>();

            Assert.AreEqual(45f, config.FirstFarmSeconds);
            Assert.AreEqual(35f, config.FarmSeconds);
            Assert.AreEqual(5f, config.EarlyBonusPerSecond);
            Assert.AreEqual(5, config.Waves.Count);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void HiveConfig_HoldsTheEconomyOfPlanSectionM()
        {
            var config = ScriptableObject.CreateInstance<HiveConfig>();

            Assert.AreEqual(6f, config.ProductionPerSecond);
            Assert.AreEqual(0.5f, config.ProductionHealthFraction);
            Assert.AreEqual(300f, config.StoreCapacity);
            Assert.AreEqual(100f, config.StartStore);
            Assert.AreEqual(3f, config.CollectRadius);
            Assert.AreEqual(150f, config.CollectPerSecond);
            Assert.AreEqual(60f, config.HealWaspsPerSecond);
            Assert.AreEqual(2f, config.WaspsPerHealthPoint);
            Assert.AreEqual(0.8f, config.DamagedStartFraction, 0.0001f);

            Object.DestroyImmediate(config);
        }

        [TestCase(typeof(StingSong), 10)]
        [TestCase(typeof(GustSong), 20)]
        [TestCase(typeof(DomeSong), 40)]
        [TestCase(typeof(TornadoSong), 60)]
        [TestCase(typeof(MeteorSong), 90)]
        [TestCase(typeof(ColossusSong), 0)]
        [TestCase(typeof(HiveCallSong), 0)]
        public void SongAssets_CostThePricesOfPlanSectionM(System.Type songType, int cost)
        {
            var song = (SwarmSongEffect)ScriptableObject.CreateInstance(songType);

            Assert.AreEqual(cost, song.WaspCost);

            Object.DestroyImmediate(song);
        }
    }
}
