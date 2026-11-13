#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// The first Sting of a match against the second. Mono compiles a method the first time it runs, so a cast whose code
    /// has never run pays for compiling it in its own frame; the root's warm-up (ScriptWarmup) compiles the game's code
    /// ahead, a little each frame, from the scene's start. This fixture's name sorts it before every other F-010 fixture
    /// that casts, so in a run of the F-010 PlayMode tests its Sting is the first of the process.
    /// </summary>
    /// <remarks>
    /// A frame's time is measured on the wall clock from one resume of this coroutine to the next, which spans the cast's
    /// Update and the LateUpdate and drawing after it. The Editor draws more slowly than a player, so only the difference
    /// between the first and the second cast counts.
    /// </remarks>
    public sealed class HiveDefenseColdCastTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const float FrameSeconds = 1f / 60f;
        private const int MostWarmupFrames = 20000;
        private const int SettleFrames = 30;
        private const int FramesAfterCast = 3;
        private const int MarkersReported = 10;
        private const double NanosecondsPerMillisecond = 1000000.0;
        private const float CooldownWaitSeconds = 2.5f;

        // Before the warm-up compiled for real (plan section Q), RuntimeHelpers.PrepareMethod took 0.03 µs a method under
        // Mono, which is no compiling at all, and the first Sting took 2.2 ms against 0.5 ms for the second in the Editor
        // (15.5 ms against 8.3 ms in a player). Compiling a method takes microseconds.
        private const double LeastMillisecondsPerMethod = 0.002;

        // A guard against a first cast that is far slower than the second; the Editor's own frames are noisy.
        private const double MostExtraMilliseconds = 3.0;

        private HiveDefenseRoot _root;
        private float _savedCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = FrameSeconds;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _root = UnityEngine.Object.FindAnyObjectByType<HiveDefenseRoot>();
            Assert.That(_root, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstSting_AfterTheWarmUpCompiledTheCode_IsNoSlowerThanTheSecond()
        {
            ScriptWarmup warmup = _root.ScriptWarmup;
            int warmupFrames = 0;
            while (!warmup.IsDone && warmupFrames < MostWarmupFrames)
            {
                warmupFrames++;
                yield return null;
            }

            for (int frame = 0; frame < SettleFrames; frame++)
            {
                yield return null;
            }

            // Every timed profiler marker, so the log says where a first cast's extra time goes.
            var markers = new MarkerRecorders();
            try
            {
                // A pass that plays nothing, so this test's own code has run once and compiled before the first cast.
                yield return CastSting(new CastTiming(markers.Count), markers, false);

                var first = new CastTiming(markers.Count);
                yield return CastSting(first, markers, true);
                float waited = 0f;
                while (waited < CooldownWaitSeconds)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                var second = new CastTiming(markers.Count);
                yield return CastSting(second, markers, true);

                Debug.Log(
                    $"[F010] cold cast: warm-up compiled {warmup.PreparedCount} methods ({warmup.FailedCount} failed) in {warmupFrames} frames " +
                    $"and {warmup.SpentMilliseconds:0.0} ms; first Sting {first.CastMilliseconds:0.00} ms against {first.TypicalMilliseconds:0.00} ms " +
                    $"around it; second {second.CastMilliseconds:0.00} ms against {second.TypicalMilliseconds:0.00} ms" +
                    markers.Slowest(first.Markers, second.Markers, MarkersReported));
                Check(warmup, first, second);
            }
            finally
            {
                markers.Dispose();
            }
        }

        private static void Check(ScriptWarmup warmup, CastTiming first, CastTiming second)
        {
            Assert.That(warmup.IsDone, Is.True, "The warm-up did not finish.");
            Assert.That(warmup.SpentMilliseconds / Math.Max(warmup.PreparedCount, 1), Is.GreaterThanOrEqualTo(LeastMillisecondsPerMethod),
                "The warm-up took no time per method, so it compiled nothing ahead.");
            Assert.That(first.WasCast && second.WasCast, Is.True, "A Sting was not cast.");
            Assert.That(first.CastMilliseconds, Is.LessThanOrEqualTo(second.CastMilliseconds + MostExtraMilliseconds),
                "The first Sting's frame was slower than the second's: its code compiled on the cast.");
        }

        // Plays the Sting's notes one a frame, or nothing for a dry pass, and times every frame from the first note until a
        // few after the cast; a dry pass counts its frames as if the cast had come on time.
        private IEnumerator CastSting(CastTiming timing, MarkerRecorders markers, bool isPlayed)
        {
            IReadOnlyList<int> keys = StingKeys();
            int castFrame = -1;
            Action<int> onCast = _ => castFrame = Time.frameCount;
            _root.Session.SongCastNow += onCast;
            try
            {
                double before = Time.realtimeSinceStartupAsDouble;
                int frames = keys.Count + 1 + FramesAfterCast;
                for (int frame = 0; frame < frames; frame++)
                {
                    if (isPlayed && frame < keys.Count)
                    {
                        _root.PressHole(keys[frame]);
                    }
                    else if (!isPlayed && frame == keys.Count)
                    {
                        castFrame = Time.frameCount;
                    }

                    yield return null;
                    double now = Time.realtimeSinceStartupAsDouble;
                    double milliseconds = (now - before) * 1000.0;
                    before = now;

                    // The cast's own frame and the one after it, whose LateUpdate draws what the cast began.
                    bool isCastFrame = castFrame >= 0 && Time.frameCount - castFrame <= 1;
                    timing.Add(milliseconds, isCastFrame);
                    if (castFrame >= 0 && Time.frameCount - castFrame <= FramesAfterCast)
                    {
                        markers.TakeMost(timing.Markers);
                    }
                }

                timing.WasCast = castFrame >= 0;
            }
            finally
            {
                _root.Session.SongCastNow -= onCast;
            }
        }

        private IReadOnlyList<int> StingKeys()
        {
            HiveDefenseSession session = _root.Session;
            for (int song = 0; song < session.Caster.Count; song++)
            {
                if (session.Caster[song] is StingSong)
                {
                    return session.Caster[song].Song.Keys;
                }
            }

            Assert.Fail("The scene has no Sting.");
            return null;
        }

        private sealed class CastTiming
        {
            private readonly List<double> _others = new List<double>();

            public CastTiming(int markerCount)
            {
                Markers = new long[markerCount];
            }

            // Each marker's longest frame from the cast on, in nanoseconds.
            public long[] Markers { get; }

            public bool WasCast { get; set; }

            public double CastMilliseconds { get; private set; }

            public double TypicalMilliseconds
            {
                get
                {
                    if (_others.Count == 0)
                    {
                        return 0.0;
                    }

                    _others.Sort();
                    return _others[_others.Count / 2];
                }
            }

            public void Add(double milliseconds, bool isCastFrame)
            {
                if (isCastFrame)
                {
                    CastMilliseconds = Math.Max(CastMilliseconds, milliseconds);
                }
                else
                {
                    _others.Add(milliseconds);
                }
            }
        }

        // Records every profiler marker timed in nanoseconds, a frame at a time.
        private sealed class MarkerRecorders : IDisposable
        {
            private readonly List<string> _names = new List<string>();
            private readonly List<ProfilerRecorder> _recorders = new List<ProfilerRecorder>();

            public MarkerRecorders()
            {
                var handles = new List<ProfilerRecorderHandle>();
                ProfilerRecorderHandle.GetAvailable(handles);
                foreach (ProfilerRecorderHandle handle in handles)
                {
                    ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                    if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds)
                    {
                        continue;
                    }

                    var recorder = new ProfilerRecorder(handle, 1, ProfilerRecorderOptions.SumAllSamplesInFrame | ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
                    recorder.Start();
                    _names.Add(description.Category.Name + "/" + description.Name);
                    _recorders.Add(recorder);
                }
            }

            public int Count => _recorders.Count;

            public void TakeMost(long[] into)
            {
                for (int index = 0; index < _recorders.Count; index++)
                {
                    into[index] = Math.Max(into[index], _recorders[index].LastValue);
                }
            }

            // The markers that took the most longer in the first cast than in the second.
            public string Slowest(long[] first, long[] second, int count)
            {
                var order = new List<int>();
                for (int index = 0; index < _recorders.Count; index++)
                {
                    order.Add(index);
                }

                order.Sort((a, b) => (first[b] - second[b]).CompareTo(first[a] - second[a]));
                var text = new System.Text.StringBuilder();
                for (int rank = 0; rank < count && rank < order.Count; rank++)
                {
                    int index = order[rank];
                    text.Append($"\n[F010] cold cast marker {_names[index]}: first {first[index] / NanosecondsPerMillisecond:0.000} ms, second {second[index] / NanosecondsPerMillisecond:0.000} ms");
                }

                return text.ToString();
            }

            public void Dispose()
            {
                foreach (ProfilerRecorder recorder in _recorders)
                {
                    recorder.Dispose();
                }
            }
        }
    }
}
#endif
