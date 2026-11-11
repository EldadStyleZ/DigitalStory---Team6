using DigitalStory.Logic;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Attacks with everything it can afford and nothing else: in a wave it flies at the nearest beekeeper, runs from sticks
    /// on the core, and casts the giant when Fury is full, then the Strike song that fits (<see cref="StrikeChoice"/>: the
    /// Meteor at a crowd, the Scythe at a pack in front, the Tornado when surrounded, the Lance at a file or a far smoker,
    /// the Hydra at several), spending the Meteor on anyone in reach and the Tornado on anyone close when nothing else fits,
    /// and the Sting otherwise. It keeps no wasps back, never heals, never raises a shield or a tower and never fetches a
    /// nest. Between waves it waits beside the hive with the fullest store, taking it, until the timer brings the wave.
    /// </summary>
    internal sealed class AttackOnlyPlan : IPlaytestPlan
    {
        // A stick this close and on the core is one to run from.
        private const float EvadeMetres = 3f;

        public string Name => "AttackOnly";

        public void Decide(PlaytestBot bot)
        {
            Act(bot);

            // Last, so it overrides only where the core flies.
            bot.StepOffNest();
        }

        private static void Act(PlaytestBot bot)
        {
            HiveDefenseSession session = bot.Session;
            if (session.IsColossusActive)
            {
                bot.FightAsGiant();
                return;
            }

            if (!bot.IsSurvive)
            {
                int richest = bot.RichestHive();
                if (richest >= 0)
                {
                    bot.FlyTo(bot.BesideHive(richest));
                }

                return;
            }

            BeekeeperBrain target = bot.NearestBeekeeper(bot.Core);
            if (target == null)
            {
                return;
            }

            if (!bot.Evade(EvadeMetres))
            {
                bot.Approach(target.Position, StrikeChoice.StandOff(bot, target, 0));
            }

            if (session.Fury.IsFull && bot.TryPlay(PlaytestMatch.ColossusIndex))
            {
                return;
            }

            // No reserve, and rich enough at any count to spend the big songs on a lone beekeeper too.
            if (StrikeChoice.TryBigStrike(bot, 0, 0))
            {
                return;
            }

            if (!bot.IsRunning(SongSlot.Strike) && session.AimTarget != null)
            {
                bot.TryPlay(PlaytestMatch.StingIndex);
            }
        }
    }
}
