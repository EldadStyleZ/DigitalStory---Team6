using System.Collections.Generic;
using System.Globalization;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Which seed a match's map is drawn from (plan section S). A fixed seed, as the tests set, wins. Otherwise a seed on the
    /// command line draws that map for the first match of the run and the next seed for each restart, so a demo plays the
    /// same maps in the same order every time. Otherwise each match takes a fresh seed.
    /// </summary>
    public static class MatchSeedRule
    {
        /// <summary>Chooses the seed of a match.</summary>
        /// <param name="fixedSeed">The seed every match uses, or null.</param>
        /// <param name="commandLineSeed">The seed given on the command line, or null.</param>
        /// <param name="matchIndex">How many matches this run played before this one: 0 for the first.</param>
        /// <param name="freshSeed">A seed nobody chose, such as one from the clock.</param>
        public static int Choose(int? fixedSeed, int? commandLineSeed, int matchIndex, int freshSeed)
        {
            if (fixedSeed.HasValue)
            {
                return fixedSeed.Value;
            }

            if (commandLineSeed.HasValue)
            {
                return unchecked(commandLineSeed.Value + matchIndex);
            }

            return freshSeed;
        }

        /// <summary>
        /// Reads the whole number that follows <paramref name="option"/> among <paramref name="arguments"/>. False, with a
        /// seed of 0, when the option is missing or no whole number follows it.
        /// </summary>
        public static bool TryRead(IReadOnlyList<string> arguments, string option, out int seed)
        {
            seed = 0;
            if (arguments == null || string.IsNullOrEmpty(option))
            {
                return false;
            }

            for (int index = 0; index < arguments.Count - 1; index++)
            {
                if (arguments[index] == option)
                {
                    return int.TryParse(arguments[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out seed);
                }
            }

            return false;
        }
    }
}
