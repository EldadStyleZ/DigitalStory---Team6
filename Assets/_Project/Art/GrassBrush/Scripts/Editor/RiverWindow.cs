using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DigitalStory.GrassBrush.Editor
{
    /// <summary>
    /// Carves winding rivers into the terrain and fills them with Crest water. Each river crosses the whole map from edge to
    /// edge in lazy bends, bending away from the hives, the nests, the spawn points and the open middle, so the game's places
    /// stay dry. Its bed sinks below the water level and its banks slope softly back to the ground. One Crest water surface
    /// sits just below the ground, so it only shows in the riverbeds. Grass and scenery leave the water.
    /// Open it from Tools > DigitalStory > Rivers. One Ctrl/Cmd + Z undoes a run.
    /// </summary>
    public sealed class RiverWindow : EditorWindow
    {
        private const string WaterTypeName = "WaveHarmonic.Crest.WaterRenderer, WaveHarmonic.Crest";
        private const string WaterObjectName = "Water (Crest)";

        private Terrain _terrain;
        private int _seed = 5;
        private int _riverCount = 2;
        private float _width = 7f;
        private float _depth = 2.4f;
        private float _bank = 7f;
        private float _bends = 0.6f;
        private float _waterLevel = -0.7f;
        private float _reach = 140f;
        private float _keepClear = 14f;
        private float _openMiddle = 22f;
        private Transform _sceneryRoot;

        [MenuItem("Tools/DigitalStory/Rivers")]
        private static void Open()
        {
            GetWindow<RiverWindow>("Rivers");
        }

        private void OnEnable()
        {
            if (_terrain == null)
            {
                _terrain = Terrain.activeTerrain;
            }

            if (_sceneryRoot == null)
            {
                var scenery = GameObject.Find("Scenery");
                _sceneryRoot = scenery != null ? scenery.transform : null;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            _terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", _terrain, typeof(Terrain), true);
            _sceneryRoot = (Transform)EditorGUILayout.ObjectField(new GUIContent("Scenery Root", "Its objects that stand in a river move to the bank."), _sceneryRoot, typeof(Transform), true);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Rivers", EditorStyles.boldLabel);
            _riverCount = EditorGUILayout.IntSlider("How Many", _riverCount, 1, 4);
            _width = EditorGUILayout.Slider(new GUIContent("Width", "Metres of water across."), _width, 2f, 30f);
            _depth = EditorGUILayout.Slider(new GUIContent("Depth", "Metres the bed sinks below the ground."), _depth, 1f, 8f);
            _bank = EditorGUILayout.Slider(new GUIContent("Bank", "Metres over which the bank slopes back up to the ground."), _bank, 1f, 25f);
            _bends = EditorGUILayout.Slider(new GUIContent("Bends", "How much the rivers wind."), _bends, 0f, 1f);
            _reach = EditorGUILayout.Slider(new GUIContent("Reach", "Metres from the middle where the rivers start and end."), _reach, 40f, 400f);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Keep Dry", EditorStyles.boldLabel);
            _keepClear = EditorGUILayout.Slider(new GUIContent("Round Gameplay", "Metres the water keeps from hives, nests and spawn points."), _keepClear, 0f, 40f);
            _openMiddle = EditorGUILayout.Slider(new GUIContent("Open Middle", "Metres round the middle with no river."), _openMiddle, 0f, 80f);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Water", EditorStyles.boldLabel);
            _waterLevel = EditorGUILayout.Slider(new GUIContent("Water Level", "The water's height. Keep it a little below the ground, so it shows only in the beds."), _waterLevel, -5f, 0f);
            EditorGUILayout.LabelField("Crest", Type.GetType(WaterTypeName) != null ? "found" : "not found: rivers are carved, but no water is added");
            EditorGUILayout.EndVertical();

            using (new EditorGUILayout.HorizontalScope())
            {
                _seed = EditorGUILayout.IntField("Seed", _seed);
                if (GUILayout.Button("New", GUILayout.Width(50)))
                {
                    _seed = UnityEngine.Random.Range(1, 99999);
                }
            }

            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(_terrain == null))
            {
                if (GUILayout.Button("Carve Rivers", GUILayout.Height(30)))
                {
                    Carve();
                }
            }

            using (new EditorGUI.DisabledScope(_terrain == null))
            {
                if (GUILayout.Button(new GUIContent("Smooth Banks", "Keeps the rivers as they are: gives the terrain finer height points and softens the beds and banks, so they stop looking stepped.")))
                {
                    SmoothBanks();
                }
            }

            if (GUILayout.Button(new GUIContent("Night Water With Reflections", "Gives the water a dark night material and turns on reflections of the scene, so the moon, the swarm and the nests' lights shine in it.")))
            {
                SetUpNightWater();
            }

            EditorGUILayout.HelpBox("Each run carves new rivers into the terrain as it is now. Ctrl/Cmd + Z undoes a run; undo before carving again to replace the rivers.", MessageType.None);
        }

        private void Carve()
        {
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();

            var terrainData = _terrain.terrainData;
            var terrainTransform = _terrain.transform;
            Undo.RegisterCompleteObjectUndo(terrainData, "Carve Rivers");
            Undo.RecordObject(terrainTransform, "Carve Rivers");

            // A river is only a few metres wide, so it needs height points close together, or its banks come out as steps.
            Refine(terrainData);
            _terrain.heightmapPixelError = Mathf.Min(_terrain.heightmapPixelError, 2f);

            // A terrain cannot go below its own position, so it is lowered first, and its heights raised to match, so the
            // ground stays where it was and the beds have room below it.
            var floor = _waterLevel - _depth - 0.5f;
            var size = terrainData.size;
            var resolution = terrainData.heightmapResolution;
            var heights = terrainData.GetHeights(0, 0, resolution, resolution);
            if (terrainTransform.position.y > floor)
            {
                var drop = terrainTransform.position.y - floor;
                if (size.y < drop * 4f)
                {
                    size.y = Mathf.Max(size.y, drop * 4f);
                }

                var oldHeight = terrainData.size.y;
                terrainData.size = size;
                for (var row = 0; row < resolution; row++)
                {
                    for (var column = 0; column < resolution; column++)
                    {
                        heights[row, column] = Mathf.Clamp01(((heights[row, column] * oldHeight) + drop) / size.y);
                    }
                }

                terrainTransform.position -= new Vector3(0f, drop, 0f);
            }

            var origin = terrainTransform.position;
            var rivers = PlanRivers();

            // Each heightmap sample near a river: its distance to the nearest river line, then the bed's shape.
            var nearest = new float[resolution, resolution];
            for (var row = 0; row < resolution; row++)
            {
                for (var column = 0; column < resolution; column++)
                {
                    nearest[row, column] = float.MaxValue;
                }
            }

            var influence = (_width * 0.5f) + _bank;
            var step = new Vector2(size.x / (resolution - 1), size.z / (resolution - 1));
            foreach (var river in rivers)
            {
                for (var index = 0; index < river.Count - 1; index++)
                {
                    var a = river[index];
                    var b = river[index + 1];
                    var minX = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - influence - origin.x) / step.x));
                    var maxX = Mathf.Min(resolution - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + influence - origin.x) / step.x));
                    var minZ = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - influence - origin.z) / step.y));
                    var maxZ = Mathf.Min(resolution - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + influence - origin.z) / step.y));
                    for (var row = minZ; row <= maxZ; row++)
                    {
                        for (var column = minX; column <= maxX; column++)
                        {
                            var point = new Vector2(origin.x + (column * step.x), origin.z + (row * step.y));
                            var distance = DistanceToSegment(point, a, b);
                            if (distance < nearest[row, column])
                            {
                                nearest[row, column] = distance;
                            }
                        }
                    }
                }
            }

            var bed = _waterLevel - _depth;
            for (var row = 0; row < resolution; row++)
            {
                for (var column = 0; column < resolution; column++)
                {
                    var distance = nearest[row, column];
                    if (distance > influence)
                    {
                        continue;
                    }

                    var ground = (heights[row, column] * size.y) + origin.y;

                    // A rounded bed across the water, then a soft bank back up to the ground.
                    var across = Mathf.Clamp01(distance / (_width * 0.5f));
                    var inWater = Mathf.Lerp(bed, _waterLevel + 0.1f, across * across);
                    var t = Mathf.Clamp01((distance - (_width * 0.5f)) / _bank);
                    var shaped = distance <= _width * 0.5f ? inWater : Mathf.Lerp(_waterLevel + 0.1f, ground, t * t * (3f - (2f * t)));
                    var carved = Mathf.Min(ground, shaped);
                    heights[row, column] = Mathf.Clamp01((carved - origin.y) / size.y);
                }
            }

            terrainData.SetHeights(0, 0, heights);
            _terrain.Flush();
            EditorUtility.SetDirty(terrainData);
            Physics.SyncTransforms();

            MoveSceneryOutOfRivers(rivers, influence);
            KeepGrassDry();
            PlaceWater();

            Undo.CollapseUndoOperations(group);
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Softens the rivers already carved, without moving them: finer height points, then a few gentle blurs that touch
        /// only the ground near water level (the beds and banks), so the land around stays as it was.
        /// </summary>
        private void SmoothBanks()
        {
            var terrainData = _terrain.terrainData;
            Undo.RegisterCompleteObjectUndo(terrainData, "Smooth Banks");
            Refine(terrainData);
            _terrain.heightmapPixelError = Mathf.Min(_terrain.heightmapPixelError, 2f);

            var resolution = terrainData.heightmapResolution;
            var size = terrainData.size;
            var originY = _terrain.GetPosition().y;
            var heights = terrainData.GetHeights(0, 0, resolution, resolution);

            // Only the riverside: ground below the bank tops, and a few points beyond so the top edge eases in too.
            var bankTop = _waterLevel + 1.2f;
            var spacing = size.x / (resolution - 1);
            var grow = Mathf.Max(1, Mathf.RoundToInt(1.5f / spacing));
            var near = new bool[resolution, resolution];
            for (var row = 0; row < resolution; row++)
            {
                for (var column = 0; column < resolution; column++)
                {
                    if ((heights[row, column] * size.y) + originY > bankTop)
                    {
                        continue;
                    }

                    for (var dz = -grow; dz <= grow; dz++)
                    {
                        for (var dx = -grow; dx <= grow; dx++)
                        {
                            var r = row + dz;
                            var c = column + dx;
                            if (r >= 0 && c >= 0 && r < resolution && c < resolution)
                            {
                                near[r, c] = true;
                            }
                        }
                    }
                }
            }

            var reach = Mathf.Max(1, Mathf.RoundToInt(1f / spacing));
            var copy = new float[resolution, resolution];
            for (var pass = 0; pass < 3; pass++)
            {
                System.Array.Copy(heights, copy, heights.Length);
                for (var row = 0; row < resolution; row++)
                {
                    for (var column = 0; column < resolution; column++)
                    {
                        if (!near[row, column])
                        {
                            continue;
                        }

                        var sum = 0f;
                        var count = 0;
                        for (var dz = -reach; dz <= reach; dz++)
                        {
                            for (var dx = -reach; dx <= reach; dx++)
                            {
                                var r = Mathf.Clamp(row + dz, 0, resolution - 1);
                                var c = Mathf.Clamp(column + dx, 0, resolution - 1);
                                sum += copy[r, c];
                                count++;
                            }
                        }

                        heights[row, column] = sum / count;
                    }
                }
            }

            terrainData.SetHeights(0, 0, heights);
            _terrain.Flush();
            EditorUtility.SetDirty(terrainData);
            Physics.SyncTransforms();

            foreach (var field in Resources.FindObjectsOfTypeAll<GrassField>())
            {
                if (field.gameObject.scene.IsValid())
                {
                    field.SnapToTerrain(_terrain);
                    field.MarkDirty();
                }
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Raises the heightmap's resolution until its points are at most half a metre apart (up to 4097 a side), keeping
        /// the ground's shape by sampling the old heights smoothly. Does nothing when it is already that fine.
        /// </summary>
        private static void Refine(TerrainData terrainData)
        {
            const float WantedSpacing = 0.5f;
            var size = terrainData.size;
            var oldResolution = terrainData.heightmapResolution;
            var wanted = Mathf.Max(size.x, size.z) / WantedSpacing;
            var resolution = oldResolution;
            while (resolution - 1 < wanted && resolution < 4097)
            {
                resolution = ((resolution - 1) * 2) + 1;
            }

            if (resolution == oldResolution)
            {
                return;
            }

            var old = terrainData.GetHeights(0, 0, oldResolution, oldResolution);
            terrainData.heightmapResolution = resolution;
            terrainData.size = size; // Changing the resolution can change the size, so it is put back.

            var heights = new float[resolution, resolution];
            var scale = (oldResolution - 1f) / (resolution - 1f);
            for (var row = 0; row < resolution; row++)
            {
                var y = row * scale;
                var y0 = Mathf.Min(Mathf.FloorToInt(y), oldResolution - 2);
                var fy = y - y0;
                for (var column = 0; column < resolution; column++)
                {
                    var x = column * scale;
                    var x0 = Mathf.Min(Mathf.FloorToInt(x), oldResolution - 2);
                    var fx = x - x0;
                    var bottom = Mathf.Lerp(old[y0, x0], old[y0, x0 + 1], fx);
                    var top = Mathf.Lerp(old[y0 + 1, x0], old[y0 + 1, x0 + 1], fx);
                    heights[row, column] = Mathf.Lerp(bottom, top, fy);
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        // Each river runs from one side of the map to the other, through the middle's neighbourhood but round it, in bends.
        private List<List<Vector2>> PlanRivers()
        {
            var random = new System.Random(_seed);
            var blocked = GameplaySpots();
            var rivers = new List<List<Vector2>>();
            var firstAngle = (float)random.NextDouble() * 180f;

            for (var r = 0; r < _riverCount; r++)
            {
                // Rivers cross at spread angles, and pass to one side of the middle.
                var angle = (firstAngle + (r * 180f / _riverCount) + (float)((random.NextDouble() - 0.5) * 30.0)) * Mathf.Deg2Rad;
                var along = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var side = new Vector2(-along.y, along.x);
                var shift = side * ((_openMiddle + _width + 10f + ((float)random.NextDouble() * 25f)) * (r % 2 == 0 ? 1f : -1f));
                var phase = (float)random.NextDouble() * 10f;

                var points = new List<Vector2>();
                const int Count = 120;
                for (var i = 0; i <= Count; i++)
                {
                    var t = (float)i / Count;
                    var distance = Mathf.Lerp(-_reach, _reach, t);

                    // Bends: two waves, a long lazy one and a shorter one, drifting with noise.
                    var bend = (Mathf.Sin((distance * 0.035f) + phase) * 18f) + (Mathf.Sin((distance * 0.09f) + (phase * 2f)) * 6f);
                    bend += (Mathf.PerlinNoise((distance * 0.02f) + phase, r * 3.7f) - 0.5f) * 20f;
                    points.Add((along * distance) + shift + (side * bend * _bends));
                }

                // Push the river away from every place the game happens, a few times, keeping the line smooth.
                for (var pass = 0; pass < 12; pass++)
                {
                    for (var i = 1; i < points.Count - 1; i++)
                    {
                        var point = points[i];
                        var clear = (_width * 0.5f) + _bank * 0.5f;
                        foreach (var spot in blocked)
                        {
                            var away = point - spot;
                            var need = _keepClear + clear;
                            if (away.magnitude < need)
                            {
                                point = spot + (away.sqrMagnitude > 0.0001f ? away.normalized : side) * need;
                            }
                        }

                        if (point.magnitude < _openMiddle + clear)
                        {
                            point = (point.sqrMagnitude > 0.0001f ? point.normalized : side) * (_openMiddle + clear);
                        }

                        points[i] = point;
                    }

                    for (var i = 1; i < points.Count - 1; i++)
                    {
                        points[i] = Vector2.Lerp(points[i], (points[i - 1] + points[i + 1]) * 0.5f, 0.5f);
                    }
                }

                rivers.Add(points);
            }

            return rivers;
        }

        private void MoveSceneryOutOfRivers(List<List<Vector2>> rivers, float influence)
        {
            if (_sceneryRoot == null)
            {
                return;
            }

            var origin = _terrain.GetPosition();
            var moved = new List<Transform>();
            foreach (Transform item in _sceneryRoot)
            {
                moved.Add(item);
            }

            Undo.RecordObjects(moved.ToArray(), "Carve Rivers");
            foreach (var item in moved)
            {
                var flat = new Vector2(item.position.x, item.position.z);
                var safe = (_width * 0.5f) + (_bank * 0.6f);
                for (var pass = 0; pass < 4; pass++)
                {
                    foreach (var river in rivers)
                    {
                        for (var i = 0; i < river.Count - 1; i++)
                        {
                            var closest = ClosestOnSegment(flat, river[i], river[i + 1]);
                            var away = flat - closest;
                            if (away.magnitude < safe)
                            {
                                flat = closest + ((away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.right) * safe);
                            }
                        }
                    }
                }

                var position = new Vector3(flat.x, item.position.y, flat.y);
                position.y = _terrain.SampleHeight(position) + origin.y;
                item.position = position;
            }
        }

        // Grass stops at the water: covering grass grows only above it, and painted grass in a bed goes.
        private void KeepGrassDry()
        {
            var dryAbove = _waterLevel + 0.15f;
            foreach (var field in Resources.FindObjectsOfTypeAll<GrassField>())
            {
                if (!field.gameObject.scene.IsValid())
                {
                    continue;
                }

                Undo.RecordObject(field, "Carve Rivers");
                field.SetNoGrassBelow(dryAbove);
                EditorUtility.SetDirty(field);
                if (field.Data != null)
                {
                    Undo.RecordObject(field.Data, "Carve Rivers");
                    field.SnapToTerrain(_terrain);
                    field.Data.RemoveWhere(position => position.y < dryAbove);
                    EditorUtility.SetDirty(field.Data);
                    AssetDatabase.SaveAssetIfDirty(field.Data);
                }

                field.MarkDirty();
            }
        }

        /// <summary>
        /// A dark night version of Crest's water material, and planar reflections that include pixel lights, so the moon,
        /// the swarm's glow and the nests' lights all shine in the rivers. Grass is left out of the reflection.
        /// </summary>
        private static void SetUpNightWater()
        {
            var waterType = Type.GetType(WaterTypeName);
            var water = waterType != null ? FindAnyObjectByType(waterType) as Component : null;
            if (water == null)
            {
                EditorUtility.DisplayDialog("Night Water", "There is no Crest water in the scene. Carve rivers first.", "OK");
                return;
            }

            const string folder = "Assets/_Project/Art/Water";
            const string path = folder + "/Night Water.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.waveharmonic.crest/Runtime/Materials/Water.mat");
                if (source == null)
                {
                    Debug.LogError("Crest's Water.mat was not found.");
                    return;
                }

                System.IO.Directory.CreateDirectory(folder);
                material = new Material(source) { name = "Night Water" };
                AssetDatabase.CreateAsset(material, path);
            }

            Undo.RecordObject(material, "Night Water");

            // Deep night water: dark blue-green scattering, little sky light, and soft foam, so lights stand out on it.
            SetColor(material, "_Crest_Scattering", new Color(0.015f, 0.035f, 0.05f, 1f));
            SetColor(material, "_Crest_AbsorptionColor", new Color(0.02f, 0.05f, 0.07f, 1f));
            SetColor(material, "_Crest_Absorption", new Color(1.2f, 0.6f, 0.45f, 1f));
            SetFloat(material, "_Crest_PlanarReflectionsEnabled", 1f);
            SetFloat(material, "_Crest_AmbientTerm", 0.25f);
            SetFloat(material, "_Crest_DirectTerm", 0.35f);
            SetFloat(material, "_Crest_SSSIntensity", 0.15f);
            SetFloat(material, "_Crest_FoamIntensityAlbedo", 0.2f);
            SetFloat(material, "_Crest_ShorelineFoamStrength", 0.35f);
            SetFloat(material, "_Crest_Smoothness", 0.93f);
            SetFloat(material, "_Crest_PlanarReflectionsIntensity", 1f);
            SetFloat(material, "_Crest_PlanarReflectionsDistortion", 0.25f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            var serialized = new SerializedObject(water);
            SetProperty(serialized, "_Surface._Material", property => property.objectReferenceValue = material);
            SetProperty(serialized, "_Reflections._Enabled", property => property.boolValue = true);
            SetProperty(serialized, "_Reflections._DisablePixelLights", property => property.boolValue = false);
            SetProperty(serialized, "_Reflections._Sky", property => property.boolValue = true);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(water);
            SceneView.RepaintAll();
            Debug.Log("Night water is on, with reflections of the scene's lights. Save the scene to keep it.");
        }

        private static void SetColor(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void SetProperty(SerializedObject serialized, string path, Action<SerializedProperty> set)
        {
            var property = serialized.FindProperty(path);
            if (property == null)
            {
                Debug.LogWarning($"Crest's water has no setting {path}; it was skipped.");
                return;
            }

            set(property);
        }

        // One Crest water surface just below the ground; the terrain hides it everywhere but in the beds.
        private void PlaceWater()
        {
            var waterType = Type.GetType(WaterTypeName);
            if (waterType == null)
            {
                Debug.LogWarning("Crest's WaterRenderer was not found, so the rivers are carved but hold no water.");
                return;
            }

            var water = FindAnyObjectByType(waterType) as Component;
            if (water == null)
            {
                var go = new GameObject(WaterObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Carve Rivers");
                water = ObjectFactory.AddComponent(go, waterType);
            }

            Undo.RecordObject(water.transform, "Carve Rivers");
            water.transform.position = new Vector3(0f, _waterLevel, 0f);
            EditorUtility.SetDirty(water);
        }

        // The hives, every nest tree and every spawn point.
        private static List<Vector2> GameplaySpots()
        {
            var spots = new List<Vector2>();
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var name = root.name;
                if (name.StartsWith("Hive A") || name.StartsWith("Hive B"))
                {
                    spots.Add(new Vector2(root.transform.position.x, root.transform.position.z));
                }
                else if (name == "Nests" || name == "Spawn Points")
                {
                    foreach (Transform child in root.transform)
                    {
                        spots.Add(new Vector2(child.position.x, child.position.z));
                    }
                }
            }

            return spots;
        }

        private static Vector2 ClosestOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;
            return a + (ab * t);
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            return Vector2.Distance(point, ClosestOnSegment(point, a, b));
        }
    }
}
