using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.GrassBrush.Editor
{
    /// <summary>
    /// Shapes a Terrain into a valley: a flat clearing where the game happens, ringed by rolling hills with
    /// ridges that climb toward the far edges. The clearing's edge wanders, so it does not read as a circle.
    /// Open it from Tools > DigitalStory > Hill Generator. Every run can be undone, and painted grass follows.
    /// </summary>
    public sealed class HillGeneratorWindow : EditorWindow
    {
        private Terrain _terrain;
        private int _seed = 7;
        private Vector2 _centre = Vector2.zero;
        private float _clearingRadius = 60f;
        private float _edgeWander = 12f;
        private float _ramp = 25f;
        private float _hillHeight = 16f;
        private float _ridges = 0.6f;
        private float _hillSize = 32f;
        private Transform _objectsRoot;
        private bool _snapObjects = true;
        private float _edgeRise = 35f;
        private float _clearingBumps = 0f;

        [MenuItem("Tools/DigitalStory/Hill Generator")]
        private static void Open()
        {
            GetWindow<HillGeneratorWindow>("Hill Generator");
        }

        private void OnEnable()
        {
            if (_terrain == null)
            {
                _terrain = Terrain.activeTerrain;
            }

            if (_objectsRoot == null)
            {
                var scenery = GameObject.Find("Scenery");
                _objectsRoot = scenery != null ? scenery.transform : null;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            _terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", _terrain, typeof(Terrain), true);
            if (_terrain == null)
            {
                EditorGUILayout.HelpBox("Pick the Terrain to shape.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Clearing", EditorStyles.boldLabel);
            _centre = EditorGUILayout.Vector2Field("Centre (X, Z)", _centre);
            _clearingRadius = EditorGUILayout.Slider("Radius", _clearingRadius, 5f, 400f);
            if (GUILayout.Button("Fit the clearing around the gameplay (not the scenery)"))
            {
                FitClearing();
            }

            _edgeWander = EditorGUILayout.Slider("Edge Wander", _edgeWander, 0f, 60f);
            _clearingBumps = EditorGUILayout.Slider("Bumps Inside", _clearingBumps, 0f, 2f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hills", EditorStyles.boldLabel);
            _ramp = EditorGUILayout.Slider("Slope Width", _ramp, 5f, 200f);
            _hillHeight = EditorGUILayout.Slider("Hill Height", _hillHeight, 0f, 120f);
            _hillSize = EditorGUILayout.Slider("Hill Size (smaller = more hills)", _hillSize, 8f, 300f);
            _ridges = EditorGUILayout.Slider("Ridges", _ridges, 0f, 1f);
            _edgeRise = EditorGUILayout.Slider("Rise Toward Edges", _edgeRise, 0f, 200f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);
            _objectsRoot = (Transform)EditorGUILayout.ObjectField("Objects Root", _objectsRoot, typeof(Transform), true);
            _snapObjects = EditorGUILayout.Toggle("Keep Them On The Ground", _snapObjects);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Snap Root's Objects Now"))
                {
                    SnapObjects(CollectObjects(), null, "Snap Objects To Terrain");
                }

                if (GUILayout.Button("Snap Selected Now"))
                {
                    var selected = new List<Transform>(Selection.transforms);
                    SnapObjects(selected, null, "Snap Selected To Terrain");
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                _seed = EditorGUILayout.IntField("Seed", _seed);
                if (GUILayout.Button("New", GUILayout.Width(50)))
                {
                    _seed = Random.Range(1, 99999);
                    Generate();
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Hills", GUILayout.Height(30)))
            {
                Generate();
            }

            if (GUILayout.Button("Flatten Terrain"))
            {
                ApplyAndKeepObjects((x, z) => 0f, "Flatten Terrain");
            }

            EditorGUILayout.HelpBox("Ctrl/Cmd + Z undoes a run. Painted grass moves onto the new ground.", MessageType.None);
        }

        private void Generate()
        {
            var random = new System.Random(_seed);
            var offsets = new Vector2[6];
            for (var i = 0; i < offsets.Length; i++)
            {
                offsets[i] = new Vector2((float)random.NextDouble() * 10000f, (float)random.NextDouble() * 10000f);
            }

            var size = _terrain.terrainData.size;
            var origin = _terrain.GetPosition();
            var farthest = Mathf.Max(
                Vector2.Distance(_centre, new Vector2(origin.x, origin.z)),
                Vector2.Distance(_centre, new Vector2(origin.x + size.x, origin.z)),
                Vector2.Distance(_centre, new Vector2(origin.x, origin.z + size.z)),
                Vector2.Distance(_centre, new Vector2(origin.x + size.x, origin.z + size.z)));

            ApplyAndKeepObjects((x, z) =>
            {
                var offset = new Vector2(x, z) - _centre;
                var distance = offset.magnitude;

                // The clearing's edge wanders in and out, so the hills start at different distances.
                var wander = (Mathf.PerlinNoise(x / 40f + offsets[0].x, z / 40f + offsets[0].y) - 0.5f) * 2f * _edgeWander;
                var start = _clearingRadius + wander;
                var hills = Smooth(Mathf.Clamp01((distance - start) / _ramp));

                // Rolling hills: soft noise in a few sizes.
                var rolling = Fbm(x / _hillSize + offsets[1].x, z / _hillSize + offsets[1].y, 4);

                // Ridges: sharp crests where the noise folds, warped a little so they curve like real ridges.
                var warpX = (Mathf.PerlinNoise(x / 90f + offsets[2].x, z / 90f + offsets[2].y) - 0.5f) * 30f;
                var warpZ = (Mathf.PerlinNoise(x / 90f + offsets[3].x, z / 90f + offsets[3].y) - 0.5f) * 30f;
                var ridged = Ridged((x + warpX) / (_hillSize * 1.4f) + offsets[4].x, (z + warpZ) / (_hillSize * 1.4f) + offsets[4].y, 3);

                var shape = Mathf.Lerp(rolling, ridged, _ridges);

                // The land climbs toward the far edges, so the clearing sits in a valley.
                var toEdge = Mathf.Clamp01((distance - start) / Mathf.Max(1f, farthest - start));
                var rise = Mathf.Pow(toEdge, 1.6f) * _edgeRise;

                var bumps = (Fbm(x / 25f + offsets[5].x, z / 25f + offsets[5].y, 2) - 0.5f) * 2f * _clearingBumps;
                bumps = Mathf.Max(0f, bumps) * (1f - hills);

                return hills * (shape * _hillHeight + rise) + bumps;
            }, "Generate Hills");
        }

        /// <summary>
        /// Reshapes the terrain, and when "Keep Them On The Ground" is on, moves the root's objects with it:
        /// each keeps the height it had above the old ground, so a tree stays planted and a floating lamp stays floating.
        /// </summary>
        private void ApplyAndKeepObjects(System.Func<float, float, float> heightAt, string undoName)
        {
            var objects = _snapObjects ? CollectObjects() : new List<Transform>();
            var offsets = new List<float>(objects.Count);
            foreach (var item in objects)
            {
                offsets.Add(item.position.y - GroundAt(item.position));
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Apply(heightAt, undoName);
            SnapObjects(objects, offsets, undoName);
            Undo.CollapseUndoOperations(group);
        }

        /// <summary>Every direct child of the root, which for the Scenery root means each tree, bush and ruin.</summary>
        private List<Transform> CollectObjects()
        {
            var objects = new List<Transform>();
            if (_objectsRoot == null)
            {
                return objects;
            }

            foreach (Transform child in _objectsRoot)
            {
                objects.Add(child);
            }

            return objects;
        }

        private void SnapObjects(List<Transform> objects, List<float> offsets, string undoName)
        {
            if (objects.Count == 0)
            {
                return;
            }

            Undo.RecordObjects(objects.ToArray(), undoName);
            for (var i = 0; i < objects.Count; i++)
            {
                var item = objects[i];
                if (!IsOverTerrain(item.position))
                {
                    continue;
                }

                var position = item.position;
                var offset = offsets != null ? offsets[i] : 0f;
                position.y = GroundAt(position) + offset;
                item.position = position;
            }

            SceneView.RepaintAll();
        }

        private float GroundAt(Vector3 position)
        {
            return _terrain.SampleHeight(position) + _terrain.GetPosition().y;
        }

        private bool IsOverTerrain(Vector3 position)
        {
            var origin = _terrain.GetPosition();
            var size = _terrain.terrainData.size;
            return position.x >= origin.x && position.x <= origin.x + size.x &&
                position.z >= origin.z && position.z <= origin.z + size.z;
        }

        private void Apply(System.Func<float, float, float> heightAt, string undoName)
        {
            var terrainData = _terrain.terrainData;
            Undo.RegisterCompleteObjectUndo(terrainData, undoName);

            var resolution = terrainData.heightmapResolution;
            var size = terrainData.size;
            var origin = _terrain.GetPosition();
            var heights = new float[resolution, resolution];
            var step = 1f / (resolution - 1);

            for (var row = 0; row < resolution; row++)
            {
                var z = origin.z + row * step * size.z;
                for (var column = 0; column < resolution; column++)
                {
                    var x = origin.x + column * step * size.x;
                    heights[row, column] = Mathf.Clamp01(heightAt(x, z) / size.y);
                }
            }

            terrainData.SetHeights(0, 0, heights);
            _terrain.Flush();
            EditorUtility.SetDirty(terrainData);

            foreach (var field in Resources.FindObjectsOfTypeAll<GrassField>())
            {
                if (field.gameObject.scene.IsValid())
                {
                    field.SnapToTerrain(_terrain);
                }
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Sets the clearing so the gameplay stays on flat ground: the hives, nests, spawn points and the rest,
        /// everything except the scenery under the Objects Root, which rides on the hills instead.
        /// </summary>
        private void FitClearing()
        {
            var points = new List<Vector3>();
            foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (!transform.gameObject.scene.IsValid() || !transform.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (_objectsRoot != null && (transform == _objectsRoot || transform.IsChildOf(_objectsRoot)))
                {
                    continue;
                }

                if (transform.GetComponent<Terrain>() != null || transform.GetComponent<Camera>() != null ||
                    transform.GetComponent<Light>() != null || transform.GetComponent<GrassField>() != null)
                {
                    continue;
                }

                var renderer = transform.GetComponent<Renderer>();
                if (renderer != null && (renderer.bounds.size.x > 60f || renderer.bounds.size.z > 60f))
                {
                    continue;
                }

                var flat = new Vector3(transform.position.x, 0f, transform.position.z);
                if (flat.magnitude < 250f)
                {
                    points.Add(flat);
                }
            }

            if (points.Count == 0)
            {
                return;
            }

            var max = 0f;
            var centre3 = new Vector3(_centre.x, 0f, _centre.y);
            foreach (var point in points)
            {
                max = Mathf.Max(max, Vector3.Distance(point, centre3));
            }

            _clearingRadius = Mathf.Clamp(max + 6f + _edgeWander, 5f, 400f);
            Repaint();
        }

        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float Fbm(float x, float z, int octaves)
        {
            var total = 0f;
            var amplitude = 0.5f;
            var frequency = 1f;
            var sum = 0f;
            for (var i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
                sum += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.03f;
            }

            return total / sum;
        }

        private static float Ridged(float x, float z, int octaves)
        {
            var total = 0f;
            var amplitude = 0.5f;
            var frequency = 1f;
            var sum = 0f;
            for (var i = 0; i < octaves; i++)
            {
                var n = 1f - Mathf.Abs(Mathf.PerlinNoise(x * frequency, z * frequency) * 2f - 1f);
                total += n * n * amplitude;
                sum += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.1f;
            }

            return total / sum;
        }
    }
}
