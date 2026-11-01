using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class ComboCooldownsTests
    {
        private CreatureType _small;
        private CreatureType _big;
        private ComboCooldowns _cooldowns;

        [SetUp]
        public void CreateCooldowns()
        {
            _small = Creature(Sturdy, "Small", 1, 5f, 1f, H, J);
            _big = Creature(Sturdy, "Big", 2, 5f, 8f, K, L, H);
            _cooldowns = new ComboCooldowns(new[] { _small, _big });
        }

        [Test]
        public void IsReady_AtTheStart_IsTrue()
        {
            Assert.That(_cooldowns.IsReady(_small), Is.True);
        }

        [Test]
        public void Start_ACombo_MakesItWait()
        {
            _cooldowns.Start(_small);

            Assert.That(_cooldowns.IsReady(_small), Is.False);
            Assert.That(_cooldowns.SecondsLeft(_small), Is.EqualTo(_small.CooldownSeconds));
        }

        [Test]
        public void Start_OneCombo_LeavesTheOthersReady()
        {
            _cooldowns.Start(_big);

            Assert.That(_cooldowns.IsReady(_small), Is.True);
        }

        [Test]
        public void Tick_PastTheCooldown_MakesItReady()
        {
            _cooldowns.Start(_small);

            _cooldowns.Tick(_small.CooldownSeconds + 0.01f);

            Assert.That(_cooldowns.IsReady(_small), Is.True);
        }

        [Test]
        public void Tick_TheSameTime_LeavesABigComboWaiting()
        {
            _cooldowns.Start(_small);
            _cooldowns.Start(_big);

            _cooldowns.Tick(_small.CooldownSeconds + 0.01f);

            Assert.That(_cooldowns.IsReady(_small), Is.True);
            Assert.That(_cooldowns.IsReady(_big), Is.False);
        }

        [Test]
        public void Reset_WhileWaiting_MakesEverythingReady()
        {
            _cooldowns.Start(_big);

            _cooldowns.Reset();

            Assert.That(_cooldowns.IsReady(_big), Is.True);
        }
    }
}
