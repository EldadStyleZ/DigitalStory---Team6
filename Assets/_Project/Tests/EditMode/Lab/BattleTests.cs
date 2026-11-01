using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class BattleTests
    {
        private const float StepSeconds = 1f / 30f;
        private const float OnlyOneHit = 1000f;

        private static readonly FighterStats Statue = new FighterStats(1000f, 0f, 0f, OnlyOneHit, 1f);

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

        [Test]
        public void Minion_FarFromThePiper_StaysAsleepAndStill()
        {
            Build(Corridor(12));
            Minion minion = _battle.AddMinion(Minion(Sturdy, detectTiles: 8f), Cell(6));
            Vector2 start = minion.Position;

            Run(1f);

            Assert.That(minion.IsAwake, Is.False);
            Assert.That(minion.Position, Is.EqualTo(start));
            Assert.That(_battle.IsInBattle, Is.False);
        }

        [Test]
        public void Minion_WithinDetection_WakesAndStartsABattle()
        {
            Build(Corridor(12));
            Minion minion = _battle.AddMinion(Minion(Sturdy, detectTiles: 8f), Cell(1));

            Run(0.3f);

            Assert.That(minion.IsAwake, Is.True);
            Assert.That(_battle.IsInBattle, Is.True);
        }

        [Test]
        public void Minion_CloseButBehindAWall_DoesNotNotice()
        {
            Build(Horseshoe());
            Minion minion = _battle.AddMinion(Minion(Sturdy, detectTiles: 8f), Cell(1));

            Run(0.5f);

            Assert.That(minion.IsAwake, Is.False, "Four tiles away in a straight line, twelve by walking.");
        }

        [Test]
        public void Minion_WhenThePiperGetsAway_GivesUp()
        {
            Build(Corridor(20));
            Minion minion = _battle.AddMinion(Minion(Statue, detectTiles: 8f), Cell(1));
            Run(0.3f);

            _piper.MoveTo(19);
            Run(0.5f);

            Assert.That(minion.IsAwake, Is.False);
            Assert.That(_battle.IsInBattle, Is.False);
        }

        [Test]
        public void Minion_WithNoCreaturesLeft_ChasesAndHitsThePiper()
        {
            Build(Corridor(12));
            _battle.AddMinion(Minion(new FighterStats(50f, 4f, 5f, 0.5f, 0.5f)), Cell(1));

            Run(4f);

            Assert.That(_health.Current, Is.LessThan(_health.Max));
        }

        [Test]
        public void Minion_WithACreatureAlive_NeverHitsThePiper()
        {
            Build(Corridor(12));
            _battle.AddCreature(Creature(Sturdy));
            _battle.AddMinion(Minion(new FighterStats(5000f, 4f, 5f, 0.5f, 0.5f)), Cell(1));

            Run(6f);

            Assert.That(_health.Current, Is.EqualTo(_health.Max));
        }

        [Test]
        public void Minion_WhenTheLastCreatureDies_TurnsOnThePiper()
        {
            Build(Corridor(12));
            Creature creature = _battle.AddCreature(Creature(new FighterStats(5f, 0f, 0f, 100f, 0.5f)));
            _battle.AddMinion(Minion(new FighterStats(5000f, 4f, 10f, 0.5f, 0.5f)), Cell(1));

            Run(6f);

            Assert.That(creature.IsAlive, Is.False);
            Assert.That(_battle.Creatures, Is.Empty);
            Assert.That(_health.Current, Is.LessThan(_health.Max));
        }

        [Test]
        public void Minion_WithASingleHit_HurtsOneCreature()
        {
            Build(Corridor(12));
            Creature first = _battle.AddCreature(Creature(Statue));
            Creature second = _battle.AddCreature(Creature(Statue));
            _battle.AddMinion(Minion(new FighterStats(1000f, 4f, 5f, OnlyOneHit, 1f)), Cell(0));

            Run(1f);

            Assert.That(Lost(first) + Lost(second), Is.EqualTo(5f));
        }

        [Test]
        public void Minion_WithAnAreaHit_HurtsEveryCreatureAround_ButNotThePiper()
        {
            Build(Corridor(12));
            Creature first = _battle.AddCreature(Creature(Statue));
            Creature second = _battle.AddCreature(Creature(Statue));
            _battle.AddMinion(Minion(new FighterStats(1000f, 4f, 5f, OnlyOneHit, 1f, 4f)), Cell(0));

            Run(1f);

            Assert.That(Lost(first), Is.EqualTo(5f));
            Assert.That(Lost(second), Is.EqualTo(5f));
            Assert.That(_health.Current, Is.EqualTo(_health.Max));
        }

        [Test]
        public void Creature_WithASingleHit_HurtsOneMinion()
        {
            Build(Corridor(12));
            _battle.AddCreature(Creature(new FighterStats(1000f, 4f, 5f, OnlyOneHit, 1f)));
            Minion first = _battle.AddMinion(Minion(Statue), Cell(0));
            Minion second = _battle.AddMinion(Minion(Statue), Cell(0));

            Run(1f);

            Assert.That(Lost(first) + Lost(second), Is.EqualTo(5f));
        }

        [Test]
        public void Creature_WithAnAreaHit_HurtsEveryMinionAround()
        {
            Build(Corridor(12));
            _battle.AddCreature(Creature(new FighterStats(1000f, 4f, 5f, OnlyOneHit, 1f, 4f)));
            Minion first = _battle.AddMinion(Minion(Statue), Cell(0));
            Minion second = _battle.AddMinion(Minion(Statue), Cell(0));

            Run(1f);

            Assert.That(Lost(first), Is.EqualTo(5f));
            Assert.That(Lost(second), Is.EqualTo(5f));
        }

        [Test]
        public void AreaHit_NextToAWall_DoesNotReachThroughIt()
        {
            Build(Horseshoe());
            _battle.AddCreature(Creature(new FighterStats(1000f, 4f, 5f, OnlyOneHit, 1f, 20f)));
            Minion near = _battle.AddMinion(Minion(Statue, detectTiles: 100f), Cell(0));
            Minion behindTheWall = _battle.AddMinion(Minion(Statue, detectTiles: 100f), Cell(1));

            Run(1f);

            Assert.That(Lost(near), Is.EqualTo(5f));
            Assert.That(Lost(behindTheWall), Is.EqualTo(0f));
        }

        [Test]
        public void Fighter_InContact_HitsNoFasterThanItsRhythm()
        {
            Build(Corridor(12));
            _battle.AddCreature(Creature(new FighterStats(1000f, 4f, 1f, 1f, 1f)));
            Minion minion = _battle.AddMinion(Minion(Statue), Cell(0));

            Run(2.5f);

            Assert.That(Lost(minion), Is.InRange(2f, 3f));
        }

        [Test]
        public void Creature_WithNothingToFight_FollowsThePiper()
        {
            Build(Corridor(12));
            Creature creature = _battle.AddCreature(Creature(Sturdy));

            _piper.MoveTo(3);
            Run(4f);

            Assert.That(Vector2.Distance(creature.Position, _piper.Position), Is.LessThanOrEqualTo(_config.FollowTiles + 0.5f));
        }

        [Test]
        public void Creature_FollowingAcrossAWall_WalksRoundIt()
        {
            Build(Horseshoe());
            Creature creature = _battle.AddCreature(Creature(Sturdy));

            _piper.MoveTo(1);
            Run(5f);

            Assert.That(_maze.AreLinked(_maze.CellAt(creature.Position), 1), Is.True);
            Assert.That(_maze.CellAt(creature.Position), Is.Not.EqualTo(0), "It reached the Piper's side, not the near side of the wall.");
        }

        [Test]
        public void Creature_WithAnEnemyAwake_GoesToFightIt()
        {
            Build(Corridor(12));
            _battle.AddCreature(Creature(new FighterStats(1000f, 5f, 3f, 0.5f, 0.5f)));
            Minion minion = _battle.AddMinion(Minion(Statue, detectTiles: 30f), Cell(3));

            Run(4f);

            Assert.That(Lost(minion), Is.GreaterThan(0f));
        }

        [Test]
        public void Creature_PastTheLeash_DoesNotGoLookingForAFight()
        {
            Build(Corridor(30));
            _piper.MoveTo(14);
            Creature creature = _battle.AddCreature(Creature(Sturdy));
            _battle.AddMinion(Minion(Statue, detectTiles: 30f), Cell(19));
            Run(0.2f);
            float start = creature.Position.x;

            _piper.MoveTo(0);
            Run(1f);

            Assert.That(creature.Position.x, Is.LessThan(start - 2f));
        }

        // A minion that hits from a distance once wore creatures down forever, because each one
        // turned back at the leash just short of it, and the fight could never end.
        [Test]
        public void Creature_PastTheLeash_StillFightsAMinionBesideIt()
        {
            Build(Corridor(30));
            _piper.MoveTo(10);
            _battle.AddCreature(Creature(new FighterStats(5000f, 5f, 3f, 0.5f, 0.5f)));
            Minion minion = _battle.AddMinion(Minion(new FighterStats(1000f, 0f, 1f, 0.5f, 4f), detectTiles: 30f), Cell(11));

            _piper.MoveTo(0);
            Run(3f);

            Assert.That(Lost(minion), Is.GreaterThan(0f));
        }

        [Test]
        public void Killing_AMinion_ReportsItOnceAndRemovesIt()
        {
            Build(Corridor(12));
            var killed = new List<Minion>();
            _battle.MinionKilled += killed.Add;
            _battle.AddCreature(Creature(new FighterStats(1000f, 4f, 50f, 0.5f, 1f)));
            Minion minion = _battle.AddMinion(Minion(new FighterStats(5f, 0f, 0f, 100f, 1f)), Cell(0));

            Run(1f);

            Assert.That(killed, Is.EqualTo(new[] { minion }));
            Assert.That(_battle.Minions, Is.Empty);
        }

        [Test]
        public void IsInBattle_WhenTheLastAwakeMinionDies_Ends()
        {
            Build(Corridor(12));
            _battle.AddCreature(Creature(new FighterStats(1000f, 4f, 50f, 0.5f, 1f)));
            _battle.AddMinion(Minion(new FighterStats(120f, 0f, 0f, 100f, 1f)), Cell(0));
            Run(0.1f);
            Assert.That(_battle.IsInBattle, Is.True, "The fight has to start for this test to mean anything.");

            Run(2f);

            Assert.That(_battle.IsInBattle, Is.False);
        }

        [Test]
        public void HasRoomForACreature_AtTheMost_IsFalse()
        {
            Build(Corridor(12));

            for (int creature = 0; creature < _config.MaxCreatures; creature++)
            {
                Assert.That(_battle.HasRoomForACreature, Is.True);
                _battle.AddCreature(Creature(Sturdy));
            }

            Assert.That(_battle.HasRoomForACreature, Is.False);
        }

        [Test]
        public void Fighters_StandingOnEachOther_SpreadOut()
        {
            Build(Corridor(12));
            Creature first = _battle.AddCreature(Creature(Sturdy));
            Creature second = _battle.AddCreature(Creature(Sturdy));

            Run(1f);

            Assert.That(Vector2.Distance(first.Position, second.Position), Is.GreaterThan(0.5f));
        }

        [Test]
        public void Retreat_DuringAFight_ClearsCreaturesAndCalmsMinions()
        {
            Build(Corridor(12));
            _battle.AddCreature(Creature(Sturdy));
            Minion minion = _battle.AddMinion(Minion(Statue), Cell(1));
            Run(0.3f);

            _battle.Retreat();

            Assert.That(_battle.Creatures, Is.Empty);
            Assert.That(minion.IsAwake, Is.False);
            Assert.That(_battle.IsInBattle, Is.False);
        }
    }
}
