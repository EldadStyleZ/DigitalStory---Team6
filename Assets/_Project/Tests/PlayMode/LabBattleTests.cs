using System.Collections;
using System.Linq;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class LabBattleTests
    {
        private const string SceneName = "eldad-lab-scene";
        private const int H = 0;
        private const int J = 1;

        private static readonly FighterLook Plain = new FighterLook(Color.red, 1f, FighterShape.Rat);
        private static readonly FighterStats Statue = new FighterStats(5000f, 0f, 0f, 1000f, 1f);

        private LabRoot _root;
        private LabUnitsView _units;
        private LabEffectsView _effects;
        private LabHudView _hud;
        private LabScreensView _screens;
        private LabSoundView _sound;
        private LabSoundConfig _soundConfig;
        private LabTextConfig _texts;

        private LabSession Session => _root.Session;

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            _root = Object.FindAnyObjectByType<LabRoot>();
            _units = Object.FindAnyObjectByType<LabUnitsView>();
            _effects = Object.FindAnyObjectByType<LabEffectsView>();
            _hud = Object.FindAnyObjectByType<LabHudView>();
            _screens = Object.FindAnyObjectByType<LabScreensView>();
            _sound = Object.FindAnyObjectByType<LabSoundView>();
            _soundConfig = Resources.FindObjectsOfTypeAll<LabSoundConfig>()[0];
            _texts = Resources.FindObjectsOfTypeAll<LabTextConfig>()[0];
        }

        private Minion StartBattle(FighterStats stats, float xp = 10f)
        {
            var type = new MinionType("Test", 12f, (int)xp, 0f, 1f, stats, Plain);
            int next = Session.Maze.Entrance;

            for (int direction = 0; direction < Maze.DirectionCount && next == Session.Maze.Entrance; direction++)
            {
                int cell = Session.Maze.NeighbourThrough(Session.Maze.Entrance, direction);
                next = cell != Maze.NoCell ? cell : next;
            }

            Minion minion = Session.Battle.AddMinion(type, Session.Maze.CenterOf(next));
            Session.Tick(Vector2.zero, 0.01f);
            Assert.That(Session.Battle.IsInBattle, Is.True, "The test needs a fight.");
            return minion;
        }

        private void Press(string buttonName)
        {
            Button button = _screens.Card.Q<Button>(buttonName);
            Assert.That(button, Is.Not.Null, $"There is no {buttonName} on screen.");

            using (NavigationSubmitEvent press = NavigationSubmitEvent.GetPooled())
            {
                press.target = button;
                button.SendEvent(press);
            }
        }

        [Test]
        public void Scene_WhenLoaded_HasMinionsAndSeveralBosses()
        {
            Assert.That(Session.Battle.Minions.Count, Is.GreaterThan(100));
            Assert.That(Session.Battle.Minions.Count(minion => minion.IsBoss), Is.GreaterThanOrEqualTo(3));
            Assert.That(Session.Battle.Minions.Count(minion => minion.IsFinalBoss), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Scene_AtTheEntrance_IsCalm()
        {
            yield return new WaitForSeconds(0.3f);

            Assert.That(Session.Battle.IsInBattle, Is.False);
        }

        [UnityTest]
        public IEnumerator Units_EveryFighter_IsDrawn()
        {
            StartBattle(Statue);
            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(_units.DrawnCount, Is.EqualTo(Session.Battle.Minions.Count + Session.Battle.Creatures.Count));
            Assert.That(Session.Battle.Creatures.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Effects_WhenACreatureArrives_AreShown()
        {
            StartBattle(Statue);

            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(_effects.ActiveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Effects_WhenTheyFinish_LeaveTheScreen()
        {
            StartBattle(Statue);
            Session.Press(H);
            Session.Press(J);
            yield return new WaitForSeconds(1f);

            Assert.That(_effects.ActiveCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Hud_Bars_FollowTheRules()
        {
            Session.PiperHealth.Damage(Session.PiperHealth.Max / 2f);
            Session.Stamina.Lose(Session.Stamina.Max / 4f);
            yield return null;

            Assert.That(_hud.HealthBar.Share, Is.EqualTo(0.5f).Within(0.02f));
            Assert.That(_hud.StaminaBar.Share, Is.EqualTo(0.75f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator Hud_AtLevelOne_OffersOneCombo()
        {
            yield return null;

            Assert.That(_hud.ShownCombos, Is.EqualTo(1));
            Assert.That(_hud.ShownLevel, Does.EndWith("1"));
        }

        [UnityTest]
        public IEnumerator Hud_OnLevellingUp_OffersTheNewCombo()
        {
            Session.Levels.Add(1000);
            yield return null;

            Assert.That(_hud.ShownCombos, Is.GreaterThan(1));
            Assert.That(_hud.ShownLevel, Does.EndWith(Session.Levels.Level.ToString()));
        }

        [UnityTest]
        public IEnumerator Hud_WhenAKeyIsPressedInPeace_SaysWhyNothingHappened()
        {
            Session.Press(H);
            yield return null;

            Assert.That(_hud.ShownMessage, Is.EqualTo(_texts.NotInBattleMessage));
        }

        [UnityTest]
        public IEnumerator Hud_WhenABossNotices_ShowsItsBar()
        {
            Assert.That(_hud.IsShowingABoss, Is.False);
            var type = new MinionType("Test Boss", 12f, 10, 0f, 1f, Statue, Plain);

            Session.Battle.AddMinion(type, Session.Piper.Position, MinionRank.Boss);
            yield return new WaitForSeconds(0.1f);

            Assert.That(_hud.IsShowingABoss, Is.True);
        }

        [UnityTest]
        public IEnumerator Sound_WhenACreatureArrives_Plays()
        {
            StartBattle(Statue);

            Session.Press(H);
            Session.Press(J);
            yield return null;

            Assert.That(_sound.LastPlayed, Is.SameAs(_soundConfig.Summoned));
        }

        [UnityTest]
        public IEnumerator Sound_WhenAKeyIsPressedInPeace_PlaysTheRefusal()
        {
            Session.Press(H);
            yield return null;

            Assert.That(_sound.LastPlayed, Is.SameAs(_soundConfig.Refused));
        }

        [UnityTest]
        public IEnumerator Pause_WhileShown_StopsTheRun()
        {
            yield return null;
            _root.Flow.TogglePause();
            yield return null;
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.PausedTitle));

            float stamina = Session.Stamina.Current;
            Session.Stamina.Lose(10f);
            yield return null;
            yield return null;

            Assert.That(Session.Stamina.Current, Is.EqualTo(stamina - 10f).Within(0.001f), "Nothing recovers while paused.");
        }

        [UnityTest]
        public IEnumerator Dying_AsksForNewGameOrContinue()
        {
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            yield return null;
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Dead));
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.DeadTitle));
            Assert.That(_screens.Card.Q<Button>("continueButton"), Is.Not.Null);
            Assert.That(_screens.Card.Q<Button>("newGameButton"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ContinueButton_KeepsTheLevelAndGoesOn()
        {
            Session.Levels.Add(200);
            int level = Session.Levels.Level;
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            yield return null;
            yield return null;

            Press("continueButton");
            yield return null;

            Assert.That(_root.Flow.IsPlaying, Is.True);
            Assert.That(Session.Levels.Level, Is.EqualTo(level));
            Assert.That(Session.PiperHealth.Current, Is.EqualTo(Session.PiperHealth.Max));
            Assert.That(_screens.ShownTitle, Is.Null);
        }

        [UnityTest]
        public IEnumerator NewGameButton_StartsAgainAtLevelOne()
        {
            Session.Levels.Add(200);
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            yield return null;
            yield return null;

            Press("newGameButton");
            yield return null;
            yield return null;
            yield return null;

            _root = Object.FindAnyObjectByType<LabRoot>();
            Assert.That(_root.Flow.IsPlaying, Is.True);
            Assert.That(_root.Session.Levels.Level, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BeatingTheFinalBoss_ShowsTheEnd()
        {
            Minion boss = Session.Battle.Minions.Single(minion => minion.IsFinalBoss);
            boss.Health.Damage(boss.Health.Max);
            yield return null;
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Won));
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.WonTitle));
        }
    }
}
