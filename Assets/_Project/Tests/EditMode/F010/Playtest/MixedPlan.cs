using DigitalStory.Logic;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// Plays as a careful player who never fetches a nest. In a wave it guards the weaker of the hives under attack, flying
    /// at the beekeeper nearest it (or at the aimed beekeeper while no hive is threatened), and attacks: the giant when Fury
    /// is full, the Meteor at a crowd, the Tornado when it is surrounded and the Sting otherwise, keeping a reserve of wasps;
    /// with plenty to spare above it, it spends the big songs on a lone beekeeper too. It raises the Dome when sticks are on
    /// the core and runs from them while the Dome cannot rise, blows smoke off the core with the Gust, and calls the stores
    /// home with Hive Call when the swarm runs low and nobody is near. Between waves it heals a hive that has stopped making
    /// wasps just past the line where it makes them again, spending almost all it has since the income pays it back, then a
    /// hive below 60% to full as far as its reserve allows. It takes the stores in, and calls the wave once the bonus for
    /// calling early would fill the swarm and the stores are banked again, waiting beside the fullest store until then.
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

        // Beekeepers within this many metres of a hive threaten it.
        private const float ThreatMetres = 8f;

        private const int NoHive = -1;

        private readonly bool _avoidsNests;

        // The hive being healed and the share of its health to heal it to, kept until it gets there so the plan does not
        // flit between two hurt hives.
        private int _healing = NoHive;
        private float _healTo;

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
            if (swarm.Room <= session.FarmSecondsLeft * bot.Match.Waves.EarlyBonusPerSecond && AreStoresBanked(bot))
            {
                bot.CallWave();
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
            if (bot.IsCoreUnderAttack(DomeMetres) && !bot.IsRunning(SongSlot.Guard) && bot.TryPlay(PlaytestMatch.DomeIndex))
            {
                return;
            }

            if (session.Smoke.IsCoreInSmoke && bot.TryPlay(PlaytestMatch.GustIndex))
            {
                return;
            }

            // Under the Dome it stands and fights; without it, it runs from the sticks and stings as it goes.
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

            // It guards the weaker of the hives under attack: it flies at the beekeeper nearest that hive.
            int guarded = WeakestHiveUnderThreat(bot);
            ISwarmTarget target = guarded != NoHive
                ? bot.NearestBeekeeper(session.Hives[guarded].Position)
                : session.AimTarget ?? bot.NearestBeekeeper(bot.Core);
            if (target == null)
            {
                return;
            }

            bot.Approach(target.Position, bot.StandOffMetres);
            Attack(bot);
        }

        private static void Attack(PlaytestBot bot)
        {
            HiveDefenseSession session = bot.Session;
            if (session.Fury.IsFull && session.ActiveBeekeeperCount > 1 && bot.TryPlay(PlaytestMatch.ColossusIndex))
            {
                return;
            }

            float size = session.SizeScale;
            bool isBigStrikeRunning = bot.IsRunning(SongSlot.Strike) && session.Forms.Slot(SongSlot.Strike).Form != SwarmForm.Sting;
            if (!isBigStrikeRunning)
            {
                MeteorSong meteor = bot.Match.Meteor;
                int crowd = bot.BiggestCrowd(meteor.MaxRange * size, meteor.Radius * size);
                if (CanSpare(bot, PlaytestMatch.MeteorIndex) && (crowd > 1 || (crowd > 0 && IsRich(bot, PlaytestMatch.MeteorIndex)))
                    && bot.TryPlay(PlaytestMatch.MeteorIndex))
                {
                    return;
                }

                int close = bot.CountWithin(bot.Core, bot.Match.Tornado.Radius * size);
                if (CanSpare(bot, PlaytestMatch.TornadoIndex) && (close > 1 || (close > 0 && IsRich(bot, PlaytestMatch.TornadoIndex)))
                    && bot.TryPlay(PlaytestMatch.TornadoIndex))
                {
                    return;
                }
            }

            if (!bot.IsRunning(SongSlot.Strike) && session.AimTarget != null)
            {
                bot.TryPlay(PlaytestMatch.StingIndex);
            }
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
                bool isThreatened = !hives[hive].IsDestroyed && bot.CountWithin(hives[hive].Position, ThreatMetres) > 0;
                if (isThreatened && (weakest == NoHive || HealthShare(hives[hive]) < HealthShare(hives[weakest])))
                {
                    weakest = hive;
                }
            }

            return weakest;
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

        private static bool CanSpare(PlaytestBot bot, int song)
        {
            SwarmBody swarm = bot.Session.Swarm;
            return swarm.Count - bot.CostOf(song) >= swarm.Capacity * ReserveShare;
        }

        private static bool IsRich(PlaytestBot bot, int song)
        {
            SwarmBody swarm = bot.Session.Swarm;
            return swarm.Count - (swarm.Capacity * ReserveShare) >= bot.CostOf(song) * RichInCasts;
        }
    }
}
