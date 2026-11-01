using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using NUnit.Framework;
using UnityEngine;
using static DigitalStory.Tests.EditMode.LabTestData;

namespace DigitalStory.Tests.EditMode
{
    public sealed class LabSessionTests
    {
        private const float Tolerance = 0.001f;
        private const float Step = 1f / 30f;

        private static readonly FighterStats Statue = new FighterStats(10000f, 0f, 0f, 1000f, 1f);

        private StaminaConfig _stamina;
        private ComboConfig _combos;
        private LevelConfig _levels;
        private BattleConfig _battle;
        private CreatureType _mouse;
        private CreatureType _cat;
        private LabSession _session;
        private List<SummonRefusal> _refusals;
        private List<CreatureType> _summoned;

        [SetUp]
        public void CreateSession()
        {
            _stamina = ScriptableObject.CreateInstance<StaminaConfig>();
            _combos = ScriptableObject.CreateInstance<ComboConfig>();
            _levels = ScriptableObject.CreateInstance<LevelConfig>();
            _battle = ScriptableObject.CreateInstance<BattleConfig>();
            _mouse = Creature(Sturdy, "Mouse", 1, 10f, 2f, H, J);
            _cat = Creature(Sturdy, "Cat", 2, 25f, 4f, K, L, H);
            _session = NewSession();
            _refusals = new List<SummonRefusal>();
            _summoned = new List<CreatureType>();
            _session.SummonRefused += _refusals.Add;
            _session.Summoned += _summoned.Add;
        }

        private LabSession NewSession(float staminaMax = 0f)
        {
            var rules = new LabRules(
                new MazeShape(16, 1, 3, 1, 0f),
                1,
                ScriptableObject.CreateInstance<LabPlayerConfig>(),
                _stamina,
                _combos,
                _levels,
                _battle,
                new[] { _mouse, _cat },
                new PopulationPlan(Array.Empty<MinionType>(), Array.Empty<BossPlacement>(), 0f, 0));
            return new LabSession(rules);
        }

        private Maze Maze => _session.Maze;

        private Minion StartBattle()
        {
            Minion minion = _session.Battle.AddMinion(Minion(Statue, detectTiles: 8f), Maze.CenterOf(1));
            Tick();
            Assert.That(_session.Battle.IsInBattle, Is.True, "The test needs a fight.");
            return minion;
        }

        private void Tick(float seconds = Step) => _session.Tick(Vector2.zero, seconds);

        private void Play(params int[] notes)
        {
            foreach (int note in notes)
            {
                _session.Press(note);
            }
        }

        [Test]
        public void Press_TheComboOutsideABattle_SummonsNothing()
        {
            Play(H, J);

            Assert.That(_session.Battle.Creatures, Is.Empty);
            Assert.That(_summoned, Is.Empty);
        }

