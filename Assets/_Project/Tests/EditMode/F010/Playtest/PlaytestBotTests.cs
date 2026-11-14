using System.Collections.Generic;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The playtest bot reads which hive a Honeycomb would shell as the Hive aim rule chooses it (plan section E): the
    /// standing hive hit within the last <see cref="DigitalStory.Data.AimConfig.HiveRecentSeconds"/>, otherwise the standing
    /// hive nearest the core. The towers plans place their shells by it, so the tower shares in section M rest on it. It
    /// also fights as the giant with the giant's keys (plan section I), so the giant's share of the damage rests on that.
    /// </summary>
    public sealed class PlaytestBotTests
    {
        private const uint Seed = 1;

        // As EconomyPlaytestTests: a match neither won nor lost by then has stalled.
        private const float MatchSeconds = 20f * 60f;

        // PlaytestMatch's hives stand wherever the seed's map puts them, at least 18 m apart (plan section T), so a spot 4 m
        // from one hive toward the other is nearer that one. The names are the old fixed map's.
        private const int West = 0;
        private const int East = 1;
        private const float NearMetres = 4f;

        [Test]
        public void ShellHive_OneHiveHitASecondAgoAndTheOtherNeverHit_IsTheHitHive()
        {
            using (var match = new PlaytestMatch(Seed))
            {
                var bot = new PlaytestBot(match, Seed);
                float westHealth = match.Session.Hives[West].Health;

                bot.Play(new HitOncePlan(Near(match, West), East, 1f), 2f);

                Assert.That(match.Session.Hives[West].Health, Is.EqualTo(westHealth), "the west hive was never hit");
                Assert.That(bot.ShellHive(), Is.EqualTo(East), "the hive hit within the last 3 s, though the core stands nearer the other");
            }
        }

        [Test]
        public void ShellHive_NoHiveEverHit_IsTheHiveNearestTheCore()
        {
            using (var match = new PlaytestMatch(Seed))
            {
                var bot = new PlaytestBot(match, Seed);

                bot.Play(new HitOncePlan(Near(match, East), -1, 0f), 2f);

                Vector3 core = bot.Core;
                Assert.That(
                    (core - match.Session.Hives[East].Position).sqrMagnitude,
                    Is.LessThan((core - match.Session.Hives[West].Position).sqrMagnitude),
                    "the core stands nearer the east hive");
                Assert.That(bot.ShellHive(), Is.EqualTo(East));
            }
        }

        [Test]
        public void ShellHive_AHitLongerAgoThanTheRecentSeconds_IsForgotten_SoTheHiveNearestTheCore()
        {
            using (var match = new PlaytestMatch(Seed))
            {
                var bot = new PlaytestBot(match, Seed);

                bot.Play(new HitOncePlan(Near(match, West), East, 0.5f), 0.5f + match.Aim.HiveRecentSeconds + 1f);

                Assert.That(bot.ShellHive(), Is.EqualTo(West));
            }
        }

        // Plan section I: over whole matches the bot, pressing the giant's keys, roars, punches, kicks and slams, and plays
        // both phrases, the spin kick and the leap slam. Since songs-v2 V Fury fills at 1 point per 11.5 damage, so a match
        // without nests stands the giant about twice, and with nests the giant fells most beekeepers before a third punch; so
        // the moves are gathered over five matches without nests and three with them.
        [Test]
        public void FightAsGiant_OverAMatch_UsesEveryMoveAndBothPhrases()
        {
            var started = new HashSet<ColossusAttack>();
            for (uint seed = Seed; seed < Seed + 8; seed++)
            {
                using (var match = new PlaytestMatch(seed))
                {
                    match.Session.ColossusAttackStarted += attack => started.Add(attack);
                    IPlaytestPlan plan = seed < Seed + 5 ? new MixedPlan() : (IPlaytestPlan)new MixedWithNestsPlan();
                    new PlaytestBot(match, seed).Play(plan, MatchSeconds);
                }
            }

            var moves = new[]
            {
                ColossusAttack.Roar, ColossusAttack.Punch, ColossusAttack.Kick, ColossusAttack.Slam, ColossusAttack.SpinKick,
                ColossusAttack.LeapSlam,
            };
            Assert.That(started, Is.SupersetOf(moves));
        }

        // Songs-v2 R: as a player at a beekeeper too far to reach soon, the bot launches the giant's hands, both of them, and
        // the fists hit.
        [Test]
        public void FightAsGiant_OverAMatch_LaunchesItsHandsAtFarBeekeepers()
        {
            using (var match = new PlaytestMatch(Seed))
            {
                var thrown = new List<ColossusHand>();
                int landed = 0;
                match.Session.ColossusFistThrown += thrown.Add;
                match.Session.ColossusFistLanded += _ => landed++;

                new PlaytestBot(match, Seed).Play(new MixedPlan(), MatchSeconds);

                Assert.That(thrown, Does.Contain(ColossusHand.Right));
                Assert.That(thrown, Does.Contain(ColossusHand.Left), "a player uses both hands, the other while one regrows");
                Assert.That(landed, Is.GreaterThan(0));
            }
        }

        // Flies the core toward a point, and hits one hive once, by a point of health, at the first tick from a given time.
        private static Vector3 Near(PlaytestMatch match, int hive)
        {
            Vector3 own = match.Session.Hives[hive].Position;
            Vector3 other = match.Session.Hives[1 - hive].Position;
            return own + ((other - own).normalized * NearMetres);
        }

        private sealed class HitOncePlan : IPlaytestPlan
        {
            private readonly Vector3 _standAt;
            private readonly int _hive;
            private readonly float _hitAtSeconds;
            private bool _hasHit;

            public HitOncePlan(Vector3 standAt, int hive, float hitAtSeconds)
            {
                _standAt = standAt;
                _hive = hive;
                _hitAtSeconds = hitAtSeconds;
            }

            public string Name => "HitOnce";

            public void Decide(PlaytestBot bot)
            {
                bot.FlyTo(_standAt);
                if (_hive >= 0 && !_hasHit && bot.Time >= _hitAtSeconds)
                {
                    bot.Session.Hives[_hive].Damage(1f);
                    _hasHit = true;
                }
            }
        }
    }
}
