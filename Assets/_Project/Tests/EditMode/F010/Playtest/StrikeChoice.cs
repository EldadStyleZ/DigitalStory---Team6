using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// How the attacking plans choose a big Strike song, each for the map problem plan section A gives it: the Meteor for a
    /// crowd further off, the Scythe for a pack in front, the Tornado when surrounded, the Lance for a file or a far smoker,
    /// and the Hydra for several spread out. Every shape is measured at the swarm's level, as the song would strike, and
    /// needs two beekeepers or more, except the far smoker. The plans count on several stings too (plan section O): one
    /// Sting sends one sting for every <see cref="StingSong.WaspsPerSting"/> living wasps, so two beekeepers or more in its
    /// reach are a shape for it, each with its own sting, as long as it lasts. Of the songs whose shape is there, that are
    /// ready and that the plan can pay for while keeping <c>reserve</c> wasps, it plays the one that would deal the most
    /// damage (each beekeeper it reaches, times its damage; the Hydra reaches one a head; the Sting deals its cast's damage,
    /// shared by its stings, so it seldom beats a big song that fits), the first in
    /// that order on a tie, the Sting last. A plan rich enough (<c>richInCasts</c> more casts above its reserve) also spends
    /// the Meteor on one beekeeper in its reach and the Tornado on one close by, once no shape is there. The Sting on a lone
    /// beekeeper, the cheap move for everything else, is the plan's own call. With the Scythe ready, a plan steps in on a
    /// pack (<see cref="StandOff"/>), since the blade reaches less far than a player stands.
    /// </summary>
    internal static class StrikeChoice
    {
        // How many beekeepers make each shape: within the Meteor's radius of one another, in the Scythe's half-disc, close
        // around the core with one of them behind it, on one line of the Lance, and in the Hydra's reach.
        private const int Crowd = 2;
        private const int Pack = 2;
        private const int Surrounded = 2;
        private const int File = 2;
        private const int Several = 2;

        // How far inside the Scythe's reach a plan steps to cut a pack.
        private const float StepInsideMetres = 1f;

        /// <summary>
        /// Starts the big Strike song that fits the beekeepers around the core best, when one fits and the plan can pay for
        /// it, and no big Strike song is still on its way to land. True when it started one.
        /// </summary>
        public static bool TryBigStrike(PlaytestBot bot, int reserve, int richInCasts)
        {
            HiveDefenseSession session = bot.Session;
            if (IsBigStrikeRunning(bot))
            {
                return false;
            }

            PlaytestMatch match = bot.Match;
            StingSong sting = match.Sting;
            float size = session.SizeScale;
            MeteorSong meteor = match.Meteor;
            ScytheSong scythe = match.Scythe;
            TornadoSong tornado = match.Tornado;
            LanceSong lance = match.Lance;
            HydraSong hydra = match.Hydra;

            int crowd = bot.BiggestCrowd(meteor.MaxRange * size, meteor.Radius * size);
            BeekeeperBrain faced = bot.MostThreatening(scythe.FacingRange * size);
            int inFront = faced != null ? bot.CountInFront(faced.Position, scythe.Radius * size) : 0;
            int close = bot.CountWithin(bot.Core, tornado.Radius * size);
            int line = bot.BestLine(lance.Length * size, lance.Width * size);
            bool isFarSmoker = bot.IsSmokerBetween(match.Sting.MaxRange * size, lance.Length * size);
            int inReach = Mathf.Min(bot.CountWithin(bot.Core, hydra.Reach * size), hydra.HeadCount(session.Level));

            int best = -1;
            float most = 0f;
            Consider(bot, PlaytestMatch.MeteorIndex, crowd >= Crowd ? crowd * meteor.Damage : 0f, reserve, ref best, ref most);
            Consider(bot, PlaytestMatch.ScytheIndex, inFront >= Pack ? inFront * scythe.Damage : 0f, reserve, ref best, ref most);
            bool isSurrounded = close >= Surrounded && close > inFront;
            Consider(bot, PlaytestMatch.TornadoIndex, isSurrounded ? close * tornado.Damage : 0f, reserve, ref best, ref most);
            bool isLanceShape = line >= File || isFarSmoker;
            Consider(bot, PlaytestMatch.LanceIndex, isLanceShape ? Mathf.Max(line, 1) * lance.Damage : 0f, reserve, ref best, ref most);
            Consider(bot, PlaytestMatch.HydraIndex, inReach >= Several ? inReach * hydra.Damage : 0f, reserve, ref best, ref most);

            // A running Sting is not cut for another, and a lone beekeeper is the plan's own call. Its stings share the cast's
            // damage (the owner made it the weakest attack, 2026-11-14), so however many it sends it deals the same.
            int stung = Mathf.Min(sting.StingCount(session.Swarm.Count), bot.CountWithin(bot.Core, sting.MaxRange * size));
            float perCast = sting.DamagePerSecond * Mathf.Max(0f, sting.Seconds - sting.ArriveSeconds);
            bool isStingShape = stung >= Several && !bot.IsRunning(SongSlot.Strike);
            Consider(bot, PlaytestMatch.StingIndex, isStingShape ? perCast : 0f, reserve, ref best, ref most);
            if (best >= 0)
            {
                return bot.TryPlay(best);
            }

            if (crowd > 0 && IsRich(bot, PlaytestMatch.MeteorIndex, reserve, richInCasts) && TryPaid(bot, PlaytestMatch.MeteorIndex, reserve))
            {
                return true;
            }

            return close > 0 && IsRich(bot, PlaytestMatch.TornadoIndex, reserve, richInCasts) && TryPaid(bot, PlaytestMatch.TornadoIndex, reserve);
        }

        /// <summary>
        /// How close to <paramref name="target"/> the plan stands: the player's own stand-off, or, while the Scythe is ready
        /// and affordable and a pack stands within its reach of the target, inside the blade's reach.
        /// </summary>
        public static float StandOff(PlaytestBot bot, ISwarmTarget target, int reserve)
        {
            float reach = bot.Match.Scythe.Radius * bot.Session.SizeScale;
            bool isPack = bot.CountWithin(target.Position, reach) >= Pack;
            bool isScytheReady = bot.IsReady(PlaytestMatch.ScytheIndex) && CanSpare(bot, PlaytestMatch.ScytheIndex, reserve);
            return isPack && isScytheReady ? Mathf.Min(bot.StandOffMetres, reach - StepInsideMetres) : bot.StandOffMetres;
        }

        /// <summary>True while a Strike song other than the Sting is on its way to land: a new cast would cut it.</summary>
        public static bool IsBigStrikeRunning(PlaytestBot bot)
        {
            return bot.IsRunning(SongSlot.Strike) && bot.Session.Forms.Slot(SongSlot.Strike).Form != SwarmForm.Sting;
        }

        /// <summary>True when the swarm keeps <paramref name="reserve"/> wasps after paying what the song costs now.</summary>
        public static bool CanSpare(PlaytestBot bot, int song, int reserve)
        {
            return bot.Session.Swarm.Count - bot.Session.SongPrice(song) >= reserve;
        }

        // The song becomes the best when it would deal more than the best so far and the plan can play it now.
        private static void Consider(PlaytestBot bot, int song, float damage, int reserve, ref int best, ref float most)
        {
            if (damage > most && bot.IsReady(song) && CanSpare(bot, song, reserve) && bot.CanAfford(song))
            {
                best = song;
                most = damage;
            }
        }

        private static bool TryPaid(PlaytestBot bot, int song, int reserve)
        {
            return CanSpare(bot, song, reserve) && bot.TryPlay(song);
        }

        private static bool IsRich(PlaytestBot bot, int song, int reserve, int richInCasts)
        {
            return bot.Session.Swarm.Count - reserve >= bot.CostOf(song) * richInCasts;
        }
    }
}
