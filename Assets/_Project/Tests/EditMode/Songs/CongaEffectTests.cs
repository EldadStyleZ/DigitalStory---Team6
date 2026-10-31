using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class CongaEffectTests
    {
        private static CongaEffect Song => ScriptableObject.CreateInstance<CongaEffect>();

        [Test]
        public void Cast_FrontOfTheLine_FollowsThePiper()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f), SongWorld.RatBeside(20f) });

            world.Cast<CongaEffect>();

            Assert.That(TargetOf(world, 1), Is.EqualTo(SongWorld.Piper));
        }

        [Test]
        public void Cast_BehindAnotherRat_FollowsTheRatAhead()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f), SongWorld.RatBeside(20f) });

            world.Cast<CongaEffect>();

            Assert.That(TargetOf(world, 0), Is.EqualTo(world.Swarm[1].Position));
        }

        [Test]
        public void Target_RatAheadWasEaten_FollowsTheNextOneUp()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(30f), SongWorld.RatBeside(20f), SongWorld.RatBeside(10f) });
            world.Cast<CongaEffect>();

            world.Swarm.Eat(1);

            Assert.That(TargetOf(world, 0), Is.EqualTo(world.Swarm[2].Position));
        }

        [Test]
        public void Target_NoDancerWithinLookAhead_FollowsThePiper()
        {
            var rats = new Rat[Song.LookAhead + 2];

            for (int index = 0; index < rats.Length; index++)
            {
                rats[index] = SongWorld.RatBeside(10f + index);
            }

            var world = new SongWorld(rats);
            world.Cast<CongaEffect>();

            for (int index = 1; index < rats.Length - 1; index++)
            {
                world.Swarm.Eat(index);
            }

            Assert.That(TargetOf(world, rats.Length - 1), Is.EqualTo(SongWorld.Piper));
        }

        [Test]
        public void Tick_SongRunning_KeepsRatsFullyCharmed()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });
            world.Cast<CongaEffect>();

            world.Run(Song.Seconds - 1f);

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(1f));
        }

        [Test]
        public void Tick_SongRunsOut_LineBreaksUp()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });
            world.Cast<CongaEffect>();

            world.Run(Song.Seconds + 0.1f);

            Assert.That(world.Swarm[0].Trance, Is.Null);
        }

        private static Vector2 TargetOf(SongWorld world, int rat)
        {
            Assert.That(world.Swarm[rat].Trance, Is.InstanceOf<CongaTrance>());
            return world.Swarm[rat].Trance.Target(in world.Swarm[rat], SongWorld.Piper);
        }
    }
}
