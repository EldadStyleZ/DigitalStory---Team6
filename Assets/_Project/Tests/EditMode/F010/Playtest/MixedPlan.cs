using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plays as a careful player who never fetches a nest. Sticks go for the hives and turn on the core only when a song hurt
    /// them (plan section O), so in a wave it guards where they hit: it stands beside the weaker of the hives under threat
    /// and fights from there, and while no hive is threatened it flies at the aimed beekeeper on its way in. It raises the
    /// Dome at the hive it guards while that hive is being hit, since the Dome shields what is inside it, and on the core when
    /// a stick it provoked swings at it; without the Dome it runs from such a stick, which follows it off the hive. It throws
    /// a pack off the hive it guards with the Tornado, or with the cheaper Gust while the Tornado cannot be played, and blows
    /// smoke off the core with the Gust. It attacks with the giant when Fury is full, then the Strike song that would deal
    /// the most (<see cref="StrikeChoice"/>: several stings, one for every 400 wasps, the Meteor at a crowd, the Scythe at a
    /// pack in front, the Tornado when it is surrounded, the Lance at a file or a far smoker, the Hydra at several spread out)
    /// and the Sting otherwise, keeping a reserve of wasps; with plenty to spare above it, it spends the Meteor and the
    /// Tornado on a lone beekeeper too. It calls the stores home with Hive Call when the swarm runs low and nobody is near.
    /// It keeps the one Post slot busy: before each wave it leaves a Nest on the lane the wave's first beekeeper walks (when
    /// it calls the wave, or 12 s before the farm phase ends if it never got ready to call), and
    /// in the wave, once that nest has gone off, it shells the hive it leaves with the Honeycomb (the threatened hive it is
    /// not guarding, once auto-aim would shell it), or lobs a Nest onto a pack. Between waves it heals a hive that has
    /// stopped making wasps just past the line where it makes them again, spending almost all it has since the income pays
    /// it back, then a hive below 60% to full as far as its reserve allows. It takes the stores in, and calls the wave once
    /// the bonus for calling early would fill the swarm and the stores are banked again, waiting beside the fullest store
    /// until then.
    /// </summary>
    internal sealed class MixedPlan : IPlaytestPlan
    {
        // The big songs are only paid while the swarm keeps this share of its capacity after paying. With wasps to spare
        // for this many more casts above that reserve, it spends them on a lone beekeeper too, not only on a crowd.
        private const float ReserveShare = 0.4f;
        private const int RichInCasts = 6;

        // Between waves: a hive that has stopped making wasps is healed first, the cheapest first, to RestartMargin past the
        // line where it makes them again, spending down to RestartReserveShare of the capacity, since the income pays it
        // back; then a hive below HealBelowShare of its health is healed to full, as far as the reserve allows.
        private const float RestartMargin = 0.05f;
        private const float RestartReserveShare = 0.15f;
        private const float HealBelowShare = 0.6f;

        // In a wave, Hive Call is played below this share of the capacity, with no beekeeper within SafeMetres.
        private const float RefillBelowShare = 0.35f;
        private const float SafeMetres = 10f;

        // Fewest wasps in the stores worth a Hive Call between waves, and how full each producing store must be before it
        // calls the wave, so the wave's Hive Calls have wasps to bring.
        private const float WorthCallingWasps = 40f;
        private const float BankedShare = 0.9f;

        // A stick this close and on the core is the moment for the Dome, or, while the Dome cannot rise, to run.
        private const float DomeMetres = 3f;

        // Beekeepers within this many metres of a hive's middle are on it, swinging at it (a stick swings from 1.8 m). This
        // many on the hive it guards make a pack worth throwing off.
        private const float OnHiveMetres = 2.5f;
        private const int HivePack = 2;

        // The nest left before a wave lies this many metres out from the hive along the first beekeeper's lane, where the
        // beekeeper walks through it on its way in. In a wave, a nest is lobbed onto this many beekeepers or more.
        private const float LaneNestMetres = 6f;
        private const int NestPack = 2;

        // A farm phase it never got ready to call (the swarm did not fill) still ends in a wave: this many seconds before, it
        // leaves the lane's nest anyway, time enough to fly within the Nest's reach and play it.
        private const float LastNestSeconds = 12f;

        // Beekeepers within this many metres of a hive threaten it.
        private const float ThreatMetres = 8f;

        private const int NoHive = -1;

        private readonly bool _avoidsNests;

        // The hive being healed and the share of its health to heal it to, kept until it gets there so the plan does not
        // flit between two hurt hives.
        private int _healing = NoHive;
        private float _healTo;

        // Once ready for the wave it stays ready for the rest of the farm phase, so leaving the store to lay the lane's
        // nest does not make it wait there again.
        private bool _isReadyForWave;

        /// <summary>
        /// Makes the plan. Played on its own it never collects a nest, so it steps off one it strays onto;
        /// <paramref name="avoidsNests"/> false leaves that to the plan it serves.
        /// </summary>
        public MixedPlan(bool avoidsNests = true)
        {
            _avoidsNests = avoidsNests;
        }

        public string Name => "Mixed";

        public void Decide(PlaytestBot bot)
        {
            if (bot.Session.IsColossusActive)
            {
                bot.FightAsGiant();
            }
            else if (bot.IsSurvive)
            {
                _healing = NoHive;
                _isReadyForWave = false;
                Survive(bot);
            }
            else
            {
                Farm(bot);
            }

            // Last, so it overrides only where the core flies.
            if (_avoidsNests)
            {
                bot.StepOffNest();
            }
        }

        private void Farm(PlaytestBot bot)
        {
            HiveDefenseSession session = bot.Session;
            SwarmBody swarm = session.Swarm;
            if (_healing != NoHive && (!session.Hives[_healing].IsDamaged || HealthShare(session.Hives[_healing]) >= _healTo))
            {
                _healing = NoHive;
            }

            if (_healing == NoHive)
            {
                ChooseHiveToHeal(bot);
            }

            float reserve = _healTo < 1f ? RestartReserveShare : ReserveShare;
            if (_healing != NoHive && swarm.Count <= swarm.Capacity * reserve)
            {
                _healing = NoHive;
            }

            if (_healing != NoHive)
            {
                bot.FlyTo(bot.BesideHive(_healing));
                if (session.NearHive == _healing)
                {
                    bot.HoldGive();
                }

                return;
            }

            if (session.IsCoreRooted)
            {
                return;
            }

            if (swarm.Room >= 1f && bot.StoredWasps() >= WorthCallingWasps)
            {
                bot.TryPlay(PlaytestMatch.HiveCallIndex);
                return;
            }

            // Ready once calling early would fill the swarm (the bonus for the seconds skipped covers the room left) and the
            // stores are banked for the wave's Hive Calls. Until then it waits beside the fullest store, which keeps filling.
            _isReadyForWave |= swarm.Room <= session.FarmSecondsLeft * bot.Match.Waves.EarlyBonusPerSecond && AreStoresBanked(bot);
            if (_isReadyForWave)
            {
                if (!LeaveLaneNest(bot))
                {
                    bot.CallWave();
                }

                return;
            }

            // Not ready, but the wave comes soon all the same: the lane still gets its nest before it does.
            if (session.FarmSecondsLeft <= LastNestSeconds && LeaveLaneNest(bot))
            {
                return;
            }

            int richest = bot.RichestHive();
            if (richest >= 0)
            {
                bot.FlyTo(bot.BesideHive(richest));
            }
        }

        private static void Survive(PlaytestBot bot)
        {
            HiveDefenseSession session = bot.Session;
            SwarmBody swarm = session.Swarm;
            int guarded = WeakestHiveUnderThreat(bot);
            bool isDomeTime = IsDomeOnHiveUnderAttack(bot, guarded) || bot.IsCoreUnderAttack(DomeMetres);
            if (isDomeTime && !bot.IsRunning(SongSlot.Guard) && bot.TryPlay(PlaytestMatch.DomeIndex))
            {
                return;
            }

            if (session.Smoke.IsCoreInSmoke && bot.TryPlay(PlaytestMatch.GustIndex))
            {
                return;
            }

            if (PlaceTower(bot))
            {
                return;
            }

            if (guarded != NoHive && ThrowPackOffHive(bot, guarded))
            {
                return;
            }

            // Under the Dome it stands and fights; without it, it runs from a stick it provoked, which follows it off the
            // hive, and stings as it goes.
            if (!session.Forms.IsDomed && bot.Evade(DomeMetres))
            {
                Attack(bot);
                return;
            }

            bool isSafe = bot.CountWithin(bot.Core, SafeMetres) == 0;
            if (session.IsCoreRooted && isSafe)
            {
                return;
            }

            if (isSafe && swarm.Count < swarm.Capacity * RefillBelowShare && bot.StoredWasps() >= WorthCallingWasps
                && bot.TryPlay(PlaytestMatch.HiveCallIndex))
            {
                return;
            }

            // It guards where the sticks hit: beside the weaker of the hives under threat, fighting from there.
            if (guarded != NoHive)
            {
                bot.FlyTo(bot.BesideHive(guarded));
                Attack(bot);
                return;
            }

            ISwarmTarget target = session.AimTarget ?? bot.NearestBeekeeper(bot.Core);
            if (target == null)
            {
                return;
            }

            bot.Approach(target.Position, StrikeChoice.StandOff(bot, target, Reserve(bot)));
            Attack(bot);
        }

        // The Dome shields what is inside it, hives included: the moment is a hive being hit whose middle the Dome would cover.
        private static bool IsDomeOnHiveUnderAttack(PlaytestBot bot, int hive)
        {
            if (hive == NoHive || !bot.Session.Hives[hive].IsUnderAttack)
            {
                return false;
            }

            float radius = bot.Match.Dome.Radius * bot.Session.SizeScale;
            Vector3 offset = bot.Session.Hives[hive].Position - bot.Core;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        // A pack swinging at the hive it guards is thrown off: by the Tornado, which hurts and throws every beekeeper in its
        // reach, or by the Gust, which only shoves and stuns but costs a third, while the Tornado cannot be played. Only from
        // beside the hive, where the throw carries them away from it.
        private static bool ThrowPackOffHive(PlaytestBot bot, int hive)
        {
            HiveDefenseSession session = bot.Session;
            Vector3 hivePosition = session.Hives[hive].Position;
            if (session.NearHive != hive || bot.CountWithin(hivePosition, OnHiveMetres) < HivePack)
            {
                return false;
            }

            int reserve = Reserve(bot);
            float size = session.SizeScale;
            bool isTornadoReach = !StrikeChoice.IsBigStrikeRunning(bot)
                && bot.CountWithin(bot.Core, bot.Match.Tornado.Radius * size) >= HivePack;
            if (isTornadoReach && bot.IsReady(PlaytestMatch.TornadoIndex) && StrikeChoice.CanSpare(bot, PlaytestMatch.TornadoIndex, reserve)
                && bot.TryPlay(PlaytestMatch.TornadoIndex))
            {
                return true;
            }

            bool isGustReach = bot.CountWithin(bot.Core, bot.Match.Gust.PushRadius * size) >= HivePack;
            return isGustReach && StrikeChoice.CanSpare(bot, PlaytestMatch.GustIndex, reserve) && bot.TryPlay(PlaytestMatch.GustIndex);
        }

        private static void Attack(PlaytestBot bot)
        {
            HiveDefenseSession session = bot.Session;
            if (session.Fury.IsFull && session.ActiveBeekeeperCount > 1 && bot.TryPlay(PlaytestMatch.ColossusIndex))
            {
                return;
            }

            if (StrikeChoice.TryBigStrike(bot, Reserve(bot), RichInCasts))
            {
                return;
            }

            if (!bot.IsRunning(SongSlot.Strike) && session.AimTarget != null)
            {
                bot.TryPlay(PlaytestMatch.StingIndex);
            }
        }

        // Before the wave: a nest on the lane its first beekeeper walks, while the Post slot is free and the nest is ready and
        // affordable; a nest the last wave left unset still lies armed, so it stays. True while it is still flying there or
        // playing it, so the wave waits for it.
        private static bool LeaveLaneNest(PlaytestBot bot)
        {
            if (bot.IsTyping)
            {
                return true;
            }

            if (bot.IsRunning(SongSlot.Post) || !bot.IsReady(PlaytestMatch.NestIndex)
                || !StrikeChoice.CanSpare(bot, PlaytestMatch.NestIndex, Reserve(bot)))
            {
                return false;
            }

            bot.TryPlayAhead(PlaytestMatch.NestIndex, bot.LaneSpot(LaneNestMetres, out _));
            return true;
        }

        // In a wave, while the Post slot is free: the Honeycomb on the hive it leaves, the threatened hive it is not guarding,
        // once auto-aim would shell that one; otherwise a nest lobbed onto a pack.
        private static bool PlaceTower(PlaytestBot bot)
        {
            if (bot.IsRunning(SongSlot.Post))
            {
                return false;
            }

            HiveDefenseSession session = bot.Session;
            int reserve = Reserve(bot);
            int shell = bot.ShellHive();
            bool isLeft = shell >= 0 && shell != WeakestHiveUnderThreat(bot) && IsThreatened(bot, shell);
            if (isLeft && StrikeChoice.CanSpare(bot, PlaytestMatch.HoneycombIndex, reserve) && bot.TryPlay(PlaytestMatch.HoneycombIndex))
            {
                return true;
            }

            NestSong nest = bot.Match.Nest;
            float size = session.SizeScale;
            return bot.BiggestCrowd(nest.MaxRange * size, nest.BlastRadius * size) >= NestPack
                && StrikeChoice.CanSpare(bot, PlaytestMatch.NestIndex, reserve)
                && bot.TryPlay(PlaytestMatch.NestIndex);
        }

        private void ChooseHiveToHeal(PlaytestBot bot)
        {
            HiveSet hives = bot.Session.Hives;
            _healing = NoHive;
            for (int hive = 0; hive < hives.Count; hive++)
            {
                bool isStopped = !hives[hive].IsDestroyed && !hives[hive].IsProducing;
                if (isStopped && (_healing == NoHive || hives[hive].Health > hives[_healing].Health))
                {
                    _healing = hive;
                }
            }

            if (_healing != NoHive)
            {
                _healTo = bot.Match.Hive.ProductionHealthFraction + RestartMargin;
                return;
            }

            _healing = bot.MostHurtHive(HealBelowShare);
            _healTo = 1f;
        }

        private static int WeakestHiveUnderThreat(PlaytestBot bot)
        {
            HiveSet hives = bot.Session.Hives;
            int weakest = NoHive;
            for (int hive = 0; hive < hives.Count; hive++)
            {
                if (IsThreatened(bot, hive) && (weakest == NoHive || HealthShare(hives[hive]) < HealthShare(hives[weakest])))
                {
                    weakest = hive;
                }
            }

            return weakest;
        }

        private static bool IsThreatened(PlaytestBot bot, int hive)
        {
            Hive candidate = bot.Session.Hives[hive];
            return !candidate.IsDestroyed && bot.CountWithin(candidate.Position, ThreatMetres) > 0;
        }

        private static float HealthShare(Hive hive)
        {
            return hive.Health / hive.MaxHealth;
        }

        private static bool AreStoresBanked(PlaytestBot bot)
        {
            HiveSet hives = bot.Session.Hives;
            for (int hive = 0; hive < hives.Count; hive++)
            {
                if (hives[hive].IsProducing && hives[hive].Store < hives[hive].StoreCapacity * BankedShare)
                {
                    return false;
                }
            }

            return true;
        }

        // The wasps the big songs and the towers leave behind: a share of the capacity.
        private static int Reserve(PlaytestBot bot)
        {
            return Mathf.CeilToInt(bot.Session.Swarm.Capacity * ReserveShare);
        }
    }
}
