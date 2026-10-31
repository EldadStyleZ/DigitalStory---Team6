using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    public sealed class NightOutcomeRuleTests
    {
        private const int Quota = 45;

        [TestCase(10, 60, 1f, NightOutcome.InProgress)]
        [TestCase(45, 25, 1f, NightOutcome.Won)]
        [TestCase(44, 0, 1f, NightOutcome.QuotaOutOfReach)]
        [TestCase(10, 60, 0f, NightOutcome.DawnBroke)]
        [TestCase(45, 25, 0f, NightOutcome.Won)]
        [TestCase(44, 0, 0f, NightOutcome.QuotaOutOfReach)]
        public void Resolve_EachSituation_GivesTheRightOutcome(int drowned, int alive, float timeLeft, NightOutcome expected)
        {
            NightOutcome outcome = NightOutcomeRule.Resolve(drowned, alive, Quota, timeLeft);

            Assert.That(outcome, Is.EqualTo(expected));
        }
    }
}
