using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DigitalStory.GrassBrush.Editor
{
    /// <summary>
    /// Paints wind-blown low poly grass onto the terrain, in as many grass types as you like.
    /// Open it from Tools > DigitalStory > Grass Brush. Pick a type, then drag in the Scene view to paint,
    /// hold Shift to erase, and hold Ctrl (Cmd on a Mac) while scrolling to change the brush size.
    /// Each grass type is its own Grass Field with its own look, colours, sizes and saved grass.
    /// The brush only takes Scene view clicks while the chosen type's Grass Field is selected, so other tools such as
    /// the Terrain's sculpting brushes keep working whenever something else is selected.
    /// </summary>
    public sealed class GrassBrushWindow : EditorWindow
    {
        private const string Folder = "Assets/_Project/Art/GrassBrush";
        private const string MaterialPath = Folder + "/WindGrass.mat";
        private const string TypesFolder = Folder + "/Types";
        private const string DataFolder = Folder + "/Data";

        private GrassField _field;
        private bool _painting = true;
        private float _radius = 2f;
        private float _density = 12f;
        private float _falloff = 0.5f;
        private LayerMask _surfaces = ~0;
        private bool _terrainOnly = true;
        private bool _eraseAllTypes;

        private bool _showBrush = true;
        private bool _showCover = true;
        private bool _showHeight = true;
        private bool _showShape;
        private bool _showColours;
        private bool _showWind;
        private bool _showPerformance;
        private Vector2 _scroll;

        private bool _hasHit;
        private Vector3 _hitPoint;
        private Vector3 _hitNormal = Vector3.up;
        private Vector3 _lastStroke;

        private static readonly (string Name, int Blades, int Segments, float Width, float Spread, float Lean, float Variation, float Min, float Max, Color Root, Color Tip)[] Presets =
        {
            ("Short Lawn", 14, 1, 0.12f, 0.15f, 0.12f, 0.3f, 0.2f, 0.35f, new Color(0.07f, 0.15f, 0.05f), new Color(0.3f, 0.52f, 0.16f)),
            ("Tall Grass", 10, 2, 0.14f, 0.2f, 0.3f, 0.45f, 0.7f, 1.3f, new Color(0.06f, 0.14f, 0.05f), new Color(0.38f, 0.58f, 0.2f)),
            ("Wild Meadow", 18, 1, 0.16f, 0.35f, 0.4f, 0.7f, 0.35f, 0.9f, new Color(0.08f, 0.16f, 0.06f), new Color(0.45f, 0.6f, 0.22f)),
            ("Dry Grass", 12, 1, 0.1f, 0.25f, 0.35f, 0.5f, 0.3f, 0.7f, new Color(0.2f, 0.18f, 0.08f), new Color(0.62f, 0.55f, 0.3f)),
            ("Dark Tufts", 20, 1, 0.2f, 0.12f, 0.2f, 0.4f, 0.25f, 0.5f, new Color(0.03f, 0.08f, 0.04f), new Color(0.12f, 0.28f, 0.12f)),
        };

        [MenuItem("Tools/DigitalStory/Grass Brush")]
        private static void Open()
        {
            GetWindow<GrassBrushWindow>("Grass Brush");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGui;
            Undo.undoRedoPerformed += OnUndoRedo;
            Selection.selectionChanged += PickFromSelection;
            PickFromSelection();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Selection.selectionChanged -= PickFromSelection;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawTypes();

            if (_field != null && _field.Data != null && _field.Material != null)
            {
                DrawBrush();
                DrawLook();
                DrawTools();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTypes()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grass Type", EditorStyles.boldLabel);

            var fields = SceneFields();
            if (fields.Count > 0)
            {
                var names = fields.Select(field => field.name).ToArray();
                var index = Mathf.Max(0, fields.IndexOf(_field));
                var chosen = EditorGUILayout.Popup("Paint With", index, names);
                if (chosen != index || _field == null)
                {
                    ChooseField(fields[chosen]);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Grass Type ▾"))
                {
                    var menu = new GenericMenu();
                    foreach (var preset in Presets)
                    {
                        var presetName = preset.Name;
                        menu.AddItem(new GUIContent(presetName), false, () => CreateType(presetName));
                    }

                    menu.ShowAsContext();
                }

                using (new EditorGUI.DisabledScope(_field == null))
                {
                    if (GUILayout.Button("Rename") && _field != null)
                    {
                        RenamePopup.Open(_field);
                    }
                }
            }

            if (_field == null)
            {
                EditorGUILayout.HelpBox("Make a grass type to start painting.", MessageType.Info);
                return;
            }

            if (_field.Data == null || _field.Material == null)
            {
                EditorGUILayout.HelpBox("This Grass Field is missing its data or material.", MessageType.Warning);
                if (GUILayout.Button("Fix it"))
                {
                    FillField(_field, null);
                }
            }
        }

        private void DrawBrush()
        {
            if (!BeginSection("Brush", ref _showBrush))
            {
                return;
            }

            _painting = GUILayout.Toggle(_painting, _painting ? "Painting (click to stop)" : "Not painting (click to start)", "Button", GUILayout.Height(24));
            if (!IsFieldSelected())
            {
                EditorGUILayout.HelpBox("The brush works while this type's Grass Field is selected, so other tools keep their clicks.", MessageType.Info);
                if (GUILayout.Button("Select It To Paint"))
                {
                    Selection.activeGameObject = _field.gameObject;
                }
            }

            _radius = EditorGUILayout.Slider(new GUIContent("Size", "The brush's radius in metres. Ctrl/Cmd + scroll in the Scene view changes it too."), _radius, 0.2f, 30f);
            using (new EditorGUI.DisabledScope(_field.CoversTerrain))
            {
                _density = EditorGUILayout.Slider(new GUIContent("Density", "Clumps per square metre a stroke paints. A type that covers the terrain uses Cover Density instead."), _density, 0.5f, 150f);
            }

            _falloff = EditorGUILayout.Slider(new GUIContent("Edge Falloff", "0 paints evenly to the edge. Higher thins the grass toward the brush's sides, so strokes blend in."), _falloff, 0f, 1f);
            _eraseAllTypes = EditorGUILayout.Toggle(new GUIContent("Shift Erases All Types", "Off: Shift erases only this type."), _eraseAllTypes);
            _terrainOnly = EditorGUILayout.Toggle(new GUIContent("Terrain Only", "Grass grows only on the terrain, never on top of objects. It still grows up to and under them."), _terrainOnly);
            if (!_terrainOnly)
            {
                _surfaces = LayerMaskField("Paint On", _surfaces);
            }

            EndSection();
        }

        private void DrawLook()
        {
            var serialized = new SerializedObject(_field);
            serialized.Update();

            if (BeginSection("Cover The Whole Terrain", ref _showCover))
            {
                Field(serialized, "_coverTerrain", "Cover Terrain", "Grows this type over the whole terrain inside the area below. Shift + drag erases it, a normal drag brings it back.");
                using (new EditorGUI.DisabledScope(!_field.CoversTerrain))
                {
                    Field(serialized, "_coverDensity", "Density", "Clumps per square metre over the terrain.");
                    Field(serialized, "_coverHalfSize", "Reach", "Metres from the middle to each side of the covered square.");
                    Field(serialized, "_coverCentre", "Middle", "The middle of the covered square, in world X and Z.");
                }

                EndSection();
            }

            if (BeginSection("Height", ref _showHeight))
            {
                var min = serialized.FindProperty("_paintMinSize");
                var max = serialized.FindProperty("_paintMaxSize");
                float low = Mathf.Min(min.floatValue, max.floatValue);
                float high = Mathf.Max(min.floatValue, max.floatValue);
                EditorGUILayout.MinMaxSlider(new GUIContent("Grass Height", "The shortest and tallest clump, in metres."), ref low, ref high, 0.05f, 3f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(" ");
                    low = EditorGUILayout.FloatField(low, GUILayout.MinWidth(40));
                    GUILayout.Label("to", GUILayout.Width(18));
                    high = EditorGUILayout.FloatField(high, GUILayout.MinWidth(40));
                    GUILayout.Label("m", GUILayout.Width(14));
                }

                min.floatValue = Mathf.Clamp(Mathf.Min(low, high), 0.05f, 5f);
                max.floatValue = Mathf.Clamp(Mathf.Max(low, high), 0.05f, 5f);
                Field(serialized, "_heightVariation", "Blade Height Mix", "How much the blades within one clump differ in height.");
                EndSection();
            }

            if (BeginSection("Shape", ref _showShape))
            {
                Field(serialized, "_bladesPerClump", "Blades Per Clump", "More blades makes denser grass, and costs more.");
                Field(serialized, "_bladeSegments", "Segments", "1 makes each blade a single triangle: the most low poly, and the cheapest.");
                Field(serialized, "_bladeWidth", "Blade Width", null);
                Field(serialized, "_clumpSpread", "Clump Spread", "How far a clump's blades spread from its centre.");
                Field(serialized, "_bladeLean", "Lean", "How far each blade leans outward.");
                EndSection();
            }

            if (BeginSection("Colour And Light", ref _showColours))
            {
                Material material = _field.Material;
                ColorProperty(material, "_BaseColor", "Root");
                ColorProperty(material, "_TipColor", "Tip");
                FloatProperty(material, "_ColorVariation", "Patchiness", 0f, 1f);
                FloatProperty(material, "_Brightness", "Brightness", 0f, 3f);
                FloatProperty(material, "_Facets", "Facet Shading", 0f, 1f);
                FloatProperty(material, "_ColorSteps", "Colour Steps", 0f, 6f);
                FloatProperty(material, "_GlowResponse", "Swarm Light", 0f, 4f);
                FloatProperty(material, "_GlowWrap", "Light Through Blades", 0f, 1f);
                EndSection();
            }

            if (BeginSection("Wind", ref _showWind))
            {
                if (!_field.DrivesWind)
                {
                    EditorGUILayout.HelpBox("The wind is shared by all grass. Another type's settings are the ones in use.", MessageType.None);
                }

                Field(serialized, "_windZone", "Wind Zone", "Optional. When set, its direction, Main and Turbulence drive the grass.");
                Field(serialized, "_windDirection", "Direction", null);
                Field(serialized, "_windStrength", "Strength", null);
                Field(serialized, "_flutter", "Flutter", null);
                Field(serialized, "_gustSpeed", "Gust Speed", null);
                Field(serialized, "_gustSize", "Gust Size", null);
                EndSection();
            }

            if (BeginSection("Performance", ref _showPerformance))
            {
                Field(serialized, "_drawDistance", "Draw Distance", "Grass farther than this from the camera is not drawn, in metres.");
                Field(serialized, "_cellSize", "Cell Size", "Grass is drawn in squares this wide, so squares out of view are skipped.");
                Field(serialized, "_followTerrain", "Follow Terrain", "Keeps painted grass on the terrain when it is sculpted.");
                EditorGUILayout.LabelField("Painted clumps", _field.Data.Count.ToString("N0"));
                var triangles = SceneFields().Sum(field => field.TrianglesAtMost);
                EditorGUILayout.LabelField("Triangles, all grass", $"{triangles / 1000000f:0.0} million at most");
                if (triangles > 6000000)
                {
                    EditorGUILayout.HelpBox("A lot of grass for the GPU. Keep Draw Distance near 80, or lower Blades Per Clump or Segments.", MessageType.Warning);
                }

                EndSection();
            }

            if (serialized.ApplyModifiedProperties())
            {
                _field.MarkDirty();
                SceneView.RepaintAll();
            }
        }

        // A titled box that folds. Returns true while it is open; then call EndSection after its fields.
        private static bool BeginSection(string title, ref bool open)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            open = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
            if (!open)
            {
                EditorGUILayout.EndVertical();
                return false;
            }

            EditorGUILayout.Space(2);
            return true;
        }

        private static void EndSection()
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        private static void Field(SerializedObject serialized, string property, string label, string tooltip)
        {
            var field = serialized.FindProperty(property);
            if (field != null)
            {
                EditorGUILayout.PropertyField(field, new GUIContent(label, tooltip));
            }
        }

        private static void ColorProperty(Material material, string property, string label)
        {
            if (!material.HasProperty(property))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.ColorField(label, material.GetColor(property));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(material, "Grass Colour");
                material.SetColor(property, value);
                EditorUtility.SetDirty(material);
            }
        }

        private static void FloatProperty(Material material, string property, string label, float min, float max)
        {
            if (!material.HasProperty(property))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.Slider(label, material.GetFloat(property), min, max);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(material, "Grass Light");
                material.SetFloat(property, value);
                EditorUtility.SetDirty(material);
            }
        }

        private void DrawTools()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Drag in the Scene view to paint.  Shift + drag erases.  Ctrl/Cmd + scroll changes the size.\n" +
                "Turn on the Scene view's Always Refresh to see the wind.",
                MessageType.None);

            if (GUILayout.Button(new GUIContent("Fill Bare Patches", "Grows this type, at the brush's density, on every bare patch inside the grass: round trees and ruins, and holes left by erasing. It does not spread past the grass's outer edge.")))
            {
                Undo.RecordObject(_field.Data, "Fill Bare Patches");
                var added = FillGaps(_field, _density);
                Changed(_field);
                SaveTouched();
                ShowNotification(new GUIContent($"Grew {added:N0} clumps"));
            }

            if (GUILayout.Button("Remove Grass That Is On Top Of Objects"))
            {
                Undo.RecordObject(_field.Data, "Remove Grass Off Terrain");
                var removed = _field.Data.RemoveWhere(position => IsOffTerrain(position));
                Changed(_field);
                SaveTouched();
                ShowNotification(new GUIContent($"Removed {removed} clumps"));
            }

            if (GUILayout.Button("Clear This Type") &&
                EditorUtility.DisplayDialog("Clear grass", $"Remove every clump of {_field.name}?", "Clear", "Cancel"))
            {
                Undo.RecordObject(_field.Data, "Clear Grass");
                _field.Data.Clear();
                Changed(_field);
                SaveTouched();
            }
        }

        private void OnSceneGui(SceneView sceneView)
        {
            if (!_painting || _field == null || _field.Data == null || _field.Material == null || !IsFieldSelected())
            {
                _hasHit = false;
                return;
            }

            var current = Event.current;
            var control = GUIUtility.GetControlID(FocusType.Passive);

            if (current.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(control);
            }

            if (current.type == EventType.ScrollWheel && (current.control || current.command))
            {
                _radius = Mathf.Clamp(_radius * (current.delta.y > 0 ? 0.9f : 1.1f), 0.2f, 30f);
                current.Use();
                Repaint();
            }

            if (current.type == EventType.MouseMove || current.type == EventType.MouseDrag || current.type == EventType.MouseDown)
            {
                _hasHit = FindSurface(HandleUtility.GUIPointToWorldRay(current.mousePosition), out _hitPoint, out _hitNormal);
                sceneView.Repaint();
            }

            if (_hasHit && current.type == EventType.Repaint)
            {
                var colour = current.shift ? new Color(1f, 0.35f, 0.3f, 0.9f) : new Color(0.5f, 1f, 0.4f, 0.9f);
                Handles.color = colour;
                Handles.DrawWireDisc(_hitPoint, _hitNormal, _radius);
                Handles.color = colour * new Color(1f, 1f, 1f, 0.12f);
                Handles.DrawSolidDisc(_hitPoint, _hitNormal, _radius);
                if (_falloff > 0.01f && !current.shift)
                {
                    // The inner ring is where the grass starts to thin.
                    Handles.color = colour * new Color(1f, 1f, 1f, 0.6f);
                    Handles.DrawWireDisc(_hitPoint, _hitNormal, _radius * (1f - _falloff));
                }
            }

            if (current.button != 0 || current.alt || !_hasHit)
            {
                return;
            }

            if (current.type == EventType.MouseUp)
            {
                SaveTouched();
                return;
            }

            if (current.type == EventType.MouseDown)
            {
                Stroke(current.shift);
                _lastStroke = _hitPoint;
                current.Use();
            }
            else if (current.type == EventType.MouseDrag)
            {
                if (Vector3.Distance(_hitPoint, _lastStroke) > _radius * 0.3f)
                {
                    Stroke(current.shift);
                    _lastStroke = _hitPoint;
                }

                current.Use();
            }
        }

        private void Stroke(bool erase)
        {
            // A type that covers the terrain: the brush edits where it is missing, and erasing also takes painted clumps.
            if (_field.CoversTerrain)
            {
                Undo.RecordObject(_field.Data, erase ? "Erase Grass" : "Restore Grass");
                _field.PaintCover(_hitPoint, _radius, erase, Weight);
                if (erase)
                {
                    _field.Data.RemoveInRadius(_hitPoint, _radius);
                    _field.MarkDirty();
                }

                EditorUtility.SetDirty(_field.Data);
                SceneView.RepaintAll();
                return;
            }

            if (erase)
            {
                var targets = _eraseAllTypes ? SceneFields() : new List<GrassField> { _field };
                foreach (var target in targets)
                {
                    if (target.Data == null)
                    {
                        continue;
                    }

                    Undo.RecordObject(target.Data, "Erase Grass");
                    target.Data.RemoveInRadius(_hitPoint, _radius);
                    Changed(target);
                }

                return;
            }

            var data = _field.Data;
            Undo.RecordObject(data, "Paint Grass");

            // With falloff the brush is full in its inner part and thins to nothing at its rim, so it holds fewer clumps.
            var inner = 1f - _falloff;
            var fill = inner * inner + (1f - inner * inner) * 0.5f;
            var wanted = Mathf.RoundToInt(_density * Mathf.PI * _radius * _radius * fill);
            var missing = wanted - data.CountInRadius(_hitPoint, _radius);
            var attempts = Mathf.Max(0, missing) * 4;
            var minSize = _field.PaintMinSize;
            var maxSize = _field.PaintMaxSize;

            while (missing > 0 && attempts-- > 0)
            {
                var flat = Random.insideUnitCircle * _radius;
                if (Random.value > Weight(flat.magnitude / _radius))
                {
                    continue;
                }

                var spot = _hitPoint + new Vector3(flat.x, 0f, flat.y);
                Vector3 ground;
                if (_terrainOnly)
                {
                    if (!TryTerrainGround(spot, out ground))
                    {
                        continue;
                    }
                }
                else
                {
                    var above = spot + Vector3.up * (_radius + 5f);
                    if (!Physics.Raycast(above, Vector3.down, out var hit, _radius * 2f + 10f, _surfaces, QueryTriggerInteraction.Ignore) ||
                        Vector3.Dot(hit.normal, Vector3.up) < 0.5f)
                    {
                        continue;
                    }

                    ground = hit.point;
                }

                data.Add(ground, Random.Range(0f, 360f), Random.Range(minSize, maxSize));
                missing--;
            }

            Changed(_field);
        }

        // 1 inside the inner ring, easing down to 0 at the rim.
        private float Weight(float distance01)
        {
            var inner = 1f - _falloff;
            if (distance01 <= inner || _falloff <= 0.001f)
            {
                return 1f;
            }

            var t = Mathf.Clamp01((distance01 - inner) / _falloff);
            return 1f - (t * t * (3f - 2f * t));
        }

        /// <summary>
        /// Finds the terrain's surface under a spot. Grass goes on the terrain itself, never on top of an object, but it does
        /// grow right up to and under trees, ruins and rocks, so they stand in the grass instead of in a bare circle.
        /// </summary>
        private static bool TryTerrainGround(Vector3 spot, out Vector3 ground)
        {
            ground = default;
            var top = new Vector3(spot.x, spot.y + 500f, spot.z);
            var hits = Physics.RaycastAll(top, Vector3.down, 1500f, ~0, QueryTriggerInteraction.Ignore);

            var terrainY = float.NegativeInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider is TerrainCollider && hit.point.y > terrainY)
                {
                    terrainY = hit.point.y;
                }
            }

            if (float.IsNegativeInfinity(terrainY))
            {
                return false;
            }

            ground = new Vector3(spot.x, terrainY, spot.z);
            return true;
        }

        /// <summary>
        /// Grows grass on bare patches that grass surrounds. The field is sampled on a jittered grid at the density's spacing;
        /// a spot gets a clump when no clump of any type is within half a spacing of it and grass grows in most of the 5 m
        /// squares around it, so holes fill in while the open ground beyond the grass's edge stays open.
        /// </summary>
        private static int FillGaps(GrassField field, float density)
        {
            const float GapReach = 5f;
            var spacing = 1f / Mathf.Sqrt(Mathf.Max(0.1f, density));
            var cell = Mathf.Max(spacing, 1f);

            var cells = new Dictionary<Vector2Int, List<Vector2>>();
            var coarse = new HashSet<Vector2Int>();
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var other in SceneFields())
            {
                if (other.Data == null)
                {
                    continue;
                }

                for (var i = 0; i < other.Data.Count; i++)
                {
                    var position = other.Data.GetPosition(i);
                    var flat = new Vector2(position.x, position.z);
                    var key = new Vector2Int(Mathf.FloorToInt(flat.x / cell), Mathf.FloorToInt(flat.y / cell));
                    if (!cells.TryGetValue(key, out var list))
                    {
                        list = new List<Vector2>();
                        cells.Add(key, list);
                    }

                    list.Add(flat);
                    coarse.Add(new Vector2Int(Mathf.FloorToInt(flat.x / GapReach), Mathf.FloorToInt(flat.y / GapReach)));
                    min = Vector2.Min(min, flat);
                    max = Vector2.Max(max, flat);
                }
            }

            if (cells.Count == 0)
            {
                return 0;
            }

            // Quick: is there grass in this 5 m square or one beside it? Close enough to tell a hole from the open beyond.
            bool InsideGrass(Vector2 point)
            {
                // Grass in most of the squares around it: a patch inside the grass, even a wide one, not the open beyond.
                var home = new Vector2Int(Mathf.FloorToInt(point.x / GapReach), Mathf.FloorToInt(point.y / GapReach));
                var around = 0;
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        around += coarse.Contains(new Vector2Int(home.x + dx, home.y + dy)) ? 1 : 0;
                    }
                }

                return around >= 6;
            }

            bool Near(Vector2 point, float reach)
            {
                var span = Mathf.CeilToInt(reach / cell);
                var home = new Vector2Int(Mathf.FloorToInt(point.x / cell), Mathf.FloorToInt(point.y / cell));
                var reachSquared = reach * reach;
                for (var dx = -span; dx <= span; dx++)
                {
                    for (var dy = -span; dy <= span; dy++)
                    {
                        if (!cells.TryGetValue(new Vector2Int(home.x + dx, home.y + dy), out var list))
                        {
                            continue;
                        }

                        foreach (var other in list)
                        {
                            if ((other - point).sqrMagnitude <= reachSquared)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            var added = 0;
            var data = field.Data;
            for (var x = min.x; x <= max.x; x += spacing)
            {
                for (var z = min.y; z <= max.y; z += spacing)
                {
                    var point = new Vector2(x + Random.Range(-0.4f, 0.4f) * spacing, z + Random.Range(-0.4f, 0.4f) * spacing);
                    if (!InsideGrass(point) || Near(point, spacing * 0.5f))
                    {
                        continue;
                    }

                    if (!TryTerrainGround(new Vector3(point.x, 0f, point.y), out var ground))
                    {
                        continue;
                    }

                    data.Add(ground, Random.Range(0f, 360f), Random.Range(field.PaintMinSize, field.PaintMaxSize));
                    var key = new Vector2Int(Mathf.FloorToInt(point.x / cell), Mathf.FloorToInt(point.y / cell));
                    if (!cells.TryGetValue(key, out var list))
                    {
                        list = new List<Vector2>();
                        cells.Add(key, list);
                    }

                    list.Add(point);
                    added++;
                }
            }

            return added;
        }

        // True for grass that is not on the terrain's surface, such as grass painted on top of an object.
        private static bool IsOffTerrain(Vector3 position)
        {
            return !TryTerrainGround(position, out var ground) || Mathf.Abs(ground.y - position.y) > 0.1f;
        }

        private bool FindSurface(Ray ray, out Vector3 point, out Vector3 normal)
        {
            if (Physics.Raycast(ray, out var hit, 5000f, ~0, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                normal = hit.normal;
                return true;
            }

            point = default;
            normal = Vector3.up;
            return false;
        }

        /// <summary>Removes, from every grass type, the grass on top of objects. Other tools call it after moving objects.</summary>
        internal static void RemoveGrassUnderObjects()
        {
            foreach (var field in SceneFields())
            {
                if (field.Data == null)
                {
                    continue;
                }

                Undo.RecordObject(field.Data, "Remove Grass Under Objects");
                if (field.Data.RemoveWhere(position => IsOffTerrain(position)) > 0)
                {
                    EditorUtility.SetDirty(field.Data);
                    field.MarkDirty();
                }
            }

            SaveTouched();
        }

        /// <summary>Writes the painted grass to disk at the end of every stroke, so a crash cannot lose it.</summary>
        private static void SaveTouched()
        {
            foreach (var field in SceneFields())
            {
                if (field.Data != null)
                {
                    AssetDatabase.SaveAssetIfDirty(field.Data);
                }
            }
        }

        private void Changed(GrassField field)
        {
            EditorUtility.SetDirty(field.Data);
            field.MarkDirty();
            SceneView.RepaintAll();
            Repaint();
        }

        private void OnUndoRedo()
        {
            foreach (var field in Resources.FindObjectsOfTypeAll<GrassField>())
            {
                field.MarkDirty();
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private bool IsFieldSelected()
        {
            return _field != null && Selection.activeGameObject == _field.gameObject;
        }

        private void PickFromSelection()
        {
            var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<GrassField>() : null;
            if (selected != null && selected != _field)
            {
                _field = selected;
            }

            Repaint();
            SceneView.RepaintAll();
        }

        private void ChooseField(GrassField field)
        {
            _field = field;
            Selection.activeGameObject = field.gameObject;
        }

        private static List<GrassField> SceneFields()
        {
            return Resources.FindObjectsOfTypeAll<GrassField>()
                .Where(field => field.gameObject.scene.IsValid())
                .OrderBy(field => field.name)
                .ToList();
        }

        private void CreateType(string presetName)
        {
            var preset = Presets.First(item => item.Name == presetName);
            var parent = SceneFields().FirstOrDefault();

            var go = new GameObject(UniqueName(preset.Name));
            Undo.RegisterCreatedObjectUndo(go, "Create Grass Type");
            if (parent != null && parent.transform.parent != null)
            {
                go.transform.SetParent(parent.transform.parent, false);
            }

            var field = go.AddComponent<GrassField>();
            FillField(field, preset.Name);

            var serialized = new SerializedObject(field);
            serialized.FindProperty("_bladesPerClump").intValue = preset.Blades;
            serialized.FindProperty("_bladeSegments").intValue = preset.Segments;
            serialized.FindProperty("_bladeWidth").floatValue = preset.Width;
            serialized.FindProperty("_clumpSpread").floatValue = preset.Spread;
            serialized.FindProperty("_bladeLean").floatValue = preset.Lean;
            serialized.FindProperty("_heightVariation").floatValue = preset.Variation;
            serialized.FindProperty("_paintMinSize").floatValue = preset.Min;
            serialized.FindProperty("_paintMaxSize").floatValue = preset.Max;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            field.Material.SetColor("_BaseColor", preset.Root);
            field.Material.SetColor("_TipColor", preset.Tip);
            EditorUtility.SetDirty(field.Material);
            AssetDatabase.SaveAssets();

            EditorSceneManager.MarkSceneDirty(go.scene);
            ChooseField(field);
        }

        private static string UniqueName(string name)
        {
            var taken = new HashSet<string>(SceneFields().Select(field => field.name));
            if (!taken.Contains(name))
            {
                return name;
            }

            for (var i = 2; ; i++)
            {
                var candidate = $"{name} {i}";
                if (!taken.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        /// <summary>Gives a field its data asset and, for a new type, its own copy of the grass material.</summary>
        private static void FillField(GrassField field, string typeName)
        {
            Undo.RecordObject(field, "Set Up Grass Field");
            if (field.Material == null)
            {
                var shared = LoadOrCreateMaterial();
                if (typeName == null)
                {
                    field.Material = shared;
                }
                else
                {
                    Directory.CreateDirectory(TypesFolder);
                    var path = AssetDatabase.GenerateUniqueAssetPath($"{TypesFolder}/{field.name}.mat");
                    var copy = new Material(shared) { enableInstancing = true };
                    AssetDatabase.CreateAsset(copy, path);
                    field.Material = copy;
                }
            }

            if (field.Data == null)
            {
                Directory.CreateDirectory(DataFolder);
                var sceneName = string.IsNullOrEmpty(field.gameObject.scene.name) ? "Untitled" : field.gameObject.scene.name;
                var path = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/{sceneName}-{field.name}.asset");
                var data = CreateInstance<GrassData>();
                AssetDatabase.CreateAsset(data, path);
                field.Data = data;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(field);
            field.MarkDirty();
        }

        private static Material LoadOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("DigitalStory/WindGrass")) { enableInstancing = true };
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static LayerMask LayerMaskField(string label, LayerMask mask)
        {
            var names = UnityEditorInternal.InternalEditorUtility.layers;
            var bits = 0;
            for (var i = 0; i < names.Length; i++)
            {
                if ((mask.value & (1 << LayerMask.NameToLayer(names[i]))) != 0)
                {
                    bits |= 1 << i;
                }
            }

            bits = EditorGUILayout.MaskField(label, bits, names);
            var result = 0;
            for (var i = 0; i < names.Length; i++)
            {
                if ((bits & (1 << i)) != 0)
                {
                    result |= 1 << LayerMask.NameToLayer(names[i]);
                }
            }

            return bits == -1 ? (LayerMask)~0 : (LayerMask)result;
        }

        /// <summary>A small window that renames a grass type.</summary>
        private sealed class RenamePopup : EditorWindow
        {
            private GrassField _target;
            private string _name;

            public static void Open(GrassField target)
            {
                var window = CreateInstance<RenamePopup>();
                window._target = target;
                window._name = target.name;
                window.titleContent = new GUIContent("Rename Grass Type");
                window.ShowUtility();
                window.minSize = window.maxSize = new Vector2(300, 70);
            }

            private void OnGUI()
            {
                _name = EditorGUILayout.TextField("Name", _name);
                if (GUILayout.Button("Rename") && _target != null && !string.IsNullOrWhiteSpace(_name))
                {
                    Undo.RecordObject(_target.gameObject, "Rename Grass Type");
                    _target.gameObject.name = _name.Trim();
                    Close();
                }
            }
        }
    }
}
