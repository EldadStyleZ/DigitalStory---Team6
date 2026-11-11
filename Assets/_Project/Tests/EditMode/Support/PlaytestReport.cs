using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Writes the playtest bot's results as a table: one line for each match, then one summary line for each plan and one
    /// line of where each plan's damage came from, every line starting with "[F010] bot:" so the log can be searched for them.
    /// </summary>
    internal static class PlaytestReport
    {
        private const string Prefix = "[F010] bot: ";

        private const string Header =
            "plan            seed result  rnd hives weakest  wasps/cap   songs towers  heal heal% twr% stores nests early  hits clp lvl casts  time  wave quiet";

        /// <summary>
        /// The table's lines for every run, plan by plan, followed by one summary line for each plan, then one line for each
        /// plan with the share of the damage each song dealt, in its wins (in all its matches when it won none).
        /// </summary>
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

            foreach (IReadOnlyList<PlaytestRun> runs in plans)
            {
                lines.Add(Prefix + DamageLine(runs));
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

        /// <summary>
        /// Each song's share, from 0 to 1, of all the damage dealt in <paramref name="runs"/> together, in the match's song
        /// order. All zeros when none was dealt.
        /// </summary>
        public static float[] DamageShares(IReadOnlyList<PlaytestRun> runs)
        {
            var shares = new float[runs[0].DamageBySong.Length];
            float total = 0f;
            foreach (PlaytestRun run in runs)
            {
                for (int song = 0; song < shares.Length; song++)
                {
                    shares[song] += run.DamageBySong[song];
                }

                total += run.DamageDealt;
            }

            for (int song = 0; total > 0f && song < shares.Length; song++)
            {
                shares[song] /= total;
            }

            return shares;
        }

        /// <summary>The runs that were won.</summary>
        public static IReadOnlyList<PlaytestRun> WonRuns(IReadOnlyList<PlaytestRun> runs)
        {
            var won = new List<PlaytestRun>();
            foreach (PlaytestRun run in runs)
            {
                if (run.Won)
                {
                    won.Add(run);
                }
            }

            return won;
        }

        private static string Row(PlaytestRun run)
        {
            string result = run.Won ? "won" : run.TimedOut ? "timeout" : "lost";
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0,-15} {1,4} {2,-7} {3,3} {4,4:P0} {5,7:P0} {6,5}/{7,-5} {8,5} {9,6} {10,5} {11,5:P0} {12,4:P0} {13,6} {14,5} "
                + "{15,5} {16,5} {17,3} {18,3} {19,5} {20,4:F0}s {21,4:F0}s {22,4:F1}s",
                run.Plan, run.Seed, result, run.Round, run.HiveHealthShare, run.WeakestHiveShare, run.WaspsLeft, run.Capacity,
                run.SpentOnSongs, run.SpentOnTowers, run.SpentOnHealing, run.HealingShare, run.TowerShare, run.EarnedFromStores,
                run.EarnedFromNests, run.EarnedFromEarlyCalls, run.LostToHits, run.Collapses, run.Level, run.SongsCast,
                run.Seconds, run.SurviveSeconds, run.LongestQuietSeconds);
        }

        // Every song that dealt any damage, the most first, with its casts; then any damage no song could be tied to.
        private static string DamageLine(IReadOnlyList<PlaytestRun> runs)
        {
            IReadOnlyList<PlaytestRun> won = WonRuns(runs);
            IReadOnlyList<PlaytestRun> measured = won.Count > 0 ? won : runs;
            float[] shares = DamageShares(measured);
            var casts = new int[shares.Length];
            float unattributed = 0f;
            float total = 0f;
            foreach (PlaytestRun run in measured)
            {
                for (int song = 0; song < casts.Length; song++)
                {
                    casts[song] += run.CastsBySong[song];
                }

                unattributed += run.UnattributedDamage;
                total += run.DamageDealt;
            }

            var order = new List<int>();
            for (int song = 0; song < shares.Length; song++)
            {
                order.Add(song);
            }

            order.Sort((a, b) => shares[b] != shares[a] ? shares[b].CompareTo(shares[a]) : a.CompareTo(b));
            var line = new StringBuilder();
            line.AppendFormat(
                CultureInfo.InvariantCulture, "damage  {0,-15} {1} ({2:F0} dealt):", runs[0].Plan, won.Count > 0 ? "in wins" : "in all", total);
            foreach (int song in order)
            {
                if (casts[song] > 0 || shares[song] > 0f)
                {
                    line.AppendFormat(CultureInfo.InvariantCulture, " {0} {1:P0} x{2},", runs[0].SongNames[song], shares[song], casts[song]);
                }
            }

            if (unattributed > 0f)
            {
                line.AppendFormat(CultureInfo.InvariantCulture, " untied {0:F0}", unattributed);
            }

            return line.ToString().TrimEnd(',');
        }

        private static string Summary(IReadOnlyList<PlaytestRun> runs)
        {
            var rounds = new StringBuilder();
            float healing = 0f;
            float towers = 0f;
            float seconds = 0f;
            float hives = 0f;
            int wins = Wins(runs);
            foreach (PlaytestRun run in runs)
            {
                rounds.Append(rounds.Length > 0 ? "," : string.Empty).Append(run.Won ? "W" : run.Round.ToString(CultureInfo.InvariantCulture));
                if (run.Won)
                {
                    healing += run.HealingShare;
                    towers += run.TowerShare;
                    seconds += run.Seconds;
                    hives += run.HiveHealthShare;
                }
            }

            string inWins = wins > 0
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "in wins: healing {0:P0}, towers {1:P0}, hives {2:P0}, {3:F0} s",
                    healing / wins,
                    towers / wins,
                    hives / wins,
                    seconds / wins)
                : "no wins";
            return string.Format(
                CultureInfo.InvariantCulture, "summary {0,-15} wins {1}/{2}  rounds {3}  {4}", runs[0].Plan, wins, runs.Count, rounds, inWins);
        }
    }
}
