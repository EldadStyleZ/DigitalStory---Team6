#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DigitalStory.Data;
using DigitalStory.Logic;
using DigitalStory.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plan section X: the free swarm flows round the trees instead of through them. The core walks the river straight
    /// through a nest tree, once with the swarm flowing round its obstacles and once with none sent, reading every wasp back
    /// from the GPU each frame. Flowing, no wasp comes inside the tree's radius below its top, the wasps going by use both
    /// sides, and a little past the tree the stream is as wide again as it is without the tree. Without the bend, wasps fly
    /// through the tree, which shows the test can see it. It also times the swarm's GPU step with every obstacle and with
    /// none, at the largest swarm. A second test draws the river passing a tree and the eight beside one
    /// (snap-24-flow-*.png) when F010_SNAPSHOT_DIR names a folder.
    /// </summary>
    public sealed class HiveDefenseFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string SwarmConfigPath = "Assets/_Project/GameData/F010/SwarmConfig.asset";
        private const string CoreMoveConfigPath = "Assets/_Project/GameData/F010/CoreMoveConfig.asset";
        private const string ComputePath = "Assets/_Project/Art/F010/Shaders/WaspSwarm.compute";
        private const string DirectoryVariable = "F010_SNAPSHOT_DIR";
        private const float FrameSeconds = 1f / 60f;
        private const float SettleSeconds = 0.1f;
        private const float StandSeconds = 3f;

        // The walk: from StartBack metres before the tree to WalkEnd past it, on a line LineOffset to one side of its middle so
        // the core never walks dead on into a trunk. The corridor keeps Clearance metres of open ground from every other
        // obstacle and EdgeRoom from the play area's edge.
        private const float StartBack = 12f;
        private const float WalkEnd = 12f;
        private const float LineOffset = 0.25f;
        private const float Clearance = 3f;
        private const float EdgeRoom = 3f;
        private const int Directions = 16;
        private const float MostWalkSeconds = 10f;

        // The width is measured when the core is WidthAtCore metres past the tree, so the river runs from the core back
        // through the tree, in a slab SlabDepth deep that starts SlabStart past the closing distance.
        private const float WidthAtCore = 8f;
        private const float SlabStart = 0.5f;
        private const float SlabDepth = 1f;
        private const int LeastSlabWasps = 20;
        private const float SpreadLow = 0.1f;
        private const float SpreadHigh = 0.9f;
        private const float MostWidthRatio = 1.3f;
        private const float LeastWidthRatio = 0.7f;
        private const float WidthSlack = 0.15f;

        // A wasp goes by the tree while it is level with it (within its radius along the walk) and within BesideReach of its
        // edge across it. Each side must carry at least LeastSideShare of them.
        private const float BesideReach = 2f;
        private const float LeastSideShare = 0.25f;

        // The GPU step, timed over Dispatches steps of the largest swarm, Rounds times each way, after FillSeconds for the
        // new wasps to arrive. With every obstacle it may cost at most MostGpuRatio times the step with none plus GpuSlack.
        private const int Dispatches = 200;
        private const int Rounds = 3;
        private const float FillSeconds = 3f;
        private const double MostGpuRatio = 2.0;
        private const double GpuSlackMilliseconds = 0.2;

        // The strip: six pictures 0.15 s apart as the river passes the tree, from a steep camera beside the walk, and one of
        // the eight round the core standing EightBeside metres past the tree's edge.
        private const int StripFrames = 6;
        private const float StripSpacingSeconds = 0.15f;
        private const float StripStartPast = 1f;
        private const int StripWidth = 960;
        private const int StripHeight = 540;
        private const int SheetColumns = 3;
        private const float StripDistance = 17f;
        private const float StripPitch = 62f;
        private const float EightBeside = 0.8f;
        private const float EightStandSeconds = 4f;

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
        public IEnumerator RiverThroughATree_PartsRoundIt_UsesBothSides_AndClosesBehindIt()
        {
            var swarmConfig = AssetDatabase.LoadAssetAtPath<SwarmConfig>(SwarmConfigPath);
            Assert.That(swarmConfig, Is.Not.Null);

            var free = new Walk(false);
            yield return Run(free, swarmConfig, false);
            var flowing = new Walk(true);
            yield return Run(flowing, swarmConfig, true);

            Debug.Log($"[F010] flow: tree at ({flowing.Tree.x:0.0}, {flowing.Tree.y:0.0}) radius {flowing.Tree.z:0.00} m, top {flowing.Tree.w:0.0} m, " +
                      $"{flowing.ObstacleCount} obstacles\n{free.Report()}\n{flowing.Report()}");

            Assert.That(free.InsideCount, Is.GreaterThan(0), "Without the bend the river must fly through the tree, or the walk misses it.");
            Assert.That(flowing.InsideCount, Is.EqualTo(0),
                $"Wasps came inside the tree's radius below its top {flowing.InsideCount} times, the deepest {flowing.DeepestInside:0.00} m in.");
            int beside = flowing.LeftCount + flowing.RightCount;
            Assert.That(beside, Is.GreaterThan(0), "No wasp went by the tree.");
            float leftShare = flowing.LeftCount / (float)beside;
            Assert.That(leftShare, Is.InRange(LeastSideShare, 1f - LeastSideShare),
                $"The stream must part to both sides: {flowing.LeftCount} went left and {flowing.RightCount} right.");
            Assert.That(free.SlabCount, Is.GreaterThanOrEqualTo(LeastSlabWasps), "Too few wasps in the slab without the tree.");
            Assert.That(flowing.SlabCount, Is.GreaterThanOrEqualTo(LeastSlabWasps), "Too few wasps in the slab past the tree.");
            Assert.That(flowing.Width, Is.LessThanOrEqualTo((free.Width * MostWidthRatio) + WidthSlack),
                $"Past the tree the stream must close again: {flowing.Width:0.00} m wide against {free.Width:0.00} m without it.");
            Assert.That(flowing.Width, Is.GreaterThanOrEqualTo((free.Width * LeastWidthRatio) - WidthSlack),
                $"Past the tree the stream must be as wide as before: {flowing.Width:0.00} m against {free.Width:0.00} m without it.");

            // The largest swarm, timed on the GPU with every obstacle and with none.
            HiveDefenseSession session = _root.Session;
            session.Swarm.GrowCapacity(session.Swarm.Max);
            session.Swarm.Gain(session.Swarm.Max);
            yield return Frames(FillSeconds);
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
            Assert.That(compute, Is.Not.Null);
            var saved = new Vector4[SwarmObstacles.MostObstacles];
            int savedCount = _swarmView.ObstacleCount;
            for (int index = 0; index < savedCount; index++)
            {
                saved[index] = _swarmView.Obstacle(index);
            }

            double withAll = double.MaxValue;
            double withNone = double.MaxValue;
            for (int round = 0; round < Rounds; round++)
            {
                _swarmView.SetObstacles(saved, savedCount);
                withAll = Math.Min(withAll, TimeSteps(compute, swarmConfig.MaxWasps));
                _swarmView.SetObstacles(saved, 0);
                withNone = Math.Min(withNone, TimeSteps(compute, swarmConfig.MaxWasps));
            }

            _swarmView.SetObstacles(saved, savedCount);
            Debug.Log($"[F010] flow GPU step, {session.Swarm.Count} wasps: {withAll:0.000} ms with {savedCount} obstacles, {withNone:0.000} ms with none");
            Assert.That(withAll, Is.LessThanOrEqualTo((withNone * MostGpuRatio) + GpuSlackMilliseconds),
                $"Flowing round the obstacles costs too much on the GPU: {withAll:0.000} ms against {withNone:0.000} ms.");
        }

        [UnityTest]
        public IEnumerator Flow_RiverPastATreeAndTheEightBesideOne_DrawsTheStrip()
        {
            string directory = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrEmpty(directory))
            {
                Assert.Ignore($"Set {DirectoryVariable} to a folder to write the F-010 snapshots.");
            }

            Directory.CreateDirectory(directory);
            yield return Load();
            Corridor corridor = FindCorridor();
            Camera camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            Assert.That(camera, Is.Not.Null);
            var capturer = new GameObject("Flow Capturer").AddComponent<SnapshotCapturer>();
            capturer.Initialize(camera, StripWidth, StripHeight);

            _core.PlaceAt(corridor.Start, true);
            yield return Frames(StandSeconds);
            while (Ahead(corridor, _core.Position) < StripStartPast)
            {
                _core.Move(corridor.Direction, 1f, false, Time.deltaTime);
                yield return null;
            }

            var paths = new List<string>();
            Vector3 middle = new Vector3(corridor.Tree.x, 0f, corridor.Tree.y);
            Vector3 view = (corridor.Across * Mathf.Cos(StripPitch * Mathf.Deg2Rad)) + (Vector3.down * Mathf.Sin(StripPitch * Mathf.Deg2Rad));
            Quaternion rotation = Quaternion.LookRotation(view, corridor.Direction);
            Vector3 position = middle - (view * StripDistance);
            int spacing = Mathf.RoundToInt(StripSpacingSeconds / Time.captureDeltaTime);
            for (int picture = 0; picture < StripFrames; picture++)
            {
                string path = Path.Combine(directory, $"snap-24-flow-river-{picture}.png");
                paths.Add(path);
                capturer.RequestCapture(path, position, rotation);
                for (int frame = 0; frame < spacing; frame++)
                {
                    _core.Move(corridor.Direction, 1f, false, Time.deltaTime);
                    yield return null;
                }

                Assert.That(capturer.IsBusy, Is.False, $"{path} was not written.");
                Assert.That(capturer.LastWasFlat, Is.False, $"{path} is a single flat colour.");
            }

            Sheet(paths, Path.Combine(directory, "snap-24-flow-river-sheet.png"));

            // The eight round the core standing just past the tree's edge, so one lobe reaches over the tree.
            float flowRadius = corridor.Tree.z + AssetDatabase.LoadAssetAtPath<SwarmConfig>(SwarmConfigPath).ObstacleMargin;
            _core.PlaceAt(middle + (corridor.Direction * (flowRadius + EightBeside)), true);
            yield return Frames(EightStandSeconds);
            string eightPath = Path.Combine(directory, "snap-24-flow-eight.png");
            capturer.RequestCapture(eightPath, position, rotation);
            yield return null;
            yield return null;
            Assert.That(capturer.IsBusy, Is.False, $"{eightPath} was not written.");
            Debug.Log($"[F010] snapshot {eightPath}");
        }

        private IEnumerator Run(Walk walk, SwarmConfig swarmConfig, bool isFlowing)
        {
            yield return Load();
            Corridor corridor = FindCorridor();
            walk.Tree = corridor.Tree;
            walk.ObstacleCount = _swarmView.ObstacleCount;
            if (!isFlowing)
            {
                _swarmView.SetObstacles(new Vector4[0], 0);
            }

            float flowRadius = corridor.Tree.z + swarmConfig.ObstacleMargin;
            float slabNear = flowRadius + swarmConfig.ObstacleCloseMeters + SlabStart;
            GraphicsBuffer buffer = _swarmView.WaspStates;
            Assert.That(buffer, Is.Not.Null, "The swarm has no GPU buffer to read.");
            int living = _root.Session.Swarm.Count;
            var states = new WaspState[living];
            var slab = new List<float>();

            _core.PlaceAt(corridor.Start, true);
            yield return Frames(StandSeconds);

            bool hasMeasuredWidth = false;
            int frames = Mathf.RoundToInt(MostWalkSeconds / FrameSeconds);
            for (int frame = 0; frame < frames && Ahead(corridor, _core.Position) < WalkEnd; frame++)
            {
                _core.Move(corridor.Direction, 1f, false, Time.deltaTime);
                yield return null;

                buffer.GetData(states, 0, 0, living);
                bool isWidthFrame = !hasMeasuredWidth && Ahead(corridor, _core.Position) >= WidthAtCore;
                for (int index = 0; index < living; index++)
                {
                    WaspState state = states[index];
                    if (state.State > 0.5f)
                    {
                        continue;
                    }

                    Vector2 offset = new Vector2(state.Position.x - corridor.Tree.x, state.Position.z - corridor.Tree.y);
                    float ahead = Vector2.Dot(offset, new Vector2(corridor.Direction.x, corridor.Direction.z));
                    float aside = Vector2.Dot(offset, new Vector2(corridor.Across.x, corridor.Across.z));
                    if (state.Position.y < corridor.Tree.w)
                    {
                        float reach = offset.magnitude;
                        if (reach < corridor.Tree.z)
                        {
                            walk.InsideCount++;
                            float coreAhead = Ahead(corridor, _core.Position);
                            walk.CountInsideAt(coreAhead);
                            if (corridor.Tree.z - reach > walk.DeepestInside)
                            {
                                walk.DeepestInside = corridor.Tree.z - reach;
                                walk.DeepestNote = $"wasp {index} at frame {frame}, core {coreAhead:0.0} m past, wasp {ahead:0.00} ahead {aside:0.00} aside " +
                                                   $"{state.Position.y:0.00} up, speed {new Vector2(state.Velocity.x, state.Velocity.z).magnitude:0.0} m/s";
                            }
                        }

                        if (Mathf.Abs(ahead) <= corridor.Tree.z && Mathf.Abs(aside) <= flowRadius + BesideReach)
                        {
                            if (aside < 0f)
                            {
                                walk.LeftCount++;
                            }
                            else
                            {
                                walk.RightCount++;
                            }
                        }
                    }

                    if (isWidthFrame && ahead >= slabNear && ahead <= slabNear + SlabDepth)
                    {
                        slab.Add(aside);
                    }
                }

                if (isWidthFrame)
                {
                    hasMeasuredWidth = true;
                    walk.SlabCount = slab.Count;
                    walk.Width = Percentile(slab, SpreadHigh) - Percentile(slab, SpreadLow);
                }
            }

            Assert.That(hasMeasuredWidth, Is.True, "The core never walked far enough past the tree.");
        }

        private IEnumerator Load()
        {
            Time.captureDeltaTime = FrameSeconds;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Frames(SettleSeconds);
            _root = Object.FindAnyObjectByType<HiveDefenseRoot>();
            _core = Object.FindAnyObjectByType<SwarmCoreMotor>();
            _swarmView = Object.FindAnyObjectByType<SwarmGpuView>();
            Assert.That(_root, Is.Not.Null);
            Assert.That(_core, Is.Not.Null);
            Assert.That(_swarmView, Is.Not.Null);
            Assert.That(_swarmView.IsRunning, Is.True, "The swarm's GPU view must run to be measured.");
            Assert.That(_swarmView.ObstacleCount, Is.GreaterThan(0), "The match sent the swarm no obstacles.");
        }

        private static IEnumerator Frames(float seconds)
        {
            int frames = Mathf.Max(1, Mathf.RoundToInt(seconds / Time.captureDeltaTime));
            for (int frame = 0; frame < frames; frame++)
            {
                yield return null;
            }
        }

        // A nest tree with a straight walk through it, clear of every other obstacle and inside the play area.
        private Corridor FindCorridor()
        {
            var coreMove = AssetDatabase.LoadAssetAtPath<CoreMoveConfig>(CoreMoveConfigPath);
            Assert.That(coreMove, Is.Not.Null);
            float edge = coreMove.PlayAreaHalfSize - EdgeRoom;
            NestField nests = _root.Session.Nests;
            for (int nest = 0; nest < nests.Count; nest++)
            {
                int tree = NearestObstacle(nests[nest].Position);
                if (tree < 0)
                {
                    continue;
                }

                Vector4 obstacle = _swarmView.Obstacle(tree);
                var middle = new Vector3(obstacle.x, 0f, obstacle.y);
                for (int turn = 0; turn < Directions; turn++)
                {
                    float angle = turn * (360f / Directions);
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    Vector3 across = Vector3.Cross(Vector3.up, direction);
                    Vector3 start = middle - (direction * StartBack) + (across * LineOffset);
                    Vector3 end = middle + (direction * WalkEnd) + (across * LineOffset);
                    if (!IsInside(start, edge) || !IsInside(end, edge) || !IsClear(tree, start, end))
                    {
                        continue;
                    }

                    start.y = _core.Position.y;
                    return new Corridor { Tree = obstacle, Start = start, Direction = direction, Across = across };
                }
            }

            Assert.Fail("No nest tree has a clear straight walk through it on this map.");
            return default;
        }

        private int NearestObstacle(Vector3 position)
        {
            int nearest = -1;
            float best = float.MaxValue;
            for (int index = 0; index < _swarmView.ObstacleCount; index++)
            {
                Vector4 obstacle = _swarmView.Obstacle(index);
                float reach = new Vector2(obstacle.x - position.x, obstacle.y - position.z).magnitude;
                if (reach < best && reach < obstacle.z + 1f)
                {
                    best = reach;
                    nearest = index;
                }
            }

            return nearest;
        }

        private static bool IsInside(Vector3 point, float edge)
        {
            return Mathf.Abs(point.x) <= edge && Mathf.Abs(point.z) <= edge;
        }

        private bool IsClear(int tree, Vector3 start, Vector3 end)
        {
            var from = new Vector2(start.x, start.z);
            var to = new Vector2(end.x, end.z);
            for (int index = 0; index < _swarmView.ObstacleCount; index++)
            {
                if (index == tree)
                {
                    continue;
                }

                Vector4 obstacle = _swarmView.Obstacle(index);
                if (DistanceToSegment(new Vector2(obstacle.x, obstacle.y), from, to) < obstacle.z + Clearance)
                {
                    return false;
                }
            }

            return true;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 line = to - from;
            float along = Mathf.Clamp01(Vector2.Dot(point - from, line) / Mathf.Max(line.sqrMagnitude, 0.0001f));
            return (point - (from + (line * along))).magnitude;
        }

        private static float Ahead(Corridor corridor, Vector3 position)
        {
            return ((position.x - corridor.Tree.x) * corridor.Direction.x) + ((position.z - corridor.Tree.y) * corridor.Direction.z);
        }

        private static float Percentile(List<float> values, float share)
        {
            if (values.Count == 0)
            {
                return 0f;
            }

            var sorted = new List<float>(values);
            sorted.Sort();
            int index = Mathf.Clamp(Mathf.RoundToInt(share * (sorted.Count - 1)), 0, sorted.Count - 1);
            return sorted[index];
        }

        // Milliseconds per swarm step on the GPU: Dispatches steps back to back, waited for by a one-wasp read before and after.
        private double TimeSteps(ComputeShader compute, int maxWasps)
        {
            int kernel = compute.FindKernel("Simulate");
            int groups = (maxWasps + 63) / 64;
            var probe = new WaspState[1];
            GraphicsBuffer buffer = _swarmView.WaspStates;
            buffer.GetData(probe, 0, 0, 1);
            Stopwatch watch = Stopwatch.StartNew();
            for (int step = 0; step < Dispatches; step++)
            {
                compute.Dispatch(kernel, groups, 1, 1);
            }

            buffer.GetData(probe, 0, 0, 1);
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds / Dispatches;
        }

        // Puts the pictures side by side at half size, three to a row.
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
                Object.Destroy(sheet);
                Object.Destroy(picture);
            }
        }

        // 48 bytes, the same layout as WaspDynamic in WaspSwarm.compute.
        [StructLayout(LayoutKind.Sequential)]
        private struct WaspState
        {
            public Vector3 Position;
            public float State;
            public Vector3 Velocity;
            public float StateTime;
            public Vector4 Tint;
        }

        private struct Corridor
        {
            public Vector4 Tree;
            public Vector3 Start;
            public Vector3 Direction;
            public Vector3 Across;
        }

        private sealed class Walk
        {
            private readonly bool _isFlowing;

            public Walk(bool isFlowing)
            {
                _isFlowing = isFlowing;
            }

            public Vector4 Tree { get; set; }

            public int ObstacleCount { get; set; }

            public int InsideCount { get; set; }

            public float DeepestInside { get; set; }

            public int LeftCount { get; set; }

            public int RightCount { get; set; }

            public int SlabCount { get; set; }

            public float Width { get; set; }

            public string DeepestNote { get; set; } = string.Empty;

            private readonly int[] _insideByCore = new int[12];

            // Inside counts by where the core was, in 2 m steps from 8 m before the tree.
            public void CountInsideAt(float coreAhead)
            {
                int bucket = Mathf.Clamp(Mathf.FloorToInt((coreAhead + 8f) / 2f), 0, _insideByCore.Length - 1);
                _insideByCore[bucket]++;
            }

            public string Report()
            {
                string name = _isFlowing ? "flowing" : "no bend";
                return $"[F010] flow {name}: {InsideCount} wasp-frames inside the tree (deepest {DeepestInside:0.00} m), " +
                       $"beside it {LeftCount} left and {RightCount} right, width past it {Width:0.00} m from {SlabCount} wasps; " +
                       $"inside by core place from -8 m in 2 m steps: {string.Join(" ", _insideByCore)}; deepest: {DeepestNote}";
            }
        }
    }
}
#endif
