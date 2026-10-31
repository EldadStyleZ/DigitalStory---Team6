using System.Collections;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class EffectsViewTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;

        private GameRoot _root;
        private EffectsView _effects;

        private EffectPool Pool => _effects.Pool;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<GameRoot>();
            _effects = Object.FindAnyObjectByType<EffectsView>();
            _root.Flow.Begin();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Effects_QuietNight_ShowNothing()
        {
            yield return null;

            Assert.That(Pool.ActiveCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Effects_RatEaten_ShowsAPuff()
        {
            yield return null;

            _root.Session.Swarm.Eat(0);

            Assert.That(Pool.ActiveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Effects_RatDrowned_ShowsARippleAndAPuff()
        {
            yield return null;

            _root.Session.Swarm.Enchant(0, new IntoTheRiver(), 0f);
            yield return null;
            yield return null;

            Assert.That(_root.Session.Swarm.DrownedCount, Is.EqualTo(1));
            Assert.That(Pool.ActiveCount, Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator Effects_BalloonPopped_ShowsAPuff()
        {
            yield return null;
            var balloons = ScriptableObject.CreateInstance<BalloonsEffect>();

            _root.Session.Swarm.Enchant(0, new FloatTrance(balloons), 0f);
            yield return null;
            yield return null;

            Assert.That(Pool.ActiveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Effects_SongCast_ShowsARing()
        {
            yield return null;

            foreach (int hole in new[] { 0, 2, 4 })
            {
                int bit = 1 << hole;
                _root.Session.Tick(new PiperIntent(Vector2.zero, bit, bit), 1f / 60f);
            }

            Assert.That(Pool.ActiveCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Effects_WhilePlayingTheFlute_ShowNotesAndTheCircle()
        {
            FluteView flute = Object.FindAnyObjectByType<FluteView>();
            flute.StartPlaying(0);

            for (int frame = 0; frame < 5; frame++)
            {
                yield return null;
            }

            Assert.That(_root.Session.Flute.CurrentHole, Is.EqualTo(0));
            Assert.That(Pool.ActiveCount, Is.GreaterThan(0));
            Assert.That(_effects.IsMusicCircleShown, Is.True);
        }

        [UnityTest]
        public IEnumerator Effects_FluteStops_TheCircleGoes()
        {
            FluteView flute = Object.FindAnyObjectByType<FluteView>();
            flute.StartPlaying(0);
            yield return null;
            yield return null;

            flute.StopPlaying();
            yield return null;
            yield return null;

            Assert.That(_effects.IsMusicCircleShown, Is.False);
        }

        [UnityTest]
        public IEnumerator Effects_WhilePaused_HoldStill()
        {
            yield return null;
            _root.Session.Swarm.Eat(0);
            int shown = Pool.ActiveCount;
            _root.Flow.Pause();

            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            Assert.That(Pool.ActiveCount, Is.EqualTo(shown));
        }

        [UnityTest]
        public IEnumerator Cat_Asleep_ShowsAZ()
        {
            yield return null;
            SpriteRenderer snooze = Object.FindAnyObjectByType<CatView>().transform.GetChild(0).GetChild(1).GetComponent<SpriteRenderer>();
            Assert.That(snooze.enabled, Is.False);

            _root.Session.Cats.PutToSleepNear(_root.Session.Cats[0].Position, 1f, 60f);
            yield return null;

            Assert.That(snooze.enabled, Is.True);
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
