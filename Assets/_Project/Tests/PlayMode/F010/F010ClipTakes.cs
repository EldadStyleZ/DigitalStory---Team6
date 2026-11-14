#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Short frame sequences of the hive defense scene for a person to watch; they check nothing. The swarm standing, walking
    /// and standing again (the river's wave), the giant's two hand launches, and three maps from above. Explicit, so only a
    /// run that names this class takes them, and ignored unless F010_SNAPSHOT_DIR names a folder. The frames go to its
    /// clips/&lt;take&gt; folders as frame-0000.png and on, 30 to a game second, for a tool such as ffmpeg to join.
    /// </summary>
    public sealed class F010ClipTakes
    {
        private const string ExplicitReason = "Writes frame sequences for a person to watch; run it by name.";
        private const string DirectoryVariable = "F010_SNAPSHOT_DIR";
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int Width = 960;
        private const int Height = 540;
        private const int FramesToSettle = 3;
        private const float FrameSeconds = 1f / 30f;
        private const float SettleSeconds = 1f;

        // The wave: standing, walking north, standing again.
        private const float StandBeforeSeconds = 0.8f;
        private const float WalkSeconds = 1.3f;
        private const float StandAfterSeconds = 1.7f;

        // The hands: the giant forms 4 m north of the middle, where the chase camera behind it has no scenery or nest tree to
        // pull in for, with three stunned sticks 8 to 12 m ahead; the right hand launches, then the left, and the take runs
        // until the left arm has grown back. The side take watches the same from a fixed camera off the giant's right.
        // Fills Fury at any rate up to 20 damage a point (11.5 since songs-v2 V, when 1,000 stopped filling it).
        private const float FuryDamage = 2000f;
        private const float StunSeconds = 60f;
        private const float MaxWaitSeconds = 60f;
        private const float SwoopTolerance = 0.001f;
        private const float GiantSettleSeconds = 0.6f;
        private const float BeforeRightSeconds = 0.3f;
        private const float RightToLeftSeconds = 0.7f;
        private const float AfterLeftSeconds = 3f;
        private static readonly Vector3 GiantCore = new Vector3(0f, 0f, 4f);

        private static readonly Vector3[] GiantBeekeepers =
        {
            new Vector3(-2.5f, 0f, 13f), new Vector3(1.5f, 0f, 15.5f), new Vector3(4.5f, 0f, 11.5f),
        };

        private static readonly Vector3 SideFocus = new Vector3(0.5f, 2.5f, 9.5f);
        private static readonly Vector3 SideOffset = new Vector3(15f, 7f, -9f);

        // The maps: the whole arena from the south, as snapshot 19 shows it, a second each.
        private const float MapSeconds = 1f;
        private const float OverviewDistance = 62f;
        private const float OverviewPitch = 58f;
        private static readonly Vector3 OverviewFocus = new Vector3(0f, 0f, -4f);
        private static readonly int[] MapSeeds = { 101, 202, 303 };

        private string _directory;
        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;
        private HiveDefenseRoot _root;
        private SwarmCoreMotor _core;
        private Camera _camera;
        private SnapshotCapturer _capturer;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            string root = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrEmpty(root))
            {
                Assert.Ignore($"Set {DirectoryVariable} to a folder to write the F-010 clips.");
            }

            _directory = Path.Combine(root, "clips");
            _savedTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            Time.captureDeltaTime = FrameSeconds;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            MatchSeed.Fixed = F010MapSeedFixture.Seed;
            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            yield return null;
        }

        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take1_IdleWalkIdle()
        {
            yield return LoadScene();
            string folder = TakeFolder("take-1-idle-walk-idle");
            RenderTexture overlay = BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy);
            try
            {
                yield return null;
                int frame = 0;
                yield return Record(folder, StandBeforeSeconds, frame, null);
                frame += FramesIn(StandBeforeSeconds);
                yield return Record(folder, WalkSeconds, frame, () => _core.Move(Vector3.forward, 1f, false, Time.deltaTime));
                frame += FramesIn(WalkSeconds);
                yield return Record(folder, StandAfterSeconds, frame, null);
            }
            finally
            {
                EndHudOverlay(overlay, document, original, copy);
            }
        }

        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take2_GiantHands()
        {
            yield return LoadScene();
            yield return StageGiant();
            string folder = TakeFolder("take-2-giant-hands");
            RenderTexture overlay = BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy);
            try
            {
                yield return null;
                int frame = 0;
                yield return Record(folder, BeforeRightSeconds, frame, null);
                frame += FramesIn(BeforeRightSeconds);
                _root.PressHand(ColossusHand.Right);
                yield return Record(folder, RightToLeftSeconds, frame, null);
                frame += FramesIn(RightToLeftSeconds);
                _root.PressHand(ColossusHand.Left);
                yield return Record(folder, AfterLeftSeconds, frame, null);
            }
            finally
            {
                EndHudOverlay(overlay, document, original, copy);
            }

            Assert.That(_root.Session.IsColossusActive, Is.True, "the giant ended during the take");
        }

        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take2b_GiantHandsSide()
        {
            yield return LoadScene();
            yield return StageGiant();
            string folder = TakeFolder("take-2b-giant-hands-side");
            Vector3 position = SideFocus + SideOffset;
            Quaternion rotation = Quaternion.LookRotation(SideFocus - position, Vector3.up);
            int frame = 0;
            yield return RecordPosed(folder, BeforeRightSeconds, frame, position, rotation);
            frame += FramesIn(BeforeRightSeconds);
            _root.PressHand(ColossusHand.Right);
            yield return RecordPosed(folder, RightToLeftSeconds, frame, position, rotation);
            frame += FramesIn(RightToLeftSeconds);
            _root.PressHand(ColossusHand.Left);
            yield return RecordPosed(folder, AfterLeftSeconds, frame, position, rotation);
            Assert.That(_root.Session.IsColossusActive, Is.True, "the giant ended during the take");
        }

        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take3_ThreeMaps()
        {
            string folder = TakeFolder("take-3-three-maps");
            int frame = 0;
            bool wasFoggy = RenderSettings.fog;
            try
            {
                foreach (int seed in MapSeeds)
                {
                    MatchSeed.Fixed = seed;
                    yield return LoadScene();
                    RenderSettings.fog = false;
                    Quaternion rotation = Quaternion.Euler(OverviewPitch, 0f, 0f);
                    Vector3 position = OverviewFocus - (rotation * Vector3.forward * OverviewDistance);
                    int frames = FramesIn(MapSeconds);
                    for (int index = 0; index < frames; index++)
                    {
                        _capturer.RequestCapture(FramePath(folder, frame + index), position, rotation);
                        yield return WaitForCapture();
                    }

                    frame += frames;
                }
            }
            finally
            {
                RenderSettings.fog = wasFoggy;
            }
        }

        private IEnumerator LoadScene()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = UnityEngine.Object.FindAnyObjectByType<HiveDefenseRoot>();
            _core = UnityEngine.Object.FindAnyObjectByType<SwarmCoreMotor>();
            _camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            Assert.That(_root, Is.Not.Null);
            Assert.That(_core, Is.Not.Null);
            Assert.That(_camera, Is.Not.Null);

            // Made in the loaded scene, so the next scene load removes it.
            _capturer = new GameObject("Clip Capturer").AddComponent<SnapshotCapturer>();
            _capturer.Initialize(_camera, Width, Height);
            yield return WaitGameSeconds(SettleSeconds);
        }

        // Three stunned sticks ahead of the core, then the giant formed at full Fury, with the chase camera settled behind it.
        private IEnumerator StageGiant()
        {
            HiveDefenseSession session = _root.Session;
            _core.PlaceAt(GiantCore, true);
            session.BeekeeperSpawned += keeper => keeper.Stun(StunSeconds);
            _root.CallWave();
            float waited = 0f;
            while (session.ActiveBeekeeperCount < GiantBeekeepers.Length && waited < MaxWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(GiantBeekeepers.Length));
            for (int index = 0; index < GiantBeekeepers.Length; index++)
            {
                BeekeeperBrain keeper = session.ActiveBeekeeper(index);
                keeper.Spawn(GiantBeekeepers[index]);
                keeper.Stun(StunSeconds);
            }

            session.Fury.AddDamage(FuryDamage);
            IReadOnlyList<int> keys = ColossusKeys(session);
            for (int index = 0; index < keys.Count; index++)
            {
                _root.PressHole(keys[index]);
                yield return null;
            }

            yield return null;
            var follow = UnityEngine.Object.FindAnyObjectByType<FollowCamera>();
            waited = 0f;
            while ((!session.IsColossusActive || follow.ChaseBlend < 1f - SwoopTolerance) && waited < MaxWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.IsColossusActive, Is.True, "The Colossus keys at full Fury did not form the Colossus.");
            yield return WaitGameSeconds(GiantSettleSeconds);
        }

        private static IReadOnlyList<int> ColossusKeys(HiveDefenseSession session)
        {
            SwarmSongCaster caster = session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                if (caster[song] is ColossusSong)
                {
                    return caster[song].Song.Keys;
                }
            }

            Assert.Fail("The scene has no ColossusSong.");
            return null;
        }

        // One frame each game frame for the seconds, with the HUD over it, doing the step first on each.
        private IEnumerator Record(string folder, float seconds, int firstFrame, Action step)
        {
            int frames = FramesIn(seconds);
            for (int index = 0; index < frames; index++)
            {
                step?.Invoke();
                _capturer.RequestOverlayRead(FramePath(folder, firstFrame + index));
                yield return WaitForCapture();
            }
        }

        // The same from a fixed camera, without the HUD.
        private IEnumerator RecordPosed(string folder, float seconds, int firstFrame, Vector3 position, Quaternion rotation)
        {
            int frames = FramesIn(seconds);
            for (int index = 0; index < frames; index++)
            {
                _capturer.RequestCapture(FramePath(folder, firstFrame + index), position, rotation);
                yield return WaitForCapture();
            }
        }

        private IEnumerator WaitForCapture()
        {
            do
            {
                yield return null;
            }
            while (_capturer.IsBusy);
        }

        private RenderTexture BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy)
        {
            document = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            Assert.That(document, Is.Not.Null);
            original = document.panelSettings;
            copy = UnityEngine.Object.Instantiate(original);
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            copy.targetTexture = target;
            copy.clearColor = false;
            document.panelSettings = copy;
            _capturer.BeginOverlay(target);
            return target;
        }

        private void EndHudOverlay(RenderTexture target, UIDocument document, PanelSettings original, PanelSettings copy)
        {
            _capturer.EndOverlay();
            document.panelSettings = original;
            UnityEngine.Object.Destroy(copy);
            target.Release();
            UnityEngine.Object.Destroy(target);
        }

        private string TakeFolder(string take)
        {
            string folder = Path.Combine(_directory, take);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
            return folder;
        }

        private static string FramePath(string folder, int frame)
        {
            return Path.Combine(folder, "frame-" + frame.ToString("0000", System.Globalization.CultureInfo.InvariantCulture) + ".png");
        }

        private static int FramesIn(float seconds) => Mathf.RoundToInt(seconds / FrameSeconds);

        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }
}
#endif
