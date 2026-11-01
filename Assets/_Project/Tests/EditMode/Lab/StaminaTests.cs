using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class StaminaTests
    {
        private const float Tolerance = 0.001f;

        private StaminaConfig _config;
        private Stamina _stamina;

        [SetUp]
        public void CreateStamina()
        {
            _config = ScriptableObject.CreateInstance<StaminaConfig>();
            _stamina = new Stamina(_config);
        }

        [Test]
        public void Stamina_AtTheStart_IsFull()
        {
            Assert.That(_stamina.Current, Is.EqualTo(_config.Max));
        }

        [Test]
        public void TrySpend_WithEnough_TakesTheCost()
        {
            Assert.That(_stamina.TrySpend(30f), Is.True);
            Assert.That(_stamina.Current, Is.EqualTo(_config.Max - 30f).Within(Tolerance));
        }

        [Test]
        public void TrySpend_WithTooLittle_TakesNothing()
        {
            Assert.That(_stamina.TrySpend(_config.Max + 1f), Is.False);
            Assert.That(_stamina.Current, Is.EqualTo(_config.Max));
        }

        [Test]
        public void Lose_MoreThanIsLeft_StopsAtZero()
        {
            _stamina.Lose(_config.Max + 50f);

            Assert.That(_stamina.Current, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_AfterSpending_Recovers()
        {
            _stamina.TrySpend(50f);

            _stamina.Tick(2f);

            Assert.That(_stamina.Current, Is.EqualTo(_config.Max - 50f + (_config.RecoveryPerSecond * 2f)).Within(Tolerance));
        }

        [Test]
        public void Tick_WhenFull_StaysAtTheMost()
        {
            _stamina.Tick(5f);

            Assert.That(_stamina.Current, Is.EqualTo(_config.Max));
        }

        [Test]
        public void Refill_AfterSpending_IsFullAgain()
        {
            _stamina.Lose(70f);

            _stamina.Refill();

            Assert.That(_stamina.Current, Is.EqualTo(_config.Max));
        }
    }
}
