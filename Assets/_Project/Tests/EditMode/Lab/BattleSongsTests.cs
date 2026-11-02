using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class BattleSongsTests
    {
        private const float Tolerance = 0.001f;
        private const float StepSeconds = 1f / 30f;
        private const float OnlyOneHit = 1000f;
        private const float HitDamage = 10f;

        private static readonly FighterStats Statue = new FighterStats(1000f, 0f, 0f, OnlyOneHit, 1f);
        private static readonly FighterStats StillHitter = new FighterStats(1000f, 0f, HitDamage, OnlyOneHit, 8f);
        private static readonly FighterStats Runner = new FighterStats(1000f, 5f, HitDamage, OnlyOneHit, 1f);
        private static readonly FighterStats Mender = new FighterStats(1000f, 0f, 0f, 1f, 4f, 0f, new FighterTraits(healing: 7f));

        private Maze _maze;
        private BattleConfig _config;
        private LabPiper _piper;
        private Health _health;
        private Battle _battle;

        [SetUp]
        public void Build()
        {
            _maze = Corridor(8);
            _config = ScriptableObject.CreateInstance<BattleConfig>();
            _piper = new LabPiper(ScriptableObject.CreateInstance<LabPlayerConfig>(), _maze);
            _health = new Health(100f);
            _battle = new Battle(_maze, _config, _piper, _health);
        }

        private void Run(float seconds)
        {
            for (float passed = 0f; passed < seconds; passed += StepSeconds)
            {
                _battle.Tick(StepSeconds);
            }
        }

        private Minion Awake(int cell, FighterStats stats, float detectTiles = 30f)
        {
            return _battle.AddMinion(Minion(stats, detectTiles), _maze.CenterOf(cell));
        }

        private static float Lost(Fighter fighter) => fighter.Health.Max - fighter.Health.Current;

        [Test]
        public void Play_ASongOfHealing_MendsEveryCreatureAtOnce()
        {
            Creature first = _battle.AddCreature(Creature(Statue));
            Creature second = _battle.AddCreature(Creature(Statue));
            first.Health.Damage(50f);
            second.Health.Damage(5f);

            _battle.Play(Song(new SongBuff(healing: 30f)));

            Assert.That(Lost(first), Is.EqualTo(20f).Within(Tolerance));
            Assert.That(Lost(second), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Play_ASongOfHealing_MendsThePiper()
        {
            _health.Damage(50f);

            _battle.Play(Song(new SongBuff(healing: 30f)));

            Assert.That(_health.Current, Is.EqualTo(80f).Within(Tolerance));
        }

        [Test]
        public void Play_ASongOfHealing_ReportsEachHealing()
        {
            var healed = new List<Vector2>();
            _battle.Healed += healed.Add;
            _battle.AddCreature(Creature(Statue)).Health.Damage(5f);
            _health.Damage(5f);

            _battle.Play(Song(new SongBuff(healing: 30f)));

            Assert.That(healed.Count, Is.EqualTo(2));
        }

        [Test]
        public void Play_ASongOfHealing_LeavesEnemiesAlone()
        {
            Minion minion = Awake(0, Statue);
            minion.Health.Damage(50f);
            Run(0.1f);

            _battle.Play(Song(new SongBuff(healing: 30f)));

            Assert.That(Lost(minion), Is.EqualTo(50f).Within(Tolerance));
        }

        [Test]
        public void Play_ASongThatHappensOnce_DoesNotCountAsRunning()
        {
            _battle.Play(Song(new SongBuff(healing: 30f)));

            Assert.That(_battle.IsBoosted, Is.False);
        }

        [Test]
        public void Play_ASongOfShielding_TakesItsShareOffCreatureHits()
        {
            Awake(1, StillHitter);
            Creature creature = _battle.AddCreature(Creature(Statue));
            _battle.Play(Song(new SongBuff(seconds: 5f, shieldShare: 0.4f)));

            Run(0.2f);

            Assert.That(Lost(creature), Is.EqualTo(HitDamage * 0.6f).Within(Tolerance));
        }

        [Test]
        public void Play_AFullShield_StopsEveryHit()
        {
            Awake(1, new FighterStats(1000f, 0f, HitDamage, OnlyOneHit, 8f, 3f));
            Creature creature = _battle.AddCreature(Creature(Statue));
            _battle.Play(Song(new SongBuff(seconds: 5f, shieldShare: 1f)));

            Run(0.2f);

            Assert.That(Lost(creature), Is.EqualTo(0f));
        }

        [Test]
        public void Play_AShield_WearsOff()
        {
            _battle.Play(Song(new SongBuff(seconds: 1f, shieldShare: 1f)));
            Run(1.1f);
            Awake(1, StillHitter);
            Creature creature = _battle.AddCreature(Creature(Statue));

            Run(0.2f);

            Assert.That(Lost(creature), Is.EqualTo(HitDamage).Within(Tolerance));
        }

        [Test]
        public void Play_ASongOfPace_MakesCreaturesWalkFaster()
        {
            Awake(7, Statue, 60f);
            Run(0.1f);
            Creature plain = _battle.AddCreature(Creature(Runner));
            Run(0.5f);
            float plainWalk = (plain.Position - _piper.Position).magnitude;

            _battle.Retreat();
            Awake(7, Statue, 60f);
            Run(0.1f);
            Creature quick = _battle.AddCreature(Creature(Runner));
            _battle.Play(Song(new SongBuff(seconds: 5f, paceMultiplier: 2f)));
            Run(0.5f);

            Assert.That((quick.Position - _piper.Position).magnitude, Is.GreaterThan(plainWalk * 1.5f));
        }

        [Test]
        public void Play_ASongOfPace_MakesCreaturesHitFaster()
        {
            Minion minion = Awake(0, Statue);
            _battle.AddCreature(Creature(new FighterStats(1000f, 0f, 1f, 1f, 2f)));
            _battle.Play(Song(new SongBuff(seconds: 5f, paceMultiplier: 2f)));

            Run(2.1f);

            Assert.That(Lost(minion), Is.GreaterThanOrEqualTo(4f));
        }

        [Test]
        public void Play_ASongOfPace_LeavesMinionsAlone()
        {
            Minion hitter = Awake(0, new FighterStats(1000f, 0f, 1f, 1f, 2f));
            Creature creature = _battle.AddCreature(Creature(Statue));
            _battle.Play(Song(new SongBuff(seconds: 5f, paceMultiplier: 2f)));

            Run(2.1f);

            Assert.That(Lost(creature), Is.LessThanOrEqualTo(3f));
            Assert.That(hitter.IsAlive, Is.True);
        }

        [Test]
        public void Play_ASongOfPace_DoesNotShortenTheCreaturesWait()
        {
            _battle.AddCreature(Creature(Statue));
            _battle.Play(Song(new SongBuff(seconds: 100f, paceMultiplier: 2f)));

            Run(_config.QuietSecondsBeforeLeaving - 1f);

            Assert.That(_battle.Creatures.Count, Is.EqualTo(1));
        }

        [Test]
        public void Play_AFinisher_MultipliesDamageToAHurtTarget()
        {
            Minion minion = Awake(0, Statue);
            minion.Health.Damage(600f);
            _battle.AddCreature(Creature(Runner));
            _battle.Play(Song(new SongBuff(seconds: 5f, finisherMultiplier: 2f)));

            Run(0.2f);

            Assert.That(Lost(minion), Is.EqualTo(600f + (HitDamage * 2f)).Within(Tolerance));
        }

        [Test]
        public void Play_AFinisher_LeavesAHealthyTargetAlone()
        {
            Minion minion = Awake(0, Statue);
            _battle.AddCreature(Creature(Runner));
            _battle.Play(Song(new SongBuff(seconds: 5f, finisherMultiplier: 2f)));

            Run(0.2f);

            Assert.That(Lost(minion), Is.EqualTo(HitDamage).Within(Tolerance));
        }

        [Test]
        public void Play_ASongOfSilence_StopsEnemyHealers()
        {
            Minion hurt = Awake(1, Statue);
            hurt.Health.Damage(50f);
            Awake(1, Mender);
            _battle.Play(Song(new SongBuff(seconds: 5f, stopsEnemyHealing: true)));

            Run(2f);

            Assert.That(Lost(hurt), Is.EqualTo(50f).Within(Tolerance));
        }

        [Test]
        public void Play_ASongOfSilence_LeavesCreatureHealersAlone()
        {
            Creature hurt = _battle.AddCreature(Creature(Statue));
            hurt.Health.Damage(50f);
            _battle.AddCreature(Creature(Mender));
            _battle.Play(Song(new SongBuff(seconds: 5f, stopsEnemyHealing: true)));

            Run(0.2f);

            Assert.That(Lost(hurt), Is.EqualTo(43f).Within(Tolerance));
        }

        [Test]
        public void Play_ASongOfSilence_WearsOff()
        {
            Minion hurt = Awake(1, Statue);
            hurt.Health.Damage(50f);
            Awake(1, Mender);
            _battle.Play(Song(new SongBuff(seconds: 1f, stopsEnemyHealing: true)));

            Run(1.3f);

            Assert.That(Lost(hurt), Is.LessThan(50f));
        }

        [Test]
        public void Play_ASongThatSlows_SlowsEveryAwakeEnemy()
        {
            Minion near = Awake(1, Statue);
            Minion far = Awake(3, Statue);
            Run(0.1f);

            _battle.Play(Song(new SongBuff(enemySlowShare: 0.5f, enemySlowSeconds: 2f)));

            Assert.That(near.IsSlowed, Is.True);
            Assert.That(far.IsSlowed, Is.True);
        }

        [Test]
        public void Play_ASongThatSlows_LeavesASleepingEnemyAlone()
        {
            Minion asleep = Awake(6, Statue, 1f);
            Run(0.1f);

            _battle.Play(Song(new SongBuff(enemySlowShare: 0.5f, enemySlowSeconds: 2f)));

            Assert.That(asleep.IsSlowed, Is.False);
        }

        [Test]
        public void Play_ASongThatStings_HurtsEveryAwakeEnemy()
        {
            Minion near = Awake(1, Statue);
            Minion far = Awake(3, Statue);
            Run(0.1f);

            _battle.Play(Song(new SongBuff(burstDamage: 25f)));

            Assert.That(Lost(near), Is.EqualTo(25f).Within(Tolerance));
            Assert.That(Lost(far), Is.EqualTo(25f).Within(Tolerance));
        }

        [Test]
        public void Play_ASongThatStings_LeavesASleepingEnemyAlone()
        {
            Minion asleep = Awake(6, Statue, 1f);
            Run(0.1f);

            _battle.Play(Song(new SongBuff(burstDamage: 25f)));

            Assert.That(Lost(asleep), Is.EqualTo(0f));
        }

        [Test]
        public void Play_ASongThatStings_LetsAKillCount()
        {
            var killed = new List<Minion>();
            _battle.MinionKilled += killed.Add;
            Minion minion = Awake(1, new FighterStats(10f, 0f, 0f, OnlyOneHit, 1f));
            Run(0.1f);

            _battle.Play(Song(new SongBuff(burstDamage: 25f)));
            Run(0.1f);

            Assert.That(killed, Is.EqualTo(new[] { minion }));
        }

        [Test]
        public void Play_TwoDifferentSongs_WorkTogether()
        {
            Minion minion = Awake(0, Statue);
            _battle.AddCreature(Creature(Runner));
            _battle.Play(Song(new SongBuff(seconds: 5f, damageMultiplier: 2f)));
            _battle.Play(Song(new SongBuff(seconds: 5f, damageMultiplier: 1.5f)));

            Run(0.2f);

            Assert.That(Lost(minion), Is.EqualTo(HitDamage * 3f).Within(Tolerance));
        }

        [Test]
        public void Play_TheSameSongAgain_RestartsItRatherThanAddingUp()
        {
            BattleSong song = Song(new SongBuff(seconds: 5f, damageMultiplier: 2f));
            Minion minion = Awake(0, Statue);
            _battle.AddCreature(Creature(Runner));
            _battle.Play(song);
            _battle.Play(song);

            Run(0.2f);

            Assert.That(Lost(minion), Is.EqualTo(HitDamage * 2f).Within(Tolerance));
            Assert.That(_battle.Songs.Running.Count, Is.EqualTo(1));
        }

        [Test]
        public void Songs_EachEndsOnItsOwnTime()
        {
            BattleSong shortSong = Song(new SongBuff(seconds: 1f, damageMultiplier: 2f));
            BattleSong longSong = Song(new SongBuff(seconds: 5f, shieldShare: 0.5f));
            _battle.Play(shortSong);
            _battle.Play(longSong);

            Run(1.2f);

            Assert.That(_battle.Songs.Running.Count, Is.EqualTo(1));
            Assert.That(_battle.Songs.Running[0].Song, Is.SameAs(longSong));
            Assert.That(_battle.Songs.DamageMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Retreat_EndsEverySong()
        {
            _battle.Play(Song(new SongBuff(seconds: 5f, damageMultiplier: 2f)));
            _battle.Play(Song(new SongBuff(seconds: 5f, shieldShare: 0.5f)));

            _battle.Retreat();

            Assert.That(_battle.Songs.Running, Is.Empty);
            Assert.That(_battle.Songs.ShieldShare, Is.EqualTo(0f));
        }

        [Test]
        public void TheRealSongs_EachDoesSomething()
        {
            foreach (IComboSong song in AllSongs(ScriptableObject.CreateInstance<CreatureRoster>()))
            {
                if (song is BattleSong battleSong)
                {
                    SongBuff buff = battleSong.Buff;
                    bool lasts = buff.Lasts && (buff.DamageMultiplier > 1f || buff.ShieldShare > 0f || buff.PaceMultiplier > 1f || buff.FinisherMultiplier > 1f || buff.StopsEnemyHealing);
                    bool isAtOnce = buff.Healing > 0f || buff.EnemySlowShare > 0f || buff.BurstDamage > 0f;
                    Assert.That(lasts || isAtOnce, Is.True, song.Name);
                }
            }
        }
    }
}
