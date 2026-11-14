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

namespace DigitalStory.Tests.PlayMode
{
    /// <summary>
    /// Plan section S in the scene: when a match starts the root draws its map and puts everything on it. The tests' map
    /// comes from <see cref="F010MapSeedFixture.Seed"/>; without a fixed seed each match draws another. The hives, the
    /// nest trees and the spawn points stand where the map says, the scenery stands on the ground where it says or is
    /// hidden, and the chase camera's blocker boxes are measured where the props now stand. Plan section T: every ring,
    /// bar and light of a hive or nest stands on the match's map, not where the saved scene had it, on the first match and
    /// after a restart. With F010_SNAPSHOT_DIR set, <see cref="Snapshot19_ThreeMaps"/> and <see cref="Snapshot21_Maps"/>
    /// draw the arena from above for several seeds.
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

        // Plan section T: the arena reaches 42 m, so the view from above stands farther back.
        private const float WideOverviewDistance = 80f;
        private static readonly int[] WideSnapshotSeeds = { 7, 42, 303, 606 };
        private static readonly int[] GameViewSeeds = { F010MapSeedFixture.Seed, 7 };

        // Plan section T: the saved scene's map and the configs it was drawn with.
        private const string LayoutConfigPath = "Assets/_Project/GameData/F010/LayoutConfig.asset";
        private const string NestConfigPath = "Assets/_Project/GameData/F010/NestConfig.asset";
        private const float ClearlyElsewhereMetres = 8f;
        private const float OnTheSpotMetres = 0.05f;
        private const float LightReachMetres = 2f; // The hive's lamp hangs 1.6 m toward the camera.
        private const float PartReachMetres = 4f;
        private const int RestartSeed = 7;

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
        public IEnumerator FixtureMap_StandsClearlyAwayFromTheSavedScenesMap()
        {
            yield return LoadScene();
            ArenaLayout map = Root().Layout;
            ArenaLayout saved = SavedSceneMap();

            for (int index = 0; index < map.Hives.Count; index++)
            {
                Assert.That(Ground(map.Hives[index], saved.Hives[index]), Is.GreaterThan(ClearlyElsewhereMetres), $"hive {index} stands near where the saved scene has it");
            }

            for (int index = 0; index < map.Spawns.Count; index++)
            {
                Assert.That(Ground(map.Spawns[index], saved.Spawns[index]), Is.GreaterThan(ClearlyElsewhereMetres), $"spawn point {index}");
            }
        }

        // The bug the owner saw: the hives moved to each match's map, but their rings stayed behind.
        [UnityTest]
        public IEnumerator EveryHiveAndNestPart_StandsOnTheMatchsMap_AndAgainAfterARestart()
        {
            yield return LoadScene();
            CheckEveryPartOnTheMap(Root(), "first match");

            // A restart loads the scene again and draws another map.
            MatchSeed.Fixed = RestartSeed;
            yield return LoadScene();
            HiveDefenseRoot restarted = Root();
            Assert.That(restarted.Layout.Seed, Is.EqualTo(RestartSeed));
            CheckEveryPartOnTheMap(restarted, "after a restart");
        }

        // The reviewer's finding: the session's chance came from the scene's fixed seed (1010), so every match showed the same
        // nest combos whatever the map. It comes from the match's map seed now.
        [UnityTest]
        public IEnumerator NestCombos_ComeFromTheMatchsSeed_SoAnotherMapShowsOtherCombos()
        {
            yield return LoadScene();
            string first = CombosOf(Root());

            MatchSeed.Fixed = RestartSeed;
            yield return LoadScene();
            string second = CombosOf(Root());

            Assert.That(second, Is.Not.EqualTo(first), "a match on another map showed the same combos");
        }

        private static string CombosOf(HiveDefenseRoot root)
        {
            var text = new System.Text.StringBuilder();
            for (int nest = 0; nest < root.Session.Nests.Count; nest++)
            {
                foreach (int note in root.Session.Nests[nest].Combo)
                {
                    text.Append(note);
                }

                text.Append(' ');
            }

            return text.ToString();
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

        [UnityTest]
        [Category("F010Snapshot")]
        public IEnumerator Snapshot21_Maps()
        {
            string directory = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrEmpty(directory))
            {
                Assert.Ignore($"Set {DirectoryVariable} to a folder to write the F-010 snapshots.");
            }

            Directory.CreateDirectory(directory);
            Time.captureDeltaTime = FrameSeconds;
            foreach (int seed in WideSnapshotSeeds)
            {
                yield return CaptureMap(seed, Path.Combine(directory, $"snap-21-map-seed{seed}.png"), isFromAbove: true);
            }

            foreach (int seed in GameViewSeeds)
            {
                yield return CaptureMap(seed, Path.Combine(directory, $"snap-21-map-seed{seed}-opening.png"), isFromAbove: false);
            }
        }

