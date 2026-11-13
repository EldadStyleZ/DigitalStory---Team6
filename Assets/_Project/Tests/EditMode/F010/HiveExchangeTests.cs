using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plan section M: what passes between the core and the hive it stands at. On the default configs a store hands out
    /// 150 wasps a second within 3 m of its hive, and Give pays 60 wasps a second into the hive's health at 2 wasps a point.
    /// Hive 0 stands at the origin at full health; hive 1 stands 20 m away and starts at 80% health, as the last hive does.
    /// </summary>
    public sealed class HiveExchangeTests
    {
        private static readonly Vector3 AtHive0 = Vector3.zero;
        private static readonly Vector3 AtHive1 = new Vector3(20f, 0f, 0f);
        private static readonly Vector3 FarFromBoth = new Vector3(10f, 0f, 0f);

        private readonly List<Object> _assets = new List<Object>();
        private readonly List<int> _collected = new List<int>();
        private readonly List<int> _gave = new List<int>();

        private SwarmConfig _swarmConfig;
        private HiveConfig _config;
        private HiveSet _hives;
        private SwarmBody _swarm;
        private HiveExchange _exchange;

        [SetUp]
        public void SetUp()
        {
            _swarmConfig = Make<SwarmConfig>();
            _config = Make<HiveConfig>();

            // The swarm starts full at its capacity (plan section H), so these start half full to leave room for a store.
            SetSwarmInt("StartingWasps", _swarmConfig.StartingCapacity / 2);
            Build(AtHive1);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in _assets)
            {
                Object.DestroyImmediate(asset);
            }

            _assets.Clear();
        }

        [Test]
        public void New_MissingAnyPart_Throws()
        {
            Assert.That(() => new HiveExchange(null, _hives, _swarm), Throws.ArgumentNullException);
            Assert.That(() => new HiveExchange(_config, null, _swarm), Throws.ArgumentNullException);
            Assert.That(() => new HiveExchange(_config, _hives, null), Throws.ArgumentNullException);
        }

        [Test]
        public void New_IsNearNothingAndMovesNothing()
        {
            Assert.That(_exchange.NearHive, Is.EqualTo(-1));
            Assert.That(_exchange.IsCollecting, Is.False);
            Assert.That(_exchange.IsGiving, Is.False);
        }

        // ---- Near a hive ----

        [Test]
        public void Tick_CoreFarFromEveryHive_IsNearNothingAndNothingFlows()
        {
            Tick(1f, FarFromBoth);

            Assert.That(_exchange.NearHive, Is.EqualTo(-1));
            Assert.That(_exchange.IsCollecting, Is.False);
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
            Assert.That(_hives[0].Store, Is.EqualTo(_config.StartStore));
        }

        [TestCase(0f, 0f, 2.9f)]
        [TestCase(3f, 0f, 0f)]
        [TestCase(0f, 6f, -2f)]
        public void Tick_CoreWithinCollectRadiusOnTheGround_IsNearThatHive(float x, float y, float z)
        {
            Tick(0.01f, new Vector3(x, y, z));

            Assert.That(_exchange.NearHive, Is.EqualTo(0));
        }

        [Test]
        public void Tick_CoreJustOutsideTheRadius_IsNearNothing()
        {
            Tick(0.01f, new Vector3(3.01f, 0f, 0f));

            Assert.That(_exchange.NearHive, Is.EqualTo(-1));
        }

        [Test]
        public void Tick_TwoHivesInReach_IsNearTheNearer()
        {
            Build(new Vector3(4f, 0f, 0f));

            Tick(0.01f, new Vector3(2.5f, 0f, 0f));

            Assert.That(_exchange.NearHive, Is.EqualTo(1));
        }

        [Test]
        public void Tick_NearADestroyedHive_IsNearNothing()
        {
            _hives[0].Damage(10000f);

            Tick(0.5f, AtHive0);

            Assert.That(_exchange.NearHive, Is.EqualTo(-1));
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
        }

        // ---- Collecting a store ----

        [Test]
        public void Tick_NearAHive_ItsStoreFlowsInAtCollectPerSecond()
        {
            Tick(0.5f, AtHive0);

            float flowed = 0.5f * _config.CollectPerSecond;
            Assert.That(_exchange.IsCollecting, Is.True);
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps + (int)flowed));
            Assert.That(_hives[0].Store, Is.EqualTo(_config.StartStore - flowed).Within(0.001f));
            Assert.That(_collected, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Tick_NearAHive_TakesNoMoreThanItsStore()
        {
            Tick(10f, AtHive0);

            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps + (int)_config.StartStore));
            Assert.That(_hives[0].Store, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_NearAHive_StopsWhenTheSwarmIsFull()
        {
            SetSwarmInt("StartingWasps", _swarmConfig.StartingCapacity - 10);
            Build(AtHive1);

            Tick(0.5f, AtHive0);

            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingCapacity), "full at its capacity");
            Assert.That(_hives[0].Store, Is.EqualTo(_config.StartStore - 10f).Within(0.001f), "the store keeps what the swarm had no room for");

            Tick(0.5f, AtHive0);

            Assert.That(_exchange.IsCollecting, Is.False);
            Assert.That(_hives[0].Store, Is.EqualTo(_config.StartStore - 10f).Within(0.001f));
        }

        [Test]
        public void Tick_NearAnEmptyStore_CollectsNothing()
        {
            _hives[0].TakeFromStore(_hives[0].Store);

            Tick(0.5f, AtHive0);

            Assert.That(_exchange.NearHive, Is.EqualTo(0));
            Assert.That(_exchange.IsCollecting, Is.False);
            Assert.That(_collected, Is.Empty);
        }

        [Test]
        public void Tick_DuringHiveCall_LeavesTheNearHiveToTheCall()
        {
            _exchange.Tick(0.5f, AtHive0, false, true);

            Assert.That(_exchange.NearHive, Is.EqualTo(0));
            Assert.That(_exchange.IsCollecting, Is.False);
            Assert.That(_hives[0].Store, Is.EqualTo(_config.StartStore), "Hive Call already calls this store; it must not flow twice");
        }

        [Test]
        public void Tick_WhileCollapsed_CollectsNothing()
        {
            _swarm.Lose(_swarmConfig.StartingWasps);

            Tick(0.5f, AtHive0);

            Assert.That(_exchange.IsCollecting, Is.False);
            Assert.That(_hives[0].Store, Is.EqualTo(_config.StartStore));
        }

        // ---- Giving to heal ----

        [Test]
        public void Tick_GiveHeldNearADamagedHive_PaysHealWaspsPerSecondIntoItsHealth()
        {
            float health = _hives[1].Health;

            Give(1f, AtHive1);

            int paid = (int)_config.HealWaspsPerSecond;
            Assert.That(_exchange.IsGiving, Is.True);
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps - paid));
            Assert.That(_hives[1].Health, Is.EqualTo(health + (paid / _config.WaspsPerHealthPoint)).Within(0.001f));
            Assert.That(_gave, Is.EqualTo(new[] { 1 }));
        }

        // The clear rule for giving and collecting at once: while Give is held, the hive hands out nothing.
        [Test]
        public void Tick_GiveHeld_TheHiveHandsOutNothing()
        {
            Give(0.5f, AtHive1);

            Assert.That(_hives[1].Store, Is.EqualTo(_config.StartStore));
            Assert.That(_exchange.IsCollecting, Is.False);
            Assert.That(_collected, Is.Empty);
        }

        [Test]
        public void Tick_GiveHeldNearAHiveAtFullHealth_GivesNothingAndCollectsNothing()
        {
            Give(0.5f, AtHive0);

            Assert.That(_exchange.IsGiving, Is.False);
            Assert.That(_exchange.IsCollecting, Is.False);
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
            Assert.That(_hives[0].Store, Is.EqualTo(_config.StartStore));
        }

        [Test]
        public void Tick_GiveHeld_StopsAtFullHealth()
        {
            int needed = Mathf.RoundToInt((_hives[1].MaxHealth - _hives[1].Health) * _config.WaspsPerHealthPoint);

            // Whatever the price of a point: wasps enough above the collapse line to pay for the whole heal, and a second
            // longer than it takes at the give rate.
            _swarm.Gain(needed);
            int before = _swarm.Count;
            Give((needed / _config.HealWaspsPerSecond) + 1f, AtHive1);

            Assert.That(_hives[1].Health, Is.EqualTo(_hives[1].MaxHealth).Within(0.001f));
            Assert.That(_swarm.Count, Is.EqualTo(before - needed), "no wasp is paid past full health");

            Give(0.5f, AtHive1);

            Assert.That(_exchange.IsGiving, Is.False);
            Assert.That(_swarm.Count, Is.EqualTo(before - needed));
        }

        [Test]
        public void Tick_GiveHeld_NeverTakesTheSwarmBelowItsCollapseLine()
        {
            _swarm.Lose(_swarmConfig.StartingWasps - _swarm.CollapseLine - 5);
            float health = _hives[1].Health;

            Give(1f, AtHive1);

            Assert.That(_swarm.Count, Is.EqualTo(_swarm.CollapseLine));
            Assert.That(_swarm.IsCollapsed, Is.False);
            Assert.That(_hives[1].Health, Is.EqualTo(health + (5f / _config.WaspsPerHealthPoint)).Within(0.001f));

            Give(1f, AtHive1);

            Assert.That(_exchange.IsGiving, Is.False, "nothing left to spare");
            Assert.That(_swarm.Count, Is.EqualTo(_swarm.CollapseLine));
        }

        [Test]
        public void Tick_GiveHeldInSmallSteps_AddsUpToTheRate()
        {
            for (int step = 0; step < 120; step++)
            {
                Give(1f / 120f, AtHive1);
            }

            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps - (int)_config.HealWaspsPerSecond).Within(1));
        }

        [Test]
        public void Tick_GiveHeldFarFromEveryHive_GivesNothing()
        {
            Give(1f, FarFromBoth);

            Assert.That(_exchange.IsGiving, Is.False);
            Assert.That(_swarm.Count, Is.EqualTo(_swarmConfig.StartingWasps));
        }

        [Test]
        public void Tick_GiveReleased_TheStoreFlowsAgain()
        {
            Give(0.5f, AtHive1);

            Tick(0.5f, AtHive1);

            Assert.That(_exchange.IsGiving, Is.False);
            Assert.That(_exchange.IsCollecting, Is.True);
            Assert.That(_hives[1].Store, Is.LessThan(_config.StartStore));
        }

        [Test]
        public void Tick_CollectAndGive_AllocateNothing()
        {
            Tick(0.016f, AtHive0);
            Give(0.016f, AtHive1);

            Assert.That(() => { _exchange.Tick(0.016f, AtHive0, false, false); }, Is.Not.AllocatingGCMemory());
            Assert.That(() => { _exchange.Tick(0.016f, AtHive1, true, false); }, Is.Not.AllocatingGCMemory());
        }

        // Builds hive 0 at the origin at full health and hive 1 at the given place at DamagedStartFraction, as the session does with the last hive.
        private void Build(Vector3 secondHive)
        {
            _hives = new HiveSet(new[] { new Hive(_config, AtHive0), new Hive(_config, secondHive, startsDamaged: true) });
            _swarm = new SwarmBody(_swarmConfig);
            _exchange = new HiveExchange(_config, _hives, _swarm);
            _collected.Clear();
            _gave.Clear();
            _exchange.Collected += _collected.Add;
            _exchange.Gave += _gave.Add;
        }

        private void Tick(float seconds, Vector3 core)
        {
            _exchange.Tick(seconds, core, false, false);
        }

        private void Give(float seconds, Vector3 core)
        {
            _exchange.Tick(seconds, core, true, false);
        }

        private void SetSwarmInt(string name, int value)
        {
            JsonUtility.FromJsonOverwrite("{\"<" + name + ">k__BackingField\":" + value + "}", _swarmConfig);
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
