using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Writes the playtest bot's results as a table: one line for each match, then one summary line for each plan, every
    /// line starting with "[F010] bot:" so the log can be searched for them.
    /// </summary>
    internal static class PlaytestReport
    {
        private const string Prefix = "[F010] bot: ";

        private const string Header =
            "plan            seed result  rnd hives weakest  wasps/cap   songs  heal heal% stores nests early  hits clp lvl casts  time  wave quiet";

        /// <summary>The table's lines for every run, plan by plan, followed by one summary line for each plan.</summary>
        public static IReadOnlyList<string> Lines(IReadOnlyList<IReadOnlyList<PlaytestRun>> plans)
        {
            var lines = new List<string> { Prefix + Header };
            foreach (IReadOnlyList<PlaytestRun> runs in plans)
            {
                foreach (PlaytestRun run in runs)
                {
                    lines.Add(Prefix + Row(run));
                }
            }

            foreach (IReadOnlyList<PlaytestRun> runs in plans)
            {
                lines.Add(Prefix + Summary(runs));
            }

            return lines;
        }

        /// <summary>How many of the runs were won.</summary>
        public static int Wins(IReadOnlyList<PlaytestRun> runs)
        {
            int wins = 0;
            foreach (PlaytestRun run in runs)
            {
                if (run.Won)
                {
                    wins++;
                }
            }

            return wins;
        }

        private static string Row(PlaytestRun run)
        {
            string result = run.Won ? "won" : run.TimedOut ? "timeout" : "lost";
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0,-15} {1,4} {2,-7} {3,3} {4,4:P0} {5,7:P0} {6,5}/{7,-5} {8,5} {9,5} {10,5:P0} {11,6} {12,5} {13,5} {14,5} "
                + "{15,3} {16,3} {17,5} {18,4:F0}s {19,4:F0}s {20,4:F1}s",
                run.Plan, run.Seed, result, run.Round, run.HiveHealthShare, run.WeakestHiveShare, run.WaspsLeft, run.Capacity,
                run.SpentOnSongs, run.SpentOnHealing, run.HealingShare, run.EarnedFromStores, run.EarnedFromNests,
                run.EarnedFromEarlyCalls, run.LostToHits, run.Collapses, run.Level, run.SongsCast, run.Seconds,
                run.SurviveSeconds, run.LongestQuietSeconds);
        }

        private static string Summary(IReadOnlyList<PlaytestRun> runs)
        {
            var rounds = new StringBuilder();
            float healing = 0f;
            float seconds = 0f;
            float hives = 0f;
            int wins = Wins(runs);
            foreach (PlaytestRun run in runs)
            {
                rounds.Append(rounds.Length > 0 ? "," : string.Empty).Append(run.Won ? "W" : run.Round.ToString(CultureInfo.InvariantCulture));
                if (run.Won)
                {
                    healing += run.HealingShare;
                    seconds += run.Seconds;
                    hives += run.HiveHealthShare;
                }
            }

            string inWins = wins > 0
                ? string.Format(
                    CultureInfo.InvariantCulture, "in wins: healing {0:P0}, hives {1:P0}, {2:F0} s", healing / wins, hives / wins, seconds / wins)
                : "no wins";
            return string.Format(
                CultureInfo.InvariantCulture, "summary {0,-15} wins {1}/{2}  rounds {3}  {4}", runs[0].Plan, wins, runs.Count, rounds, inWins);
        }
    }
}
