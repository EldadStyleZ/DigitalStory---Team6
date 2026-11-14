using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section AB, the optional impact effects: which moment plays which effect, and the pool rule that keeps each kind
    /// under its cap by reusing the oldest instance, never making one in play, and thins out a burst by a least gap.
    /// </summary>
    public sealed class ImpactFxRulesTests
    {
        private const float HitLeastDamage = 1f;

        [Test]
        public void Pool_FirstClaims_TakeEachInstanceOnce()
        {
            var pool = new ImpactFxPool(3, 0f);

            Assert.That(pool.Claim(0f), Is.EqualTo(0));
            Assert.That(pool.Claim(0.1f), Is.EqualTo(1));
            Assert.That(pool.Claim(0.2f), Is.EqualTo(2));
        }

        [Test]
        public void Pool_WhenEveryInstanceIsTaken_ReusesTheOldest()
        {
            var pool = new ImpactFxPool(3, 0f);
            pool.Claim(0f);
            pool.Claim(0.1f);
            pool.Claim(0.2f);

            Assert.That(pool.Claim(0.3f), Is.EqualTo(0), "the fourth effect did not take the oldest instance");
            Assert.That(pool.Claim(0.4f), Is.EqualTo(1), "the fifth effect did not take the next oldest");
        }

        [Test]
        public void Pool_ManyClaimsInOneFrame_NeverPassTheCap()
        {
            var pool = new ImpactFxPool(4, 0f);
            for (int claim = 0; claim < 100; claim++)
            {
                int index = pool.Claim(1f);
                Assert.That(index, Is.InRange(0, pool.Count - 1));
            }

            Assert.That(pool.Count, Is.EqualTo(4));
        }

        [Test]
        public void Pool_WithinTheLeastGap_ClaimsNothing_AndARefusalDoesNotMoveTheGap()
        {
            var pool = new ImpactFxPool(3, 0.1f);

            Assert.That(pool.Claim(0f), Is.EqualTo(0));
            Assert.That(pool.Claim(0.05f), Is.EqualTo(ImpactFxPool.None), "an effect inside the gap played");
            Assert.That(pool.Claim(0.1f), Is.EqualTo(1), "the gap was measured from the refused effect, not the last one played");
        }

        [Test]
        public void Pool_WithABadCapOrGap_FailsLoudly()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpactFxPool(0, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpactFxPool(2, -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpactFxPool(2, float.NaN));
        }

        [Test]
        public void Pool_Claim_AllocatesNothing()
        {
            var pool = new ImpactFxPool(8, 0.01f);
            float now = 0f;
            pool.Claim(now);

            Assert.That(() =>
            {
                for (int claim = 0; claim < 64; claim++)
                {
                    now += 0.02f;
                    pool.Claim(now);
                }
            }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void ForHit_AKill_PlaysTheDeath_NotTheHit()
        {
            var hit = new SongHit(Vector3.zero, 50f, true, false, SwarmForm.Tornado, false);

            Assert.That(ImpactFxChoice.ForHit(in hit, HitLeastDamage, out ImpactFxKind kind), Is.True);
            Assert.That(kind, Is.EqualTo(ImpactFxKind.Death));
        }

        [TestCase(1f, true)]
        [TestCase(30f, true)]
        [TestCase(0.6f, false)]
        [TestCase(0.3f, false)]
        public void ForHit_AHitThatDoesNotKill_PlaysTheHitFromTheLeastDamage(float damage, bool expected)
        {
            var hit = new SongHit(Vector3.zero, damage, false, false, SwarmForm.Sting, false);

            bool plays = ImpactFxChoice.ForHit(in hit, HitLeastDamage, out ImpactFxKind kind);

            Assert.That(plays, Is.EqualTo(expected));
            if (plays)
            {
                Assert.That(kind, Is.EqualTo(ImpactFxKind.Hit));
            }
        }

        [Test]
        public void ForHit_ABlockedBlow_PlaysNothing()
        {
            var hit = new SongHit(Vector3.zero, 40f, false, true, SwarmForm.Dome, false);

            Assert.That(ImpactFxChoice.ForHit(in hit, HitLeastDamage, out _), Is.False);
        }

        [TestCase(SwarmForm.Meteor, true)]
        [TestCase(SwarmForm.Tornado, false)]
        [TestCase(SwarmForm.Lance, false)]
        [TestCase(SwarmForm.Hydra, false)]
        public void ForFormLanded_OnlyTheMeteor_PlaysALanding(SwarmForm form, bool expected)
        {
            bool plays = ImpactFxChoice.ForFormLanded(form, out ImpactFxKind kind);

            Assert.That(plays, Is.EqualTo(expected));
            if (plays)
            {
                Assert.That(kind, Is.EqualTo(ImpactFxKind.MeteorLanding));
            }
        }

        [TestCase(ColossusAttack.Slam, true)]
        [TestCase(ColossusAttack.LeapSlam, true)]
        [TestCase(ColossusAttack.Punch, false)]
        [TestCase(ColossusAttack.HeavyPunch, false)]
        [TestCase(ColossusAttack.Kick, false)]
        public void ForColossusAttack_TheSlamAndTheLeapsLanding_PlayTheGroundSlam(ColossusAttack attack, bool expected)
        {
            bool plays = ImpactFxChoice.ForColossusAttack(attack, out ImpactFxKind kind);

            Assert.That(plays, Is.EqualTo(expected));
            if (plays)
            {
                Assert.That(kind, Is.EqualTo(ImpactFxKind.GiantSlam));
            }
        }
    }
}
