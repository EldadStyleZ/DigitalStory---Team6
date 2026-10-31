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
    public sealed class TownSceneTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;
        private const int FramesToMove = 30;

        private GameRoot _root;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<GameRoot>();
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_StartsTheNight()
        {
            Assert.That(_root, Is.Not.Null);
            Assert.That(_root.Session, Is.Not.Null);
            Assert.That(_root.Session.Outcome, Is.EqualTo(NightOutcome.InProgress));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_DrawsEveryRatAndHouse()
        {
            RatSwarmView rats = Object.FindAnyObjectByType<RatSwarmView>();
            TownView town = Object.FindAnyObjectByType<TownView>();

            Assert.That(rats.transform.childCount, Is.EqualTo(_root.Session.Swarm.Count));
            Assert.That(town.transform.childCount, Is.GreaterThan(_root.Session.Town.Houses.Count));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Town_AfterAFewFrames_RatsMoveOnScreen()
        {
            RatSwarmView rats = Object.FindAnyObjectByType<RatSwarmView>();
            Transform firstRat = rats.transform.GetChild(0);
            Vector3 start = firstRat.position;

            for (int frame = 0; frame < FramesToMove; frame++)
            {
                yield return null;
            }

            Assert.That(firstRat.position, Is.Not.EqualTo(start));
            Assert.That(firstRat.position.y, Is.LessThan(0f), "Town space points down, so rats sit below the scene origin.");
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_ShowsTheGoalOnScreen()
        {
            NightStatusView status = Object.FindAnyObjectByType<NightStatusView>();

            yield return null;

            Assert.That(status.GoalText, Is.EqualTo($"0/{_root.Session.Quota}"));
            Assert.That(status.TimeText, Is.EqualTo($"{Mathf.CeilToInt(_root.Session.TimeLeft)}s"));
            Assert.That(status.RatsText, Is.EqualTo($"{_root.Session.Swarm.AliveCount} rats left"));
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_PanelScalesWithTheWindow()
        {
            UIDocument document = Object.FindAnyObjectByType<UIDocument>();

            yield return null;

            Assert.That(document.panelSettings.scaleMode, Is.EqualTo(PanelScaleMode.ScaleWithScreenSize));
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_EveryLabelFitsOnScreen()
        {
            VisualElement root = Object.FindAnyObjectByType<UIDocument>().rootVisualElement;

            yield return null;

            root.Query<Label>().ForEach(label =>
                Assert.That(label.worldBound.xMax, Is.LessThanOrEqualTo(root.worldBound.xMax), label.text));
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_PointsTheCameraAtThePiper()
        {
            PiperView piper = Object.FindAnyObjectByType<PiperView>();
            Camera sceneCamera = Camera.main;

            yield return null;

            Vector3 onScreen = sceneCamera.WorldToViewportPoint(piper.transform.position);
            Assert.That(onScreen.x, Is.InRange(0f, 1f));
            Assert.That(onScreen.y, Is.InRange(0f, 1f));
        }
    }
}
