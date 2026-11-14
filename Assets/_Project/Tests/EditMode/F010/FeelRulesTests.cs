using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section Y: the trauma behind every camera shake (events add, it is capped at 1, it falls linearly, the shake is its
    /// square), and the kill chain behind the rising kill sound and the x3 / x6 / x10 stamps.
    /// </summary>
    public sealed class FeelRulesTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void Trauma_AddsAndIsCappedAtOne()
        {
            var trauma = new TraumaShake(1.2f, 2f);
            trauma.Add(0.4f);
            trauma.Add(0.4f);
            Assert.That(trauma.Trauma, Is.EqualTo(0.8f).Within(Tolerance));
            trauma.Add(0.7f);
            Assert.That(trauma.Trauma, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Trauma_NegativeOrNaNAmount_ChangesNothing()
        {
            var trauma = new TraumaShake(1.2f, 2f);
            trauma.Add(0.5f);
            trauma.Add(-0.3f);
            trauma.Add(float.NaN);
            Assert.That(trauma.Trauma, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Trauma_FallsLinearlyToRestAndNeverBelow()
        {
            var trauma = new TraumaShake(1.2f, 2f);
            trauma.Add(0.6f);
            trauma.Step(0.25f);
            Assert.That(trauma.Trauma, Is.EqualTo(0.3f).Within(Tolerance));
            trauma.Step(5f);
            Assert.That(trauma.Trauma, Is.EqualTo(0f));
            Assert.That(trauma.Strength, Is.EqualTo(0f));
            Assert.That(trauma.IsResting, Is.True);
        }

        [Test]
        public void Trauma_StrengthIsTheSquare_SoSmallEventsBarelyShake()
        {
            var trauma = new TraumaShake(1.2f, 2f);
            trauma.Add(0.2f);
            Assert.That(trauma.Strength, Is.EqualTo(0.04f).Within(Tolerance));
            trauma.Add(0.8f);
            Assert.That(trauma.Strength, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Trauma_Clear_RestsAtOnce()
        {
            var trauma = new TraumaShake(1.2f, 2f);
            trauma.Add(1f);
            trauma.Clear();
            Assert.That(trauma.IsResting, Is.True);
        }

        [Test]
        public void Trauma_WithABadDecayOrExponent_FailsLoudly()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new TraumaShake(0f, 2f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new TraumaShake(1f, 0f));
        }

        [Test]
        public void KillChain_KillsWithinTheWindow_ClimbOneSemitoneEach()
        {
            var chain = new KillChain(0.5f, 12, new[] { 3, 6, 10 });
            Assert.That(chain.Kill(0f).Semitones, Is.EqualTo(0));
            Assert.That(chain.Kill(0.4f).Semitones, Is.EqualTo(1));
            KillChainStep third = chain.Kill(0.8f);
            Assert.That(third.Count, Is.EqualTo(3));
            Assert.That(third.Semitones, Is.EqualTo(2));
        }

        [Test]
        public void KillChain_AGapLongerThanTheWindow_StartsOver()
        {
            var chain = new KillChain(0.5f, 12, new[] { 3, 6, 10 });
            chain.Kill(0f);
            chain.Kill(0.3f);
            KillChainStep after = chain.Kill(0.81f);
            Assert.That(after.Count, Is.EqualTo(1));
            Assert.That(after.Semitones, Is.EqualTo(0));
        }

        [Test]
        public void KillChain_ReachesEachMilestoneOnce_AndTheLadderStopsAtItsTop()
        {
            var chain = new KillChain(0.5f, 12, new[] { 3, 6, 10 });
            int milestones = 0;
            KillChainStep last = default;
            for (int kill = 0; kill < 20; kill++)
            {
                last = chain.Kill(kill * 0.1f);
                if (last.Milestone > 0)
                {
                    milestones++;
                    Assert.That(last.Milestone, Is.EqualTo(last.Count));
                }
            }

            Assert.That(milestones, Is.EqualTo(3));
            Assert.That(last.Count, Is.EqualTo(20));
            Assert.That(last.Semitones, Is.EqualTo(12));
        }

        [Test]
        public void KillChain_ManyKillsInOneFrame_CountEach()
        {
            var chain = new KillChain(0.5f, 12, new[] { 3, 6, 10 });
            chain.Kill(1f);
            chain.Kill(1f);
            KillChainStep third = chain.Kill(1f);
            Assert.That(third.Milestone, Is.EqualTo(3));
        }

        [Test]
        public void KillChain_IsRunning_UntilTheWindowPassesWithoutAKill()
        {
            var chain = new KillChain(0.5f, 12, new[] { 3 });
            Assert.That(chain.IsRunning(0f), Is.False);
            chain.Kill(1f);
            Assert.That(chain.IsRunning(1.4f), Is.True);
            Assert.That(chain.IsRunning(1.6f), Is.False);
        }

        [Test]
        public void KillChain_WithABadWindowOrMilestones_FailsLoudly()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new KillChain(0f, 12, new[] { 3 }));
            Assert.Throws<System.ArgumentNullException>(() => new KillChain(0.5f, 12, null));
        }

        // Plan section V: one sting's squeeze deals 0.3 to 0.6, so a hit below 1 shows its tenths instead of reading 1.
        [TestCase(0.3f, "0.3")]
        [TestCase(0.6f, "0.6")]
        [TestCase(0.04f, "0.1")]
        [TestCase(0f, "0.1")]
        [TestCase(0.96f, "1")]
        [TestCase(1.4f, "1")]
        [TestCase(12.4f, "12")]
        [TestCase(73f, "73")]
        [TestCase(5000f, "999+")]
        public void HitNumberText_ShowsTenthsBelowOneAndWholeNumbersAbove(float damage, string expected)
        {
            Assert.That(HitNumberText.Of(damage), Is.EqualTo(expected));
        }

        [Test]
        public void HitNumberText_AllocatesNothingOnceMade()
        {
            HitNumberText.Of(0.3f);
            Assert.That(() => { HitNumberText.Of(0.3f); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { HitNumberText.Of(42f); }, Is.Not.AllocatingGCMemory());
        }
    }
}
