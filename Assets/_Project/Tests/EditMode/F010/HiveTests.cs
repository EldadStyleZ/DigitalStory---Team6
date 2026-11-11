using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class HiveTests
    {
        private const float Tolerance = 0.001f;

        private static HiveConfig MakeConfig(float health = 300f, float underAttackSeconds = 2f)
        {
            var config = ScriptableObject.CreateInstance<HiveConfig>();
            JsonUtility.FromJsonOverwrite(
                $"{{\"<Health>k__BackingField\":{health},\"<UnderAttackSeconds>k__BackingField\":{underAttackSeconds}}}",
                config);
            return config;
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.That(() => new Hive(null, Vector3.zero), Throws.ArgumentNullException);
        }

        [Test]
        public void New_StartsStandingAtFullHealth()
        {
            var hive = new Hive(MakeConfig(), new Vector3(3f, 0f, 4f));

            Assert.That(hive.Position, Is.EqualTo(new Vector3(3f, 0f, 4f)));
            Assert.That(hive.Health, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(hive.MaxHealth, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(hive.IsDestroyed, Is.False);
            Assert.That(hive.IsUnderAttack, Is.False);
        }

        [Test]
        public void Damage_RemovesHealthAndRaisesDamaged()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            Hive reported = null;
            hive.Damaged += h => reported = h;

            hive.Damage(100f);

            Assert.That(hive.Health, Is.EqualTo(200f).Within(Tolerance));
            Assert.That(reported, Is.SameAs(hive));
        }

        [Test]
        public void Damage_ExactlyTheHealth_DestroysIt()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);

            hive.Damage(300f);

            Assert.That(hive.IsDestroyed, Is.True);
            Assert.That(hive.Health, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Damage_MoreThanTheHealth_ClampsToZero()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);

            hive.Damage(5000f);

            Assert.That(hive.Health, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(hive.IsDestroyed, Is.True);
        }

        [Test]
        public void Damage_JustBelowTheHealth_LeavesItStanding()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);

            hive.Damage(299f);

            Assert.That(hive.IsDestroyed, Is.False);
            Assert.That(hive.Health, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Damage_FatalHit_RaisesDamagedThenDestroyedOnce()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            var order = new System.Collections.Generic.List<string>();
            hive.Damaged += _ => order.Add("damaged");
            hive.Destroyed += _ => order.Add("destroyed");

            hive.Damage(300f);

            Assert.That(order, Is.EqualTo(new[] { "damaged", "destroyed" }));
        }

        [Test]
        public void Damage_AfterDestroyed_IsIgnoredAndRaisesNothing()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            hive.Damage(300f);
            int damagedCount = 0;
            int destroyedCount = 0;
            hive.Damaged += _ => damagedCount++;
            hive.Destroyed += _ => destroyedCount++;

            hive.Damage(10f);

            Assert.That(damagedCount, Is.EqualTo(0));
            Assert.That(destroyedCount, Is.EqualTo(0));
            Assert.That(hive.Health, Is.EqualTo(0f).Within(Tolerance));
        }

        [TestCase(0f)]
        [TestCase(-20f)]
        public void Damage_ZeroOrNegative_ChangesNothing(float amount)
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            int damagedCount = 0;
            hive.Damaged += _ => damagedCount++;

            hive.Damage(amount);

            Assert.That(hive.Health, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(hive.IsUnderAttack, Is.False);
            Assert.That(damagedCount, Is.EqualTo(0));
        }

        [Test]
        public void IsUnderAttack_AfterDamage_IsTrueUntilTheWindowEnds()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            hive.Damage(10f);

            hive.Tick(1.9f);
            Assert.That(hive.IsUnderAttack, Is.True);

            hive.Tick(0.2f);
            Assert.That(hive.IsUnderAttack, Is.False);
        }

        [Test]
        public void IsUnderAttack_ExactlyAtTheWindow_IsOver()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            hive.Damage(10f);

            hive.Tick(2f);

            Assert.That(hive.IsUnderAttack, Is.False);
        }

        [Test]
        public void IsUnderAttack_HitAgainMidWindow_RestartsTheWindow()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            hive.Damage(10f);
            hive.Tick(1.5f);

            hive.Damage(10f);
            hive.Tick(1.5f);

            Assert.That(hive.IsUnderAttack, Is.True);
        }

        [Test]
        public void Tick_WithoutDamage_NeverShowsUnderAttack()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);

            hive.Tick(1f);

            Assert.That(hive.IsUnderAttack, Is.False);
        }

        [Test]
        public void Health_UsesTheConfiguredValue()
        {
            var hive = new Hive(MakeConfig(health: 80f), Vector3.zero);

            hive.Damage(30f);

            Assert.That(hive.MaxHealth, Is.EqualTo(80f).Within(Tolerance));
            Assert.That(hive.Health, Is.EqualTo(50f).Within(Tolerance));
        }
    }
}
