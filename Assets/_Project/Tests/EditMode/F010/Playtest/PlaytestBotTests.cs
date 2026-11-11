using NUnit.Framework;
using UnityEngine;

namespace DigitalStory.Tests.EditMode
{
    /// <summary>
    /// The playtest bot reads which hive a Honeycomb would shell as the Hive aim rule chooses it (plan section E): the
    /// standing hive hit within the last <see cref="DigitalStory.Data.AimConfig.HiveRecentSeconds"/>, otherwise the standing
    /// hive nearest the core. The towers plans place their shells by it, so the tower shares in section M rest on it.
    /// </summary>
    public sealed class PlaytestBotTests
    {
        private const uint Seed = 1;

        // PlaytestMatch's hives: west at x = -12.5 and east at x = 12.5, the core between them.
        private const int West = 0;
        private const int East = 1;

        private static readonly Vector3 NearWest = new Vector3(-6f, 0f, 0f);
        private static readonly Vector3 NearEast = new Vector3(6f, 0f, 0f);

        [Test]
        public void ShellHive_OneHiveHitASecondAgoAndTheOtherNeverHit_IsTheHitHive()
        {
            using (var match = new PlaytestMatch(Seed))
            {
                var bot = new PlaytestBot(match, Seed);
                float westHealth = match.Session.Hives[West].Health;

                bot.Play(new HitOncePlan(NearWest, East, 1f), 2f);

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

                bot.Play(new HitOncePlan(NearEast, -1, 0f), 2f);

                Assert.That(bot.Core.x, Is.GreaterThan(0f), "the core stands nearer the east hive");
                Assert.That(bot.ShellHive(), Is.EqualTo(East));
            }
        }

        [Test]
        public void ShellHive_AHitLongerAgoThanTheRecentSeconds_IsForgotten_SoTheHiveNearestTheCore()
        {
            using (var match = new PlaytestMatch(Seed))
            {
                var bot = new PlaytestBot(match, Seed);

                bot.Play(new HitOncePlan(NearWest, East, 0.5f), 0.5f + match.Aim.HiveRecentSeconds + 1f);

                Assert.That(bot.ShellHive(), Is.EqualTo(West));
            }
        }

        // Flies the core toward a point, and hits one hive once, by a point of health, at the first tick from a given time.
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
