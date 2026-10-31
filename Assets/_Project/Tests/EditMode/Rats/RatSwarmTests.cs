using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class RatSwarmTests
    {
        private const float Step = 0.1f;
        private const float Tolerance = 0.001f;
        private const float MusicRadius = 240f;

        private static readonly Vector2 FarAway = new Vector2(100f, 100f);
        private static readonly Vector2 PiperPosition = new Vector2(800f, 600f);

        private static RatConfig Config => TestRules.Default.Rats;

        [Test]
        public void Tick_PlayingNearby_RaisesCharmMoreWhenCloser()
        {
            RatSwarm swarm = SwarmOf(RatAt(PiperPosition + (Vector2.left * 120f)));

            swarm.Tick(Call(), Step);

            Assert.That(swarm[0].Charm, Is.EqualTo(Step * ((2.2f * 0.5f) + 0.6f)).Within(Tolerance));
        }

        [Test]
        public void Tick_FluteSilent_CharmFades()
        {
            Rat rat = RatAt(PiperPosition + (Vector2.left * 120f));
            rat.Charm = 0.5f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(Silence(), 1f);

            Assert.That(swarm[0].Charm, Is.EqualTo(0.5f - Config.CharmFadePerSecond).Within(Tolerance));
        }

        [Test]
        public void Tick_OutOfRangeBesideCharmedRat_CatchesCharm()
        {
            Rat charmed = RatAt(FarAway);
            charmed.Charm = 0.8f;
            RatSwarm swarm = SwarmOf(charmed, RatAt(FarAway + Vector2.right));

            swarm.Tick(Call(), Step);

            Assert.That(swarm[1].Charm, Is.EqualTo(Step * Config.CharmSpreadPerSecond).Within(Tolerance));
        }

        [Test]
        public void Tick_OutOfRangeWithFluteSilent_DoesNotCatchCharm()
        {
            Rat charmed = RatAt(FarAway);
            charmed.Charm = 0.8f;
            RatSwarm swarm = SwarmOf(charmed, RatAt(FarAway + Vector2.right));

            swarm.Tick(Silence(), Step);

            Assert.That(swarm[1].Charm, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_ScareMode_RemovesCharmAndPushesAway()
        {
            Rat rat = RatAt(PiperPosition + (Vector2.left * 100f));
            rat.Charm = 0.5f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(Scare(), Step);

            Assert.That(swarm[0].Charm, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(swarm[0].Velocity.x, Is.LessThan(0f));
        }

        [Test]
        public void Tick_ScareModeOutOfRange_ChangesNothingButFade()
        {
            Rat rat = RatAt(FarAway);
            rat.Charm = 0.5f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(Scare(), Step);

            Assert.That(swarm[0].Charm, Is.EqualTo(0.5f - (Step * Config.CharmFadePerSecond)).Within(Tolerance));
        }

        [Test]
        public void Tick_CharmedRat_HeadsForThePiper()
        {
            Rat rat = RatAt(PiperPosition + (Vector2.left * 200f));
            rat.Charm = 1f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(Call(), Step);

            Assert.That(swarm[0].Velocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Tick_MarchMode_HeadsForThePointAheadOfThePiper()
        {
            Rat rat = RatAt(PiperPosition + (Vector2.right * 50f));
            rat.Charm = 1f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(March(facing: 0f), Step);

            Assert.That(swarm[0].Velocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Tick_UncharmedFarFromHome_DriftsHome()
        {
            var home = new Vector2(100f, 500f);
            var rat = new Rat(new Vector2(500f, 500f), home, Mathf.PI / 2f, 0f);
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(Silence(), Step);

            Assert.That(swarm[0].Velocity.x, Is.LessThan(0f));
        }

        [Test]
        public void Tick_UncharmedNearTheBank_TurnsBack()
        {
            var start = new Vector2(TestRules.Bank().EdgeAt(600f) - 50f, 600f);
            RatSwarm swarm = SwarmOf(new Rat(start, start, 0f, 0f));

            swarm.Tick(Silence(), Step);

            Assert.That(swarm[0].Velocity.x, Is.LessThan(0f));
        }

        [Test]
        public void Tick_Fast_CapsSpeedByCharm()
        {
            Rat rat = RatAt(FarAway);
            rat.Velocity = Vector2.right * 1000f;
            rat.Charm = 0.5f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(Silence(), Step);

            float charm = swarm[0].Charm;
            float cap = Config.BaseSpeed + (Config.CharmSpeedBonus * charm);
            Assert.That(swarm[0].Velocity.magnitude, Is.LessThanOrEqualTo(cap + Tolerance));
        }

        [Test]
        public void Tick_TwoRatsTouching_PushApart()
        {
            RatSwarm swarm = SwarmOf(RatAt(FarAway), RatAt(FarAway + (Vector2.right * 5f)));

            swarm.Tick(Silence(), Step);

            Assert.That(Vector2.Distance(swarm[0].Position, swarm[1].Position), Is.GreaterThan(5f));
        }

        [Test]
        public void Tick_PastTheBank_Drowns()
        {
            float x = TestRules.Bank().EdgeAt(600f) + Config.DrownMargin + 15f;
            RatSwarm swarm = SwarmOf(RatAt(new Vector2(x, 600f)));

            swarm.Tick(Silence(), Step);

            Assert.That(swarm[0].IsAlive, Is.False);
            Assert.That(swarm.DrownedCount, Is.EqualTo(1));
            Assert.That(swarm.AliveCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_DeadRat_NeverMovesAgain()
        {
            Rat rat = RatAt(FarAway);
            rat.IsAlive = false;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(Call(), Step);

            Assert.That(swarm[0].Position, Is.EqualTo(FarAway));
        }

        [Test]
        public void Tick_RunningIntoTownEdge_BouncesBack()
        {
            Rat rat = RatAt(new Vector2(5.5f, 500f));
            rat.Velocity = Vector2.left * 50f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(Silence(), Step);

            Assert.That(swarm[0].Position.x, Is.EqualTo(Config.Radius).Within(Tolerance));
            Assert.That(swarm[0].Velocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Tick_AwakeCatNearby_PushesTheRatAwayAndDrainsCharm()
        {
            CatConfig cats = TestRules.Default.Cats;
            Vector2 ratPosition = FarAway + (Vector2.right * (cats.FearRange / 2f));
            Rat rat = RatAt(ratPosition);
            rat.Charm = 0.5f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(new SwarmInfluence(PiperPosition, 0f, FluteMode.None, 0f, PackWithCatAt(FarAway, 0f)), Step);

            float expectedCharm = 0.5f - ((Config.CharmFadePerSecond + cats.CharmLossPerSecond) * Step);
            Assert.That(swarm[0].Charm, Is.EqualTo(expectedCharm).Within(Tolerance));
            Assert.That(swarm[0].Velocity.x, Is.GreaterThan(cats.FearStrength * 0.25f * Step));
        }

        [Test]
        public void Tick_SleepingCatNearby_IsIgnored()
        {
            CatConfig cats = TestRules.Default.Cats;
            Rat rat = RatAt(FarAway + (Vector2.right * (cats.FearRange / 2f)));
            rat.Charm = 0.5f;
            RatSwarm swarm = SwarmOf(rat);

            swarm.Tick(new SwarmInfluence(PiperPosition, 0f, FluteMode.None, 0f, PackWithCatAt(FarAway, 5f)), Step);

            Assert.That(swarm[0].Charm, Is.EqualTo(0.5f - (Config.CharmFadePerSecond * Step)).Within(Tolerance));
        }

        [Test]
        public void Eat_LivingRat_CountsItAsLostNotDrowned()
        {
            RatSwarm swarm = SwarmOf(RatAt(FarAway), RatAt(FarAway));

            swarm.Eat(0);

            Assert.That(swarm[0].IsAlive, Is.False);
            Assert.That(swarm.AliveCount, Is.EqualTo(1));
            Assert.That(swarm.LostCount, Is.EqualTo(1));
            Assert.That(swarm.DrownedCount, Is.EqualTo(0));
        }

        private static CatPack PackWithCatAt(Vector2 position, float sleepSeconds)
        {
            var cat = new Cat(new Junction(position, 0, 0), 0f) { SleepLeft = sleepSeconds };
            var town = new TownLayout(1, 1, System.Array.Empty<House>(), System.Array.Empty<Junction>(), System.Array.Empty<Vector2>());
            return new CatPack(new[] { cat }, TestRules.Default, town, TestRules.Bank(), TestRules.NoHouses(), new FixedRandomSource());
        }

        private static Rat RatAt(Vector2 position) => new Rat(position, position, 0f, 0f);

        private static RatSwarm SwarmOf(params Rat[] rats)
        {
            return new RatSwarm(rats, TestRules.Default, TestRules.Bank(), TestRules.NoHouses(), new FixedRandomSource());
        }

        private static SwarmInfluence Silence() => new SwarmInfluence(PiperPosition, 0f, FluteMode.None, 0f);

        private static SwarmInfluence Call() => new SwarmInfluence(PiperPosition, 0f, FluteMode.Call, MusicRadius);

        private static SwarmInfluence Scare() => new SwarmInfluence(PiperPosition, 0f, FluteMode.Scare, 200f);

        private static SwarmInfluence March(float facing)
        {
            return new SwarmInfluence(PiperPosition, facing, FluteMode.March, 190f);
        }
    }
}
