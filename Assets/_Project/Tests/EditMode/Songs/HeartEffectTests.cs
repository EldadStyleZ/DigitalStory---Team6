using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class HeartEffectTests
    {
        private const float Tolerance = 0.001f;

        private static HeartEffect Song => ScriptableObject.CreateInstance<HeartEffect>();

        [TestCase(0, 0f, -5f)]
        [TestCase(1, 16f, -4f)]
        [TestCase(2, 0f, 17f)]
        [TestCase(3, -16f, -4f)]
        public void Target_PlacesAroundTheHeartCurve(int slot, float expectedX, float expectedY)
        {
            var trance = new HeartTrance(4, 1f, 6f);
            Rat rat = SongWorld.RatAt(Vector2.zero);
            rat.TranceSlot = slot;

            Vector2 target = trance.Target(in rat, Vector2.zero);

            Assert.That(target.x, Is.EqualTo(expectedX).Within(Tolerance));
            Assert.That(target.y, Is.EqualTo(expectedY).Within(Tolerance));
        }

        [TestCase(10, 5f)]
        [TestCase(88, 13f)]
        [TestCase(200, 14f)]
        public void ScaleFor_RatCount_GrowsBetweenItsLimits(int ratCount, float expected)
        {
            Assert.That(Song.ScaleFor(ratCount), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Cast_RatInRange_WalksToItsPlaceInTheHeart()
        {
            Rat rat = SongWorld.RatBeside(100f);
            var world = new SongWorld(new[] { rat });

            world.Cast<HeartEffect>();
            world.Run(5f);

            Vector2 place = world.Swarm[0].Trance.Target(in world.Swarm[0], SongWorld.Piper);
            Assert.That(Vector2.Distance(world.Swarm[0].Position, place), Is.LessThan(Song.StopDistance * 2f));
        }

        [Test]
        public void Tick_SongRunning_KeepsTheRatFullyCharmed()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(100f) });
            world.Cast<HeartEffect>();

            world.Run(Song.Seconds - 1f);

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(1f));
        }

        [Test]
        public void Tick_SongRunsOut_CharmStartsToFade()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(100f) });
            world.Cast<HeartEffect>();

            world.Run(Song.Seconds + 1f);

            Assert.That(world.Swarm[0].Trance, Is.Null);
            Assert.That(world.Swarm[0].Charm, Is.LessThan(1f));
        }

        [Test]
        public void Cast_OnARatInAnotherFormation_ReplacesIt()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });
            world.Cast<CongaEffect>();

            world.Cast<HeartEffect>();

            Assert.That(world.Swarm[0].Trance, Is.InstanceOf<HeartTrance>());
            Assert.That(world.Swarm[0].TranceLeft, Is.EqualTo(Song.Seconds));
        }

        [Test]
        public void Cast_BalloonsOverAHeart_LeavesNoFormationBehindWhenItLands()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });
            BalloonsEffect balloons = ScriptableObject.CreateInstance<BalloonsEffect>();
            world.Cast<HeartEffect>();

            world.Cast(balloons);
            world.Run(balloons.ShortestSeconds + balloons.ExtraSeconds + 0.1f);

            Assert.That(world.Swarm[0].Trance, Is.Null);
            Assert.That(world.Swarm[0].Charm, Is.LessThan(1f));
        }
    }
}
