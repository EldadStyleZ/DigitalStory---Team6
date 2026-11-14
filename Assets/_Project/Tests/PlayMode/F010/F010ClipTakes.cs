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

        // The song strips: the core 6 m south of the middle, three sticks 4 to 6 m ahead of it, and when each picture is
        // taken, in game seconds after the cast (the Meteor lands at 1 s, the giant forms over about 1.5 s).
        private const float StripWalkMetres = 1.5f;
        private const float StripWalkSeconds = 0.3f;
        private const float StripSettleSeconds = 1.2f;
        private static readonly Vector3 StripCore = new Vector3(0f, 0f, -6f);

        private static readonly Vector3[] StripBeekeepers =
        {
            new Vector3(-3f, 0f, -1.5f), new Vector3(0.5f, 0f, 0f), new Vector3(3.5f, 0f, -2f),
        };

        private static readonly int[] StripWasps = { 800, 4000 };
        private static readonly float[] StripSeconds = { 0.1f, 0.25f, 0.4f, 0.6f, 1f, 1.8f };
        private static readonly float[] MeteorStripSeconds = { 0.25f, 0.6f, 0.95f, 1.1f, 1.4f, 2.2f };
        private static readonly float[] GiantStripSeconds = { 0.3f, 0.8f, 1.4f, 2.2f, 3.2f, 4.5f };

        // Songs-v2 AA: the owner's clips, about 3 s each.
        private const float ClipSeconds = 3f;
        private const float ComboStandBackMetres = 1.2f;
        private const float ComboSettleSeconds = 0.8f;
        private const float ComboBeforeSeconds = 0.5f;
        private const float ComboNoteSeconds = 0.35f;
        private const float ComboAfterSeconds = 1f;
        private const int TreeDirections = 8;
        private const float TreeLineOffset = 0.25f;
        private const float TreeStartBackMetres = 12f;
        private const float TreeWalkEndMetres = 10f;
        private const float TreeClearance = 2f;
        private const float TreePlayEdge = 38f;
        private const float TreeRecordBeforeMetres = 4f;
        private const float TreeViewPitch = 62f;
        private const float TreeViewDistance = 15f;
        private const float TreeViewAheadMetres = -1f;
        private const int PunchHole = 1;
        private const int SlamHole = 3;
        private const float SlamAfterPunchSeconds = 0.5f;
        private const float ThrowClipSeconds = 3.3f;
        private const int FeelWasps = 4000;
        private const float OccluderMarginMetres = 1f;
        private static readonly Vector3 ThrowFocus = new Vector3(0f, 2.5f, 7f);
        private static readonly Vector3 ThrowOffset = new Vector3(9f, 1.5f, -2f);

        // Songs-v2 AB: the impact effects, from the Meteor's cast past its landing (at 1 s) and from the slam's key past its hit.
        private const float ImpactMeteorSeconds = 1.6f;
        private const float ImpactSlamSeconds = 1.5f;
        private const float ImpactNestSeconds = 1.2f;
        private const float LastBreathHealth = 1f;

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

        // Songs-v2 Z: a strip of each song's cast, from the cast to the settle, in the game camera (so the feel layer's
        // shake and punches are in it), at a swarm of 800 and of 4,000. Three stunned sticks stand ahead of the core, in every
        // attack's reach. Each song gets a fresh scene. One picture per song: the 800 row above the 4,000 row.
        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take4_SongStrips()
        {
            string folder = TakeFolder("take-4-song-strips");
            Type[] songs =
            {
                typeof(StingSong), typeof(TornadoSong), typeof(MeteorSong), typeof(HydraSong), typeof(ScytheSong),
                typeof(LanceSong), typeof(DomeSong), typeof(GustSong), typeof(HoneycombSong), typeof(NestSong),
                typeof(HiveCallSong), typeof(ColossusSong),
            };

            foreach (Type song in songs)
            {
                float[] times = song == typeof(ColossusSong) ? GiantStripSeconds
                    : song == typeof(MeteorSong) ? MeteorStripSeconds : StripSeconds;
                var names = new List<string>();
                foreach (int wasps in StripWasps)
                {
                    yield return LoadScene();
                    yield return StageSong(song, wasps);
                    int index = SongIndexOf(song);
                    IReadOnlyList<int> keys = _root.Session.Caster[index].Song.Keys;
                    for (int key = 0; key < keys.Count; key++)
                    {
                        _root.PressHole(keys[key]);
                        yield return null;
                    }

                    // The last note is read on the next tick: the cast.
                    yield return null;
                    float castAt = Time.time;
                    for (int shot = 0; shot < times.Length; shot++)
                    {
                        while (Time.time - castAt < times[shot])
                        {
                            yield return null;
                        }

                        string name = $"{song.Name}-{wasps}-{shot}";
                        _capturer.RequestCapture(Path.Combine(folder, name + ".png"));
                        yield return WaitForCapture();
                        names.Add(name);
                    }
                }

                CombineStrip(folder, names, times.Length, "strip-" + song.Name);
            }
        }

        // Songs-v2 AA, for the owner: the first open nest collected by its combo, in the game camera with the HUD (the combo
        // row fills slot by slot and bursts), a note every 0.35 s, as a player taps.
        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take5_NestCombo()
        {
            yield return LoadScene();
            NestField nests = _root.Session.Nests;
            int nest = 0;
            while (nest < nests.Count && !nests[nest].IsOpen)
            {
                nest++;
            }

            Assert.That(nest, Is.LessThan(nests.Count), "no nest is open at the start");
            _core.PlaceAt(nests[nest].Position + (Vector3.back * ComboStandBackMetres), true);
            yield return WaitGameSeconds(ComboSettleSeconds);
            string folder = TakeFolder("take-5-nest-combo");
            RenderTexture overlay = BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy);
            try
            {
                yield return null;
                int frame = 0;
                yield return Record(folder, ComboBeforeSeconds, frame, null);
                frame += FramesIn(ComboBeforeSeconds);
                IReadOnlyList<int> combo = nests[nest].Combo;
                for (int note = 0; note < combo.Count; note++)
                {
                    _root.PressHole(combo[note]);
                    yield return Record(folder, ComboNoteSeconds, frame, null);
                    frame += FramesIn(ComboNoteSeconds);
                }

                yield return Record(folder, ComboAfterSeconds, frame, null);
            }
            finally
            {
                EndHudOverlay(overlay, document, original, copy);
            }

            Assert.That(nests[nest].IsCollected, Is.True, "the combo did not collect the nest");
        }

        // Songs-v2 X, for the owner: the river walks straight through a nest tree, seen from a steep camera beside the walk,
        // from 4 m before the tree until the river has closed behind it.
        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take6_RiverSplitsRoundATree()
        {
            yield return LoadScene();
            var swarmView = UnityEngine.Object.FindAnyObjectByType<SwarmGpuView>();
            Assert.That(swarmView, Is.Not.Null);
            FindTreeWalk(swarmView, out Vector3 tree, out Vector3 direction, out Vector3 start);
            _core.PlaceAt(start, true);
            yield return WaitGameSeconds(SettleSeconds);
            while (Vector3.Dot(_core.Position - tree, direction) < -TreeRecordBeforeMetres)
            {
                _core.Move(direction, 1f, false, Time.deltaTime);
                yield return null;
            }

            string folder = TakeFolder("take-6-river-splits-round-a-tree");
            Vector3 across = Vector3.Cross(Vector3.up, direction);
            Vector3 view = (across * Mathf.Cos(TreeViewPitch * Mathf.Deg2Rad)) + (Vector3.down * Mathf.Sin(TreeViewPitch * Mathf.Deg2Rad));
            Quaternion rotation = Quaternion.LookRotation(view, direction);
            Vector3 position = tree + (direction * TreeViewAheadMetres) - (view * TreeViewDistance);
            int frames = FramesIn(ClipSeconds);
            for (int index = 0; index < frames; index++)
            {
                _core.Move(direction, 1f, false, Time.deltaTime);
                _capturer.RequestCapture(FramePath(folder, index), position, rotation);
                yield return WaitForCapture();
            }
        }

        // Songs-v2 W, for the owner: two giant attacks, a punch and then the slam, from a fixed camera close off the giant's
        // right, so the red thrown wasps read: they leave on each hit, fly out and stream home gold.
        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take7_GiantThrowsSide()
        {
            yield return LoadScene();
            yield return StageGiant();
            string folder = TakeFolder("take-7-giant-throws-side");
            Vector3 position = ThrowFocus + ThrowOffset;
            Quaternion rotation = Quaternion.LookRotation(ThrowFocus - position, Vector3.up);
            int landed = 0;
            void OnLanded(ColossusAttack attack) => landed++;
            _root.Session.ColossusAttackLanded += OnLanded;
            List<Renderer> hidden = HideBetween(position, GiantCore);
            hidden.AddRange(HideBetween(position, GiantBeekeepers[1]));
            try
            {
                int frames = FramesIn(ThrowClipSeconds);
                int slamAt = -1;
                for (int index = 0; index < frames; index++)
                {
                    if (index == FramesIn(BeforeRightSeconds))
                    {
                        _root.PressHole(PunchHole);
                    }

                    if (landed >= 1 && slamAt < 0)
                    {
                        slamAt = index + FramesIn(SlamAfterPunchSeconds);
                    }

                    if (index == slamAt)
                    {
                        _root.PressHole(SlamHole);
                    }

                    _capturer.RequestCapture(FramePath(folder, index), position, rotation);
                    yield return WaitForCapture();
                }
            }
            finally
            {
                _root.Session.ColossusAttackLanded -= OnLanded;
                foreach (Renderer renderer in hidden)
                {
                    renderer.enabled = true;
                }
            }

            Assert.That(landed, Is.GreaterThanOrEqualTo(2), "the punch and the slam did not both land during the take");
        }

        // Props and nest trees standing between the fixed camera and the giant would hide it on some maps; they are hidden for
        // the take and shown again after it.
        private static List<Renderer> HideBetween(Vector3 camera, Vector3 subject)
        {
            var hidden = new List<Renderer>();
            var a = new Vector2(camera.x, camera.z);
            var b = new Vector2(subject.x, subject.z);
            var owners = new List<Component>();
            owners.AddRange(UnityEngine.Object.FindObjectsByType<SceneryView>());
            owners.AddRange(UnityEngine.Object.FindObjectsByType<NestView>());
            foreach (Component owner in owners)
            {
                foreach (Renderer renderer in owner.GetComponentsInChildren<Renderer>())
                {
                    Bounds bounds = renderer.bounds;
                    var middle = new Vector2(bounds.center.x, bounds.center.z);
                    float reach = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
                    Vector2 segment = b - a;
                    float along = Mathf.Clamp01(Vector2.Dot(middle - a, segment) / segment.sqrMagnitude);
                    if (renderer.enabled && Vector2.Distance(middle, a + (segment * along)) < reach + OccluderMarginMetres)
                    {
                        renderer.enabled = false;
                        hidden.Add(renderer);
                    }
                }
            }

            return hidden;
        }

        // Songs-v2 Y, for the owner: a strong feel moment, the Meteor at 4,000 wasps into three sticks, in the game camera with
        // the HUD: the cast's zoom punch, the climb, the landing's shake, the hit numbers and the kill chain's stamp.
        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take8_FeelMeteor()
        {
            yield return LoadScene();
            yield return StageSong(typeof(MeteorSong), FeelWasps);
            string folder = TakeFolder("take-8-feel-meteor");
            RenderTexture overlay = BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy);
            try
            {
                yield return null;
                IReadOnlyList<int> keys = _root.Session.Caster[SongIndexOf(typeof(MeteorSong))].Song.Keys;
                int frame = 0;
                for (int key = 0; key < keys.Count; key++)
                {
                    _root.PressHole(keys[key]);
                    yield return Record(folder, FrameSeconds, frame, null);
                    frame++;
                }

                yield return Record(folder, ClipSeconds, frame, null);
            }
            finally
            {
                EndHudOverlay(overlay, document, original, copy);
            }
        }

        // Songs-v2 AB, for the owner: the optional impact effects in the game camera with the HUD. One folder: the Meteor at
        // 4,000 wasps into three stunned sticks on their last breath (its landing and the kill), then the giant's ground slam.
        // A second folder: the first open nest collected by its combo, from its last note. Without the local library it shows
        // the game's own effects.
        [UnityTest]
        [Explicit(ExplicitReason)]
        public IEnumerator Take9_ImpactEffects()
        {
            string folder = TakeFolder("take-9-impact-effects");
            int frame = 0;
            yield return LoadScene();
            yield return StageSong(typeof(MeteorSong), FeelWasps);

            // On their last breath, so the Meteor's hit kills and the death effect shows.
            for (int index = 0; index < StripBeekeepers.Length; index++)
            {
                BeekeeperBrain keeper = _root.Session.ActiveBeekeeper(index);
                keeper.TakeDamage(keeper.Health - LastBreathHealth);
            }

            RenderTexture overlay = BeginHudOverlay(out UIDocument document, out PanelSettings original, out PanelSettings copy);
            try
            {
                yield return null;
                IReadOnlyList<int> keys = _root.Session.Caster[SongIndexOf(typeof(MeteorSong))].Song.Keys;
                for (int key = 0; key < keys.Count; key++)
                {
                    _root.PressHole(keys[key]);
                    yield return Record(folder, FrameSeconds, frame, null);
                    frame++;
                }

                yield return Record(folder, ImpactMeteorSeconds, frame, null);
                frame += FramesIn(ImpactMeteorSeconds);
            }
            finally
            {
                EndHudOverlay(overlay, document, original, copy);
            }

            yield return LoadScene();
            yield return StageGiant();
            overlay = BeginHudOverlay(out document, out original, out copy);
            try
            {
                yield return null;
                _root.PressHole(SlamHole);
                yield return Record(folder, ImpactSlamSeconds, frame, null);
            }
            finally
            {
                EndHudOverlay(overlay, document, original, copy);
            }

            yield return LoadScene();
            NestField nests = _root.Session.Nests;
            int nest = 0;
            while (nest < nests.Count && !nests[nest].IsOpen)
            {
                nest++;
            }

            Assert.That(nest, Is.LessThan(nests.Count), "no nest is open at the start");
            _core.PlaceAt(nests[nest].Position + (Vector3.back * ComboStandBackMetres), true);
            yield return WaitGameSeconds(ComboSettleSeconds);
            string nestFolder = TakeFolder("take-9b-nest-burst");
            overlay = BeginHudOverlay(out document, out original, out copy);
            try
            {
                IReadOnlyList<int> combo = nests[nest].Combo;
                for (int note = 0; note < combo.Count - 1; note++)
                {
                    _root.PressHole(combo[note]);
                    yield return WaitGameSeconds(ComboNoteSeconds);
                }

                _root.PressHole(combo[combo.Count - 1]);
                yield return Record(nestFolder, ImpactNestSeconds, 0, null);
            }
            finally
            {
                EndHudOverlay(overlay, document, original, copy);
            }

            Assert.That(nests[nest].IsCollected, Is.True, "the combo did not collect the nest");
        }

        // A nest tree with a straight walk through it, clear of the other obstacles and inside the play area.
        private void FindTreeWalk(SwarmGpuView swarmView, out Vector3 tree, out Vector3 direction, out Vector3 start)
        {
            NestField nests = _root.Session.Nests;
            for (int nest = 0; nest < nests.Count; nest++)
            {
                Vector3 nestPosition = nests[nest].Position;
                for (int index = 0; index < swarmView.ObstacleCount; index++)
                {
                    Vector4 obstacle = swarmView.Obstacle(index);
                    var middle = new Vector3(obstacle.x, 0f, obstacle.y);
                    if (Vector3.Distance(middle, new Vector3(nestPosition.x, 0f, nestPosition.z)) > obstacle.z + 1f)
                    {
                        continue;
                    }

                    for (int turn = 0; turn < TreeDirections; turn++)
                    {
                        Vector3 heading = Quaternion.Euler(0f, turn * (360f / TreeDirections), 0f) * Vector3.forward;
                        Vector3 side = Vector3.Cross(Vector3.up, heading) * TreeLineOffset;
                        Vector3 from = middle - (heading * TreeStartBackMetres) + side;
                        Vector3 to = middle + (heading * TreeWalkEndMetres) + side;
                        if (IsInPlayArea(from) && IsInPlayArea(to) && IsClearOfOthers(swarmView, index, from, to))
                        {
                            tree = middle;
                            direction = heading;
                            start = new Vector3(from.x, _core.Position.y, from.z);
                            return;
                        }
                    }
                }
            }

            Assert.Fail("No nest tree has a clear straight walk through it on this map.");
            tree = direction = start = Vector3.zero;
        }

        private static bool IsInPlayArea(Vector3 point) => Mathf.Abs(point.x) <= TreePlayEdge && Mathf.Abs(point.z) <= TreePlayEdge;

        private static bool IsClearOfOthers(SwarmGpuView swarmView, int tree, Vector3 from, Vector3 to)
        {
            var a = new Vector2(from.x, from.z);
            var b = new Vector2(to.x, to.z);
            for (int index = 0; index < swarmView.ObstacleCount; index++)
            {
                if (index == tree)
                {
                    continue;
                }

                Vector4 obstacle = swarmView.Obstacle(index);
                var point = new Vector2(obstacle.x, obstacle.y);
                Vector2 segment = b - a;
                float along = Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
                if (Vector2.Distance(point, a + (segment * along)) < obstacle.z + TreeClearance)
                {
                    return false;
                }
            }

            return true;
        }

        // Puts the core 6 m south of the middle facing north, with three sticks stunned ahead, the swarm at `wasps` and, for the
        // giant, full Fury; then lets the river settle.
        private IEnumerator StageSong(Type song, int wasps)
        {
            HiveDefenseSession session = _root.Session;
            _core.PlaceAt(StripCore + (Vector3.back * StripWalkMetres), true);
            float walked = 0f;
            while (walked < StripWalkSeconds)
            {
                _core.Move(Vector3.forward, 1f, false, Time.deltaTime);
                walked += Time.deltaTime;
                yield return null;
            }

            session.BeekeeperSpawned += keeper => keeper.Stun(StunSeconds);
            _root.CallWave();
            float waited = 0f;
            while (session.ActiveBeekeeperCount < StripBeekeepers.Length && waited < MaxWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(StripBeekeepers.Length));
            for (int index = 0; index < StripBeekeepers.Length; index++)
            {
                BeekeeperBrain keeper = session.ActiveBeekeeper(index);
                keeper.Spawn(StripBeekeepers[index]);
                keeper.Stun(StunSeconds);
            }

            SwarmBody swarm = session.Swarm;
            swarm.GrowCapacity(Mathf.Max(0, wasps - swarm.Capacity));
            if (swarm.Count < wasps)
            {
                swarm.Gain(wasps - swarm.Count);
            }
            else
            {
                swarm.Lose(swarm.Count - wasps);
            }

            if (song == typeof(ColossusSong))
            {
                session.Fury.AddDamage(FuryDamage);
            }

            yield return WaitGameSeconds(StripSettleSeconds);
        }

        private int SongIndexOf(Type song)
        {
            SwarmSongCaster caster = _root.Session.Caster;
            for (int index = 0; index < caster.Count; index++)
            {
                if (caster[index].GetType() == song)
                {
                    return index;
                }
            }

            Assert.Fail("The scene has no " + song.Name + ".");
            return -1;
        }

        // Writes the pictures, at half size, in rows of `columns`, the first at the top left, as one file.
        private static void CombineStrip(string folder, List<string> names, int columns, string name)
        {
            var images = new Texture2D[names.Count];
            Texture2D strip = null;
            try
            {
                for (int index = 0; index < names.Count; index++)
                {
                    images[index] = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    Assert.That(images[index].LoadImage(File.ReadAllBytes(Path.Combine(folder, names[index] + ".png"))), Is.True, names[index]);
                }

                int width = images[0].width / 2;
                int height = images[0].height / 2;
                int rows = (names.Count + columns - 1) / columns;
                strip = new Texture2D(width * columns, height * rows, TextureFormat.RGB24, false);
                var pixels = new Color[width * height];
                for (int index = 0; index < images.Length; index++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            pixels[(y * width) + x] = images[index].GetPixel(x * 2, y * 2);
                        }
                    }

                    int column = index % columns;
                    int row = rows - 1 - (index / columns);
                    strip.SetPixels(column * width, row * height, width, height, pixels);
                }

                strip.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), strip.EncodeToPNG());
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

                if (strip != null)
                {
                    UnityEngine.Object.Destroy(strip);
                }
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
