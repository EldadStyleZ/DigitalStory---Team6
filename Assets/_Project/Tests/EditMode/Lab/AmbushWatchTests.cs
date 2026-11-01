using System;
using System.Collections.Generic;
using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class AmbushWatchTests
    {
        private const float StepSeconds = 1f / 30f;

        private static readonly FighterStats Statue = new FighterStats(1000f, 0f, 0f, 1000f, 1f);

        private Maze _maze;
        private AmbushConfig _config;
        private LabPiper _piper;
        private Battle _battle;
        private AmbushWatch _watch;
        private List<Ambush> _ambushes;
        private List<Vector2> _warnings;
        private List<Vector2> _arrivals;

        private void Build(Maze maze, params int[] ambushCells)
        {
            _maze = maze;
            _config = ScriptableObject.CreateInstance<AmbushConfig>();
            _piper = new LabPiper(ScriptableObject.CreateInstance<LabPlayerConfig>(), maze);
            _battle = new Battle(maze, ScriptableObject.CreateInstance<BattleConfig>(), _piper, new Health(100f));
            MinionType hidden = Minion(Statue, detectTiles: 60f);
            _ambushes = ambushCells.Select(cell => new Ambush(cell, new[] { hidden, hidden, hidden })).ToList();
            _watch = new AmbushWatch(maze, _config, _battle, _ambushes);
            _warnings = new List<Vector2>();
            _arrivals = new List<Vector2>();
            _watch.Warned += _warnings.Add;
            _watch.Sprang += _arrivals.Add;
        }

        private void Run(float seconds)
        {
            for (float passed = 0f; passed < seconds; passed += StepSeconds)
            {
                _battle.Tick(StepSeconds);
                _watch.Tick(StepSeconds);
            }
        }

        private void RunToTheWarning() => Run(_config.DelaySeconds + (StepSeconds * 3f));

        private void RunToTheArrival() => Run(_config.DelaySeconds + _config.WarningSeconds + (StepSeconds * 4f));

        [Test]
        public void Constructor_NoAmbushes_FailsNothing()
        {
            Build(Corridor(4));

            Assert.DoesNotThrow(() => Run(1f));
        }

        [Test]
        public void Tick_ThePiperFarAway_LeavesItAsleep()
        {
            Build(Corridor(12), 11);

            Run(5f);

            Assert.That(_ambushes[0].State, Is.EqualTo(AmbushState.Dormant));
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void Tick_ThePiperClose_DoesNotWarnStraightAway()
        {
            Build(Corridor(12), 2);

            Run(_config.DelaySeconds - 0.3f);

            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void Tick_ThePiperClose_WarnsAfterTheDelay()
        {
            Build(Corridor(12), 2);

            RunToTheWarning();

            Assert.That(_warnings.Count, Is.EqualTo(1));
            Assert.That(_ambushes[0].State, Is.EqualTo(AmbushState.Warning));
        }

        [Test]
        public void Tick_DuringTheWarning_NoEnemyExistsYet()
        {
            Build(Corridor(12), 2);

            RunToTheWarning();

            Assert.That(_battle.Minions, Is.Empty);
        }

        [Test]
        public void Tick_TheWarning_LastsItsWholeTime()
        {
            Build(Corridor(12), 2);
            RunToTheWarning();

            Run(_config.WarningSeconds - (StepSeconds * 5f));

            Assert.That(_battle.Minions, Is.Empty);
        }

        [Test]
        public void Tick_AfterTheWarning_TheEnemiesAppear()
        {
            Build(Corridor(12), 2);

            RunToTheArrival();

            Assert.That(_battle.Minions.Count, Is.EqualTo(3));
            Assert.That(_arrivals.Count, Is.EqualTo(1));
            Assert.That(_ambushes[0].State, Is.EqualTo(AmbushState.Sprung));
        }

        [Test]
        public void Tick_TheEnemiesAppearing_AreAwakeAtOnce()
        {
            Build(Corridor(12), 2);

            RunToTheArrival();

            Assert.That(_battle.Minions.All(minion => minion.IsAwake), Is.True);
            Assert.That(_battle.IsInBattle, Is.True);
        }

        [Test]
        public void Tick_TheEnemiesAppearing_ComeFromBehindThePiper()
        {
            Build(Corridor(12), 7);
            _piper.MoveTo(5);

            RunToTheArrival();

            Vector2 expected = _maze.CenterOf(5 - _config.EmergeSteps);
            Assert.That(_arrivals[0], Is.EqualTo(expected));
            Assert.That(_battle.Minions.All(minion => _maze.CellAt(minion.Position) == 5 - _config.EmergeSteps), Is.True);
        }

        [Test]
        public void Tick_ThePiperInADeadEnd_TheyAppearOnTheOnlyWayIn()
        {
            Build(Corridor(12), 2);

            RunToTheArrival();

            Assert.That(_maze.CellAt(_arrivals[0]), Is.EqualTo(_config.EmergeSteps));
        }

        [Test]
        public void Tick_TheWarning_ShowsWhereTheyWillAppear()
        {
            Build(Corridor(12), 7);
            _piper.MoveTo(5);

            RunToTheArrival();

            Assert.That(_warnings[0], Is.EqualTo(_arrivals[0]));
        }

        [Test]
        public void Tick_TheEnemiesAppearing_StandApartOnTheFloor()
        {
            Build(Corridor(12), 2);

            RunToTheArrival();

            Vector2[] places = _battle.Minions.Select(minion => minion.Position).ToArray();
            Assert.That(places.Distinct().Count(), Is.EqualTo(places.Length));
            Assert.That(places.All(place => _maze.IsFloor(Mathf.FloorToInt(place.x), Mathf.FloorToInt(place.y))), Is.True);
        }

        [Test]
        public void Tick_AfterItSprang_NeverComesAgain()
        {
            Build(Corridor(12), 2);

            RunToTheArrival();
            Run(_config.RestSeconds + 5f);

            Assert.That(_battle.Minions.Count, Is.EqualTo(3));
            Assert.That(_warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void Tick_TwoAmbushesInReach_TheSecondWaitsForTheRest()
        {
            Build(Corridor(12), 2, 2);

            RunToTheArrival();
            Run(_config.RestSeconds - 1f);

            Assert.That(_battle.Minions.Count, Is.EqualTo(3));
        }

        [Test]
        public void Tick_AfterTheRest_TheSecondCanCome()
        {
            Build(Corridor(12), 2, 2);

            RunToTheArrival();
            Run(_config.RestSeconds + _config.DelaySeconds + _config.WarningSeconds + 1f);

            Assert.That(_battle.Minions.Count, Is.EqualTo(6));
        }

        [Test]
        public void Calm_DuringTheWarning_PutsItBack()
        {
            Build(Corridor(12), 2);
            RunToTheWarning();

            _watch.Calm();

            Assert.That(_ambushes[0].State, Is.EqualTo(AmbushState.Dormant));
            Assert.That(_battle.Minions, Is.Empty);
        }

        [Test]
        public void Calm_DuringTheWarning_LetsItComeAgain()
        {
            Build(Corridor(12), 2);
            RunToTheWarning();
            _watch.Calm();

            RunToTheArrival();

            Assert.That(_battle.Minions.Count, Is.EqualTo(3));
        }

        [Test]
        public void Calm_AfterItSprang_LeavesItSpent()
        {
            Build(Corridor(12), 2);
            RunToTheArrival();

            _watch.Calm();

            Assert.That(_ambushes[0].State, Is.EqualTo(AmbushState.Sprung));
        }

        [Test]
        public void Constructor_NoConfig_FailsLoudly()
        {
            Build(Corridor(4));

            Assert.Throws<ArgumentNullException>(() => new AmbushWatch(_maze, null, _battle, Array.Empty<Ambush>()));
        }
    }
}
