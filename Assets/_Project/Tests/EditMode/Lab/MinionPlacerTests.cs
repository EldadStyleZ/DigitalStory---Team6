using System;
using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class MinionPlacerTests
    {
        private const float DepthTolerance = 0.05f;

        private static readonly MazeShape Shape = new MazeShape(30, 30, 3, 1, 0f);

        private Maze _maze;
        private StepField _fromEntrance;
        private Battle _battle;
        private PopulationPlan _plan;

        [SetUp]
        public void Populate()
        {
            _plan = ScriptableObject.CreateInstance<PopulationConfig>().Plan;
            _battle = Place(_plan, 3u);
        }

        private Battle Place(PopulationPlan plan, uint seed)
        {
            _maze = MazeGenerator.Generate(Shape, new Mulberry32Random(3u));
            _fromEntrance = new StepField(_maze);
            _fromEntrance.Fill(_maze.Entrance);

            var player = ScriptableObject.CreateInstance<LabPlayerConfig>();
            var battle = new Battle(_maze, ScriptableObject.CreateInstance<BattleConfig>(), new LabPiper(player, _maze), new Health(100f));
            MinionPlacer.Populate(battle, _maze, plan, new Mulberry32Random(seed));
            return battle;
        }

        private float DepthOf(Minion minion)
        {
            return _fromEntrance.StepsAt(_maze.CellAt(minion.Position)) / (float)_fromEntrance.Furthest;
        }

        [Test]
        public void Populate_NearTheEntrance_LeavesTheFirstCellsEmpty()
        {
            foreach (Minion minion in _battle.Minions)
            {
                Assert.That(_fromEntrance.StepsAt(_maze.CellAt(minion.Position)), Is.GreaterThanOrEqualTo(_plan.SafeCells));
            }
        }

        [Test]
        public void Populate_OrdinaryMinions_StayInsideTheirDepths()
        {
            foreach (Minion minion in _battle.Minions.Where(minion => !minion.IsBoss))
            {
                Assert.That(DepthOf(minion), Is.InRange(minion.Type.FirstDepth, minion.Type.LastDepth), minion.Type.Name);
            }
        }

        [Test]
        public void Populate_FurtherIn_MinionsAreStronger()
        {
            Minion[] ordinary = _battle.Minions.Where(minion => !minion.IsBoss).ToArray();
            double shallow = ordinary.Where(minion => DepthOf(minion) < 0.33f).Average(minion => minion.Stats.MaxHealth);
            double deep = ordinary.Where(minion => DepthOf(minion) > 0.66f).Average(minion => minion.Stats.MaxHealth);

            Assert.That(deep, Is.GreaterThan(shallow));
        }

        [Test]
        public void Populate_TheRealPlan_HasSeveralBossesThroughTheMaze()
        {
            Assert.That(_plan.Bosses.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(_battle.Minions.Count(minion => minion.IsBoss), Is.EqualTo(_plan.Bosses.Count));
        }

        [Test]
        public void Populate_EachBoss_WaitsAtItsDepth()
        {
            foreach (BossPlacement placement in _plan.Bosses)
            {
                Minion boss = _battle.Minions.Single(minion => minion.Type == placement.Boss);

                Assert.That(DepthOf(boss), Is.EqualTo(placement.Depth).Within(DepthTolerance), placement.Boss.Name);
            }
        }

        [Test]
        public void Populate_TheLastBoss_IsTheFinalOne_AtTheDeepestCell()
        {
            Minion[] finals = _battle.Minions.Where(minion => minion.IsFinalBoss).ToArray();

            Assert.That(finals.Length, Is.EqualTo(1));
            Assert.That(finals[0].Type, Is.SameAs(_plan.Bosses[_plan.Bosses.Count - 1].Boss));
            Assert.That(_fromEntrance.StepsAt(_maze.CellAt(finals[0].Position)), Is.EqualTo(_fromEntrance.Furthest));
        }

        [Test]
        public void Populate_SameSeed_PutsEveryoneInTheSamePlace()
        {
            Vector2[] first = Place(_plan, 9u).Minions.Select(minion => minion.Position).ToArray();
            Vector2[] second = Place(_plan, 9u).Minions.Select(minion => minion.Position).ToArray();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void Populate_NoChance_PlacesOnlyTheBosses()
        {
            var empty = new PopulationPlan(_plan.Minions, _plan.Bosses, 0f, 0);

            Battle battle = Place(empty, 1u);

            Assert.That(battle.Minions.All(minion => minion.IsBoss), Is.True);
        }

        [Test]
        public void Populate_NobodyFitsThisDepth_LeavesTheCellEmpty()
        {
            var shallowOnly = new[] { Minion(Sturdy, firstDepth: 0f, lastDepth: 0.1f) };
            var plan = new PopulationPlan(shallowOnly, Array.Empty<BossPlacement>(), 1f, 0);

            Battle battle = Place(plan, 1u);

            Assert.That(battle.Minions.All(minion => DepthOf(minion) <= 0.1f), Is.True);
            Assert.That(battle.Minions, Is.Not.Empty);
        }

        [Test]
        public void TheRealData_SomeMinionsAndSomeCreaturesHitAnArea()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();

            Assert.That(_plan.Minions.Any(minion => minion.Stats.HitsAnArea), Is.True);
            Assert.That(_plan.Minions.Any(minion => !minion.Stats.HitsAnArea), Is.True);
            Assert.That(roster.Creatures.Any(creature => creature.Stats.HitsAnArea), Is.True);
            Assert.That(roster.Creatures.Any(creature => !creature.Stats.HitsAnArea), Is.True);
        }
    }
}
