using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The swarm's count, its capacity and the lines drawn as shares of the capacity (plan sections H and M), on the default
    /// SwarmConfig: 800 wasps of a capacity of 800, a ceiling of 4,000, a collapse below 12% of the capacity, a respawn of
    /// 50% from the stores with a floor of 15%, and a low warning below 30%.
    /// </summary>
    public sealed class SwarmBodyTests
    {
        // The dome's share of a loss, the default on its song asset.
        private const float DomeShare = 0.15f;

        // What one nest adds to the capacity, the default on NestConfig.
        private const int NestCapacity = 400;

        private static SwarmConfig NewConfig()
        {
            return ScriptableObject.CreateInstance<SwarmConfig>();
        }

        /// <summary>A config whose swarm starts with half its capacity, so wasps have room to join.</summary>
        private static SwarmConfig HalfFullConfig()
        {
            var config = NewConfig();
            JsonUtility.FromJsonOverwrite("{\"<StartingWasps>k__BackingField\":" + (config.StartingCapacity / 2) + "}", config);
            return config;
        }

        private static SwarmBody NewBody()
        {
            return new SwarmBody(NewConfig());
        }

        /// <summary>A body that has collapsed and is waiting for its respawn.</summary>
        private static SwarmBody CollapsedBody(SwarmConfig config)
        {
            var body = new SwarmBody(config);
            body.Lose(config.StartingWasps);
            return body;
        }

        [Test]
        public void Constructor_NullConfig_Throws()
        {
            Assert.That(() => new SwarmBody(null), Throws.ArgumentNullException);
        }

        [Test]
        public void New_StartsAtStartingWaspsAndStartingCapacityAndNotCollapsed()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
            Assert.That(body.Capacity, Is.EqualTo(config.StartingCapacity));
            Assert.That(body.Max, Is.EqualTo(config.MaxWasps), "Max stays the ceiling the capacity can reach");
            Assert.That(body.IsCollapsed, Is.False);
        }

        [Test]
        public void New_EightHundredOfEightHundred_UnderACeilingOfFourThousand()
        {
            var body = NewBody();

            Assert.That(body.Count, Is.EqualTo(800));
            Assert.That(body.Capacity, Is.EqualTo(800));
            Assert.That(body.Max, Is.EqualTo(4000));
        }

        [TestCase("{\"<StartingCapacity>k__BackingField\":4001}")]
        [TestCase("{\"<StartingCapacity>k__BackingField\":0}")]
        [TestCase("{\"<StartingWasps>k__BackingField\":801}")]
        [TestCase("{\"<StartingWasps>k__BackingField\":-1}")]
        [TestCase("{\"<CollapseShare>k__BackingField\":1.5}")]
        [TestCase("{\"<CollapseShare>k__BackingField\":-0.1}")]
        [TestCase("{\"<RespawnShare>k__BackingField\":1.5}")]
        [TestCase("{\"<LowShare>k__BackingField\":-0.1}")]
        [TestCase("{\"<MinRespawnShare>k__BackingField\":0.11}")]
        [TestCase("{\"<MinRespawnShare>k__BackingField\":1.01}")]
        public void Constructor_CountsOrSharesThatCannotHold_Throw(string json)
        {
            var config = NewConfig();
            JsonUtility.FromJsonOverwrite(json, config);

            Assert.That(() => new SwarmBody(config), Throws.ArgumentException);
        }

        // ---- The lines are shares of the capacity (plan section H) ----

        [Test]
        public void Lines_AtTheStartingCapacity_AreTwelveAndThirtyPercentOfIt()
        {
            var body = NewBody();

            Assert.That(body.CollapseLine, Is.EqualTo(96), "12% of 800");
            Assert.That(body.LowLine, Is.EqualTo(240), "30% of 800");
        }

        [Test]
        public void Lines_FollowTheCapacityAsItGrows()
        {
            var body = NewBody();

            body.GrowCapacity(NestCapacity);

            Assert.That(body.CollapseLine, Is.EqualTo(144), "12% of 1,200");
            Assert.That(body.LowLine, Is.EqualTo(360), "30% of 1,200");
        }

        [Test]
        public void Lose_ToBelowTheLineOfAGrownCapacity_Collapses()
        {
            var small = NewBody();
            var grown = NewBody();
            grown.GrowCapacity(NestCapacity);

            small.Lose(800 - 140);
            grown.Lose(800 - 140);

            Assert.That(small.IsCollapsed, Is.False, "140 is above 12% of 800");
            Assert.That(grown.IsCollapsed, Is.True, "140 is below 12% of 1,200");
        }

        [Test]
        public void IsLow_BelowThirtyPercentOfTheCapacity()
        {
            var body = NewBody();

            body.Lose(800 - 240);
            Assert.That(body.IsLow, Is.False, "exactly the low line is not low");

            body.Lose(1);
            Assert.That(body.IsLow, Is.True);

            body.GrowCapacity(NestCapacity);
            body.Gain(120f);
            Assert.That(body.Count, Is.EqualTo(359));
            Assert.That(body.IsLow, Is.True, "the line grew to 360 with the capacity");
        }

        // ---- GrowCapacity: a nest raises how many wasps the swarm may hold (plan section H) ----

        [Test]
        public void GrowCapacity_AddsToTheCapacityAndSaysHowMuch_WithoutAddingWasps()
        {
            var body = NewBody();

            int grown = body.GrowCapacity(NestCapacity);

            Assert.That(grown, Is.EqualTo(NestCapacity));
            Assert.That(body.Capacity, Is.EqualTo(1200));
            Assert.That(body.Count, Is.EqualTo(800), "the wasps come separately");
            Assert.That(body.Room, Is.EqualTo(400f).Within(0.001f));
        }

        [Test]
        public void GrowCapacity_StopsAtTheCeiling()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            for (int nest = 0; nest < 8; nest++)
            {
                body.GrowCapacity(NestCapacity);
            }

            Assert.That(body.Capacity, Is.EqualTo(config.MaxWasps), "800 + 8 x 400 would be 4,000 exactly");
            Assert.That(body.GrowCapacity(NestCapacity), Is.EqualTo(0), "nothing past the ceiling");
            Assert.That(body.Capacity, Is.EqualTo(config.MaxWasps));
        }

        [Test]
        public void GrowCapacity_NearTheCeiling_GrowsOnlyToIt()
        {
            var body = NewBody();
            body.GrowCapacity(3000);

            int grown = body.GrowCapacity(NestCapacity);

            Assert.That(grown, Is.EqualTo(200));
            Assert.That(body.Capacity, Is.EqualTo(4000));
        }

        [Test]
        public void GrowCapacity_Negative_Throws()
        {
            Assert.That(() => NewBody().GrowCapacity(-1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Gain_AfterGrowCapacity_FillsUpToTheNewCapacity()
        {
            var body = NewBody();
            body.GrowCapacity(NestCapacity);

            int joined = body.Gain(1000f);

            Assert.That(joined, Is.EqualTo(400));
            Assert.That(body.Count, Is.EqualTo(1200));
        }

        // ---- Lose ----

        [Test]
        public void Lose_NotDomed_RemovesAllOfThem()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            int removed = body.Lose(300);

            Assert.That(removed, Is.EqualTo(300));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 300));
        }

        [Test]
        public void Lose_DomeShare_OnlyThatShareGetsThrough()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int expected = Mathf.RoundToInt(1000 * DomeShare);

            int removed = body.Lose(1000, DomeShare);

            Assert.That(expected, Is.EqualTo(150));
            Assert.That(removed, Is.EqualTo(150));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 150));
        }

        [Test]
        public void Lose_DomeShareOfATinyLoss_RoundsToZero()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            int removed = body.Lose(3, DomeShare);

            Assert.That(removed, Is.EqualTo(0));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
        }

        [Test]
        public void Lose_AnotherShare_UsesTheShareGiven()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            int removed = body.Lose(1000, 0.5f);

            Assert.That(removed, Is.EqualTo(500));
        }

        [TestCase(-0.01f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        public void LoseAndDrain_ShareOutsideZeroToOne_Throw(float share)
        {
            var body = NewBody();

            Assert.That(() => body.Lose(100, share), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => body.Drain(100f, 1f, share), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [TestCase(0)]
        [TestCase(-50)]
        public void Lose_ZeroOrNegative_ChangesNothingAndRaisesNothing(int wasps)
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int raised = 0;
            body.CountChanged += _ => raised++;

            int removed = body.Lose(wasps);

            Assert.That(removed, Is.EqualTo(0));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Lose_RaisesCountChangedWithTheNewCount()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int seen = -1;
            int raised = 0;
            body.CountChanged += count =>
            {
                seen = count;
                raised++;
            };

            body.Lose(400);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(seen, Is.EqualTo(config.StartingWasps - 400));
        }

        // ---- Collapse ----

        [Test]
        public void Lose_ToExactlyTheCollapseLine_DoesNotCollapse()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            bool collapsed = false;
            body.Collapsed += () => collapsed = true;

            body.Lose(config.StartingWasps - body.CollapseLine);

            Assert.That(body.Count, Is.EqualTo(body.CollapseLine));
            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(collapsed, Is.False);
        }

        [Test]
        public void Lose_OneBelowTheCollapseLine_CollapsesAndCountIsZero()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Lose(config.StartingWasps - body.CollapseLine + 1);

            Assert.That(body.IsCollapsed, Is.True);
            Assert.That(body.Count, Is.EqualTo(0));
        }

        [Test]
        public void Lose_Collapse_RaisesCollapsedOnceWithCountAlreadyZero()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int raised = 0;
            int countInHandler = -1;
            bool collapsedInHandler = false;
            body.Collapsed += () =>
            {
                raised++;
                countInHandler = body.Count;
                collapsedInHandler = body.IsCollapsed;
            };

            body.Lose(config.StartingWasps);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(countInHandler, Is.EqualTo(0));
            Assert.That(collapsedInHandler, Is.True);
        }

        [Test]
        public void Lose_Collapse_ReportsZeroAsTheNewCount()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int lastSeen = -1;
            body.CountChanged += count => lastSeen = count;

            body.Lose(config.StartingWasps - (body.CollapseLine / 2));

            Assert.That(lastSeen, Is.EqualTo(0));
        }

        [Test]
        public void Lose_MoreThanTheCount_DoesNotGoBelowZero()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Lose(config.StartingWasps * 3);

            Assert.That(body.Count, Is.EqualTo(0));
        }

        [Test]
        public void Lose_WhileCollapsed_RemovesNothingAndRaisesNothing()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            int collapsedRaised = 0;
            int countRaised = 0;
            body.Collapsed += () => collapsedRaised++;
            body.CountChanged += _ => countRaised++;

            int removed = body.Lose(500);

            Assert.That(removed, Is.EqualTo(0));
            Assert.That(collapsedRaised, Is.EqualTo(0));
            Assert.That(countRaised, Is.EqualTo(0));
            Assert.That(body.Count, Is.EqualTo(0));
        }

        [Test]
        public void Lose_DomedLossStillCollapses_WhenItTakesTheCountBelowTheLimit()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Lose(config.StartingWasps - body.CollapseLine);

            body.Lose(1000, DomeShare);

            Assert.That(body.IsCollapsed, Is.True);
            Assert.That(body.Count, Is.EqualTo(0));
        }

        // ---- Drain ----

        [Test]
        public void Drain_NotDomed_RemovesRateTimesTime()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Drain(120f, 1f);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 120));
        }

        [Test]
        public void Drain_DomeShare_OnlyThatShareGetsThrough()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Drain(1000f, 1f, DomeShare);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 150));
        }

        [Test]
        public void Drain_FractionsAreCarriedBetweenCalls()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Drain(100f, 0.005f);
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));

            body.Drain(100f, 0.005f);
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 1));
        }

        [Test]
        public void Drain_ManySmallSteps_AddUpToTheSameAsOneBigStep()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            for (int i = 0; i < 60; i++)
            {
                body.Drain(120f, 1f / 60f);
            }

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 120).Within(1));
        }

        [Test]
        public void Drain_BelowOneWasp_RaisesNoEvent()
        {
            var body = NewBody();
            int raised = 0;
            body.CountChanged += _ => raised++;

            body.Drain(100f, 0.005f);

            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Drain_ToBelowTheLimit_Collapses()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Lose(config.StartingWasps - body.CollapseLine);
            int raised = 0;
            body.Collapsed += () => raised++;

            body.Drain(10f, 1f);

            Assert.That(body.IsCollapsed, Is.True);
            Assert.That(body.Count, Is.EqualTo(0));
            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void Drain_WhileCollapsed_DoesNothing()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            int raised = 0;
            body.CountChanged += _ => raised++;

            body.Drain(500f, 1f);

            Assert.That(body.Count, Is.EqualTo(0));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Drain_ZeroTime_DoesNothing()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Drain(120f, 0f);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
        }

        // ---- Gain: wasps from the hives' stores, never from nothing (plan section M), up to the capacity (section H) ----
        // The default swarm starts full, so these start half full to leave room.

        [Test]
        public void Gain_AddsTheWaspsAndSaysHowManyJoined()
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);

            int joined = body.Gain(150f);

            Assert.That(joined, Is.EqualTo(150));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + 150));
        }

        [TestCase(0f)]
        [TestCase(-20f)]
        public void Gain_NothingOrLess_AddsNothingAndRaisesNothing(float wasps)
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);
            int raised = 0;
            body.CountChanged += _ => raised++;

            Assert.That(body.Gain(wasps), Is.EqualTo(0));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
            Assert.That(raised, Is.EqualTo(0));
        }

        // A store flows in a little each tick, so the parts of a wasp must add up.
        [Test]
        public void Gain_PartsOfAWasp_AreCarriedBetweenCalls()
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);

            Assert.That(body.Gain(0.5f), Is.EqualTo(0));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));

            Assert.That(body.Gain(0.5f), Is.EqualTo(1));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + 1));
        }

        [Test]
        public void Gain_ManySmallParts_AddUpToTheWhole()
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);

            for (int i = 0; i < 60; i++)
            {
                body.Gain(150f / 60f);
            }

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + 150).Within(1));
        }

        [Test]
        public void Gain_CapsAtTheCapacity_NotTheCeiling()
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);

            int joined = body.Gain(100000f);

            Assert.That(body.Count, Is.EqualTo(config.StartingCapacity));
            Assert.That(body.Count, Is.LessThan(body.Max), "the live limit is the capacity");
            Assert.That(joined, Is.EqualTo(config.StartingCapacity - config.StartingWasps));
        }

        [Test]
        public void Gain_JustBelowTheCapacity_StopsExactlyAtIt()
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);
            body.Gain(100000f);
            body.Lose(10);

            body.Gain(100f);

            Assert.That(body.Count, Is.EqualTo(config.StartingCapacity));
        }

        [Test]
        public void Gain_AtTheCapacity_RaisesNoEvent()
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);
            body.Gain(100000f);
            int raised = 0;
            body.CountChanged += _ => raised++;

            Assert.That(body.Gain(100f), Is.EqualTo(0));

            Assert.That(raised, Is.EqualTo(0));
            Assert.That(body.Count, Is.EqualTo(config.StartingCapacity));
        }

        [Test]
        public void Gain_RaisesCountChangedWithTheNewCount()
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);
            int seen = -1;
            body.CountChanged += count => seen = count;

            body.Gain(100f);

            Assert.That(seen, Is.EqualTo(body.Count));
        }

        [Test]
        public void Gain_WhileCollapsed_AddsNothing()
        {
            var config = HalfFullConfig();
            var body = CollapsedBody(config);

            Assert.That(body.Gain(200f), Is.EqualTo(0));

            Assert.That(body.Count, Is.EqualTo(0));
            Assert.That(body.IsCollapsed, Is.True);
        }

        [Test]
        public void Room_IsWhatIsLeftToTheCapacity_LessThePartOfAWaspOnItsWay()
        {
            var config = HalfFullConfig();
            var body = new SwarmBody(config);

            body.Gain(0.25f);

            Assert.That(body.Room, Is.EqualTo(config.StartingCapacity - config.StartingWasps - 0.25f).Within(0.001f));
        }

        [Test]
        public void Room_FullOrCollapsed_IsZero()
        {
            var config = HalfFullConfig();
            var full = new SwarmBody(config);
            full.Gain(100000f);

            Assert.That(full.Room, Is.EqualTo(0f));
            Assert.That(CollapsedBody(config).Room, Is.EqualTo(0f), "a collapsed swarm takes nothing in");
        }

        // ---- Spend: a price paid only above the collapse line (plan section M) ----

        [Test]
        public void Spendable_IsWhatLiesAboveTheCollapseLine()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            Assert.That(body.Spendable, Is.EqualTo(config.StartingWasps - body.CollapseLine));
            Assert.That(CollapsedBody(config).Spendable, Is.EqualTo(0));
        }

        [Test]
        public void TrySpend_ThatLeavesTheCollapseLineOrMore_PaysAndRaisesCountChanged()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int seen = -1;
            body.CountChanged += count => seen = count;

            bool paid = body.TrySpend(40);

            Assert.That(paid, Is.True);
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 40));
            Assert.That(seen, Is.EqualTo(config.StartingWasps - 40));
        }

        [Test]
        public void TrySpend_DownToExactlyTheCollapseLine_PaysAndDoesNotCollapse()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Lose(config.StartingWasps - body.CollapseLine - 40);

            bool paid = body.TrySpend(40);

            Assert.That(paid, Is.True);
            Assert.That(body.Count, Is.EqualTo(body.CollapseLine));
            Assert.That(body.IsCollapsed, Is.False);
        }

        [Test]
        public void TrySpend_ThatWouldLeaveFewerThanTheCollapseLine_IsRefusedAndChangesNothing()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Lose(config.StartingWasps - body.CollapseLine - 39);
            int raised = 0;
            body.CountChanged += _ => raised++;
            body.Collapsed += () => raised++;

            bool paid = body.TrySpend(40);

            Assert.That(paid, Is.False);
            Assert.That(body.Count, Is.EqualTo(body.CollapseLine + 39));
            Assert.That(body.IsCollapsed, Is.False, "paying can never collapse the swarm");
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void TrySpend_WhileCollapsed_IsRefused()
        {
            var body = CollapsedBody(NewConfig());

            Assert.That(body.TrySpend(1), Is.False);
            Assert.That(body.Count, Is.EqualTo(0));
        }

        [Test]
        public void TrySpend_Nothing_PaysWithoutAnEvent()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int raised = 0;
            body.CountChanged += _ => raised++;

            Assert.That(body.TrySpend(0), Is.True);
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void TrySpend_Negative_Throws()
        {
            Assert.That(() => NewBody().TrySpend(-1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // ---- Respawn: taken from the hives' stores, with a floor (plan section M), both shares of the capacity (section H) ----

        // At the starting capacity of 800 the respawn takes 50%, 400 wasps, and the floor is 15%, 120 wasps.
        private const int Respawn = 400;
        private const int Floor = 120;

        // Rich stores hold more than a respawn takes between them, so a respawn takes its full share.
        private const int RichStore = 600;

        // Two standing hives whose stores each hold the given whole wasps.
        private static HiveSet Hives(int storeEach)
        {
            var config = ScriptableObject.CreateInstance<HiveConfig>();
            JsonUtility.FromJsonOverwrite(
                "{\"<StoreCapacity>k__BackingField\":" + storeEach + ",\"<StartStore>k__BackingField\":" + storeEach + "}", config);
            return new HiveSet(new[] { new Hive(config, new Vector3(-10f, 0f, 0f)), new Hive(config, new Vector3(10f, 0f, 0f)) });
        }

        private static HiveSet RichHives() => Hives(RichStore);

        private static HiveSet NoStandingHives()
        {
            HiveSet hives = RichHives();
            hives[0].Damage(10000f);
            hives[1].Damage(10000f);
            return hives;
        }

        [Test]
        public void RespawnLines_AreFiftyAndFifteenPercentOfTheCapacity()
        {
            var body = NewBody();

            Assert.That(body.RespawnWasps, Is.EqualTo(Respawn));
            Assert.That(body.MinRespawnWasps, Is.EqualTo(Floor));

            body.GrowCapacity(NestCapacity);

            Assert.That(body.RespawnWasps, Is.EqualTo(600), "50% of 1,200");
            Assert.That(body.MinRespawnWasps, Is.EqualTo(180), "15% of 1,200");
        }

        [Test]
        public void Tick_RespawnAfterTheCapacityGrew_TakesHalfTheNewCapacityFromTheStores()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.GrowCapacity(NestCapacity);
            body.Lose(body.Count);
            HiveSet hives = RichHives();

            body.Tick(config.CollapseBlockSeconds, hives);

            Assert.That(body.Count, Is.EqualTo(600));
            Assert.That(hives[0].Store + hives[1].Store, Is.EqualTo((2 * RichStore) - 600).Within(0.001f), "no wasp comes from nothing");
        }

        [Test]
        public void Tick_RespawnAfterTheCapacityGrew_ComesBackWithAtLeastTheNewFloor()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.GrowCapacity(NestCapacity);
            body.Lose(body.Count);

            body.Tick(config.CollapseBlockSeconds, Hives(0));

            Assert.That(body.Count, Is.EqualTo(180), "15% of 1,200, above the grown collapse line of 144");
            Assert.That(body.IsCollapsed, Is.False);
        }

        [Test]
        public void Tick_NotCollapsed_ChangesNothing()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            HiveSet hives = RichHives();
            int raised = 0;
            body.CountChanged += _ => raised++;
            body.Respawned += () => raised++;

            body.Tick(10f, hives);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
            Assert.That(raised, Is.EqualTo(0));
            Assert.That(hives[0].Store, Is.EqualTo(RichStore), "the stores are only taken from by a respawn");
        }

        [Test]
        public void Tick_BeforeTheBlockEnds_StaysCollapsed()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);

            body.Tick(config.CollapseBlockSeconds - 0.1f, RichHives());

            Assert.That(body.IsCollapsed, Is.True);
            Assert.That(body.Count, Is.EqualTo(0));
        }

        [Test]
        public void Tick_BlockEndsExactly_RespawnsWithHalfTheCapacityTakenFromTheStores()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = RichHives();

            body.Tick(config.CollapseBlockSeconds, hives);

            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(Respawn));
            Assert.That(hives[0].Store + hives[1].Store, Is.EqualTo((2 * RichStore) - Respawn).Within(0.001f), "no wasp comes from nothing");
        }

        [Test]
        public void Tick_RespawnWithStoresBelowWhatARespawnTakes_TakesAllTheyHold()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = Hives(150);

            body.Tick(config.CollapseBlockSeconds, hives);

            Assert.That(body.Count, Is.EqualTo(300), "less than the 400 a respawn takes, more than the floor");
            Assert.That(hives[0].Store + hives[1].Store, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_RespawnWithStoresBelowTheFloor_ComesBackWithTheFloor()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = Hives(50);

            body.Tick(config.CollapseBlockSeconds, hives);

            Assert.That(body.Count, Is.EqualTo(Floor), "the player can always act");
            Assert.That(hives[0].Store + hives[1].Store, Is.EqualTo(0f), "what the stores held is used first");
        }

        [Test]
        public void Tick_RespawnWithEmptyStores_ComesBackWithTheFloor()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);

            body.Tick(config.CollapseBlockSeconds, Hives(0));

            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(Floor));
        }

        [Test]
        public void Tick_BlockCountsDownAcrossSeveralTicks()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = RichHives();

            body.Tick(1f, hives);
            body.Tick(1f, hives);
            Assert.That(body.IsCollapsed, Is.True);

            body.Tick(1f, hives);
            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(Respawn));
        }

        // Plan section O: the HUD says the swarm is returning and counts the seconds.
        [Test]
        public void SecondsToRespawn_FromTheCollapse_CountsTheBlockDownToTheRespawn()
        {
            var config = NewConfig();
            SwarmBody body = NewBody();
            HiveSet hives = RichHives();
            Assert.That(body.SecondsToRespawn, Is.EqualTo(0f), "a living swarm is not returning");

            body.Lose(config.StartingWasps);
            Assert.That(body.SecondsToRespawn, Is.EqualTo(config.CollapseBlockSeconds));

            body.Tick(1.25f, hives);
            Assert.That(body.SecondsToRespawn, Is.EqualTo(config.CollapseBlockSeconds - 1.25f).Within(1e-5f));

            body.Tick(config.CollapseBlockSeconds, hives);
            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.SecondsToRespawn, Is.EqualTo(0f));
        }

        [Test]
        public void SecondsToRespawn_BlockEndedButNoHiveStands_IsZeroWhileItWaits()
        {
            var config = NewConfig();
            SwarmBody body = CollapsedBody(config);

            body.Tick(config.CollapseBlockSeconds + 1f, NoStandingHives());

            Assert.That(body.IsCollapsed, Is.True);
            Assert.That(body.SecondsToRespawn, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_Respawn_RaisesRespawnedOnceAndCountChangedWithTheNewCount()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = RichHives();
            int respawned = 0;
            int seen = -1;
            bool collapsedInHandler = true;
            body.Respawned += () =>
            {
                respawned++;
                collapsedInHandler = body.IsCollapsed;
            };
            body.CountChanged += count => seen = count;

            body.Tick(config.CollapseBlockSeconds + 1f, hives);
            body.Tick(1f, hives);

            Assert.That(respawned, Is.EqualTo(1));
            Assert.That(collapsedInHandler, Is.False);
            Assert.That(seen, Is.EqualTo(Respawn));
        }

        [Test]
        public void Tick_BlockEndedButNoHiveStands_WaitsForAHive()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            int respawned = 0;
            body.Respawned += () => respawned++;

            body.Tick(config.CollapseBlockSeconds + 10f, NoStandingHives());

            Assert.That(body.IsCollapsed, Is.True);
            Assert.That(respawned, Is.EqualTo(0));

            body.Tick(0.01f, RichHives());

            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(Respawn));
            Assert.That(respawned, Is.EqualTo(1));
        }

        [Test]
        public void Tick_NullHives_Throws()
        {
            Assert.That(() => NewBody().Tick(1f, null), Throws.ArgumentNullException);
        }

        [Test]
        public void Tick_AfterRespawn_LoseWorksAgain()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            body.Tick(config.CollapseBlockSeconds, RichHives());

            int removed = body.Lose(100);

            Assert.That(removed, Is.EqualTo(100));
            Assert.That(body.Count, Is.EqualTo(Respawn - 100));
        }

        [Test]
        public void Collapse_Twice_BlocksForTheFullTimeEachTime()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = RichHives();
            body.Tick(config.CollapseBlockSeconds, hives);
            body.Lose(body.Count);
            Assert.That(body.IsCollapsed, Is.True);

            body.Tick(config.CollapseBlockSeconds - 0.1f, hives);

            Assert.That(body.IsCollapsed, Is.True);

            body.Tick(0.1f, hives);

            Assert.That(body.IsCollapsed, Is.False);
        }

        [Test]
        public void Gain_AfterRespawn_Works()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            body.Tick(config.CollapseBlockSeconds, RichHives());

            body.Gain(100f);

            Assert.That(body.Count, Is.EqualTo(Respawn + 100));
        }

        [Test]
        public void Respawn_ChangedBlockSeconds_UsesTheConfig()
        {
            var config = NewConfig();
            JsonUtility.FromJsonOverwrite("{\"<CollapseBlockSeconds>k__BackingField\":10}", config);
            var body = CollapsedBody(config);
            HiveSet hives = RichHives();

            body.Tick(9.9f, hives);
            Assert.That(body.IsCollapsed, Is.True);

            body.Tick(0.2f, hives);
            Assert.That(body.IsCollapsed, Is.False);
        }

        [Test]
        public void Tick_DoesNotTouchTheCountOfAHealthySwarm()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Lose(300);

            body.Tick(5f, RichHives());

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 300));
        }
    }
}
