using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class RiverBankTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void EdgeAt_SampleHeights_MatchesPrototype()
        {
            var bank = TestRules.Bank();

            for (int index = 0; index < PrototypeValues.RiverSampleY.Length; index++)
            {
                float height = (float)PrototypeValues.RiverSampleY[index];
                float expected = (float)PrototypeValues.RiverEdgeAtSample[index];

                Assert.That(bank.EdgeAt(height), Is.EqualTo(expected).Within(Tolerance));
            }
        }
    }
}
