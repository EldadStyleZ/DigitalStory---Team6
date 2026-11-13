using System;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class RateLimiterTests
    {
        [Test]
        public void Constructor_NegativeGap_Throws()
        {
            Assert.That(() => new RateLimiter(-0.1f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TryPass_First_Passes()
        {
            var limiter = new RateLimiter(0.15f);

            Assert.That(limiter.TryPass(), Is.True);
        }

        [Test]
        public void TryPass_AgainWithinTheGap_IsHeldBack()
        {
            var limiter = new RateLimiter(0.15f);
            limiter.TryPass();
            limiter.Tick(0.1f);

            Assert.That(limiter.TryPass(), Is.False);
        }

        [Test]
        public void TryPass_AfterTheGap_PassesAgain()
        {
            var limiter = new RateLimiter(0.15f);
            limiter.TryPass();
            limiter.Tick(0.1f);
            limiter.Tick(0.06f);

            Assert.That(limiter.TryPass(), Is.True);
        }

        // A held-back try does not restart the gap, so a steady stream still passes once per gap.
        [Test]
        public void TryPass_HeldBackTries_DoNotRestartTheGap()
        {
            var limiter = new RateLimiter(0.15f);
            limiter.TryPass();
            limiter.Tick(0.1f);
            limiter.TryPass();
            limiter.Tick(0.06f);

            Assert.That(limiter.TryPass(), Is.True);
        }

        [Test]
        public void TryPass_GapOfZero_AlwaysPasses()
        {
            var limiter = new RateLimiter(0f);
            limiter.TryPass();

            Assert.That(limiter.TryPass(), Is.True);
        }

        [Test]
        public void Reset_LetsTheNextTryPass()
        {
            var limiter = new RateLimiter(10f);
            limiter.TryPass();

            limiter.Reset();

            Assert.That(limiter.TryPass(), Is.True);
        }
    }
}
