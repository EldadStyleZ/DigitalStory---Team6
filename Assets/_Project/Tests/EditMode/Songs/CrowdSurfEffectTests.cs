using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class CrowdSurfEffectTests
    {
        private static CrowdSurfEffect Song => ScriptableObject.CreateInstance<CrowdSurfEffect>();

        [Test]
        public void Cast_RatInRange_FullyCharmsIt()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(Song.Radius - 1f) });

            world.Cast<CrowdSurfEffect>();

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(1f));
        }

        [Test]
        public void Tick_SongRunning_KeepsRatsInTheZoneFullyCharmed()
        {
            Rat rat = SongWorld.RatBeside(Song.CharmZoneRadius / 2f);
            rat.Charm = 0.2f;
            var world = new SongWorld(new[] { rat });
            world.Stage.StartSurf(Song.Seconds, Song.CharmZoneRadius);

            world.Run(0.1f);

            Assert.That(world.Swarm[0].Charm, Is.EqualTo(1f));
        }

        [Test]
        public void Tick_SongRunning_RatsOutsideTheZoneStillFade()
        {
            Rat rat = SongWorld.RatBeside(Song.CharmZoneRadius + 50f);
            rat.Charm = 0.5f;
            var world = new SongWorld(new[] { rat });
            world.Stage.StartSurf(Song.Seconds, Song.CharmZoneRadius);

            world.Run(0.5f);

            Assert.That(world.Swarm[0].Charm, Is.LessThan(0.5f));
        }

        [Test]
        public void Tick_SongRunsOut_ThePiperComesDown()
        {
            var world = new SongWorld(new Rat[0]);
            world.Cast<CrowdSurfEffect>();
            Assert.That(world.Stage.IsSurfing, Is.True);

            world.Run(Song.Seconds + 0.1f);

            Assert.That(world.Stage.IsSurfing, Is.False);
            Assert.That(world.Stage.CharmZoneRadius, Is.EqualTo(0f));
        }
    }
}
