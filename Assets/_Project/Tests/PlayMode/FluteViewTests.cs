using System.Collections;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class FluteViewTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;
        private const int HoleCount = 6;
        private const float StepSeconds = 1f / 60f;

        private GameRoot _root;
        private FluteView _flute;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<GameRoot>();
            _flute = Object.FindAnyObjectByType<FluteView>();
            _root.Flow.Begin();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Flute_OnLoad_ShowsEveryHole()
        {
            yield return null;

            Assert.That(_flute.HoleCount, Is.EqualTo(HoleCount));
        }

        [UnityTest]
        public IEnumerator Flute_PressingAHole_SoundsThatNote()
        {
            _flute.StartPlaying(2);
            yield return null;
            yield return null;

            Assert.That(_root.Session.Flute.CurrentHole, Is.EqualTo(2));
            Assert.That(_flute.IsLit(2), Is.True);
            Assert.That(_flute.IsLit(3), Is.False);
        }

        [UnityTest]
        public IEnumerator Flute_LettingGo_StopsTheNote()
        {
            _flute.StartPlaying(2);
            yield return null;

            _flute.StopPlaying();
            yield return null;
            yield return null;

            Assert.That(_root.Session.Flute.CurrentHole, Is.EqualTo(Flute.NoHole));
            Assert.That(_flute.IsLit(2), Is.False);
        }

        [UnityTest]
        public IEnumerator Flute_SlidingToAnotherHole_SwitchesTheNote()
        {
            _flute.StartPlaying(1);
            yield return null;

            _flute.SlideTo(3);
            yield return null;
            yield return null;

            Assert.That(_root.Session.Flute.CurrentHole, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Flute_PlayingAKeyboardNote_LightsItsHole()
        {
            int bit = 1 << 4;

            _root.Session.Tick(new PiperIntent(Vector2.zero, bit, bit), StepSeconds);
            Assert.That(_root.Session.Flute.CurrentHole, Is.EqualTo(4));
            _flute.SendMessage("Update");

            Assert.That(_flute.IsLit(4), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Flute_PlayingACombo_CastsTheSong()
        {
            int cast = SongBook.NoSong;
            _root.Session.SongCast += song => cast = song;

            foreach (int hole in new[] { 2, 1, 0 })
            {
                _flute.StartPlaying(hole);
                yield return null;
                _flute.StopPlaying();
                yield return null;
            }

            Assert.That(cast, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Flute_Pressed_IsBeingPlayedUntilLetGo()
        {
            Assert.That(_flute.IsBeingPlayed, Is.False);

            _flute.StartPlaying(0);
            Assert.That(_flute.IsBeingPlayed, Is.True);

            _flute.SlideTo(Flute.NoHole);
            Assert.That(_flute.IsBeingPlayed, Is.True, "Sliding off the holes must not start walking.");
            Assert.That(_flute.HeldHoles, Is.EqualTo(0));

            _flute.StopPlaying();
            Assert.That(_flute.IsBeingPlayed, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Flute_OutOfBreath_LooksDimmed()
        {
            yield return null;
            Assert.That(_flute.IsDimmed, Is.False);

            _root.Session.Tick(new PiperIntent(Vector2.zero, 1, 0), 10f);
            yield return null;

            Assert.That(_root.Session.Breath.IsExhausted, Is.True);
            Assert.That(_flute.IsDimmed, Is.True);
        }
    }
}
