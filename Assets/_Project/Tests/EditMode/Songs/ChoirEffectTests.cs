using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class ChoirEffectTests
    {
        private static ChoirEffect Song => ScriptableObject.CreateInstance<ChoirEffect>();

        [Test]
        public void Cast_RatInRange_FullyCharmsIt()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(Song.Radius - 1f) });

            world.Cast<ChoirEffect>();

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(1f));
        }

        [Test]
        public void Cast_RatOutOfRange_LeavesItAlone()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(Song.Radius + 1f) });

            world.Cast<ChoirEffect>();

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_SongRunsOut_TheRatsStopSinging()
        {
            var world = new SongWorld(new Rat[0]);
            world.Cast<ChoirEffect>();
            Assert.That(world.Stage.IsSinging, Is.True);

            world.Run(Song.Seconds + 0.1f);

            Assert.That(world.Stage.IsSinging, Is.False);
        }
    }
}
