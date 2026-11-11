using DigitalStory.Data;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SwarmConfigTests
    {
        private const float Tolerance = 0.0001f;

        private static SwarmConfig Config => ScriptableObject.CreateInstance<SwarmConfig>();

        [Test]
        public void CoreRingShareFor_ABigSwarm_IsThePlainShare()
        {
            SwarmConfig config = Config;

            Assert.That(config.CoreRingShareFor(config.MaxWasps), Is.EqualTo(config.CoreRingShare).Within(Tolerance));
        }

        [Test]
        public void CoreRingShareFor_AYoungSwarm_KeepsTheFewestWasps()
        {
            SwarmConfig config = Config;
            int young = config.StartingWasps;

            float share = config.CoreRingShareFor(young);

            Assert.That(share * young, Is.EqualTo(config.CoreRingLeastWasps).Within(0.5f));
            Assert.That(share, Is.GreaterThan(config.CoreRingShare));
        }

        [Test]
        public void CoreRingShareFor_ATinySwarm_NeverTakesMoreThanTheMostShare()
        {
            SwarmConfig config = Config;

            Assert.That(config.CoreRingShareFor(config.CoreRingLeastWasps), Is.EqualTo(config.CoreRingMostShare).Within(Tolerance));
        }

        [Test]
        public void CoreRingShareFor_NoWasps_IsZero()
        {
            Assert.That(Config.CoreRingShareFor(0), Is.Zero);
        }
    }
}
