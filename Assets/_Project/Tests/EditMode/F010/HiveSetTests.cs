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
            var config = MakeConfig();
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
    }
}
