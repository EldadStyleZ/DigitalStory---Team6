using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class TownGeneratorTests
    {
        private const double SumTolerance = 0.01d;

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Generate_EachPrototypeSeed_MatchesPrototypeLayout(int nightIndex)
        {
            PrototypeValues.Night expected = PrototypeValues.Nights[nightIndex];

            TownLayout town = Generate(nightIndex);

            Assert.That(town.Columns, Is.EqualTo(expected.Columns));
            Assert.That(town.Rows, Is.EqualTo(expected.Rows));
            Assert.That(town.Houses.Count, Is.EqualTo(expected.HouseCount));
            Assert.That(town.Junctions.Count, Is.EqualTo(expected.JunctionCount));
            Assert.That(town.Lamps.Count, Is.EqualTo(expected.LampCount));
            Assert.That(SumHouses(town), Is.EqualTo(expected.HouseSum).Within(SumTolerance));
            Assert.That(SumLamps(town), Is.EqualTo(expected.LampSum).Within(SumTolerance));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Generate_EachPrototypeSeed_MatchesPrototypeRoofsAndChimneys(int nightIndex)
        {
            PrototypeValues.Night expected = PrototypeValues.Nights[nightIndex];
            int roofSum = 0;
            int chimneyCount = 0;
            double chimneyOffsetSum = 0d;

            foreach (House house in Generate(nightIndex).Houses)
            {
                roofSum += house.RoofStyle;
                chimneyCount += house.HasChimney ? 1 : 0;
                chimneyOffsetSum += house.ChimneyOffset.x + house.ChimneyOffset.y;
            }

            Assert.That(roofSum, Is.EqualTo((int)expected.RoofSum));
            Assert.That(chimneyCount, Is.EqualTo(expected.ChimneyCount));
            Assert.That(chimneyOffsetSum, Is.EqualTo(expected.ChimneyOffsetSum).Within(SumTolerance));
        }

        [Test]
        public void Generate_AnySeed_KeepsHousesClearOfTheRiver()
        {
            TownConfig config = TestRules.Default.Town;

            foreach (House house in Generate(3).Houses)
            {
                Assert.That(house.Right, Is.LessThan(config.RiverX));
            }
        }

        private static TownLayout Generate(int nightIndex)
        {
            var random = new Mulberry32Random((uint)TestRules.PrototypeNight(nightIndex).Seed);
            return new TownGenerator(TestRules.Default.Town, random).Generate();
        }

        private static double SumHouses(TownLayout town)
        {
            double sum = 0d;

            foreach (House house in town.Houses)
            {
                sum += house.X + (house.Y * 3d) + (house.Width * 5d) + (house.Height * 7d);
            }

            return sum;
        }

        private static double SumLamps(TownLayout town)
        {
            double sum = 0d;

            foreach (Vector2 lamp in town.Lamps)
            {
                sum += lamp.x + (lamp.y * 3d);
            }

            return sum;
        }
    }
}
