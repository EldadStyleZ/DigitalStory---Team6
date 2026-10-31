using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class RatSpawnerTests
    {
        private const double PositionSumTolerance = 0.5d;
        private const double AngleSumTolerance = 0.001d;
        private const double RandomTolerance = 1e-12d;
        private const int MostDraws = 100000;
        private const string BeyondTheWholeTown = "{\"<MinimumNestDistance>k__BackingField\":100000}";

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Spawn_EachPrototypeSeed_MatchesPrototype(int nightIndex)
        {
            PrototypeValues.Night expected = PrototypeValues.Nights[nightIndex];
            NightRules night = TestRules.PrototypeNight(nightIndex);
            var random = new Mulberry32Random((uint)night.Seed);
            TownLayout town = new TownGenerator(TestRules.Default.Town, random).Generate();
            var spawner = new RatSpawner(TestRules.Default, new HouseCollider(town.Houses), random);

            Rat[] rats = spawner.Spawn(night, town, TestRules.Default.PiperStart);

            double positionSum = 0d;
            double wanderSum = 0d;
            double phaseSum = 0d;

            foreach (Rat rat in rats)
            {
                positionSum += rat.Position.x + (rat.Position.y * 3d);
                wanderSum += rat.WanderAngle;
                phaseSum += rat.Phase;
            }

            Assert.That(rats.Length, Is.EqualTo(night.RatCount));
            Assert.That(positionSum, Is.EqualTo(expected.RatPositionSum).Within(PositionSumTolerance));
            Assert.That(wanderSum, Is.EqualTo(expected.RatWanderSum).Within(AngleSumTolerance));
            Assert.That(phaseSum, Is.EqualTo(expected.RatPhaseSum).Within(AngleSumTolerance));
            Assert.That(random.NextDouble(), Is.EqualTo(expected.NextRandom).Within(RandomTolerance));
        }

        [Test]
        public void Spawn_AnyNight_StartsRatsAliveAndUncharmed()
        {
            NightRules night = TestRules.PrototypeNight(0);
            var random = new Mulberry32Random((uint)night.Seed);
            TownLayout town = new TownGenerator(TestRules.Default.Town, random).Generate();
            var spawner = new RatSpawner(TestRules.Default, new HouseCollider(town.Houses), random);

            foreach (Rat rat in spawner.Spawn(night, town, TestRules.Default.PiperStart))
            {
                Assert.That(rat.IsAlive, Is.True);
                Assert.That(rat.Charm, Is.EqualTo(0f));
                Assert.That(rat.Velocity, Is.EqualTo(Vector2.zero));
            }
        }

        [Test]
        public void Spawn_NoJunctionFarEnoughFromThePiper_FailsWithAClearError()
        {
            var ratConfig = ScriptableObject.CreateInstance<RatConfig>();
            JsonUtility.FromJsonOverwrite(BeyondTheWholeTown, ratConfig);
            GameRules defaults = TestRules.Default;
            var rules = new GameRules(defaults.Town, defaults.Piper, defaults.Breath, defaults.Flute, ratConfig, defaults.Cats);
            NightRules night = TestRules.PrototypeNight(0);
            TownLayout town = new TownGenerator(rules.Town, new Mulberry32Random((uint)night.Seed)).Generate();
            var spawner = new RatSpawner(rules, TestRules.NoHouses(), new LimitedRandomSource(MostDraws));

            Assert.That(
                () => spawner.Spawn(night, town, rules.PiperStart),
                Throws.InvalidOperationException.With.Message.Contains(nameof(RatConfig.MinimumNestDistance)));
        }

        /// <summary>Fails the test if the code under test draws forever, so a hang cannot freeze the Editor.</summary>
        private sealed class LimitedRandomSource : IRandomSource
        {
            private readonly int _drawsAllowed;
            private int _draws;

            public LimitedRandomSource(int drawsAllowed) => _drawsAllowed = drawsAllowed;

            public double NextDouble()
            {
                _draws++;

                if (_draws > _drawsAllowed)
                {
                    throw new AssertionException($"Drew more than {_drawsAllowed} numbers, so this would never finish.");
                }

                return 0.5d;
            }
        }
    }
}
