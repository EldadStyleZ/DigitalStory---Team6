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
        // Fills Fury at any rate up to 20 damage a point (11.5 since songs-v2 V, when 1,000 stopped filling it).
        private const float FuryDamage = 2000f;
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
        private const float NestComboReadSeconds = 0.6f;
        private const float MaxCooldownWaitSeconds = 20f;
        private const float NestComboPopSettleSeconds = 0.35f;
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

        // Snapshot 15, the five songs after the Meteor. The beekeepers of the first wave are stunned as they come, then put
        // where a picture needs them and stunned for its length, so they stand still and never hit the swarm.
        private const float StageStunSeconds = 60f;
        private const float StageSettleSeconds = 1.5f;
        private const float FaceNorthSeconds = 0.25f;
        private const float HydraRearShotSeconds = 0.45f;
        private const float HydraBiteShotSeconds = 0.7f;
        private const float HydraFocusHeight = 2f;
        private const float HydraCloseDistance = 13f;
        private const float HydraClosePitch = 22f;
        private const float HydraCloseYaw = 40f;
        private const float ScytheCutShotSeconds = 0.3f;
        private const float ScytheCloseDistance = 10f;
        private const float ScytheClosePitch = 50f;
        private const float LanceDrawShotSeconds = 0.35f;
        private const float LanceFireShotSeconds = 0.5f;
        private const float LanceHeight = 1.2f;
        private const float LanceCloseMargin = 5f;
        private const float LanceClosePitch = 22f;
        private const float NestArmedShotSeconds = 1.3f;
        private const float NestStepInMetres = 1.5f;
        private const float NestShotFocusHeight = 1f;
        private const float NestCloseDistance = 8f;
        private const float NestCloseYaw = 70f;
        private const float NestClosePitch = 30f;
        private const float MaxSetOffSeconds = 2f;
        private const float HoneycombBuiltSeconds = 2.4f;
        private const float HoneycombFocusHeight = 1.4f;
        private const float HoneycombCloseDistance = 8f;
        private const float HoneycombClosePitch = 25f;
        private const float HoneycombCloseYaw = 30f;
        private const float MaxHiveHitSeconds = 3f;
        private const float AllSlotsCloseDistance = 14f;
        private const float AllSlotsClosePitch = 35f;
        private const int KnownInHudTwelve = 7;

        // Snapshot 16, after the owner played (plan section O): the figure-eight round the standing core at 800 and 4,000
        // wasps, its winding in and out as the core stops and starts, several stings, a blow the Dome blocks on a hive, the
        // Tornado's throw and a hit's number.
        private const float EightWalkSeconds = 2f;
        private const float EightStandSeconds = 3f;
        private const float EightGrowSeconds = 4f;
        private const float EightTopDistance = 11f;
        private const float EightTopHeight = 1f;
        private const float StraightDown = 90f;
        private const int SequenceColumns = 3;
        private const string TopSuffix = "-top";
        private const int ThreeStingWasps = 1200;
        private const int ThreeStings = 3;
        private const float StingFlightShotSeconds = 0.22f;
        private const float StingRopesShotSeconds = 0.8f;
        private const float StingsCloseDistance = 13f;
        private const float StingsClosePitch = 40f;
        private const float DomeHiveCoreMetres = 2f;
        private const float DomeHiveCloseDistance = 9f;
        private const float DomeHiveClosePitch = 30f;
        private const float MaxBlockedSeconds = 4f;
        private const float ThrowEarlyShotSeconds = 0.2f;
        private const float ThrowLateShotSeconds = 0.45f;
        private const float ThrowCloseDistance = 15f;
        private const float ThrowClosePitch = 15f;
        private const float HitNumberShotSeconds = 0.05f;

        // Snapshot 17, the giant version 2 in the chase view (plan section I). Holes count from 0 as H J K L. Each move's
        // picture is taken this many seconds after the move starts: about when it lands.
        private const int RoarHole = 0;
        private const int PunchHole = 1;
        private const int KickHole = 2;
        private const int SlamHole = 3;
        private const int SwoopShots = 4;
        private const int SwoopColumns = 2;
        private const float SwoopTolerance = 0.001f;
        private const float GiantSettleSeconds = 0.6f;
        private const float GiantWalkSeconds = 1.2f;
        private const float GiantStopSeconds = 0.5f;
        private const float GiantBetweenMovesSeconds = 0.9f;
        private const float GiantPunchShotSeconds = 0.2f;
        private const float GiantKickShotSeconds = 0.3f;
        private const float GiantRoarShotSeconds = 0.3f;
        private const float GiantSpinShotSeconds = 0.25f;
        private const float GiantSlamShotSeconds = 0.3f;
        private const float GiantLeapAirShotSeconds = 0.3f;
        private const float GiantLeapLandShotSeconds = 0.68f;
        private const float GiantOrbitYawDegrees = 60f;
        private const float GiantOrbitPitchDegrees = 8f;
        private const float GiantOrbitSeconds = 0.6f;
        private const float MaxMoveWaitSeconds = 2f;

        // When the fist pictures are taken: near the end of the thrust, halfway through the flight, a third into the regrow, and
        // a moment after the arm is whole.
        private const float FistThrustShotShare = 0.8f;
        private const float FistFlightShotShare = 0.5f;
        private const float FistMissingShotShare = 0.3f;
        private const float FistRegrownShotSeconds = 0.3f;

        // The giant forms 10 m south of the middle, facing north, with three stunned sticks 8 to 12 m ahead of it.
        private static readonly Vector3 GiantCore = new Vector3(0f, 0f, -10f);

        private static readonly Vector3[] GiantBeekeepers =
        {
            new Vector3(-2.5f, 0f, -1f), new Vector3(1.5f, 0f, 1.5f), new Vector3(4.5f, 0f, -2.5f),
        };

        // When the pictures of the stop and of the start are taken, in seconds after the core stops or starts.
        private static readonly float[] WindShotSeconds = { 0.2f, 0.5f, 0.8f, 1.1f, 1.4f, 2f };
        private static readonly float[] UnwindShotSeconds = { 0.1f, 0.25f, 0.4f, 0.6f, 0.8f, 1.2f };

        // Three sticks in front of a core 6 m south of the middle, each in the Sting's reach and well apart.
        private static readonly Vector3 StingsCore = new Vector3(0f, 0f, -6f);

        private static readonly Vector3[] StingsBeekeepers =
        {
            new Vector3(-4.5f, 0f, -1.5f), new Vector3(0.5f, 0f, 0f), new Vector3(4.5f, 0f, -2f),
        };

        // Four sticks round a core, inside the Tornado's 5 m reach.
        private static readonly Vector3 ThrowCore = new Vector3(0f, 0f, -4f);

        private static readonly Vector3[] ThrowBeekeepers =
        {
            new Vector3(-3f, 0f, -4f), new Vector3(3f, 0f, -4f), new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -7f),
        };

        private static readonly Vector3 HitNumberBeekeeper = new Vector3(0f, 0f, -1f);

        // Where the core stands for each picture, and where its beekeepers stand. The game camera looks north (+z).
        private static readonly Vector3 HydraCore = new Vector3(0f, 0f, -4f);

        // Three at about 6.5 m in front, the nearest three; a fourth at 7.6 m, bitten only by a fourth head (level 4 on).
        private static readonly Vector3[] HydraBeekeepers =
        {
            new Vector3(-4f, 0f, 1f), new Vector3(0.5f, 0f, 2.5f), new Vector3(4.5f, 0f, 0.5f), new Vector3(-7f, 0f, -1f),
        };

        private static readonly Vector3 ScytheCore = new Vector3(0f, 0f, -4f);

        // Three in front within the blade's 4.5 m, the nearest of them the threat it faces, and one behind it.
        private static readonly Vector3[] ScytheBeekeepers =
        {
            new Vector3(-2.5f, 0f, -1.5f), new Vector3(0f, 0f, -0.6f), new Vector3(2.4f, 0f, -2f), new Vector3(0f, 0f, -8.2f),
        };

        private static readonly Vector3 LanceCore = new Vector3(-5f, 0f, -6f);

        // A file walking in from the north-east, 4, 7.5 and 11 m out on one line; the fourth stands well off it.
        private static readonly Vector3[] LanceBeekeepers =
        {
            new Vector3(-1.8f, 0f, -3.6f), new Vector3(1f, 0f, -1.5f), new Vector3(3.8f, 0f, 0.6f), new Vector3(-9f, 0f, 2f),
        };

        private static readonly Vector3 NestCore = new Vector3(0f, 0f, -6f);

        // Parked far from the core and from where the nest lands, until it walks into the trap.
        private static readonly Vector3 NestParked = new Vector3(-16f, 0f, -14f);

        // East of the east hive, in a stick's reach of it and beyond its notice of the core.
        private static readonly Vector3 HiveStickOffset = new Vector3(1.75f, 0f, 0.3f);
        private static readonly Vector3 CoreOffHive = new Vector3(0f, 0f, -7f);

        // Around a core 7 m south of the east hive, as offsets from that hive: within the Hydra's reach, none at the hive.
        private static readonly Vector3[] AllSlotsBeekeeperOffsets =
        {
            new Vector3(-4.5f, 0f, -4.5f), new Vector3(4.5f, 0f, -4.5f), new Vector3(2.5f, 0f, -12f),
        };

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

        // Two songs are found besides Sting, so the songbook shows three known songs and nine empty slots, the "New song"
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

        // Plan section O: once the core stops, the river winds back into the figure-eight round it.
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
        // the seconds skipped, and the wasp price on each known song's chip. The Dome and the Tornado were found in the farm
        // phase, so their chips show a price; the Sting has never been cast, and its first cast is free, so its chip says so.
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

        // Plan sections H and U: while the core plays a nest's combo, the ring on the ground fills clockwise from the top,
        // here about half way, and motes stream from the nest down to it. The core waits outside the ring first, so the
        // camera has caught up.
        [UnityTest]
        public IEnumerator Snapshot14_Collecting()
        {
            Nest nest = _root.Session.Nests[0];
            _core.PlaceAt(nest.Position + (Vector3.back * OutsideNestRingMetres), true);
            yield return Wait(SettleCameraSeconds);
            Assert.That(nest.Progress01, Is.EqualTo(0f), "the nest was being collected from outside its ring");

            _core.PlaceAt(nest.Position + (Vector3.back * BesideNestMetres), true);
            yield return Wait(NestComboReadSeconds);
            yield return Play(HalfOf(nest.Combo));
            yield return Wait(NestComboReadSeconds);
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

        // ---- Snapshot 15: Hydra, Scythe, Lance, Nest and Honeycomb (plan sections A and B) ----
        // Each beat is taken from the game camera and from a close camera (-close), on the frame the rules reach it.

        // The Hydra at level 1: three necks rear back behind the core (rear, 0.45 s), then the heads snap onto the three
        // beekeepers nearest the core and bite on the rule's frame (bite, 0.7 s). The fourth beekeeper is spared.
        [UnityTest]
        public IEnumerator Snapshot15_Hydra()
        {
            return HydraShots(string.Empty, 0);
        }

        // The same at level 5, after four nests: a fourth head, taller necks and wider splashes.
        [UnityTest]
        public IEnumerator Snapshot15_HydraLevel5()
        {
            return HydraShots(LevelFiveSuffix, LevelFiveNests);
        }

        // The Scythe's sweep on the cut's frame (0.3 s): the blade's edge on the aim, the smear behind it.
        [UnityTest]
        public IEnumerator Snapshot15_Scythe()
        {
            yield return Stage(ScytheCore, ScytheBeekeepers);
            yield return Play(KeysOf<ScytheSong>());
            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Scythe));

            yield return WaitForSlot(SongSlot.Strike, ScytheCutShotSeconds);
            yield return CaptureBoth(
                "snap-15-scythe-sweep", ScytheCore + (Vector3.up * CloseFocusHeight), ScytheCloseDistance, ScytheClosePitch, 0f);
        }

        // The Lance at level 1: drawn back behind the core (draw, 0.35 s), then the streak at full length on the hit's frame
        // (fire, 0.5 s), down a file of three beekeepers.
        [UnityTest]
        public IEnumerator Snapshot15_Lance()
        {
            return LanceShots(string.Empty, 0);
        }

        // The same at level 5: a longer, thicker spear.
        [UnityTest]
        public IEnumerator Snapshot15_LanceLevel5()
        {
            return LanceShots(LevelFiveSuffix, LevelFiveNests);
        }

        // The Nest armed on the ground ahead of the core with nobody near (armed), then a beekeeper walks into its ring and
        // it bursts on the rule's frame (burst).
        [UnityTest]
        public IEnumerator Snapshot15_Nest()
        {
            yield return Stage(NestCore, NestParked);
            HiveDefenseSession session = _root.Session;
            yield return Play(KeysOf<NestSong>());
            Assert.That(session.Forms.Slot(SongSlot.Post).Form, Is.EqualTo(SwarmForm.Nest));

            yield return WaitForSlot(SongSlot.Post, NestArmedShotSeconds);
            Vector4 nest = session.Forms.Slot(SongSlot.Post).ParamA;
            var spot = new Vector3(nest.x, nest.y, nest.z);
            Vector3 focus = spot + (Vector3.up * NestShotFocusHeight);
            Assert.That(nest.w, Is.LessThan(0f), "the nest went off before anyone came");
            yield return CaptureBoth("snap-15-nest-armed", focus, NestCloseDistance, NestClosePitch, NestCloseYaw);

            session.ActiveBeekeeper(0).Spawn(spot + (Vector3.right * NestStepInMetres));
            float waited = 0f;
            while (session.Forms.Slot(SongSlot.Post).ParamA.w < 0f && waited < MaxSetOffSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            float setOff = session.Forms.Slot(SongSlot.Post).ParamA.w;
            Assert.That(setOff, Is.GreaterThanOrEqualTo(0f), "the beekeeper in the ring did not set the nest off");
            NestSong song = (NestSong)session.Caster[SongIndex<NestSong>()];
            yield return WaitForSlot(SongSlot.Post, setOff + song.FuseSeconds);
            yield return CaptureBoth("snap-15-nest-burst", focus, NestCloseDistance, NestClosePitch, NestCloseYaw);
        }

        // The Honeycomb built on the east hive, and a stick that strikes it: its cell flashes and a ripple runs from that side.
        [UnityTest]
        public IEnumerator Snapshot15_Honeycomb()
        {
            Vector3 hive = EastHivePosition();
            yield return Stage(hive + CoreOffHive, NestParked);
            HiveDefenseSession session = _root.Session;
            yield return Play(KeysOf<HoneycombSong>());
            Assert.That(session.Forms.Slot(SongSlot.Post).Form, Is.EqualTo(SwarmForm.Honeycomb));
            yield return WaitForSlot(SongSlot.Post, HoneycombBuiltSeconds);

            session.ActiveBeekeeper(0).Spawn(hive + HiveStickOffset);
            float waited = 0f;
            while (session.Forms.Slot(SongSlot.Post).ParamB.y < 0f && waited < MaxHiveHitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.Forms.Slot(SongSlot.Post).ParamB.y, Is.GreaterThanOrEqualTo(0f), "the stick never hit the shell");
            yield return CaptureBoth(
                "snap-15-honeycomb", hive + (Vector3.up * HoneycombFocusHeight), HoneycombCloseDistance, HoneycombClosePitch, HoneycombCloseYaw);
        }

        // A Honeycomb on the east hive (Post), a Dome on the core (Guard) and a Hydra biting (Strike), all at once.
        [UnityTest]
        public IEnumerator Snapshot15_AllThreeSlots()
        {
            Vector3 hive = EastHivePosition();
            var spots = new Vector3[AllSlotsBeekeeperOffsets.Length];
            for (int index = 0; index < spots.Length; index++)
            {
                spots[index] = hive + AllSlotsBeekeeperOffsets[index];
            }

            yield return Stage(hive + CoreOffHive, spots);
            SwarmFormMachine forms = _root.Session.Forms;
            yield return Play(KeysOf<HoneycombSong>());
            yield return WaitForSlot(SongSlot.Post, HoneycombBuiltSeconds);
            yield return Play(KeysOf<DomeSong>());
            yield return Wait(DomeWovenSeconds);
            yield return Play(KeysOf<HydraSong>());
            yield return WaitForSlot(SongSlot.Strike, HydraBiteShotSeconds);

            Assert.That(forms.Slot(SongSlot.Post).Form, Is.EqualTo(SwarmForm.Honeycomb));
            Assert.That(forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            Assert.That(forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Hydra));
            Assert.That(forms.Slot(SongSlot.Post).ReleasedSeconds < 0f && forms.Slot(SongSlot.Guard).ReleasedSeconds < 0f, Is.True);
            Vector3 focus = ((hive + _core.Position) * 0.5f) + (Vector3.up * CloseFocusHeight);
            yield return CaptureBoth("snap-15-all-three-slots", focus, AllSlotsCloseDistance, AllSlotsClosePitch, 0f);
        }

        // The song table with all twelve slots: the Sting and six songs found, and the Meteor's first three notes played.
        [UnityTest]
        public IEnumerator Snapshot15_HudTwelve()
        {
            HiveDefenseSession session = _root.Session;
            Assert.That(session.Caster.Count, Is.EqualTo(12));
            yield return Wait(SongSeconds);
            yield return PlayAndPause(KeysOf<DomeSong>());
            yield return PlayAndPause(KeysOf<HydraSong>());
            yield return PlayAndPause(KeysOf<LanceSong>());
            yield return PlayAndPause(KeysOf<NestSong>());
            yield return PlayAndPause(KeysOf<HoneycombSong>());
            yield return PlayAndPause(KeysOf<HiveCallSong>());
            IReadOnlyList<int> meteor = KeysOf<MeteorSong>();
            var start = new int[meteor.Count - HalfSongNotesMissing];
            for (int index = 0; index < start.Length; index++)
            {
                start[index] = meteor[index];
            }

            yield return Play(start);
            yield return Wait(HintPauseSeconds);
            Assert.That(session.Discovery.KnownCount, Is.EqualTo(KnownInHudTwelve));
            yield return CaptureHud("snap-15-hud-twelve");
        }

        // ---- Snapshot 16: after the owner played (plan section O) ----

        // The young swarm of 800 stands round its core after a walk: the figure-eight from the game camera, from close by and
        // from straight above.
        [UnityTest]
        public IEnumerator Snapshot16_EightYoung()
        {
            return EightShots(false);
        }

        // The same with the full swarm of 4,000 at level 1: the same eight, thicker.
        [UnityTest]
        public IEnumerator Snapshot16_EightFull()
        {
            return EightShots(true);
        }

        // The core stops after a walk, and the river winds into the eight: six moments, from the game camera and from above.
        [UnityTest]
        public IEnumerator Snapshot16_EightWind()
        {
            yield return Walk(Vector3.forward, EightWalkSeconds);
            var names = new string[WindShotSeconds.Length];
            var tops = new string[WindShotSeconds.Length];
            float stopped = Time.time;
            for (int shot = 0; shot < WindShotSeconds.Length; shot++)
            {
                yield return WaitSince(stopped, WindShotSeconds[shot]);
                names[shot] = "snap-16-eight-wind-" + shot;
                tops[shot] = names[shot] + TopSuffix;
                yield return Capture(names[shot]);
                yield return CapturePosed(tops[shot], TopFocus(), EightTopDistance, StraightDown, 0f);
            }

            CombineGrid(names, SequenceColumns, "snap-16-eight-wind");
            CombineGrid(tops, SequenceColumns, "snap-16-eight-wind" + TopSuffix);
        }

        // The core walks off sideways from the standing eight, which unwinds into the river: six moments, as above. The core
        // keeps walking while each picture is taken.
        [UnityTest]
        public IEnumerator Snapshot16_EightUnwind()
        {
            yield return Walk(Vector3.forward, EightWalkSeconds);
            yield return Wait(EightStandSeconds);
            var names = new string[UnwindShotSeconds.Length];
            var tops = new string[UnwindShotSeconds.Length];
            float started = Time.time;
            for (int shot = 0; shot < UnwindShotSeconds.Length; shot++)
            {
                while (Time.time - started < UnwindShotSeconds[shot])
                {
                    _core.Move(Vector3.right, 1f, false, Time.deltaTime);
                    yield return null;
                }

                names[shot] = "snap-16-eight-unwind-" + shot;
                tops[shot] = names[shot] + TopSuffix;
                yield return CaptureWalking(names[shot], false, Vector3.right);
                yield return CaptureWalking(tops[shot], true, Vector3.right);
            }

            CombineGrid(names, SequenceColumns, "snap-16-eight-unwind");
            CombineGrid(tops, SequenceColumns, "snap-16-eight-unwind" + TopSuffix);
        }

        // A Sting with 1,200 wasps sends three stings, each a rope of its own to its own beekeeper: in flight, then wrapped.
        [UnityTest]
        public IEnumerator Snapshot16_Stings()
        {
            SwarmBody swarm = _root.Session.Swarm;
            swarm.GrowCapacity(ThreeStingWasps - swarm.Capacity);
            swarm.Gain(ThreeStingWasps - swarm.Count);
            Assert.That(swarm.Count, Is.EqualTo(ThreeStingWasps));
            yield return Stage(StingsCore, StingsBeekeepers);
            yield return Play(KeysOf<StingSong>());
            SwarmFormMachine forms = _root.Session.Forms;
            Assert.That(forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Sting));
            Assert.That(forms.StingCount, Is.EqualTo(ThreeStings));

            Vector3 middle = StingsCore;
            for (int index = 0; index < StingsBeekeepers.Length; index++)
            {
                middle += StingsBeekeepers[index];
            }

            Vector3 focus = (middle / (StingsBeekeepers.Length + 1)) + (Vector3.up * CloseFocusHeight);
            yield return WaitForSlot(SongSlot.Strike, StingFlightShotSeconds);
            yield return CaptureBoth("snap-16-stings-flight", focus, StingsCloseDistance, StingsClosePitch, 0f);
            yield return WaitForSlot(SongSlot.Strike, StingRopesShotSeconds);
            yield return CaptureBoth("snap-16-stings", focus, StingsCloseDistance, StingsClosePitch, 0f);
        }

        // The core beside the east hive raises the Dome, and a stick strikes the hive inside it: the blocked blow flashes blue
        // on the frame it lands, with its "blocked" number and the HUD's line about the Dome.
        [UnityTest]
        public IEnumerator Snapshot16_DomeOnHive()
        {
            Vector3 hive = EastHivePosition();
            yield return Stage(hive + (Vector3.back * DomeHiveCoreMetres), NestParked);
            HiveDefenseSession session = _root.Session;
            yield return Play(KeysOf<DomeSong>());
            Assert.That(session.Forms.Slot(SongSlot.Guard).Form, Is.EqualTo(SwarmForm.Dome));
            yield return Wait(DomeWovenSeconds);

            bool isBlocked = false;
            Action<SongHit> onLanded = hit => isBlocked |= hit.IsBlocked;
            session.SongLanded += onLanded;
            try
            {
                session.ActiveBeekeeper(0).Spawn(hive + HiveStickOffset);
                float waited = 0f;
                while (!isBlocked && waited < MaxBlockedSeconds)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }
            finally
            {
                session.SongLanded -= onLanded;
            }

            Assert.That(isBlocked, Is.True, "the stick's blow on the hive inside the Dome was not blocked");
            Vector3 focus = ((hive + _core.Position) * 0.5f) + (Vector3.up * CloseFocusHeight);
            yield return CaptureBoth("snap-16-dome-on-hive", focus, DomeHiveCloseDistance, DomeHiveClosePitch, 0f);
            yield return CaptureHud("snap-16-dome-on-hive-hud");
        }

        // The Tornado's hit throws the four beekeepers round it outward and up: just after the hit, and at the top of the arc.
        [UnityTest]
        public IEnumerator Snapshot16_TornadoThrow()
        {
            yield return Stage(ThrowCore, ThrowBeekeepers);
            yield return Play(KeysOf<TornadoSong>());
            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Tornado));
            TornadoSong tornado = (TornadoSong)_root.Session.Caster[SongIndex<TornadoSong>()];
            Vector3 focus = ThrowCore + (Vector3.up * TornadoFocusHeight);
            yield return WaitForSlot(SongSlot.Strike, tornado.HitSeconds + ThrowEarlyShotSeconds);
            yield return CaptureBoth("snap-16-tornado-throw", focus, ThrowCloseDistance, ThrowClosePitch, 0f);
            yield return WaitForSlot(SongSlot.Strike, tornado.HitSeconds + ThrowLateShotSeconds);
            yield return CaptureBoth("snap-16-tornado-throw-late", focus, ThrowCloseDistance, ThrowClosePitch, 0f);
        }

        // A Tornado hit on one beekeeper, with the HUD: the number rises from it.
        [UnityTest]
        public IEnumerator Snapshot16_HitNumber()
        {
            yield return Stage(ThrowCore, ThrowCore + HitNumberBeekeeper);
            yield return Play(KeysOf<TornadoSong>());
            TornadoSong tornado = (TornadoSong)_root.Session.Caster[SongIndex<TornadoSong>()];
            yield return WaitForSlot(SongSlot.Strike, tornado.HitSeconds + HitNumberShotSeconds);
            var numbers = UnityEngine.Object.FindAnyObjectByType<DamageNumbersView>();
            Assert.That(numbers, Is.Not.Null, "the HUD made no DamageNumbersView");
            Assert.That(numbers.ActiveCount, Is.GreaterThan(0), "the Tornado's hit showed no number");
            yield return CaptureHud("snap-16-hit-number");
        }

        // The swarm has collapsed: nothing of it shows, and the HUD says it is returning and counts the seconds.
        [UnityTest]
        public IEnumerator Snapshot16_ReturningHud()
        {
            yield return Wait(SongSeconds);
            SwarmBody swarm = _root.Session.Swarm;
            swarm.Lose(swarm.Count);
            Assert.That(swarm.IsCollapsed, Is.True);
            yield return Wait(HintPauseSeconds);
            Assert.That(swarm.SecondsToRespawn, Is.GreaterThan(0f), "the swarm came back before the picture");
            yield return CaptureHud("snap-16-returning-hud");
        }

        // Snapshot 17, the giant version 2 (plan section I) from the real game camera: the swoop from the top view into the
        // chase view, then the giant standing, walking, and each of its moves, and the view turned by the mouse. Every picture
        // after the swoop has the HUD drawn over it: its ring, its cooldown marks and, at first, the line of its keys. Three
        // stunned sticks stand ahead of the giant, so the picture shows what it fights.
        [UnityTest]
        public IEnumerator Snapshot17_GiantChase()
        {
            yield return Stage(GiantCore, GiantBeekeepers);
            FollowCamera follow = UnityEngine.Object.FindAnyObjectByType<FollowCamera>();
            Assert.That(follow, Is.Not.Null, "The scene has no FollowCamera.");
            HiveDefenseSession session = _root.Session;
            var started = new List<ColossusAttack>();
            float startedAt = 0f;
            session.ColossusAttackStarted += attack =>
            {
                started.Add(attack);
                startedAt = Time.time;
            };

            session.Fury.AddDamage(FuryDamage);
            yield return Play(KeysOf<ColossusSong>());
            float waited = 0f;
            while (!session.IsColossusActive && waited < MaxColossusWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.IsColossusActive, Is.True, "The Colossus keys at full Fury did not form the Colossus.");
            var swoop = new string[SwoopShots];
            for (int shot = 0; shot < SwoopShots; shot++)
            {
                float wanted = shot / (float)(SwoopShots - 1);
                while (follow.ChaseBlend < wanted - SwoopTolerance)
                {
                    yield return null;
                }

                swoop[shot] = "snap-17-swoop-" + shot;
                yield return Capture(swoop[shot]);
            }

            CombineGrid(swoop, SwoopColumns, "snap-17-swoop");
            Assert.That(follow.IsChasing, Is.True, "the camera did not chase the giant");

            RenderTexture overlay = BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy);
            try
            {
                yield return Wait(GiantSettleSeconds);
                yield return ReadHud("snap-17-giant-chase");

                _root.HoldMove(Vector2.up);
                yield return Wait(GiantWalkSeconds);
                yield return ReadHud("snap-17-giant-walk");
                _root.HoldMove(Vector2.zero);
                yield return Wait(GiantStopSeconds);

                yield return Move(started, () => startedAt, PunchHole, ColossusAttack.Punch, GiantPunchShotSeconds, "snap-17-giant-punch");
                yield return Wait(GiantBetweenMovesSeconds);
                yield return Move(started, () => startedAt, KickHole, ColossusAttack.Kick, GiantKickShotSeconds, "snap-17-giant-kick");
                yield return Wait(GiantBetweenMovesSeconds);
                yield return Move(started, () => startedAt, RoarHole, ColossusAttack.Roar, GiantRoarShotSeconds, "snap-17-giant-roar");
                yield return Wait(GiantBetweenMovesSeconds);

                // K K: the second kick waits for the first to land, then the spin starts.
                _root.PressHole(KickHole);
                yield return null;
                _root.PressHole(KickHole);
                yield return WaitForMove(started, ColossusAttack.SpinKick);
                yield return WaitSince(startedAt, GiantSpinShotSeconds);
                yield return ReadHud("snap-17-giant-spin");
                yield return Wait(GiantBetweenMovesSeconds);

                yield return Move(started, () => startedAt, SlamHole, ColossusAttack.Slam, GiantSlamShotSeconds, "snap-17-giant-slam");
                while (session.SlamCooldown01 > 0f)
                {
                    yield return null;
                }

                // L L: the leap takes off as the slam lands, to where the camera looks on the ground.
                _root.PressHole(SlamHole);
                yield return null;
                _root.PressHole(SlamHole);
                yield return WaitForMove(started, ColossusAttack.LeapSlam);
                Assert.That(session.IsColossusLeaping, Is.True, "the leap did not take off");
                float leapAt = startedAt;
                yield return WaitSince(leapAt, GiantLeapAirShotSeconds);
                yield return ReadHud("snap-17-giant-leap-air");
                yield return WaitSince(leapAt, GiantLeapLandShotSeconds);
                Assert.That(session.IsColossusLeaping, Is.False, "the giant had not landed for the picture");
                yield return ReadHud("snap-17-giant-leap-land");
                yield return Wait(GiantBetweenMovesSeconds);

                follow.Orbit(GiantOrbitYawDegrees, GiantOrbitPitchDegrees);
                yield return Wait(GiantOrbitSeconds);
                Assert.That(session.IsColossusActive, Is.True, "the giant ended before the last picture");
                yield return ReadHud("snap-17-giant-orbit");
            }
            finally
            {
                _root.HoldMove(Vector2.zero);
                EndHudOverlay(overlay, document, original, copy);
            }
        }

        // Songs-v2 R: the giant's right hand launch, with the HUD over it: the arm thrusting, the fist in flight to the stick
        // auto-aim picks, the arm missing as the wasps stream back, and the arm whole again.
        [UnityTest]
        public IEnumerator Snapshot20_Fist()
        {
            yield return Stage(GiantCore, GiantBeekeepers);
            FollowCamera follow = UnityEngine.Object.FindAnyObjectByType<FollowCamera>();
            HiveDefenseSession session = _root.Session;
            session.Fury.AddDamage(FuryDamage);
            yield return Play(KeysOf<ColossusSong>());
            float waited = 0f;
            while ((!session.IsColossusActive || follow.ChaseBlend < 1f - SwoopTolerance) && waited < MaxColossusWaitSeconds + SettleCameraSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.IsColossusActive, Is.True, "The Colossus keys at full Fury did not form the Colossus.");
            RenderTexture overlay = BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy);
            try
            {
                yield return Wait(GiantSettleSeconds);
                ColossusConfig giant = LoadColossus();
                _root.PressHand(ColossusHand.Right);
                yield return null;
                Assert.That(session.FistPhaseOf(ColossusHand.Right), Is.EqualTo(FistPhase.Thrust), "the right hand did not thrust");
                yield return Wait(giant.FistThrustSeconds * FistThrustShotShare);
                yield return ReadHud("snap-20-fist-thrust");

                yield return WaitForFist(session, FistPhase.Flight);
                yield return Wait(session.FistFlightSeconds(ColossusHand.Right) * FistFlightShotShare);
                yield return ReadHud("snap-20-fist-flight");

                yield return WaitForFist(session, FistPhase.Regrow);
                yield return Wait(giant.FistRegrowSeconds * FistMissingShotShare);
                yield return ReadHud("snap-20-fist-missing");

                yield return WaitForFist(session, FistPhase.Ready);
                yield return Wait(FistRegrownShotSeconds);
                Assert.That(session.IsColossusActive, Is.True, "the giant ended before the last picture");
                yield return ReadHud("snap-20-fist-regrown");
            }
            finally
            {
                EndHudOverlay(overlay, document, original, copy);
            }
        }

        // Plan section U, from the game camera with the HUD over it: a nest's combo as the core steps into the ring, empty;
        // then with its first half played; then the same named with the controller's diamonds, as after the pad was used.
        [UnityTest]
        public IEnumerator Snapshot22_NestCombo()
        {
            Nest nest = _root.Session.Nests[0];
            var input = UnityEngine.Object.FindAnyObjectByType<HiveDefenseInput>();
            var hud = UnityEngine.Object.FindAnyObjectByType<HiveDefenseHudView>();
            Assert.That(input, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            _core.PlaceAt(nest.Position + (Vector3.back * OutsideNestRingMetres), true);
            yield return Wait(SettleCameraSeconds);

            RenderTexture overlay = BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy);
            try
            {
                _core.PlaceAt(nest.Position + (Vector3.back * BesideNestMetres), true);
                yield return Wait(NestComboReadSeconds);
                Assert.That(hud.NestCombo.IsShown, Is.True, "the combo is not shown in the ring");
                Assert.That(hud.NestCombo.FilledCount, Is.EqualTo(0));
                yield return ReadHud("snap-22-nestcombo-empty");

                IReadOnlyList<int> half = HalfOf(nest.Combo);
                yield return Play(half);
                yield return Wait(NestComboPopSettleSeconds);
                Assert.That(hud.NestCombo.FilledCount, Is.EqualTo(half.Count));
                yield return ReadHud("snap-22-nestcombo-half");

                input.RecordUse(false, true);
                yield return Wait(NestComboPopSettleSeconds);
                Assert.That(hud.ShownDevice, Is.EqualTo(ControlDevice.Gamepad));
                yield return ReadHud("snap-22-nestcombo-pad");
            }
            finally
            {
                input.RecordUse(true, false);
                EndHudOverlay(overlay, document, original, copy);
            }
        }

        // The first half of a combo, rounded down: two notes of four or five.
        private static IReadOnlyList<int> HalfOf(IReadOnlyList<int> combo)
        {
            var half = new List<int>();
            for (int index = 0; index < combo.Count / 2; index++)
            {
                half.Add(combo[index]);
            }

            return half;
        }

        private static IEnumerator WaitForFist(HiveDefenseSession session, FistPhase phase)
        {
            float waited = 0f;
            while (session.FistPhaseOf(ColossusHand.Right) != phase && waited < MaxMoveWaitSeconds * 2f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.FistPhaseOf(ColossusHand.Right), Is.EqualTo(phase), "the right fist never reached " + phase);
        }

        private static ColossusConfig LoadColossus()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<ColossusConfig>("Assets/_Project/GameData/F010/ColossusConfig.asset");
            Assert.That(config, Is.Not.Null, "No ColossusConfig asset.");
            return config;
        }

        // Presses one of the giant's keys, waits for its move to start and then for the moment of the picture.
        private IEnumerator Move(List<ColossusAttack> started, Func<float> startedAt, int hole, ColossusAttack move, float shotSeconds, string name)
        {
            _root.PressHole(hole);
            yield return WaitForMove(started, move);
            yield return WaitSince(startedAt(), shotSeconds);
            yield return ReadHud(name);
        }

        private IEnumerator WaitForMove(List<ColossusAttack> started, ColossusAttack move)
        {
            float waited = 0f;
            while (!started.Contains(move) && waited < MaxMoveWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(started.Contains(move), Is.True, move + " did not start");
            started.Clear();
        }

        // Points the HUD panel at a render texture the capturer fills with the game camera every frame, so each later read is
        // the game's own picture with the HUD over it. EndHudOverlay puts the panel back.
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

        private IEnumerator ReadHud(string name)
        {
            _capturer.RequestOverlayRead(Path.Combine(_directory, name + ".png"));
            yield return WaitForCapture(name);
        }

        private IEnumerator EightShots(bool isFull)
        {
            SwarmBody swarm = _root.Session.Swarm;
            if (isFull)
            {
                swarm.GrowCapacity(swarm.Max - swarm.Capacity);
                swarm.Gain(swarm.Max - swarm.Count);
                yield return Wait(EightGrowSeconds);
            }

            string suffix = "-" + swarm.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            yield return Walk(Vector3.forward, EightWalkSeconds);
            yield return Wait(EightStandSeconds);
            Assert.That(UnityEngine.Object.FindAnyObjectByType<SwarmGpuView>().EightShare, Is.EqualTo(1f), "the swarm is not wound into the eight");
            yield return Capture("snap-16-eight-game" + suffix);
            yield return CapturePosed("snap-16-eight-close" + suffix, CoreFocus(), CloseDistance, ClosePitch, _camera.transform.eulerAngles.y);
            yield return CapturePosed("snap-16-eight-top" + suffix, TopFocus(), EightTopDistance, StraightDown, 0f);
        }

        private Vector3 TopFocus() => _core.Position + (Vector3.up * EightTopHeight);

        // A picture taken while the core keeps walking, from the game camera or from straight above the core.
        private IEnumerator CaptureWalking(string name, bool isTop, Vector3 direction)
        {
            string path = Path.Combine(_directory, name + ".png");
            if (isTop)
            {
                Quaternion down = Quaternion.Euler(StraightDown, 0f, 0f);
                _capturer.RequestCapture(path, TopFocus() - (down * Vector3.forward * EightTopDistance), down);
            }
            else
            {
                _capturer.RequestCapture(path);
            }

            while (_capturer.IsBusy)
            {
                _core.Move(direction, 1f, false, Time.deltaTime);
                yield return null;
            }

            yield return WaitForCapture(name);
        }

        // Writes pictures of the same size in rows of this many columns, in one file, the first at the top left.
        private void CombineGrid(string[] names, int columns, string name)
        {
            var images = new Texture2D[names.Length];
            Texture2D grid = null;
            try
            {
                for (int index = 0; index < names.Length; index++)
                {
                    images[index] = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    Assert.That(images[index].LoadImage(File.ReadAllBytes(Path.Combine(_directory, names[index] + ".png"))), Is.True, names[index]);
                }

                int width = images[0].width;
                int height = images[0].height;
                int rows = (names.Length + columns - 1) / columns;
                grid = new Texture2D(width * columns, height * rows, TextureFormat.RGB24, false);
                for (int index = 0; index < images.Length; index++)
                {
                    Assert.That(images[index].width == width && images[index].height == height, Is.True, "the pictures differ in size");
                    int column = index % columns;
                    int row = rows - 1 - (index / columns);
                    grid.SetPixels(column * width, row * height, width, height, images[index].GetPixels());
                }

                grid.Apply();
                File.WriteAllBytes(Path.Combine(_directory, name + ".png"), grid.EncodeToPNG());
            }
            finally
            {
                for (int index = 0; index < images.Length; index++)
                {
                    if (images[index] != null)
                    {
                        UnityEngine.Object.Destroy(images[index]);
                    }
                }

                if (grid != null)
                {
                    UnityEngine.Object.Destroy(grid);
                }
            }
        }

        private IEnumerator HydraShots(string suffix, int nests)
        {
            yield return GrowTo(nests);
            yield return Stage(HydraCore, HydraBeekeepers);
            yield return Play(KeysOf<HydraSong>());
            Assert.That(_root.Session.Forms.Slot(SongSlot.Strike).Form, Is.EqualTo(SwarmForm.Hydra));

            Vector3 middle = HydraCore;
            for (int index = 0; index < HydraBeekeepers.Length; index++)
            {
                middle += HydraBeekeepers[index];
            }

            Vector3 focus = (middle / (HydraBeekeepers.Length + 1)) + (Vector3.up * HydraFocusHeight);
            yield return WaitForSlot(SongSlot.Strike, HydraRearShotSeconds);
            yield return CaptureBoth("snap-15-hydra-rear" + suffix, focus, HydraCloseDistance, HydraClosePitch, HydraCloseYaw);
            yield return WaitForSlot(SongSlot.Strike, HydraBiteShotSeconds);
            yield return CaptureBoth("snap-15-hydra-bite" + suffix, focus, HydraCloseDistance, HydraClosePitch, HydraCloseYaw);
        }

        // The close camera looks across the line from its side, far enough back to hold all of it.
        private IEnumerator LanceShots(string suffix, int nests)
        {
            yield return GrowTo(nests);
            yield return Stage(LanceCore, LanceBeekeepers);
            yield return Play(KeysOf<LanceSong>());
            SlotState strike = _root.Session.Forms.Slot(SongSlot.Strike);
            Assert.That(strike.Form, Is.EqualTo(SwarmForm.Lance));

            yield return WaitForSlot(SongSlot.Strike, LanceDrawShotSeconds);
            strike = _root.Session.Forms.Slot(SongSlot.Strike);
            var origin = new Vector3(strike.ParamA.x, LanceHeight, strike.ParamA.y);
            var direction = new Vector3(strike.ParamA.z, 0f, strike.ParamA.w);
            float length = strike.ParamB.x;
            Vector3 focus = origin + (direction * (length * 0.5f));
            float distance = length + LanceCloseMargin;
            float yaw = (Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg) - QuarterTurnDegrees - _camera.transform.eulerAngles.y;
            yield return CaptureBoth("snap-15-lance-draw" + suffix, focus, distance, LanceClosePitch, yaw);
            yield return WaitForSlot(SongSlot.Strike, LanceFireShotSeconds);
            yield return CaptureBoth("snap-15-lance-fire" + suffix, focus, distance, LanceClosePitch, yaw);
        }

        // Collects nests until the swarm is at level 1 + nests: its capacity, its wasps and its level grow by the rules.
        private IEnumerator GrowTo(int nests)
        {
            if (nests <= 0)
            {
                yield break;
            }

            yield return CollectNests(nests);
            Assert.That(_root.Session.Level, Is.EqualTo(1 + nests));
        }

        // Puts the core at `core`, facing north, calls the first wave, stuns each beekeeper as it comes (for the rest of the
        // test: the scene and its session are loaded afresh for the next), and once there is one for each spot puts them
        // there, stunned for the picture, and lets the camera and the river settle.
        private IEnumerator Stage(Vector3 core, params Vector3[] spots)
        {
            _core.PlaceAt(core + (Vector3.back * FaceNorthMetres()), true);
            yield return Walk(Vector3.forward, FaceNorthSeconds);
            HiveDefenseSession session = _root.Session;
            session.BeekeeperSpawned += keeper => keeper.Stun(StageStunSeconds);
            yield return WaitForBeekeepers(spots.Length);

            Assert.That(session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(spots.Length));
            for (int index = 0; index < spots.Length; index++)
            {
                BeekeeperBrain keeper = session.ActiveBeekeeper(index);
                keeper.Spawn(spots[index]);
                keeper.Stun(StageStunSeconds);
            }

            yield return Wait(StageSettleSeconds);
        }

        // The metres the core walks north in FaceNorthSeconds, so it ends on its spot facing north.
        private float FaceNorthMetres() => LoadCoreMove().Speed * FaceNorthSeconds;

        private Vector3 EastHivePosition()
        {
            HiveSet hives = _root.Session.Hives;
            Vector3 east = hives[0].Position;
            for (int index = 1; index < hives.Count; index++)
            {
                if (hives[index].Position.x > east.x)
                {
                    east = hives[index].Position;
                }
            }

            return east;
        }

        // Waits until the song in the slot has run this many seconds by the rules' clock, or the slot has emptied.
        private IEnumerator WaitForSlot(SongSlot slot, float seconds)
        {
            SwarmFormMachine forms = _root.Session.Forms;
            while (forms.Slot(slot).IsActive && forms.Slot(slot).Seconds < seconds)
            {
                yield return null;
            }

            Assert.That(forms.Slot(slot).IsActive, Is.True, slot + " emptied before " + seconds + " s");
        }

        // The game camera's picture on this frame, then the close camera's on the next, turned yawOffset degrees from the
        // game camera's heading.
        private IEnumerator CaptureBoth(string name, Vector3 focus, float distance, float pitch, float yawOffset)
        {
            float yaw = _camera.transform.eulerAngles.y + yawOffset;
            yield return Capture(name);
            yield return CapturePosed(name + CloseSuffix, focus, distance, pitch, yaw);
        }

        private IEnumerator PlayAndPause(IReadOnlyList<int> holes)
        {
            yield return Play(holes);
            yield return Wait(HintPauseSeconds);
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

            // Plan section U: four combos take a few seconds, not the eight they took by standing, so the song may still be
            // cooling down from its level-1 cast.
            SwarmSongCaster caster = _root.Session.Caster;
            for (int song = 0; song < caster.Count; song++)
            {
                float waited = 0f;
                while (caster[song] is T && caster.CooldownFraction(song) > 0f && waited < MaxCooldownWaitSeconds)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }
            }

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
                yield return Wait(NestStayMarginSeconds);
                yield return Play(nests[index].Combo);
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
