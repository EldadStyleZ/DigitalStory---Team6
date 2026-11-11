using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class AutoAimTests
    {
        private const float Tolerance = 0.001f;
        private const float StingRange = 12f;

        private sealed class FakeTarget : ISwarmTarget
        {
            public FakeTarget(float x, float z)
            {
                Position = new Vector3(x, 0f, z);
                IsAlive = true;
            }

            public Vector3 Position { get; set; }

            public bool IsAlive { get; set; }

            public void TakeDamage(float amount)
            {
            }

            public void Knockback(Vector3 velocity)
            {
            }

            public void Stun(float seconds)
            {
            }
        }

        private AimConfig _config;
        private HiveConfig _hiveConfig;
        private List<ISwarmTarget> _targets;
        private List<Hive> _hiveList;
        private HiveSet _hives;
        private AutoAim _aim;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<AimConfig>();
            _hiveConfig = ScriptableObject.CreateInstance<HiveConfig>();
            _targets = new List<ISwarmTarget>();
            _hiveList = new List<Hive>();
        }

        private FakeTarget Add(float x, float z)
        {
            var target = new FakeTarget(x, z);
            _targets.Add(target);
            return target;
        }

        private Hive AddHive(float x, float z)
        {
            var hive = new Hive(_hiveConfig, new Vector3(x, 0f, z));
            _hiveList.Add(hive);
            return hive;
        }

        [Test]
        public void ThreeNearest_WithTheDefaultConfig_HoldsUpToFiveForTheHydrasMostHeads()
        {
            for (int index = 1; index <= 6; index++)
            {
                Add(0f, index);
            }

            Build();

            Select(AimRule.ThreeNearest, 9f);

            Assert.That(_aim.NearestCount, Is.EqualTo(5));
            Assert.That(_aim.GetNearest(4), Is.EqualTo(4));
        }

        private void HoldNearest(int count)
        {
            JsonUtility.FromJsonOverwrite("{\"<NearestCount>k__BackingField\":" + count + "}", _config);
        }

        // Builds the aim after the test has placed its targets and hives.
        private AutoAim Build()
        {
            _hives = new HiveSet(_hiveList);
            _aim = new AutoAim(_config, _targets, _hives);
            return _aim;
        }

        private bool Select(AimRule rule, float range = StingRange)
        {
            return _aim.Select(rule, range, Vector3.zero, Vector3.forward, false, Vector3.zero);
        }

        private bool SelectMouse(AimRule rule, Vector3 mouse)
        {
            return _aim.Select(rule, StingRange, Vector3.zero, Vector3.forward, true, mouse);
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            _hives = new HiveSet(_hiveList);

            Assert.That(() => new AutoAim(null, _targets, _hives), Throws.ArgumentNullException);
            Assert.That(() => new AutoAim(_config, null, _hives), Throws.ArgumentNullException);
            Assert.That(() => new AutoAim(_config, _targets, null), Throws.ArgumentNullException);
        }

        [Test]
        public void Threat_PicksTheNearestBeekeeper()
        {
            Add(0f, 5f);
            Add(0f, 3f);
            Add(0f, 8f);
            Build();

            Assert.That(Select(AimRule.Threat), Is.True);

            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
            Assert.That(_aim.Target, Is.SameAs(_targets[1]));
            Assert.That(_aim.AimPoint, Is.EqualTo(new Vector3(0f, 0f, 3f)));
        }

        [Test]
        public void Threat_Tie_GoesToTheLowerIndex()
        {
            Add(3f, 0f);
            Add(0f, 3f);
            Build();

            Select(AimRule.Threat);

            Assert.That(_aim.TargetIndex, Is.EqualTo(0));
        }

        [Test]
        public void Threat_BeekeeperNearAStandingHive_BeatsANearerOne()
        {
            Add(0f, 4f);
            Add(0f, 8.5f);
            AddHive(0f, 10f);
            Build();

            Select(AimRule.Threat);

            // Scores: 4 against 8.5 - 6 = 2.5.
            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
        }

        [Test]
        public void Threat_DestroyedHive_GivesNoBonus()
        {
            Add(0f, 4f);
            Add(0f, 8.5f);
            Hive hive = AddHive(0f, 10f);
            hive.Damage(hive.MaxHealth);
            Build();

            Select(AimRule.Threat);

            Assert.That(_aim.TargetIndex, Is.EqualTo(0));
        }

        [Test]
        public void Threat_IgnoresDeadAndOutOfRange()
        {
            FakeTarget dead = Add(0f, 2f);
            dead.IsAlive = false;
            Add(0f, 13f);
            Add(0f, 11f);
            Build();

            Select(AimRule.Threat, 12f);

            Assert.That(_aim.TargetIndex, Is.EqualTo(2));
        }

        [Test]
        public void Threat_RangeComesFromTheArgument()
        {
            Add(0f, 8f);
            Build();

            Assert.That(Select(AimRule.Threat, 6f), Is.False);
            Assert.That(Select(AimRule.Threat, 9f), Is.True);
        }

        [Test]
        public void Hold_KeepsTheTargetForOneSecond_ThenMovesOn()
        {
            Add(0f, 5f);
            FakeTarget second = Add(0f, 8f);
            Build();
            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(0));

            ((FakeTarget)_targets[0]).Position = new Vector3(0f, 0f, 9f);
            second.Position = new Vector3(0f, 0f, 2f);

            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(0), "held at once");

            _aim.Tick(0.99f);
            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(0), "held just under a second");

            _aim.Tick(0.02f);
            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(1), "free after a second");
        }

        [Test]
        public void Hold_EndsWhenTheTargetDies()
        {
            FakeTarget first = Add(0f, 5f);
            Add(0f, 8f);
            Build();
            Select(AimRule.Threat);

            first.IsAlive = false;
            Select(AimRule.Threat);

            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
        }

        [Test]
        public void Hold_EndsWhenTheTargetLeavesRange()
        {
            FakeTarget first = Add(0f, 5f);
            Add(0f, 8f);
            Build();
            Select(AimRule.Threat);

            first.Position = new Vector3(0f, 0f, 13f);
            Select(AimRule.Threat);

            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
        }

        // A lone beekeeper 5 m away, and a crowd of three about 12 m away (index 1 is the crowd's nearest, at 11.7 m).
        private void AddLonerAndCrowd()
        {
            Add(0f, 5f);
            Add(10f, 6f);
            Add(11f, 6f);
            Add(10.5f, 7f);
        }

        // Plan section E: the marker selects by Threat every frame, and a cast by its own rule. One rule's fresh pick must
        // not stand in for another's.
        [Test]
        public void Hold_AThreatPickJustMade_DoesNotDecideTheCrowdPick()
        {
            AddLonerAndCrowd();
            Build();
            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(0), "the loner is the nearest");

            Select(AimRule.Crowd, 18f);

            Assert.That(_aim.TargetIndex, Is.EqualTo(1), "the crowd, whatever the Threat rule picked a moment ago");
        }

        [Test]
        public void Hold_ACrowdPickJustMade_DoesNotDecideTheThreatPick()
        {
            AddLonerAndCrowd();
            Build();
            Select(AimRule.Threat);
            Select(AimRule.Crowd, 18f);
            Assert.That(_aim.TargetIndex, Is.EqualTo(1));

            Select(AimRule.Threat);

            Assert.That(_aim.TargetIndex, Is.EqualTo(0), "the crowd stands within 12 m, but the Threat rule picked the loner");
        }

        [Test]
        public void Hold_EachRuleKeepsItsOwnPickForOneSecond_WhileAnotherRulePicks()
        {
            AddLonerAndCrowd();
            Build();
            Select(AimRule.Threat);
            FakeTarget newcomer = Add(0f, 2f);

            Select(AimRule.Crowd, 18f);
            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(0), "the Threat pick is still held, though a nearer beekeeper came");

            _aim.Tick(1.01f);
            Select(AimRule.Threat);
            Assert.That(_aim.Target, Is.SameAs(newcomer), "free after a second");
        }

        // The config holds as many as the Hydra's most heads (five); this test holds three to show the cut-off.
        [Test]
        public void ThreeNearest_FillsTheNearestThreeToTheCore_NearestFirst()
        {
            HoldNearest(3);
            Add(0f, 4f);
            Add(0f, 2f);
            Add(0f, 6f);
            Add(0f, 3f);
            Add(0f, 20f);
            Build();

            Select(AimRule.ThreeNearest, 9f);

            Assert.That(_aim.NearestCount, Is.EqualTo(3));
            Assert.That(_aim.GetNearest(0), Is.EqualTo(1));
            Assert.That(_aim.GetNearest(1), Is.EqualTo(3));
            Assert.That(_aim.GetNearest(2), Is.EqualTo(0));
            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
        }

        [Test]
        public void ThreeNearest_WithFewerInRange_HoldsOnlyThose()
        {
            Add(0f, 4f);
            Add(0f, 30f);
            Build();

            Select(AimRule.ThreeNearest, 9f);

            Assert.That(_aim.NearestCount, Is.EqualTo(1));
            Assert.That(() => _aim.GetNearest(1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void ThreeNearest_Tie_GoesToTheLowerIndex()
        {
            Add(3f, 0f);
            Add(0f, 3f);
            Add(-3f, 0f);
            Add(0f, -3f);
            Build();

            Select(AimRule.ThreeNearest, 9f);

            Assert.That(_aim.GetNearest(0), Is.EqualTo(0));
            Assert.That(_aim.GetNearest(1), Is.EqualTo(1));
            Assert.That(_aim.GetNearest(2), Is.EqualTo(2));
        }

        [Test]
        public void ThreeNearest_WhileMouseLive_UsesTheNearestToTheCursor()
        {
            HoldNearest(3);
            Add(0f, 4f);
            Add(0f, 2f);
            Add(0f, 6f);
            Add(0f, 3f);
            Build();

            SelectMouse(AimRule.ThreeNearest, new Vector3(0f, 0f, 6f));

            Assert.That(_aim.NearestCount, Is.EqualTo(3));
            Assert.That(_aim.GetNearest(0), Is.EqualTo(2));
            Assert.That(_aim.GetNearest(1), Is.EqualTo(0));
            Assert.That(_aim.GetNearest(2), Is.EqualTo(3));
        }

        [Test]
        public void Line_PicksTheStripHoldingTheMostBeekeepers_OverANearerLoner()
        {
            Add(0f, 4f);
            Add(0f, 8f);
            Add(0.3f, 11f);
            Add(3f, 0f);
            Build();

            Select(AimRule.Line, 14f);

            Assert.That(_aim.TargetIndex, Is.EqualTo(0));
        }

        [Test]
        public void Line_Tie_GoesToThreat()
        {
            Add(0f, 5f);
            Add(3f, 0f);
            Build();

            Select(AimRule.Line, 14f);

            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
        }

        [Test]
        public void Line_DoesNotCountBeekeepersBehindTheCore()
        {
            Add(0f, -3f);
            Add(0f, 4f);
            Add(0f, 8f);
            Build();

            Select(AimRule.Line, 14f);

            // The line through index 1 holds 2; the line through index 0 holds 1, although it is nearest.
            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
        }

        [Test]
        public void Line_StripIsAsWideAsTheConfigSays()
        {
            Add(0f, 5f);
            Add(1.5f, 9f);
            Add(3f, 0f);
            Build();

            // No line passes within 0.6 m of another beekeeper, so nothing pairs up and the nearest (index 2) wins.
            Select(AimRule.Line, 14f);

            Assert.That(_aim.TargetIndex, Is.EqualTo(2));
        }

        [Test]
        public void Crowd_PicksTheBeekeeperWithTheMostOthersNear()
        {
            Add(0f, 3f);
            Add(10f, 0f);
            Add(11f, 0f);
            Add(12f, 0f);
            Build();

            Select(AimRule.Crowd, 18f);

            // Index 0 has no one near it. Indexes 1, 2 and 3 each have two, and index 1 is nearest the core.
            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
        }

        [Test]
        public void Crowd_BeatsTheNearerLoner_AndIgnoresTheDead()
        {
            Add(0f, 3f);
            Add(10f, 0f);
            Add(11f, 0f);
            FakeTarget dead = Add(10.5f, 1f);
            dead.IsAlive = false;
            Build();

            Select(AimRule.Crowd, 18f);

            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
        }

        [Test]
        public void Crowd_RadiusComesFromTheConfig()
        {
            Add(0f, 3f);
            Add(10f, 0f);
            Add(13.6f, 0f);
            Build();

            // 3.6 m apart is outside 3.5 m, so nobody has a crowd and the nearest wins.
            Select(AimRule.Crowd, 18f);

            Assert.That(_aim.TargetIndex, Is.EqualTo(0));
        }

        [Test]
        public void Hive_NoRecentHit_PicksTheNearestStandingHive()
        {
            AddHive(10f, 0f);
            AddHive(0f, 20f);
            Hive near = AddHive(2f, 0f);
            near.Damage(near.MaxHealth);
            Build();

            Assert.That(Select(AimRule.Hive), Is.True);

            Assert.That(_aim.HiveIndex, Is.EqualTo(0));
            Assert.That(_aim.Target, Is.Null);
            Assert.That(_aim.HasTarget, Is.True);
            Assert.That(_aim.AimPoint, Is.EqualTo(new Vector3(10f, 0f, 0f)));
        }

        [Test]
        public void Hive_PrefersTheHiveHitMostRecently()
        {
            AddHive(10f, 0f);
            Hive second = AddHive(0f, 20f);
            Hive third = AddHive(30f, 0f);
            Build();

            second.Damage(10f);
            _aim.Tick(0f);
            _aim.Tick(1f);
            third.Damage(10f);
            _aim.Tick(0f);
            Select(AimRule.Hive);
            Assert.That(_aim.HiveIndex, Is.EqualTo(2));

            _aim.Tick(1f);
            second.Damage(10f);
            _aim.Tick(0f);
            Select(AimRule.Hive);
            Assert.That(_aim.HiveIndex, Is.EqualTo(1));
        }

        [Test]
        public void Hive_HitStopsCountingAfterThreeSeconds()
        {
            AddHive(10f, 0f);
            Hive far = AddHive(0f, 20f);
            Build();
            far.Damage(10f);
            _aim.Tick(0f);

            _aim.Tick(3f);
            Select(AimRule.Hive);
            Assert.That(_aim.HiveIndex, Is.EqualTo(1), "exactly three seconds still counts");

            _aim.Tick(0.01f);
            Select(AimRule.Hive);
            Assert.That(_aim.HiveIndex, Is.EqualTo(0), "after three seconds the nearest wins");
        }

        [Test]
        public void Hive_RecentlyHitButDestroyed_IsSkipped()
        {
            AddHive(10f, 0f);
            Hive far = AddHive(0f, 20f);
            Build();
            far.Damage(far.MaxHealth);
            _aim.Tick(0f);

            Select(AimRule.Hive);

            Assert.That(_aim.HiveIndex, Is.EqualTo(0));
        }

        [Test]
        public void Hive_AllDestroyed_StrikesAheadOfTheCore()
        {
            Hive only = AddHive(10f, 0f);
            only.Damage(only.MaxHealth);
            Build();

            Assert.That(Select(AimRule.Hive), Is.False);

            Assert.That(_aim.HiveIndex, Is.EqualTo(-1));
            Assert.That(_aim.AimPoint, Is.EqualTo(new Vector3(0f, 0f, 6f)));
        }

        [Test]
        public void Mouse_AimsAtItsGroundPoint_AndPicksTheNearestBeekeeperWithinThreeMetres()
        {
            Add(5f, 5f);
            Add(8f, 5f);
            Build();
            var mouse = new Vector3(7f, 0f, 5f);

            Assert.That(SelectMouse(AimRule.Threat, mouse), Is.True);

            Assert.That(_aim.TargetIndex, Is.EqualTo(1));
            Assert.That(_aim.AimPoint, Is.EqualTo(mouse));
        }

        [Test]
        public void Mouse_BeekeeperBeyondThreeMetres_IsNotPicked_ButTheAimStaysOnTheMouse()
        {
            Add(5f, 5f);
            Build();
            var mouse = new Vector3(9.5f, 0f, 5f);

            Assert.That(SelectMouse(AimRule.Threat, mouse), Is.False);

            Assert.That(_aim.HasTarget, Is.False);
            Assert.That(_aim.AimPoint, Is.EqualTo(mouse));
        }

        [Test]
        public void Mouse_TakesOver_AndHandsBackThroughTheHold()
        {
            Add(5f, 5f);
            Add(8f, 5f);
            Build();

            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(0), "auto-aim picks the nearest");

            SelectMouse(AimRule.Threat, new Vector3(8f, 0f, 5f));
            Assert.That(_aim.TargetIndex, Is.EqualTo(1), "the mouse takes over at once");

            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(1), "the mouse's pick is held for a second");

            _aim.Tick(1.1f);
            Select(AimRule.Threat);
            Assert.That(_aim.TargetIndex, Is.EqualTo(0), "then auto-aim is back in charge");
        }

        [Test]
        public void EmptyMap_StrikesSixMetresAheadOfWhereTheCoreFaces()
        {
            Build();

            Assert.That(Select(AimRule.Threat), Is.False);

            Assert.That(_aim.HasTarget, Is.False);
            Assert.That(_aim.Target, Is.Null);
            Assert.That(_aim.TargetIndex, Is.EqualTo(-1));
            Assert.That(_aim.AimPoint, Is.EqualTo(new Vector3(0f, 0f, 6f)));
        }

        [Test]
        public void EmptyMap_AheadFollowsTheFacing_IgnoresHeight_AndSurvivesNoFacing()
        {
            Build();
            var core = new Vector3(2f, 1f, 3f);

            _aim.Select(AimRule.Crowd, 18f, core, new Vector3(1f, 5f, 0f), false, Vector3.zero);
            Assert.That(_aim.AimPoint.x, Is.EqualTo(8f).Within(Tolerance));
            Assert.That(_aim.AimPoint.y, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(_aim.AimPoint.z, Is.EqualTo(3f).Within(Tolerance));

            _aim.Select(AimRule.Crowd, 18f, core, Vector3.zero, false, Vector3.zero);
            Assert.That(_aim.AimPoint.x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(_aim.AimPoint.z, Is.EqualTo(9f).Within(Tolerance));
        }

        [Test]
        public void TargetThatLeavesTheList_ClearsWithoutThrowing()
        {
            Add(0f, 5f);
            Build();
            Select(AimRule.Threat);

            _targets.Clear();

            Assert.That(Select(AimRule.Threat), Is.False);
            Assert.That(_aim.Target, Is.Null);
        }
    }
}
