using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class DanceEffectTests
    {
        private static DanceEffect Song => ScriptableObject.CreateInstance<DanceEffect>();

        [Test]
        public void Cast_RatInRange_FullyCharmsIt()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(Song.Radius - 1f) });

            world.Cast<DanceEffect>();

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(1f));
        }

        [Test]
        public void Cast_RatOutOfRange_LeavesItAlone()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(Song.Radius + 1f) });

            world.Cast<DanceEffect>();

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(0f));
        }

        [Test]
        public void Cast_RatPlayingDead_StillGetsCharmed()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(10f) });
            world.Swarm.Enchant(0, new FaintTrance(1f), 5f);

            world.Cast<DanceEffect>();

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(1f));
        }

        [Test]
        public void Cast_DeadRat_StaysUncharmed()
        {
            Rat dead = SongWorld.RatBeside(10f);
            dead.IsAlive = false;
            var world = new SongWorld(new[] { dead });

            world.Cast<DanceEffect>();

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(0f));
        }
    }
}