        private static IEnumerator CaptureMap(int seed, string path, bool isFromAbove)
        {
            MatchSeed.Fixed = seed;
            yield return LoadScene();
            yield return WaitGameSeconds(SettleCameraSeconds);
            ArenaLayout map = Root().Layout;
            Debug.Log($"[F010] Snapshot 21 map seed {seed}: hives {map.Hives[0]} and {map.Hives[1]}, spawns {map.Spawns[0]} {map.Spawns[1]} {map.Spawns[2]} {map.Spawns[3]}, {Placed(map)} of {map.Scenery.Count} props, fallback {map.IsFallback}.");

            var capturer = new GameObject("Snapshot Capturer").AddComponent<SnapshotCapturer>();
            capturer.Initialize(Camera.main, Width, Height);
            bool wasFoggy = RenderSettings.fog;
            if (isFromAbove)
            {
                RenderSettings.fog = false;
                Quaternion rotation = Quaternion.Euler(OverviewPitch, 0f, 0f);
                capturer.RequestCapture(path, OverviewFocus - (rotation * Vector3.forward * WideOverviewDistance), rotation);
            }
            else
            {
                capturer.RequestCapture(path);
            }

            while (capturer.IsBusy)
            {
                yield return null;
            }

            RenderSettings.fog = wasFoggy;
        }

        // Every ring and bar a hive draws stands on its hive, its lamp within reach, and every part of a hive or a nest tree
        // within a few metres of where the map puts it.
        private static void CheckEveryPartOnTheMap(HiveDefenseRoot root, string which)
        {
            ArenaLayout map = root.Layout;
            HiveView[] hiveViews = UnityEngine.Object.FindObjectsByType<HiveView>();
            Assert.That(hiveViews.Length, Is.EqualTo(map.Hives.Count), which);
            foreach (HiveView view in hiveViews)
            {
                Vector3 hive = NearestOf(map.Hives, view.transform.position);
                string name = $"{which}: {view.name}";
                Assert.That(Ground(view.transform.position, hive), Is.LessThan(SamePlaceMetres), name);
                foreach (string part in new[] { "AttackRing", "CollectRing", "HealthBar" })
                {
                    Transform child = view.transform.Find(part);
                    Assert.That(child, Is.Not.Null, $"{name} has no {part}");
                    Assert.That(Ground(child.position, hive), Is.LessThan(OnTheSpotMetres), $"{name}'s {part} is not on its hive");
                }

                CheckParts(view.transform, hive, name);
            }

            NestView[] nestViews = UnityEngine.Object.FindObjectsByType<NestView>();
            Assert.That(nestViews.Length, Is.EqualTo(map.Nests.Count), which);
            foreach (NestView view in nestViews)
            {
                Vector3 nest = NearestOf(map.Nests, view.transform.position);
                string name = $"{which}: {view.name}";
                Assert.That(Ground(view.transform.position, nest), Is.LessThan(SamePlaceMetres), name);
                CheckParts(view.transform, nest, name);
            }
        }

        private static void CheckParts(Transform owner, Vector3 place, string name)
        {
            foreach (Light light in owner.GetComponentsInChildren<Light>(true))
            {
                Assert.That(Ground(light.transform.position, place), Is.LessThan(LightReachMetres), $"{name}'s light {light.name} is not by it");
            }

            foreach (Renderer part in owner.GetComponentsInChildren<Renderer>(true))
            {
                if (part.transform.parent != null && part.transform.parent.name.EndsWith("Motes", StringComparison.Ordinal))
                {
                    continue; // The motes fly between the hive or nest and the swarm.
                }

                Assert.That(Ground(part.transform.position, place), Is.LessThan(PartReachMetres), $"{name}'s {part.name} is not by it");
            }
        }

        private static Vector3 NearestOf(IReadOnlyList<Vector3> places, Vector3 position)
        {
            Vector3 nearest = places[0];
            foreach (Vector3 place in places)
            {
                nearest = Ground(place, position) < Ground(nearest, position) ? place : nearest;
            }

            return nearest;
        }

        // The map the scene builder saved into the scene: the hives, spawn points and nests do not depend on the scenery.
        private static ArenaLayout SavedSceneMap()
        {
            var layout = AssetDatabase.LoadAssetAtPath<LayoutConfig>(LayoutConfigPath);
            var nests = AssetDatabase.LoadAssetAtPath<NestConfig>(NestConfigPath);
            Assert.That(layout, Is.Not.Null, LayoutConfigPath);
            Assert.That(nests, Is.Not.Null, NestConfigPath);
            return LayoutGenerator.Generate(MatchSeed.SavedSceneSeed, Vector3.zero, layout, nests, new float[0]);
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
