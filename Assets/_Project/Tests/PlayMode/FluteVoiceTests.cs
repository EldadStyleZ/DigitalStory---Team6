using System.Collections;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class FluteVoiceTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;
        private const int FramesForAudio = 30;

        // Sound takes real time to reach the listener, and frames can be very short.
        private const float SecondsForAudio = 0.5f;

        private GameRoot _root;
        private FluteVoice _voice;
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
            _voice = Object.FindAnyObjectByType<FluteVoice>();
            _flute = Object.FindAnyObjectByType<FluteView>();
            _root.Flow.Begin();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Voice_WhileTheFluteSounds_PlaysThatHole()
        {
            _flute.StartPlaying(3);
            yield return null;
            yield return null;

            Assert.That(_voice.HolePlaying, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator Voice_WhenTheFluteStops_GoesSilent()
        {
            _flute.StartPlaying(3);
            yield return null;
            yield return null;

            _flute.StopPlaying();
            yield return null;
            yield return null;

            Assert.That(_voice.HolePlaying, Is.EqualTo(FluteSynth.Silent));
        }

        [UnityTest]
        public IEnumerator Voice_WhilePaused_GoesSilent()
        {
            _root.Session.Tick(new PiperIntent(Vector2.zero, 1, 1), 1f / 60f);
            _root.Flow.Pause();
            yield return null;

            Assert.That(_root.Session.Flute.CurrentHole, Is.EqualTo(0), "The rules still hold the note.");
            Assert.That(_voice.HolePlaying, Is.EqualTo(FluteSynth.Silent));
        }

        [UnityTest]
        public IEnumerator Voice_WhilePlaying_ReachesTheListener()
        {
            _flute.StartPlaying(0);
            yield return ListenerMeter.Connect();
            yield return new WaitForSecondsRealtime(SecondsForAudio);

            Assert.That(ListenerMeter.Loudness(), Is.GreaterThan(0.005f));
        }

        [UnityTest]
        public IEnumerator Voice_AfterTheAudioDeviceChanges_KeepsPlaying()
        {
            _flute.StartPlaying(0);
            yield return null;

            AudioSettings.Reset(AudioSettings.GetConfiguration());

            yield return new WaitForSecondsRealtime(SecondsForAudio);
            yield return ListenerMeter.Connect();
            int before = _voice.BuffersFilled;
            yield return new WaitForSecondsRealtime(SecondsForAudio);

            Assert.That(_voice.BuffersFilled, Is.GreaterThan(before));
            Assert.That(ListenerMeter.Loudness(), Is.GreaterThan(0.005f));
        }

        [UnityTest]
        public IEnumerator Voice_InTheScene_DoesNotStartBeforeItIsReady()
        {
            yield return null;

            Assert.That(_voice.GetComponent<AudioSource>().playOnAwake, Is.False);
        }

        [UnityTest]
        public IEnumerator Voice_WhenSilent_NothingReachesTheListener()
        {
            yield return ListenerMeter.Connect();
            yield return new WaitForSecondsRealtime(SecondsForAudio);

            Assert.That(ListenerMeter.Loudness(), Is.LessThan(0.0001f));
        }

        [UnityTest]
        public IEnumerator Voice_InTheRunningGame_FillsSoundBuffers()
        {
            int before = _voice.BuffersFilled;

            for (int frame = 0; frame < FramesForAudio; frame++)
            {
                yield return null;
            }

            Assert.That(_voice.BuffersFilled, Is.GreaterThan(before), "Unity never called the audio filter.");
        }
    }
}
