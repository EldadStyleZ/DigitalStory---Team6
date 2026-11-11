using DigitalStory.Logic;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Knows one move: it stands beside the hive under the most threat and casts the Sting whenever it can, which is when
    /// the Strike slot is free and a beekeeper is in reach. It never heals, never calls a wave and never fetches a nest; it
    /// takes a store only because it stands beside a hive.
    /// </summary>
    internal sealed class StingOnlyPlan : IPlaytestPlan
    {
        // Beekeepers within this many metres of a hive count as threatening it.
        private const float ThreatMetres = 8f;

        public string Name => "StingOnly";

        public void Decide(PlaytestBot bot)
        {
            int hive = bot.MostThreatenedHive(ThreatMetres);
            BeekeeperBrain nearest = bot.NearestBeekeeper(bot.Core);
            if (hive < 0 && nearest != null)
            {
                bot.Session.Hives.TryGetNearestStanding(nearest.Position, out hive);
            }

            if (hive < 0)
            {
                hive = bot.RichestHive();
            }

            if (hive >= 0)
            {
                bot.FlyTo(bot.BesideHive(hive));
            }

            if (bot.IsSurvive && bot.Session.AimTarget != null && !bot.IsRunning(SongSlot.Strike))
            {
                bot.TryPlay(PlaytestMatch.StingIndex);
            }
        }
    }
}
