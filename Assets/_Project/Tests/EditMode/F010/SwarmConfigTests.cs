using DigitalStory.Data;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The figure-eight the free swarm flies round the standing core, which replaced the inner ring that marked it. The
    /// owner asked for it after playing; WaspSwarm.compute draws it from these numbers.
    /// </summary>
    public sealed class SwarmConfigTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void EightDefaults_AreTheOwnersFigureEight()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            Assert.AreEqual(2.6f, config.EightRadius, Tolerance);
            Assert.AreEqual(1.4f, config.EightHeight, Tolerance);
            Assert.AreEqual(0.5f, config.EightTilt, Tolerance);
            Assert.AreEqual(0.45f, config.EightThickness, Tolerance);
            Assert.AreEqual(5f, config.EightLapSeconds, Tolerance);

            Object.DestroyImmediate(config);
        }

        // The tilted plane lifts the high lobe's tip by EightRadius x sin(EightTilt) and drops the low one's by as much.
        [Test]
        public void EightDefaults_KeepTheLowLobeOffTheGround()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            float lowTip = config.EightHeight - (config.EightRadius * Mathf.Sin(config.EightTilt));

            Assert.That(lowTip, Is.GreaterThan(0f));

            Object.DestroyImmediate(config);
        }

        // A lobe is half as wide as it is long, so a ribbon wider than a quarter of its length would fill the lobe's hole
        // and the eight would read as a blob.
        [Test]
        public void EightDefaults_LeaveAHoleInEachLobe()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            Assert.That(config.EightThickness, Is.LessThan(config.EightRadius * 0.25f));

            Object.DestroyImmediate(config);
        }
    }
}
