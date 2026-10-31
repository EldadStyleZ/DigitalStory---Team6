using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class Mulberry32RandomTests
    {
        private const double Tolerance = 1e-12d;

        [Test]
        public void NextDouble_Seed1234_MatchesPrototype()
        {
            AssertSequence(1234u, PrototypeValues.RandomFromSeed1234);
        }

        [Test]
        public void NextDouble_SeedZero_MatchesPrototype()
        {
            AssertSequence(0u, PrototypeValues.RandomFromSeed0);
        }

        private static void AssertSequence(uint seed, double[] expected)
        {
            var random = new Mulberry32Random(seed);

            foreach (double value in expected)
            {
                Assert.That(random.NextDouble(), Is.EqualTo(value).Within(Tolerance));
            }
        }
    }
}
