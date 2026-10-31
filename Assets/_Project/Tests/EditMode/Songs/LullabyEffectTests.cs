using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class LullabyEffectTests
    {
        private static LullabyEffect Song => ScriptableObject.CreateInstance<LullabyEffect>();

        [Test]
        public void Cast_CatInRange_PutsItToSleep()
        {
            var world = new SongWorld(new Rat[0], SongWorld.CatBeside(Song.Radius - 1f));

            world.Cast<LullabyEffect>();

            Assert.That(world.Cats[0].SleepLeft, Is.EqualTo(Song.SleepSeconds));
        }

        [Test]
        public void Cast_CatOutOfRange_LeavesItAwake()
        {
            var world = new SongWorld(new Rat[0], SongWorld.CatBeside(Song.Radius + 1f));

            world.Cast<LullabyEffect>();

            Assert.That(world.Cats[0].IsAsleep, Is.False);
        }

        [Test]
        public void Cast_RatsNearby_DoesNotTouchThem()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(10f) });

            world.Cast<LullabyEffect>();

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(0f));
        }
    }
}
