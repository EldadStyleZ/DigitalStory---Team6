using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class FuryMeterTests
    {
        private const float Tolerance = 0.001f;

        private static FuryConfig Config => ScriptableObject.CreateInstance<FuryConfig>();

        private static FuryMeter FullMeter()
        {
            var config = Config;
            var meter = new FuryMeter(config);
            meter.AddDamage(config.Max * config.DamagePerFuryPoint);
            return meter;
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.That(() => new FuryMeter(null), Throws.ArgumentNullException);
        }

        [Test]
        public void New_StartsEmpty()
        {
            var meter = new FuryMeter(Config);

            Assert.That(meter.Value, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(meter.Fraction, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(meter.IsFull, Is.False);
        }

        [Test]
        public void AddDamage_FillsOnePointPerDamagePerFuryPoint()
        {
            var config = Config;
            var meter = new FuryMeter(config);

            meter.AddDamage(config.DamagePerFuryPoint * 10f);

            Assert.That(meter.Value, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(meter.Fraction, Is.EqualTo(10f / config.Max).Within(Tolerance));
        }

        [Test]
        public void AddDamage_FractionOfAPoint_IsKept()
        {
            var config = Config;
            var meter = new FuryMeter(config);

            meter.AddDamage(config.DamagePerFuryPoint * 0.5f);
            meter.AddDamage(config.DamagePerFuryPoint * 0.5f);

            Assert.That(meter.Value, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void AddDamage_JustBelowFull_IsNotFull()
        {
            var config = Config;
            var meter = new FuryMeter(config);

            meter.AddDamage((config.Max - 1f) * config.DamagePerFuryPoint);

            Assert.That(meter.IsFull, Is.False);
            Assert.That(meter.Value, Is.EqualTo(config.Max - 1f).Within(Tolerance));
        }

        [Test]
        public void AddDamage_ExactlyToMax_IsFull()
        {
            var meter = FullMeter();

            Assert.That(meter.IsFull, Is.True);
            Assert.That(meter.Fraction, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void AddDamage_MoreThanMax_CapsAtMax()
        {
            var config = Config;
            var meter = new FuryMeter(config);

            meter.AddDamage(config.Max * config.DamagePerFuryPoint * 5f);

            Assert.That(meter.Value, Is.EqualTo(config.Max).Within(Tolerance));
            Assert.That(meter.Fraction, Is.EqualTo(1f).Within(Tolerance));
        }

        [TestCase(0f)]
        [TestCase(-10f)]
        public void AddDamage_ZeroOrNegative_ChangesNothingAndRaisesNothing(float damage)
        {
            var config = Config;
            var meter = new FuryMeter(config);
            meter.AddDamage(config.DamagePerFuryPoint * 10f);
            int raised = 0;
            meter.Changed += _ => raised++;

            meter.AddDamage(damage);

            Assert.That(meter.Value, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void AddDamage_RaisesChangedWithTheNewFraction()
        {
            var config = Config;
            var meter = new FuryMeter(config);
            float seen = -1f;
            int raised = 0;
            meter.Changed += fraction =>
            {
                seen = fraction;
                raised++;
            };

            meter.AddDamage(config.DamagePerFuryPoint * config.Max * 0.25f);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(seen, Is.EqualTo(0.25f).Within(Tolerance));
        }

        [Test]
        public void AddDamage_AlreadyFull_RaisesNothing()
        {
            var config = Config;
            var meter = FullMeter();
            int raised = 0;
            meter.Changed += _ => raised++;

            meter.AddDamage(config.DamagePerFuryPoint * 10f);

            Assert.That(raised, Is.EqualTo(0));
            Assert.That(meter.Value, Is.EqualTo(config.Max).Within(Tolerance));
        }

        [Test]
        public void AddDamage_PastMax_RaisesChangedOnceWithOne()
        {
            var config = Config;
            var meter = new FuryMeter(config);
            float seen = -1f;
            int raised = 0;
            meter.Changed += fraction =>
            {
                seen = fraction;
                raised++;
            };

            meter.AddDamage(config.Max * config.DamagePerFuryPoint * 3f);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(seen, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void TryUse_NotFull_ReturnsFalseAndKeepsTheValue()
        {
            var config = Config;
            var meter = new FuryMeter(config);
            meter.AddDamage((config.Max - 1f) * config.DamagePerFuryPoint);
            int raised = 0;
            meter.Changed += _ => raised++;

            bool used = meter.TryUse();

            Assert.That(used, Is.False);
            Assert.That(meter.Value, Is.EqualTo(config.Max - 1f).Within(Tolerance));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void TryUse_Empty_ReturnsFalse()
        {
            var meter = new FuryMeter(Config);

            Assert.That(meter.TryUse(), Is.False);
        }

        [Test]
        public void TryUse_Full_EmptiesItAndReturnsTrue()
        {
            var meter = FullMeter();

            bool used = meter.TryUse();

            Assert.That(used, Is.True);
            Assert.That(meter.Value, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(meter.IsFull, Is.False);
        }

        [Test]
        public void TryUse_Full_RaisesChangedWithZero()
        {
            var meter = FullMeter();
            float seen = -1f;
            int raised = 0;
            meter.Changed += fraction =>
            {
                seen = fraction;
                raised++;
            };

            meter.TryUse();

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(seen, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TryUse_TwiceInARow_SecondFails()
        {
            var meter = FullMeter();

            meter.TryUse();

            Assert.That(meter.TryUse(), Is.False);
        }

        [Test]
        public void AddDamage_AfterUse_FillsAgain()
        {
            var config = Config;
            var meter = FullMeter();
            meter.TryUse();

            meter.AddDamage(config.DamagePerFuryPoint * 4f);

            Assert.That(meter.Value, Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void AddDamage_ChangedDamagePerPoint_ScalesTheFill()
        {
            var config = Config;
            JsonUtility.FromJsonOverwrite("{\"<DamagePerFuryPoint>k__BackingField\":10}", config);
            var meter = new FuryMeter(config);

            meter.AddDamage(50f);

            Assert.That(meter.Value, Is.EqualTo(5f).Within(Tolerance));
        }
    }
}
