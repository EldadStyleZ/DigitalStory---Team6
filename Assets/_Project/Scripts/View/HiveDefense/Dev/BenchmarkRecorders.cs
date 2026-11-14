using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Development tool, not game code. The profiler counters the benchmark reads each frame. A counter that does not
    /// exist in this player (most markers only exist in development builds) stays invalid and is written as an empty field.
    /// </summary>
    internal sealed class BenchmarkRecorders : IDisposable
    {
        private const int MostMarkers = 24;
        private const double NanosecondsPerMillisecond = 1000000.0;

        // Counters that release players have: the frame timings (they switch FrameTimingManager on by themselves) and
        // the rendering statistics. The memory counter and the Main Thread marker exist only in development players.
        private static readonly (string Column, ProfilerCategory Category, string Name)[] Fixed =
        {
            ("rec_cpu_total_ms", ProfilerCategory.Render, "CPU Total Frame Time"),
            ("rec_cpu_main_ms", ProfilerCategory.Render, "CPU Main Thread Frame Time"),
            ("rec_cpu_render_ms", ProfilerCategory.Render, "CPU Render Thread Frame Time"),
            ("rec_gpu_ms", ProfilerCategory.Render, "GPU Frame Time"),
            ("main_thread_ms", ProfilerCategory.Internal, "Main Thread"),
            ("gc_alloc_bytes", ProfilerCategory.Memory, "GC Allocated In Frame"),
            ("setpass", ProfilerCategory.Render, "SetPass Calls Count"),
            ("draw_calls", ProfilerCategory.Render, "Draw Calls Count"),
            ("batches", ProfilerCategory.Render, "Batches Count"),
            ("total_batches", ProfilerCategory.Render, "Total Batches Count"),
            ("triangles", ProfilerCategory.Render, "Triangles Count"),
            ("vertices", ProfilerCategory.Render, "Vertices Count"),
            ("shadow_casters", ProfilerCategory.Render, "Shadow Casters Count"),
            ("skinned_visible", ProfilerCategory.Render, "Visible Skinned Meshes Count"),

            // Counted in a release player too: a drop on a long frame shows the garbage collector ran in it.
            ("gc_used_bytes", ProfilerCategory.Memory, "GC Used Memory"),
        };

        // Markers worth a column in a development player: first-use work (shader programs, pipeline states), the big
        // blocks of a frame, and waits that show which side is the bottleneck.
        private static readonly string[] MarkerKeywords =
        {
            "CreateGPUProgram", "PipelineState", "CreatePipeline", "Shader.Parse", "CompileShader", "Shader.Compile",
            "Gfx.WaitForPresent", "Gfx.WaitForGfxCommandsFromMainThread", "Gfx.PresentFrame", "Gfx.WaitForRenderThread",
            "Camera.Render", "Shadows.RenderShadowMap", "Render.OpaqueGeometry", "Render.TransparentGeometry",
            "UIElementsUpdatePanels", "UIR.DrawChain", "Animators.Update", "GC.Collect", "Instantiate",
        };

        private readonly List<string> _columns = new List<string>();
        private readonly List<ProfilerRecorder> _recorders = new List<ProfilerRecorder>();
        private readonly List<bool> _isTime = new List<bool>();
        private readonly FrameTiming[] _timings = new FrameTiming[1];

        public BenchmarkRecorders(bool addsMarkers, string markerListPath)
        {
            foreach ((string column, ProfilerCategory category, string name) in Fixed)
            {
                Add(column, category, name);
            }

            if (addsMarkers)
            {
                AddMarkers(markerListPath);
            }
        }

        /// <summary>How many recorder columns there are.</summary>
        public int Count => _recorders.Count;

        /// <summary>FrameTimingManager's last finished frame (four frames back), in milliseconds; zero when it has none.</summary>
        public FrameTiming LatestTiming => _timings[0];

        /// <summary>The column name of a recorder.</summary>
        public string Column(int index) => _columns[index];

        /// <summary>True while the recorder is attached to its counter or marker.</summary>
        public bool IsValid(int index) => _recorders[index].Valid;

        /// <summary>The last frame's value: milliseconds for times, otherwise the raw count or bytes.</summary>
        public double Value(int index)
        {
            long raw = _recorders[index].LastValue;
            return _isTime[index] ? raw / NanosecondsPerMillisecond : raw;
        }

        /// <summary>Asks FrameTimingManager for the latest finished frame. Call once a frame.</summary>
        public void CaptureTiming()
        {
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _timings) == 0)
            {
                _timings[0] = default;
            }
        }

        /// <summary>One line per recorder: its column, whether it is attached and its unit, for the run's notes.</summary>
        public void Describe(TextWriter writer)
        {
            for (int index = 0; index < _recorders.Count; index++)
            {
                ProfilerRecorder recorder = _recorders[index];
                writer.WriteLine($"recorder {_columns[index]}: valid={recorder.Valid}, unit={(recorder.Valid ? recorder.UnitType.ToString() : "-")}");
            }
        }

        public void Dispose()
        {
            for (int index = 0; index < _recorders.Count; index++)
            {
                _recorders[index].Dispose();
            }

            _recorders.Clear();
        }

        private void Add(string column, ProfilerCategory category, string name)
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, name);
            _columns.Add(column);
            _recorders.Add(recorder);
            _isTime.Add(column.EndsWith("_ms", StringComparison.Ordinal) || (recorder.Valid && recorder.UnitType == ProfilerMarkerDataUnit.TimeNanoseconds));
        }

        // Lists every counter and marker this player knows to a file, and gives a column to those that match a keyword.
        private void AddMarkers(string markerListPath)
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            using (var list = new StreamWriter(markerListPath))
            {
                foreach (ProfilerRecorderHandle handle in handles)
                {
                    ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                    list.WriteLine($"{description.Category.Name}\t{description.Name}\t{description.UnitType}");
                    if (_recorders.Count >= Fixed.Length + MostMarkers || !seen.Add(description.Name) || !Matches(description.Name))
                    {
                        continue;
                    }

                    Add("m_" + Sanitize(description.Name) + "_ms", description.Category, description.Name);
                }
            }
        }

        private static bool Matches(string name)
        {
            foreach (string keyword in MarkerKeywords)
            {
                if (name.IndexOf(keyword, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Sanitize(string name)
        {
            char[] characters = name.ToCharArray();
            for (int index = 0; index < characters.Length; index++)
            {
                if (!char.IsLetterOrDigit(characters[index]))
                {
                    characters[index] = '_';
                }
            }

            return new string(characters);
        }
    }
}
