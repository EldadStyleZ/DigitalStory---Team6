using System;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Shows one hive in the world: a health bar above it, a pulsing red ring on the ground while it is attacked, and a wrecked look once it falls.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Hive View")]
    public sealed class HiveView : MonoBehaviour
    {
        private const int RingSegments = 48;
        private const float FillDepthOffset = -0.01f;
        private const float RingHeight = 0.05f;
        private const string ShaderName = "Sprites/Default";

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static Material s_sharedMaterial;

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
        [SerializeField, Min(0.1f), Tooltip("Radius of the red ring on the ground, in metres.")]
        private float _ringRadius = 3.5f;

        [SerializeField, Range(0.05f, 0.95f), Tooltip("Inner edge of the ring as a share of its radius.")]
        private float _ringInnerRatio = 0.85f;

        [SerializeField, Min(0f), Tooltip("Pulses per second times two pi: how fast the ring pulses while the hive is attacked.")]
        private float _pulseSpeed = 8f;

        [SerializeField, Tooltip("The ring colour. Its alpha is the brightest the pulse reaches.")]
        private Color _ringColor = new Color(0.95f, 0.1f, 0.05f, 0.8f);

        [SerializeField, Range(0f, 1f), Tooltip("The faintest the pulse gets, as a share of the ring's alpha.")]
        private float _ringMinAlpha = 0.25f;

        private Hive _hive;
        private Camera _camera;
        private Transform _bar;
        private Transform _barBack;
        private Transform _barFill;
        private Transform _ring;
        private Renderer _backRenderer;
        private Renderer _fillRenderer;
        private Renderer _ringRenderer;
        private Renderer[] _modelRenderers;
        private Vector3 _modelStartScale;
        private Mesh _quadMesh;
        private Mesh _ringMesh;
        private MaterialPropertyBlock _block;
        private float _shownFraction = -1f;
        private bool _ringVisible;
        private bool _showsDestroyed;

        /// <summary>Supplies the hive to show and the camera the bar faces. Call once, before this object starts.</summary>
        public void Initialize(Hive hive, Camera camera)
        {
            if (hive == null)
            {
                throw new ArgumentNullException(nameof(hive), "HiveView needs a hive.");
            }

            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera), "HiveView needs the camera the health bar faces.");
            }

            if (_hive != null)
            {
                throw new InvalidOperationException("HiveView is already initialized.");
            }

            _hive = hive;
            _camera = camera;
            _block = new MaterialPropertyBlock();

            if (_model != null)
            {
                _modelStartScale = _model.localScale;
                _modelRenderers = _model.GetComponentsInChildren<Renderer>();
            }

            Material material = SharedMaterial();
            BuildBar(material);
            BuildRing(material);

            transform.position = hive.Position;
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

        private void ShowDestroyed()
        {
            _showsDestroyed = true;
            _bar.gameObject.SetActive(false);
            _ringRenderer.enabled = false;
            _ringVisible = false;

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

            _barFill = NewQuad("Fill", _quadMesh, material, out _fillRenderer);
            _barFill.localPosition = new Vector3(-_barSize.x * 0.5f, 0f, FillDepthOffset);
            _barFill.localScale = new Vector3(_barSize.x, _barSize.y * 0.8f, 1f);
        }

        private Transform NewQuad(string objectName, Mesh mesh, Material material, out Renderer renderer)
        {
            var quad = new GameObject(objectName);
            quad.transform.SetParent(_bar, false);
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
