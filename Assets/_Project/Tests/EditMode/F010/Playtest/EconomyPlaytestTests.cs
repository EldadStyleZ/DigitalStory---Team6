using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The lecturer's "balanced economy with real trade-offs" (plan sections H, L, M and O), checked by the playtest bot.
    /// Once for the class it plays whole matches with each plan on five seeds at the code defaults, prints the results table
    /// to the log (lines starting "[F010] bot:"), and each test asserts one target on those results. The owner's two are
    /// section H's "lost without nests, winnable with them" and section M's "none of the three always wins and doing
    /// nothing always loses": no plan without nests wins more than two matches in five, and the player who fetches the
    /// nests wins at least four and ends clearly better off. The bot also checks that every sink of wasps matters (healing
    /// and the towers), that a won match lasts six to nine minutes, that no one song deals most of the damage in the wins,
    /// and that the giant stays under 45% of it. <see cref="WideSweep_ThirtySeeds_HoldsTheRates"/> plays thirty seeds; it
    /// is explicit, so only a person asks for it.
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
        private const float FewestTowerShare = 0.08f;
        private const float MostDamageShare = 0.5f;

        // Plan section O: the giant was near half the damage, so it must stay clearly below it.
        private const float MostColossusShare = 0.45f;
        private const float ShortestWinSeconds = 6f * 60f;
        private const float LongestWinSeconds = 9f * 60f;

        private const string Idle = "Idle";
        private const string StingOnly = "StingOnly";
        private const string AttackOnly = "AttackOnly";
        private const string HealOnly = "HealOnly";
        private const string TowersOnly = "TowersOnly";
        private const string Mixed = "Mixed";
        private const string MixedWithNests = "MixedWithNests";

        // A plan may remember things during a match, so each match gets a new one.
        private static readonly Func<IPlaytestPlan>[] Plans =
        {
            () => new IdlePlan(), () => new StingOnlyPlan(), () => new AttackOnlyPlan(), () => new HealOnlyPlan(),
            () => new TowersOnlyPlan(), () => new MixedPlan(), () => new MixedWithNestsPlan(),
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

        // No pure plan is enough: one Sting, however many stings it sends, and attacking without healing let the hives fall,
        // and healing, with or without the towers, never ends a wave; since plan section O the sticks walk past the core to
        // the hives, so the healer cannot hold them off either.
        [TestCase(StingOnly)]
        [TestCase(AttackOnly)]
        [TestCase(HealOnly)]
        [TestCase(TowersOnly)]
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

        // Section H: "winnable with them". The same player who fetches the nests wins nearly every match, but is not
        // required to win them all.
        [Test]
        public void MixedWithNests_WinsAtLeastFourInFive_AndEndsClearlyBetterOffThanMixed()
        {
            IReadOnlyList<PlaytestRun> nests = _runs[MixedWithNests];
            IReadOnlyList<PlaytestRun> mixed = _runs[Mixed];
            float nestsHives = AverageHiveShare(nests);
            float mixedHives = AverageHiveShare(mixed);

            Assert.GreaterOrEqual(PlaytestReport.Wins(nests), 4, Wins(MixedWithNests));
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

        // A tower stations real wasps, and the plans that use every sink spend a real share on the towers too.
        [TestCase(Mixed)]
        [TestCase(MixedWithNests)]
        public void Wins_SpendAtLeastEightPercentOfTheirWaspsOnTowers(string plan)
        {
            foreach (PlaytestRun run in _runs[plan])
            {
                if (run.Won)
                {
                    string spent = $"{Describe(run)}: {run.SpentOnTowers} of {run.Spent} wasps on towers";
                    Assert.GreaterOrEqual(run.TowerShare, FewestTowerShare, spent);
                }
            }
        }

        // The songs differ, so none carries the wins: across every match won, no one song (the giant included) deals more
        // than half the damage, and the giant less than 45%.
        [Test]
        public void Wins_NoSongDealsMoreThanHalfTheDamage_AndTheGiantUnderFortyFivePercent()
        {
            AssertNoSongCarriesTheWins(_runs);
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
        [Explicit("Plays 210 matches; run it by name after changing the economy.")]
        [Category("PlaytestSweep")]
        public void WideSweep_ThirtySeeds_HoldsTheRates()
        {
            Dictionary<string, IReadOnlyList<PlaytestRun>> runs = PlayAll(WideSeedCount);

            Assert.AreEqual(0, PlaytestReport.Wins(runs[Idle]), "Idle");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[StingOnly]), WideSeedCount * 2 / 5, "StingOnly");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[AttackOnly]), WideSeedCount * 2 / 5, "AttackOnly");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[HealOnly]), WideSeedCount * 2 / 5, "HealOnly");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[TowersOnly]), WideSeedCount * 2 / 5, "TowersOnly");
            Assert.LessOrEqual(PlaytestReport.Wins(runs[Mixed]), WideSeedCount * 2 / 5, "Mixed");
            Assert.GreaterOrEqual(PlaytestReport.Wins(runs[MixedWithNests]), WideSeedCount * 4 / 5, "MixedWithNests");
            AssertNoSongCarriesTheWins(runs);
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

        private static void AssertNoSongCarriesTheWins(Dictionary<string, IReadOnlyList<PlaytestRun>> byPlan)
        {
            var wins = new List<PlaytestRun>();
            foreach (IReadOnlyList<PlaytestRun> runs in byPlan.Values)
            {
                wins.AddRange(PlaytestReport.WonRuns(runs));
            }

            Assert.IsNotEmpty(wins, "no match was won");
            float[] shares = PlaytestReport.DamageShares(wins);
            for (int song = 0; song < shares.Length; song++)
            {
                Assert.LessOrEqual(shares[song], MostDamageShare, $"{wins[0].SongNames[song]} dealt {shares[song]:P1} of the damage in the wins");
            }

            float giant = shares[PlaytestMatch.ColossusIndex];
            Assert.Less(giant, MostColossusShare, $"the giant dealt {giant:P1} of the damage in the wins");
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
