using System;
using System.Collections;
using DigitalStory.Data;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class SwarmFluteVoiceTests
    {
        // Longer than the default NoteSeconds of 0.3, in real time.
        private const float SecondsPastNote = 0.6f;

        private GameObject _host;
        private SoundConfig _sound;
        private PiperMoveConfig _move;
        private SwarmFluteVoice _voice;

        [SetUp]
        public void Create()
        {
            _sound = ScriptableObject.CreateInstance<SoundConfig>();
            _move = ScriptableObject.CreateInstance<PiperMoveConfig>();
            _host = new GameObject("SwarmFluteVoice");
            _voice = _host.AddComponent<SwarmFluteVoice>();
            _voice.Initialize(_sound, _move);
        }

        [TearDown]
        public void Destroy()
        {
            UnityEngine.Object.DestroyImmediate(_host);
            UnityEngine.Object.DestroyImmediate(_sound);
            UnityEngine.Object.DestroyImmediate(_move);
        }

        [UnityTest]
        public IEnumerator PlayNote_SoundsThatHole()
        {
            yield return null;

            _voice.PlayNote(2);

            Assert.That(_voice.HolePlaying, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Note_AfterItsLengthWithNoKeyHeld_GoesSilent()
        {
            yield return null;
            _voice.PlayNote(2);

            yield return new WaitForSecondsRealtime(SecondsPastNote);

            Assert.That(_voice.HolePlaying, Is.EqualTo(FluteSynth.Silent));
        }

        [UnityTest]
        public IEnumerator PlayNote_WhileAnotherSounds_ReplacesIt()
        {
            yield return null;
            _voice.PlayNote(2);

            _voice.PlayNote(4);

            Assert.That(_voice.HolePlaying, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator PlayNote_WhileAnotherSounds_RestartsTheNoteLength()
        {
            yield return null;
            _voice.PlayNote(2);
            yield return new WaitForSecondsRealtime(_move.NoteSeconds * 0.8f);

            _voice.PlayNote(4);
            yield return new WaitForSecondsRealtime(_move.NoteSeconds * 0.5f);

            Assert.That(_voice.HolePlaying, Is.EqualTo(4));
        }

        [Test]
        public void Initialize_WithANullConfig_Throws()
        {
            GameObject other = new GameObject("Other");

            try
            {
                SwarmFluteVoice voice = other.AddComponent<SwarmFluteVoice>();

                Assert.Throws<ArgumentNullException>(() => voice.Initialize(null, _move));
                Assert.Throws<ArgumentNullException>(() => voice.Initialize(_sound, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
            }
        }
    }
}
