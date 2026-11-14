using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.GrassBrush.Editor
{
    /// <summary>
    /// Lays the scenery out as a place rather than a scatter. The ruins and shacks stand in a broken ring around the arena,
    /// turned to face its middle like what is left of an old village wall. The trees grow in groves between and beyond them,
    /// thicker toward the outside, and the bushes gather at the feet of the trees and the ruins. Nothing lands on a hive, a
    /// nest, a spawn point or the arena's open middle, and everything sits on the terrain.
    /// Open it from Tools > DigitalStory > Scatter Scenery. One Ctrl/Cmd + Z undoes a run.
    /// </summary>
    public sealed class ScatterSceneryWindow : EditorWindow
    {
        private Transform _sceneryRoot;
        private Terrain _terrain;
        private int _seed = 11;
        private Vector2 _centre = Vector2.zero;
        private float _openMiddle = 14f;
        private float _ringRadius = 46f;
        private float _ringGaps = 0.25f;
        private float _groveReach = 75f;
        private float _keepClear = 6f;
        private float _scaleVariation = 0.2f;

        [MenuItem("Tools/DigitalStory/Scatter Scenery")]
        private static void Open()
        {
            GetWindow<ScatterSceneryWindow>("Scatter Scenery");
        }

        private void OnEnable()
        {
            if (_sceneryRoot == null)
            {
                var scenery = GameObject.Find("Scenery");
                _sceneryRoot = scenery != null ? scenery.transform : null;
            }

            if (_terrain == null)
            {
                _terrain = Terrain.activeTerrain;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            _sceneryRoot = (Transform)EditorGUILayout.ObjectField("Scenery Root", _sceneryRoot, typeof(Transform), true);
            _terrain = (Terrain)EditorGUILayout.ObjectField("Terrain (optional)", _terrain, typeof(Terrain), true);

            EditorGUILayout.Space();
            _centre = EditorGUILayout.Vector2Field("Arena Centre (X, Z)", _centre);
            _openMiddle = EditorGUILayout.Slider(new GUIContent("Open Middle", "Nothing is placed this close to the centre."), _openMiddle, 0f, 60f);
            _ringRadius = EditorGUILayout.Slider(new GUIContent("Ruin Ring Radius", "Where the ring of ruins and shacks stands."), _ringRadius, 10f, 150f);
            _ringGaps = EditorGUILayout.Slider(new GUIContent("Ring Gaps", "How broken the ring is: wider gaps and more wobble."), _ringGaps, 0f, 1f);
            _groveReach = EditorGUILayout.Slider(new GUIContent("Grove Reach", "How far out the tree groves go."), _groveReach, 15f, 250f);
            _keepClear = EditorGUILayout.Slider(new GUIContent("Keep Clear Of Gameplay", "Metres kept free around hives, nests and spawn points."), _keepClear, 0f, 20f);
            _scaleVariation = EditorGUILayout.Slider("Size Variation", _scaleVariation, 0f, 0.6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                _seed = EditorGUILayout.IntField("Seed", _seed);
                if (GUILayout.Button("New", GUILayout.Width(50)))
                {
                    _seed = Random.Range(1, 99999);
                    Scatter();
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_sceneryRoot == null))
            {
                if (GUILayout.Button("Scatter", GUILayout.Height(30)))
                {
                    Scatter();
                }
            }

            EditorGUILayout.HelpBox("Moves only the Scenery Root's children. Ctrl/Cmd + Z undoes a run.", MessageType.None);
        }

        private void Scatter()
        {
            var ruins = new List<Transform>();
            var trees = new List<Transform>();
            var bushes = new List<Transform>();
            var others = new List<Transform>();
            foreach (Transform child in _sceneryRoot)
            {
                var name = child.name;
                if (name.StartsWith("Ruin") || name.StartsWith("Shack"))
                {
                    ruins.Add(child);
                }
                else if (name.StartsWith("Tree"))
                {
                    trees.Add(child);
                }
                else if (name.StartsWith("Bush"))
                {
                    bushes.Add(child);
                }
                else
                {
                    others.Add(child);
                }
            }

            var all = new List<Transform>();
            all.AddRange(ruins);
            all.AddRange(trees);
            all.AddRange(bushes);
            all.AddRange(others);
            Undo.RecordObjects(all.ToArray(), "Scatter Scenery");

            var random = new System.Random(_seed);
            var blocked = GameplaySpots();
            var placed = new List<(Vector2 Point, float Radius)>();
            var centre = _centre;

            // The ruin ring: evenly round the circle, then pushed about so the ring reads as broken, with one wide gap
            // that opens the arena toward a side.
            Shuffle(ruins, random);
            var gapAngle = (float)random.NextDouble() * 360f;
            var step = 360f / Mathf.Max(1, ruins.Count + 1 + Mathf.RoundToInt(ruins.Count * _ringGaps));
            var angle = gapAngle + step * 1.5f;
            foreach (var ruin in ruins)
            {
                var size = Footprint(ruin);
                Vector2 point = default;
                for (var attempt = 0; attempt < 30; attempt++)
                {
                    var a = angle + Jitter(random, step * 0.35f * _ringGaps);
                    var r = _ringRadius + Jitter(random, 6f * _ringGaps + attempt * 0.3f);
                    point = centre + Around(a) * r;
                    if (IsFree(point, size, placed, blocked))
                    {
                        break;
                    }
                }

                Place(ruin, point, LookAt(point, centre) + Jitter(random, 12f), random);
                placed.Add((point, size));
                angle += step * (1f + Jitter(random, _ringGaps));
            }

            // The groves: a few centres between the ring and the reach, a few inside the ring, more trees in each further out.
            Shuffle(trees, random);
            var groveCount = Mathf.Max(1, Mathf.RoundToInt(trees.Count / 4f));
            var groves = new List<Vector2>();
            for (var g = 0; g < groveCount; g++)
            {
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    var a = (float)random.NextDouble() * 360f;
                    var inside = g % 3 == 0;
                    var r = inside
                        ? Mathf.Lerp(_openMiddle + 8f, _ringRadius - 8f, (float)random.NextDouble())
                        : Mathf.Lerp(_ringRadius + 6f, _groveReach, Mathf.Sqrt((float)random.NextDouble()));
                    var candidate = centre + Around(a) * r;
                    if (IsFree(candidate, 8f, placed, blocked) && groves.TrueForAll(other => Vector2.Distance(other, candidate) > 14f))
                    {
                        groves.Add(candidate);
                        break;
                    }
                }
            }

            if (groves.Count == 0)
            {
                groves.Add(centre + Around(0f) * _groveReach * 0.7f);
            }

            for (var i = 0; i < trees.Count; i++)
            {
                var grove = groves[i % groves.Count];
                var size = Footprint(trees[i]);
                var point = grove;
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    point = grove + Gaussian(random) * (4f + attempt * 0.25f);
                    if (IsFree(point, size, placed, blocked))
                    {
                        break;
                    }
                }

                Place(trees[i], point, (float)random.NextDouble() * 360f, random);
                placed.Add((point, size));
            }

            // Bushes at the feet of trees and ruins, so every group has an undergrowth.
            Shuffle(bushes, random);
            var anchors = new List<(Vector2 Point, float Radius)>(placed);
            foreach (var bush in bushes)
            {
                var size = Footprint(bush);
                Vector2 point = default;
                for (var attempt = 0; attempt < 40; attempt++)
                {
                    var anchor = anchors[random.Next(anchors.Count)];
                    var a = (float)random.NextDouble() * 360f;
                    point = anchor.Point + Around(a) * (anchor.Radius + size + 0.5f + (float)random.NextDouble() * 2.5f);
                    if (IsFree(point, size * 0.8f, placed, blocked))
                    {
                        break;
                    }
                }

                Place(bush, point, (float)random.NextDouble() * 360f, random);
                placed.Add((point, size * 0.8f));
            }

            // Anything else goes just outside the ring.
            foreach (var other in others)
            {
                var size = Footprint(other);
                var point = centre + Around((float)random.NextDouble() * 360f) * (_ringRadius + 10f);
                for (var attempt = 0; attempt < 30 && !IsFree(point, size, placed, blocked); attempt++)
                {
                    point = centre + Around((float)random.NextDouble() * 360f) * (_ringRadius + 10f + attempt);
                }

                Place(other, point, (float)random.NextDouble() * 360f, random);
                placed.Add((point, size));
            }

            Physics.SyncTransforms();
            GrassBrushWindow.RemoveGrassUnderObjects();
            SceneView.RepaintAll();
        }

        private void Place(Transform item, Vector2 point, float yaw, System.Random random)
        {
            var position = new Vector3(point.x, item.position.y, point.y);
            if (_terrain != null)
            {
                position.y = _terrain.SampleHeight(position) + _terrain.GetPosition().y;
            }

            item.position = position;
            var euler = item.eulerAngles;
            item.rotation = Quaternion.Euler(euler.x, yaw, euler.z);

            var scale = 1f + Jitter(random, _scaleVariation);
            item.localScale = PreferredScale(item) * scale;
        }

        // The scale an item had before the first scatter, kept in its session state so repeated runs do not drift.
        private static Vector3 PreferredScale(Transform item)
        {
            var key = "ScatterScenery.Scale." + GlobalObjectId.GetGlobalObjectIdSlow(item);
            var stored = SessionState.GetVector3(key, Vector3.zero);
            if (stored == Vector3.zero)
            {
                stored = item.localScale;
                SessionState.SetVector3(key, stored);
            }

            return stored;
        }

        private bool IsFree(Vector2 point, float radius, List<(Vector2 Point, float Radius)> placed, List<Vector2> blocked)
        {
            if (Vector2.Distance(point, _centre) < _openMiddle + radius)
            {
                return false;
            }

            foreach (var spot in blocked)
            {
                if (Vector2.Distance(point, spot) < _keepClear + radius)
                {
                    return false;
                }
            }

            foreach (var other in placed)
            {
                if (Vector2.Distance(point, other.Point) < (other.Radius + radius) * 0.9f)
                {
                    return false;
                }
            }

            if (_terrain != null)
            {
                var origin = _terrain.GetPosition();
                var size = _terrain.terrainData.size;
                if (point.x < origin.x + 2f || point.x > origin.x + size.x - 2f || point.y < origin.z + 2f || point.y > origin.z + size.z - 2f)
                {
                    return false;
                }
            }

            return true;
        }

        // The hives, every nest tree and every spawn point: the places the game happens.
        private static List<Vector2> GameplaySpots()
        {
            var spots = new List<Vector2>();
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var name = root.name;
                if (name.StartsWith("Hive A") || name.StartsWith("Hive B"))
                {
                    spots.Add(Flat(root.transform.position));
                }
                else if (name == "Nests" || name == "Spawn Points")
                {
                    foreach (Transform child in root.transform)
                    {
                        spots.Add(Flat(child.position));
                    }
                }
            }

            return spots;
        }

        // Half the width of what the item covers on the ground.
        private static float Footprint(Transform item)
        {
            var renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return 1f;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), 0.5f, 12f);
        }

        private static float LookAt(Vector2 from, Vector2 to)
        {
            var direction = to - from;
            return Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        }

        private static Vector2 Around(float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        private static Vector2 Flat(Vector3 position) => new Vector2(position.x, position.z);

        private static float Jitter(System.Random random, float amount) => ((float)random.NextDouble() * 2f - 1f) * amount;

        private static Vector2 Gaussian(System.Random random)
        {
            var u = 1.0 - random.NextDouble();
            var v = random.NextDouble();
            var length = Mathf.Sqrt(-2f * Mathf.Log((float)u));
            var turn = (float)(2.0 * Mathf.PI * v);
            return new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * length * 0.6f;
        }

        private static void Shuffle<T>(List<T> list, System.Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
