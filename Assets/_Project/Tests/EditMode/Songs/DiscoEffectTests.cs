using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class DiscoEffectTests
    {
        private const float Tolerance = 0.001f;

        private static DiscoEffect Song => ScriptableObject.CreateInstance<DiscoEffect>();

        [Test]
        public void Cast_RatInRange_SpinsOnTheSpot()
        {
            Rat rat = SongWorld.RatBeside(50f);
            var world = new SongWorld(new[] { rat });

            world.Cast<DiscoEffect>();
            world.Run(2f);

            Assert.That(world.Swarm[0].Trance, Is.InstanceOf<SpinTrance>());
            Assert.That(world.Swarm[0].Velocity.magnitude, Is.EqualTo(Song.SpinSpeed).Within(Tolerance));
            Assert.That(Vector2.Distance(world.Swarm[0].Position, rat.Position), Is.LessThan(5f));
        }

        [Test]
        public void Tick_SongRunsOut_RatStopsDancing()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });
            world.Cast<DiscoEffect>();

            world.Run(Song.Seconds + 0.1f);

            Assert.That(world.Swarm[0].Trance, Is.Null);
        }

        [Test]
        public void Cast_AnyTime_LightsTheDiscoUntilTheSongEnds()
        {
            var world = new SongWorld(new Rat[0]);

            world.Cast<DiscoEffect>();
            Assert.That(world.Stage.IsDiscoing, Is.True);

            world.Run(Song.Seconds + 0.1f);
            Assert.That(world.Stage.IsDiscoing, Is.False);
        }

        [Test]
        public void CatPack_DancingRatWithinReach_CanStillBeEaten()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(5f) }, SongWorld.CatBeside(0f));
            world.Cast<DiscoEffect>();

            world.Cats.Tick(SongWorld.Piper, world.Swarm, 1f / 60f);

            Assert.That(world.Swarm.LostCount, Is.EqualTo(1));
        }
    }
}
