#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plan section AC, the settings panel in the real scene: opening it pauses the match through the root's one clock and
    /// ignores notes, closing it lets time run again; a volume change reaches the note voice's music gain and the recorded
    /// sounds' effects gain at once; and the volumes come back after a reload of the scene through the store given to the
    /// root, never through the player's own PlayerPrefs.
    /// </summary>
    public sealed class HiveDefenseSettingsTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const float FrameSeconds = 1f / 30f;
        private const int Rate = 48000;

        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;
        private AudioClip _clip;
        private GameSoundLibrary _standIn;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            _savedTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            Time.captureDeltaTime = FrameSeconds;
            _clip = AudioClip.Create("Stand-in", Rate / 4, 1, Rate, false);
            _standIn = ScriptableObject.CreateInstance<GameSoundLibrary>();
            _standIn.SetSlot(GameSoundEvent.BeekeeperHit, new GameSoundSlot(new[] { _clip }, 0.3f, 0f, 2, 0f, 80));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            HiveDefenseRoot root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            if (root != null)
            {
                root.ShowSettings(false);
            }

            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            Object.Destroy(_standIn);
            Object.Destroy(_clip);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Opening_PausesTheMatch_IgnoresNotes_AndClosing_LetsItRun()
        {
            HiveDefenseRoot root = null;
            yield return LoadScene(found => root = found);
            root.UseVolumeStore(new MemoryStore());
            SwarmFluteVoice voice = Object.FindAnyObjectByType<SwarmFluteVoice>();

            root.ShowSettings(true);
            yield return null;

            Assert.That(root.IsSettingsOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "the panel did not pause the match");
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None), "the cursor is not free while the panel is open");
            int recent = root.Session.Caster.RecentNoteCount;
            root.PressHole(1);
            yield return null;
            yield return null;
            Assert.That(root.Session.Caster.RecentNoteCount, Is.EqualTo(recent), "a note reached the songs while paused");
            Assert.That(voice.HolePlaying, Is.Not.EqualTo(1), "the pressed note sounded while paused");

            root.ShowSettings(false);
            yield return null;

            Assert.That(root.IsSettingsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "closing did not let the match run");
        }

        [UnityTest]
        public IEnumerator AVolumeChange_ReachesTheNoteVoice_AndTheRecordedSounds_AtOnce()
        {
            HiveDefenseRoot root = null;
            yield return LoadScene(found => root = found);
            root.UseVolumeStore(new MemoryStore());
            root.UseRecordedSounds(_standIn);
            SwarmFluteVoice voice = Object.FindAnyObjectByType<SwarmFluteVoice>();
            HitSoundView hitSound = Object.FindAnyObjectByType<HitSoundView>();
            root.ShowSettings(true);

            root.Volumes.Set(VolumeGroup.Master, 80);
            root.Volumes.Set(VolumeGroup.Music, 50);
            root.Volumes.Set(VolumeGroup.Effects, 40);

            Assert.That(voice.MusicGain, Is.EqualTo(0.64f * 0.25f).Within(1e-5f));
            Assert.That(root.GameSound.EffectsGain, Is.EqualTo(0.64f * 0.16f).Within(1e-5f));
            Assert.That(root.Settings.PercentText(VolumeGroup.Music), Is.EqualTo("50%"));
            Assert.That(hitSound, Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheVolumes_ComeBackAfterAReload_ThroughTheStore()
        {
            var store = new MemoryStore();
            HiveDefenseRoot root = null;
            yield return LoadScene(found => root = found);
            root.UseVolumeStore(store);
            root.ShowSettings(true);
            root.Volumes.Set(VolumeGroup.Music, 30);
            root.Volumes.Set(VolumeGroup.Effects, 70);
            root.ShowSettings(false);
            Assert.That(store.Saves, Is.EqualTo(1), "closing the panel did not save");

            yield return LoadScene(found => root = found);
            root.UseVolumeStore(store);

            Assert.That(root.Volumes.PercentOf(VolumeGroup.Music), Is.EqualTo(30));
            Assert.That(root.Volumes.PercentOf(VolumeGroup.Effects), Is.EqualTo(70));
            SwarmFluteVoice voice = Object.FindAnyObjectByType<SwarmFluteVoice>();
            Assert.That(voice.MusicGain, Is.EqualTo(0.09f).Within(1e-5f), "the reloaded volume did not reach the voice");
        }

        // Plan section AD: the frame-rate and render-scale rows under the volumes. In the Editor nothing reaches the screen, so
        // what would be applied is read from the root's applier.
        [UnityTest]
        public IEnumerator TheDisplayChoices_ShowInThePanel_AreApplied_AndComeBackAfterAReload()
        {
            var store = new MemoryStore();
            HiveDefenseRoot root = null;
            yield return LoadScene(found => root = found);
            root.UseVolumeStore(store);
            root.ShowSettings(true);

            root.Settings.MoveFocus(10);
            Assert.That(root.Settings.FocusedRow, Is.EqualTo(SettingsRow.RenderScale), "the render scale is the last row");
            root.Display.StepFrameRate(2);
            root.Display.StepRenderScale(2);

            Assert.That(root.Settings.ValueText(SettingsRow.FrameRate), Does.Contain("60 fps"));
            Assert.That(root.Settings.ValueText(SettingsRow.RenderScale), Does.Contain("75%"));
            FramePacing pacing = root.DisplayApplier.LastPacing;
            Assert.That(pacing.VSyncCount > 0 || pacing.TargetFrameRate == 60, Is.True, "60 fps is neither vsync pacing nor a 60 cap");
            Assert.That(root.DisplayApplier.LastScale, Is.EqualTo(0.75f).Within(1e-5f));
            root.ShowSettings(false);
            Assert.That(store.Saves, Is.EqualTo(1), "closing the panel did not save the display choices");

            yield return LoadScene(found => root = found);
            root.UseVolumeStore(store);

            Assert.That(root.Display.FrameRate, Is.EqualTo(FrameRateChoice.Rate60));
            Assert.That(root.Display.RenderScale, Is.EqualTo(RenderScaleChoice.ThreeQuarters));
            Assert.That(root.Settings.ValueText(SettingsRow.RenderScale), Does.Contain("75%"));
        }

        private static IEnumerator LoadScene(System.Action<HiveDefenseRoot> found)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            HiveDefenseRoot root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            Assert.That(root, Is.Not.Null);
            found(root);
        }

        private sealed class MemoryStore : IVolumeStore
        {
            private readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int Saves { get; private set; }

            public bool TryGet(string key, out int value) => _values.TryGetValue(key, out value);

            public void Set(string key, int value) => _values[key] = value;

            public void Save() => Saves++;
        }
    }
}
#endif
