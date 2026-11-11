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

        // ---- The store (plan section M) ----

        private static HiveConfig ConfigWith(string fields)
        {
            HiveConfig config = MakeConfig();
            JsonUtility.FromJsonOverwrite("{" + fields + "}", config);
            return config;
        }

        [Test]
        public void New_StartsWithStartStoreInAStoreOfTheConfiguredCapacity()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);

            Assert.That(hive.Store, Is.EqualTo(config.StartStore).Within(Tolerance));
            Assert.That(hive.StoreCapacity, Is.EqualTo(config.StoreCapacity).Within(Tolerance));
            Assert.That(hive.IsProducing, Is.True);
            Assert.That(hive.IsDamaged, Is.False);
        }

        [Test]
        public void New_StartsDamaged_HasDamagedStartFractionOfItsHealth()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero, startsDamaged: true);

            Assert.That(hive.Health, Is.EqualTo(300f * config.DamagedStartFraction).Within(Tolerance));
            Assert.That(hive.MaxHealth, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(hive.IsDamaged, Is.True);
            Assert.That(hive.IsUnderAttack, Is.False, "it starts hurt, not hit");
        }

        [Test]
        public void New_StartStoreAboveTheCapacity_Throws()
        {
            HiveConfig config = ConfigWith("\"<StoreCapacity>k__BackingField\":50,\"<StartStore>k__BackingField\":60");

            Assert.That(() => new Hive(config, Vector3.zero), Throws.ArgumentException);
        }

        [Test]
        public void New_NoWaspsPerHealthPoint_Throws()
        {
            HiveConfig config = ConfigWith("\"<WaspsPerHealthPoint>k__BackingField\":0");

            Assert.That(() => new Hive(config, Vector3.zero), Throws.ArgumentException);
        }

        [Test]
        public void Tick_AtFullHealth_MakesProductionPerSecondIntoTheStore()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);

            hive.Tick(2f);

            Assert.That(hive.Store, Is.EqualTo(config.StartStore + (2f * config.ProductionPerSecond)).Within(Tolerance));
        }

        [Test]
        public void Tick_ExactlyAtTheProductionHealthFraction_StillProduces()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);
            hive.Damage(300f * (1f - config.ProductionHealthFraction));

            hive.Tick(1f);

            Assert.That(hive.IsProducing, Is.True);
            Assert.That(hive.Store, Is.EqualTo(config.StartStore + config.ProductionPerSecond).Within(Tolerance));
        }

        [Test]
        public void Tick_BelowTheProductionHealthFraction_MakesNothing()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);
            hive.Damage((300f * (1f - config.ProductionHealthFraction)) + 1f);

            hive.Tick(5f);

            Assert.That(hive.IsProducing, Is.False);
            Assert.That(hive.Store, Is.EqualTo(config.StartStore).Within(Tolerance), "the store keeps what it had");
        }

        [Test]
        public void Tick_HealedBackToTheFraction_ProducesAgain()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);
            hive.Damage(200f);
            Assert.That(hive.IsProducing, Is.False);

            hive.Heal(50f);
            hive.Tick(1f);

            Assert.That(hive.IsProducing, Is.True);
            Assert.That(hive.Store, Is.EqualTo(config.StartStore + config.ProductionPerSecond).Within(Tolerance));
        }

        [Test]
        public void Tick_StoreFull_StaysAtTheCapacity()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);

            hive.Tick(1000f);

            Assert.That(hive.Store, Is.EqualTo(config.StoreCapacity).Within(Tolerance));
            Assert.That(hive.IsProducing, Is.True, "a full hive is still healthy enough to produce");
        }

        [Test]
        public void Destroyed_EmptiesTheStoreAndProducesNoMore()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            float storeWhenDestroyed = -1f;
            hive.Destroyed += h => storeWhenDestroyed = h.Store;

            hive.Damage(300f);
            hive.Tick(10f);

            Assert.That(storeWhenDestroyed, Is.EqualTo(0f), "the store is gone with the hive");
            Assert.That(hive.Store, Is.EqualTo(0f));
            Assert.That(hive.IsProducing, Is.False);
            Assert.That(hive.IsDamaged, Is.False, "a destroyed hive cannot be healed");
        }

        [Test]
        public void HandOut_FlowsCollectPerSecondOutOfTheStore()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);

            float handed = hive.HandOut(0.5f, 1000f);

            Assert.That(handed, Is.EqualTo(0.5f * config.CollectPerSecond).Within(Tolerance));
            Assert.That(hive.Store, Is.EqualTo(config.StartStore - handed).Within(Tolerance));
        }

        [Test]
        public void HandOut_NeverMoreThanTheStoreHolds()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);

            float handed = hive.HandOut(10f, 1000f);

            Assert.That(handed, Is.EqualTo(config.StartStore).Within(Tolerance));
            Assert.That(hive.Store, Is.EqualTo(0f));
        }

        [Test]
        public void HandOut_NeverMoreThanAskedFor()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);

            float handed = hive.HandOut(10f, 12.5f);

            Assert.That(handed, Is.EqualTo(12.5f).Within(Tolerance));
            Assert.That(hive.Store, Is.EqualTo(config.StartStore - 12.5f).Within(Tolerance));
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        public void HandOut_NothingAskedFor_HandsOutNothing(float most)
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);

            Assert.That(hive.HandOut(1f, most), Is.EqualTo(0f));
            Assert.That(hive.Store, Is.EqualTo(config.StartStore).Within(Tolerance));
        }

        [Test]
        public void TakeFromStore_TakesWhatItCanAndSaysHowMuch()
        {
            HiveConfig config = MakeConfig();
            var hive = new Hive(config, Vector3.zero);

            Assert.That(hive.TakeFromStore(30f), Is.EqualTo(30f).Within(Tolerance));
            Assert.That(hive.TakeFromStore(1000f), Is.EqualTo(config.StartStore - 30f).Within(Tolerance));
            Assert.That(hive.Store, Is.EqualTo(0f));
        }

        // ---- Healing (plan section M) ----

        [Test]
        public void Heal_AddsHealthAndSaysHowMuch()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            hive.Damage(100f);

            float healed = hive.Heal(40f);

            Assert.That(healed, Is.EqualTo(40f).Within(Tolerance));
            Assert.That(hive.Health, Is.EqualTo(240f).Within(Tolerance));
        }

        [Test]
        public void Heal_StopsAtFullHealth()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            hive.Damage(10f);

            float healed = hive.Heal(40f);

            Assert.That(healed, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(hive.Health, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(hive.IsDamaged, Is.False);
        }

        [Test]
        public void Heal_ADestroyedHive_DoesNothing()
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            hive.Damage(300f);

            Assert.That(hive.Heal(50f), Is.EqualTo(0f));
            Assert.That(hive.IsDestroyed, Is.True);
        }

        [TestCase(0f)]
        [TestCase(-10f)]
        public void Heal_ZeroOrNegative_ChangesNothing(float health)
        {
            var hive = new Hive(MakeConfig(), Vector3.zero);
            hive.Damage(100f);

            Assert.That(hive.Heal(health), Is.EqualTo(0f));
            Assert.That(hive.Health, Is.EqualTo(200f).Within(Tolerance));
        }
    }
}
