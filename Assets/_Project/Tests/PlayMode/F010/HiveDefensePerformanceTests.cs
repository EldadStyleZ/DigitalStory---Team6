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
    /// Measures the per-frame cost of the Wasp Piper at the maximum swarm: garbage allocated and main thread time.
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
        private const float GainSeconds = 1000f;
        private const int StandingHives = 2;
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

            // Hive Call is the public way to add wasps: one big step fills the swarm to its maximum.
            _root.Session.Swarm.Gain(GainSeconds, StandingHives);
            Assert.That(_root.Session.Swarm.Count, Is.EqualTo(_root.Session.Swarm.Max));

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
    }
}
#endif
