using System.Collections;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class NightLookTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;
        private const int CallHoleBit = 1;

        private GameRoot _root;
        private DarknessView _darkness;
        private NightLookConfig _look;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<GameRoot>();
            _darkness = Object.FindAnyObjectByType<DarknessView>();
            _look = Resources.FindObjectsOfTypeAll<NightLookConfig>()[0];
            _root.Flow.Begin();
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Darkness_WhilePlaying_CoversTheWholeView()
        {
            yield return null;
            Camera sceneCamera = Camera.main;
            Bounds shown = _darkness.GetComponent<SpriteRenderer>().bounds;
            float height = sceneCamera.orthographicSize * 2f;
            float width = height * sceneCamera.aspect;

            Assert.That(shown.size.x, Is.GreaterThanOrEqualTo(width));
            Assert.That(shown.size.y, Is.GreaterThanOrEqualTo(height));
            Assert.That(shown.center.x, Is.EqualTo(sceneCamera.transform.position.x).Within(0.01f));
            Assert.That(shown.center.y, Is.EqualTo(sceneCamera.transform.position.y).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Darkness_DrawnOverTheTownButUnderThePiper()
        {
            yield return null;
            int order = _darkness.GetComponent<SpriteRenderer>().sortingOrder;
            RatSwarmView rats = Object.FindAnyObjectByType<RatSwarmView>();
            PiperView piper = Object.FindAnyObjectByType<PiperView>();

            Assert.That(order, Is.GreaterThan(rats.GetComponentInChildren<SpriteRenderer>().sortingOrder));
            Assert.That(order, Is.LessThan(piper.GetComponent<SpriteRenderer>().sortingOrder));
        }

        [UnityTest]
        public IEnumerator Darkness_EarlyInTheNight_UsesTheNightColor()
        {
            yield return null;

            Assert.That(_darkness.ShownDarkness.a, Is.EqualTo(_look.NightColor.a).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Darkness_AtDawn_Lifts()
        {
            _root.Session.Tick(new PiperIntent(Vector2.zero, 0, 0), _root.Session.TimeLimit * 0.99f);
            yield return null;

            Assert.That(_darkness.ShownDarkness.a, Is.LessThan(_look.NightColor.a - 0.2f));
        }

        [UnityTest]
        public IEnumerator Darkness_FirstLight_IsThePiper()
        {
            yield return null;
            _darkness.Paint();

            Vector2 piper = TownSpace.ToScene(_root.Session.Piper.Position);

            Assert.That(_darkness.Field.Count, Is.GreaterThan(0));
            Assert.That(_darkness.Field.Lights[0].x, Is.EqualTo(piper.x).Within(0.001f));
            Assert.That(_darkness.Field.Lights[0].z, Is.EqualTo(TownSpace.ToSceneLength(_look.PiperReach)).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator Darkness_WhilePlayingTheFlute_ThePiperLightsMore()
        {
            yield return null;

            _root.Session.Tick(new PiperIntent(Vector2.zero, CallHoleBit, CallHoleBit), 1f / 60f);
            _darkness.Paint();

            float least = TownSpace.ToSceneLength(_look.PlayingReach - _look.PlayingPulse);
            Assert.That(_darkness.Field.Lights[0].z, Is.GreaterThanOrEqualTo(least));
        }

        [UnityTest]
        public IEnumerator Darkness_DuringDisco_LightsMoreAndAddsColor()
        {
            yield return null;
            _darkness.Paint();
            int before = _darkness.Field.Count;

            _root.Session.Stage.StartDisco(5f);
            _darkness.Paint();

            Assert.That(_darkness.Field.Lights[0].z, Is.EqualTo(TownSpace.ToSceneLength(_look.DiscoReach)).Within(0.001f));
            Assert.That(_darkness.Field.Count, Is.GreaterThan(before));
        }

        [UnityTest]
        public IEnumerator Darkness_Painting_AllocatesNothing()
        {
            yield return null;

            Assert.That(() => _darkness.Paint(), Is.Not.AllocatingGCMemory());
        }

        [UnityTest]
        public IEnumerator Town_OnLoad_DrawsEveryLamp()
        {
            yield return null;
            TownView town = Object.FindAnyObjectByType<TownView>();
            var lamps = 0;

            foreach (Transform child in town.transform)
            {
                lamps += child.name == "Lamp" ? 1 : 0;
            }

            Assert.That(lamps, Is.EqualTo(_root.Session.Town.Lamps.Count));
        }

        [UnityTest]
        public IEnumerator Cat_OnThePounce_HasGlowingEyes()
        {
            yield return null;
            SpriteRenderer eyes = CatEyes();

            Assert.That(eyes.enabled, Is.True);
            Assert.That(eyes.sortingOrder, Is.GreaterThan(_darkness.GetComponent<SpriteRenderer>().sortingOrder));
        }

        [UnityTest]
        public IEnumerator Cat_Asleep_ClosesItsEyes()
        {
            yield return null;
            SpriteRenderer eyes = CatEyes();

            _root.Session.Cats.PutToSleepNear(_root.Session.Cats[0].Position, 1f, 60f);
            yield return null;

            Assert.That(eyes.enabled, Is.False);
        }

        private static SpriteRenderer CatEyes()
        {
            return Object.FindAnyObjectByType<CatView>().transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
        }
    }
}
