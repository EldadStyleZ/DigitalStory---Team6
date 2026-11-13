#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
    /// Plan section Q: the swarm's motion, measured. The test reads a sample of wasps back from the GPU every frame while the
    /// core stands, walks north, stops, walks east, stops again and casts Hive Call, once at 60 and once at 120 frames a
    /// second, and measures three things for each hand-over between standing and moving or between forms:
    /// <list type="bullet">
    /// <item>the largest change of the swarm's mean velocity from one frame to the next, per second (a jolt the eye reads as
    /// the whole swarm lurching at once);</item>
    /// <item>how spread out the wasps' answers are (from the 10th to the 90th percentile): a wave runs through the swarm, a
    /// snap moves every wasp together. A wasp has answered a start once it flies with the core at 60% of its speed, a stop
    /// once it has slowed below that, and Hive Call once it is in the halo;</item>
    /// <item>how long the front of the swarm (its quickest tenth) takes to answer, which must stay quick.</item>
    /// </list>
    /// It also compares the two frame rates: the swarm must move the same at both. A second test draws the hand-overs as a
    /// strip of pictures (snap-18-wave-*.png) when F010_SNAPSHOT_DIR names a folder.
    /// </summary>
    /// <remarks>
    /// Reading the swarm back stalls the GPU, which is why the game itself never does it. A coroutine resumes after Update
    /// and before LateUpdate, where SwarmGpuView steps the swarm, so each read holds the step of the frame before. A move the
    /// test makes after Update reaches the swarm in the next frame's step; a cast, made in Update, reaches it in that
    /// frame's step. Times below run from the first step that saw the change.
    /// </remarks>
    public sealed class HiveDefenseMotionTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string DirectoryVariable = "F010_SNAPSHOT_DIR";
        private const float SettleSeconds = 0.1f;
        private const int SampleCount = 200;
        private const float CoreSpeed = 4.5f;

        // The scenario, in game seconds. Three seconds of walking at 4.5 m/s stretch the whole river out behind the core.
        private const float FirstStandSeconds = 3f;
        private const float WalkSeconds = 3f;
        private const float StandSeconds = 4f;
        private const float CastWatchSeconds = 2f;

        // A wasp that moves this far in one frame jumped: a wasp at the river's tail starts again at its head. Its own speed
        // is reset, so the frame after a jump is not counted either.
        private const float JumpMetres = 1f;

        // A wasp has answered a start once it flies the core's way at this share of its speed for AnswerHoldSeconds, and a
        // stop once it has slowed below it for as long. Only the wasps that were not yet flying that fast count for a start,
        // and only those flying faster than the stop's floor for a stop: a wasp that has just jumped from the river's tail
        // to its head is still, and one round the eight may already be flying the core's way.
        private const float AnswerSpeedShare = 0.6f;
        private const float StopFloorShare = 0.7f;
        private const float AnswerHoldSeconds = 0.1f;
        private const float FrontPercentile = 0.1f;
        private const float SpreadLow = 0.1f;
        private const float SpreadHigh = 0.9f;
        private const int CurveBuckets = 20;
        private const float CurveBucketSeconds = 0.1f;

        // Hive Call's halo: a ring 0.9 m round the core, 2.2 m over it (HaloTarget in WaspSwarm.compute).
        private const float HaloRadius = 0.9f;
        private const float HaloHeight = 2.2f;
        private const float HaloSlack = 0.45f;

        // The bounds the motion of section Q meets with margin. Before it (2026-11-13, same test) every wasp of the standing
        // eight followed the core's start at once and every wasp of the river stopped with it: the mean acceleration peaked
        // at 17, 53, 48 and 52 m/s² on the start, stop, turn start and turn stop and at 79 on Hive Call, the river's wasps
        // all stopped within 0.017 s of each other, and 60 and 120 fps differed by up to 1.19 m/s. The first three bounds
        // fail for it. The front bounds guard the answer: before, the front of the swarm answered a start in 0.10 to 0.13 s
        // and reached Hive Call's halo in 0.13 s.
        private const float MostMeanAcceleration = 15f;

        // A change of form sweeps through the swarm in river order, which round the standing eight is loop order, so the
        // wasps easing over at any moment come from one stretch of the loop and lift and close in together: that clump,
        // not the whole swarm, keeps Hive Call's mean acceleration near 48 m/s² (79 before).
        private const float MostCastMeanAcceleration = 60f;
        private const float LeastStopSpread = 0.25f;
        private const float MostRateDifference = 0.6f;
        private const float MostFrontSeconds = 0.3f;
        private const float MostHaloFrontSeconds = 0.3f;

        // The strip: six pictures 0.1 s apart from the start of a walk, and six from its stop, at half size in a sheet.
        private const int StripFrames = 6;
        private const float StripSpacingSeconds = 0.1f;
        private const int StripWidth = 960;
        private const int StripHeight = 540;
        private const int SheetColumns = 3;
        private const float StripDistance = 12f;
        private const float StripPitch = 38f;
        private const float StripFocusHeight = 0.8f;
        private const float StripLookAhead = 2f;
        private const float StripStandSeconds = 3f;
        private const float StripWalkSeconds = 2.5f;

        private HiveDefenseRoot _root;
        private SwarmCoreMotor _core;
        private SwarmGpuView _swarmView;
        private float _savedTimeScale;
        private float _savedCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator Save()
        {
            _savedTimeScale = Time.timeScale;
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Motion_At60And120FramesASecond_HandsOverAsAWaveAndMovesTheSame()
        {
            var slow = new Recording(1f / 60f);
            var fast = new Recording(1f / 120f);
            yield return Run(slow);
            yield return Run(fast);

            float rateDifference = slow.MostDifferenceFrom(fast);
            Debug.Log(slow.Report() + "\n" + fast.Report() + $"\n[F010] motion: mean velocity at 60 against 120 fps differs by {rateDifference:0.000} m/s at most");

            // Every bound is checked before any fails, so one run reports all that is wrong.
            var failures = new StringBuilder();
            foreach (Recording recording in new[] { slow, fast })
            {
                foreach (Phase phase in recording.Phases)
                {
                    string where = $"{phase.Name} at {1f / recording.FrameSeconds:0} fps";
                    if (phase.Kind == PhaseKind.Cast)
                    {
                        Expect(failures, phase.PeakMeanAcceleration <= MostCastMeanAcceleration, $"{where}: the whole swarm lurched at once ({phase.PeakMeanAcceleration:0.0} m/s², most {MostCastMeanAcceleration}).");
                        Expect(failures, phase.FrontSeconds <= MostHaloFrontSeconds, $"{where}: the front reached Hive Call's halo late ({phase.FrontSeconds:0.000} s, most {MostHaloFrontSeconds}).");
                        continue;
                    }

                    Expect(failures, phase.PeakMeanAcceleration <= MostMeanAcceleration, $"{where}: the whole swarm lurched at once ({phase.PeakMeanAcceleration:0.0} m/s², most {MostMeanAcceleration}).");
                    if (phase.Kind == PhaseKind.Stop)
                    {
                        Expect(failures, phase.Spread >= LeastStopSpread, $"{where}: every wasp stopped together instead of in a wave (spread {phase.Spread:0.000} s, least {LeastStopSpread}).");
                    }
                    else
                    {
                        Expect(failures, phase.FrontSeconds <= MostFrontSeconds, $"{where}: the head of the swarm answered late ({phase.FrontSeconds:0.000} s, most {MostFrontSeconds}).");
                    }
                }
            }

            Expect(failures, rateDifference <= MostRateDifference, $"The swarm moves differently at 60 and at 120 frames a second ({rateDifference:0.000} m/s, most {MostRateDifference}).");
            Assert.That(failures.Length, Is.EqualTo(0), failures.ToString());
        }

        [UnityTest]
        public IEnumerator Motion_StartAndStop_DrawsTheWaveStrip()
        {
            string directory = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrEmpty(directory))
            {
                Assert.Ignore($"Set {DirectoryVariable} to a folder to write the F-010 snapshots.");
            }

            Directory.CreateDirectory(directory);
            yield return Load(1f / 60f);
            Camera camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
            Assert.That(camera, Is.Not.Null);
            var capturer = new GameObject("Strip Capturer").AddComponent<SnapshotCapturer>();
            capturer.Initialize(camera, StripWidth, StripHeight);

            yield return Hold(Vector3.zero, StripStandSeconds);
            yield return Hold(Vector3.forward, StripWalkSeconds);
            yield return Hold(Vector3.zero, StripStandSeconds);

            // Walking east, seen from the south: the core moves to the right of the picture.
            var start = new List<string>();
            yield return Strip(capturer, directory, "snap-18-wave-start", Vector3.right, _core.Position + (Vector3.right * StripLookAhead), start);
            yield return Hold(Vector3.right, StripWalkSeconds - (StripFrames * StripSpacingSeconds));
            var stop = new List<string>();
            yield return Strip(capturer, directory, "snap-18-wave-stop", Vector3.zero, _core.Position - (Vector3.right * StripLookAhead), stop);

            Sheet(start, Path.Combine(directory, "snap-18-wave-start-sheet.png"));
            Sheet(stop, Path.Combine(directory, "snap-18-wave-stop-sheet.png"));
        }

        private static void Expect(StringBuilder failures, bool isMet, string message)
        {
            if (!isMet)
            {
                failures.AppendLine(message);
            }
        }

        private IEnumerator Load(float frameSeconds)
        {
            Time.captureDeltaTime = frameSeconds;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            int settleFrames = Mathf.RoundToInt(SettleSeconds / frameSeconds);
            for (int frame = 0; frame < settleFrames; frame++)
            {
                yield return null;
            }

            _root = UnityEngine.Object.FindAnyObjectByType<HiveDefenseRoot>();
            _core = UnityEngine.Object.FindAnyObjectByType<SwarmCoreMotor>();
            _swarmView = UnityEngine.Object.FindAnyObjectByType<SwarmGpuView>();
            Assert.That(_root, Is.Not.Null);
            Assert.That(_core, Is.Not.Null);
            Assert.That(_swarmView, Is.Not.Null);
            Assert.That(_swarmView.IsRunning, Is.True, "The swarm's GPU view must run to be measured.");
        }

        private IEnumerator Run(Recording recording)
        {
            yield return Load(recording.FrameSeconds);

            // Room in the swarm, so Hive Call has wasps to bring and keeps its form for a moment.
            HiveDefenseSession session = _root.Session;
            session.Swarm.GrowCapacity(session.Swarm.Count);
            recording.Begin(_swarmView.WaspStates, session.Swarm.Count);

            yield return Watch(recording, null, Vector3.zero, FirstStandSeconds);
            yield return Watch(recording, new Phase("start", PhaseKind.Start, Vector3.forward), Vector3.forward, WalkSeconds);
            yield return Watch(recording, new Phase("stop", PhaseKind.Stop, Vector3.forward), Vector3.zero, StandSeconds);
            yield return Watch(recording, new Phase("turn start", PhaseKind.Start, Vector3.right), Vector3.right, WalkSeconds);
            yield return Watch(recording, new Phase("turn stop", PhaseKind.Stop, Vector3.right), Vector3.zero, StandSeconds);

            // Hive Call: the whole swarm's form changes on the frame its last note lands.
            int castFrame = -1;
            Action<int> onCast = _ => castFrame = Time.frameCount;
            session.SongCastNow += onCast;
            try
            {
                // One note every 60th of a second at both rates, so the cast lands at the same moment.
                IReadOnlyList<int> keys = KeysOf<HiveCallSong>();
                var cast = new Phase("hive call", PhaseKind.Cast, Vector3.zero);
                recording.Start(cast, int.MaxValue);
                int framesPerNote = Mathf.Max(1, Mathf.RoundToInt((1f / 60f) / recording.FrameSeconds));
                for (int frame = 0; frame < keys.Count * framesPerNote; frame++)
                {
                    if (frame % framesPerNote == 0)
                    {
                        _root.PressHole(keys[frame / framesPerNote]);
                    }

                    yield return null;
                    recording.Read(_core.Position);
                }

                int frames = Mathf.RoundToInt(CastWatchSeconds / recording.FrameSeconds);
                for (int frame = 0; frame < frames; frame++)
                {
                    if (castFrame >= 0)
                    {
                        cast.EffectFrame = castFrame;
                    }

                    yield return null;
                    recording.Read(_core.Position);
                }

                Assert.That(castFrame, Is.GreaterThanOrEqualTo(0), "Hive Call was not cast.");
                recording.Finish();
            }
            finally
            {
                session.SongCastNow -= onCast;
            }
        }

        // Walks (or stands, for a zero direction) for a time, reading the swarm each frame. A phase's change reaches the
        // swarm in the step of the frame after its first.
        private IEnumerator Watch(Recording recording, Phase phase, Vector3 direction, float seconds)
        {
            if (phase != null)
            {
                recording.Start(phase, Time.frameCount + 1);
            }

            int frames = Mathf.RoundToInt(seconds / recording.FrameSeconds);
            for (int frame = 0; frame < frames; frame++)
            {
                if (direction != Vector3.zero)
                {
                    _core.Move(direction, 1f, false, Time.deltaTime);
                }

                yield return null;
                recording.Read(_core.Position);
            }

            recording.Finish();
        }

        private IEnumerator Hold(Vector3 direction, float seconds)
        {
            int frames = Mathf.RoundToInt(seconds / Time.captureDeltaTime);
            for (int frame = 0; frame < frames; frame++)
            {
                if (direction != Vector3.zero)
                {
                    _core.Move(direction, 1f, false, Time.deltaTime);
                }

                yield return null;
            }
        }

        // Six pictures 0.1 s apart from a fixed camera south of the focus, walking or standing all the while.
        private IEnumerator Strip(SnapshotCapturer capturer, string directory, string name, Vector3 direction, Vector3 focus, List<string> paths)
        {
            Quaternion rotation = Quaternion.Euler(StripPitch, 0f, 0f);
            Vector3 position = focus + (Vector3.up * StripFocusHeight) - (rotation * Vector3.forward * StripDistance);
            int spacing = Mathf.RoundToInt(StripSpacingSeconds / Time.captureDeltaTime);
            for (int picture = 0; picture < StripFrames; picture++)
            {
                string path = Path.Combine(directory, $"{name}-{picture}.png");
                paths.Add(path);
                capturer.RequestCapture(path, position, rotation);
                for (int frame = 0; frame < spacing; frame++)
                {
                    if (direction != Vector3.zero)
                    {
                        _core.Move(direction, 1f, false, Time.deltaTime);
                    }

                    yield return null;
                }

                Assert.That(capturer.IsBusy, Is.False, $"{name}-{picture}.png was not written.");
                Assert.That(capturer.LastWasFlat, Is.False, $"{name}-{picture}.png is a single flat colour.");
            }
        }

        // Puts the pictures side by side at half size, three to a row, so the whole hand-over reads at a glance.
        private static void Sheet(List<string> paths, string sheetPath)
        {
            int rows = (paths.Count + SheetColumns - 1) / SheetColumns;
            int tileWidth = StripWidth / 2;
            int tileHeight = StripHeight / 2;
            var sheet = new Texture2D(tileWidth * SheetColumns, tileHeight * rows, TextureFormat.RGB24, false);
            var picture = new Texture2D(2, 2);
            try
            {
                for (int index = 0; index < paths.Count; index++)
                {
                    Assert.That(picture.LoadImage(File.ReadAllBytes(paths[index])), Is.True, paths[index]);
                    int left = (index % SheetColumns) * tileWidth;
                    int bottom = (rows - 1 - (index / SheetColumns)) * tileHeight;
                    for (int y = 0; y < tileHeight; y++)
                    {
                        for (int x = 0; x < tileWidth; x++)
                        {
                            sheet.SetPixel(left + x, bottom + y, picture.GetPixel(x * 2, y * 2));
                        }
                    }
                }

                sheet.Apply();
                File.WriteAllBytes(sheetPath, sheet.EncodeToPNG());
                Debug.Log($"[F010] snapshot {sheetPath}");
            }
            finally
            {
                UnityEngine.Object.Destroy(sheet);
                UnityEngine.Object.Destroy(picture);
            }
        }

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

        private enum PhaseKind
        {
            Start,
            Stop,
            Cast,
        }

        // 48 bytes, WaspDynamic in WaspSwarm.compute.
        [StructLayout(LayoutKind.Sequential)]
        private struct WaspState
        {
            public Vector3 Position;
            public float State;
            public Vector3 Velocity;
            public float StateTime;
            public Vector4 Tint;
        }

        // One hand-over: its peak jolt, each sampled wasp's answer, and the swarm's mean velocity every 60th of a second
        // from the first step that saw the change, which lines the two frame rates up.
        private sealed class Phase
        {
            private readonly List<float> _answers = new List<float>();

            public Phase(string name, PhaseKind kind, Vector3 direction)
            {
                Name = name;
                Kind = kind;
                Direction = direction;
                EffectFrame = int.MaxValue;
            }

            public string Name { get; }

            public PhaseKind Kind { get; }

            public Vector3 Direction { get; }

            // The first frame whose swarm step saw the change.
            public int EffectFrame { get; set; }

            public float PeakMeanAcceleration { get; private set; }

            public float PeakMeanVelocityStep { get; private set; }

            public float PeakSeconds { get; private set; }

            // The largest mean acceleration in each tenth of a second from the change, for the report.
            public float[] Curve { get; } = new float[CurveBuckets];

            public float RateDifference { get; set; }

            public List<Vector3> MeanVelocityTicks { get; } = new List<Vector3>();

            public float Low { get; private set; }

            public float High { get; private set; }

            public float Spread => High - Low;

            public int Counted { get; private set; }

            public int NeverAnswered { get; private set; }

            public float FrontSeconds { get; private set; }

            public void Close(float[] answers, bool[] counts, float watched)
            {
                _answers.Clear();
                NeverAnswered = 0;
                for (int index = 0; index < answers.Length; index++)
                {
                    if (!counts[index])
                    {
                        continue;
                    }

                    if (answers[index] < 0f)
                    {
                        NeverAnswered++;
                    }

                    _answers.Add(answers[index] >= 0f ? answers[index] : watched);
                }

                Counted = _answers.Count;
                Low = Percentile(_answers, SpreadLow);
                High = Percentile(_answers, SpreadHigh);
                FrontSeconds = Percentile(_answers, FrontPercentile);
            }

            public void Accelerated(float meanAcceleration, float since, float frameSeconds)
            {
                if (meanAcceleration > PeakMeanAcceleration)
                {
                    PeakMeanAcceleration = meanAcceleration;
                    PeakMeanVelocityStep = meanAcceleration * frameSeconds;
                    PeakSeconds = since;
                }

                int bucket = Mathf.FloorToInt(since / CurveBucketSeconds);
                if (bucket >= 0 && bucket < CurveBuckets)
                {
                    Curve[bucket] = Mathf.Max(Curve[bucket], meanAcceleration);
                }
            }

            public string Report()
            {
                var curve = new StringBuilder();
                foreach (float value in Curve)
                {
                    curve.Append(' ').Append(Mathf.RoundToInt(value));
                }

                return $"{Name}: peak mean acceleration {PeakMeanAcceleration:0.0} m/s² at {PeakSeconds:0.000} s (mean velocity step {PeakMeanVelocityStep:0.000} m/s a frame), " +
                    $"answers {Low:0.000} to {High:0.000} s (spread {Spread:0.000} s; {Counted} counted, {NeverAnswered} never), front {FrontSeconds:0.000} s, " +
                    $"60 against 120 fps {RateDifference:0.000} m/s; by tenths of a second:{curve}";
            }

            private static float Percentile(List<float> values, float share)
            {
                if (values.Count == 0)
                {
                    return -1f;
                }

                values.Sort();
                int index = Mathf.Clamp(Mathf.RoundToInt(share * (values.Count - 1)), 0, values.Count - 1);
                return values[index];
            }
        }

        // Everything one frame rate's run measured.
        private sealed class Recording
        {
            private readonly List<Phase> _phases = new List<Phase>();
            private readonly float[] _answers = new float[SampleCount];
            private readonly bool[] _counts = new bool[SampleCount];
            private readonly float[] _held = new float[SampleCount];
            private readonly Vector3[] _lastPosition = new Vector3[SampleCount];
            private readonly Vector3[] _lastVelocity = new Vector3[SampleCount];
            private readonly bool[] _hasLast = new bool[SampleCount];
            private readonly int[] _indices = new int[SampleCount];
            private GraphicsBuffer _buffer;
            private WaspState[] _states;
            private int _readCount;
            private Phase _current;
            private bool _isFirstRead;
            private float _watched;

            public Recording(float frameSeconds)
            {
                FrameSeconds = frameSeconds;
            }

            public float FrameSeconds { get; }

            public IReadOnlyList<Phase> Phases => _phases;

            public void Begin(GraphicsBuffer buffer, int living)
            {
                Assert.That(buffer, Is.Not.Null, "The swarm has no GPU buffer to read.");
                _buffer = buffer;
                _readCount = living;
                _states = new WaspState[living];
                for (int sample = 0; sample < SampleCount; sample++)
                {
                    _indices[sample] = (sample * living) / SampleCount;
                }
            }

            public void Start(Phase phase, int effectFrame)
            {
                _current = phase;
                _current.EffectFrame = effectFrame;
                _isFirstRead = true;
                _watched = 0f;
                for (int sample = 0; sample < SampleCount; sample++)
                {
                    _answers[sample] = -1f;
                    _counts[sample] = false;
                    _held[sample] = 0f;
                }
            }

            public void Finish()
            {
                if (_current != null)
                {
                    _current.Close(_answers, _counts, _watched);
                    _phases.Add(_current);
                    _current = null;
                }
            }

            public void Read(Vector3 core)
            {
                _buffer.GetData(_states, 0, 0, _readCount);
                float dt = FrameSeconds;

                // The step this read holds is the frame before's; its time from the first step that saw the change.
                float since = _current != null ? (Time.frameCount - _current.EffectFrame) * dt : -1f;
                bool isFirst = since > 0f && _isFirstRead;
                if (isFirst)
                {
                    _isFirstRead = false;
                }

                Vector3 accelerationSum = Vector3.zero;
                Vector3 velocitySum = Vector3.zero;
                int counted = 0;
                int living = 0;
                for (int sample = 0; sample < SampleCount; sample++)
                {
                    WaspState state = _states[_indices[sample]];
                    if (state.State > 0.5f)
                    {
                        _hasLast[sample] = false;
                        continue;
                    }

                    living++;
                    velocitySum += state.Velocity;
                    bool jumped = _hasLast[sample] && (state.Position - _lastPosition[sample]).magnitude > JumpMetres;
                    if (_hasLast[sample] && !jumped)
                    {
                        accelerationSum += (state.Velocity - _lastVelocity[sample]) / dt;
                        counted++;
                    }

                    if (since > 0f)
                    {
                        if (isFirst)
                        {
                            _counts[sample] = Counts(state);
                        }

                        Track(sample, state, core, since);
                    }

                    _hasLast[sample] = !jumped;
                    _lastPosition[sample] = state.Position;
                    _lastVelocity[sample] = state.Velocity;
                }

                if (_current == null || since <= 0f)
                {
                    return;
                }

                // Every 60th of a second from the change: each frame at 60 fps, every other at 120.
                float ticks = since * 60f;
                if (Mathf.Abs(ticks - Mathf.Round(ticks)) < 0.01f && Mathf.RoundToInt(ticks) == _current.MeanVelocityTicks.Count + 1)
                {
                    _current.MeanVelocityTicks.Add(living > 0 ? velocitySum / living : Vector3.zero);
                }

                if (counted > 0)
                {
                    _current.Accelerated((accelerationSum / counted).magnitude, since, dt);
                }

                _watched = since;
            }

            // The most the swarm's mean velocity differs between this run and another at the same moment of a phase.
            public float MostDifferenceFrom(Recording other)
            {
                float most = 0f;
                int phases = Mathf.Min(_phases.Count, other._phases.Count);
                for (int phase = 0; phase < phases; phase++)
                {
                    List<Vector3> mine = _phases[phase].MeanVelocityTicks;
                    List<Vector3> theirs = other._phases[phase].MeanVelocityTicks;
                    int ticks = Mathf.Min(mine.Count, theirs.Count);
                    float phaseMost = 0f;
                    for (int tick = 0; tick < ticks; tick++)
                    {
                        phaseMost = Mathf.Max(phaseMost, (mine[tick] - theirs[tick]).magnitude);
                    }

                    _phases[phase].RateDifference = phaseMost;
                    other._phases[phase].RateDifference = phaseMost;
                    most = Mathf.Max(most, phaseMost);
                }

                return most;
            }

            public string Report()
            {
                var text = new StringBuilder();
                text.Append($"[F010] motion at {1f / FrameSeconds:0} fps, {SampleCount} of {_readCount} wasps:");
                foreach (Phase phase in _phases)
                {
                    text.Append("\n[F010] motion   ").Append(phase.Report());
                }

                return text.ToString();
            }

            // Whether a wasp's answer counts, from how it flies on the first step that saw the change.
            private bool Counts(WaspState state)
            {
                float along = Vector3.Dot(state.Velocity, _current.Direction);
                switch (_current.Kind)
                {
                    case PhaseKind.Start:
                        return along < AnswerSpeedShare * CoreSpeed;
                    case PhaseKind.Stop:
                        return along > StopFloorShare * CoreSpeed;
                    default:
                        return true;
                }
            }

            private void Track(int sample, WaspState state, Vector3 core, float since)
            {
                if (_answers[sample] >= 0f)
                {
                    return;
                }

                bool isThere;
                float hold = AnswerHoldSeconds;
                float along = Vector3.Dot(state.Velocity, _current.Direction);
                if (_current.Kind == PhaseKind.Cast)
                {
                    Vector3 fromCore = state.Position - core;
                    float height = fromCore.y;
                    fromCore.y = 0f;
                    isThere = Mathf.Abs(fromCore.magnitude - HaloRadius) <= HaloSlack && Mathf.Abs(height - HaloHeight) <= HaloSlack;
                    hold = 0f;
                }
                else if (_current.Kind == PhaseKind.Start)
                {
                    isThere = along >= AnswerSpeedShare * CoreSpeed;
                }
                else
                {
                    isThere = along <= AnswerSpeedShare * CoreSpeed;
                }

                _held[sample] = isThere ? _held[sample] + FrameSeconds : 0f;
                if (isThere && _held[sample] >= hold)
                {
                    _answers[sample] = since - _held[sample] + FrameSeconds;
                }
            }
        }
    }
}
#endif
