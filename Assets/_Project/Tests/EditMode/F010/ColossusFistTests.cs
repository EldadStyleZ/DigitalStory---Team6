using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>Songs-v2 R: the timing of one hand's launch, from the thrust through the flight and the hit to the regrown arm.</summary>
    public sealed class ColossusFistTests
    {
        private const float Step = 1f / 60f;
        private const float Tolerance = 1e-4f;

        private ColossusConfig _config;
        private ColossusFist _fist;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ColossusConfig>();
            _fist = new ColossusFist(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // Ticks until a step comes, at most the given seconds; returns the step and the seconds it took.
        private FistStep TickUntilStep(float maxSeconds, out float seconds)
        {
            for (seconds = Step; seconds <= maxSeconds + Step; seconds += Step)
            {
                FistStep step = _fist.Tick(Step);
                if (step != FistStep.None)
                {
                    return step;
                }
            }

            return FistStep.None;
        }

        [Test]
        public void New_IsReadyWithNoWait()
        {
            Assert.That(_fist.Phase, Is.EqualTo(FistPhase.Ready));
            Assert.That(_fist.IsReady, Is.True);
            Assert.That(_fist.Cooldown01, Is.EqualTo(0f));
            Assert.That(_fist.SecondsSinceLaunch, Is.EqualTo(-1f));
        }

        [Test]
        public void TryThrust_WhenReady_ThrustsAndLaunchesAfterTheThrustSeconds()
        {
            Assert.That(_fist.TryThrust(), Is.True);
            Assert.That(_fist.Phase, Is.EqualTo(FistPhase.Thrust));

            FistStep step = TickUntilStep(1f, out float seconds);

            Assert.That(step, Is.EqualTo(FistStep.Launch));
            Assert.That(seconds, Is.EqualTo(_config.FistThrustSeconds).Within(Step + Tolerance));
            Assert.That(_fist.Phase, Is.EqualTo(FistPhase.Flight));
        }

        [Test]
        public void Tick_AfterTheLaunch_HitsAfterItsFlight_ThenIsWholeAfterTheRegrow()
        {
            _fist.TryThrust();
            TickUntilStep(1f, out _);
            _fist.Fly(0.5f);

            FistStep impact = TickUntilStep(2f, out float flown);
            FistPhase growing = _fist.Phase;
            FistStep regrown = TickUntilStep(5f, out float grown);

            Assert.That(impact, Is.EqualTo(FistStep.Impact));
            Assert.That(flown, Is.EqualTo(0.5f).Within(Step * 2f));
            Assert.That(growing, Is.EqualTo(FistPhase.Regrow));
            Assert.That(regrown, Is.EqualTo(FistStep.Regrown));
            Assert.That(grown, Is.EqualTo(_config.FistRegrowSeconds).Within(Step * 2f));
            Assert.That(_fist.IsReady, Is.True);
        }

        [Test]
        public void TryThrust_UntilTheArmIsWhole_IsRefused()
        {
            _fist.TryThrust();
            bool whileThrusting = _fist.TryThrust();
            TickUntilStep(1f, out _);
            _fist.Fly(0.3f);
            bool whileFlying = _fist.TryThrust();
            TickUntilStep(1f, out _);
            bool whileRegrowing = _fist.TryThrust();
            _fist.Tick(_config.FistRegrowSeconds - (Step * 2f));
            bool justBeforeWhole = _fist.TryThrust();
            TickUntilStep(1f, out _);

            Assert.That(whileThrusting, Is.False);
            Assert.That(whileFlying, Is.False);
            Assert.That(whileRegrowing, Is.False);
            Assert.That(justBeforeWhole, Is.False);
            Assert.That(_fist.TryThrust(), Is.True, "whole again");
        }

        [Test]
        public void Cooldown01_IsOneUntilTheHit_ThenFallsToZeroAsTheArmGrows()
        {
            _fist.TryThrust();
            float thrusting = _fist.Cooldown01;
            TickUntilStep(1f, out _);
            _fist.Fly(0.3f);
            float flying = _fist.Cooldown01;
            TickUntilStep(1f, out _);
            _fist.Tick(_config.FistRegrowSeconds * 0.5f);

            Assert.That(thrusting, Is.EqualTo(1f));
            Assert.That(flying, Is.EqualTo(1f));
            Assert.That(_fist.Cooldown01, Is.EqualTo(0.5f).Within(0.02f));
        }

        [Test]
        public void Fly_ShorterThanTheLeastFlight_FliesTheLeastFlight()
        {
            _fist.TryThrust();
            TickUntilStep(1f, out _);

            _fist.Fly(0.01f);

            Assert.That(_fist.FlightSeconds, Is.EqualTo(_config.FistLeastFlightSeconds).Within(Tolerance));
        }

        [Test]
        public void Fly_BeforeTheLaunch_DoesNothing()
        {
            _fist.Fly(3f);
            _fist.TryThrust();
            _fist.Fly(3f);

            Assert.That(_fist.FlightSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void SecondsSinceLaunch_CountsFromTheLaunchThroughTheRegrow()
        {
            _fist.TryThrust();
            float thrusting = _fist.SecondsSinceLaunch;
            TickUntilStep(1f, out _);
            _fist.Fly(0.4f);
            _fist.Tick(0.1f);
            float flying = _fist.SecondsSinceLaunch;
            TickUntilStep(1f, out _);
            _fist.Tick(0.5f);

            Assert.That(thrusting, Is.EqualTo(-1f));
            Assert.That(flying, Is.EqualTo(0.1f).Within(Step + Tolerance));
            Assert.That(_fist.SecondsSinceLaunch, Is.EqualTo(0.9f).Within((Step * 2f) + Tolerance));
        }

        [Test]
        public void Reset_WhileTheFistFlies_NeverHits()
        {
            _fist.TryThrust();
            TickUntilStep(1f, out _);
            _fist.Fly(0.5f);
            _fist.Tick(0.2f);

            _fist.Reset();
            FistStep after = TickUntilStep(3f, out _);

            Assert.That(after, Is.EqualTo(FistStep.None));
            Assert.That(_fist.IsReady, Is.True);
            Assert.That(_fist.Cooldown01, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_OneLongTick_TakesOneStepAtATime()
        {
            _fist.TryThrust();

            FistStep first = _fist.Tick(10f);
            FistStep second = _fist.Tick(Step);

            Assert.That(first, Is.EqualTo(FistStep.Launch));
            Assert.That(second, Is.EqualTo(FistStep.Impact), "the time over the thrust ran on into the flight");
        }
    }
}