        [Test]
        public void Press_OutsideABattle_CostsNothing()
        {
            Play(L, L, H, J);

            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max));
            Assert.That(_session.Cooldowns.IsReady(_mouse), Is.True);
        }

        [Test]
        public void Press_OnlyInABattle_ReportsTheNote()
        {
            var notes = new List<int>();
            _session.NotePlayed += notes.Add;

            Play(K);
            StartBattle();
            Play(H);

            Assert.That(notes, Is.EqualTo(new[] { H }));
        }

        [Test]
        public void Press_OutsideABattle_RemembersNoNotes()
        {
            Play(H);

            Assert.That(_session.Combos.Notes, Is.Empty);
        }

        [Test]
        public void Press_OutsideABattle_SaysWhyNothingHappened()
        {
            Play(H);

            Assert.That(_refusals, Is.EqualTo(new[] { SummonRefusal.NotInBattle }));
        }

        [Test]
        public void Press_TheComboInABattle_SummonsTheCreature()
        {
            StartBattle();

            Play(H, J);

            Assert.That(_session.Battle.Creatures.Count, Is.EqualTo(1));
            Assert.That(_session.Battle.Creatures[0].Type, Is.SameAs(_mouse));
            Assert.That(_summoned, Is.EqualTo(new[] { _mouse }));
        }

        [Test]
        public void Press_TheComboInABattle_SpendsItsStamina()
        {
            StartBattle();

            Play(H, J);

            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max - _mouse.StaminaCost).Within(Tolerance));
        }

        [Test]
        public void Press_TheComboInABattle_StartsItsCooldown()
        {
            StartBattle();

            Play(H, J);

            Assert.That(_session.Cooldowns.SecondsLeft(_mouse), Is.EqualTo(_mouse.CooldownSeconds));
        }

        [Test]
        public void Press_AWrongNoteInABattle_LosesStaminaButStartsNoCooldown()
        {
            StartBattle();

            Play(L);

            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max - _stamina.FailedComboCost).Within(Tolerance));
            Assert.That(_session.Cooldowns.IsReady(_mouse), Is.True);
            Assert.That(_session.Battle.Creatures, Is.Empty);
        }

        [Test]
        public void Tick_PausingTooLongMidCombo_LosesStamina()
        {
            StartBattle();
            Play(H);

            Tick(_combos.SecondsToNextNote + 0.01f);

            float recovered = _stamina.RecoveryPerSecond * (_combos.SecondsToNextNote + 0.01f);
            Assert.That(_session.Combos.Notes, Is.Empty);
            Assert.That(_session.Stamina.Current, Is.LessThanOrEqualTo(_stamina.Max - _stamina.FailedComboCost + recovered + Tolerance));
        }

        [Test]
        public void Press_TheComboWhileItCools_SummonsNothingAndCostsNothing()
        {
            StartBattle();
            Play(H, J);
            float afterFirst = _session.Stamina.Current;

            Play(H, J);

            Assert.That(_session.Battle.Creatures.Count, Is.EqualTo(1));
            Assert.That(_session.Stamina.Current, Is.EqualTo(afterFirst));
            Assert.That(_refusals, Is.EqualTo(new[] { SummonRefusal.CoolingDown }));
        }

        [Test]
        public void Press_TheComboTooTired_SummonsNothing()
        {
            StartBattle();
            _session.Stamina.Lose(_stamina.Max);

            Play(H, J);

            Assert.That(_session.Battle.Creatures, Is.Empty);
            Assert.That(_refusals, Is.EqualTo(new[] { SummonRefusal.TooTired }));
            Assert.That(_session.Cooldowns.IsReady(_mouse), Is.True);
        }

        [Test]
        public void Press_TheComboWithNoRoom_SummonsNothing()
        {
            StartBattle();

            for (int creature = 0; creature < _battle.MaxCreatures; creature++)
            {
                _session.Battle.AddCreature(_mouse);
            }

            Play(H, J);

            Assert.That(_session.Battle.Creatures.Count, Is.EqualTo(_battle.MaxCreatures));
            Assert.That(_refusals, Is.EqualTo(new[] { SummonRefusal.NoRoom }));
            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max));
        }

        [Test]
        public void BattleEnding_MidCombo_DropsItForFree()
        {
            StartBattle();
            Play(H);

            _session.Piper.MoveTo(15);
            Tick(0.5f);
            Tick(_combos.SecondsToNextNote + 1f);

            Assert.That(_session.Battle.IsInBattle, Is.False);
            Assert.That(_session.Combos.Notes, Is.Empty);
            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max));
        }

        [Test]
        public void Killing_AMinion_GivesItsExperience()
        {
            Minion minion = StartBattle();

            minion.Health.Damage(minion.Health.Max);
            Tick();

            Assert.That(_session.Levels.Xp, Is.EqualTo(minion.Type.XpReward));
        }

        [Test]
        public void LevellingUp_UnlocksTheNextCombo()
        {
            StartBattle();

            _session.Levels.Add(_levels.XpToReach[1]);
            Play(K, L, H);

            Assert.That(_summoned, Is.EqualTo(new[] { _cat }));
        }

        [Test]
        public void LevellingUp_RefillsStaminaAndHealth()
        {
            _session.Stamina.Lose(50f);
            _session.PiperHealth.Damage(50f);

            _session.Levels.Add(_levels.XpToReach[1]);

            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max));
            Assert.That(_session.PiperHealth.Current, Is.EqualTo(_session.PiperHealth.Max));
        }

        [Test]
        public void Dying_ReportsItOnce()
        {
            int deaths = 0;
            _session.PiperDied += () => deaths++;

            _session.PiperHealth.Damage(_session.PiperHealth.Max);
            Tick();
            Tick();

            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void Continue_KeepsLevelAndExperience()
        {
            _session.Levels.Add(_levels.XpToReach[2] + 3);
            _session.PiperHealth.Damage(_session.PiperHealth.Max);
            Tick();

            _session.Continue();

            Assert.That(_session.Levels.Level, Is.EqualTo(3));
            Assert.That(_session.Levels.Xp, Is.EqualTo(_levels.XpToReach[2] + 3));
        }

        [Test]
        public void Continue_RestoresHealthStaminaAndCooldowns()
        {
            StartBattle();
            Play(H, J);
            _session.PiperHealth.Damage(_session.PiperHealth.Max);
            Tick();

            _session.Continue();

            Assert.That(_session.PiperHealth.Current, Is.EqualTo(_session.PiperHealth.Max));
            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max));
            Assert.That(_session.Cooldowns.IsReady(_mouse), Is.True);
        }

        [Test]
        public void Continue_WithNoBossBeaten_ReturnsToTheEntrance()
        {
            _session.Piper.MoveTo(9);
            _session.PiperHealth.Damage(_session.PiperHealth.Max);
            Tick();

            _session.Continue();

            Assert.That(_session.Piper.Position, Is.EqualTo(Maze.CenterOf(Maze.Entrance)));
        }

        [Test]
        public void Continue_AfterBeatingABoss_StartsWhereItFell()
        {
            Minion boss = _session.Battle.AddMinion(Minion(Statue, detectTiles: 8f), Maze.CenterOf(5), MinionRank.Boss);
            boss.Health.Damage(boss.Health.Max);
            Tick();
            _session.PiperHealth.Damage(_session.PiperHealth.Max);
            Tick();

            _session.Continue();

            Assert.That(_session.Piper.Position, Is.EqualTo(Maze.CenterOf(5)));
        }

        [Test]
        public void Continue_LeavesKilledMinionsDead_AndCreaturesGone()
        {
            Minion killed = StartBattle();
            Minion other = _session.Battle.AddMinion(Minion(Statue), Maze.CenterOf(12));
            Play(H, J);
            killed.Health.Damage(killed.Health.Max);
            Tick();
            _session.PiperHealth.Damage(_session.PiperHealth.Max);
            Tick();

            _session.Continue();

            Assert.That(_session.Battle.Minions, Is.EqualTo(new[] { other }));
            Assert.That(_session.Battle.Creatures, Is.Empty);
        }

        [Test]
        public void BeatingTheFinalBoss_WinsTheGame()
        {
            int wins = 0;
            _session.Won += () => wins++;
            Minion boss = _session.Battle.AddMinion(Minion(Statue), Maze.CenterOf(5), MinionRank.FinalBoss);

            boss.Health.Damage(boss.Health.Max);
            Tick();

            Assert.That(wins, Is.EqualTo(1));
        }

        [Test]
        public void BeatingAnEarlierBoss_DoesNotWinTheGame()
        {
            int wins = 0;
            _session.Won += () => wins++;
            Minion boss = _session.Battle.AddMinion(Minion(Statue), Maze.CenterOf(5), MinionRank.Boss);

            boss.Health.Damage(boss.Health.Max);
            Tick();

            Assert.That(wins, Is.EqualTo(0));
        }
    }
}
