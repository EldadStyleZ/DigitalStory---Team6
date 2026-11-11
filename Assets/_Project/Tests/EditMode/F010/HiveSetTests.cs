using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class HiveSetTests
    {
        private static HiveConfig MakeConfig()
        {
            return ScriptableObject.CreateInstance<HiveConfig>();
        }

        private static HiveSet MakeSet(params Vector3[] positions)
        {
            return MakeSet(out _, positions);
        }

        private static HiveSet MakeSet(out HiveConfig config, params Vector3[] positions)
        {
            config = MakeConfig();
            var hives = new List<Hive>();
            foreach (Vector3 position in positions)
            {
                hives.Add(new Hive(config, position));
            }

            return new HiveSet(hives);
        }

        [Test]
        public void Constructor_NullList_Throws()
        {
            Assert.That(() => new HiveSet(null), Throws.ArgumentNullException);
        }

        [Test]
        public void New_AllHivesStand()
        {
            var set = MakeSet(new Vector3(-12f, 0f, 0f), new Vector3(12f, 0f, 0f));

            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.StandingCount, Is.EqualTo(2));
            Assert.That(set.AllDestroyed, Is.False);
            Assert.That(set[1].Position, Is.EqualTo(new Vector3(12f, 0f, 0f)));
        }

        [Test]
        public void StandingCount_AfterOneFalls_IsOneLess()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));

            set[0].Damage(1000f);

            Assert.That(set.StandingCount, Is.EqualTo(1));
            Assert.That(set.AllDestroyed, Is.False);
        }

        [Test]
        public void AllDestroyed_WhenEveryHiveFalls_IsTrue()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));

            set[0].Damage(1000f);
            set[1].Damage(1000f);

            Assert.That(set.StandingCount, Is.EqualTo(0));
            Assert.That(set.AllDestroyed, Is.True);
        }

        [Test]
        public void HiveDestroyed_RaisesTheIndexOfTheHiveThatFell()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));
            var fallen = new List<int>();
            set.HiveDestroyed += fallen.Add;

            set[1].Damage(1000f);

            Assert.That(fallen, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void HiveDestroyed_DamageAfterTheFall_DoesNotRaiseAgain()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));
            int count = 0;
            set.HiveDestroyed += _ => count++;

            set[0].Damage(1000f);
            set[0].Damage(1000f);

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void TryGetNearestStanding_PicksTheClosestHive()
        {
            var set = MakeSet(new Vector3(-12f, 0f, 0f), new Vector3(12f, 0f, 0f));

            bool found = set.TryGetNearestStanding(new Vector3(5f, 0f, 0f), out int index);

            Assert.That(found, Is.True);
            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void TryGetNearestStanding_SkipsDestroyedHives()
        {
            var set = MakeSet(new Vector3(-12f, 0f, 0f), new Vector3(12f, 0f, 0f));
            set[0].Damage(1000f);

            bool found = set.TryGetNearestStanding(new Vector3(-11f, 0f, 0f), out int index);

            Assert.That(found, Is.True);
            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void TryGetNearestStanding_NoneStanding_ReturnsFalseWithMinusOne()
        {
            var set = MakeSet(Vector3.zero);
            set[0].Damage(1000f);

            bool found = set.TryGetNearestStanding(Vector3.zero, out int index);

            Assert.That(found, Is.False);
            Assert.That(index, Is.EqualTo(-1));
        }

        [Test]
        public void TryGetNearestStanding_IgnoresHeight()
        {
            // Hive 0 is 3 m away on the ground but 100 m up; hive 1 is 4 m away on the ground.
            var set = MakeSet(new Vector3(3f, 100f, 0f), new Vector3(0f, 0f, 4f));

            set.TryGetNearestStanding(Vector3.zero, out int index);

            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void TryGetNearestStanding_EqualDistance_PicksTheLowerIndex()
        {
            var set = MakeSet(new Vector3(-5f, 0f, 0f), new Vector3(5f, 0f, 0f));

            set.TryGetNearestStanding(Vector3.zero, out int index);

            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void Tick_AdvancesEveryHivesUnderAttackTimer()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));
            set[0].Damage(10f);
            set[1].Damage(10f);

            set.Tick(2.5f);

            Assert.That(set[0].IsUnderAttack, Is.False);
            Assert.That(set[1].IsUnderAttack, Is.False);
        }

        [Test]
        public void Count_EmptySet_IsZeroAndAllDestroyed()
        {
            var set = new HiveSet(new List<Hive>());

            Assert.That(set.Count, Is.EqualTo(0));
            Assert.That(set.StandingCount, Is.EqualTo(0));
            Assert.That(set.AllDestroyed, Is.True);
        }

        // ---- The stores (plan section M) ----

        [Test]
        public void Tick_MakesWaspsIntoEveryHealthyHivesStore()
        {
            var set = MakeSet(out HiveConfig config, Vector3.zero, new Vector3(25f, 0f, 0f));

            set.Tick(1f);

            Assert.That(set[0].Store, Is.EqualTo(config.StartStore + config.ProductionPerSecond).Within(0.001f));
            Assert.That(set[1].Store, Is.EqualTo(config.StartStore + config.ProductionPerSecond).Within(0.001f));
        }

        [Test]
        public void HandOutAll_EveryStandingHiveHandsOutAtTheCollectRate()
        {
            var set = MakeSet(out HiveConfig config, Vector3.zero, new Vector3(25f, 0f, 0f));
            float each = 0.1f * config.CollectPerSecond;

            float handed = set.HandOutAll(0.1f, 1000f);

            Assert.That(handed, Is.EqualTo(2f * each).Within(0.001f));
            Assert.That(set[0].Store, Is.EqualTo(config.StartStore - each).Within(0.001f));
            Assert.That(set[1].Store, Is.EqualTo(config.StartStore - each).Within(0.001f));
        }

        [Test]
        public void HandOutAll_SkipsDestroyedHives()
        {
            var set = MakeSet(out HiveConfig config, Vector3.zero, new Vector3(25f, 0f, 0f));
            set[0].Damage(1000f);

            float handed = set.HandOutAll(10f, 1000f);

            Assert.That(handed, Is.EqualTo(config.StartStore).Within(0.001f), "only the standing hive's store");
            Assert.That(set[1].Store, Is.EqualTo(0f));
        }

        [Test]
        public void HandOutAll_StopsAtTheMostAskedFor_TakingTheLowerIndexFirst()
        {
            var set = MakeSet(out HiveConfig config, Vector3.zero, new Vector3(25f, 0f, 0f));

            float handed = set.HandOutAll(10f, config.StartStore + 20f);

            Assert.That(handed, Is.EqualTo(config.StartStore + 20f).Within(0.001f));
            Assert.That(set[0].Store, Is.EqualTo(0f));
            Assert.That(set[1].Store, Is.EqualTo(config.StartStore - 20f).Within(0.001f));
        }

        [Test]
        public void HasStoredWasps_AStandingHiveHoldsAWholeWasp_IsTrue()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));
            set[0].TakeFromStore(set[0].Store);
            set[1].TakeFromStore(set[1].Store - 1f);

            Assert.That(set.HasStoredWasps, Is.True);
        }

        [Test]
        public void HasStoredWasps_LessThanOneWaspInEveryStore_IsFalse()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));
            set[0].TakeFromStore(set[0].Store - 0.5f);
            set[1].TakeFromStore(set[1].Store);

            Assert.That(set.HasStoredWasps, Is.False, "half a wasp is not a wasp");
        }

        [Test]
        public void HasStoredWasps_OnlyADestroyedHiveHadAStore_IsFalse()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));
            set[1].TakeFromStore(set[1].Store);

            set[0].Damage(1000f);

            Assert.That(set.HasStoredWasps, Is.False);
        }

        [Test]
        public void HasDamagedHive_EveryHiveAtFullHealth_IsFalse()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));

            Assert.That(set.HasDamagedHive, Is.False);
        }

        [Test]
        public void HasDamagedHive_AStandingHiveBelowFullHealth_IsTrue()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));

            set[1].Damage(10f);

            Assert.That(set.HasDamagedHive, Is.True);
        }

        [Test]
        public void HasDamagedHive_OnlyADestroyedHiveWasHurt_IsFalse()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));

            set[0].Damage(1000f);

            Assert.That(set.HasDamagedHive, Is.False, "a fallen hive cannot be healed");
        }

        [Test]
        public void TakeFromStores_TakesWholeWaspsUpToTheAmount_TheLowerIndexFirst()
        {
            var set = MakeSet(out HiveConfig config, Vector3.zero, new Vector3(25f, 0f, 0f));

            int taken = set.TakeFromStores((int)config.StartStore + 30);

            Assert.That(taken, Is.EqualTo((int)config.StartStore + 30));
            Assert.That(set[0].Store, Is.EqualTo(0f));
            Assert.That(set[1].Store, Is.EqualTo(config.StartStore - 30f).Within(0.001f));
        }

        [Test]
        public void TakeFromStores_MoreThanTheyHold_TakesTheWholeWaspsAndLeavesTheParts()
        {
            var set = MakeSet(Vector3.zero, new Vector3(25f, 0f, 0f));
            set[0].TakeFromStore(set[0].Store - 10.5f);
            set[1].TakeFromStore(set[1].Store - 4f);

            int taken = set.TakeFromStores(1000);

            Assert.That(taken, Is.EqualTo(14));
            Assert.That(set[0].Store, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(set[1].Store, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void TakeFromStores_SkipsDestroyedHives()
        {
            var set = MakeSet(out HiveConfig config, Vector3.zero, new Vector3(25f, 0f, 0f));
            set[0].Damage(1000f);

            Assert.That(set.TakeFromStores(1000), Is.EqualTo((int)config.StartStore));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void TakeFromStores_NothingAskedFor_TakesNothing(int wasps)
        {
            var set = MakeSet(out HiveConfig config, Vector3.zero, new Vector3(25f, 0f, 0f));

            Assert.That(set.TakeFromStores(wasps), Is.EqualTo(0));
            Assert.That(set[0].Store, Is.EqualTo(config.StartStore).Within(0.001f));
        }
    }
}
