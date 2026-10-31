using System.Collections;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class GameFlowSceneTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;
        private const int FramesToRun = 10;
        private const string LosesFocusMessage = "OnApplicationFocus";

        private GameRoot _root;
        private MenuScreensView _menus;
        private HudLayout _hud;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            FindTheGame();
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_WaitsOnTheTitleScreen()
        {
            float startTime = _root.Session.TimeLeft;

            for (int frame = 0; frame < FramesToRun; frame++)
            {
                yield return null;
            }

            Assert.That(_root.Flow.Phase, Is.EqualTo(GamePhase.Title));
            Assert.That(_menus.ShownTitle, Is.EqualTo("The Piper"));
            Assert.That(_root.Session.TimeLeft, Is.EqualTo(startTime));
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_HidesTheHud()
        {
            yield return null;

            Assert.That(_hud.IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_ShowsTheGoalOfThisNight()
        {
            yield return null;

            Assert.That(CardText(), Does.Contain($"Drown {_root.Session.Quota} of {_root.Session.Swarm.Count} rats"));
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_TheTitleFitsOnScreen()
        {
            yield return null;

            AssertCardFitsOnScreen();
        }

        [UnityTest]
        public IEnumerator Town_PressingStart_RunsTheNight()
        {
            float startTime = _root.Session.TimeLeft;

            PressButton(TitleScreen.StartButtonName);

            for (int frame = 0; frame < FramesToRun; frame++)
            {
                yield return null;
            }

            Assert.That(_root.Flow.IsPlaying, Is.True);
            Assert.That(_menus.IsShowing, Is.False);
            Assert.That(_hud.IsShowing, Is.True);
            Assert.That(_root.Session.TimeLeft, Is.LessThan(startTime));
        }

        [UnityTest]
        public IEnumerator Town_Paused_StopsTheClockAndShowsThePauseScreen()
        {
            yield return StartTheNight();

            _root.Flow.TogglePause();
            yield return null;
            float pausedTime = _root.Session.TimeLeft;

            for (int frame = 0; frame < FramesToRun; frame++)
            {
                yield return null;
            }

            Assert.That(_root.Session.TimeLeft, Is.EqualTo(pausedTime));
            Assert.That(_menus.ShownTitle, Is.EqualTo("Pause"));
            Assert.That(_hud.IsShowing, Is.True);
            AssertCardFitsOnScreen();
        }

        [UnityTest]
        public IEnumerator Town_PressingResume_CarriesOn()
        {
            yield return StartTheNight();
            _root.Flow.TogglePause();
            yield return null;

            PressButton(PauseScreen.ResumeButtonName);
            yield return null;

            Assert.That(_root.Flow.IsPlaying, Is.True);
            Assert.That(_menus.IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator Town_PressingQuit_GoesBackToAFreshTitle()
        {
            yield return StartTheNight();
            _root.Flow.TogglePause();
            yield return null;

            PressButton(PauseScreen.QuitButtonName);
            yield return WaitForTheNewScene();

            AssertAFreshNightOnTheTitle();
        }

        [UnityTest]
        public IEnumerator Town_LosingFocusWhilePlaying_Pauses()
        {
            yield return StartTheNight();

            _root.SendMessage(LosesFocusMessage, false);
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(GamePhase.Paused));
            Assert.That(_menus.ShownTitle, Is.EqualTo("Pause"));
        }

        [UnityTest]
        public IEnumerator Town_LosingFocusOnTheTitle_StaysOnTheTitle()
        {
            _root.SendMessage(LosesFocusMessage, false);
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(GamePhase.Title));
        }

        [UnityTest]
        public IEnumerator Town_DawnBreaks_ShowsTheLostResult()
        {
            yield return StartTheNight();

            EndTheNightAtDawn();
            yield return null;

            Assert.That(_root.Flow.Phase, Is.EqualTo(GamePhase.Ended));
            Assert.That(_menus.ShownTitle, Is.EqualTo("The sun rose. You died."));
            Assert.That(CardText(), Does.Contain($"{_root.Session.Swarm.DrownedCount}/{_root.Session.Quota} drowned"));
            Assert.That(_menus.Card.Q<Button>(ResultScreen.AgainButtonName).text, Is.EqualTo("Try again"));
            Assert.That(_hud.IsShowing, Is.False);
            AssertCardFitsOnScreen();
        }

        [UnityTest]
        public IEnumerator Town_TooManyRatsEaten_ShowsTheOutOfReachResult()
        {
            yield return StartTheNight();
            int canLose = _root.Session.Swarm.Count - _root.Session.Quota;

            for (int rat = 0; rat <= canLose; rat++)
            {
                _root.Session.Swarm.Eat(rat);
            }

            yield return null;
            yield return null;

            Assert.That(_menus.ShownTitle, Is.EqualTo("The rats won tonight"));
            Assert.That(CardText(), Does.Contain("The cats ate too many rats."));
        }

        [UnityTest]
        public IEnumerator Town_GoalReached_ShowsTheWonResult()
        {
            yield return StartTheNight();
            int quota = _root.Session.Quota;

            for (int rat = 0; rat < quota; rat++)
            {
                _root.Session.Swarm.Enchant(rat, new IntoTheRiver(), 0f);
            }

            yield return null;
            yield return null;

            Assert.That(_menus.ShownTitle, Is.EqualTo("Hamelin is saved"));
            Assert.That(CardText(), Does.Contain($"{quota} rats in the river"));
            Assert.That(_menus.Card.Q<Button>(ResultScreen.AgainButtonName).text, Is.EqualTo("Play again"));
        }

        [UnityTest]
        public IEnumerator Town_PressingTryAgain_GoesBackToAFreshTitle()
        {
            yield return StartTheNight();
            EndTheNightAtDawn();
            yield return null;

            PressButton(ResultScreen.AgainButtonName);
            yield return WaitForTheNewScene();

            AssertAFreshNightOnTheTitle();
        }

        private IEnumerator StartTheNight()
        {
            PressButton(TitleScreen.StartButtonName);
            yield return null;
        }

        private IEnumerator WaitForTheNewScene()
        {
            for (int frame = 0; frame < FramesToSettle + FramesToSettle; frame++)
            {
                yield return null;
            }

            FindTheGame();
        }

        private void EndTheNightAtDawn()
        {
            _root.Session.Tick(new PiperIntent(Vector2.zero, 0, 0), _root.Session.TimeLeft + 1f);
        }

        private void AssertAFreshNightOnTheTitle()
        {
            Assert.That(_root.Flow.Phase, Is.EqualTo(GamePhase.Title));
            Assert.That(_menus.ShownTitle, Is.EqualTo("The Piper"));
            Assert.That(_root.Session.TimeLeft, Is.EqualTo(_root.Session.TimeLimit));
            Assert.That(_root.Session.Swarm.AliveCount, Is.EqualTo(_root.Session.Swarm.Count));
        }

        private void FindTheGame()
        {
            _root = Object.FindAnyObjectByType<GameRoot>();
            _menus = Object.FindAnyObjectByType<MenuScreensView>();
            _hud = Object.FindAnyObjectByType<HudLayout>();
        }

        private string CardText()
        {
            var words = new System.Text.StringBuilder();
            _menus.Card.Query<Label>().ForEach(label => words.AppendLine(label.text));
            return words.ToString();
        }

        private void PressButton(string buttonName)
        {
            Button button = _menus.Card.Q<Button>(buttonName);
            Assert.That(button, Is.Not.Null, buttonName);

            using (NavigationSubmitEvent press = NavigationSubmitEvent.GetPooled())
            {
                press.target = button;
                button.SendEvent(press);
            }
        }

        private void AssertCardFitsOnScreen()
        {
            Rect screen = _menus.Card.panel.visualTree.worldBound;
            Rect card = _menus.Card.worldBound;

            Assert.That(card.yMin, Is.GreaterThanOrEqualTo(screen.yMin));
            Assert.That(card.yMax, Is.LessThanOrEqualTo(screen.yMax));
            Assert.That(card.xMax, Is.LessThanOrEqualTo(screen.xMax));
        }

        /// <summary>Carries a rat past the bank, so it drowns as soon as the trance ends.</summary>
        private sealed class IntoTheRiver : RatTrance
        {
            private const float FarOutInTheRiver = 1900f;

            public override bool MovesTheRat => true;

            public override void Move(ref Rat rat, in TranceStep step) => rat.Position.x = FarOutInTheRiver;
        }
    }
}
