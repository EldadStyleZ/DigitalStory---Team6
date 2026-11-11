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

        // ---- Gain ----

        [Test]
        public void Gain_AddsRatePerHivePerSecond()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Gain(1f, 2);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + 2 * (int)config.HiveCallWaspsPerSecondPerHive));
        }

        [Test]
        public void Gain_OneHive_AddsHalfAsMuchAsTwo()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);

            body.Gain(1f, 1);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + (int)config.HiveCallWaspsPerSecondPerHive));
        }

        [Test]
        public void Gain_NoStandingHives_AddsNothing()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int raised = 0;
            body.CountChanged += _ => raised++;

            body.Gain(1f, 0);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Gain_FractionsAreCarriedBetweenCalls()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            float halfAWaspSeconds = 0.5f / config.HiveCallWaspsPerSecondPerHive;

            body.Gain(halfAWaspSeconds, 1);
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));

            body.Gain(halfAWaspSeconds, 1);
            Assert.That(body.Count, Is.EqualTo(config.StartingWasps + 1));
        }

        [Test]
        public void Gain_CapsAtMax()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Gain(100f, 2);

            Assert.That(body.Count, Is.EqualTo(config.MaxWasps));
        }

        [Test]
        public void Gain_JustBelowMax_StopsExactlyAtMax()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Gain(100f, 2);
            body.Lose(10);

            body.Gain(1f, 2);

            Assert.That(body.Count, Is.EqualTo(config.MaxWasps));
        }

        [Test]
        public void Gain_AtMax_RaisesNoEvent()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Gain(100f, 2);
            int raised = 0;
            body.CountChanged += _ => raised++;

            body.Gain(1f, 2);

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

            body.Gain(1f, 1);

            Assert.That(seen, Is.EqualTo(body.Count));
        }

        [Test]
        public void Gain_WhileCollapsed_AddsNothing()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);

            body.Gain(1f, 2);

            Assert.That(body.Count, Is.EqualTo(0));
            Assert.That(body.IsCollapsed, Is.True);
        }

        // ---- Respawn ----

        [Test]
        public void Tick_NotCollapsed_ChangesNothing()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            int raised = 0;
            body.CountChanged += _ => raised++;
            body.Respawned += () => raised++;

            body.Tick(10f, 2);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps));
            Assert.That(raised, Is.EqualTo(0));
        }

        [Test]
        public void Tick_BeforeTheBlockEnds_StaysCollapsed()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);

            body.Tick(config.CollapseBlockSeconds - 0.1f, 2);

            Assert.That(body.IsCollapsed, Is.True);
            Assert.That(body.Count, Is.EqualTo(0));
        }

        [Test]
        public void Tick_BlockEndsExactly_RespawnsWithRespawnWasps()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);

            body.Tick(config.CollapseBlockSeconds, 2);

            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps));
        }

        [Test]
        public void Tick_BlockCountsDownAcrossSeveralTicks()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);

            body.Tick(1f, 2);
            body.Tick(1f, 2);
            Assert.That(body.IsCollapsed, Is.True);

            body.Tick(1f, 2);
            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps));
        }

        [Test]
        public void Tick_Respawn_RaisesRespawnedOnceAndCountChangedWithRespawnWasps()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            int respawned = 0;
            int seen = -1;
            bool collapsedInHandler = true;
            body.Respawned += () =>
            {
                respawned++;
                collapsedInHandler = body.IsCollapsed;
            };
            body.CountChanged += count => seen = count;

            body.Tick(config.CollapseBlockSeconds + 1f, 1);
            body.Tick(1f, 1);

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

            body.Tick(config.CollapseBlockSeconds + 10f, 0);

            Assert.That(body.IsCollapsed, Is.True);
            Assert.That(respawned, Is.EqualTo(0));

            body.Tick(0.01f, 1);

            Assert.That(body.IsCollapsed, Is.False);
            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps));
            Assert.That(respawned, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AfterRespawn_LoseWorksAgain()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            body.Tick(config.CollapseBlockSeconds, 1);

            int removed = body.Lose(100);

            Assert.That(removed, Is.EqualTo(100));
            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps - 100));
        }

        [Test]
        public void Collapse_Twice_BlocksForTheFullTimeEachTime()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            body.Tick(config.CollapseBlockSeconds, 1);
            body.Lose(config.RespawnWasps);
            Assert.That(body.IsCollapsed, Is.True);

            body.Tick(config.CollapseBlockSeconds - 0.1f, 1);

            Assert.That(body.IsCollapsed, Is.True);

            body.Tick(0.1f, 1);

            Assert.That(body.IsCollapsed, Is.False);
        }

        [Test]
        public void Gain_AfterRespawn_Works()
        {
            var config = NewConfig();
            var body = CollapsedBody(config);
            body.Tick(config.CollapseBlockSeconds, 1);

            body.Gain(1f, 1);

            Assert.That(body.Count, Is.EqualTo(config.RespawnWasps + (int)config.HiveCallWaspsPerSecondPerHive));
        }

        [Test]
        public void Respawn_ChangedBlockSeconds_UsesTheConfig()
        {
            var config = NewConfig();
            JsonUtility.FromJsonOverwrite("{\"<CollapseBlockSeconds>k__BackingField\":10}", config);
            var body = CollapsedBody(config);

            body.Tick(9.9f, 1);
            Assert.That(body.IsCollapsed, Is.True);

            body.Tick(0.2f, 1);
            Assert.That(body.IsCollapsed, Is.False);
        }

        [Test]
        public void Tick_DoesNotTouchTheCountOfAHealthySwarm()
        {
            var config = NewConfig();
            var body = new SwarmBody(config);
            body.Lose(1000);

            body.Tick(5f, 2);

            Assert.That(body.Count, Is.EqualTo(config.StartingWasps - 1000));
        }
    }
}
