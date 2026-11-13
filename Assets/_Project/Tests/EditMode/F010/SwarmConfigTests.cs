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

        // Plan section Q: a damping of 1 or more brings a wasp to its place in the river without flying past it, so a
        // start, a stop or a turn never makes the river bounce.
        [Test]
        public void MotionDefaults_TheRiverSpringNeverOvershoots()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            Assert.That(config.RiverDamping, Is.GreaterThanOrEqualTo(1f));

            Object.DestroyImmediate(config);
        }

        // The springs cut each frame into equal steps of at most SpringStepSeconds. A step that divides both a 60 and a
        // 120 frames-a-second frame into whole steps makes both rates take the very same steps.
        [TestCase(60f)]
        [TestCase(120f)]
        public void MotionDefaults_TheSpringStepDividesAFrameIntoWholeSteps(float framesPerSecond)
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            float steps = 1f / (framesPerSecond * config.SpringStepSeconds);

            Assert.That(steps, Is.GreaterThanOrEqualTo(1f));
            Assert.That(steps, Is.EqualTo(Mathf.Round(steps)).Within(Tolerance));

            Object.DestroyImmediate(config);
        }

        // The river's tail follows the core's path this many seconds behind its head. SwarmGpuView keeps a little more
        // than two seconds of the core's past for the swarm to read, so the delay must stay inside it.
        [Test]
        public void MotionDefaults_TheTailsDelayFitsTheCoresKeptPast()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            Assert.That(config.FollowDelaySeconds, Is.GreaterThan(0f));
            Assert.That(config.FollowDelaySeconds * (1f + config.WaveJitter), Is.LessThanOrEqualTo(2f));

            Object.DestroyImmediate(config);
        }

        // The owner felt the swarm answer late once, so a wasp eases into a new whole-swarm form quickly, and the head of
        // the river follows the core at once while its tail thins out behind it.
        [Test]
        public void MotionDefaults_KeepTheHeadQuickAndThinTheTail()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();

            Assert.That(config.FormBlendSeconds, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.3f));
            Assert.That(config.FollowDelayPower, Is.GreaterThan(1f));
            Assert.That(config.TailWidthShare, Is.GreaterThan(0f).And.LessThan(1f));

            Object.DestroyImmediate(config);
        }
    }
}
