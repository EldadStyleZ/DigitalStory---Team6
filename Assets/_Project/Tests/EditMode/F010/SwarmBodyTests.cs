using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    public sealed class SwarmBodyTests
    {
        // The dome's share of a loss, the default on its song asset.
        private const float DomeShare = 0.15f;

        private static SwarmConfig NewConfig()
        {
            return ScriptableObject.CreateInstance<SwarmConfig>();
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
        public void New_StartsAtStartingWaspsAndNotCollapsed()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
            Assert.That(body.Max, Is.EqualTo(config.MaxWasps));
            Assert.That(body.IsCollapsed, Is.False);
        }

        // ---- Lose ----

        [Test]
        public void Lose_NotDomed_RemovesAllOfThem()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            int removed = body.Lose(1000);

            Assert.That(removed, Is.EqualTo(1000));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 1000));
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
        public void Lose_ToExactlyCollapseBelow_DoesNotCollapse()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            bool collapsed = false;
            body.Collapsed += () => collapsed = true;

            body.Lose(config.StartingWasps - config.CollapseBelow);

            Assert.That(body.Count, Is.EqualTo(config.CollapseBelow));
            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(collapsed, Is.False);
        }

        [Test]
        public void Lose_OneBelowCollapseBelow_CollapsesAndCountIsZero()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Lose(config.StartingWasps - config.CollapseBelow + 1);

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

            body.Lose(config.StartingWasps - 100);

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
            body.Lose(config.StartingWasps - config.CollapseBelow);

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
            body.Lose(config.StartingWasps - config.CollapseBelow);
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

        // ---- Gain: wasps from the hives' stores, never from nothing (plan section M) ----

        [Test]
        public void Gain_AddsTheWaspsAndSaysHowManyJoined()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            int joined = body.Gain(150f);

            Assert.That(joined, Is.EqualTo(150));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + 150));
        }

        [TestCase(0f)]
        [TestCase(-20f)]
        public void Gain_NothingOrLess_AddsNothingAndRaisesNothing(float wasps)
        {
            var config = NewConfig();
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
            var config = NewConfig();
            var body = new SwarmBody(config);

            Assert.That(body.Gain(0.5f), Is.EqualTo(0));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));

            Assert.That(body.Gain(0.5f), Is.EqualTo(1));
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + 1));
        }

        [Test]
        public void Gain_ManySmallParts_AddUpToTheWhole()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            for (int i = 0; i < 60; i++)
            {
                body.Gain(150f / 60f);
            }

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + 150).Within(1));
        }

        [Test]
        public void Gain_CapsAtMax()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            int joined = body.Gain(100000f);

            Assert.That(body.Count, Is.EqualTo(config.MaxWasps));
            Assert.That(joined, Is.EqualTo(config.MaxWasps - config.StartingWasps));
        }

        [Test]
        public void Gain_JustBelowMax_StopsExactlyAtMax()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Gain(100000f);
            body.Lose(10);

            body.Gain(100f);

            Assert.That(body.Count, Is.EqualTo(config.MaxWasps));
        }

        [Test]
        public void Gain_AtMax_RaisesNoEvent()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Gain(100000f);
            int raised = 0;
            body.CountChanged += _ => raised++;

            Assert.That(body.Gain(100f), Is.EqualTo(0));

            Assert.That(raised, Is.EqualTo(0));
            Assert.That(body.Count, Is.EqualTo(config.MaxWasps));
        }

        [Test]
        public void Gain_RaisesCountChangedWithTheNewCount()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int seen = -1;
            body.CountChanged += count => seen = count;

            body.Gain(100f);

            Assert.That(seen, Is.EqualTo(body.Count));
        }

        [Test]
        public void Gain_WhileCollapsed_AddsNothing()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);

            Assert.That(body.Gain(200f), Is.EqualTo(0));

            Assert.That(body.Count, Is.EqualTo(0));
            Assert.That(body.IsCollapsed, Is.True);
        }

        [Test]
        public void Room_IsWhatIsLeftToTheMaximum_LessThePartOfAWaspOnItsWay()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Gain(0.25f);

            Assert.That(body.Room, Is.EqualTo(config.MaxWasps - config.StartingWasps - 0.25f).Within(0.001f));
        }

        [Test]
        public void Room_FullOrCollapsed_IsZero()
        {
            var config = NewConfig();
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

            Assert.That(body.Spendable, Is.EqualTo(config.StartingWasps - config.CollapseBelow));
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
            body.Lose(config.StartingWasps - config.CollapseBelow - 40);

            bool paid = body.TrySpend(40);

            Assert.That(paid, Is.True);
            Assert.That(body.Count, Is.EqualTo(config.CollapseBelow));
            Assert.That(body.IsCollapsed, Is.False);
        }

        [Test]
        public void TrySpend_ThatWouldLeaveFewerThanTheCollapseLine_IsRefusedAndChangesNothing()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Lose(config.StartingWasps - config.CollapseBelow - 39);
            int raised = 0;
            body.CountChanged += _ => raised++;
            body.Collapsed += () => raised++;

            bool paid = body.TrySpend(40);

            Assert.That(paid, Is.False);
            Assert.That(body.Count, Is.EqualTo(config.CollapseBelow + 39));
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

        // ---- Respawn: taken from the hives' stores, with a floor (plan section M) ----

        // Rich stores hold more than RespawnWasps between them, so a respawn takes the full RespawnWasps.
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
        public void Constructor_MinRespawnWaspsBelowTheCollapseLineOrAboveMax_Throws()
        {
            var below = NewConfig();
            JsonUtility.FromJsonOverwrite("{\"<MinRespawnWasps>k__BackingField\":239}", below);
            var above = NewConfig();
            JsonUtility.FromJsonOverwrite("{\"<MinRespawnWasps>k__BackingField\":4001}", above);

            Assert.That(() => new SwarmBody(below), Throws.ArgumentException);
            Assert.That(() => new SwarmBody(above), Throws.ArgumentException);
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
        public void Tick_BlockEndsExactly_RespawnsWithRespawnWaspsTakenFromTheStores()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = RichHives();

            body.Tick(config.CollapseBlockSeconds, hives);

            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps));
            Assert.That(hives[0].Store + hives[1].Store, Is.EqualTo((2 * RichStore) - config.RespawnWasps).Within(0.001f), "no wasp comes from nothing");
        }

        [Test]
        public void Tick_RespawnWithStoresBelowRespawnWasps_TakesAllTheyHold()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = Hives(300);

            body.Tick(config.CollapseBlockSeconds, hives);

            Assert.That(body.Count, Is.EqualTo(600));
            Assert.That(hives[0].Store + hives[1].Store, Is.EqualTo(0f));
        }

        [Test]
        public void Tick_RespawnWithStoresBelowTheFloor_ComesBackWithMinRespawnWasps()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = Hives(50);

            body.Tick(config.CollapseBlockSeconds, hives);

            Assert.That(body.Count, Is.EqualTo(config.MinRespawnWasps), "the player can always act");
            Assert.That(hives[0].Store + hives[1].Store, Is.EqualTo(0f), "what the stores held is used first");
        }

        [Test]
        public void Tick_RespawnWithEmptyStores_ComesBackWithMinRespawnWasps()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);

            body.Tick(config.CollapseBlockSeconds, Hives(0));

            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(config.MinRespawnWasps));
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
            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps));
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
            Assert.That(seen, Is.EqualTo(config.RespawnWasps));
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
            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps));
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
            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps - 100));
        }

        [Test]
        public void Collapse_Twice_BlocksForTheFullTimeEachTime()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            HiveSet hives = RichHives();
            body.Tick(config.CollapseBlockSeconds, hives);
            body.Lose(config.RespawnWasps);
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

            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps + 100));
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
            body.Lose(1000);

            body.Tick(5f, RichHives());

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 1000));
        }
    }
}
