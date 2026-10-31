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
        private const float FrameSeconds = 1f / 60f;

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
            _root.Flow.Begin();
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
        public IEnumerator Town_OnLoad_DrawsEveryCat()
        {
            CatView cats = Object.FindAnyObjectByType<CatView>();

            Assert.That(_root.Session.Cats.Count, Is.EqualTo(1));
            Assert.That(cats.transform.childCount, Is.EqualTo(_root.Session.Cats.Count));
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
        public IEnumerator Town_OnLoad_ListsEverySongAndItsKeys()
        {
            ComboRowView row = Object.FindAnyObjectByType<ComboRowView>();

            yield return null;

            Assert.That(row.ChipCount, Is.EqualTo(9));
            Assert.That(row.ChipText(0), Is.EqualTo("Lullaby 3 2 1"));
            Assert.That(row.ChipText(7), Is.EqualTo("Crowd Surf 3 3 6"));
            Assert.That(row.ChipText(8), Is.EqualTo("Choir 1 2 3 4"));
        }

        [UnityTest]
        public IEnumerator Town_PlayingACombo_LightsItUpAndDimsItWhileItRests()
        {
            ComboRowView row = Object.FindAnyObjectByType<ComboRowView>();
            yield return null;
            Assert.That(row.IsDimmed(0), Is.False);

            PlayLullaby();
            yield return null;

            Assert.That(row.IsGlowing(0), Is.True);
            Assert.That(row.IsDimmed(0), Is.True);
            Assert.That(row.IsGlowing(1), Is.False);
            Assert.That(row.IsDimmed(1), Is.False);
        }

        [UnityTest]
        public IEnumerator Town_PlayingLullaby_CastsTheSongFromItsAsset()
        {
            int cast = -1;
            _root.Session.SongCast += song => cast = song;

            PlayLullaby();
            yield return null;

            Assert.That(cast, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Town_RatFloating_GrowsAndChangesColor()
        {
            RatSwarmView rats = Object.FindAnyObjectByType<RatSwarmView>();
            Transform firstRat = rats.transform.GetChild(0);
            Color normalColor = firstRat.GetComponent<SpriteRenderer>().color;

            _root.Session.Swarm.Enchant(0, new FloatTrance(ScriptableObject.CreateInstance<BalloonsEffect>()), 5f);
            yield return null;

            Assert.That(firstRat.localScale.x, Is.GreaterThan(1f));
            Assert.That(firstRat.GetComponent<SpriteRenderer>().color, Is.Not.EqualTo(normalColor));
        }

        [UnityTest]
        public IEnumerator Town_RatPlayingDeadDuringChoir_DoesNotHop()
        {
            RatSwarmView rats = Object.FindAnyObjectByType<RatSwarmView>();
            RatSwarm swarm = _root.Session.Swarm;
            swarm.FullyCharm(0);
            swarm.Enchant(0, new FaintTrance(0f), 5f);

            _root.Session.Stage.StartChoir(5f);
            yield return null;

            Vector3 expected = TownSpace.ToScene(swarm[0].Position);
            Assert.That(rats.transform.GetChild(0).position.y, Is.EqualTo(expected.y).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator Town_CrowdSurfing_LiftsThePiper()
        {
            Transform piper = Object.FindAnyObjectByType<PiperView>().transform;
            float onTheGround = TownSpace.ToScene(_root.Session.Piper.Position).y;

            _root.Session.Stage.StartSurf(5f, 220f);
            yield return null;

            Assert.That(piper.position.y, Is.GreaterThan(onTheGround));
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

        private void PlayLullaby()
        {
            foreach (int hole in new[] { 2, 1, 0 })
            {
                int bit = 1 << hole;
                _root.Session.Tick(new PiperIntent(Vector2.zero, bit, bit), FrameSeconds);
            }
        }
    }
}
