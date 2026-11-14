using System;
using DigitalStory.Logic;
using NUnit.Framework;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section AD: at most a few grenade flashes light the scene at once, because each pixel light draws every grass
    /// cell, beekeeper and prop in its reach again. A claim succeeds while the budget has room, a release gives room back,
    /// a release never goes below empty, and a bad budget fails loudly.
    /// </summary>
    public sealed class LightBudgetTests
    {
        [Test]
        public void Claims_SucceedUpToTheBudget_ThenFail()
        {
            var budget = new LightBudget(2);

            Assert.That(budget.TryClaim(), Is.True);
            Assert.That(budget.TryClaim(), Is.True);
            Assert.That(budget.TryClaim(), Is.False, "a third light at once");
            Assert.That(budget.InUse, Is.EqualTo(2));
        }

        [Test]
        public void Release_GivesRoomBack()
        {
            var budget = new LightBudget(1);
            budget.TryClaim();

            budget.Release();

            Assert.That(budget.InUse, Is.EqualTo(0));
            Assert.That(budget.TryClaim(), Is.True);
        }

        [Test]
        public void Release_OnAnEmptyBudget_StaysEmpty()
        {
            var budget = new LightBudget(2);

            budget.Release();

            Assert.That(budget.InUse, Is.EqualTo(0));
            Assert.That(budget.TryClaim(), Is.True);
            Assert.That(budget.TryClaim(), Is.True);
            Assert.That(budget.TryClaim(), Is.False, "an extra release must not add room");
        }

        [Test]
        public void Clear_EmptiesTheBudget()
        {
            var budget = new LightBudget(1);
            budget.TryClaim();

            budget.Clear();

            Assert.That(budget.TryClaim(), Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RefusesABudgetBelowOne(int most)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LightBudget(most));
        }
    }
}
