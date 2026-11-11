using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SpacePartyGame.Tests
{
    public sealed class VerminWanderTests
    {
        private static VerminWander Make(params float[] rolls)
        {
            var queue = new Queue<float>(rolls);
            return new VerminWander(speed: 2f, minSeconds: 1f, maxSeconds: 3f, pauseChance: 0.3f, random: () => queue.Dequeue());
        }

        [Test]
        public void Tick_First_PicksADirectionAtFullSpeed()
        {
            // Rolls: stand-still check 0.9 (walk), angle 0.25 (a quarter turn: up), duration 0 (the shortest).
            VerminWander wander = Make(0.9f, 0.25f, 0f);

            Vector2 velocity = wander.Tick(0.02f);

            Assert.That(velocity.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(velocity.y, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void Tick_LowStandStillRoll_StandsStill()
        {
            VerminWander wander = Make(0.1f, 0.5f, 0f);

            Assert.That(wander.Tick(0.02f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Tick_BeforeTheTimeIsUp_KeepsTheSameDirection()
        {
            // Duration roll 0.5 gives 2 seconds; a second pick would need more rolls and throw.
            VerminWander wander = Make(0.9f, 0f, 0.5f);
            Vector2 first = wander.Tick(0.02f);

            Vector2 later = wander.Tick(1.5f);

            Assert.That(later, Is.EqualTo(first));
        }

        [Test]
        public void Tick_AfterTheTimeIsUp_PicksAgain()
        {
            // First pick: right for 1 second. Second pick: left (half a turn).
            VerminWander wander = Make(0.9f, 0f, 0f, 0.9f, 0.5f, 0f);
            wander.Tick(0.02f);

            Vector2 next = wander.Tick(1.1f);

            Assert.That(next.x, Is.EqualTo(-2f).Within(1e-4f));
        }
    }

    public sealed class VerminRefillTests
    {
        [TestCase(21, 0, 0)]
        [TestCase(20, 0, 15)]
        [TestCase(5, 0, 15)]
        [TestCase(10, 15, 0)]
        [TestCase(6, 14, 15)]
        public void HowManyToAdd_ByFreeAndQueued(int free, int queued, int expected)
        {
            Assert.That(VerminRefill.HowManyToAdd(free, queued, threshold: 20, amount: 15), Is.EqualTo(expected));
        }
    }

    public sealed class VerminFacingTests
    {
        [TestCase(0.2f, 0.5f, -1)]
        [TestCase(0.49f, 0.5f, -1)]
        [TestCase(0.5f, 0.5f, 1)]
        [TestCase(0.9f, 0.5f, 1)]
        [TestCase(0f, 0f, 1)]
        [TestCase(0.99f, 1f, -1)]
        public void AtSpawn_LeftWhenTheRollIsUnderTheChance(float roll, float leftChance, int expected)
        {
            Assert.That(VerminFacing.AtSpawn(roll, leftChance), Is.EqualTo(expected));
        }
    }

    public sealed class FadeInTests
    {
        [TestCase(0f, 0f)]
        [TestCase(0.25f, 0.5f)]
        [TestCase(0.5f, 1f)]
        [TestCase(2f, 1f)]
        [TestCase(-1f, 0f)]
        public void AlphaAt_LerpsFromZeroToOne(float elapsed, float expected)
        {
            Assert.That(FadeIn.AlphaAt(elapsed, 0.5f), Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void AlphaAt_NoDuration_IsFullyShown()
        {
            Assert.That(FadeIn.AlphaAt(0f, 0f), Is.EqualTo(1f));
        }
    }

    public sealed class SpawnPickerTests
    {
        [Test]
        public void Pick_SkipsInvalidPoints()
        {
            var rolls = new Queue<float>(new[] { 0.1f, 0.1f, 0.9f, 0.9f });
            var area = new Rect(0f, 0f, 10f, 10f);

            bool found = SpawnPicker.TryPick(area, p => p.x > 5f, () => rolls.Dequeue(), 5, out Vector2 point);

            Assert.That(found, Is.True);
            Assert.That(point.x, Is.EqualTo(9f).Within(1e-4f));
            Assert.That(point.y, Is.EqualTo(9f).Within(1e-4f));
        }

        [Test]
        public void Pick_NothingValid_GivesUpAfterTheTries()
        {
            int calls = 0;

            bool found = SpawnPicker.TryPick(new Rect(0f, 0f, 1f, 1f), p => false, () => { calls++; return 0.5f; }, 3, out _);

            Assert.That(found, Is.False);
            Assert.That(calls, Is.EqualTo(6));
        }
    }
}
