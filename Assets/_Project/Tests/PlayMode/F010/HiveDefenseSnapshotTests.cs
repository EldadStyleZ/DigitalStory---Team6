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
    /// Renders the hive defense scene's camera into PNG files so a reviewer can see the swarm without opening the Editor.
    /// Each test is ignored unless the F010_SNAPSHOT_DIR environment variable names a folder. Every song is played with
    /// the keys on its own asset, so a change of keys never breaks these tests.
    /// </summary>
    /// <remarks>
    /// Batch mode runs thousands of frames a second, so every frame is pinned to 1/30 s of game time with
    /// Time.captureDeltaTime: each wait below is in game seconds, and the swarm has really moved by each picture.
    /// The pictures are taken by <see cref="SnapshotCapturer"/>, after every LateUpdate of the frame.
    /// </remarks>
    [Category("F010Snapshot")]
    public sealed class HiveDefenseSnapshotTests
    {
        private const string DirectoryVariable = "F010_SNAPSHOT_DIR";
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string CloseSuffix = "-close";
        private const int Width = 960;
        private const int Height = 540;
        private const int FramesToSettle = 3;
        private const int HudFrames = 4;
        private const float FrameSeconds = 1f / 30f;
        private const float StandSeconds = 3f;
        private const float WalkSeconds = 4f;
        private const float TurnRate = 0.45f;
        private const float SongSeconds = 1.5f;
        private const float StingSeconds = 0.7f;
        private const float TornadoSeconds = 0.8f;
        private const float ColossusSeconds = 2.5f;
        private const float MaxColossusWaitSeconds = 2f;
        private const float FuryDamage = 1000f;
        private const float MaxWaveSeconds = 60f;
        private const float MaxWalkSeconds = 15f;
        private const float NearBeekeeperMeters = 6f;
        private const float StingFromMeters = 9f;
        private const float SettleCameraSeconds = 1f;
        private const float StingSettleSeconds = 0.5f;
        private const int WantedBeekeepers = 2;
        private const float CloseDistance = 6f;
        private const float ClosePitch = 35f;
        private const float CloseFocusHeight = 1f;
        private const float WideCloseDistance = 11f;
        private const float LowPitch = 20f;
        private const float ColossusFocusHeight = 2f;
        private const float TornadoFocusHeight = 1.5f;
        private const float HurtShare = 0.4f;
        private const float HiveShowSeconds = 0.3f;
        private const string GameSuffix = "-game";
        private const float MeteorFromMeters = 10f;
        private const float MeteorRiseShotSeconds = 0.7f;
        private const float MeteorDiveShotSeconds = 1.05f;
        private const float MeteorImpactShotSeconds = 1.45f;
        private const float MeteorViewDistance = 20f;
        private const float MeteorViewPitch = 10f;
        private const float MeteorFocusHeight = 6.5f;
        private const float QuarterTurnDegrees = 90f;
        private const float StopAfterWalkMeters = 6f;
        private const float StopAfterWalkSeconds = 2.5f;
        private const float HintPauseSeconds = 0.5f;
        private const int KnownInHud = 3;
        private const float DomeWovenSeconds = 0.5f;
        private const float DomeAndStingFromMeters = 10f;
        private const float StingLaunchShotSeconds = 0.35f;
        private const float StingWrapShotSeconds = 1.2f;
        private const float TornadoGrowShotSeconds = 0.3f;
        private const string LaunchSuffix = "-launch";
        private const string WrapSuffix = "-wrap";
        private const string GrowSuffix = "-grow";

        private HiveDefenseRoot _root;
        private SwarmCoreMotor _core;
        private Camera _camera;
        private SnapshotCapturer _capturer;
        private string _directory;
        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _directory = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrEmpty(_directory))
            {
                Assert.Ignore($"Set {DirectoryVariable} to a folder to write the F-010 snapshots.");
            }

            Directory.CreateDirectory(_directory);
            _savedTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            Time.captureDeltaTime = FrameSeconds;
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

            // Made in the loaded scene, so the next test's scene load removes it.
            _capturer = new GameObject("Snapshot Capturer").AddComponent<SnapshotCapturer>();
            _capturer.Initialize(_camera, Width, Height);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Snapshot1_RiverStanding()
        {
            yield return Wait(StandSeconds);
            yield return Capture("snap-1-river-standing");
            yield return CaptureClose("snap-1-river-standing", CoreFocus());

            // One hive hurt (shorter bar and the red ring), one destroyed (flattened and dark, no bar).
            Hive hurt = _root.Session.Hives[0];
            Hive wrecked = _root.Session.Hives[1];
            hurt.Damage(hurt.MaxHealth * HurtShare);
            wrecked.Damage(wrecked.MaxHealth);
            yield return Wait(HiveShowSeconds);
            yield return CaptureClose("snap-1-hive", hurt.Position + (Vector3.up * CloseFocusHeight));
            yield return CaptureClose("snap-1-hive-destroyed", wrecked.Position + (Vector3.up * CloseFocusHeight));
        }

        [UnityTest]
        public IEnumerator Snapshot2_RiverWalking()
        {
            float elapsed = 0f;
            float angle = 0f;
            while (elapsed < WalkSeconds)
            {
                float dt = Time.deltaTime;
                angle += TurnRate * dt;
                _core.Move(new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)), 1f, false, dt);
                elapsed += dt;
                yield return null;
            }

            yield return Capture("snap-2-river-walking");
            yield return CaptureClose("snap-2-river-walking", CoreFocus());
        }

        [UnityTest]
        public IEnumerator Snapshot3_Dome()
        {
            yield return Wait(SongSeconds);
            yield return Play(KeysOf<DomeSong>());
            Assert.That(_root.Session.Forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            yield return Wait(SongSeconds);
            yield return Capture("snap-3-dome");
            yield return CaptureClose("snap-3-dome", CoreFocus());
        }

        [UnityTest]
        public IEnumerator Snapshot4_HiveCall()
        {
            yield return Wait(SongSeconds);
            yield return Play(KeysOf<HiveCallSong>());
            Assert.That(_root.Session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            yield return Wait(SongSeconds);
            yield return Capture("snap-4-hivecall");
        }

        [UnityTest]
        public IEnumerator Snapshot5_Wave()
        {
            yield return WaitForBeekeepers(WantedBeekeepers);
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(WantedBeekeepers));
            yield return WalkToNearestBeekeeper();
            yield return Wait(SettleCameraSeconds);

            yield return Capture("snap-5-wave");
            yield return CaptureClose("snap-5-wave", MidpointToNearestBeekeeper());
        }

        [UnityTest]
        public IEnumerator Snapshot6_Sting()
        {
            yield return WaitForBeekeepers(1);
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1));

            // Stops beyond the beekeeper's notice range, so it is still far enough away for the arc to show.
            yield return WalkToNearestBeekeeper(StingFromMeters);
            yield return Wait(StingSettleSeconds);

            yield return Play(KeysOf<StingSong>());
            float castTime = Time.time;
            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting));

            // The comet on its way, the wrap as the head arrives, and the two rings once every wasp is on them.
            yield return WaitSince(castTime, StingLaunchShotSeconds);
            yield return CaptureClose("snap-6-sting" + LaunchSuffix, MidpointToNearestBeekeeper());
            yield return WaitSince(castTime, StingSeconds);
            yield return Capture("snap-6-sting");
            yield return CaptureClose("snap-6-sting", MidpointToNearestBeekeeper());
            yield return WaitSince(castTime, StingWrapShotSeconds);
            Vector3 target = _root.Session.Forms.Slot(SongSlot.Strike).ParamA;
            yield return CaptureClose("snap-6-sting" + WrapSuffix, target + (Vector3.up * CloseFocusHeight));
        }

        [UnityTest]
        public IEnumerator Snapshot7_Colossus()
        {
            yield return Wait(SongSeconds);
            _root.Session.Fury.AddDamage(FuryDamage);
            Assert.That(_root.Session.Fury.IsFull, Is.True);
            yield return Play(KeysOf<ColossusSong>());

            float waited = 0f;
            while (!_root.Session.IsColossusActive && waited < MaxColossusWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(_root.Session.IsColossusActive, Is.True, "The Colossus keys at full Fury did not form the Colossus.");
            yield return Wait(ColossusSeconds);
            yield return Capture("snap-7-colossus");
            Vector3 giant = UnityEngine.Object.FindAnyObjectByType<ColossusMotor>().transform.position;
            yield return CaptureClose("snap-7-colossus", giant + (Vector3.up * ColossusFocusHeight), WideCloseDistance, LowPitch);
        }

        [UnityTest]
        public IEnumerator Snapshot8_Tornado()
        {
            yield return Wait(SongSeconds);
            yield return Play(KeysOf<TornadoSong>());
            float castTime = Time.time;
            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Tornado));

            // The hit: the column has grown from the ground and the shock ring flies out to the reach.
            yield return WaitSince(castTime, TornadoGrowShotSeconds);
            yield return CaptureClose(
                "snap-8-tornado" + GrowSuffix, _core.Position + (Vector3.up * TornadoFocusHeight), WideCloseDistance, LowPitch);
            yield return WaitSince(castTime, TornadoSeconds);
            yield return Capture("snap-8-tornado");
            yield return CaptureClose("snap-8-tornado", _core.Position + (Vector3.up * TornadoFocusHeight), WideCloseDistance, LowPitch);
        }

        // Camera.Render does not draw UI Toolkit, so the HUD panel is pointed at a render texture for a few frames,
        // through a runtime copy of its PanelSettings. The PanelSettings asset itself is never changed.
        // Two songs are found besides Sting, so the songbook shows three known songs and four empty slots, the "New song"
        // line, and all but the last of the Tornado's notes glowing as a hint.
        [UnityTest]
        public IEnumerator Snapshot9_Hud()
        {
            yield return Wait(SongSeconds);
            yield return Play(KeysOf<HiveCallSong>());
            yield return Wait(SongSeconds);
            yield return Play(KeysOf<DomeSong>());
            yield return Wait(HintPauseSeconds);
            IReadOnlyList<int> tornado = KeysOf<TornadoSong>();
            var hint = new int[tornado.Count - 1];
            for (int index = 0; index < hint.Length; index++)
            {
                hint[index] = tornado[index];
            }

            yield return Play(hint);
            Assert.That(_root.Session.Discovery.KnownCount, Is.EqualTo(KnownInHud));
            Assert.That(_root.Session.Discovery.HintNotes(_root.Session.Caster), Is.EqualTo(hint.Length));

            UIDocument document = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            Assert.That(document, Is.Not.Null);
            PanelSettings original = document.panelSettings;
            PanelSettings copy = UnityEngine.Object.Instantiate(original);
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            copy.targetTexture = target;
            copy.clearColor = false;

            try
            {
                document.panelSettings = copy;
                _capturer.BeginOverlay(target);
                for (int frame = 0; frame < HudFrames; frame++)
                {
                    yield return null;
                }

                _capturer.RequestOverlayRead(Path.Combine(_directory, "snap-9-hud.png"));
                while (_capturer.IsBusy)
                {
                    yield return null;
                }
            }
            finally
            {
                _capturer.EndOverlay();
                document.panelSettings = original;
                UnityEngine.Object.Destroy(copy);
                target.Release();
                UnityEngine.Object.Destroy(target);
            }

            Assert.That(_capturer.LastWasFlat, Is.False, "snap-9-hud.png is a single flat colour.");
        }

        // Each moment is taken twice: from the side, far and low enough to show the climb (the named picture), and
        // from the game camera (the -game picture), which looks down too steeply to see the top of the climb.
        [UnityTest]
        public IEnumerator Snapshot10_Meteor()
        {
            yield return WaitForBeekeepers(1);
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1));
            yield return WalkToNearestBeekeeper(MeteorFromMeters);
            yield return Wait(StingSettleSeconds);

            yield return Play(KeysOf<MeteorSong>());
            float castTime = Time.time;
            SlotState strike = _root.Session.Forms.Slot(SongSlot.Strike);
            Assert.That(strike.Form, Is.EqualTo(SwarmForm.Meteor));
            Vector3 core = _core.Position;
            Vector3 impact = strike.ParamA;

            yield return WaitSince(castTime, MeteorRiseShotSeconds);
            yield return CaptureMeteor("snap-10-meteor-rise", core, impact);
            yield return WaitSince(castTime, MeteorDiveShotSeconds);
            yield return CaptureMeteor("snap-10-meteor-dive", core, impact);
            yield return WaitSince(castTime, MeteorImpactShotSeconds);
            yield return CaptureMeteor("snap-10-meteor-impact", core, impact);
        }

        // Spec 9.3: once the core stops, the river curls back into the ring around it.
        [UnityTest]
        public IEnumerator Snapshot11_StopAfterWalk()
        {
            Vector3 start = _core.Position;
            float elapsed = 0f;
            while (GroundDistance(start, _core.Position) < StopAfterWalkMeters && elapsed < MaxWalkSeconds)
            {
                float dt = Time.deltaTime;
                _core.Move(Vector3.forward, 1f, false, dt);
                elapsed += dt;
                yield return null;
            }

            Assert.That(GroundDistance(start, _core.Position), Is.GreaterThanOrEqualTo(StopAfterWalkMeters));
            yield return Wait(StopAfterWalkSeconds);
            yield return Capture("snap-11-stop-after-walk");
            yield return CaptureClose("snap-11-stop-after-walk", CoreFocus());
        }

        // Plan section G, M1: the Dome (blue, around the core) and the Sting (red, on a beekeeper) at once, while the rest
        // of the swarm still flows as the yellow river.
        [UnityTest]
        public IEnumerator Snapshot12_DomeAndSting()
        {
            yield return WaitForBeekeepers(1);
            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1));

            // Far enough that the beekeeper, walking in, is still clear of the Dome when the picture is taken.
            yield return WalkToNearestBeekeeper(DomeAndStingFromMeters);
            yield return Play(KeysOf<DomeSong>());
            yield return Wait(DomeWovenSeconds);
            yield return Play(KeysOf<StingSong>());
            SwarmFormMachine forms = _root.Session.Forms;
            Assert.That(forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting));
            yield return Wait(StingSeconds);
            Assert.That(forms.Slot(SongSlot.Guard).IsActive && forms.Slot(SongSlot.Strike).IsActive, Is.True, "both songs run in the picture");

            yield return Capture("snap-12-dome-and-sting");
            yield return CaptureClose("snap-12-dome-and-sting", MidpointToNearestBeekeeper(), WideCloseDistance, ClosePitch);
        }

        private IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Waits until this many game seconds have passed since startTime, a Time.time.
        private IEnumerator WaitSince(float startTime, float seconds)
        {
            while (Time.time - startTime < seconds)
            {
                yield return null;
            }
        }

        private static float GroundDistance(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset.magnitude;
        }

        // The keys on the scene's own song asset.
        private IReadOnlyList<int> KeysOf<T>() where T : SwarmSongEffect
        {
            HiveDefenseSession session = _root.Session;
            for (int song = 0; song < session.Caster.Count; song++)
            {
                if (session.Caster[song] is T)
                {
                    return session.Caster[song].Song.Keys;
                }
            }

            Assert.Fail("The scene has no " + typeof(T).Name + ".");
            return null;
        }

        private IEnumerator Play(IReadOnlyList<int> holes)
        {
            for (int index = 0; index < holes.Count; index++)
            {
                _root.PressHole(holes[index]);
                yield return null;
            }

            // The last note is read on the next tick.
            yield return null;
        }

        private IEnumerator WaitForBeekeepers(int wanted)
        {
            float elapsed = 0f;
            while (_root.Session.ActiveBeekeeperCount < wanted && elapsed < MaxWaveSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator WalkToNearestBeekeeper() => WalkToNearestBeekeeper(NearBeekeeperMeters);

        // Walks the core toward the nearest beekeeper until it is this close, so both are in the picture.
        private IEnumerator WalkToNearestBeekeeper(float stopMeters)
        {
            float elapsed = 0f;
            while (elapsed < MaxWalkSeconds && TryNearestBeekeeper(out BeekeeperBrain nearest))
            {
                Vector3 offset = nearest.Position - _core.Position;
                offset.y = 0f;
                if (offset.magnitude <= stopMeters)
                {
                    break;
                }

                float dt = Time.deltaTime;
                _core.Move(offset.normalized, 1f, false, dt);
                elapsed += dt;
                yield return null;
            }
        }

        private bool TryNearestBeekeeper(out BeekeeperBrain nearest)
        {
            nearest = null;
            float best = float.MaxValue;
            HiveDefenseSession session = _root.Session;
            for (int index = 0; index < session.ActiveBeekeeperCount; index++)
            {
                BeekeeperBrain brain = session.ActiveBeekeeper(index);
                if (brain == null || !brain.IsAlive)
                {
                    continue;
                }

                float distance = (brain.Position - _core.Position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = brain;
                }
            }

            return nearest != null;
        }

        private Vector3 CoreFocus() => _core.Position + (Vector3.up * CloseFocusHeight);

        private Vector3 MidpointToNearestBeekeeper()
        {
            if (!TryNearestBeekeeper(out BeekeeperBrain nearest))
            {
                return CoreFocus();
            }

            return ((_core.Position + nearest.Position) * 0.5f) + (Vector3.up * CloseFocusHeight);
        }

        private IEnumerator Capture(string name)
        {
            string path = Path.Combine(_directory, name + ".png");
            _capturer.RequestCapture(path);
            yield return WaitForCapture(name);
        }

        private IEnumerator CaptureClose(string name, Vector3 focus) => CaptureClose(name, focus, CloseDistance, ClosePitch);

        private IEnumerator CaptureClose(string name, Vector3 focus, float distance, float pitch)
        {
            return CapturePosed(name + CloseSuffix, focus, distance, pitch, _camera.transform.eulerAngles.y);
        }

        // Looks across the line from the core to the impact, so both stand apart in the picture, then from the game camera.
        private IEnumerator CaptureMeteor(string name, Vector3 core, Vector3 impact)
        {
            Vector3 across = impact - core;
            across.y = 0f;
            float yaw = _camera.transform.eulerAngles.y;
            if (across.sqrMagnitude > 0f)
            {
                yaw = (Mathf.Atan2(across.x, across.z) * Mathf.Rad2Deg) + QuarterTurnDegrees;
            }

            Vector3 focus = ((core + impact) * 0.5f) + (Vector3.up * MeteorFocusHeight);
            yield return CapturePosed(name, focus, MeteorViewDistance, MeteorViewPitch, yaw);
            yield return Capture(name + GameSuffix);
        }

        // A temporary pose this far from the focus. The capturer puts the camera back afterwards.
        private IEnumerator CapturePosed(string fileName, Vector3 focus, float distance, float pitch, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = focus - (rotation * Vector3.forward * distance);
            _capturer.RequestCapture(Path.Combine(_directory, fileName + ".png"), position, rotation);
            yield return WaitForCapture(fileName);
        }

        private IEnumerator WaitForCapture(string name)
        {
            while (_capturer.IsBusy)
            {
                yield return null;
            }

            Assert.That(File.Exists(Path.Combine(_directory, name + ".png")), Is.True, $"{name}.png was not written.");
            Assert.That(_capturer.LastWasFlat, Is.False, $"{name}.png is a single flat colour.");
        }
    }
}
#endif
