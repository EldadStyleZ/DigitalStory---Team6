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
        private AmbushConfig _ambush;
        private CreatureType _mouse;
        private CreatureType _cat;
        private BattleSong _song;
        private LabSession _session;
        private List<SummonRefusal> _refusals;
        private List<IComboSong> _summoned;

        [SetUp]
        public void CreateSession()
        {
            _stamina = ScriptableObject.CreateInstance<StaminaConfig>();
            _combos = ScriptableObject.CreateInstance<ComboConfig>();
            _levels = ScriptableObject.CreateInstance<LevelConfig>();
            _battle = ScriptableObject.CreateInstance<BattleConfig>();
            _ambush = ScriptableObject.CreateInstance<AmbushConfig>();
            _mouse = Creature(Sturdy, "Mouse", 1, 10f, 2f, H, J);
            _cat = Creature(Sturdy, "Cat", 2, 25f, 4f, K, L, H);
            _song = new BattleSong("War Song", "Boost", 1, new[] { J, K, L }, 30f, 20f, 2f, 5f);
            _session = NewSession();
            _refusals = new List<SummonRefusal>();
            _summoned = new List<IComboSong>();
            _session.SummonRefused += _refusals.Add;
            _session.ComboPlayed += _summoned.Add;
        }

        private LabSession NewSession(float signpostChance = 0f, int[] bossCells = null)
        {
            var rules = new LabRules(
                new MazeShape(16, bossCells == null ? 1 : 16, 3, 1, 0f),
                1,
                ScriptableObject.CreateInstance<LabPlayerConfig>(),
                _stamina,
                _combos,
                _levels,
                _battle,
                new[] { _mouse, _cat },
                new[] { _song },
                new PopulationPlan(Array.Empty<MinionType>(), Bosses(bossCells), 0f, 0),
                _ambush,
                signpostChance);
            return new LabSession(rules);
        }

        private static BossPlacement[] Bosses(int[] cells)
        {
            var bosses = new BossPlacement[cells == null ? 0 : cells.Length];

            for (int index = 0; index < bosses.Length; index++)
            {
                bosses[index] = new BossPlacement(Minion(Statue, detectTiles: 0f), (index + 1f) / bosses.Length);
            }

            return bosses;
        }

        private static void AssertArrowsLeadToTheNextBoss(LabSession session)
        {
            Minion boss = session.Battle.Minions[0];
            int goal = session.Maze.CellAt(boss.Position);
            var field = new StepField(session.Maze);
            field.Fill(goal);

            Assert.That(session.Signposts.Cells, Is.Not.Empty);

            foreach (int cell in session.Signposts.Cells)
            {
                int direction = session.Signposts.DirectionAt(cell);

                if (cell != goal)
                {
                    int next = session.Maze.NeighbourThrough(cell, direction);
                    Assert.That(field.StepsAt(next), Is.LessThan(field.StepsAt(cell)), $"The arrow at {cell} leads away from the boss.");
                }
            }
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
        public void Press_TheComboWithNoFightOn_SummonsTheCreature()
        {
            Play(H, J);

            Assert.That(_session.Battle.Creatures.Count, Is.EqualTo(1));
            Assert.That(_summoned, Is.EqualTo(new[] { _mouse }));
        }

        [Test]
        public void Press_TheComboWithNoFightOn_PaysForIt()
        {
            Play(H, J);

            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max - _mouse.StaminaCost).Within(Tolerance));
            Assert.That(_session.Cooldowns.IsReady(_mouse), Is.False);
        }

        [Test]
        public void Press_AWrongNoteWithNoFightOn_LosesStamina()
        {
            Play(L);

            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max - _stamina.FailedComboCost).Within(Tolerance));
        }

        [Test]
        public void Press_WithNoFightOn_ReportsTheNoteAndRemembersIt()
        {
            var notes = new List<int>();
            _session.NotePlayed += notes.Add;

            Play(H);

            Assert.That(notes, Is.EqualTo(new[] { H }));
            Assert.That(_session.Combos.Notes, Is.EqualTo(new[] { H }));
            Assert.That(_refusals, Is.Empty);
        }

        [Test]
        public void ACreatureSummonedInPeace_GoesHomeIfNoFightComes()
        {
            Play(H, J);

            Tick(_battle.QuietSecondsBeforeLeaving + 0.1f);

            Assert.That(_session.Battle.Creatures, Is.Empty);
        }

        [Test]
        public void Press_ANoteThatKeepsBuilding_ReportsIt()
        {
            int building = 0;
            _session.ComboBuilding += () => building++;

            Play(H);

            Assert.That(building, Is.EqualTo(1));
        }

        [Test]
        public void Press_TheLastNote_DoesNotReportBuilding()
        {
            int building = 0;
            Play(H);
            _session.ComboBuilding += () => building++;

            Play(J);

            Assert.That(building, Is.EqualTo(0));
        }

        [Test]
        public void Press_TheBattleSong_BoostsTheCreatures()
        {
            Play(J, K, L);

            Assert.That(_session.Battle.IsBoosted, Is.True);
            Assert.That(_session.Battle.BoostSecondsLeft, Is.EqualTo(_song.Seconds).Within(Tolerance));
            Assert.That(_summoned, Is.EqualTo(new[] { _song }));
        }

        [Test]
        public void Press_TheBattleSong_PaysForItAndCools()
        {
            Play(J, K, L);

            Assert.That(_session.Stamina.Current, Is.EqualTo(_stamina.Max - _song.StaminaCost).Within(Tolerance));
            Assert.That(_session.Cooldowns.SecondsLeft(_song), Is.EqualTo(_song.CooldownSeconds));
        }

        [Test]
        public void Press_TheBattleSongWithNoRoomForCreatures_StillPlays()
        {
            for (int creature = 0; creature < _battle.MaxCreatures; creature++)
            {
                _session.Battle.AddCreature(_mouse);
            }

            Play(J, K, L);

            Assert.That(_session.Battle.IsBoosted, Is.True);
        }

        [Test]
        public void Press_TheBattleSongWhileItCools_DoesNotPlay()
        {
            Play(J, K, L);
            float afterFirst = _session.Stamina.Current;
            Tick(_song.Seconds + 0.1f);
            float recovered = _session.Stamina.Current;

            Play(J, K, L);

            Assert.That(_session.Battle.IsBoosted, Is.False);
            Assert.That(_session.Stamina.Current, Is.EqualTo(recovered).Within(Tolerance));
            Assert.That(recovered, Is.GreaterThanOrEqualTo(afterFirst));
            Assert.That(_refusals, Is.EqualTo(new[] { SummonRefusal.CoolingDown }));
        }

        [Test]
        public void Songs_ListEveryComboByUnlockLevel()
        {
            Assert.That(_session.Songs, Is.EqualTo(new IComboSong[] { _mouse, _song, _cat }));
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
        public void BattleEnding_MidCombo_LeavesTheComboGoing()
        {
            StartBattle();
            Play(H);

            _session.Piper.MoveTo(15);
            Tick(0.2f);
            Tick(0.2f);

            Assert.That(_session.Battle.IsInBattle, Is.False);
            Assert.That(_session.Combos.Notes, Is.EqualTo(new[] { H }));
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
        public void Signposts_AtTheStart_PointToTheShallowestBoss()
        {
            LabSession session = NewSession(signpostChance: 1f, bossCells: new[] { 5, 10 });

            AssertArrowsLeadToTheNextBoss(session);
        }

        [Test]
        public void Signposts_WhenABossFalls_PointToTheNext()
        {
            LabSession session = NewSession(signpostChance: 1f, bossCells: new[] { 5, 10 });
            Minion first = session.Battle.Minions[0];

            first.Health.Damage(first.Health.Max);
            session.Tick(Vector2.zero, Step);

            AssertArrowsLeadToTheNextBoss(session);
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
        public void Continue_DuringAnAmbushWarning_CallsItOff()
        {
            _session.Ambushes.Add(new Ambush(Maze.Entrance, new[] { Minion(Statue) }));
            Tick();
            Tick();
            Tick(_ambush.DelaySeconds + 0.1f);

            _session.Continue();
            Tick(_ambush.WarningSeconds + 0.1f);

            Assert.That(_session.Battle.Minions, Is.Empty);
        }

        [Test]
        public void Continue_EndsTheBoost()
        {
            Play(J, K, L);

            _session.Continue();

            Assert.That(_session.Battle.IsBoosted, Is.False);
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
