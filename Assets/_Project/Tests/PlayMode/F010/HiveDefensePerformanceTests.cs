#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using DigitalStory.View;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Measures the per-frame cost of the hive defense scene at the maximum swarm: garbage allocated and main thread time.
    /// In batch mode there is no vsync and no Game view, so the times are CPU-side numbers, not Editor Profiler numbers.
    /// </summary>
    public sealed class HiveDefensePerformanceTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const int FramesToSettle = 3;
        private const int WarmUpFrames = 120;
        private const int MeasuredFrames = 300;
        private const float FastTimeScale = 5f;
        private const float MaxWaveSeconds = 40f;
        private const int MaxFramesForWave = 300000;
        private const double NanosecondsPerMillisecond = 1000000.0;

        private HiveDefenseRoot _root;
        private float _savedTimeScale;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            _savedTimeScale = Time.timeScale;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = _savedTimeScale;
            yield return null;
        }

        private sealed class Sample
        {
            public readonly List<long> Allocations = new List<long>(MeasuredFrames);
            public double TotalMs;
            public double WorstMs;

            public long Median
            {
                get
                {
                    var sorted = new List<long>(Allocations);
                    sorted.Sort();
                    return sorted.Count == 0 ? -1L : sorted[sorted.Count / 2];
                }
            }

            public long Max => Allocations.Count == 0 ? -1L : System.Linq.Enumerable.Max(Allocations);
            public double AverageMs => Allocations.Count == 0 ? 0.0 : TotalMs / Allocations.Count;
        }

        private static IEnumerator Record(Sample sample, int frames)
        {
            using (ProfilerRecorder gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            using (ProfilerRecorder main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread"))
            {
                yield return null; // The recorders sample at the end of each frame; skip the partial first one.

                for (int frame = 0; frame < frames; frame++)
                {
                    yield return null;
                    if (!gc.Valid || !main.Valid)
                    {
                        continue;
                    }

                    sample.Allocations.Add(gc.LastValue);
                    double ms = main.LastValue / NanosecondsPerMillisecond;
                    sample.TotalMs += ms;
                    sample.WorstMs = System.Math.Max(sample.WorstMs, ms);
                }
            }
        }

        [UnityTest]
        public IEnumerator FullSwarm_PerFrame_AllocatesNoGarbageBeyondTheEditorFloor()
        {
            // Floor: an empty scene in the same Editor run. The Test Runner and the MCP plugin allocate a little every
            // frame, and that is not F-010 code.
            Scene empty = SceneManager.CreateScene("PerfBaseline");
            SceneManager.SetActiveScene(empty);
            for (int frame = 0; frame < WarmUpFrames; frame++)
            {
                yield return null;
            }

            var floor = new Sample();
            yield return Record(floor, MeasuredFrames);

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            Assert.That(_root, Is.Not.Null);

            // The capacity grows to its ceiling, as nests raise it, and wasps that arrive fill the swarm to it.
            _root.Session.Swarm.GrowCapacity(_root.Session.Swarm.Max);
            _root.Session.Swarm.Gain(_root.Session.Swarm.Max);
            Assert.That(_root.Session.Swarm.Count, Is.EqualTo(_root.Session.Swarm.Max));

            // The match starts in a farm phase with nobody on the map, so the wave is called at once.
            _root.CallWave();
            Time.timeScale = FastTimeScale;
            int frames = 0;
            float elapsed = 0f;
            while (_root.Session.ActiveBeekeeperCount < 1 && elapsed < MaxWaveSeconds && frames < MaxFramesForWave)
            {
                elapsed += Time.deltaTime;
                frames++;
                yield return null;
            }

            Assert.That(_root.Session.ActiveBeekeeperCount, Is.GreaterThanOrEqualTo(1), "The first wave must have started.");

            for (int frame = 0; frame < WarmUpFrames; frame++)
            {
                yield return null;
            }

            var whole = new Sample();
            yield return Record(whole, MeasuredFrames);
            Assert.That(whole.Allocations.Count, Is.GreaterThan(0), "The profiler counters were not available.");

            Debug.Log(
                $"[F010] perf: wasps {_root.Session.Swarm.Count}, beekeepers {_root.Session.ActiveBeekeeperCount}, " +
                $"main thread avg {whole.AverageMs:F3} ms, worst {whole.WorstMs:F3} ms, " +
                $"GC per frame median {whole.Median} B, max {whole.Max} B (empty-scene floor median {floor.Median} B)");

            // While any UIDocument is enabled, UI Toolkit's input provider asks the Input Manager for the "Next" and
            // "Previous" buttons every frame. The project defines neither, so each call throws and catches an exception:
            // 1216 B per frame inside the engine, in every scene with a UIDocument (Town too), none of it from F-010.
            // So the UIDocument is measured apart: everything but it must stay at the floor.
            foreach (UnityEngine.UIElements.UIDocument document in Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>())
            {
                document.enabled = false;
            }

            for (int frame = 0; frame < FramesToSettle * 2; frame++)
            {
                yield return null;
            }

            var withoutUi = new Sample();
            yield return Record(withoutUi, MeasuredFrames);
            Debug.Log(
                $"[F010] perf without UIDocument: GC per frame median {withoutUi.Median} B, max {withoutUi.Max} B, " +
                $"main thread avg {withoutUi.AverageMs:F3} ms");

            // Assert on medians: the max can be non-zero from one-off Editor allocations.
            Assert.That(withoutUi.Median, Is.LessThanOrEqualTo(floor.Median),
                "The swarm, beekeepers and views must not allocate garbage each frame.");
        }

        [UnityTest]
        public IEnumerator Diagnose_FirstStingCastFrame()
        {
            float savedCapture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int frame = 0; frame < 300; frame++)
            {
                yield return null;
            }

            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            Debug.Log($"[F010] diag warmup done {_root.ScriptWarmup.IsDone} methods {_root.ScriptWarmup.MethodCount} failed {_root.ScriptWarmup.FailedCount}");
            var handles = new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
            Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(handles);
            var names = new List<string>();
            var recorders = new List<ProfilerRecorder>();
            foreach (Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle handle in handles)
            {
                Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderDescription description = Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(handle);
                if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds)
                {
                    continue;
                }

                var recorder = new ProfilerRecorder(handle, 1, ProfilerRecorderOptions.SumAllSamplesInFrame | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                recorder.Start();
                names.Add(description.Category.Name + "/" + description.Name);
                recorders.Add(recorder);
            }

            int sting = -1;
            for (int song = 0; song < _root.Session.Caster.Count; song++)
            {
                if (_root.Session.Caster[song] is DigitalStory.Logic.StingSong)
                {
                    sting = song;
                }
            }

            yield return null;
            yield return null;
            var first = new long[recorders.Count];
            var second = new long[recorders.Count];
            var lines = new System.Text.StringBuilder();
            for (int cast = 0; cast < 2; cast++)
            {
                long[] into = cast == 0 ? first : second;
                int castFrame = -1;
                int frameIndex = 0;
                System.Action<int> onCast = _ => castFrame = frameIndex;
                _root.Session.SongCastNow += onCast;
                IReadOnlyList<int> keys = _root.Session.Caster[sting].Song.Keys;
                for (frameIndex = 0; frameIndex < 12; frameIndex++)
                {
                    if (frameIndex < keys.Count)
                    {
                        _root.PressHole(keys[frameIndex]);
                    }

                    yield return null;
                    long main = 0;
                    for (int index = 0; index < recorders.Count; index++)
                    {
                        long value = recorders[index].LastValue;
                        if (names[index] == "Internal/Main Thread")
                        {
                            main = value;
                        }

                        if (frameIndex >= keys.Count - 1 && frameIndex <= keys.Count + 3)
                        {
                            into[index] = System.Math.Max(into[index], value);
                        }
                    }

                    lines.Append($" {frameIndex}:{main / NanosecondsPerMillisecond:F2}{(castFrame == frameIndex ? "*" : string.Empty)}");
                }

                _root.Session.SongCastNow -= onCast;
                Debug.Log($"[F010] diag cast {cast} main thread per frame:{lines}");
                lines.Clear();
                for (int frame = 0; frame < 600; frame++)
                {
                    yield return null;
                }
            }

            var order = new List<int>();
            for (int index = 0; index < recorders.Count; index++)
            {
                order.Add(index);
            }

            order.Sort((a, b) => (first[b] - second[b]).CompareTo(first[a] - second[a]));
            var text = new System.Text.StringBuilder();
            for (int rank = 0; rank < 50 && rank < order.Count; rank++)
            {
                int index = order[rank];
                text.Append($"\n[F010] diag {names[index]}: first {first[index] / NanosecondsPerMillisecond:F3} ms, second {second[index] / NanosecondsPerMillisecond:F3} ms");
            }

            Debug.Log("[F010] diag top:" + text);
            foreach (ProfilerRecorder recorder in recorders)
            {
                recorder.Dispose();
            }

            Time.captureDeltaTime = savedCapture;
        }
    }
}
#endif
