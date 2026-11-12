using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DigitalStory.View
{
    /// <summary>
    /// Shows one wasp nest in the world. Closed, the nest hangs dark and still. Open, it glows warm and pulses, owns a small
    /// point light, lets a few gold motes drift around it and lies a faint ring on the ground at the collect radius. While the
    /// core collects it, a bright ring fills clockwise with the progress and motes stream from the nest down to the ground
    /// circle. Once collected, a short burst of motes plays, then the nest, its light and the rings go and the tree stays.
    /// It polls the nest each frame and changes nothing in the logic.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Nest View")]
    public sealed class NestView : MonoBehaviour
    {
        private const string NestObjectName = "WaspNest";
        private const string ShaderName = "Sprites/Default";
        private const int RingSegments = 64;
        private const int DotSegments = 10;
        private const int MoteCount = 12;
        private const int IdleMoteCount = 4;
        private const float GroundRingHeight = 0.05f;
        private const float FillRingHeight = 0.07f;
        private const float GroundRingInnerRatio = 0.96f;
        private const float FillRingInnerRatio = 0.88f;
        private const float MoteSize = 0.38f;
        private const float MoteFadeShare = 0.2f;
        private const float StreamCycleSeconds = 0.9f;
        private const float StreamArc = 1.2f;
        private const float IdleCycleSeconds = 3.2f;
        private const float IdleRadius = 1.1f;
        private const float IdleRise = 1.6f;
        private const float BurstSeconds = 0.9f;
        private const float BurstRadius = 3.5f;
        private const float BurstLift = 1.8f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static Material s_sharedMaterial;
        private static Material s_fillMaterial;

        [Header("Look")]
        [SerializeField, Tooltip("The tint of the nest while it is closed. Dark, so a closed nest reads as dead.")]
        private Color _closedTint = new Color(0.16f, 0.13f, 0.12f, 1f);

        [SerializeField, Tooltip("The nest's tint at the faint end of the pulse while it is open.")]
        private Color _openTintLow = new Color(1f, 0.55f, 0.18f, 1f);

        [SerializeField, Tooltip("The nest's tint at the bright end of the pulse while it is open.")]
        private Color _openTintHigh = new Color(1f, 0.85f, 0.45f, 1f);

        [SerializeField, Min(0f), Tooltip("How strongly the nest's emission follows the pulse, for materials that glow.")]
        private float _emissionStrength = 1.6f;

        [SerializeField, Min(0.05f), Tooltip("Pulses per second of the glow. Slow.")]
        private float _pulsesPerSecond = 0.4f;

        [Header("Light")]
        [SerializeField, Min(0f), Tooltip("Metres above the nest's position where its point light hangs.")]
        private float _lightHeight = 3f;

        [SerializeField, Min(0.1f), Tooltip("Reach of the point light, in metres.")]
        private float _lightRange = 10f;

        [SerializeField, Min(0f), Tooltip("Light intensity at the faint end of the pulse.")]
        private float _lightIntensityLow = 1.4f;

        [SerializeField, Min(0f), Tooltip("Light intensity at the bright end of the pulse.")]
        private float _lightIntensityHigh = 2.8f;

        [SerializeField, Tooltip("The colour of the light.")]
        private Color _lightColor = new Color(1f, 0.7f, 0.3f, 1f);

        [Header("Ring and motes")]
        [SerializeField, Tooltip("The faint ring on the ground while the nest is open. Its alpha is the faint value.")]
        private Color _ringColor = new Color(1f, 0.8f, 0.2f, 0.3f);

        [SerializeField, Tooltip("The colour of the ring that fills while the core collects the nest.")]
        private Color _fillColor = new Color(1f, 0.85f, 0.25f, 0.9f);

        [SerializeField, Tooltip("The colour of the gold motes.")]
        private Color _moteColor = new Color(1f, 0.85f, 0.25f, 1f);

        private Nest _nest;
        private NestConfig _config;
        private Camera _camera;
        private GameObject _nestObject;
        private Renderer[] _nestRenderers;
        private Vector3 _nestCenter;
        private GameObject _lightObject;
        private Light _light;
        private Renderer _groundRenderer;
        private Renderer _fillRenderer;
        private Mesh _groundMesh;
        private Mesh _fillMesh;
        private Mesh _dotMesh;
        private Transform[] _motes;
        private Renderer[] _moteRenderers;
        private MaterialPropertyBlock _block;
        private ShownState _state = ShownState.Closed;
        private int _shownSegments;
        private int _enabledMotes;
        private float _burstStart;
        private bool _isGone;

        private enum ShownState
        {
            Closed,
            Open,
            Collecting,
            Collected,
        }

        /// <summary>True while the nest is shown open: glowing, with its ring, whether or not it is being collected.</summary>
        public bool IsOpenShown => _state == ShownState.Open || _state == ShownState.Collecting;

        /// <summary>Supplies the nest to show and the nest config (collect radius). Call once, before this object starts.</summary>
        public void Initialize(Nest nest, NestConfig config)
        {
            if (nest == null)
            {
                throw new ArgumentNullException(nameof(nest), "NestView needs a nest.");
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "NestView needs the nest config for the collect ring.");
            }

            if (_nest != null)
            {
                throw new InvalidOperationException("NestView is already initialized.");
            }

            _nestObject = FindNestObject();
            _nest = nest;
            _config = config;
            _block = new MaterialPropertyBlock();
            transform.position = nest.Position;

            _nestRenderers = _nestObject.GetComponentsInChildren<Renderer>(true);
            _nestCenter = nest.Position + (Vector3.up * _lightHeight * 0.5f);
            if (_nestRenderers.Length > 0)
            {
                Bounds bounds = _nestRenderers[0].bounds;
                for (int index = 1; index < _nestRenderers.Length; index++)
                {
                    bounds.Encapsulate(_nestRenderers[index].bounds);
                }

                _nestCenter = bounds.center;
            }

            Material material = SharedMaterial();
            BuildLight();
            BuildRings(material);
            BuildMotes(material);

            if (nest.IsCollected)
            {
                Vanish();
                return;
            }

            ApplyState(ReadState());
        }

        private void Update()
        {
            if (_nest == null || _isGone)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            ShownState wanted = ReadState();
            if (wanted != _state)
            {
                ApplyState(wanted);
            }

            switch (_state)
            {
                case ShownState.Closed:
                    break;
                case ShownState.Open:
                    ShowGlow(0f);
                    ShowIdleMotes();
                    break;
                case ShownState.Collecting:
                    ShowGlow(_nest.Progress01);
                    ShowFill();
                    ShowStreamMotes();
                    break;
                case ShownState.Collected:
                    ShowBurst();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_groundMesh != null)
            {
                Destroy(_groundMesh);
            }

            if (_fillMesh != null)
            {
                Destroy(_fillMesh);
            }

            if (_dotMesh != null)
            {
                Destroy(_dotMesh);
            }
        }

        private ShownState ReadState()
        {
            if (_nest.IsCollected)
            {
                return ShownState.Collected;
            }

            if (!_nest.IsOpen)
            {
                return ShownState.Closed;
            }

            return _nest.Progress01 > 0f ? ShownState.Collecting : ShownState.Open;
        }

        // Runs only when the shown state changes: switches the parts on or off and sets the one-time look of the state.
        private void ApplyState(ShownState state)
        {
            _state = state;
            bool isOpen = state == ShownState.Open || state == ShownState.Collecting;

            // The light is never switched on or off: it stays enabled and goes to zero, so no frame pays for its first use.
            if (!isOpen)
            {
                _light.intensity = 0f;
            }

            _groundRenderer.enabled = isOpen;
            if (!isOpen || state == ShownState.Open)
            {
                SetFillSegments(0);
            }

            if (state == ShownState.Closed)
            {
                SetNestTint(_closedTint, Color.black);
                SetMotesEnabled(0);
            }
            else if (state == ShownState.Open)
            {
                SetMotesEnabled(IdleMoteCount);
            }
            else if (state == ShownState.Collecting)
            {
                SetMotesEnabled(MoteCount);
            }
            else
            {
                _burstStart = Time.time;
                SetFillSegments(0);
                SetMotesEnabled(MoteCount);
            }
        }

        // The pulse is one slow sine. Collecting brightens the glow with the progress, so the nest answers the core.
        private void ShowGlow(float progress)
        {
            float wave = 0.5f + (0.5f * Mathf.Sin(Time.time * _pulsesPerSecond * Mathf.PI * 2f));
            float boost = 1f + progress;
            Color tint = Color.Lerp(_openTintLow, _openTintHigh, wave);
            SetNestTint(tint, tint * (_emissionStrength * boost * (0.6f + (0.4f * wave))));
            _light.intensity = Mathf.Lerp(_lightIntensityLow, _lightIntensityHigh, wave) * boost;
        }

        private void ShowFill()
        {
            int segments = Mathf.CeilToInt(Mathf.Clamp01(_nest.Progress01) * RingSegments);
            SetFillSegments(segments);
        }

        // Shows only the first segments of the fill ring, which run clockwise from the top. A sub-mesh descriptor is a struct,
        // so this allocates nothing, and it runs only when the count changes.
        private void SetFillSegments(int segments)
        {
            if (segments == _shownSegments)
            {
                return;
            }

            _shownSegments = segments;
            _fillRenderer.enabled = segments > 0;
            if (segments > 0)
            {
                var descriptor = new SubMeshDescriptor(0, segments * 6, MeshTopology.Triangles);
                _fillMesh.SetSubMesh(0, descriptor, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            }
        }

        // A few motes drift up and around the nest and fade in and out, so an open nest looks alive even from far away.
        private void ShowIdleMotes()
        {
            float baseTime = Time.time / IdleCycleSeconds;
            Quaternion facing = Facing();
            for (int index = 0; index < IdleMoteCount; index++)
            {
                float t = Mathf.Repeat(baseTime + ((float)index / IdleMoteCount), 1f);
                float angle = (t * Mathf.PI * 2f) + (index * 1.7f);
                var offset = new Vector3(Mathf.Sin(angle) * IdleRadius, (t - 0.3f) * IdleRise, Mathf.Cos(angle) * IdleRadius);
                PlaceMote(index, _nestCenter + offset, facing, t);
            }
        }

        // Motes ride from the nest down to points spread around the ground circle, so the stream says "this is for that circle".
        private void ShowStreamMotes()
        {
            float baseTime = Time.time / StreamCycleSeconds;
            Quaternion facing = Facing();
            Vector3 ground = _nest.Position + (Vector3.up * GroundRingHeight);
            for (int index = 0; index < MoteCount; index++)
            {
                float t = Mathf.Repeat(baseTime + ((float)index / MoteCount), 1f);
                float angle = index * Mathf.PI * 2f / MoteCount;
                Vector3 target = ground + (new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * _config.CollectRadius);
                Vector3 position = Vector3.Lerp(_nestCenter, target, t);
                position.y += Mathf.Sin(t * Mathf.PI) * StreamArc;
                PlaceMote(index, position, facing, t);
            }
        }

        // The burst flies outward and up once, then the nest, its light and its rings are gone and only the tree stays.
        private void ShowBurst()
        {
            float t = (Time.time - _burstStart) / BurstSeconds;
            if (t >= 1f)
            {
                Vanish();
                return;
            }

            Quaternion facing = Facing();
            for (int index = 0; index < MoteCount; index++)
            {
                float angle = index * Mathf.PI * 2f / MoteCount;
                var outward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 position = _nestCenter + (outward * (BurstRadius * t));
                position.y += Mathf.Sin(t * Mathf.PI) * BurstLift;
                PlaceMote(index, position, facing, 1f - t);
            }
        }

        // Puts one mote down. Its alpha follows the cycle position: in at the start, out at the end.
        private void PlaceMote(int index, Vector3 position, Quaternion facing, float t)
        {
            _motes[index].SetPositionAndRotation(position, facing);
            Color color = _moteColor;
            color.a = _moteColor.a * Mathf.Clamp01(Mathf.Min(t, 1f - t) / MoteFadeShare);
            _block.SetColor(ColorId, color);
            _moteRenderers[index].SetPropertyBlock(_block);
        }

        private void Vanish()
        {
            _isGone = true;
            _state = ShownState.Collected;
            _nestObject.SetActive(false);
            _light.intensity = 0f;
            _groundRenderer.enabled = false;
            _fillRenderer.enabled = false;
            SetMotesEnabled(0);
        }

        private void SetMotesEnabled(int count)
        {
            if (count == _enabledMotes && count != 0)
            {
                return;
            }

            _enabledMotes = count;
            for (int index = 0; index < MoteCount; index++)
            {
                _moteRenderers[index].enabled = index < count;
            }
        }

        private void SetNestTint(Color tint, Color emission)
        {
            _block.SetColor(ColorId, tint);
            _block.SetColor(EmissionId, emission);
            for (int index = 0; index < _nestRenderers.Length; index++)
            {
                _nestRenderers[index].SetPropertyBlock(_block);
            }
        }

        private Quaternion Facing()
        {
            return _camera != null ? _camera.transform.rotation : Quaternion.identity;
        }

        // Raz's prefab stays as it is: the nest is found by name among the children, and a missing one is a setup mistake.
        private GameObject FindNestObject()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index] != transform && children[index].name == NestObjectName)
                {
                    return children[index].gameObject;
                }
            }

            throw new InvalidOperationException("NestView found no child named " + NestObjectName + ". Place the nest tree prefab under this object.");
        }

        private void BuildLight()
        {
            _lightObject = new GameObject("NestLight");
            _lightObject.transform.SetParent(transform, false);
            _lightObject.transform.position = _nest.Position + (Vector3.up * _lightHeight);
            _light = _lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = _lightRange;
            _light.color = _lightColor;
            _light.shadows = LightShadows.None;
            _light.renderMode = LightRenderMode.ForceVertex; // 'Not Important': the nest light never takes a pixel-light slot.
            _light.intensity = 0f;
        }

        private void BuildRings(Material material)
        {
            _groundMesh = BuildRingMesh(GroundRingInnerRatio);
            NewMeshObject("NestRing", _groundMesh, material, GroundRingHeight, out _groundRenderer);
            _block.SetColor(ColorId, _ringColor);
            _groundRenderer.SetPropertyBlock(_block);
            _groundRenderer.enabled = false;

            _fillMesh = BuildRingMesh(FillRingInnerRatio);
            NewMeshObject("NestFill", _fillMesh, FillMaterial(material), FillRingHeight, out _fillRenderer);
            _block.SetColor(ColorId, _fillColor);
            _fillRenderer.SetPropertyBlock(_block);
            _fillRenderer.enabled = false;
        }

        private Transform NewMeshObject(string objectName, Mesh mesh, Material material, float height, out Renderer renderer)
        {
            var ring = new GameObject(objectName);
            ring.transform.SetParent(transform, false);
            ring.transform.position = _nest.Position + (Vector3.up * height);
            ring.transform.localScale = new Vector3(_config.CollectRadius, 1f, _config.CollectRadius);
            ring.AddComponent<MeshFilter>().sharedMesh = mesh;

            var meshRenderer = ring.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            renderer = meshRenderer;
            return ring.transform;
        }

        // A fixed pool of centred dots, made once and moved in code; no allocation per frame.
        private void BuildMotes(Material material)
        {
            _dotMesh = BuildDotMesh();
            var root = new GameObject("NestMotes").transform;
            root.SetParent(transform, false);
            _motes = new Transform[MoteCount];
            _moteRenderers = new Renderer[MoteCount];
            for (int index = 0; index < MoteCount; index++)
            {
                var mote = new GameObject("Mote");
                mote.transform.SetParent(root, false);
                mote.transform.localScale = new Vector3(MoteSize, MoteSize, 1f);
                mote.AddComponent<MeshFilter>().sharedMesh = _dotMesh;
                var meshRenderer = mote.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.enabled = false;
                _motes[index] = mote.transform;
                _moteRenderers[index] = meshRenderer;
            }
        }

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

            s_sharedMaterial = new Material(shader) { name = "NestView Unlit", hideFlags = HideFlags.HideAndDontSave };
            return s_sharedMaterial;
        }

        // The fill ring lies a little above the faint ring and draws later, so the faint ring never darkens it.
        private static Material FillMaterial(Material shared)
        {
            if (s_fillMaterial == null)
            {
                s_fillMaterial = new Material(shared)
                {
                    name = "NestView Unlit Fill",
                    hideFlags = HideFlags.HideAndDontSave,
                    renderQueue = shared.renderQueue + 1,
                };
            }

            return s_fillMaterial;
        }

        // A round dot of diameter 1, centred, facing -z.
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

            var mesh = new Mesh { name = "NestView Dot", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        // A flat ring of outer radius 1 on the ground plane. Its segments run clockwise seen from above, starting at the top
        // (+z), and its triangles are in that order, so drawing the first n segments draws the ring filled n/64 of the way.
        private static Mesh BuildRingMesh(float innerRatio)
        {
            var vertices = new Vector3[RingSegments * 2];
            var triangles = new int[RingSegments * 6];
            for (int segment = 0; segment < RingSegments; segment++)
            {
                float angle = segment * Mathf.PI * 2f / RingSegments;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                vertices[segment * 2] = direction;
                vertices[(segment * 2) + 1] = direction * innerRatio;

                int next = (segment + 1) % RingSegments;
                int outer = segment * 2;
                int inner = outer + 1;
                int nextOuter = next * 2;
                int nextInner = nextOuter + 1;
                int t = segment * 6;
                triangles[t] = outer;
                triangles[t + 1] = inner;
                triangles[t + 2] = nextOuter;
                triangles[t + 3] = inner;
                triangles[t + 4] = nextInner;
                triangles[t + 5] = nextOuter;
            }

            var mesh = new Mesh { name = "NestView Ring", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
