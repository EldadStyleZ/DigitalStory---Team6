using System.Collections;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    public sealed class SoundViewTests
    {
        private const string SceneName = "Town";
        private const int FramesToSettle = 3;

        private GameRoot _root;
        private SoundView _sound;
        private SoundConfig _config;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<GameRoot>();
            _sound = Object.FindAnyObjectByType<SoundView>();
            _config = Resources.FindObjectsOfTypeAll<SoundConfig>()[0];
            _root.Flow.Begin();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Sound_RatEaten_PlaysTheEatenSound()
        {
            yield return null;

            _root.Session.Swarm.Eat(0);

            Assert.That(_sound.LastPlayed, Is.SameAs(_config.Eaten));
        }

        [UnityTest]
        public IEnumerator Sound_RatDrowned_PlaysASplash()
        {
            yield return null;

            _root.Session.Swarm.Enchant(0, new IntoTheRiver(), 0f);
            yield return null;
            yield return null;

            Assert.That(_config.Splashes, Does.Contain(_sound.LastPlayed));
        }

        [UnityTest]
        public IEnumerator Sound_BalloonPopped_PlaysAPop()
        {
            yield return null;

            _root.Session.Swarm.Enchant(0, new FloatTrance(ScriptableObject.CreateInstance<BalloonsEffect>()), 0f);
            yield return null;
            yield return null;

            Assert.That(_sound.LastPlayed, Is.SameAs(_config.Pop));
        }

        [UnityTest]
        public IEnumerator Sound_RatWakes_Squeaks()
        {
            yield return null;

            _root.Session.Swarm.Enchant(0, new FaintTrance(1f), 0f);
            yield return null;
            yield return null;

            Assert.That(_config.Squeaks, Does.Contain(_sound.LastPlayed));
        }

        [UnityTest]
        public IEnumerator Sound_SongWithATuneCast_PlaysTheChimeAndTheTune()
        {
            yield return null;
            int before = _sound.PlayCount;

            foreach (int hole in new[] { 0, 1, 2, 3 })
            {
                int bit = 1 << hole;
                _root.Session.Tick(new PiperIntent(Vector2.zero, bit, bit), 1f / 60f);
            }

            Assert.That(_sound.PlayCount, Is.GreaterThanOrEqualTo(before + 2));
            Assert.That(_sound.HasPlayed(_config.SongChime), Is.True);
            Assert.That(_sound.LastPlayed, Is.Not.SameAs(_config.SongChime), "The song's own tune plays after the chime.");
        }

        [UnityTest]
        public IEnumerator Sound_NightLost_PlaysTheLostSound()
        {
            yield return null;

            _root.Session.Tick(new PiperIntent(Vector2.zero, 0, 0), _root.Session.TimeLeft + 1f);
            yield return null;

            Assert.That(_sound.HasPlayed(_config.Lost), Is.True);
            Assert.That(_sound.HasPlayed(_config.Won), Is.False);
        }

        [UnityTest]
        public IEnumerator Sound_ManyRatsCharmedAtOnce_SqueaksOnce()
        {
            yield return null;
            int before = _sound.PlayCount;
            RatSwarm swarm = _root.Session.Swarm;

            for (int rat = 0; rat < 3; rat++)
            {
                swarm.Enchant(rat, new FaintTrance(1f), 0f);
            }

            yield return null;
            yield return null;

            Assert.That(_sound.PlayCount, Is.EqualTo(before + 1));
        }

        [UnityTest]
        public IEnumerator Sound_ManySqueaks_AreNotAllTheSame()
        {
            yield return null;
            var heard = new System.Collections.Generic.HashSet<SoundCue>();

            for (int squeak = 0; squeak < 30; squeak++)
            {
                _root.Session.Swarm.Enchant(0, new FaintTrance(1f), 0f);
                yield return new WaitForSeconds(_config.SqueakGapSeconds * 1.2f);

                if (Contains(_config.Squeaks, _sound.LastPlayed))
                {
                    heard.Add(_sound.LastPlayed);
                }
            }

            Assert.That(heard.Count, Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator Sound_DiscoAfterConga_StartsOnTheTick()
        {
            yield return null;
            _root.Session.Stage.StartConga(0.1f);

            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            _root.Session.Stage.StartDisco(5f);
            yield return null;
            yield return null;

            Assert.That(_sound.HasPlayed(_config.DiscoTick), Is.True, "Every Disco starts on the same beat.");
        }

        [UnityTest]
        public IEnumerator Sound_DuringDisco_KeepsABeat()
        {
            yield return null;

            _root.Session.Stage.StartDisco(5f);

            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
            }

            Assert.That(_sound.HasPlayed(_config.DiscoKick), Is.True);
        }

        [UnityTest]
        public IEnumerator Sound_DuringConga_KeepsABeat()
        {
            yield return null;

            _root.Session.Stage.StartConga(5f);

            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
            }

            Assert.That(_sound.HasPlayed(_config.CongaBeat), Is.True);
        }

        [UnityTest]
        public IEnumerator Sound_AfterTheAudioDeviceChanges_StillReachesTheListener()
        {
            yield return null;
            AudioSettings.Reset(AudioSettings.GetConfiguration());
            yield return new WaitForSecondsRealtime(0.5f);
            yield return ListenerMeter.Connect();

            int before = _sound.PlayCount;
            _root.Session.Tick(new PiperIntent(Vector2.zero, 0, 0), _root.Session.TimeLeft + 1f);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_sound.PlayCount, Is.GreaterThan(before));
            Assert.That(ListenerMeter.Peak(), Is.GreaterThan(0.001f));
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<SoundCue> cues, SoundCue cue)
        {
            foreach (SoundCue candidate in cues)
            {
                if (candidate == cue)
                {
                    return true;
                }
            }

            return false;
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
