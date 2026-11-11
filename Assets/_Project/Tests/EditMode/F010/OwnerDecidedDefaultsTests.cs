using DigitalStory.Data;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Pins the numbers the owner decided: wasp counts scaled by 0.4 with a wasp twice as long, and a first wave after 20 seconds. Other tests read the config, so only this one catches a reverted default.</summary>
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
            Assert.AreEqual(100f, config.HiveCallWaspsPerSecondPerHive);
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
        public void WaveConfig_FirstWaveComesAfterTwentySeconds()
        {
            var config = ScriptableObject.CreateInstance<WaveConfig>();

            Assert.AreEqual(20f, config.FirstWaveDelaySeconds);

            Object.DestroyImmediate(config);
        }
    }
}
