using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The lecturer's "balanced economy with real trade-offs" (plan sections H, L and M), checked by the playtest bot. Once
    /// for the class it plays whole matches with each plan on five seeds at the code defaults, prints the results table to
    /// the log (lines starting "[F010] bot:"), and each test asserts one target on those results. The owner's two are
    /// section H's "lost without nests, winnable with them" and section M's "none of the three always wins and doing
    /// nothing always loses": no plan without nests wins more than two matches in five, and the player who fetches the
    /// nests wins at least three and ends clearly better off. The bot also checks that both sinks of wasps matter and that a
    /// won match lasts six to nine minutes. <see cref="WideSweep_ThirtySeeds_HoldsTheRates"/> plays thirty seeds; it is
    /// explicit, so only a person asks for it.
    /// </summary>
    public sealed class EconomyPlaytestTests
    {
        private const int SeedCount = 5;
        private const int WideSeedCount = 30;

        // A match that has neither been won nor lost by then has stalled; it counts as not won.
        private const float MatchLimitSeconds = 20f * 60f;

        // How much more hive health, as a share, the player who fetches nests must keep than the one who does not.
        private const float ClearlyBetterHiveShare = 0.2f;

        private const float FewestHealingShare = 0.15f;
        private const float MostHealingShare = 0.45f;
        private const float ShortestWinSeconds = 6f * 60f;
        private const float LongestWinSeconds = 9f * 60f;

        private const string Idle = "Idle";
        private const string StingOnly = "StingOnly";
        private const string AttackOnly = "AttackOnly";
        private const string HealOnly = "HealOnly";
        private const string Mixed = "Mixed";
        private const string MixedWithNests = "MixedWithNests";

        // A plan may remember things during a match, so each match gets a new one.
        private static readonly Func<IPlaytestPlan>[] Plans =
        {
            () => new IdlePlan(), () => new StingOnlyPlan(), () => new AttackOnlyPlan(), () => new HealOnlyPlan(),
            () => new MixedPlan(), () => new MixedWithNestsPlan(),
        };

        private Dictionary<string, IReadOnlyList<PlaytestRun>> _runs;

        [OneTimeSetUp]
        public void PlayEveryPlan()
        {
            _runs = PlayAll(SeedCount);
        }

        [Test]
        public void Idle_AlwaysLoses_ByRoundThreeAtTheLatest()
        {
            foreach (PlaytestRun run in _runs[Idle])
            {
                Assert.IsFalse(run.Won || run.TimedOut, Describe(run));
                Assert.LessOrEqual(run.Round, 3, Describe(run));
            }
        }

        [Test]
        public void StingOnly_WinsAtMostOneMatchInFive()
        {
            Assert.LessOrEqual(PlaytestReport.Wins(_runs[StingOnly]), 1, Wins(StingOnly));
        }

        // Neither pure plan is enough: attacking without healing lets the hives fall, and healing without attacking never ends a wave.
        [TestCase(AttackOnly)]
        [TestCase(HealOnly)]
        public void PurePlan_WinsAtMostTwoMatchesInFive(string plan)
        {
            Assert.LessOrEqual(PlaytestReport.Wins(_runs[plan]), 2, Wins(plan));
        }

        // Section H: "lost without nests". Even a careful player who never fetches a nest loses most matches.
        [Test]
        public void Mixed_WithoutNests_WinsAtMostTwoMatchesInFive()
        {
            Assert.LessOrEqual(PlaytestReport.Wins(_runs[Mixed]), 2, Wins(Mixed));
        }

        // Section H: "winnable with them". The same player who fetches the nests wins most matches, but is not required to
        // win them all.
        [Test]
        public void MixedWithNests_WinsAtLeastThreeInFive_AndEndsClearlyBetterOffThanMixed()
        {
            IReadOnlyList<PlaytestRun> nests = _runs[MixedWithNests];
            IReadOnlyList<PlaytestRun> mixed = _runs[Mixed];
            float nestsHives = AverageHiveShare(nests);
            float mixedHives = AverageHiveShare(mixed);

            Assert.GreaterOrEqual(PlaytestReport.Wins(nests), 3, Wins(MixedWithNests));
            string kept = $"hive health kept: {nestsHives:P0} with nests, {mixedHives:P0} without";
            Assert.Greater(nestsHives, mixedHives + ClearlyBetterHiveShare, kept);
            Assert.Greater(LowestLevel(nests), HighestLevel(mixed), "every match with nests ends at a higher level than any without");
        }

        // AttackOnly never heals, so its wins are left out: the target is for the plans that use both sinks.
        [TestCase(Mixed)]
        [TestCase(MixedWithNests)]
        public void Wins_SpendFifteenToFortyFivePercentOfTheirWaspsOnHealing(string plan)
        {
            foreach (PlaytestRun run in _runs[plan])
            {
                if (run.Won)
                {
                    string spent = $"{Describe(run)}: {run.SpentOnHealing} of {run.Spent} wasps healed";
                    Assert.That(run.HealingShare, Is.InRange(FewestHealingShare, MostHealingShare), spent);
                }
            }
        }

        [Test]
        public void Wins_LastSixToNineMinutesOfGameTime()
        {
            foreach (IReadOnlyList<PlaytestRun> runs in _runs.Values)
            {
                foreach (PlaytestRun run in runs)
                {
                    if (run.Won)
                    {
                        Assert.That(run.Seconds, Is.InRange(ShortestWinSeconds, LongestWinSeconds), Describe(run));
                    }
                }
            }
        }

        // Thirty seeds, to see how often each plan wins rather than whether five particular matches do. The rates leave room
        // for luck: what the five-seed tests demand of every five, this demands of the share.
        [Test]
        [Explicit("Plays 180 matches; run it by name after changing the economy.")]
        [Category("PlaytestSweep")]
        public void WideSweep_ThirtySeeds_HoldsTheRates()
        {
            Dictionary<string, IReadOnlyList<PlaytestRun>> runs = PlayAll(WideSeedCount);

            Assert.AreEqual(0, PlaytestReport.Wins(runs[Idle]), "Idle");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[StingOnly]), WideSeedCount / 5, "StingOnly");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[AttackOnly]), WideSeedCount * 2 / 5, "AttackOnly");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[HealOnly]), WideSeedCount * 2 / 5, "HealOnly");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[Mixed]), WideSeedCount * 2 / 5, "Mixed");
            Assert.GreaterOrEqual(PlaytestReport.Wins(runs[MixedWithNests]), WideSeedCount * 3 / 5, "MixedWithNests");
        }

        private static Dictionary<string, IReadOnlyList<PlaytestRun>> PlayAll(int seeds)
        {
            var byPlan = new Dictionary<string, IReadOnlyList<PlaytestRun>>();
            var table = new List<IReadOnlyList<PlaytestRun>>(Plans.Length);
            foreach (Func<IPlaytestPlan> makePlan in Plans)
            {
                var runs = new List<PlaytestRun>(seeds);
                for (uint seed = 1; seed <= seeds; seed++)
                {
                    using (var match = new PlaytestMatch(seed))
                    {
                        runs.Add(new PlaytestBot(match, seed).Play(makePlan(), MatchLimitSeconds));
                    }
                }

                byPlan[runs[0].Plan] = runs;
                table.Add(runs);
            }

            foreach (string line in PlaytestReport.Lines(table))
            {
                Debug.Log(line);
            }

            return byPlan;
        }

        private static float AverageHiveShare(IReadOnlyList<PlaytestRun> runs)
        {
            float sum = 0f;
            foreach (PlaytestRun run in runs)
            {
                sum += run.HiveHealthShare;
            }

            return sum / runs.Count;
        }

        private static int LowestLevel(IReadOnlyList<PlaytestRun> runs)
        {
            int lowest = int.MaxValue;
            foreach (PlaytestRun run in runs)
            {
                lowest = Math.Min(lowest, run.Level);
            }

            return lowest;
        }

        private static int HighestLevel(IReadOnlyList<PlaytestRun> runs)
        {
            int highest = 0;
            foreach (PlaytestRun run in runs)
            {
                highest = Math.Max(highest, run.Level);
            }

            return highest;
        }

        private string Wins(string plan)
        {
            return $"{plan} won {PlaytestReport.Wins(_runs[plan])} of {_runs[plan].Count}";
        }

        private static string Describe(PlaytestRun run)
        {
            string result = run.Won ? "won" : run.TimedOut ? "timed out" : "lost";
            return $"{run.Plan} seed {run.Seed}: {result} in round {run.Round} after {run.Seconds:F0} s";
        }
    }
}
