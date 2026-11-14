using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Shows one hive in the world: a health bar and a store bar above it, a pulsing orange ring on the ground while it is
    /// attacked, a soft collect ring that brightens while the core is inside it, and a wrecked look once it falls. While its
    /// store flows into the swarm, gold dots rise from it to the core; while the swarm heals it, green crosses sink from the
    /// core into it, so taking and giving differ in shape and colour, not only in direction.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Hive View")]
    public sealed class HiveView : MonoBehaviour
    {
        private const int RingSegments = 48;
        private const float FillDepthOffset = -0.01f;
        private const float RingHeight = 0.05f;
        private const string ShaderName = "Sprites/Default";
        private const int MoteCount = 10;
        private const int DotSegments = 10;
        private const float CollectRingHeight = 0.04f;
        private const float CollectRingInnerRatio = 0.94f;
        private const float StoreBarGap = 1.4f;
        private const float DotSize = 0.3f;
        private const float CrossSize = 0.42f;
        private const float CrossArmShare = 0.34f;
        private const float MoteLift = 0.9f;
        private const float MoteArc = 0.9f;
        private const float MoteFadeShare = 0.2f;
        private const float MoteCycleSeconds = 1.1f;
        private const float RingFadePerSecond = 6f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static Material s_sharedMaterial;
        private static Material s_fillMaterial;

        [Header("Model")]
        [SerializeField, Tooltip("The hive model. It is squashed and darkened when the hive is destroyed. Optional.")]
        private Transform _model;

        [SerializeField, Min(0.01f), Tooltip("How much of its height the model keeps when the hive is destroyed.")]
        private float _destroyedHeightScale = 0.3f;

        [SerializeField, Tooltip("The tint of the model when the hive is destroyed.")]
        private Color _destroyedTint = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Header("Health bar")]
        [SerializeField, Min(0f), Tooltip("Metres above the hive's position where the bar floats.")]
        private float _barHeight = 3f;

        [SerializeField, Tooltip("Width and height of the bar, in metres.")]
        private Vector2 _barSize = new Vector2(2.2f, 0.25f);

        [SerializeField, Tooltip("The dark strip behind the fill.")]
        private Color _barBackColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        [SerializeField, Tooltip("The fill colour at full health.")]
        private Color _barFullColor = new Color(0.3f, 0.85f, 0.3f, 1f);

        [SerializeField, Tooltip("The fill colour at no health.")]
        private Color _barEmptyColor = new Color(0.9f, 0.15f, 0.1f, 1f);

        [Header("Attack ring")]
        [SerializeField, Min(0.1f), Tooltip("Radius of the orange ring on the ground, in metres.")]
        private float _ringRadius = 3.5f;

        [SerializeField, Range(0.05f, 0.95f), Tooltip("Inner edge of the ring as a share of its radius.")]
        private float _ringInnerRatio = 0.85f;

        [SerializeField, Min(0f), Tooltip("Pulses per second times two pi: how fast the ring pulses while the hive is attacked.")]
        private float _pulseSpeed = 8f;

        [SerializeField, Tooltip("The ring colour. Its alpha is the brightest the pulse reaches.")]
        private Color _ringColor = new Color(1f, 0.55f, 0.1f, 0.8f);

        [SerializeField, Range(0f, 1f), Tooltip("The faintest the pulse gets, as a share of the ring's alpha.")]
        private float _ringMinAlpha = 0.25f;

        [Header("Store bar")]
        [SerializeField, Tooltip("The store bar fill while the hive makes wasps.")]
        private Color _storeColor = new Color(1f, 0.78f, 0.15f, 1f);

        [SerializeField, Tooltip("The store bar fill while the hive makes no wasps (too hurt).")]
        private Color _storeIdleColor = new Color(0.45f, 0.38f, 0.15f, 0.7f);

        [Header("Collect ring")]
        [SerializeField, Tooltip("The collect ring colour. Its alpha is the faint value, used while the core is outside.")]
        private Color _collectRingColor = new Color(1f, 0.8f, 0.2f, 0.18f);

        [SerializeField, Range(0f, 1f), Tooltip("The collect ring alpha while the core is inside it.")]
        private float _collectRingNearAlpha = 0.65f;

        [Header("Motes")]
        [SerializeField, Tooltip("The colour of the dots that rise from the hive to the core while its store flows into the swarm.")]
        private Color _moteColor = new Color(1f, 0.85f, 0.25f, 1f);

        [SerializeField, Tooltip("The colour of the crosses that sink from the core into the hive while the swarm heals it. Near the health bar's colour.")]
        private Color _healMoteColor = new Color(0.45f, 1f, 0.45f, 1f);

        private Hive _hive;
        private HiveConfig _config;
        private Camera _camera;
        private Transform _bar;
        private Transform _barBack;
        private Transform _barFill;
        private Transform _ring;
        private Renderer _backRenderer;
        private Renderer _fillRenderer;
        private Renderer _ringRenderer;
        private Transform _storeBar;
        private Transform _storeFill;
        private Renderer _storeFillRenderer;
        private Transform _collectRing;
        private Renderer _collectRingRenderer;
        private Mesh _collectRingMesh;
        private Mesh _dotMesh;
        private Mesh _crossMesh;
        private Transform[] _motes;
        private MeshFilter[] _moteFilters;
        private Renderer[] _moteRenderers;
        private Vector3 _corePosition;
        private float _shownStore = -1f;
        private bool _isProducingShown;
        private bool _hasShownProducing;
        private float _collectAlpha;
        private bool _isCoreNear;
        private bool _isCollecting;
        private bool _isGiving;
        private bool _areMotesVisible;
        private bool _areMotesCrosses;
        private Renderer[] _modelRenderers;
        private Vector3 _modelStartScale;
        private Mesh _quadMesh;
        private Mesh _ringMesh;
        private MaterialPropertyBlock _block;
        private float _shownFraction = -1f;
        private bool _ringVisible;
        private bool _showsDestroyed;

        /// <summary>Tells the hive where the swarm's core is, so the motes fly to it. The root calls this each frame.</summary>
        public void SetCorePosition(Vector3 position)
        {
            _corePosition = position;
        }

        /// <summary>Brightens the collect ring while the core is inside it. The root calls this each frame.</summary>
        public void ShowCoreNear(bool isNear)
        {
            _isCoreNear = isNear;
        }

        /// <summary>While true, gold dots rise from the hive toward the core. The root calls this each frame.</summary>
        public void ShowCollecting(bool isCollecting)
        {
            _isCollecting = isCollecting;
        }

        /// <summary>While true, green crosses sink from the core into the hive. Giving wins over collecting. The root calls this each frame.</summary>
        public void ShowGiving(bool isGiving)
        {
            _isGiving = isGiving;
        }

        /// <summary>Supplies the hive to show, the camera the bars face and the hive config (collect radius). Call once, before this object starts.</summary>
        public void Initialize(Hive hive, Camera camera, HiveConfig config)
        {
            if (hive == null)
            {
                throw new ArgumentNullException(nameof(hive), "HiveView needs a hive.");
            }

            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera), "HiveView needs the camera the health bar faces.");
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "HiveView needs the hive config for the collect ring.");
            }

            if (_hive != null)
            {
                throw new InvalidOperationException("HiveView is already initialized.");
            }

            _hive = hive;
            _camera = camera;
            _config = config;
            _block = new MaterialPropertyBlock();

            // First, before any child is placed: the rings and the bar are children placed at the hive's world position,
            // and moving this object after them would carry them by the move, off the hive, on every map but the one the
            // scene was saved with (plan section T).
            transform.position = hive.Position;

            if (_model != null)
            {
                _modelStartScale = _model.localScale;
                _modelRenderers = _model.GetComponentsInChildren<Renderer>();
            }

            Material material = SharedMaterial();
            BuildBar(material);
            BuildRing(material);
            BuildStoreBar(material);
            BuildCollectRing(material);
            BuildMotes(material);

            _hive.Destroyed += OnDestroyed;

            if (_hive.IsDestroyed)
            {
                ShowDestroyed();
            }
        }

        private void LateUpdate()
        {
            if (_hive == null || _showsDestroyed)
            {
                return;
            }

            FaceCamera();
            ShowHealth();
            ShowRing();
            ShowStore();
            ShowCollectRing();
            ShowMotes();
        }

        private void OnDestroy()
        {
            if (_hive != null)
            {
                _hive.Destroyed -= OnDestroyed;
            }

            if (_quadMesh != null)
            {
                Destroy(_quadMesh);
            }

            if (_ringMesh != null)
            {
                Destroy(_ringMesh);
            }

            if (_collectRingMesh != null)
            {
                Destroy(_collectRingMesh);
            }

            if (_dotMesh != null)
            {
                Destroy(_dotMesh);
            }

            if (_crossMesh != null)
            {
                Destroy(_crossMesh);
            }
        }

        private void OnDestroyed(Hive hive)
        {
            ShowDestroyed();
        }

        private void FaceCamera()
        {
            _bar.rotation = _camera.transform.rotation;
        }

        private void ShowHealth()
        {
            float fraction = _hive.MaxHealth > 0f ? Mathf.Clamp01(_hive.Health / _hive.MaxHealth) : 0f;
            if (Mathf.Approximately(fraction, _shownFraction))
            {
                return;
            }

            _shownFraction = fraction;
            float width = _barSize.x;
            _barFill.localScale = new Vector3(Mathf.Max(0.0001f, width * fraction), _barSize.y * 0.8f, 1f);
            _block.SetColor(ColorId, Color.Lerp(_barEmptyColor, _barFullColor, fraction));
            _fillRenderer.SetPropertyBlock(_block);
        }

        private void ShowRing()
        {
            bool attacked = _hive.IsUnderAttack;
            if (!attacked)
            {
                if (_ringVisible)
                {
                    _ringVisible = false;
                    _ringRenderer.enabled = false;
                }

                return;
            }

            _ringVisible = true;
            _ringRenderer.enabled = true;

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseSpeed);
            Color color = _ringColor;
            color.a = _ringColor.a * Mathf.Lerp(_ringMinAlpha, 1f, wave);
            _block.SetColor(ColorId, color);
            _ringRenderer.SetPropertyBlock(_block);

            float scale = _ringRadius * (1f + 0.06f * wave);
            _ring.localScale = new Vector3(scale, 1f, scale);
        }

        private void ShowStore()
        {
            float capacity = _hive.StoreCapacity;
            float fraction = capacity > 0f ? Mathf.Clamp01(_hive.Store / capacity) : 0f;
            bool producing = _hive.IsProducing;
            if (Mathf.Approximately(fraction, _shownStore) && _hasShownProducing && producing == _isProducingShown)
            {
                return;
            }

            _shownStore = fraction;
            _isProducingShown = producing;
            _hasShownProducing = true;
            _storeFill.localScale = new Vector3(Mathf.Max(0.0001f, _barSize.x * fraction), _barSize.y * 0.8f, 1f);
            _block.SetColor(ColorId, producing ? _storeColor : _storeIdleColor);
            _storeFillRenderer.SetPropertyBlock(_block);
        }

        private void ShowCollectRing()
        {
            float target = _isCoreNear ? _collectRingNearAlpha : _collectRingColor.a;
            _collectAlpha = Mathf.MoveTowards(_collectAlpha, target, RingFadePerSecond * Time.deltaTime);
            Color color = _collectRingColor;
            color.a = _collectAlpha;
            _block.SetColor(ColorId, color);
            _collectRingRenderer.SetPropertyBlock(_block);
        }

        // Motes ride a cycle from 0 to 1 along an arc. Collecting runs it hive to core with dots, giving runs it core to hive
        // with crosses. Each fades in and out at the ends of its trip, so the stream has no hard start.
        private void ShowMotes()
        {
            bool isFlowing = _isCollecting || _isGiving;
            if (!isFlowing)
            {
                HideMotes();
                return;
            }

            _areMotesVisible = true;
            if (_isGiving != _areMotesCrosses)
            {
                SetMoteShape(_isGiving);
            }

            Vector3 hivePoint = _hive.Position + (Vector3.up * MoteLift);
            Vector3 corePoint = _corePosition + (Vector3.up * MoteLift);
            Vector3 start = _isGiving ? corePoint : hivePoint;
            Vector3 end = _isGiving ? hivePoint : corePoint;
            Color baseColor = _isGiving ? _healMoteColor : _moteColor;
            float baseTime = Time.time / MoteCycleSeconds;
            Quaternion facing = _camera.transform.rotation;

            for (int index = 0; index < MoteCount; index++)
            {
                float t = Mathf.Repeat(baseTime + ((float)index / MoteCount), 1f);
                Vector3 position = Vector3.Lerp(start, end, t);
                position.y += Mathf.Sin(t * Mathf.PI) * MoteArc;
                Transform mote = _motes[index];
                mote.SetPositionAndRotation(position, facing);

                Color color = baseColor;
                color.a = baseColor.a * Mathf.Clamp01(Mathf.Min(t, 1f - t) / MoteFadeShare);
                _block.SetColor(ColorId, color);
                _moteRenderers[index].SetPropertyBlock(_block);
                _moteRenderers[index].enabled = true;
            }
        }

        private void HideMotes()
        {
            if (!_areMotesVisible)
            {
                return;
            }

            _areMotesVisible = false;
            for (int index = 0; index < MoteCount; index++)
            {
                _moteRenderers[index].enabled = false;
            }
        }

        // Changes only when the flow turns around, so no frame swaps meshes for nothing.
        private void SetMoteShape(bool isCross)
        {
            _areMotesCrosses = isCross;
            Mesh mesh = isCross ? _crossMesh : _dotMesh;
            float size = isCross ? CrossSize : DotSize;
            for (int index = 0; index < MoteCount; index++)
            {
                _moteFilters[index].sharedMesh = mesh;
                _motes[index].localScale = new Vector3(size, size, 1f);
            }
        }

        private void ShowDestroyed()
        {
            _showsDestroyed = true;
            _bar.gameObject.SetActive(false);
            _ringRenderer.enabled = false;
            _ringVisible = false;
            _collectRingRenderer.enabled = false;
            _areMotesVisible = true; // So HideMotes hides every one, whatever was shown.
            HideMotes();

            if (_model == null)
            {
                return;
            }

            _model.localScale = new Vector3(_modelStartScale.x, _modelStartScale.y * _destroyedHeightScale, _modelStartScale.z);
            _block.SetColor(ColorId, _destroyedTint);
            for (int index = 0; index < _modelRenderers.Length; index++)
            {
                _modelRenderers[index].SetPropertyBlock(_block);
            }
        }

        private void BuildBar(Material material)
        {
            _quadMesh = BuildQuadMesh();

            _bar = new GameObject("HealthBar").transform;
            _bar.SetParent(transform, false);
            _bar.position = _hive.Position + Vector3.up * _barHeight;

            _barBack = NewQuad("Back", _quadMesh, material, out _backRenderer);
            _barBack.localPosition = new Vector3(-_barSize.x * 0.5f, 0f, 0f);
            _barBack.localScale = new Vector3(_barSize.x, _barSize.y, 1f);
            _block.SetColor(ColorId, _barBackColor);
            _backRenderer.SetPropertyBlock(_block);

            _barFill = NewQuad("Fill", _quadMesh, FillMaterial(material), out _fillRenderer);
            _barFill.localPosition = new Vector3(-_barSize.x * 0.5f, 0f, FillDepthOffset);
            _barFill.localScale = new Vector3(_barSize.x, _barSize.y * 0.8f, 1f);
        }

        private void BuildStoreBar(Material material)
        {
            _storeBar = new GameObject("StoreBar").transform;
            _storeBar.SetParent(_bar, false);
            _storeBar.localPosition = new Vector3(0f, -_barSize.y * StoreBarGap, 0f);

            Transform back = NewQuad("Back", _quadMesh, material, out Renderer backRenderer, _storeBar);
            back.localPosition = new Vector3(-_barSize.x * 0.5f, 0f, 0f);
            back.localScale = new Vector3(_barSize.x, _barSize.y, 1f);
            _block.SetColor(ColorId, _barBackColor);
            backRenderer.SetPropertyBlock(_block);

            _storeFill = NewQuad("Fill", _quadMesh, FillMaterial(material), out _storeFillRenderer, _storeBar);
            _storeFill.localPosition = new Vector3(-_barSize.x * 0.5f, 0f, FillDepthOffset);
            _storeFill.localScale = new Vector3(_barSize.x, _barSize.y * 0.8f, 1f);
        }

        private void BuildCollectRing(Material material)
        {
            _collectRingMesh = BuildRingMesh(CollectRingInnerRatio);

            var ring = new GameObject("CollectRing");
            _collectRing = ring.transform;
            _collectRing.SetParent(transform, false);
            _collectRing.position = _hive.Position + Vector3.up * CollectRingHeight;
            _collectRing.localScale = new Vector3(_config.CollectRadius, 1f, _config.CollectRadius);
            ring.AddComponent<MeshFilter>().sharedMesh = _collectRingMesh;

            var meshRenderer = ring.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            _collectRingRenderer = meshRenderer;
            _collectAlpha = _collectRingColor.a;
        }

        // A fixed pool of centred motes, made once and moved in code; no allocation per frame. They start as dots.
        private void BuildMotes(Material material)
        {
            _dotMesh = BuildDotMesh();
            _crossMesh = BuildCrossMesh();

            var root = new GameObject("Motes").transform;
            root.SetParent(transform, false);
            _motes = new Transform[MoteCount];
            _moteFilters = new MeshFilter[MoteCount];
            _moteRenderers = new Renderer[MoteCount];
            for (int index = 0; index < MoteCount; index++)
            {
                var mote = new GameObject("Mote");
                mote.transform.SetParent(root, false);
                mote.transform.localScale = new Vector3(DotSize, DotSize, 1f);
                MeshFilter filter = mote.AddComponent<MeshFilter>();
                filter.sharedMesh = _dotMesh;
                var meshRenderer = mote.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.enabled = false;
                _motes[index] = mote.transform;
                _moteFilters[index] = filter;
                _moteRenderers[index] = meshRenderer;
            }
        }

        private Transform NewQuad(string objectName, Mesh mesh, Material material, out Renderer renderer)
        {
            return NewQuad(objectName, mesh, material, out renderer, _bar);
        }

        private static Transform NewQuad(string objectName, Mesh mesh, Material material, out Renderer renderer, Transform parent)
        {
            var quad = new GameObject(objectName);
            quad.transform.SetParent(parent, false);
            quad.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = quad.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            renderer = meshRenderer;
            return quad.transform;
        }

        private void BuildRing(Material material)
        {
            _ringMesh = BuildRingMesh(_ringInnerRatio);

            var ring = new GameObject("AttackRing");
            _ring = ring.transform;
            _ring.SetParent(transform, false);
            _ring.position = _hive.Position + Vector3.up * RingHeight;
            _ring.localScale = new Vector3(_ringRadius, 1f, _ringRadius);
            ring.AddComponent<MeshFilter>().sharedMesh = _ringMesh;

            var meshRenderer = ring.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;
            _ringRenderer = meshRenderer;
        }

        // One material for every hive: an unlit colour shader that always ships, tinted per renderer with a property block.
        private static Material SharedMaterial()
        {
            if (s_sharedMaterial != null)
            {
                return s_sharedMaterial;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException("The shader " + ShaderName + " was not found.");
            }

            s_sharedMaterial = new Material(shader) { name = "HiveView Unlit", hideFlags = HideFlags.HideAndDontSave };
            return s_sharedMaterial;
        }

        // Transparent quads are drawn far to near by their centres. A shortened fill's centre can lie farther from the
        // camera than the backing's, and the backing then darkens it, so the fill gets a later queue and is always on top.
        private static Material FillMaterial(Material shared)
        {
            if (s_fillMaterial == null)
            {
                s_fillMaterial = new Material(shared)
                {
                    name = "HiveView Unlit Fill",
                    hideFlags = HideFlags.HideAndDontSave,
                    renderQueue = shared.renderQueue + 1,
                };
            }

            return s_fillMaterial;
        }

        // A quad whose left edge is at x = 0, so scaling x makes a bar that shrinks toward the left.
        private static Mesh BuildQuadMesh()
        {
            var mesh = new Mesh { name = "HiveView Quad" };
            mesh.vertices = new[]
            {
                new Vector3(0f, -0.5f, 0f), new Vector3(1f, -0.5f, 0f), new Vector3(1f, 0.5f, 0f), new Vector3(0f, 0.5f, 0f),
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        // A round dot of diameter 1, centred, facing -z like the quads.
        private static Mesh BuildDotMesh()
        {
            var vertices = new Vector3[DotSegments + 1];
            var triangles = new int[DotSegments * 3];
            vertices[0] = Vector3.zero;
            for (int segment = 0; segment < DotSegments; segment++)
            {
                float angle = segment * Mathf.PI * 2f / DotSegments;
                vertices[segment + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f);
                int t = segment * 3;
                triangles[t] = 0;
                triangles[t + 1] = ((segment + 1) % DotSegments) + 1;
                triangles[t + 2] = segment + 1;
            }

            var mesh = new Mesh { name = "HiveView Dot", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        // A plus sign 1 across, centred: a full-height bar and two side arms, which do not overlap, so a fading cross stays
        // one even tint.
        private static Mesh BuildCrossMesh()
        {
            float arm = CrossArmShare * 0.5f;
            var vertices = new[]
            {
                // The upright bar.
                new Vector3(-arm, -0.5f, 0f), new Vector3(arm, -0.5f, 0f), new Vector3(arm, 0.5f, 0f), new Vector3(-arm, 0.5f, 0f),

                // The left arm.
                new Vector3(-0.5f, -arm, 0f), new Vector3(-arm, -arm, 0f), new Vector3(-arm, arm, 0f), new Vector3(-0.5f, arm, 0f),

                // The right arm.
                new Vector3(arm, -arm, 0f), new Vector3(0.5f, -arm, 0f), new Vector3(0.5f, arm, 0f), new Vector3(arm, arm, 0f),
            };
            var triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6, 8, 10, 9, 8, 11, 10 };
            var mesh = new Mesh { name = "HiveView Cross", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        // A flat ring of outer radius 1 on the ground plane.
        private static Mesh BuildRingMesh(float innerRatio)
        {
            var vertices = new Vector3[RingSegments * 2];
            var triangles = new int[RingSegments * 6];
            for (int segment = 0; segment < RingSegments; segment++)
            {
                float angle = segment * Mathf.PI * 2f / RingSegments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[segment * 2] = direction;
                vertices[segment * 2 + 1] = direction * innerRatio;

                int next = (segment + 1) % RingSegments;
                int outer = segment * 2;
                int inner = outer + 1;
                int nextOuter = next * 2;
                int nextInner = nextOuter + 1;
                int t = segment * 6;
                triangles[t] = outer;
                triangles[t + 1] = nextOuter;
                triangles[t + 2] = inner;
                triangles[t + 3] = inner;
                triangles[t + 4] = nextOuter;
                triangles[t + 5] = nextInner;
            }

            var mesh = new Mesh { name = "HiveView Ring", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
