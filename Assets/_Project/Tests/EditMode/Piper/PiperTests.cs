using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class PiperTests
    {
        private const float Tolerance = 0.01f;

        private static PiperConfig Config => TestRules.Default.Piper;

        [Test]
        public void Tick_OnLand_MovesAtWalkSpeed()
        {
            Piper piper = PiperAt(new Vector2(500f, 500f));

            piper.Tick(Vector2.left, 1f);

            Assert.That(piper.Position.x, Is.EqualTo(500f - Config.WalkSpeed).Within(Tolerance));
            Assert.That(piper.IsMoving, Is.True);
        }

        [Test]
        public void Tick_DiagonalInput_DoesNotMoveFaster()
        {
            Piper piper = PiperAt(new Vector2(500f, 500f));

            piper.Tick(new Vector2(1f, 1f), 1f);

            float moved = Vector2.Distance(piper.Position, new Vector2(500f, 500f));
            Assert.That(moved, Is.EqualTo(Config.WalkSpeed).Within(Tolerance));
        }

        [Test]
        public void Tick_InTheRiver_MovesAtWadeSpeed()
        {
            float startX = TestRules.Bank().EdgeAt(500f) + 1f;
            Piper piper = PiperAt(new Vector2(startX, 500f));

            piper.Tick(Vector2.left, 0.1f);

            Assert.That(piper.IsWading, Is.True);
            Assert.That(piper.Position.x, Is.EqualTo(startX - (Config.WadeSpeed * 0.1f)).Within(Tolerance));
        }

        [Test]
        public void Tick_WalkingPastTheShallows_StopsAtTheLimit()
        {
            Piper piper = PiperAt(new Vector2(1900f, 500f));

            piper.Tick(Vector2.right, 10f);

            float limit = TestRules.Bank().EdgeAt(500f) + TestRules.Default.Town.ShallowsWidth - Config.TownMargin;
            Assert.That(piper.Position.x, Is.EqualTo(limit).Within(Tolerance));
        }

        [Test]
        public void Tick_NoInput_StaysStill()
        {
            Piper piper = PiperAt(new Vector2(500f, 500f));

            piper.Tick(Vector2.zero, 1f);

            Assert.That(piper.Position, Is.EqualTo(new Vector2(500f, 500f)));
            Assert.That(piper.IsMoving, Is.False);
        }

        [Test]
        public void Tick_WalkingIntoHouse_StopsAtItsWall()
        {
            var house = new House(600f, 400f, 100f, 200f, 0, false, Vector2.zero);
            var collider = new HouseCollider(new[] { house });
            var piper = new Piper(Config, TestRules.Default.Town, TestRules.Bank(), collider, new Vector2(580f, 500f));

            for (int step = 0; step < 10; step++)
            {
                piper.Tick(Vector2.right, 0.1f);
            }

            Assert.That(piper.Position.x, Is.EqualTo(600f - Config.Radius).Within(Tolerance));
        }

        [Test]
        public void Tick_LongEnough_TurnsToFaceTheWayItWalks()
        {
            Piper piper = PiperAt(new Vector2(500f, 500f));

            for (int step = 0; step < 60; step++)
            {
                piper.Tick(Vector2.right, 1f / 60f);
            }

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(piper.Facing * Mathf.Rad2Deg, 0f)), Is.LessThan(1f));
        }

        private static Piper PiperAt(Vector2 position)
        {
            return new Piper(Config, TestRules.Default.Town, TestRules.Bank(), TestRules.NoHouses(), position);
        }
    }
}
