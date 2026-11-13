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

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plan section S in the scene: when a match starts the root draws its map and puts everything on it. The tests' map
    /// comes from <see cref="F010MapSeedFixture.Seed"/>; without a fixed seed each match draws another. The hives, the
    /// nest trees and the spawn points stand where the map says, the scenery stands on the ground where it says or is
    /// hidden, and the chase camera's blocker boxes are measured where the props now stand. With F010_SNAPSHOT_DIR set,
    /// <see cref="Snapshot19_ThreeMaps"/> draws the arena from above for three seeds.
    /// </summary>
    public sealed class HiveDefenseLayoutTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/eldad-lab-2-scene.unity";
        private const string DirectoryVariable = "F010_SNAPSHOT_DIR";
        private const int FramesToSettle = 3;
        private const float SamePlaceMetres = 0.01f;
        private const float GroundToleranceMetres = 0.05f;
        private const float BlockerLeastHeight = 2f;
        private const float NestCrownHeight = 3f;
        private const int Width = 960;
        private const int Height = 540;
        private const float FrameSeconds = 1f / 30f;
        private const float SettleCameraSeconds = 1f;

        // The whole arena from the south, steep enough to see between the props and low enough to see them stand.
        private const float OverviewDistance = 62f;
        private const float OverviewPitch = 58f;
        private static readonly Vector3 OverviewFocus = new Vector3(0f, 0f, -4f);
        private static readonly int[] SnapshotSeeds = { 101, 202, 303 };

        private float _savedCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            _savedCaptureDeltaTime = Time.captureDeltaTime;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            MatchSeed.Fixed = F010MapSeedFixture.Seed;
            Time.captureDeltaTime = _savedCaptureDeltaTime;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Match_UnderTheTests_DrawsItsMapFromTheFixedSeed()
        {
            yield return LoadScene();

            ArenaLayout map = Root().Layout;
            Assert.That(map.Seed, Is.EqualTo(F010MapSeedFixture.Seed), "the tests' fixed seed did not reach the match");
            Assert.That(map.IsFallback, Is.False);
        }

        [UnityTest]
        public IEnumerator Match_WithoutAFixedSeed_DrawsANewMapEachTime()
        {
            MatchSeed.Fixed = null;
            yield return LoadScene();
            ArenaLayout first = Root().Layout;
            yield return LoadScene();
            ArenaLayout second = Root().Layout;

            Assert.That(second.Seed, Is.Not.EqualTo(first.Seed));
            Assert.That(second.Hives[0], Is.Not.EqualTo(first.Hives[0]), "the restarted match stands its hives where the last did");
        }

        [UnityTest]
        public IEnumerator HivesNestsAndSpawnPoints_StandWhereTheMapPutsThem()
        {
            yield return LoadScene();
            HiveDefenseRoot root = Root();
            ArenaLayout map = root.Layout;

            for (int index = 0; index < map.Hives.Count; index++)
            {
                Assert.That(Ground(root.Session.Hives[index].Position, map.Hives[index]), Is.LessThan(SamePlaceMetres), $"hive {index}'s rules");
                Assert.That(Ground(NearestHiveView(map.Hives[index]), map.Hives[index]), Is.LessThan(SamePlaceMetres), $"hive {index}'s view");
            }

            for (int index = 0; index < map.Nests.Count; index++)
            {
                Assert.That(Ground(root.Session.Nests[index].Position, map.Nests[index]), Is.LessThan(SamePlaceMetres), $"nest {index}'s rules");
                Assert.That(Ground(NearestNestView(map.Nests[index]), map.Nests[index]), Is.LessThan(SamePlaceMetres), $"nest {index}'s tree");
            }

            Transform spawns = GameObject.Find("Spawn Points").transform;
            Assert.That(spawns.childCount, Is.EqualTo(map.Spawns.Count));
            for (int index = 0; index < map.Spawns.Count; index++)
            {
                Assert.That(Ground(spawns.GetChild(index).position, map.Spawns[index]), Is.LessThan(SamePlaceMetres), $"spawn point {index}");
            }
        }

        [UnityTest]
        public IEnumerator Scenery_StandsOnTheGroundWhereTheMapPutsIt_AndBlocksTheChaseCameraThere()
        {
            yield return LoadScene();
            ArenaLayout map = Root().Layout;
            SceneryView scenery = UnityEngine.Object.FindAnyObjectByType<SceneryView>();
            IReadOnlyList<Bounds> blockers = UnityEngine.Object.FindAnyObjectByType<FollowCamera>().ChaseBlockers;
            Assert.That(scenery, Is.Not.Null, "the scene has no SceneryView");
            Assert.That(scenery.transform.childCount, Is.EqualTo(map.Scenery.Count));

            for (int index = 0; index < map.Scenery.Count; index++)
            {
                Transform prop = scenery.transform.GetChild(index);
                SceneryPlacement placement = map.Scenery[index];
                Assert.That(prop.gameObject.activeSelf, Is.EqualTo(placement.IsPlaced), $"prop {index} {prop.name}");
                if (!placement.IsPlaced)
                {
                    continue;
                }

                Bounds box = RendererBounds(prop);
                Assert.That(Ground(prop.position, placement.Position), Is.LessThan(SamePlaceMetres), $"prop {index} {prop.name} is not where the map put it");
                Assert.That(box.min.y, Is.EqualTo(0f).Within(GroundToleranceMetres), $"prop {index} {prop.name} floats or sinks");
                if (box.size.y >= BlockerLeastHeight)
                {
                    Assert.That(IsInsideAny(blockers, box.center), Is.True, $"prop {index} {prop.name} does not block the chase camera where it stands");
                }
            }

            foreach (Vector3 nest in map.Nests)
            {
                Assert.That(IsInsideAny(blockers, nest + (Vector3.up * NestCrownHeight)), Is.True, $"the nest tree at {nest} does not block the chase camera");
            }
        }

        [UnityTest]
        [Category("F010Snapshot")]
        public IEnumerator Snapshot19_ThreeMaps()
        {
            string directory = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrEmpty(directory))
            {
                Assert.Ignore($"Set {DirectoryVariable} to a folder to write the F-010 snapshots.");
            }

            Directory.CreateDirectory(directory);
            Time.captureDeltaTime = FrameSeconds;
            foreach (int seed in SnapshotSeeds)
            {
                MatchSeed.Fixed = seed;
                yield return LoadScene();
                yield return WaitGameSeconds(SettleCameraSeconds);
                ArenaLayout map = Root().Layout;
                Debug.Log($"[F010] Snapshot 19 map seed {seed}: hives {map.Hives[0]} and {map.Hives[1]}, {Placed(map)} of {map.Scenery.Count} props, fallback {map.IsFallback}.");

                Camera camera = Camera.main;
                var capturer = new GameObject("Snapshot Capturer").AddComponent<SnapshotCapturer>();
                capturer.Initialize(camera, Width, Height);

                // The fog hides the far edge from this height; the game's own view below keeps it.
                bool wasFoggy = RenderSettings.fog;
                RenderSettings.fog = false;
                Quaternion rotation = Quaternion.Euler(OverviewPitch, 0f, 0f);
                Vector3 position = OverviewFocus - (rotation * Vector3.forward * OverviewDistance);
                capturer.RequestCapture(Path.Combine(directory, $"snap-19-layout-seed{seed}.png"), position, rotation);
                while (capturer.IsBusy)
                {
                    yield return null;
                }

                RenderSettings.fog = wasFoggy;
                capturer.RequestCapture(Path.Combine(directory, $"snap-19-layout-seed{seed}-game.png"));
                while (capturer.IsBusy)
                {
                    yield return null;
                }
            }
        }

        private static IEnumerator LoadScene()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            for (int frame = 0; frame < FramesToSettle; frame++)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        private static HiveDefenseRoot Root()
        {
            HiveDefenseRoot root = UnityEngine.Object.FindAnyObjectByType<HiveDefenseRoot>();
            Assert.That(root, Is.Not.Null, "the scene has no HiveDefenseRoot");
            Assert.That(root.Layout, Is.Not.Null, "the root drew no map");
            return root;
        }

        private static Vector3 NearestHiveView(Vector3 place)
        {
            Vector3 nearest = Vector3.positiveInfinity;
            foreach (HiveView view in UnityEngine.Object.FindObjectsByType<HiveView>())
            {
                nearest = Nearer(view.transform.position, nearest, place);
            }

            return nearest;
        }

        private static Vector3 NearestNestView(Vector3 place)
        {
            Vector3 nearest = Vector3.positiveInfinity;
            foreach (NestView view in UnityEngine.Object.FindObjectsByType<NestView>())
            {
                nearest = Nearer(view.transform.position, nearest, place);
            }

            return nearest;
        }

        private static Vector3 Nearer(Vector3 candidate, Vector3 best, Vector3 place)
        {
            return Ground(candidate, place) < Ground(best, place) ? candidate : best;
        }

        private static Bounds RendererBounds(Transform prop)
        {
            Renderer[] renderers = prop.GetComponentsInChildren<Renderer>();
            Assert.That(renderers.Length, Is.GreaterThan(0), $"{prop.name} has no renderer");
            Bounds box = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                box.Encapsulate(renderers[index].bounds);
            }

            return box;
        }

        private static bool IsInsideAny(IReadOnlyList<Bounds> blockers, Vector3 point)
        {
            for (int index = 0; index < blockers.Count; index++)
            {
                if (blockers[index].Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        private static int Placed(ArenaLayout map)
        {
            int placed = 0;
            foreach (SceneryPlacement prop in map.Scenery)
            {
                placed += prop.IsPlaced ? 1 : 0;
            }

            return placed;
        }

        private static float Ground(Vector3 a, Vector3 b)
        {
            if (float.IsInfinity(a.x) || float.IsInfinity(b.x))
            {
                return float.PositiveInfinity;
            }

            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }
    }
}
#endif
