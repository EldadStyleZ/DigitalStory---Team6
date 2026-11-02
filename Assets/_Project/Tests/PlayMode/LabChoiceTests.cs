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
    public sealed class LabChoiceTests
    {
        private const string SceneName = "eldad-lab-scene";
        private const int J = 1;
        private const int H = 0;
        private const int K = 2;

        private LabRoot _root;
        private LabHudView _hud;
        private LabScreensView _screens;
        private LabSoundView _sound;
        private LabTextConfig _texts;
        private LevelConfig _levels;

        private LabSession Session => _root.Session;

        private int LevelTwoXp => _levels.XpToReach[1];

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            _root = Object.FindAnyObjectByType<LabRoot>();
            _hud = Object.FindAnyObjectByType<LabHudView>();
            _screens = Object.FindAnyObjectByType<LabScreensView>();
            _sound = Object.FindAnyObjectByType<LabSoundView>();
            _texts = Resources.FindObjectsOfTypeAll<LabTextConfig>()[0];
            _levels = Resources.FindObjectsOfTypeAll<LevelConfig>()[0];
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

        private string ShownWords => string.Join(" ", _screens.Card.Query<Label>().ToList().Select(label => label.text));

        [UnityTest]
        public IEnumerator Choice_OnReachingItsLevel_StopsTheGame()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Choosing));
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.ChoiceTitle));
        }

        [UnityTest]
        public IEnumerator Choice_WhileShown_NothingMoves()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;
            Session.Stamina.Lose(10f);
            float stamina = Session.Stamina.Current;

            yield return null;
            yield return null;

            Assert.That(Session.Stamina.Current, Is.EqualTo(stamina));
        }

        [UnityTest]
        public IEnumerator Choice_ShowsBothCreaturesAndBothSongs()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;

            foreach (CreatureOffer offer in Session.WaitingChoice.Options)
            {
                Assert.That(ShownWords, Does.Contain(offer.Creature.Name));
                Assert.That(ShownWords, Does.Contain(offer.Creature.Role));
                Assert.That(ShownWords, Does.Contain(offer.Song.Name));
                Assert.That(ShownWords, Does.Contain(offer.Song.Role));
            }

            Assert.That(ShownWords, Does.Contain(Session.WaitingChoice.Problem));
            Assert.That(ShownWords, Does.Contain(_texts.ChoiceWarning));
        }

        [UnityTest]
        public IEnumerator Choice_OptionButtons_TakeNoFocus()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;
            yield return null;

            Assert.That(_screens.Card.panel.focusController.focusedElement, Is.Not.EqualTo(_screens.Card.Q<Button>("chooseFirst")));
            Assert.That(_screens.Card.panel.focusController.focusedElement, Is.Not.EqualTo(_screens.Card.Q<Button>("chooseSecond")));
        }

        [UnityTest]
        public IEnumerator Choice_WhenPicked_GoesBackToTheGame()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;

            Press("chooseSecond");
            yield return null;

            Assert.That(_root.Flow.IsPlaying, Is.True);
            Assert.That(_screens.ShownTitle, Is.Null);
            Assert.That(Session.Party.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Choice_TheFirstButton_PicksTheFirstOption()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;
            CreatureOffer first = Session.WaitingChoice.Options[0];

            Press("chooseFirst");
            yield return null;

            Assert.That(Session.Party.Last(), Is.SameAs(first));
        }

        [UnityTest]
        public IEnumerator Choice_TheSecondButton_PicksTheSecondOption()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;
            CreatureOffer second = Session.WaitingChoice.Options[1];

            Press("chooseSecond");
            yield return null;

            Assert.That(Session.Party.Last(), Is.SameAs(second));
        }

        [UnityTest]
        public IEnumerator Choice_ThePick_JoinsTheCombos()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;

            Press("chooseFirst");
            yield return null;

            Assert.That(_hud.ShownCombos, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Choice_TheBeetle_SummonsWithTheRatsCombo()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;
            Press("chooseSecond");
            yield return null;

            Session.Press(J);
            Session.Press(H);
            Session.Press(K);
            yield return null;

            Assert.That(Session.Battle.Creatures.Single().Type.Name, Is.EqualTo("Beetle"));
            Assert.That(_sound.LastSong.Name, Is.EqualTo("Beetle"));
        }

        [UnityTest]
        public IEnumerator Choice_TwoAtOnce_AsksTwice()
        {
            Session.Levels.Add(_levels.XpToReach[2]);
            yield return null;
            string firstProblem = Session.WaitingChoice.Problem;

            Press("chooseFirst");
            yield return null;
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Choosing));
            Assert.That(Session.WaitingChoice.Problem, Is.Not.EqualTo(firstProblem));
            Assert.That(ShownWords, Does.Contain(Session.WaitingChoice.Problem));
        }

        [UnityTest]
        public IEnumerator Choice_WaitingAsThePiperDies_ShowsTheDeathThenTheChoice()
        {
            Session.Levels.Add(LevelTwoXp);
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            Session.Tick(Vector2.zero, 0.01f);
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Dead));
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.DeadTitle));

            Press("continueButton");
            yield return null;
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Choosing));
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.ChoiceTitle));
        }

        [UnityTest]
        public IEnumerator Choice_ArrivingWithTheLastBoss_ShowsTheEnd()
        {
            Minion boss = Session.Battle.Minions.Single(minion => minion.IsFinalBoss);
            Session.Levels.Add(_levels.XpToReach[_levels.XpToReach.Count - 1] - boss.Type.XpReward);

            while (Session.WaitingChoice != null)
            {
                Session.Choose(0);
            }

            boss.Health.Damage(boss.Health.Max);
            Session.Tick(Vector2.zero, 0.01f);
            yield return null;
            yield return null;

            Assert.That(Session.WaitingChoice, Is.Not.Null, "The kill has to bring a level for this test to mean anything.");
            Assert.That(_root.Flow.Phase, Is.EqualTo(LabPhase.Won));
            Assert.That(_screens.ShownTitle, Is.EqualTo(_texts.WonTitle));
        }

        [UnityTest]
        public IEnumerator Choice_AfterDying_KeepsThePick()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;
            Press("chooseSecond");
            yield return null;
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            yield return null;
            yield return null;

            Press("continueButton");
            yield return null;

            Assert.That(Session.Party.Last().Creature.Name, Is.EqualTo("Beetle"));
        }

        [UnityTest]
        public IEnumerator NewGame_ForgetsThePick()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;
            Press("chooseSecond");
            yield return null;
            Session.PiperHealth.Damage(Session.PiperHealth.Max);
            yield return null;
            yield return null;

            Press("newGameButton");
            yield return null;
            yield return null;
            yield return null;

            _root = Object.FindAnyObjectByType<LabRoot>();
            Assert.That(_root.Session.Party.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Hud_ShowsTheSongsAtWork()
        {
            CreatureOffer starter = Session.Party[0];
            Session.Battle.Play(starter.Song);
            yield return null;

            Assert.That(_hud.ShownSongs, Does.Contain(starter.Song.Name));
        }

        [UnityTest]
        public IEnumerator Sound_AtTheStart_BakesOnlyWhatTheRunKnows()
        {
            yield return null;

            Assert.That(_sound.TunesBaked, Is.EqualTo(Session.Songs.Count));
            Assert.That(Session.AllSongs.Count, Is.GreaterThan(Session.Songs.Count));
        }

        [UnityTest]
        public IEnumerator Sound_APick_BringsItsTunes()
        {
            int before = _sound.TunesBaked;
            Session.Levels.Add(LevelTwoXp);
            yield return null;

            Press("chooseSecond");
            yield return null;

            Assert.That(_sound.TunesBaked, Is.EqualTo(before + 2));
        }

        [UnityTest]
        public IEnumerator Sound_APickedCreature_PlaysItsTune()
        {
            Session.Levels.Add(LevelTwoXp);
            yield return null;
            Press("chooseSecond");
            yield return null;

            Session.Press(J);
            Session.Press(H);
            Session.Press(K);
            yield return null;

            Assert.That(_sound.IsPlayingASong, Is.True);
        }
    }
}
