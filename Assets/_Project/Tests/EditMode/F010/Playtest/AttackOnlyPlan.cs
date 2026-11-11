using DigitalStory.Logic;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Attacks with everything it can afford and nothing else: in a wave it flies at the nearest beekeeper, runs from sticks
    /// on the core, and casts the giant when Fury is full, the Meteor at anyone in reach, the Tornado at anyone close and
    /// the Sting otherwise. It keeps no wasps back, never heals, never raises a shield and never fetches a nest. Between
    /// waves it waits beside the hive with the fullest store, taking it, until the timer brings the wave.
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
                bot.Approach(target.Position, bot.StandOffMetres);
            }

            if (session.Fury.IsFull && bot.TryPlay(PlaytestMatch.ColossusIndex))
            {
                return;
            }

            float size = session.SizeScale;
            bool isBigStrikeRunning = bot.IsRunning(SongSlot.Strike) && session.Forms.Slot(SongSlot.Strike).Form != SwarmForm.Sting;
            if (!isBigStrikeRunning && bot.CountWithin(bot.Core, bot.Match.Meteor.MaxRange * size) > 0
                && bot.TryPlay(PlaytestMatch.MeteorIndex))
            {
                return;
            }

            if (!isBigStrikeRunning && bot.CountWithin(bot.Core, bot.Match.Tornado.Radius * size) > 0
                && bot.TryPlay(PlaytestMatch.TornadoIndex))
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
