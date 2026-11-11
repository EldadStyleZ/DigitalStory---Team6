#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor;
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
        private const float HiveCallShotSeconds = 0.4f;
        private const float MoveLessonSeconds = 0.4f;
        private const float BannerShotSeconds = 0.6f;
        private const float OutsideRingMetres = 4.5f;
        private const float BesideHiveMetres = 2f;
        private const float FlowShotSeconds = 0.3f;
        private const int HalfSongNotesMissing = 1;
        private const string NestConfigPath = "Assets/_Project/GameData/F010/NestConfig.asset";
        private const string CoreMoveConfigPath = "Assets/_Project/GameData/F010/CoreMoveConfig.asset";
        private const float NestShowSeconds = 1.5f;
        private const float NestFocusHeight = 2.5f;
        private const float BesideNestMetres = 1.5f;
        private const float OutsideNestRingMetres = 5f;
        private const float NestStayMarginSeconds = 0.3f;
        private const float HalfCollectShare = 0.5f;
        private const float NestRingPitch = 45f;
        private const float NestsInViewBackMetres = 4f;
        private const float NestsSettleSeconds = 2f;
        private const float NestBehindCameraMetres = 12f;
        private const float InsideEdgeMetres = 4f;
        private const int LevelFiveNests = 4;
        private const float DomeShotSeconds = 1.5f;
        private const float DomeFocusHeight = 1f;
        private const float DomeShotDistance = 10f;
        private const float TornadoShotDistance = 18f;
        private const float LevelShotPitch = 30f;
        private const string LevelOneSuffix = "-level1";
        private const string LevelFiveSuffix = "-level5";

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

        // Hive Call lasts only while the hives' stores flow in, under a second at the start of a match.
        [UnityTest]
        public IEnumerator Snapshot4_HiveCall()
        {
            yield return Wait(SongSeconds);

            // The swarm starts full, and Hive Call ends at once with no room for the stores, so half of it is lost first.
            _root.Session.Swarm.Lose(_root.Session.Swarm.Count / 2);
            yield return Play(KeysOf<HiveCallSong>());
            Assert.That(_root.Session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall));
            yield return Wait(HiveCallShotSeconds);
            Assert.That(_root.Session.Forms.Current, Is.EqualTo(SwarmForm.HiveCall), "Hive Call ended before the picture");
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

            yield return CaptureHud("snap-9-hud");
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

        // Plan sections L and N in the first farm phase: the phase line and its timer, the lesson's second prompt once the core
        // has moved, the song table with the Sting known and the Meteor filled in but for its last note (and the Tornado's
        // first two notes), and the hive panels with their stores.
        [UnityTest]
        public IEnumerator Snapshot13_Farm()
        {
            yield return Wait(SongSeconds);
            yield return Walk(Vector3.forward, MoveLessonSeconds);
            IReadOnlyList<int> meteor = KeysOf<MeteorSong>();
            var start = new int[meteor.Count - HalfSongNotesMissing];
            for (int index = 0; index < start.Length; index++)
            {
                start[index] = meteor[index];
            }

            yield return Play(start);
            yield return Wait(HintPauseSeconds);

            HiveDefenseSession session = _root.Session;
            Assert.That(session.Phase, Is.EqualTo(MatchPhase.Farm));
            Assert.That(session.Tutorial.Current, Is.EqualTo(TutorialStep.PlaySting));
            Assert.That(session.Discovery.KnownNotes(SongIndex<MeteorSong>()), Is.EqualTo(start.Length));
            Assert.That(session.Discovery.IsKnown(SongIndex<MeteorSong>()), Is.False);
            yield return CaptureHud("snap-13-farm");
        }

        // Plan section M: the core beside a hive while its store flows into the swarm: the bright collect ring, the motes
        // rising from the hive to the core, and the store bar under the health bar. The core waits outside the ring first,
        // so the camera has caught up with it.
        [UnityTest]
        public IEnumerator Snapshot13_Collect()
        {
            Hive hive = _root.Session.Hives[0];

            // The swarm starts full, and a full swarm takes no store, so half of it is lost first.
            _root.Session.Swarm.Lose(_root.Session.Swarm.Count / 2);
            yield return ApproachHive(hive.Position);
            Assert.That(_root.Session.NearHive, Is.EqualTo(0));
            Assert.That(_root.Session.IsCollecting, Is.True);
            yield return Capture("snap-13-collect");
            yield return CaptureClose("snap-13-collect", HiveAndCoreFocus(hive.Position));
        }

        // Plan section M: Give held beside the hurt hive: the swarm's wasps flow into it as green crosses and its health bar
        // grows, while collecting shows gold dots.
        [UnityTest]
        public IEnumerator Snapshot13_Give()
        {
            int last = _root.Session.Hives.Count - 1;
            Hive hive = _root.Session.Hives[last];
            Assert.That(hive.Health, Is.LessThan(hive.MaxHealth), "the last hive does not start hurt");
            _root.HoldGive(true);
            try
            {
                yield return ApproachHive(hive.Position);
                Assert.That(_root.Session.NearHive, Is.EqualTo(last));
                Assert.That(_root.Session.IsGiving, Is.True);
                yield return Capture("snap-13-give");
                yield return CaptureClose("snap-13-give", HiveAndCoreFocus(hive.Position));

                // The hive's panel says it is being healed.
                yield return CaptureHud("snap-13-give-hud");
            }
            finally
            {
                _root.HoldGive(false);
            }
        }

        // Plan sections L and M in a wave called early: the survive phase line, the "Wave 1" banner, the wasps granted for
        // the seconds skipped, and the wasp price on each known song's chip. The Dome and the Tornado were found for free in
        // the farm phase, so three chips show a price.
        [UnityTest]
        public IEnumerator Snapshot13_Survive()
        {
            yield return Wait(SongSeconds);
            yield return Play(KeysOf<DomeSong>());
            yield return Wait(HintPauseSeconds);
            yield return Play(KeysOf<TornadoSong>());
            Assert.That(_root.Session.Discovery.KnownCount, Is.EqualTo(KnownInHud));
            _root.CallWave();
            yield return Wait(BannerShotSeconds);
            Assert.That(_root.Session.Phase, Is.EqualTo(MatchPhase.Survive));
            Assert.That(_root.Session.AreSongsFree, Is.False);
            yield return CaptureHud("snap-13-survive");
        }

        // Plan section H: an open nest glows warm and pulses, with its light, its motes and the faint ring on the ground at
        // the collect radius.
        [UnityTest]
        public IEnumerator Snapshot14_NestOpen()
        {
            Nest nest = _root.Session.Nests[0];
            Assert.That(nest.IsOpen, Is.True);
            yield return Wait(NestShowSeconds);
            yield return CaptureClose("snap-14-nest-open", nest.Position + (Vector3.up * NestFocusHeight), WideCloseDistance, ClosePitch);
        }

        // Plan section H, from the game camera at night: an open nest and the nearest closed one standing beside it as the
        // camera sees them, both ahead of the core, so the glowing one and the dark one can be told apart.
        [UnityTest]
        public IEnumerator Snapshot14_NestsGame()
        {
            Assert.That(TryNestsSideBySide(out Nest open, out Nest closed), Is.True, "no open nest has a closed one beside it");
            float nearer = Mathf.Min(open.Position.z, closed.Position.z);
            _core.PlaceAt(new Vector3((open.Position.x + closed.Position.x) * 0.5f, 0f, nearer - NestsInViewBackMetres), true);
            yield return Wait(NestsSettleSeconds);
            yield return Capture("snap-14-nests-game");
        }

        // Plan section H: while the core collects, the ring on the ground fills clockwise from the top, here half way, and
        // motes stream from the nest down to it. The core waits outside the ring first, so the camera has caught up.
        [UnityTest]
        public IEnumerator Snapshot14_Collecting()
        {
            NestConfig config = LoadNestConfig();
            Nest nest = _root.Session.Nests[0];
            _core.PlaceAt(nest.Position + (Vector3.back * OutsideNestRingMetres), true);
            yield return Wait(SettleCameraSeconds);
            Assert.That(nest.Progress01, Is.EqualTo(0f), "the nest was being collected from outside its ring");

            _core.PlaceAt(nest.Position + (Vector3.back * BesideNestMetres), true);
            yield return Wait(config.CollectSeconds * HalfCollectShare);
            Assert.That(nest.Progress01, Is.GreaterThan(0.3f).And.LessThan(0.7f), "the collecting is not about half way");
            yield return CaptureClose("snap-14-collecting", nest.Position + (Vector3.up * CloseFocusHeight), WideCloseDistance, NestRingPitch);
        }

        // Plan section H: the same Dome at level 1 and at level 5, after four nests, from the same pose, side by side: the
        // shell is bigger by the level's size multiplier.
        [UnityTest]
        public IEnumerator Snapshot14_LevelDome()
        {
            return LevelShots<DomeSong>("snap-14-level-dome", SongSlot.Guard, SwarmForm.Dome, DomeShotSeconds, DomeFocusHeight, DomeShotDistance);
        }

        // The same for the Tornado: the column is wider and taller at level 5.
        [UnityTest]
        public IEnumerator Snapshot14_LevelTornado()
        {
            return LevelShots<TornadoSong>(
                "snap-14-level-tornado", SongSlot.Strike, SwarmForm.Tornado, TornadoSeconds, TornadoFocusHeight, TornadoShotDistance);
        }

        // Plan section H in the HUD: the level beside the wasps, "Nests 2/8", the level line and the hum line after the second
        // nest, the hummed song's first notes on its empty slot, and an arrow at the screen's edge to an open nest out of
        // sight. Clearing the first wave opens the next nests; NestField.OpenAfterWave is what the session calls then.
        [UnityTest]
        public IEnumerator Snapshot14_Hud()
        {
            NestConfig config = LoadNestConfig();
            HiveDefenseSession session = _root.Session;
            int hummed = -1;
            Action<int> onHummed = song => hummed = song;
            session.Discovery.Hummed += onHummed;
            try
            {
                yield return Wait(SongSeconds);
                yield return CollectNests(config.OpenAtStart);
            }
            finally
            {
                session.Discovery.Hummed -= onHummed;
            }

            Assert.That(session.Level, Is.EqualTo(1 + config.OpenAtStart));
            Assert.That(hummed, Is.GreaterThanOrEqualTo(0), "no nest hummed a song");
            Assert.That(session.Discovery.KnownNotes(hummed), Is.GreaterThanOrEqualTo(config.HummedNotes));

            // The core stands north of the southernmost open nest, so that nest is behind the camera, which looks north.
            session.Nests.OpenAfterWave(1);
            Nest open = SouthernmostOpenNest();
            Assert.That(open, Is.Not.Null, "clearing the first wave opened no nest");
            float edge = LoadCoreMove().PlayAreaHalfSize - InsideEdgeMetres;
            _core.PlaceAt(new Vector3(open.Position.x, 0f, Mathf.Min(open.Position.z + NestBehindCameraMetres, edge)), true);
            yield return Wait(SettleCameraSeconds);
            var arrows = UnityEngine.Object.FindAnyObjectByType<NestOffscreenIndicatorView>();
            Assert.That(arrows, Is.Not.Null, "the HUD has no NestOffscreenIndicatorView");
            Assert.That(arrows.ShownCount, Is.GreaterThanOrEqualTo(1), "no arrow points at an open nest out of sight");
            yield return CaptureHud("snap-14-hud");
        }

        // Casts the song at level 1 and takes it, collects four nests, comes back to the same spot and takes it again at level
        // 5, then puts the two pictures side by side, level 1 on the left.
        private IEnumerator LevelShots<T>(string name, SongSlot slot, SwarmForm form, float shotSeconds, float focusHeight, float distance)
            where T : SwarmSongEffect
        {
            Vector3 spot = _core.Position;
            Vector3 focus = spot + (Vector3.up * focusHeight);
            float yaw = _camera.transform.eulerAngles.y;
            yield return Wait(SongSeconds);
            Assert.That(_root.Session.Level, Is.EqualTo(1));
            yield return CastAndCapture<T>(name + LevelOneSuffix, slot, form, shotSeconds, focus, distance, yaw);

            yield return CollectNests(LevelFiveNests);
            Assert.That(_root.Session.Level, Is.EqualTo(1 + LevelFiveNests));
            _core.PlaceAt(spot, true);
            yield return Wait(StandSeconds);
            Assert.That(_root.Session.Forms.Slot(slot).IsActive, Is.False, "the level-1 song still runs");
            yield return CastAndCapture<T>(name + LevelFiveSuffix, slot, form, shotSeconds, focus, distance, yaw);

            CombineSideBySide(name + LevelOneSuffix, name + LevelFiveSuffix, name);
        }

        private IEnumerator CastAndCapture<T>(
            string fileName, SongSlot slot, SwarmForm form, float shotSeconds, Vector3 focus, float distance, float yaw)
            where T : SwarmSongEffect
        {
            yield return Play(KeysOf<T>());
            float castTime = Time.time;
            Assert.That(_root.Session.Forms.Slot(slot).Form, Is.EqualTo(form), typeof(T).Name + " did not cast");
            yield return WaitSince(castTime, shotSeconds);
            yield return CapturePosed(fileName, focus, distance, LevelShotPitch, yaw);
        }

        // Collects the first nests in the order they open, as a player would over the first rounds: the ones open at the
        // start, then those each cleared wave opens. NestField.OpenAfterWave is what the session calls when a wave is cleared.
        private IEnumerator CollectNests(int count)
        {
            NestConfig config = LoadNestConfig();
            NestField nests = _root.Session.Nests;
            int wave = 0;
            for (int index = 0; index < count; index++)
            {
                while (!nests[index].IsOpen && wave < config.OpenAfterWave.Count)
                {
                    wave++;
                    nests.OpenAfterWave(wave);
                }

                Assert.That(nests[index].IsOpen, Is.True, "nest " + index + " never opened");
                _core.PlaceAt(nests[index].Position + (Vector3.back * BesideNestMetres), true);
                yield return Wait(config.CollectSeconds + NestStayMarginSeconds);
                Assert.That(nests[index].IsCollected, Is.True, "nest " + index + " was not collected");
            }
        }

        // The nearest pair of an open nest and a closed one that stand further apart across the picture than along it, so
        // neither tree hides the other from the game camera, which looks north.
        private bool TryNestsSideBySide(out Nest open, out Nest closed)
        {
            NestField nests = _root.Session.Nests;
            open = null;
            closed = null;
            float best = float.MaxValue;
            for (int first = 0; first < nests.Count; first++)
            {
                for (int second = 0; second < nests.Count; second++)
                {
                    Nest lit = nests[first];
                    Nest dark = nests[second];
                    Vector3 offset = dark.Position - lit.Position;
                    bool isSideBySide = Mathf.Abs(offset.x) >= Mathf.Abs(offset.z);
                    float distance = GroundDistance(lit.Position, dark.Position);
                    if (lit.IsOpen && !dark.IsOpen && !dark.IsCollected && isSideBySide && distance < best)
                    {
                        best = distance;
                        open = lit;
                        closed = dark;
                    }
                }
            }

            return open != null;
        }

        private Nest SouthernmostOpenNest()
        {
            NestField nests = _root.Session.Nests;
            Nest southernmost = null;
            for (int index = 0; index < nests.Count; index++)
            {
                if (nests[index].IsOpen && (southernmost == null || nests[index].Position.z < southernmost.Position.z))
                {
                    southernmost = nests[index];
                }
            }

            return southernmost;
        }

        // Writes the two pictures, of the same height, side by side in one file, the first on the left.
        private void CombineSideBySide(string left, string right, string name)
        {
            var leftImage = new Texture2D(2, 2, TextureFormat.RGB24, false);
            var rightImage = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Texture2D both = null;
            try
            {
                Assert.That(leftImage.LoadImage(File.ReadAllBytes(Path.Combine(_directory, left + ".png"))), Is.True, left);
                Assert.That(rightImage.LoadImage(File.ReadAllBytes(Path.Combine(_directory, right + ".png"))), Is.True, right);
                Assert.That(rightImage.height, Is.EqualTo(leftImage.height), "the two pictures differ in height");
                both = new Texture2D(leftImage.width + rightImage.width, leftImage.height, TextureFormat.RGB24, false);
                both.SetPixels(0, 0, leftImage.width, leftImage.height, leftImage.GetPixels());
                both.SetPixels(leftImage.width, 0, rightImage.width, rightImage.height, rightImage.GetPixels());
                both.Apply();
                File.WriteAllBytes(Path.Combine(_directory, name + ".png"), both.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(leftImage);
                UnityEngine.Object.Destroy(rightImage);
                if (both != null)
                {
                    UnityEngine.Object.Destroy(both);
                }
            }
        }

        private static NestConfig LoadNestConfig()
        {
            var nests = AssetDatabase.LoadAssetAtPath<NestConfig>(NestConfigPath);
            Assert.That(nests, Is.Not.Null, "There is no NestConfig at " + NestConfigPath + ". Rebuild the scene.");
            return nests;
        }

        private static CoreMoveConfig LoadCoreMove()
        {
            var coreMove = AssetDatabase.LoadAssetAtPath<CoreMoveConfig>(CoreMoveConfigPath);
            Assert.That(coreMove, Is.Not.Null, "There is no CoreMoveConfig at " + CoreMoveConfigPath + ". Rebuild the scene.");
            return coreMove;
        }

        // Stops outside the hive's collect ring until the camera has caught up, then steps in beside the hive.
        private IEnumerator ApproachHive(Vector3 hive)
        {
            _core.PlaceAt(hive + (Vector3.back * OutsideRingMetres), true);
            yield return Wait(SettleCameraSeconds);
            _core.PlaceAt(hive + (Vector3.back * BesideHiveMetres), true);
            yield return Wait(FlowShotSeconds);
        }

        private Vector3 HiveAndCoreFocus(Vector3 hive) => ((hive + _core.Position) * 0.5f) + (Vector3.up * CloseFocusHeight);

        private IEnumerator Walk(Vector3 direction, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                float dt = Time.deltaTime;
                _core.Move(direction, 1f, false, dt);
                elapsed += dt;
                yield return null;
            }
        }

        // Camera.Render does not draw UI Toolkit, so the HUD panel is pointed at a render texture for a few frames,
        // through a runtime copy of its PanelSettings. The PanelSettings asset itself is never changed.
        private IEnumerator CaptureHud(string name)
        {
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

                _capturer.RequestOverlayRead(Path.Combine(_directory, name + ".png"));
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

            Assert.That(_capturer.LastWasFlat, Is.False, name + ".png is a single flat colour.");
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
            return _root.Session.Caster[SongIndex<T>()].Song.Keys;
        }

        private int SongIndex<T>() where T : SwarmSongEffect
        {
            HiveDefenseSession session = _root.Session;
            for (int song = 0; song < session.Caster.Count; song++)
            {
                if (session.Caster[song] is T)
                {
                    return song;
                }
            }

            Assert.Fail("The scene has no " + typeof(T).Name + ".");
            return -1;
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

        // The match starts in a farm phase with nobody on the map, so the wave is called at once.
        private IEnumerator WaitForBeekeepers(int wanted)
        {
            _root.CallWave();
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
