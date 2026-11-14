using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DigitalStory.GrassBrush
{
    /// <summary>
    /// Draws the grass painted into a GrassData asset with GPU instancing, and drives the wind that bends it.
    /// Paint it with Tools > DigitalStory > Grass Brush. The wind comes from the Wind Zone if one is set,
    /// otherwise from the settings below. It also moves in the Editor; turn on the Scene view's
    /// "Always Refresh" to see it there.
    /// </summary>
    /// <remarks>
    /// Plan section AD. The clumps live on the GPU: the field is cut into small square cells, and the first time a cell
    /// comes within reach its painted and covering clumps go into a buffer of their own, once. Each frame only the cells
    /// in view and in reach are drawn, one draw call each, and nothing is uploaded. It used to send every clump's matrix in
    /// view of 64 m cells to the GPU each frame (up to about 800,000 clumps and 21 million triangles), which held the game
    /// near 40 frames a second and kept four processor cores busy.
    /// </remarks>
    [ExecuteAlways]
    public sealed class GrassField : MonoBehaviour
    {
        private const int ClumpStride = 20;   // GpuClump: the root's three floats, the size and the turn
        private const float BoundsPadding = 1f;
        private const float ChecksumHeightWeight = 17f;
        private const float ChecksumDepthWeight = 31f;
        private const float ChecksumSizeWeight = 7f;

        private static readonly int ClumpsId = Shader.PropertyToID("_GrassClumps");
        private static readonly int CountId = Shader.PropertyToID("_GrassCount");
        private static readonly int ThinStartId = Shader.PropertyToID("_GrassThinStart");
        private static readonly int ThinEndId = Shader.PropertyToID("_GrassThinEnd");
        private static readonly int ThinKeepId = Shader.PropertyToID("_GrassThinKeep");
        private static readonly int ThinFadeId = Shader.PropertyToID("_GrassThinFade");

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

        // Was "_cellSize" (64 in the forest-clearing scene). Renamed without FormerlySerializedAs on purpose, so the scene's
        // 64 m no longer applies: a 64 m cell drew far more grass than the view shows (plan section AD).
        [Tooltip("The grass is drawn in square cells this wide, one draw call each, so the cells out of view or out of reach are skipped. Smaller cells skip more grass and cost more draw calls.")]
        [SerializeField, Range(4f, 64f)] private float _drawCellSize = 8f;
        [Tooltip("How many new cells may be filled in one frame once the field is drawing, so walking into new ground never stalls a frame. The first frame fills every cell in reach.")]
        [SerializeField, Range(1, 256)] private int _cellBuildsPerFrame = 12;

        [Header("Distance")]
        [Tooltip("Metres from the camera where the grass starts to thin out.")]
        [SerializeField, Range(0f, 400f)] private float _thinStart = 35f;
        [Tooltip("Metres from the camera where it has thinned to the far share.")]
        [SerializeField, Range(0f, 400f)] private float _thinEnd = 90f;
        // Plan section AD: 0.35 cut the chase view's frame by about 1.1 ms (uncapped, 5120x2160) with no change in the top
        // view, whose ground ends about 30 m from the camera; the stills before and after are in the plan.
        [Tooltip("The share of clumps kept from the end of the thinning on. 1 keeps every clump at every distance.")]
        [SerializeField, Range(0.05f, 1f)] private float _farKeep = 0.35f;
        [Tooltip("The share of clumps over which a clump past the kept share shrinks away, so thinning never pops.")]
        [SerializeField, Range(0.01f, 0.5f)] private float _thinFade = 0.1f;

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

        private const float MaskCell = 0.5f;

        private readonly Plane[] _frustum = new Plane[6];
        private readonly Dictionary<Vector2Int, DrawCell> _drawCells = new Dictionary<Vector2Int, DrawCell>();
        private readonly List<GpuClump> _scratch = new List<GpuClump>();
        private Dictionary<Vector2Int, PaintedBucket> _painted = new Dictionary<Vector2Int, PaintedBucket>();
        private int _builtVersion = -1;
        private GrassData _builtData;
        private Mesh _clumpMesh;
        private int _coverSettingsHash;
        private int _builtMaskVersion = -1;
        private bool _meshDirty;
        private bool _cellsDirty;

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
            ReleaseCells();
        }

        public void SetCover(bool cover, float density, Vector2 centre, float halfSize)
        {
            _coverTerrain = cover;
            _coverDensity = Mathf.Clamp(density, 0.5f, 40f);
            _coverCentre = centre;
            _coverHalfSize = Mathf.Clamp(halfSize, 10f, 500f);
            ReleaseCells();
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

        private float DrawCellSize => Mathf.Max(1f, _drawCellSize);

        private void OnEnable()
        {
            Camera.onPreCull += DrawForCamera;
            TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= DrawForCamera;
            TerrainCallbacks.heightmapChanged -= OnHeightmapChanged;
            ReleaseCells();
            DestroyMesh();
            if (_windOwner == this)
            {
                _windOwner = null;
            }
        }

        private void OnValidate()
        {
            // The look settings change the clump's shape, and the cell size and the thinning the cells, so both are rebuilt
            // on the next draw.
            _meshDirty = true;
            _builtVersion = -1;
            _cellsDirty = true;
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
            var reach = _drawDistance;
            var reachSquared = reach * reach;
            var mesh = GetClumpMesh();
            var cell = DrawCellSize;
            var builds = _drawCells.Count == 0 ? int.MaxValue : _cellBuildsPerFrame;
            var renderParams = new RenderParams(_material)
            {
                camera = camera,
                layer = gameObject.layer,
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows = false,
            };

            var minX = Mathf.FloorToInt((eye.x - reach) / cell);
            var maxX = Mathf.FloorToInt((eye.x + reach) / cell);
            var minZ = Mathf.FloorToInt((eye.z - reach) / cell);
            var maxZ = Mathf.FloorToInt((eye.z + reach) / cell);
            for (var cx = minX; cx <= maxX; cx++)
            {
                for (var cz = minZ; cz <= maxZ; cz++)
                {
                    var key = new Vector2Int(cx, cz);
                    if (!_drawCells.TryGetValue(key, out var drawCell))
                    {
                        // A cell is filled once, when it first comes within reach, never for a corner of the square out of it.
                        if (builds <= 0 || GroundSqrDistance(key, eye) > reachSquared)
                        {
                            continue;
                        }

                        builds--;
                        drawCell = BuildCell(key);
                        _drawCells.Add(key, drawCell);
                    }

                    if (drawCell.Count == 0 || drawCell.Bounds.SqrDistance(eye) > reachSquared || !GeometryUtility.TestPlanesAABB(_frustum, drawCell.Bounds))
                    {
                        continue;
                    }

                    renderParams.worldBounds = drawCell.Bounds;
                    renderParams.matProps = drawCell.Block;
                    Graphics.RenderMeshPrimitives(renderParams, mesh, 0, InstancesToDraw(drawCell, eye));
                }
            }
        }

        // Far cells draw only the share of their shuffled clumps the thinning keeps at their nearest point, and the fade
        // past it; the shader shrinks away the rest of what is drawn clump by clump.
        private int InstancesToDraw(DrawCell drawCell, Vector3 eye)
        {
            if (_farKeep >= 1f)
            {
                return drawCell.Count;
            }

            var nearest = Mathf.Sqrt(drawCell.Bounds.SqrDistance(eye));
            var far = Mathf.Clamp01((nearest - _thinStart) / Mathf.Max(_thinEnd - _thinStart, 0.001f));
            var keep = Mathf.Lerp(1f, _farKeep, far) + _thinFade;
            return Mathf.Clamp(Mathf.CeilToInt(drawCell.Count * keep), 1, drawCell.Count);
        }

        private float GroundSqrDistance(Vector2Int key, Vector3 eye)
        {
            var cell = DrawCellSize;
            var dx = Mathf.Max(0f, Mathf.Max((key.x * cell) - eye.x, eye.x - ((key.x + 1) * cell)));
            var dz = Mathf.Max(0f, Mathf.Max((key.y * cell) - eye.z, eye.z - ((key.y + 1) * cell)));
            return (dx * dx) + (dz * dz);
        }

        // Brings the draw cells up to date with the data and the settings: a change to the painted clumps rebuilds only the
        // cells whose clumps changed; a change to the covering grass, its erase mask or the look rebuilds them all.
        private void Rebuild()
        {
            // The covering grass regrows when its settings change, or when undo or redo replace the erase mask.
            var settings = (_coverDensity, _coverCentre, _coverHalfSize, _drawCellSize, _paintMinSize, _paintMaxSize, _coverTerrain, _noGrassBelow).GetHashCode();
            if (_cellsDirty || settings != _coverSettingsHash || _builtMaskVersion != _data.MaskVersion || _builtData != _data)
            {
                _cellsDirty = false;
                _coverSettingsHash = settings;
                _builtMaskVersion = _data.MaskVersion;
                ReleaseCells();
            }

            if (_builtData == _data && _builtVersion == _data.Version)
            {
                return;
            }

            BucketPainted();
            _builtData = _data;
            _builtVersion = _data.Version;
        }

        // Sorts the painted clumps into the draw cells, and lets go of every cell whose painted clumps changed.
        private void BucketPainted()
        {
            var cell = DrawCellSize;
            var buckets = new Dictionary<Vector2Int, PaintedBucket>();
            for (var i = 0; i < _data.Count; i++)
            {
                var position = _data.GetPosition(i);
                var key = new Vector2Int(Mathf.FloorToInt(position.x / cell), Mathf.FloorToInt(position.z / cell));
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new PaintedBucket();
                    buckets.Add(key, bucket);
                }

                bucket.Add(new GpuClump(position, _data.GetSize(i), _data.GetTurn(i)));
            }

            foreach (var pair in _painted)
            {
                if (!buckets.TryGetValue(pair.Key, out var now) || !now.IsSameAs(pair.Value))
                {
                    ReleaseCell(pair.Key);
                }
            }

            foreach (var key in buckets.Keys)
            {
                if (!_painted.ContainsKey(key))
                {
                    ReleaseCell(key);
                }
            }

            _painted = buckets;
        }

        // A cell's painted clumps and its covering grass in one buffer, shuffled so any first share of them is an even
        // sample for the thinning, with the bounds they fill.
        private DrawCell BuildCell(Vector2Int key)
        {
            _scratch.Clear();
            if (_painted.TryGetValue(key, out var bucket))
            {
                _scratch.AddRange(bucket.Clumps);
            }

            if (_coverTerrain)
            {
                AddCover(key, Terrain.activeTerrain, _scratch);
            }

            if (_scratch.Count == 0)
            {
                return DrawCell.Empty;
            }

            var random = new System.Random(unchecked((key.x * 92837111) ^ (key.y * 689287499) ^ 0x2545f491));
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            for (var i = _scratch.Count - 1; i >= 0; i--)
            {
                var swap = random.Next(i + 1);
                var clump = _scratch[swap];
                _scratch[swap] = _scratch[i];
                _scratch[i] = clump;
                min = Vector3.Min(min, clump.Position);
                max = Vector3.Max(max, clump.Position + (Vector3.up * clump.Size));
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min - (Vector3.one * BoundsPadding), max + (Vector3.one * BoundsPadding));
            var buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _scratch.Count, ClumpStride);
            buffer.SetData(_scratch);
            var block = new MaterialPropertyBlock();
            block.SetBuffer(ClumpsId, buffer);
            block.SetFloat(CountId, _scratch.Count);
            block.SetFloat(ThinStartId, _thinStart);
            block.SetFloat(ThinEndId, _thinEnd);
            block.SetFloat(ThinKeepId, _farKeep);
            block.SetFloat(ThinFadeId, _thinFade);
            return new DrawCell(buffer, block, bounds, _scratch.Count);
        }

        // The covering grass of one cell: a jittered grid at the cover density, the same every time for the same cell,
        // standing on the terrain, inside the covered square and not where the brush erased it.
        private void AddCover(Vector2Int key, Terrain terrain, List<GpuClump> clumps)
        {
            var cell = DrawCellSize;
            var random = new System.Random(unchecked((key.x * 73856093) ^ (key.y * 19349663) ^ 0x5bd1e995));
            var spacing = 1f / Mathf.Sqrt(Mathf.Max(0.05f, _coverDensity));
            var cellMin = new Vector2(key.x * cell, key.y * cell);
            var area = new Rect(_coverCentre - (Vector2.one * _coverHalfSize), Vector2.one * (_coverHalfSize * 2f));
            if (!area.Overlaps(new Rect(cellMin, Vector2.one * cell)))
            {
                return;
            }

            var origin = terrain != null ? terrain.GetPosition() : Vector3.zero;
            var minSize = PaintMinSize;
            var maxSize = PaintMaxSize;
            for (var x = 0f; x < cell; x += spacing)
            {
                for (var z = 0f; z < cell; z += spacing)
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

                    clumps.Add(new GpuClump(position, size, turn));
                }
            }
        }

        private void ForgetCoverCells(float minX, float maxX, float minZ, float maxZ)
        {
            var cell = DrawCellSize;
            for (var cx = Mathf.FloorToInt(minX / cell); cx <= Mathf.FloorToInt(maxX / cell); cx++)
            {
                for (var cz = Mathf.FloorToInt(minZ / cell); cz <= Mathf.FloorToInt(maxZ / cell); cz++)
                {
                    ReleaseCell(new Vector2Int(cx, cz));
                }
            }
        }

        private void ReleaseCell(Vector2Int key)
        {
            if (_drawCells.TryGetValue(key, out var drawCell))
            {
                drawCell.Release();
                _drawCells.Remove(key);
            }
        }

        private void ReleaseCells()
        {
            foreach (var drawCell in _drawCells.Values)
            {
                drawCell.Release();
            }

            _drawCells.Clear();
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

        // One clump as the shader reads it (WindGrass.shader's GrassClump): the turn is in radians there.
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct GpuClump
        {
            public GpuClump(Vector3 position, float size, float turnDegrees)
            {
                Position = position;
                Size = size;
                Turn = turnDegrees * Mathf.Deg2Rad;
            }

            public Vector3 Position { get; }

            public float Size { get; }

            public float Turn { get; }
        }

        // The painted clumps of one draw cell, with a sum that changes when any of them moves, grows or turns.
        private sealed class PaintedBucket
        {
            public List<GpuClump> Clumps { get; } = new List<GpuClump>();

            public double Sum { get; private set; }

            public void Add(GpuClump clump)
            {
                Clumps.Add(clump);
                Vector3 position = clump.Position;
                Sum += position.x + (position.y * ChecksumHeightWeight) + (position.z * ChecksumDepthWeight) + (clump.Size * ChecksumSizeWeight) + clump.Turn;
            }

            public bool IsSameAs(PaintedBucket other) => other.Clumps.Count == Clumps.Count && other.Sum == Sum;
        }

        // One square cell's clumps on the GPU: their buffer, the property block that hands it to the shader, their bounds.
        private sealed class DrawCell
        {
            public static readonly DrawCell Empty = new DrawCell(null, null, default, 0);

            public DrawCell(GraphicsBuffer clumps, MaterialPropertyBlock block, Bounds bounds, int count)
            {
                Clumps = clumps;
                Block = block;
                Bounds = bounds;
                Count = count;
            }

            public GraphicsBuffer Clumps { get; }

            public MaterialPropertyBlock Block { get; }

            public Bounds Bounds { get; }

            public int Count { get; }

            public void Release() => Clumps?.Release();
        }
    }
}
