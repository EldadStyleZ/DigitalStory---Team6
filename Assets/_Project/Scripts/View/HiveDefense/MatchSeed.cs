using System;
using DigitalStory.Logic;

namespace DigitalStory.View
{
    /// <summary>
    /// The seed of each match's map (plan section S), chosen by <see cref="MatchSeedRule"/>: a fresh one each match; with
    /// "-f010Seed n" on the command line n for the first match and n + 1, n + 2 for each restart, for a demo that plays the
    /// same maps again; and while <see cref="Fixed"/> is set, that one every match, for tests.
    /// </summary>
    /// <remarks>
    /// Static because a restart loads the scene again, and the count of matches must outlive it. The command line is read
    /// once, at the first match.
    /// </remarks>
    public static class MatchSeed
    {
        /// <summary>
        /// The seed of the map the scene builder saves into the scene, so the Editor shows a real map before Play. Every match
        /// draws its own map over it when it starts. The PlayMode tests play another seed, whose map stands clearly elsewhere,
        /// so nothing left where the scene put it can pass them (plan section T).
        /// </summary>
        public const int SavedSceneSeed = 1010;

        /// <summary>The command-line option that names the first match's seed.</summary>
        public const string CommandLineOption = "-f010Seed";

        // Spreads the match count over the bits, so two matches started in the same clock tick still differ.
        private const int MatchMixer = 486187739;

        private static int s_matchesStarted;
        private static bool s_hasReadCommandLine;
        private static int? s_commandLineSeed;

        /// <summary>For tests: while set, every match draws its map from this seed. Set it back to null afterwards.</summary>
        public static int? Fixed { get; set; }

        /// <summary>Chooses the seed of the match that starts now, and counts the match.</summary>
        public static int Next()
        {
            if (!s_hasReadCommandLine)
            {
                s_hasReadCommandLine = true;
                if (MatchSeedRule.TryRead(Environment.GetCommandLineArgs(), CommandLineOption, out int seed))
                {
                    s_commandLineSeed = seed;
                }
            }

            int match = s_matchesStarted++;
            int fresh = unchecked((int)DateTime.UtcNow.Ticks ^ (match * MatchMixer));
            return MatchSeedRule.Choose(Fixed, s_commandLineSeed, match, fresh);
        }
    }
}
