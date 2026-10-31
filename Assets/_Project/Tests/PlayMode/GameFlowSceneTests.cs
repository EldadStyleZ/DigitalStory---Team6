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
    }
}
