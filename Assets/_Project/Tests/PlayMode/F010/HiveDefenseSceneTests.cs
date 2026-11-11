#if UNITY_EDITOR
using System.Collections;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>Loads the Wasp Piper scene and checks that it starts, draws the swarm and answers the flute.</summary>
    public sealed class HiveDefenseSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const int FramesToRun = 60;
        private const int StandingHives = 2;
        private const int StartingWasps = 5000;
        private const float FastTimeScale = 5f;
        private const int MaxFramesForWave = 300000;
        private const float MaxWaveSeconds = 40f;
        private const int FramesBetweenNotes = 1;

        private static readonly int[] DomeHoles = { 0, 1, 0 };
        private static readonly int[] MeteorHoles = { 4, 2, 0 };

        private HiveDefenseRoot _root;
        private float _savedTimeScale;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedTimeScale = Time.timeScale;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_RunsSixtyFrames_LogsNoError()
        {
            for (int frame = 0; frame < FramesToRun; frame++)
            {
                yield return null;
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Scene_OnLoad_HasAPlayingSessionWithBothHives()
        {
            Assert.That(_root, Is.Not.Null);
            Assert.That(_root.Session, Is.Not.Null);
            Assert.That(_root.Session.State, Is.EqualTo(HiveDefenseState.Playing));
            Assert.That(_root.Session.Hives.StandingCount, Is.EqualTo(StandingHives));
            Assert.That(_root.Session.Swarm.Count, Is.EqualTo(StartingWasps));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SwarmView_RunsAfterStart_AndStopsWhenDisabled()
        {
            SwarmGpuView view = Object.FindAnyObjectByType<SwarmGpuView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.IsRunning, Is.True, "The buffers are made when the scene starts.");

            view.gameObject.SetActive(false);
            yield return null;

            Assert.That(view.IsRunning, Is.False, "The buffers are released when the object is disabled.");
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheDomeKeys_FormsTheDome()
        {
            for (int index = 0; index < DomeHoles.Length; index++)
            {
                _root.PressHole(DomeHoles[index]);
                for (int frame = 0; frame < FramesBetweenNotes; frame++)
                {
                    yield return null;
                }
            }

            yield return null;
            yield return null;

            Assert.That(_root.Session.Forms.Current, Is.EqualTo(SwarmForm.Dome));
        }

        [UnityTest]
        public IEnumerator PressHole_WithTheMeteorKeys_FormsTheMeteor()
        {
            for (int index = 0; index < MeteorHoles.Length; index++)
            {
                _root.PressHole(MeteorHoles[index]);
                for (int frame = 0; frame < FramesBetweenNotes; frame++)
                {
                    yield return null;
                }
            }

            yield return null;
            yield return null;

            Assert.That(_root.Session.Forms.Current, Is.EqualTo(SwarmForm.Meteor));
        }

        [UnityTest]
        public IEnumerator Scene_PastTheFirstWaveDelay_HasAnActiveBeekeeper()
        {
            Time.timeScale = FastTimeScale;

            // Batch mode runs thousands of frames a second, so count game time, not frames.
            int frames = 0;
            float elapsed = 0f;
            while (_root.Session.ActiveBeekeeperCount < 1 && elapsed < MaxWaveSeconds && frames < MaxFramesForWave)
            {
                elapsed += Time.deltaTime;
                frames++;
                yield return null;
            }

            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1));
        }
    }
}
#endif
