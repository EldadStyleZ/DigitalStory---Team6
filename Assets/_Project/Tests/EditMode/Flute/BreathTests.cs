using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class BreathTests
    {
        private const float Tolerance = 0.001f;
        private const float ForeverSeconds = 60f;

        private static BreathConfig Config => TestRules.Default.Breath;

        [TestCase(100)]
        [TestCase(120)]
        public void Constructor_RecoveredAtNotBelowMax_FailsWithAClearError(int recoveredAt)
        {
            var config = ScriptableObject.CreateInstance<BreathConfig>();
            JsonUtility.FromJsonOverwrite($"{{\"<RecoveredAt>k__BackingField\":{recoveredAt}}}", config);

            Assert.That(
                () => new Breath(config),
                Throws.ArgumentException.With.Message.Contains(nameof(BreathConfig.RecoveredAt)));
        }

        [Test]
        public void Tick_Playing_DrainsAtConfiguredRate()
        {
            var breath = new Breath(Config);

            bool isPlaying = breath.Tick(true, 1f);

            Assert.That(isPlaying, Is.True);
            Assert.That(breath.Current, Is.EqualTo(Config.Max - Config.DrainPerSecond).Within(Tolerance));
        }

        [Test]
        public void Tick_NotPlaying_RecoversUpToMax()
        {
            var breath = new Breath(Config);
            breath.Tick(true, 2f);

            bool isPlaying = breath.Tick(false, ForeverSeconds);

            Assert.That(isPlaying, Is.False);
            Assert.That(breath.Current, Is.EqualTo(Config.Max).Within(Tolerance));
        }

        [Test]
        public void Tick_RunsOut_StopsPlayingAndIsExhausted()
        {
            var breath = new Breath(Config);

            bool isPlaying = breath.Tick(true, ForeverSeconds);

            Assert.That(isPlaying, Is.False);
            Assert.That(breath.Current, Is.EqualTo(0f));
            Assert.That(breath.IsExhausted, Is.True);
        }

        [Test]
        public void Tick_ExhaustedAndStillHolding_RecoversSlowly()
        {
            var breath = new Breath(Config);
            breath.Tick(true, ForeverSeconds);

            breath.Tick(true, 1f);

            Assert.That(breath.Current, Is.EqualTo(Config.RecoveryWhileHeldPerSecond).Within(Tolerance));
        }

        [Test]
        public void Tick_ExhaustedAtRecoveryLevel_StaysSilent()
        {
            var breath = new Breath(Config);
            breath.Tick(true, ForeverSeconds);
            breath.Tick(false, (Config.RecoveredAt - 1f) / Config.RecoveryPerSecond);

            bool isPlaying = breath.Tick(true, 0.001f);

            Assert.That(isPlaying, Is.False);
            Assert.That(breath.IsExhausted, Is.True);
        }

        [Test]
        public void Tick_RecoveredAboveLevel_CanPlayAgain()
        {
            var breath = new Breath(Config);
            breath.Tick(true, ForeverSeconds);
            breath.Tick(false, (Config.RecoveredAt + 1f) / Config.RecoveryPerSecond);

            bool isPlaying = breath.Tick(true, 0.001f);

            Assert.That(isPlaying, Is.True);
            Assert.That(breath.IsExhausted, Is.False);
        }
    }
}
