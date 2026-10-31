using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class CrowdSongTests
    {
        [Test]
        public void GatherNear_SeveralRats_ReturnsTheNearestFirst()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(100f), SongWorld.RatBeside(20f), SongWorld.RatBeside(50f) });

            int[] rats = world.Swarm.GatherNear(SongWorld.Piper, 200f);

            Assert.That(rats, Is.EqualTo(new[] { 1, 2, 0 }));
        }

        [Test]
        public void GatherNear_OutOfRangeOrDead_LeavesThemOut()
        {
            Rat dead = SongWorld.RatBeside(10f);
            dead.IsAlive = false;
            var world = new SongWorld(new[] { dead, SongWorld.RatBeside(300f), SongWorld.RatBeside(50f) });

            Assert.That(world.Swarm.GatherNear(SongWorld.Piper, 200f), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void GatherNear_RatOutOfReach_LeavesItOut()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });
            world.Swarm.Enchant(0, new FaintTrance(1f), 5f);

            Assert.That(world.Swarm.GatherNear(SongWorld.Piper, 200f), Is.Empty);
        }
    }
}
