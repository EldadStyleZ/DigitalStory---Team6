using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class CatSpawnerTests
    {
        private const double StartSumTolerance = 0.5d;
        private const double RandomTolerance = 1e-12d;
        private const string BeyondTheWholeTown = "{\"<MinimumStartDistance>k__BackingField\":100000}";

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Spawn_EachPrototypeSeed_MatchesPrototype(int nightIndex)
        {
            GameRules rules = TestRules.Default;
            NightRules night = TestRules.PrototypeNight(nightIndex);
            var random = new Mulberry32Random((uint)night.Seed);
            TownLayout town = new TownGenerator(rules.Town, random).Generate();
            new RatSpawner(rules, new HouseCollider(town.Houses), random).Spawn(night, town, rules.PiperStart);

            Cat[] cats = new CatSpawner(rules, random).Spawn(night.CatCount, town, rules.PiperStart);

            double startSum = 0d;

            foreach (Cat cat in cats)
            {
                startSum += cat.Position.x + (cat.Position.y * 3d) + cat.Phase;
            }

            Assert.That(cats.Length, Is.EqualTo(night.CatCount));
            Assert.That(startSum, Is.EqualTo(PrototypeValues.CatStartSum[nightIndex]).Within(StartSumTolerance));
            Assert.That(random.NextDouble(), Is.EqualTo(PrototypeValues.RandomAfterCats[nightIndex]).Within(RandomTolerance));
        }

        [Test]
        public void Spawn_NoCats_ReturnsNone()
        {
            GameRules rules = TestRules.Default;
            var random = new Mulberry32Random(1);
            TownLayout town = new TownGenerator(rules.Town, random).Generate();

            Cat[] cats = new CatSpawner(rules, random).Spawn(0, town, rules.PiperStart);

            Assert.That(cats, Is.Empty);
        }

        [Test]
        public void Spawn_NoJunctionFarEnoughFromThePiper_FailsWithAClearError()
        {
            var catConfig = ScriptableObject.CreateInstance<CatConfig>();
            JsonUtility.FromJsonOverwrite(BeyondTheWholeTown, catConfig);
            GameRules rules = TestRules.With(cats: catConfig);
            var random = new Mulberry32Random(1);
            TownLayout town = new TownGenerator(rules.Town, random).Generate();
            var spawner = new CatSpawner(rules, random);

            Assert.That(
                () => spawner.Spawn(1, town, rules.PiperStart),
                Throws.InvalidOperationException.With.Message.Contains(nameof(CatConfig.MinimumStartDistance)));
        }
    }
}
