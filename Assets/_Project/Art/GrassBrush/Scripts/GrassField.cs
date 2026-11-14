using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.GrassBrush
{
    /// <summary>
    /// Draws the grass painted into a GrassData asset with GPU instancing, and drives the wind that bends it.
    /// Paint it with Tools > DigitalStory > Grass Brush. The wind comes from the Wind Zone if one is set,
    /// otherwise from the settings below. It also moves in the Editor; turn on the Scene view's
    /// "Always Refresh" to see it there.
    /// </summary>
    [ExecuteAlways]
    public sealed class GrassField : MonoBehaviour
    {
        private const int MaxBatch = 1023;

        private static readonly int WindDirId = Shader.PropertyToID("_GrassWindDir");
        private static readonly int WindStrengthId = Shader.PropertyToID("_GrassWindStrength");
        private static readonly int WindSpeedId = Shader.PropertyToID("_GrassWindSpeed");
        private static readonly int WindWaveScaleId = Shader.PropertyToID("_GrassWindWaveScale");
        private static readonly int FlutterId = Shader.PropertyToID("_GrassFlutter");

        // The wind is shared by every grass type, so one field drives it: the first one enabled.
        private static GrassField _windOwner;

        [Header("Grass")]
        [SerializeField] private GrassData _data;
        [SerializeField] private Material _material;

        [Tooltip("Keeps the grass on a Terrain's surface when the Terrain is sculpted.")]
        [SerializeField] private bool _followTerrain = true;

        [Header("Cover The Terrain")]
        [Tooltip("Grows this type over the whole terrain inside the area below, at the cover density. The brush then erases it where you want (Shift) and brings it back (a normal stroke).")]
        [SerializeField] private bool _coverTerrain;
        [Tooltip("Clumps per square metre over the terrain.")]
        [SerializeField, Range(0.5f, 40f)] private float _coverDensity = 8f;
        [Tooltip("The middle of the covered square, in world X and Z.")]
        [SerializeField] private Vector2 _coverCentre = Vector2.zero;
        [Tooltip("Metres from the middle to each side of the covered square.")]
        [SerializeField, Range(10f, 500f)] private float _coverHalfSize = 115f;
        [Tooltip("No covering grass grows where the ground is lower than this height, such as in a river.")]
        [SerializeField] private float _noGrassBelow = -100f;

        [Header("Painting")]
        [Tooltip("The smallest clump this type paints, in metres.")]
        [SerializeField, Range(0.05f, 5f)] private float _paintMinSize = 0.35f;
        [Tooltip("The largest clump this type paints, in metres.")]
        [SerializeField, Range(0.05f, 5f)] private float _paintMaxSize = 0.7f;

        [Header("Low Poly Look")]
        [Tooltip("How many blades grow from each clump. More blades means denser grass.")]
        [SerializeField, Range(1, 40)] private int _bladesPerClump = 7;
        [Tooltip("1 makes each blade a single triangle, the most low poly look.")]
        [SerializeField, Range(1, 3)] private int _bladeSegments = 1;
        [SerializeField, Range(0.02f, 0.4f)] private float _bladeWidth = 0.14f;
        [Tooltip("How far the clump's blades spread from its centre.")]
        [SerializeField, Range(0f, 0.6f)] private float _clumpSpread = 0.18f;
        [Tooltip("How far each blade leans outward.")]
        [SerializeField, Range(0f, 0.8f)] private float _bladeLean = 0.25f;
        [Tooltip("How much the blades in a clump differ in height.")]
        [SerializeField, Range(0f, 0.9f)] private float _heightVariation = 0.45f;

        [Header("Performance")]
        [Tooltip("Grass farther than this from the camera is not drawn, in metres.")]
        [SerializeField, Range(10f, 400f)] private float _drawDistance = 80f;
        [Tooltip("The grass is drawn in square cells this wide, so the cells out of view or out of reach are skipped.")]
        [SerializeField, Range(4f, 64f)] private float _cellSize = 16f;

        [Header("Wind")]
        [Tooltip("Optional. When set, its direction, Main and Turbulence drive the grass.")]
        [SerializeField] private WindZone _windZone;
        [SerializeField] private Vector3 _windDirection = new Vector3(1f, 0f, 0.3f);
        [SerializeField, Range(0f, 2f)] private float _windStrength = 0.6f;
        [SerializeField, Range(0f, 2f)] private float _flutter = 0.4f;
        [Tooltip("How fast the gusts travel across the field.")]
        [SerializeField, Range(0f, 10f)] private float _gustSpeed = 2f;
        [Tooltip("How close together the gusts are. Higher means smaller, busier waves.")]
        [SerializeField, Range(0.05f, 2f)] private float _gustSize = 0.35f;

        private readonly List<Cell> _cells = new List<Cell>();
        private readonly Plane[] _frustum = new Plane[6];
        private int _builtVersion = -1;
        private GrassData _builtData;
        private Mesh _clumpMesh;
        private readonly Dictionary<Vector2Int, Cell> _coverCells = new Dictionary<Vector2Int, Cell>();
        private int _coverSettingsHash;
        private int _builtMaskVersion = -1;
        private const float MaskCell = 0.5f;
        private bool _meshDirty;

        public GrassData Data
        {
            get => _data;
            set => _data = value;
        }

        public Material Material
        {
            get => _material;
            set => _material = value;
        }

        /// <summary>Triangles drawn if every clump were in view: what the GPU pays at worst.</summary>
        public long TrianglesAtMost
        {
            get
            {
                long clumps = _data != null ? _data.Count : 0;
                if (_coverTerrain)
                {
                    clumps += (long)(_coverHalfSize * _coverHalfSize * 4f * _coverDensity);
                }

                return clumps * Mathf.Max(1, _bladesPerClump) * (2 * Mathf.Max(1, _bladeSegments) - 1);
            }
        }

        /// <summary>True while this type grows over the whole terrain and the brush edits where it is missing.</summary>
        public bool CoversTerrain => _coverTerrain;

        /// <summary>Turns covering the terrain on or off, with its density and area.</summary>
        /// <summary>Stops the covering grass growing below a height, such as under water. Regrows it.</summary>
        public void SetNoGrassBelow(float height)
        {
            _noGrassBelow = height;
            _coverCells.Clear();
        }

        public void SetCover(bool cover, float density, Vector2 centre, float halfSize)
        {
            _coverTerrain = cover;
            _coverDensity = Mathf.Clamp(density, 0.5f, 40f);
            _coverCentre = centre;
            _coverHalfSize = Mathf.Clamp(halfSize, 10f, 500f);
            _coverCells.Clear();
        }

        /// <summary>
        /// Erases the covering grass in a circle, or brings it back. Only the cells the circle touches are grown again.
        /// <paramref name="weight"/> gives the brush's falloff: the chance a spot changes, from 0 at the centre to 1 at the rim.
        /// </summary>
        public void PaintCover(Vector3 centre, float radius, bool erase, System.Func<float, float> weight)
        {
            if (!_coverTerrain || _data == null)
            {
                return;
            }

            _data.EnsureMask(_coverCentre, _coverHalfSize, MaskCell);
            _data.SetErased(new Vector2(centre.x, centre.z), radius, erase, weight);
            ForgetCoverCells(centre.x - radius, centre.x + radius, centre.z - radius, centre.z + radius);
        }

        public float PaintMinSize => Mathf.Min(_paintMinSize, _paintMaxSize);

        public float PaintMaxSize => Mathf.Max(_paintMinSize, _paintMaxSize);

        /// <summary>True for the field whose wind settings move all the grass.</summary>
        public bool DrivesWind => _windOwner == this;

        private void OnEnable()
        {
            Camera.onPreCull += DrawForCamera;
            TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= DrawForCamera;
            TerrainCallbacks.heightmapChanged -= OnHeightmapChanged;
            DestroyMesh();
            if (_windOwner == this)
            {
                _windOwner = null;
            }
        }

        private void OnValidate()
        {
            // The look settings change the clump's shape, and the cell size the cells, so both are rebuilt on the next draw.
            _meshDirty = true;
            _builtVersion = -1;
        }

        private void Update()
        {
            if (_windOwner == null || !_windOwner.isActiveAndEnabled)
            {
                _windOwner = this;
            }

            if (_windOwner == this)
            {
                PushWind();
            }
        }

        /// <summary>Rebuilds the draw data on the next frame. The brush calls it after painting.</summary>
        public void MarkDirty()
        {
            _builtVersion = -1;
        }

        /// <summary>
        /// Puts every clump that stands on the terrain back onto its surface. Clumps that stand on something
        /// else above the terrain, such as a rock or a ruin, stay where they are. Returns how many moved.
        /// </summary>
        public int SnapToTerrain(Terrain terrain)
        {
            var resolution = terrain.terrainData.heightmapResolution;
            return SnapToTerrain(terrain, new RectInt(0, 0, resolution, resolution));
        }

        private void OnHeightmapChanged(Terrain terrain, RectInt region, bool synched)
        {
            if (_coverTerrain)
            {
                var origin = terrain.GetPosition();
                var size = terrain.terrainData.size;
                var step = 1f / (terrain.terrainData.heightmapResolution - 1);
                ForgetCoverCells(
                    origin.x + ((region.xMin - 1) * step * size.x), origin.x + ((region.xMax + 1) * step * size.x),
                    origin.z + ((region.yMin - 1) * step * size.z), origin.z + ((region.yMax + 1) * step * size.z));
            }

            if (_followTerrain && _data != null && _data.Count > 0)
            {
                SnapToTerrain(terrain, region);
            }
        }

        private int SnapToTerrain(Terrain terrain, RectInt region)
        {
            if (_data == null)
            {
                return 0;
            }

            var terrainData = terrain.terrainData;
            var origin = terrain.GetPosition();
            var size = terrainData.size;
            var step = 1f / (terrainData.heightmapResolution - 1);
            var minX = origin.x + (region.xMin - 1) * step * size.x;
            var maxX = origin.x + (region.xMax + 1) * step * size.x;
            var minZ = origin.z + (region.yMin - 1) * step * size.z;
            var maxZ = origin.z + (region.yMax + 1) * step * size.z;

            var moved = 0;
            for (var i = 0; i < _data.Count; i++)
            {
                var position = _data.GetPosition(i);
                if (position.x < minX || position.x > maxX || position.z < minZ || position.z > maxZ)
                {
                    continue;
                }

                var height = terrain.SampleHeight(position) + origin.y;
                if (Mathf.Abs(position.y - height) < 0.001f)
                {
                    continue;
                }

                var above = new Vector3(position.x, Mathf.Max(height, position.y) + 100f, position.z);
                if (Physics.Raycast(above, Vector3.down, out var hit, 300f, ~0, QueryTriggerInteraction.Ignore) &&
                    !(hit.collider is TerrainCollider) && hit.point.y > height + 0.05f &&
                    Mathf.Abs(hit.point.y - position.y) < 0.05f)
                {
                    continue;
                }

                position.y = height;
                _data.SetPositionSilently(i, position);
                moved++;
            }

            if (moved > 0)
            {
                _data.MarkChanged();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(_data);
#endif
            }

            return moved;
        }

        private void PushWind()
        {
            var direction = _windDirection;
            var strength = _windStrength;
            var flutter = _flutter;
            if (_windZone != null)
            {
                direction = _windZone.transform.forward;
                strength = _windZone.windMain * 0.6f;
                flutter = _windZone.windTurbulence * 0.4f;
            }

            direction.y = 0f;
            Shader.SetGlobalVector(WindDirId, direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right);
            Shader.SetGlobalFloat(WindStrengthId, strength);
            Shader.SetGlobalFloat(WindSpeedId, _gustSpeed);
            Shader.SetGlobalFloat(WindWaveScaleId, _gustSize);
            Shader.SetGlobalFloat(FlutterId, flutter);
        }

        private void DrawForCamera(Camera camera)
        {
            if (_data == null || _material == null || (_data.Count == 0 && !_coverTerrain) || camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
            {
                return;
            }

            if ((camera.cullingMask & (1 << gameObject.layer)) == 0)
            {
                return;
            }

            Rebuild();
            GeometryUtility.CalculateFrustumPlanes(camera, _frustum);
            var eye = camera.transform.position;
            var reachSquared = _drawDistance * _drawDistance;
            var mesh = GetClumpMesh();

            if (_coverTerrain)
            {
                DrawCover(camera, mesh, eye, reachSquared);
            }

            foreach (var cell in _cells)
            {
                if (cell.Bounds.SqrDistance(eye) > reachSquared || !GeometryUtility.TestPlanesAABB(_frustum, cell.Bounds))
                {
                    continue;
                }

                var renderParams = new RenderParams(_material)
                {
                    camera = camera,
                    layer = gameObject.layer,
                    worldBounds = cell.Bounds,
                    shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                    receiveShadows = false,
                };

                for (var start = 0; start < cell.Count; start += MaxBatch)
                {
                    var count = Mathf.Min(MaxBatch, cell.Count - start);
                    Graphics.RenderMeshInstanced(renderParams, mesh, 0, cell.Matrices, count, start);
                }
            }
        }

        private void Rebuild()
        {
            // The covering grass regrows when its settings change, or when undo or redo replace the erase mask.
            var settings = (_coverDensity, _coverCentre, _coverHalfSize, _cellSize, _paintMinSize, _paintMaxSize, _coverTerrain, _noGrassBelow).GetHashCode();
            if (settings != _coverSettingsHash || _builtMaskVersion != _data.MaskVersion || _builtData != _data)
            {
                _coverSettingsHash = settings;
                _builtMaskVersion = _data.MaskVersion;
                _coverCells.Clear();
            }

            if (_builtData == _data && _builtVersion == _data.Version)
            {
                return;
            }

            // Sort the clumps into square cells, each with its own bounds, so whole cells can be skipped.
            var buckets = new Dictionary<Vector2Int, List<int>>();
            for (var i = 0; i < _data.Count; i++)
            {
                var position = _data.GetPosition(i);
                var key = new Vector2Int(Mathf.FloorToInt(position.x / _cellSize), Mathf.FloorToInt(position.z / _cellSize));
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    buckets.Add(key, list);
                }

                list.Add(i);
            }

            _cells.Clear();
            foreach (var bucket in buckets.Values)
            {
                var matrices = new Matrix4x4[bucket.Count];
                var min = Vector3.positiveInfinity;
                var max = Vector3.negativeInfinity;
                for (var j = 0; j < bucket.Count; j++)
                {
                    var i = bucket[j];
                    var position = _data.GetPosition(i);
                    var size = _data.GetSize(i);
                    matrices[j] = Matrix4x4.TRS(position, Quaternion.Euler(0f, _data.GetTurn(i), 0f), Vector3.one * size);
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position + Vector3.up * size);
                }

                var bounds = new Bounds();
                bounds.SetMinMax(min - Vector3.one, max + Vector3.one);
                _cells.Add(new Cell(matrices, bounds));
            }

            _builtData = _data;
            _builtVersion = _data.Version;
        }

        // Grows each cover cell in reach the first time it is needed, then draws the ones in view.
        private void DrawCover(Camera camera, Mesh mesh, Vector3 eye, float reachSquared)
        {
            var reach = Mathf.Sqrt(reachSquared);
            var min = new Vector2(Mathf.Max(eye.x - reach, _coverCentre.x - _coverHalfSize), Mathf.Max(eye.z - reach, _coverCentre.y - _coverHalfSize));
            var max = new Vector2(Mathf.Min(eye.x + reach, _coverCentre.x + _coverHalfSize), Mathf.Min(eye.z + reach, _coverCentre.y + _coverHalfSize));
            if (min.x > max.x || min.y > max.y)
            {
                return;
            }

            var terrain = Terrain.activeTerrain;
            for (var cx = Mathf.FloorToInt(min.x / _cellSize); cx <= Mathf.FloorToInt(max.x / _cellSize); cx++)
            {
                for (var cz = Mathf.FloorToInt(min.y / _cellSize); cz <= Mathf.FloorToInt(max.y / _cellSize); cz++)
                {
                    var key = new Vector2Int(cx, cz);
                    if (!_coverCells.TryGetValue(key, out var cell))
                    {
                        cell = BuildCoverCell(key, terrain);
                        _coverCells.Add(key, cell);
                    }

                    if (cell.Count == 0 || cell.Bounds.SqrDistance(eye) > reachSquared || !GeometryUtility.TestPlanesAABB(_frustum, cell.Bounds))
                    {
                        continue;
                    }

                    var renderParams = new RenderParams(_material)
                    {
                        camera = camera,
                        layer = gameObject.layer,
                        worldBounds = cell.Bounds,
                        shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                        receiveShadows = false,
                    };

                    for (var start = 0; start < cell.Count; start += MaxBatch)
                    {
                        Graphics.RenderMeshInstanced(renderParams, mesh, 0, cell.Matrices, Mathf.Min(MaxBatch, cell.Count - start), start);
                    }
                }
            }
        }

        // The covering grass of one cell: a jittered grid at the cover density, the same every time for the same cell,
        // standing on the terrain, inside the covered square and not where the brush erased it.
        private Cell BuildCoverCell(Vector2Int key, Terrain terrain)
        {
            var random = new System.Random(unchecked((key.x * 73856093) ^ (key.y * 19349663) ^ 0x5bd1e995));
            var spacing = 1f / Mathf.Sqrt(Mathf.Max(0.05f, _coverDensity));
            var cellMin = new Vector2(key.x * _cellSize, key.y * _cellSize);
            var area = new Rect(_coverCentre - (Vector2.one * _coverHalfSize), Vector2.one * (_coverHalfSize * 2f));
            var origin = terrain != null ? terrain.GetPosition() : Vector3.zero;
            var minSize = PaintMinSize;
            var maxSize = PaintMaxSize;

            var matrices = new List<Matrix4x4>();
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            for (var x = 0f; x < _cellSize; x += spacing)
            {
                for (var z = 0f; z < _cellSize; z += spacing)
                {
                    var px = cellMin.x + x + ((float)random.NextDouble() * spacing);
                    var pz = cellMin.y + z + ((float)random.NextDouble() * spacing);
                    var turn = (float)random.NextDouble() * 360f;
                    var size = Mathf.Lerp(minSize, maxSize, (float)random.NextDouble());
                    if (!area.Contains(new Vector2(px, pz)) || _data.IsErased(px, pz))
                    {
                        continue;
                    }

                    var position = new Vector3(px, 0f, pz);
                    position.y = terrain != null ? terrain.SampleHeight(position) + origin.y : 0f;
                    if (position.y < _noGrassBelow)
                    {
                        continue;
                    }

                    matrices.Add(Matrix4x4.TRS(position, Quaternion.Euler(0f, turn, 0f), Vector3.one * size));
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position + (Vector3.up * size));
                }
            }

            var bounds = new Bounds();
            if (matrices.Count > 0)
            {
                bounds.SetMinMax(min - Vector3.one, max + Vector3.one);
            }

            return new Cell(matrices.ToArray(), bounds);
        }

        private void ForgetCoverCells(float minX, float maxX, float minZ, float maxZ)
        {
            for (var cx = Mathf.FloorToInt(minX / _cellSize); cx <= Mathf.FloorToInt(maxX / _cellSize); cx++)
            {
                for (var cz = Mathf.FloorToInt(minZ / _cellSize); cz <= Mathf.FloorToInt(maxZ / _cellSize); cz++)
                {
                    _coverCells.Remove(new Vector2Int(cx, cz));
                }
            }
        }

        /// <summary>
        /// One low poly clump: flat, pointed blades fanned around the centre, each one metre tall at most before
        /// scaling. With one segment a blade is a single triangle. uv.y runs from 0 at the root to 1 at the tip,
        /// which the shader uses to bend the blade, and uv.x holds a number per blade for its colour.
        /// </summary>
        private Mesh GetClumpMesh()
        {
            if (_meshDirty)
            {
                DestroyMesh();
                _meshDirty = false;
            }

            if (_clumpMesh != null)
            {
                return _clumpMesh;
            }

            var blades = Mathf.Max(1, _bladesPerClump);
            var segments = Mathf.Max(1, _bladeSegments);
            var perBlade = segments * 2 + 1;
            var vertices = new Vector3[blades * perBlade];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[blades * ((segments - 1) * 6 + 3)];
            var random = new System.Random(1234);
            var v = 0;
            var t = 0;

            for (var b = 0; b < blades; b++)
            {
                // Spread the blades evenly around the clump, with a little jitter, so it reads as a tuft.
                var angle = (b + (float)random.NextDouble() * 0.6f) / blades * 360f;
                var turn = Quaternion.Euler(0f, angle, 0f);
                var distance = _clumpSpread * Mathf.Sqrt((float)random.NextDouble());
                var root = turn * new Vector3(0f, 0f, distance);
                var height = 1f - _heightVariation * (float)random.NextDouble();
                var width = _bladeWidth * (0.75f + 0.5f * (float)random.NextDouble());
                var lean = _bladeLean * (0.5f + (float)random.NextDouble());
                var face = Quaternion.Euler(0f, angle + 90f + ((float)random.NextDouble() - 0.5f) * 50f, 0f);
                var shade = (float)random.NextDouble();
                var first = v;

                for (var s = 0; s < segments; s++)
                {
                    var h = s / (float)segments;
                    var half = width * 0.5f * (1f - h);
                    var outward = turn * new Vector3(0f, 0f, lean * h * h);
                    vertices[v] = root + outward + face * new Vector3(-half, h * height, 0f);
                    vertices[v + 1] = root + outward + face * new Vector3(half, h * height, 0f);
                    uvs[v] = new Vector2(shade, h);
                    uvs[v + 1] = new Vector2(shade, h);
                    v += 2;
                }

                vertices[v] = root + turn * new Vector3(0f, height, lean);
                uvs[v] = new Vector2(shade, 1f);
                v++;

                for (var s = 0; s < segments - 1; s++)
                {
                    var i = first + s * 2;
                    triangles[t++] = i;
                    triangles[t++] = i + 2;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 2;
                    triangles[t++] = i + 3;
                }

                var top = first + (segments - 1) * 2;
                triangles[t++] = top;
                triangles[t++] = v - 1;
                triangles[t++] = top + 1;
            }

            _clumpMesh = new Mesh { name = "Low Poly Grass Clump", vertices = vertices, uv = uvs, triangles = triangles };
            _clumpMesh.RecalculateNormals();
            _clumpMesh.RecalculateBounds();
            _clumpMesh.hideFlags = HideFlags.HideAndDontSave;
            return _clumpMesh;
        }

        private void DestroyMesh()
        {
            if (_clumpMesh == null)
            {
                return;
            }

            // The mesh is never saved, so destroying it at once is safe in Play Mode and in the Editor.
            DestroyImmediate(_clumpMesh);

            _clumpMesh = null;
        }

        private readonly struct Cell
        {
            public Cell(Matrix4x4[] matrices, Bounds bounds)
            {
                Matrices = matrices;
                Bounds = bounds;
            }

            public Matrix4x4[] Matrices { get; }

            public Bounds Bounds { get; }

            public int Count => Matrices.Length;
        }
    }
}
