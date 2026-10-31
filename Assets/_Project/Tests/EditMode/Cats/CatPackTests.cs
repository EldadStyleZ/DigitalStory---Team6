using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class CatPackTests
    {
        private const float Step = 1f / 60f;
        private const float Tolerance = 0.001f;
        private const float PatrolTolerance = 0.05f;
        private const int TownColumns = 6;
        private const int TownRows = 7;

        private static readonly Vector2 Spot = new Vector2(600f, 600f);
        private static readonly Vector2 PiperFarAway = new Vector2(1600f, 100f);

        private static CatConfig Config => TestRules.Default.Cats;

        [Test]
        public void Tick_AloneInTown_PatrolsLikeThePrototype()
        {
            GameRules rules = TestRules.Default;
            NightRules night = TestRules.PrototypeNight(1);
            var townRandom = new Mulberry32Random((uint)night.Seed);
            TownLayout town = new TownGenerator(rules.Town, townRandom).Generate();
            var houses = new HouseCollider(town.Houses);
            new RatSpawner(rules, houses, townRandom).Spawn(night, town, rules.PiperStart);
            Cat[] cats = new CatSpawner(rules, townRandom).Spawn(night.CatCount, town, rules.PiperStart);
            var random = new Mulberry32Random(PrototypeValues.ScriptedRandomSeed);
            var pack = new CatPack(cats, rules, town, TestRules.Bank(), houses, random);
            RatSwarm noRats = SwarmOf();

            for (int phase = 0; phase < PrototypeValues.CatPatrol.Length / 2; phase++)
            {
                for (int tick = 0; tick < PrototypeValues.CatPatrolTicks; tick++)
                {
                    pack.Tick(rules.PiperStart, noRats, Step);
                }

                Assert.That(pack[0].Position.x, Is.EqualTo((float)PrototypeValues.CatPatrol[phase * 2]).Within(PatrolTolerance));
                Assert.That(pack[0].Position.y, Is.EqualTo((float)PrototypeValues.CatPatrol[(phase * 2) + 1]).Within(PatrolTolerance));
            }
        }

        [Test]
        public void Tick_RatInRange_RunsAtIt()
        {
            CatPack pack = PackOf(CatAt(Spot));
            RatSwarm swarm = SwarmOf(RatAt(Spot + (Vector2.right * 100f)));

            pack.Tick(PiperFarAway, swarm, Step);

            Assert.That(pack[0].IsHunting, Is.True);
            Assert.That(pack[0].Velocity.x, Is.EqualTo(Config.HuntSpeed * Step * Config.SteeringRate).Within(Tolerance));
        }

        [Test]
        public void Tick_RatOutOfRange_LeavesItAlone()
        {
            CatPack pack = PackOf(CatAt(Spot));
            RatSwarm swarm = SwarmOf(RatAt(Spot + (Vector2.right * (Config.HuntRange + 1f))));

            pack.Tick(PiperFarAway, swarm, Step);

            Assert.That(pack[0].IsHunting, Is.False);
        }

        [Test]
        public void Tick_HuntRange_IsMeasuredAlongTheStreets()
        {
            float diagonal = Config.HuntRange * 0.6f;
            CatPack pack = PackOf(CatAt(Spot));
            RatSwarm swarm = SwarmOf(RatAt(Spot + new Vector2(diagonal, diagonal)));

            pack.Tick(PiperFarAway, swarm, Step);

            Assert.That(pack[0].IsHunting, Is.False);
        }

        [Test]
        public void Tick_TwoRatsInRange_HuntsTheNearest()
        {
            CatPack pack = PackOf(CatAt(Spot));
            RatSwarm swarm = SwarmOf(RatAt(Spot + (Vector2.left * 150f)), RatAt(Spot + (Vector2.right * 60f)));

            pack.Tick(PiperFarAway, swarm, Step);

            Assert.That(pack[0].Velocity.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Tick_RatWithinReach_EatsIt()
        {
            CatPack pack = PackOf(CatAt(Spot));
            RatSwarm swarm = SwarmOf(RatAt(Spot + (Vector2.right * (Config.EatDistance - 1f))));

            pack.Tick(PiperFarAway, swarm, Step);

            Assert.That(swarm[0].IsAlive, Is.False);
            Assert.That(swarm.AliveCount, Is.EqualTo(0));
            Assert.That(swarm.LostCount, Is.EqualTo(1));
            Assert.That(swarm.DrownedCount, Is.EqualTo(0));
        }

        [Test]
        public void Tick_JustAte_IgnoresRatsUntilThePauseEnds()
        {
            Vector2 close = Spot + (Vector2.right * (Config.EatDistance - 1f));
            CatPack pack = PackOf(CatAt(Spot));
            RatSwarm swarm = SwarmOf(RatAt(close), RatAt(close));

            pack.Tick(PiperFarAway, swarm, Step);
            pack.Tick(PiperFarAway, swarm, Step);

            Assert.That(swarm.LostCount, Is.EqualTo(1));
            Assert.That(pack[0].EatLeft, Is.EqualTo(Config.EatPauseSeconds - Step).Within(Tolerance));
        }

        [Test]
        public void Tick_DeadRatInRange_IsIgnored()
        {
            Rat dead = RatAt(Spot + Vector2.right);
            dead.IsAlive = false;
            CatPack pack = PackOf(CatAt(Spot));

            pack.Tick(PiperFarAway, SwarmOf(dead), Step);

            Assert.That(pack[0].IsHunting, Is.False);
        }

        [Test]
        public void HearScare_InsideTheReach_RunsFromThePiper()
        {
            float musicRadius = 200f;
            Vector2 piper = Spot + (Vector2.left * (musicRadius + Config.ScareExtraRange - 1f));
            CatPack pack = PackOf(CatAt(Spot));

            pack.HearScare(piper, musicRadius);
            pack.Tick(piper, SwarmOf(), Step);

            Assert.That(pack[0].FleeLeft, Is.EqualTo(Config.ScareSeconds - Step).Within(Tolerance));
            Assert.That(pack[0].Velocity.x, Is.EqualTo(Config.FleeSpeed * Step * Config.SteeringRate).Within(Tolerance));
        }

        [Test]
        public void HearScare_OutsideTheReach_DoesNothing()
        {
            float musicRadius = 200f;
            Vector2 piper = Spot + (Vector2.left * (musicRadius + Config.ScareExtraRange + 1f));
            CatPack pack = PackOf(CatAt(Spot));

            pack.HearScare(piper, musicRadius);

            Assert.That(pack[0].FleeLeft, Is.EqualTo(0f));
        }

        [Test]
        public void HearScare_AlreadyFleeingLonger_KeepsTheLongerTime()
        {
            Cat cat = CatAt(Spot);
            cat.FleeLeft = Config.ScareSeconds + 3f;
            CatPack pack = PackOf(cat);

            pack.HearScare(Spot, 200f);

            Assert.That(pack[0].FleeLeft, Is.EqualTo(Config.ScareSeconds + 3f));
        }

        [Test]
        public void HearScare_CatAsleep_DoesNotWakeIt()
        {
            Cat cat = CatAt(Spot);
            cat.SleepLeft = 5f;
            CatPack pack = PackOf(cat);

            pack.HearScare(Spot, 200f);

            Assert.That(pack[0].FleeLeft, Is.EqualTo(0f));
        }

        [Test]
        public void PutToSleepNear_InRange_SleepsAndStopsFleeing()
        {
            Cat cat = CatAt(Spot);
            cat.FleeLeft = 3f;
            CatPack pack = PackOf(cat);

            pack.PutToSleepNear(Spot + (Vector2.left * 100f), 150f, 9f);

            Assert.That(pack[0].IsAsleep, Is.True);
            Assert.That(pack[0].SleepLeft, Is.EqualTo(9f));
            Assert.That(pack[0].FleeLeft, Is.EqualTo(0f));
        }

        [Test]
        public void PutToSleepNear_OutOfRange_StaysAwake()
        {
            CatPack pack = PackOf(CatAt(Spot));

            pack.PutToSleepNear(Spot + (Vector2.left * 200f), 150f, 9f);

            Assert.That(pack[0].IsAsleep, Is.False);
        }

        [Test]
        public void Tick_Asleep_StaysPutAndWakesWhenTimeRunsOut()
        {
            Cat cat = CatAt(Spot);
            cat.SleepLeft = Step * 1.5f;
            cat.Velocity = Vector2.right * 50f;
            CatPack pack = PackOf(cat);
            RatSwarm swarm = SwarmOf(RatAt(Spot + (Vector2.right * (Config.EatDistance - 1f))));

            pack.Tick(PiperFarAway, swarm, Step);

            Assert.That(pack[0].Position, Is.EqualTo(Spot));
            Assert.That(pack[0].Velocity.x, Is.EqualTo(50f * Config.SleepSlowdown).Within(Tolerance));
            Assert.That(swarm.LostCount, Is.EqualTo(0));

            pack.Tick(PiperFarAway, swarm, Step);
            Assert.That(pack[0].IsAsleep, Is.False);
        }

        [Test]
        public void Tick_Asleep_SlowsEquallyAtAnyFrameRate()
        {
            Cat cat = CatAt(Spot);
            cat.SleepLeft = 5f;
            cat.Velocity = Vector2.right * 50f;
            CatPack oneStep = PackOf(cat);
            CatPack twoSteps = PackOf(cat);

            oneStep.Tick(PiperFarAway, SwarmOf(), 2f * Step);
            twoSteps.Tick(PiperFarAway, SwarmOf(), Step);
            twoSteps.Tick(PiperFarAway, SwarmOf(), Step);

            Assert.That(oneStep[0].Velocity.x, Is.EqualTo(twoSteps[0].Velocity.x).Within(Tolerance));
        }

        [Test]
        public void Tick_AtTheBank_StaysBackFromTheWater()
        {
            RiverBank bank = TestRules.Bank();
            var nearWater = new Vector2(bank.EdgeAt(600f) - 1f, 600f);
            CatPack pack = PackOf(CatAt(nearWater));

            pack.Tick(PiperFarAway, SwarmOf(), Step);

            Assert.That(pack[0].Position.x, Is.LessThanOrEqualTo(bank.EdgeAt(pack[0].Position.y) - Config.RiverSetback + Tolerance));
        }

        [Test]
        public void Tick_LostItsPrey_HeadsForTheNearestJunction()
        {
            Cat cat = CatAt(new Vector2(720f, 420f));
            cat.IsHunting = true;
            CatPack pack = PackOf(cat);

            pack.Tick(PiperFarAway, SwarmOf(), Step);

            Assert.That(pack[0].IsHunting, Is.False);
            Assert.That(pack[0].Column, Is.EqualTo(3));
            Assert.That(pack[0].Row, Is.EqualTo(2));
            Assert.That(pack[0].Target, Is.EqualTo(new Vector2(690f, 380f)));
        }

        private static Cat CatAt(Vector2 position) => new Cat(new Junction(position, 0, 0), 0f);

        private static Rat RatAt(Vector2 position) => new Rat(position, position, 0f, 0f);

        private static CatPack PackOf(params Cat[] cats)
        {
            var town = new TownLayout(TownColumns, TownRows, Array.Empty<House>(), Array.Empty<Junction>(), Array.Empty<Vector2>());
            return new CatPack(cats, TestRules.Default, town, TestRules.Bank(), TestRules.NoHouses(), new FixedRandomSource());
        }

        private static RatSwarm SwarmOf(params Rat[] rats)
        {
            return new RatSwarm(rats, TestRules.Default, TestRules.Bank(), TestRules.NoHouses(), new FixedRandomSource());
        }
    }
}
