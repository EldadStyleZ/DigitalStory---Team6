using DigitalStory.Logic;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Defends with the two towers and healing only. It keeps the one Post slot busy: in a farm phase it leaves a Nest on
    /// the lane the next beekeeper walks, once the timer is nearly out; in a wave it shells the hive under attack with the
    /// Honeycomb (the hive auto-aim chooses, which is the one hit most recently) and otherwise lobs a Nest onto beekeepers in
    /// reach. Otherwise it heals the most hurt hive while it has wasps to spare, and waits beside the hive with the fullest
    /// store, taking it. It never casts a Strike song or the giant, never calls a wave early and never fetches a nest.
    /// </summary>
    internal sealed class TowersOnlyPlan : IPlaytestPlan
    {
        // Every hurt hive is worth healing to this plan.
        private const float HealBelowShare = 1f;

        // The lane's nest lies this far out from the hive; it is laid with this many seconds of the farm phase left, time
        // to fly there from the far hive.
        private const float LaneNestMetres = 6f;
        private const float LayNestSecondsLeft = 12f;

        public string Name => "TowersOnly";

        public void Decide(PlaytestBot bot)
        {
            if (PlaceTower(bot))
            {
                return;
            }

            int hurt = bot.MostHurtHive(HealBelowShare);
            if (hurt >= 0 && bot.Session.Swarm.Spendable > 0)
            {
                bot.FlyTo(bot.BesideHive(hurt));
                if (bot.Session.NearHive == hurt)
                {
                    bot.HoldGive();
                }
            }
            else
            {
                int richest = bot.RichestHive();
                if (richest >= 0)
                {
                    bot.FlyTo(bot.BesideHive(richest));
                }
            }

            // Last, so it overrides only where the core flies.
            bot.StepOffNest();
        }

        // True while it is placing a tower: flying to the lane, or playing a tower's notes.
        private static bool PlaceTower(PlaytestBot bot)
        {
            if (bot.IsTyping)
            {
                return true;
            }

            if (bot.IsRunning(SongSlot.Post) || bot.Session.IsColossusActive)
            {
                return false;
            }

            HiveDefenseSession session = bot.Session;
            if (!bot.IsSurvive)
            {
                if (session.FarmSecondsLeft > LayNestSecondsLeft || !bot.IsReady(PlaytestMatch.NestIndex)
                    || !bot.CanAfford(PlaytestMatch.NestIndex))
                {
                    return false;
                }

                bot.TryPlayAhead(PlaytestMatch.NestIndex, bot.LaneSpot(LaneNestMetres, out _));
                return true;
            }

            int shell = bot.ShellHive();
            if (shell >= 0 && session.Hives[shell].IsUnderAttack && bot.TryPlay(PlaytestMatch.HoneycombIndex))
            {
                return true;
            }

            NestSong nest = bot.Match.Nest;
            return bot.CountWithin(bot.Core, nest.MaxRange * session.SizeScale) > 0 && bot.TryPlay(PlaytestMatch.NestIndex);
        }
    }
}
