using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class BalloonsEffectTests
    {
        private const float Tolerance = 0.5f;

        private static BalloonsEffect Song => ScriptableObject.CreateInstance<BalloonsEffect>();

        // The test random source returns 0.5.
        private static float FlightSeconds => Song.ShortestSeconds + (Song.ExtraSeconds * 0.5f);

        [Test]
        public void Cast_RatInRange_FloatsOnTheWind()
        {
            Rat rat = SongWorld.RatBeside(50f);
            var world = new SongWorld(new[] { rat });

            world.Cast<BalloonsEffect>();
            world.Run(1f);

            Assert.That(world.Swarm[0].Position.x, Is.EqualTo(rat.Position.x + Song.WindSpeed).Within(Tolerance));
        }

        [Test]
        public void Tick_LandingOnTheStreet_StopsAndRejoinsTheFlock()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(50f) });
            world.Cast<BalloonsEffect>();

            world.Run(FlightSeconds + (1f / 30f));

            Assert.That(world.Swarm[0].Trance, Is.Null);
            Assert.That(world.Swarm[0].IsAlive, Is.True);
            Assert.That(world.Swarm.DrownedCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_FloatingOverTheRiver_DoesNotDrownUntilItLands()
        {
            Vector2 overWater = RatJustPastTheBank();
            var world = new SongWorld(new[] { SongWorld.RatAt(overWater) });
            world.Swarm.Enchant(0, new FloatTrance(Song), 1f);

            world.Run(0.5f);
            Assert.That(world.Swarm[0].IsAlive, Is.True);

            world.Run(0.6f);
            Assert.That(world.Swarm[0].IsAlive, Is.False);
            Assert.That(world.Swarm.DrownedCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_DriftingFarAcrossTown_StaysInside()
        {
            float nearFarSide = TestRules.Default.Town.Width - 1f;
            var world = new SongWorld(new[] { SongWorld.RatAt(new Vector2(nearFarSide, 600f)) });
            world.Swarm.Enchant(0, new FloatTrance(Song), 5f);

            world.Run(2f);

            Assert.That(world.Swarm[0].Position.x, Is.LessThanOrEqualTo(TestRules.Default.Town.Width - Song.SideMargin));
        }

        [Test]
        public void CatPack_FloatingRatWithinReach_IsNotEaten()
        {
            var world = new SongWorld(new[] { SongWorld.RatBeside(5f) }, SongWorld.CatBeside(0f));
            world.Cast<BalloonsEffect>();

            world.Cats.Tick(SongWorld.Piper, world.Swarm, 1f / 60f);

            Assert.That(world.Swarm.LostCount, Is.EqualTo(0));
        }

        private static Vector2 RatJustPastTheBank()
        {
            RiverBank bank = TestRules.Bank();
            return new Vector2(bank.EdgeAt(600f) + 20f, 600f);
        }
    }
}
