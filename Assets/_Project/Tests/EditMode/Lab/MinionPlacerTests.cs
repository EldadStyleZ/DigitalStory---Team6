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
    public sealed class MinionPlacerTests
    {
        private const float DepthTolerance = 0.05f;

        private static readonly MazeShape Shape = new MazeShape(30, 30, 3, 1, 0f);

        private Maze _maze;
        private StepField _fromEntrance;
        private Battle _battle;
        private PopulationPlan _plan;
        private IReadOnlyList<Ambush> _ambushes;

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
            _ambushes = MinionPlacer.Populate(battle, _maze, plan, new Mulberry32Random(seed));
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
            foreach (Minion minion in _battle.Minions.Where(minion => !minion.IsBoss && minion.Gang == null))
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

            Assert.That(battle.Minions.All(minion => minion.IsBoss || minion.Gang != null), Is.True);
            Assert.That(_ambushes, Is.Empty);
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

        private PopulationPlan GangPlan(int hiddenCount, params string[] members)
        {
            var minions = new[] { new MinionType("Imp", 8f, 1, 0f, 1f, Sturdy, default), new MinionType("Brute", 8f, 1, 0f, 1f, Sturdy, default) };
            var gang = new GangType("Test gang", members, hiddenCount, 0f, 1f);
            return new PopulationPlan(minions, Array.Empty<BossPlacement>(), 0f, 0, new[] { gang }, 0.1f);
        }

        [Test]
        public void Populate_AGang_StandsTogetherInOneCell()
        {
            Battle battle = Place(GangPlan(0, "Imp", "Imp", "Brute"), 2u);
            MinionGang gang = battle.Minions[0].Gang;

            Assert.That(battle.Minions.Count % 3, Is.EqualTo(0));
            Assert.That(gang.Members.Count, Is.EqualTo(3));
            Assert.That(gang.Members.Select(minion => _maze.CellAt(minion.Position)).Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Populate_AGang_StandsOnTheFloorApartFromEachOther()
        {
            Battle battle = Place(GangPlan(0, "Imp", "Imp", "Imp", "Brute"), 2u);

            foreach (Minion minion in battle.Minions)
            {
                Assert.That(_maze.IsFloor(Mathf.FloorToInt(minion.Position.x), Mathf.FloorToInt(minion.Position.y)), Is.True);
            }

            Vector2[] places = battle.Minions[0].Gang.Members.Select(minion => minion.Position).ToArray();
            Assert.That(places.Distinct().Count(), Is.EqualTo(places.Length));
        }

        [Test]
        public void Populate_AGangOfTheRightKinds_UsesTheNamedMinions()
        {
            Battle battle = Place(GangPlan(0, "Imp", "Brute"), 2u);

            Assert.That(battle.Minions[0].Gang.Members.Select(minion => minion.Type.Name), Is.EquivalentTo(new[] { "Imp", "Brute" }));
        }

        [Test]
        public void Populate_AGangNamingAnUnknownMinion_FailsWithAClearError()
        {
            var error = Assert.Throws<InvalidOperationException>(() => Place(GangPlan(0, "Imp", "Dragon"), 2u));

            Assert.That(error.Message, Does.Contain("Dragon").And.Contain("Test gang"));
        }

        [Test]
        public void Populate_NoGangFitsThisDepth_LeavesTheCellEmpty()
        {
            var minions = new[] { new MinionType("Imp", 8f, 1, 0f, 1f, Sturdy, default) };
            var deep = new GangType("Deep", new[] { "Imp", "Imp" }, 0, 0.5f, 1f);
            var plan = new PopulationPlan(minions, Array.Empty<BossPlacement>(), 0f, 0, new[] { deep }, 1f);

            Battle battle = Place(plan, 2u);

            Assert.That(battle.Minions.All(minion => DepthOf(minion) >= 0.5f - DepthTolerance), Is.True);
            Assert.That(battle.Minions, Is.Not.Empty);
        }

        [Test]
        public void Populate_HiddenMembers_StayOffTheFieldInAnAmbush()
        {
            Battle battle = Place(GangPlan(3, "Brute", "Imp", "Imp", "Imp"), 2u);

            Assert.That(battle.Minions.All(minion => minion.Type.Name == "Brute"), Is.True);
            Assert.That(_ambushes.Count, Is.EqualTo(battle.Minions.Count));
            Assert.That(_ambushes[0].Members.Select(minion => minion.Name), Is.EqualTo(new[] { "Imp", "Imp", "Imp" }));
            Assert.That(_ambushes[0].Cell, Is.EqualTo(_maze.CellAt(battle.Minions[0].Position)));
        }

        [Test]
        public void Populate_AWhollyHiddenGang_ShowsNothingAtAll()
        {
            Battle battle = Place(GangPlan(2, "Imp", "Brute"), 2u);

            Assert.That(battle.Minions, Is.Empty);
            Assert.That(_ambushes, Is.Not.Empty);
        }

        [Test]
        public void Populate_NoHiddenMembers_MakesNoAmbush()
        {
            Place(GangPlan(0, "Imp", "Brute"), 2u);

            Assert.That(_ambushes, Is.Empty);
        }

        [Test]
        public void Populate_ABossWithGuards_StandsInOneGangWithThem()
        {
            var minions = new[] { new MinionType("Imp", 8f, 1, 0f, 1f, Sturdy, default) };
            var bosses = new[] { new BossPlacement(Minion(Sturdy), 0.5f, "Imp", "Imp") };

            Battle battle = Place(new PopulationPlan(minions, bosses, 0f, 0), 2u);
            Minion boss = battle.Minions.Single(minion => minion.IsBoss);

            Assert.That(boss.Gang.Members.Count, Is.EqualTo(3));
            Assert.That(battle.Minions.Count, Is.EqualTo(3));
            Assert.That(boss.Position, Is.EqualTo(_maze.CenterOf(_maze.CellAt(boss.Position))));
        }

        [Test]
        public void Populate_ABossNamingAnUnknownGuard_FailsWithAClearError()
        {
            var bosses = new[] { new BossPlacement(Minion(Sturdy), 0.5f, "Dragon") };

            var error = Assert.Throws<InvalidOperationException>(() => Place(new PopulationPlan(Array.Empty<MinionType>(), bosses, 0f, 0), 2u));

            Assert.That(error.Message, Does.Contain("Dragon"));
        }

        [Test]
        public void Populate_SameSeed_HidesTheSameAmbushes()
        {
            Place(_plan, 9u);
            int[] first = _ambushes.Select(ambush => ambush.Cell).ToArray();

            Place(_plan, 9u);

            Assert.That(_ambushes.Select(ambush => ambush.Cell), Is.EqualTo(first));
            Assert.That(first, Is.Not.Empty);
        }

        [Test]
        public void TheRealPlan_HasMoreEnemiesInGangsThanAlone()
        {
            int inGangs = _battle.Minions.Count(minion => minion.Gang != null);
            int alone = _battle.Minions.Count(minion => minion.Gang == null);

            Assert.That(inGangs, Is.GreaterThan(alone));
            Assert.That(alone, Is.GreaterThan(0), "A few loners stay as bait.");
        }

        [Test]
        public void TheRealPlan_BothKindsOfAmbushAppear()
        {
            Assert.That(_plan.Gangs.Any(gang => gang.HiddenCount == gang.Members.Count), Is.True);
            Assert.That(_plan.Gangs.Any(gang => gang.HiddenCount > 0 && gang.HiddenCount < gang.Members.Count), Is.True);
        }

        [Test]
        public void TheRealData_EveryEnemyAndCreatureRoleIsPresent()
        {
            var roster = ScriptableObject.CreateInstance<CreatureRoster>();

            Assert.That(_plan.Minions.Any(minion => minion.Stats.Traits.Heals), Is.True, "An enemy healer");
            Assert.That(_plan.Minions.Count(minion => minion.Stats.Traits.StandsBack), Is.GreaterThanOrEqualTo(2), "A back line");
            Assert.That(roster.Creatures.Any(creature => creature.Stats.Traits.Heals), Is.True, "A healer");
            Assert.That(roster.Creatures.Any(creature => creature.Stats.Traits.PackBonus > 0f), Is.True, "A swarm");
            Assert.That(roster.Creatures.Any(creature => creature.Stats.Traits.HuntsBackline), Is.True, "A hunter");
            Assert.That(roster.Creatures.Any(creature => creature.Stats.Traits.SlowShare > 0f), Is.True, "A cloud");
            Assert.That(roster.Creatures.Any(creature => creature.Stats.Traits.Guards), Is.True, "A guardian");
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
