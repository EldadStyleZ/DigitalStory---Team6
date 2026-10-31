using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class FaintEffectTests
    {
        private const float Tolerance = 0.001f;

        private static FaintEffect Song => ScriptableObject.CreateInstance<FaintEffect>();

        // The test random source returns 0.5.
        private static float FaintSeconds => Song.ShortestSeconds + (Song.ExtraSeconds * 0.5f);

        [Test]
        public void Cast_RatInRange_DropsWhereItStands()
        {
            Rat rat = SongWorld.RatBeside(50f);
            rat.Velocity = Vector2.right * 100f;
            var world = new SongWorld(new[] { rat });

            world.Cast<FaintEffect>();
            world.Run(1f);

            Assert.That(world.Swarm[0].Trance, Is.InstanceOf<FaintTrance>());
            Assert.That(world.Swarm[0].Position, Is.EqualTo(rat.Position));
            Assert.That(world.Swarm[0].Velocity.magnitude, Is.LessThan(1f));
        }

        [Test]
        public void Cast_RatInRange_FaintsForTheRightTime()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });

            world.Cast<FaintEffect>();

            Assert.That(world.Swarm[0].TranceLeft, Is.EqualTo(FaintSeconds).Within(Tolerance));
        }

        [Test]
        public void Cast_RatOutOfRange_LeavesItAlone()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(Song.Radius + 1f) });

            world.Cast<FaintEffect>();

            Assert.That(world.Swarm[0].Trance, Is.Null);
        }

        [Test]
        public void Tick_FaintRunsOut_RatWakesUp()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });
            world.Cast<FaintEffect>();

            world.Run(FaintSeconds + 0.1f);

            Assert.That(world.Swarm[0].Trance, Is.Null);
            Assert.That(world.Swarm[0].IsAlive, Is.True);
        }

        [Test]
        public void CatPack_FaintedRatWithinReach_IsNotEaten()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(5f) }, SongWorld.CatBeside(0f));
            world.Cast<FaintEffect>();

            world.Cats.Tick(SongWorld.Piper, world.Swarm, 1f / 60f);

            Assert.That(world.Swarm.LostCount, Is.EqualTo(0));
        }
    }
}
