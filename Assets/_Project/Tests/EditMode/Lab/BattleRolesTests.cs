using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class BattleRolesTests
    {
        private const float Tolerance = 0.001f;
        private const float StepSeconds = 1f / 30f;
        private const float OnlyOneHit = 1000f;
        private const float HitDamage = 10f;

        private static readonly FighterStats Statue = new FighterStats(1000f, 0f, 0f, OnlyOneHit, 1f);
        private static readonly FighterStats StillHitter = new FighterStats(1000f, 0f, HitDamage, OnlyOneHit, 8f);
        private static readonly FighterStats Runner = new FighterStats(1000f, 5f, HitDamage, OnlyOneHit, 1f);

        private Maze _maze;
        private BattleConfig _config;
        private LabPiper _piper;
        private Health _health;
        private Battle _battle;

        private void Build(Maze maze)
        {
            _maze = maze;
            _config = ScriptableObject.CreateInstance<BattleConfig>();
            _piper = new LabPiper(ScriptableObject.CreateInstance<LabPlayerConfig>(), maze);
            _health = new Health(100f);
            _battle = new Battle(maze, _config, _piper, _health);
        }

        private void Run(float seconds)
        {
            for (float passed = 0f; passed < seconds; passed += StepSeconds)
            {
                _battle.Tick(StepSeconds);
            }
        }

        private Vector2 Cell(int cell) => _maze.CenterOf(cell);

        private static float Lost(Fighter fighter) => fighter.Health.Max - fighter.Health.Current;

        private static FighterStats With(FighterTraits traits, float reach = 8f, float area = 0f, float damage = HitDamage, float seconds = OnlyOneHit)
        {
            return new FighterStats(1000f, 0f, damage, seconds, reach, area, traits);
        }

        private static FighterStats Healer => With(new FighterTraits(healing: 7f), reach: 4f, damage: 0f);

        private static BattleSong Boost(float multiplier, float seconds)
        {
            return Song(new SongBuff(seconds: seconds, damageMultiplier: multiplier));
        }

        private Minion AwakeStatue(int cell, FighterStats stats, float detectTiles = 30f)
        {
            return _battle.AddMinion(Minion(stats, detectTiles), Cell(cell));
        }

        [Test]
        public void Creature_WithNoFightForTheQuietTime_GoesHome()
        {
            Build(Corridor(4));
            _battle.AddCreature(Creature(Sturdy));

            Run(_config.QuietSecondsBeforeLeaving + 0.1f);

            Assert.That(_battle.Creatures, Is.Empty);
        }

        [Test]
        public void Creature_JustBeforeTheQuietTime_Stays()
        {
            Build(Corridor(4));
            _battle.AddCreature(Creature(Sturdy));

            Run(_config.QuietSecondsBeforeLeaving - 0.5f);

            Assert.That(_battle.Creatures.Count, Is.EqualTo(1));
        }

        [Test]
        public void Creature_InAFight_NeverGoesHome()
        {
            Build(Corridor(4));
            AwakeStatue(1, Statue);
            _battle.AddCreature(Creature(Statue));

            Run(_config.QuietSecondsBeforeLeaving + 1f);

            Assert.That(_battle.Creatures.Count, Is.EqualTo(1));
        }

        [Test]
        public void Creature_SummonedLate_GetsTheWholeQuietTime()
        {
            Build(Corridor(4));
            _battle.AddCreature(Creature(Sturdy));
            Run(_config.QuietSecondsBeforeLeaving - 1f);
            Creature late = _battle.AddCreature(Creature(Sturdy));

            Run(2f);

            Assert.That(_battle.Creatures, Is.EqualTo(new[] { late }));
        }

        [Test]
        public void Creature_AfterAFight_GetsTheWholeQuietTimeAgain()
        {
            Build(Corridor(4));
            Creature creature = _battle.AddCreature(Creature(Statue));
            Run(_config.QuietSecondsBeforeLeaving - 1f);
            Minion minion = AwakeStatue(1, Statue);
            Run(0.5f);

            minion.Health.Damage(minion.Health.Max);
            Run(_config.QuietSecondsBeforeLeaving - 1f);

            Assert.That(_battle.Creatures, Is.EqualTo(new[] { creature }));
        }

        [Test]
        public void Creature_GoingHome_IsReportedAndIsNotAKill()
        {
            Build(Corridor(4));
            var left = new List<Creature>();
            int kills = 0;
            _battle.CreatureLeft += left.Add;
            _battle.MinionKilled += _ => kills++;
            Creature creature = _battle.AddCreature(Creature(Sturdy));

            Run(_config.QuietSecondsBeforeLeaving + 0.1f);

            Assert.That(left, Is.EqualTo(new[] { creature }));
            Assert.That(kills, Is.EqualTo(0));
        }

        [Test]
        public void Tick_CountsTheBattlesTime()
        {
            Build(Corridor(4));

            Run(1f);

            Assert.That(_battle.ElapsedSeconds, Is.EqualTo(1f).Within(StepSeconds));
        }

        // A sleeping minion is never rested, yet a lunge or a flash it started still has to end.
        [Test]
        public void Tick_PassesTimeForSleepingMinionsToo()
        {
            Build(Corridor(4));
            Minion sleeper = _battle.AddMinion(Minion(Statue, 0.1f), Cell(3));
            sleeper.MarkHit();

            Run(0.5f);

            Assert.That(sleeper.IsAwake, Is.False, "The minion has to sleep for this test to mean anything.");
            Assert.That(sleeper.SecondsSinceStrike, Is.EqualTo(0.5f).Within(StepSeconds));
        }

        [Test]
        public void Creature_StrikingAMinionBehindIt_TurnsToFaceIt()
        {
            Build(Corridor(4));
            _battle.AddMinion(Minion(Statue, 30f), Cell(0) + Vector2.left);
            Creature creature = _battle.AddCreature(Creature(StillHitter));

            Run(StepSeconds);

            Assert.That(Mathf.DeltaAngle(creature.Facing, 180f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Minion_StrikingThePiper_TurnsToFaceIt()
        {
            Build(Corridor(4));
            Minion minion = _battle.AddMinion(Minion(StillHitter, 30f), Cell(0) + Vector2.right);

            Run(StepSeconds);

            Assert.That(Mathf.DeltaAngle(minion.Facing, 180f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Healer_Healing_TurnsToFaceWhoItHeals()
        {
            Build(Corridor(4));
            Minion hurt = _battle.AddMinion(Minion(Statue, 30f), Cell(0) + Vector2.left);
            Minion healer = _battle.AddMinion(Minion(Healer, 30f), Cell(0) + Vector2.right);
            hurt.Health.Damage(50f);

            Run(StepSeconds);

            Assert.That(hurt.Health.Current, Is.GreaterThan(hurt.Health.Max - 50f), "The healer has to heal for this test to mean anything.");
            Assert.That(Mathf.DeltaAngle(healer.Facing, 180f), Is.EqualTo(0f).Within(Tolerance));
        }

        // Healers never hit, so a fight that only they are left in could never end.
        [Test]
        public void Creature_OnlyHealersInAFight_GoHomeAfterTheQuietTime()
        {
            Build(Corridor(4));
            AwakeStatue(1, Statue);
            _battle.AddCreature(Creature(Healer));
            _battle.AddCreature(Creature(Healer));

            Run(_config.QuietSecondsBeforeLeaving + 0.1f);

            Assert.That(_battle.IsInBattle, Is.True, "The fight has to go on for this test to mean anything.");
            Assert.That(_battle.Creatures, Is.Empty);
        }

        [Test]
        public void Creature_AHealerBesideAStriker_StaysThroughTheFight()
        {
            Build(Corridor(4));
            AwakeStatue(1, Statue);
            _battle.AddCreature(Creature(Healer));
            _battle.AddCreature(Creature(Statue));

            Run(_config.QuietSecondsBeforeLeaving + 1f);

            Assert.That(_battle.Creatures.Count, Is.EqualTo(2));
        }

        [Test]
        public void Creature_AHealerWhoseStrikerFalls_GetsTheWholeQuietTime()
        {
            Build(Corridor(4));
            AwakeStatue(1, Statue);
            Creature healer = _battle.AddCreature(Creature(Healer));
            Creature striker = _battle.AddCreature(Creature(Statue));
            Run(_config.QuietSecondsBeforeLeaving - 1f);

            striker.Health.Damage(striker.Health.Max);
            Run(_config.QuietSecondsBeforeLeaving - 1f);
            Assert.That(_battle.Creatures, Is.EqualTo(new[] { healer }));

            Run(2f);
            Assert.That(_battle.Creatures, Is.Empty);
        }

        [Test]
        public void Boost_WhileItLasts_MultipliesCreatureDamage()
        {
            Build(Corridor(4));
            Minion minion = AwakeStatue(0, Statue);
            _battle.AddCreature(Creature(Runner));
            _battle.Play(Boost(1.5f, 5f));

            Run(0.2f);

            Assert.That(Lost(minion), Is.EqualTo(HitDamage * 1.5f).Within(Tolerance));
        }

        [Test]
        public void Boost_WhenItEnds_HitsAsBefore()
        {
            Build(Corridor(4));
            _battle.Play(Boost(1.5f, 1f));
            Run(1.1f);
            Minion minion = AwakeStatue(0, Statue);
            _battle.AddCreature(Creature(Runner));

            Run(0.2f);

            Assert.That(_battle.IsBoosted, Is.False);
            Assert.That(Lost(minion), Is.EqualTo(HitDamage).Within(Tolerance));
        }

        [Test]
        public void Boost_LeavesMinionDamageAlone()
        {
            Build(Corridor(4));
            AwakeStatue(0, StillHitter);
            Creature creature = _battle.AddCreature(Creature(Statue));
            _battle.Play(Boost(3f, 5f));

            Run(0.2f);

            Assert.That(Lost(creature), Is.EqualTo(HitDamage).Within(Tolerance));
        }

        [Test]
        public void Boost_CountsDown()
        {
            Build(Corridor(4));
            _battle.Play(Boost(2f, 5f));

            Run(1f);

            Assert.That(_battle.BoostSecondsLeft, Is.EqualTo(4f).Within(0.1f));
        }

        [Test]
        public void Retreat_EndsTheBoost()
        {
            Build(Corridor(4));
            _battle.Play(Boost(2f, 5f));

            _battle.Retreat();

            Assert.That(_battle.IsBoosted, Is.False);
        }

        [Test]
        public void Pack_WithAPackmateNearby_HitsHarder()
        {
            Build(Corridor(4));
            Minion minion = AwakeStatue(0, Statue);
            CreatureType mouse = Creature(With(new FighterTraits(packBonus: 0.5f)));
            _battle.AddCreature(mouse);
            _battle.AddCreature(mouse);

            Run(0.2f);

            Assert.That(Lost(minion), Is.EqualTo(2f * HitDamage * 1.5f).Within(Tolerance));
        }

        [Test]
        public void Pack_WithNobodyElseInThePack_HitsNormally()
        {
            Build(Corridor(4));
            Minion minion = AwakeStatue(0, Statue);
            _battle.AddCreature(Creature(With(new FighterTraits(packBonus: 0.5f))));
            _battle.AddCreature(Creature(Statue));

            Run(0.2f);

            Assert.That(Lost(minion), Is.EqualTo(HitDamage).Within(Tolerance));
        }

        [Test]
        public void Pack_APackmateTooFarAway_DoesNotCount()
        {
            Build(Corridor(8));
            CreatureType mouse = Creature(With(new FighterTraits(packBonus: 0.5f)));
            Creature first = _battle.AddCreature(mouse);
            Creature second = _battle.AddCreature(Creature(With(new FighterTraits(packBonus: 0.5f), reach: 0f)));
            second.Nudge(Vector2.right * (_config.PackTiles + 1f), _maze, _config.UnitRadius);
            Minion minion = AwakeStatue(0, Statue);
            _ = first;

            Run(0.2f);

            Assert.That(Lost(minion), Is.EqualTo(HitDamage).Within(Tolerance));
        }

        [Test]
        public void Healer_WithHurtAlliesInReach_HealsTheMostHurt()
        {
            Build(Corridor(4));
            Creature bad = _battle.AddCreature(Creature(Statue));
            Creature worse = _battle.AddCreature(Creature(Statue));
            bad.Health.Damage(10f);
            worse.Health.Damage(50f);
            _battle.AddCreature(Creature(With(new FighterTraits(healing: 7f), reach: 4f)));

            Run(0.2f);

            Assert.That(Lost(worse), Is.EqualTo(43f).Within(Tolerance));
            Assert.That(Lost(bad), Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void Healer_WithThePiperHurtWorst_HealsThePiper()
        {
            Build(Corridor(4));
            _health.Damage(30f);
            _battle.AddCreature(Creature(With(new FighterTraits(healing: 7f), reach: 4f)));

            Run(0.2f);

            Assert.That(_health.Current, Is.EqualTo(77f).Within(Tolerance));
        }

        [Test]
        public void Healer_WithAnAllyHurtWorseThanThePiper_HealsTheAlly()
        {
            Build(Corridor(4));
            _health.Damage(1f);
            Creature hurt = _battle.AddCreature(Creature(Statue));
            hurt.Health.Damage(500f);
            _battle.AddCreature(Creature(With(new FighterTraits(healing: 7f), reach: 4f)));

            Run(0.2f);

            Assert.That(Lost(hurt), Is.EqualTo(493f).Within(Tolerance));
            Assert.That(_health.Current, Is.EqualTo(99f).Within(Tolerance));
        }

        [Test]
        public void Healer_NeverHealsPastFull()
        {
            Build(Corridor(4));
            Creature hurt = _battle.AddCreature(Creature(Statue));
            hurt.Health.Damage(2f);
            _battle.AddCreature(Creature(With(new FighterTraits(healing: 7f), reach: 4f)));

            Run(0.2f);

            Assert.That(hurt.Health.Current, Is.EqualTo(hurt.Health.Max).Within(Tolerance));
        }

        [Test]
        public void Healer_NeverHitsAnEnemy()
        {
            Build(Corridor(4));
            Minion minion = AwakeStatue(0, Statue);
            int strikes = 0;
            _battle.Struck += strike => strikes += strike.IsByCreature ? 1 : 0;
            _battle.AddCreature(Creature(With(new FighterTraits(healing: 7f), reach: 4f, seconds: 0.1f)));

            Run(0.5f);

            Assert.That(Lost(minion), Is.EqualTo(0f));
            Assert.That(strikes, Is.EqualTo(0));
        }

        [Test]
        public void Healer_WhenItHeals_WaitsBeforeHealingAgain()
        {
            Build(Corridor(4));
            Creature hurt = _battle.AddCreature(Creature(Statue));
            hurt.Health.Damage(100f);
            _battle.AddCreature(Creature(With(new FighterTraits(healing: 7f), reach: 4f, seconds: 1f)));

            Run(0.5f);

            Assert.That(Lost(hurt), Is.EqualTo(93f).Within(Tolerance));
        }

        [Test]
        public void Healer_WhenItHeals_ReportsWhere()
        {
            Build(Corridor(4));
            var healed = new List<Vector2>();
            _battle.Healed += healed.Add;
            Creature hurt = _battle.AddCreature(Creature(Statue));
            hurt.Health.Damage(50f);
            _battle.AddCreature(Creature(With(new FighterTraits(healing: 7f), reach: 4f)));

            Run(0.1f);

            Assert.That(healed.Count, Is.EqualTo(1));
            Assert.That(Vector2.Distance(healed[0], hurt.Position), Is.LessThan(0.5f));
        }

        [Test]
        public void MinionHealer_HealsItsOwnSide()
        {
            Build(Corridor(4));
            Minion hurt = AwakeStatue(1, Statue);
            hurt.Health.Damage(50f);
            AwakeStatue(1, With(new FighterTraits(healing: 7f), reach: 4f));

            Run(0.2f);

            Assert.That(Lost(hurt), Is.EqualTo(43f).Within(Tolerance));
        }

        [Test]
        public void MinionHealer_NeverHitsThePiper()
        {
            Build(Corridor(4));
            AwakeStatue(0, With(new FighterTraits(healing: 7f), reach: 4f, seconds: 0.1f));

            Run(0.5f);

            Assert.That(_health.Current, Is.EqualTo(_health.Max));
        }

        [Test]
        public void MinionHealer_LeavesASleepingMinionAlone()
        {
            Build(Corridor(4));
            Minion asleep = AwakeStatue(1, Statue, detectTiles: 0f);
            asleep.Health.Damage(50f);
            AwakeStatue(1, With(new FighterTraits(healing: 7f), reach: 4f));

            Run(0.2f);

            Assert.That(Lost(asleep), Is.EqualTo(50f).Within(Tolerance));
        }

        [Test]
        public void Healer_BehindAWall_HealsNobody()
        {
            Build(Horseshoe());
            Minion hurt = AwakeStatue(1, Statue);
            hurt.Health.Damage(50f);
            AwakeStatue(0, With(new FighterTraits(healing: 7f), reach: 4f));

            Run(0.2f);

            Assert.That(Lost(hurt), Is.EqualTo(50f).Within(Tolerance));
        }

        [Test]
        public void Hunter_WithAnEnemyStandingBack_RunsPastTheFront()
        {
            Build(Corridor(8));
            Minion front = AwakeStatue(1, Statue);
            Minion back = AwakeStatue(4, new FighterStats(1000f, 0f, 0f, OnlyOneHit, 1f, 0f, new FighterTraits(standsBack: true)));
            _battle.AddCreature(Creature(new FighterStats(1000f, 5f, HitDamage, OnlyOneHit, 1f, 0f, new FighterTraits(huntsBackline: true))));

            Run(3f);

            Assert.That(Lost(back), Is.EqualTo(HitDamage).Within(Tolerance));
            Assert.That(Lost(front), Is.EqualTo(0f));
        }

        [Test]
        public void Hunter_WithNobodyStandingBack_FightsTheNearest()
        {
            Build(Corridor(8));
            Minion front = AwakeStatue(1, Statue);
            _battle.AddCreature(Creature(new FighterStats(1000f, 5f, HitDamage, OnlyOneHit, 1f, 0f, new FighterTraits(huntsBackline: true))));

            Run(2f);

            Assert.That(Lost(front), Is.EqualTo(HitDamage).Within(Tolerance));
        }

        [Test]
        public void Hunter_PastItsLeash_TurnsBack()
        {
            Build(Corridor(12));
            AwakeStatue(11, new FighterStats(1000f, 0f, 0f, OnlyOneHit, 1f, 0f, new FighterTraits(standsBack: true)), 60f);
            Creature hunter = _battle.AddCreature(Creature(new FighterStats(1000f, 5f, HitDamage, OnlyOneHit, 1f, 0f, new FighterTraits(huntsBackline: true))));

            Run(12f);

            Assert.That((hunter.Position - _piper.Position).magnitude, Is.LessThanOrEqualTo(_config.LeashTiles + _maze.Shape.CellPitch));
        }

        [Test]
        public void Slow_AHitWithIt_SlowsEverythingCovered()
        {
            Build(Corridor(4));
            Minion first = AwakeStatue(0, Statue);
            Minion second = AwakeStatue(0, Statue);
            Minion outside = AwakeStatue(3, Statue);
            _battle.AddCreature(Creature(With(new FighterTraits(slowShare: 0.5f, slowSeconds: 2f), reach: 1f, area: 3f)));

            Run(0.2f);

            Assert.That(first.IsSlowed, Is.True);
            Assert.That(second.IsSlowed, Is.True);
            Assert.That(outside.IsSlowed, Is.False);
        }

        [Test]
        public void Slow_WearsOff()
        {
            Build(Corridor(4));
            Minion minion = AwakeStatue(0, Statue);
            _battle.AddCreature(Creature(With(new FighterTraits(slowShare: 0.5f, slowSeconds: 1f), reach: 1f, area: 3f)));

            Run(1.3f);

            Assert.That(minion.IsSlowed, Is.False);
        }

        [Test]
        public void Slow_MakesAFighterWalkSlower()
        {
            Build(Corridor(4));
            var slowed = new Creature(Creature(Runner), Cell(0));
            var plain = new Creature(Creature(Runner), Cell(0));
            slowed.Slow(0.5f, 5f);

            slowed.WalkToward(Cell(3), _maze, _config.UnitRadius, 1f);
            plain.WalkToward(Cell(3), _maze, _config.UnitRadius, 1f);

            Assert.That((slowed.Position - Cell(0)).magnitude, Is.EqualTo((plain.Position - Cell(0)).magnitude / 2f).Within(Tolerance));
        }

        [Test]
        public void Slow_MakesAFighterHitLessOften()
        {
            var slowed = new Creature(Creature(new FighterStats(1f, 1f, 1f, 1f, 1f)), Vector2.zero);
            slowed.MarkHit();
            slowed.Slow(0.5f, 5f);

            slowed.Rest(1f);

            Assert.That(slowed.CanHitNow, Is.False);
        }

        [Test]
        public void Guardian_InRange_DrawsTheAttackFromANearerCreature()
        {
            Build(Corridor(4));
            Creature nearer = _battle.AddCreature(Creature(Statue));
            Creature guardian = _battle.AddCreature(Creature(With(new FighterTraits(guardTiles: 6f), damage: 0f)));
            guardian.Nudge(Vector2.left * 0.5f, _maze, _config.UnitRadius);
            AwakeStatue(1, StillHitter);

            Run(0.2f);

            Assert.That(Lost(guardian), Is.EqualTo(HitDamage).Within(Tolerance));
            Assert.That(Lost(nearer), Is.EqualTo(0f));
        }

        [Test]
        public void Guardian_OutOfRange_LeavesTheNearestAsTheTarget()
        {
            Build(Corridor(4));
            Creature nearer = _battle.AddCreature(Creature(Statue));
            Creature guardian = _battle.AddCreature(Creature(With(new FighterTraits(guardTiles: 2f), damage: 0f)));
            guardian.Nudge(Vector2.left * 0.5f, _maze, _config.UnitRadius);
            AwakeStatue(1, StillHitter);

            Run(0.2f);

            Assert.That(Lost(nearer), Is.EqualTo(HitDamage).Within(Tolerance));
            Assert.That(Lost(guardian), Is.EqualTo(0f));
        }

        [Test]
        public void Guardian_BehindAWall_DrawsNothing()
        {
            Build(Horseshoe());
            Creature guardian = _battle.AddCreature(Creature(With(new FighterTraits(guardTiles: 6f), damage: 0f)));
            Minion minion = AwakeStatue(1, new FighterStats(1000f, 0f, HitDamage, OnlyOneHit, 8f));

            Run(0.2f);

            Assert.That(Lost(guardian), Is.EqualTo(0f));
            Assert.That(minion.IsAwake, Is.True);
        }

        [Test]
        public void Armour_TakesItsShareOffEveryHit()
        {
            Build(Corridor(4));
            AwakeStatue(1, StillHitter);
            Creature armoured = _battle.AddCreature(Creature(With(new FighterTraits(armour: 0.5f), damage: 0f)));

            Run(0.2f);

            Assert.That(Lost(armoured), Is.EqualTo(HitDamage * 0.5f).Within(Tolerance));
        }

        [Test]
        public void Armour_CoversAreaHitsToo()
        {
            Build(Corridor(4));
            AwakeStatue(1, new FighterStats(1000f, 0f, HitDamage, OnlyOneHit, 8f, 3f));
            Creature armoured = _battle.AddCreature(Creature(With(new FighterTraits(armour: 0.5f), damage: 0f)));

            Run(0.2f);

            Assert.That(Lost(armoured), Is.EqualTo(HitDamage * 0.5f).Within(Tolerance));
        }

        [Test]
        public void Gang_OneMemberNotices_WakesEveryone()
        {
            Build(Corridor(12));
            var gang = new MinionGang();
            Minion watcher = _battle.AddMinion(Minion(Statue, detectTiles: 16f), Cell(4), MinionRank.Ordinary, gang);
            Minion dozer = _battle.AddMinion(Minion(Statue, detectTiles: 12f), Cell(4), MinionRank.Ordinary, gang);

            Run(0.2f);

            Assert.That(watcher.IsAwake, Is.True);
            Assert.That(dozer.IsAwake, Is.True);
        }

        [Test]
        public void Gang_AMinionOutsideIt_SleepsOn()
        {
            Build(Corridor(12));
            var gang = new MinionGang();
            _battle.AddMinion(Minion(Statue, detectTiles: 16f), Cell(4), MinionRank.Ordinary, gang);
            Minion stranger = _battle.AddMinion(Minion(Statue, detectTiles: 12f), Cell(4));

            Run(0.2f);

            Assert.That(stranger.IsAwake, Is.False);
        }

        [Test]
        public void Health_Heal_StopsAtTheMaximum()
        {
            var health = new Health(10f);
            health.Damage(4f);

            health.Heal(100f);

            Assert.That(health.Current, Is.EqualTo(10f));
        }

        [Test]
        public void Health_Share_IsTheFractionLeft()
        {
            var health = new Health(10f);
            health.Damage(4f);

            Assert.That(health.Share, Is.EqualTo(0.6f).Within(Tolerance));
        }
    }
}
