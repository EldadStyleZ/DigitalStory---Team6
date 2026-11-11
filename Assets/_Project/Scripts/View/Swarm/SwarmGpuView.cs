using System;
using System.Runtime.InteropServices;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DigitalStory.View
{
    /// <summary>
    /// Moves and draws every wasp of the swarm on the GPU, with one compute dispatch and two indirect draws a frame:
    /// the wasps, then the soft glow around each. Gameplay never touches a wasp. It feeds this view the live count, the
    /// form, the colour of the last song cast and a few positions.
    /// </summary>
    /// <remarks>
    /// Why the GPU and not Jobs: docs/CODING_STANDARDS.md says "profile first, then Jobs and Burst". We go to the GPU
    /// on purpose, because thousands of independently animated agents is a known GPU workload, and DOTS would need four
    /// new packages, which is a team decision. This runs in the Built-in Render Pipeline with no new package, the CPU
    /// cost does not change with the number of wasps, and the CPU never reads GPU data back. Spec F-010, section 9.1.
    /// The glow stands in for bloom, which would need the post-processing package.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // Runs after the scripts that feed it, so the wasps never lag a frame behind the rules.
    [AddComponentMenu("DigitalStory/Swarm GPU View")]
    public sealed class SwarmGpuView : MonoBehaviour
    {
        private const string SimulateKernelName = "Simulate";
        private const string ResetKernelName = "ResetWasps";

        // Matches THREAD_GROUP_SIZE in WaspSwarm.compute.
        private const int ThreadsPerGroup = 64;
        private const int MostBones = 64;
        private const int LayoutSeed = 1010;
        private const double GoldenRatioFraction = 0.6180339887498949;
        private const float FullTurn = Mathf.PI * 2f;
        private const float MinimumSquaredLength = 0.000001f;
        private const float StandsFlag = 1f;
        private const float FallenFlag = 0f;

        // Bytes per element. Each matches a struct in WaspSwarm.compute, WaspInstanced.shader and WaspGlow.shader.
        private const int StaticStride = sizeof(float) * 12;
        private const int DynamicStride = sizeof(float) * 12;
        private const int TrailStride = sizeof(float) * 4;
        private const int BoneStride = sizeof(float) * 16;

        private static readonly int StaticId = Shader.PropertyToID("_Static");
        private static readonly int DynamicId = Shader.PropertyToID("_Dynamic");
        private static readonly int TrailId = Shader.PropertyToID("_Trail");
        private static readonly int BonesId = Shader.PropertyToID("_Bones");
        private static readonly int MaxWaspsId = Shader.PropertyToID("_MaxWasps");
        private static readonly int ActiveCountId = Shader.PropertyToID("_ActiveCount");
        private static readonly int TrailCountId = Shader.PropertyToID("_TrailCount");
        private static readonly int BoneCountId = Shader.PropertyToID("_BoneCount");
        private static readonly int FormId = Shader.PropertyToID("_Form");
        private static readonly int PreviousFormId = Shader.PropertyToID("_PreviousForm");
        private static readonly int SimTimeId = Shader.PropertyToID("_SimTime");
        private static readonly int DeltaTimeId = Shader.PropertyToID("_DeltaTime");
        private static readonly int FormStartTimeId = Shader.PropertyToID("_FormStartTime");
        private static readonly int PreviousFormStartTimeId = Shader.PropertyToID("_PreviousFormStartTime");
        private static readonly int PiperPositionId = Shader.PropertyToID("_PiperPosition");
        private static readonly int PiperForwardId = Shader.PropertyToID("_PiperForward");
        private static readonly int StingId = Shader.PropertyToID("_Sting");
        private static readonly int HiveAId = Shader.PropertyToID("_HiveA");
        private static readonly int HiveBId = Shader.PropertyToID("_HiveB");
        private static readonly int LastHitId = Shader.PropertyToID("_LastHit");
        private static readonly int MeteorId = Shader.PropertyToID("_Meteor");
        private static readonly int MeteorRiseSecondsId = Shader.PropertyToID("_MeteorRiseSeconds");
        private static readonly int MeteorDiveSecondsId = Shader.PropertyToID("_MeteorDiveSeconds");
        private static readonly int MeteorHeightId = Shader.PropertyToID("_MeteorHeight");
        private static readonly int RiverColorId = Shader.PropertyToID("_RiverColor");
        private static readonly int CastColorId = Shader.PropertyToID("_CastColor");
        private static readonly int PreviousCastColorId = Shader.PropertyToID("_PreviousCastColor");
        private static readonly int RiverSpringId = Shader.PropertyToID("_RiverSpring");
        private static readonly int RiverDampingId = Shader.PropertyToID("_RiverDamping");
        private static readonly int ShapeSpringId = Shader.PropertyToID("_ShapeSpring");
        private static readonly int ShapeDampingId = Shader.PropertyToID("_ShapeDamping");
        private static readonly int WobbleDampingId = Shader.PropertyToID("_WobbleDamping");
        private static readonly int WobbleSecondsId = Shader.PropertyToID("_WobbleSeconds");
        private static readonly int FormWaveSecondsId = Shader.PropertyToID("_FormWaveSeconds");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int RiverLengthId = Shader.PropertyToID("_RiverLength");
        private static readonly int RiverRadiusId = Shader.PropertyToID("_RiverRadius");
        private static readonly int MeanderAmplitudeId = Shader.PropertyToID("_MeanderAmplitude");
        private static readonly int StandingTrailMetersId = Shader.PropertyToID("_StandingTrailMeters");
        private static readonly int StandingShareId = Shader.PropertyToID("_StandingShare");
        private static readonly int TorusRadiusId = Shader.PropertyToID("_TorusRadius");
        private static readonly int DyingSecondsId = Shader.PropertyToID("_DyingSeconds");
        private static readonly int DomeRadiusId = Shader.PropertyToID("_DomeRadius");
        private static readonly int TornadoRadiusId = Shader.PropertyToID("_TornadoRadius");
        private static readonly int MeteorRadiusId = Shader.PropertyToID("_MeteorRadius");
        private static readonly int WaspLengthId = Shader.PropertyToID("_WaspLength");
        private static readonly int BuzzAmplitudeId = Shader.PropertyToID("_BuzzAmplitude");
        private static readonly int BuzzFrequencyId = Shader.PropertyToID("_BuzzFrequency");
        private static readonly int GlowSizeId = Shader.PropertyToID("_GlowSize");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");

        [SerializeField, Tooltip("WaspSwarm.compute, which moves the wasps.")]
        private ComputeShader _swarmCompute;

        [SerializeField, Tooltip("WaspInstanced.shader, which draws them. This view makes its own material from it.")]
        private Shader _waspShader;

        [SerializeField, Tooltip("WaspGlow.shader, which draws the soft glow around each wasp. This view makes its own material from it.")]
        private Shader _glowShader;

        private SwarmConfig _config;
        private PathTrail _trail;
        private Mesh _mesh;
        private Material _material;
        private RenderParams _renderParams;
        private Mesh _glowMesh;
        private Material _glowMaterial;
        private RenderParams _glowRenderParams;
        private int _simulateKernel;
        private int _resetKernel;
        private int _maxWasps;
        private int _groupCount;

        private WaspStatic[] _staticData;
        private Vector4[] _trailPoints;
        private GraphicsBuffer.IndirectDrawIndexedArgs[] _drawArgs;

        private GraphicsBuffer _staticBuffer;
        private GraphicsBuffer _dynamicBuffer;
        private GraphicsBuffer _trailBuffer;
        private GraphicsBuffer _boneBuffer;
        private GraphicsBuffer _argsBuffer;
        private GraphicsBuffer _glowArgsBuffer;
        private bool _needsReset;

        // The colour of the last song cast, the colour the current form shows, and the colour the form before showed.
        // The two form colours are latched once a frame, so SetForm and SetCastColor may come in either order.
        private Color _castColor;
        private Color _formColor;
        private Color _previousFormColor;
        private bool _hasFormChanged;

        private float _simTime;
        private int _activeCount;
        private SwarmForm _form = SwarmForm.River;
        private SwarmForm _previousForm = SwarmForm.River;
        private float _formStartTime;
        private float _previousFormStartTime;
        private Vector3 _piperPosition;
        private Vector3 _piperForward = Vector3.forward;
        private Vector3 _standingAnchor;
        private float _standingSeconds;
        private float _standingShare;
        private Vector3 _stingTarget;
        private float _stingFraction;
        private Vector4 _firstHive;
        private Vector4 _secondHive;
        private Vector3 _lastHit;
        private Vector3 _meteorTarget;
        private float _meteorFraction;
        private float _meteorRiseSeconds;
        private float _meteorDiveSeconds;
        private float _meteorHeight;
        private float _domeRadius;
        private float _tornadoRadius;
        private float _meteorRadius;
        private int _boneCount;
        private bool _hasColossusSamples;

        /// <summary>True while the buffers exist and the swarm is moved and drawn each frame, for tests and debugging.</summary>
        public bool IsRunning => _staticBuffer != null;

        /// <summary>The colour of the last song cast, which every form but the River shows, for tests and debugging.</summary>
        public Color CastColor => _castColor;

        /// <summary>How far the river has curled into the ring around the standing Piper: 0 while it flows behind them, 1 once it is a ring.</summary>
        public float StandingShare => _standingShare;

        private void OnEnable()
        {
            if (_config != null)
            {
                CreateBuffers();
            }
        }

        private void LateUpdate()
        {
            if (!IsRunning)
            {
                return;
            }

            float deltaTime = Mathf.Min(Time.deltaTime, _config.MaxDeltaTime);
            _simTime += deltaTime;
            TrackStanding(deltaTime);

            int trailCount = _trail.CopyTo(_trailPoints);
            if (trailCount > 0)
            {
                _trailBuffer.SetData(_trailPoints, 0, 0, trailCount);
            }

            LatchFormColors();
            BindComputeBuffers();
            PushFrame(deltaTime, trailCount);
            PushConfig();
            PushColors();

            if (_needsReset)
            {
                _swarmCompute.Dispatch(_resetKernel, _groupCount, 1, 1);
                _needsReset = false;
            }

            _swarmCompute.Dispatch(_simulateKernel, _groupCount, 1, 1);

            var bounds = new Bounds(_piperPosition, Vector3.one * (_config.BoundsMeters * 2f));
            _renderParams.worldBounds = bounds;
            Graphics.RenderMeshIndirect(_renderParams, _mesh, _argsBuffer);

            if (_glowArgsBuffer != null)
            {
                _glowRenderParams.worldBounds = bounds;
                Graphics.RenderMeshIndirect(_glowRenderParams, _glowMesh, _glowArgsBuffer);
            }
        }

        private void OnDisable() => ReleaseBuffers();

        private void OnDestroy()
        {
            ReleaseBuffers();
            DestroyOwnedAssets();
        }

        /// <summary>
        /// Supplies the swarm's settings and the Piper's trail, and starts the count at SwarmConfig.StartingWasps.
        /// Call before this object starts; calling again (a restart) starts the swarm over.
        /// Logs an error and switches this view off when a wasp shader is missing or the device has no compute shaders.
        /// Logs an error and draws the wasps without their glow when only the glow shader is missing.
        /// </summary>
        public void Initialize(SwarmConfig config, PathTrail trail)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (trail == null)
            {
                throw new ArgumentNullException(nameof(trail));
            }

            if (_config != null)
            {
                StartOver();
            }

            if (!CanRun())
            {
                enabled = false;
                return;
            }

            _simulateKernel = _swarmCompute.FindKernel(SimulateKernelName);
            _resetKernel = _swarmCompute.FindKernel(ResetKernelName);
            if (!_swarmCompute.IsSupported(_simulateKernel) || !_swarmCompute.IsSupported(_resetKernel))
            {
                Debug.LogError($"{name}: this device cannot run WaspSwarm.compute, so the wasp swarm is off.", this);
                enabled = false;
                return;
            }

            _trail = trail;
            _maxWasps = config.MaxWasps;
            _groupCount = (_maxWasps + ThreadsPerGroup - 1) / ThreadsPerGroup;
            _activeCount = config.StartingWasps;
            _mesh = WaspMeshFactory.Create();
            _material = new Material(_waspShader) { name = "Wasp Swarm" };
            if (CanGlow())
            {
                _glowMesh = WaspMeshFactory.CreateGlowQuad();
                _glowMaterial = new Material(_glowShader) { name = "Wasp Swarm Glow" };
            }

            // A restart keeps the layout and any Colossus samples, unless the wasp count changed.
            if (_staticData == null || _staticData.Length != _maxWasps)
            {
                _staticData = BuildLayout(_maxWasps);
                _hasColossusSamples = false;
            }

            _trailPoints = new Vector4[config.TrailPoints];
            _drawArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
            _castColor = config.RiverColor;
            _formColor = config.RiverColor;
            _previousFormColor = config.RiverColor;
            _config = config;

            if (isActiveAndEnabled)
            {
                CreateBuffers();
            }
        }

        /// <summary>How many wasps are alive. Wasps above it die where they are, and dead ones below it come back at a hive.</summary>
        public void SetActiveCount(int count) => _activeCount = Mathf.Max(0, count);

        /// <summary>Changes the swarm's form. The change sweeps through the swarm as a wave.</summary>
        public void SetForm(SwarmForm form)
        {
            if (form == _form)
            {
                return;
            }

            _previousForm = _form;
            _previousFormStartTime = _formStartTime;
            _form = form;
            _formStartTime = _simTime;
            _hasFormChanged = true;
        }

        /// <summary>
        /// The colour every form but the River shows until the next call: the colour of the category of the song just
        /// cast, from SwarmConfig.ColorOf. The River and the collapsed swarm always show SwarmConfig.RiverColor. Each wasp
        /// takes the new colour as the form's wave reaches it, so the colour flows through the swarm with the motion.
        /// </summary>
        public void SetCastColor(Color color) => _castColor = color;

        /// <summary>Where the Piper stands and faces. Only the facing on the ground is used.</summary>
        public void SetPiper(Vector3 position, Vector3 forward)
        {
            _piperPosition = position;
            forward.y = 0f;
            if (forward.sqrMagnitude > MinimumSquaredLength)
            {
                _piperForward = forward.normalized;
            }
        }

        /// <summary>What the sting wraps, and the share of the swarm (0 to 1) that flies there.</summary>
        public void SetSting(Vector3 target, float fraction)
        {
            _stingTarget = target;
            _stingFraction = Mathf.Clamp01(fraction);
        }

        /// <summary>
        /// The meteor: where it lands, the share of the swarm (0 to 1) that flies it, the seconds from the cast to the
        /// top of its climb, the seconds of its dive, and how many metres above the Piper it climbs. Read while the form
        /// is Meteor, and timed from the moment the form changes to it.
        /// </summary>
        public void SetMeteor(Vector3 target, float fraction, float riseSeconds, float diveSeconds, float height)
        {
            _meteorTarget = target;
            _meteorFraction = Mathf.Clamp01(fraction);
            _meteorRiseSeconds = Mathf.Max(0f, riseSeconds);
            _meteorDiveSeconds = Mathf.Max(0f, diveSeconds);
            _meteorHeight = Mathf.Max(0f, height);
        }

        /// <summary>Where both hives are and whether each still stands. Revived wasps come from a standing hive.</summary>
        public void SetHives(Vector3 first, Vector3 second, bool firstStands, bool secondStands)
        {
            _firstHive = new Vector4(first.x, first.y, first.z, firstStands ? StandsFlag : FallenFlag);
            _secondHive = new Vector4(second.x, second.y, second.z, secondStands ? StandsFlag : FallenFlag);
        }

        /// <summary>Where the swarm was last hit. Lost wasps scatter away from it.</summary>
        public void SetLastHit(Vector3 point) => _lastHit = point;

        /// <summary>The Colossus's bones, local to world, for this frame. Call each frame while the Colossus is active.</summary>
        public void SetColossusBones(Matrix4x4[] bones, int count)
        {
            if (bones == null)
            {
                throw new ArgumentNullException(nameof(bones));
            }

            if (_boneBuffer == null)
            {
                return;
            }

            _boneCount = Mathf.Clamp(count, 0, Mathf.Min(bones.Length, MostBones));
            if (_boneCount > 0)
            {
                _boneBuffer.SetData(bones, 0, 0, _boneCount);
            }
        }

        /// <summary>
        /// Gives each wasp its place on the Colossus, sampled from the puppet's mesh (spec 9.5). Call once, after
        /// Initialize. Wasp i takes sample i, wrapping when there are fewer samples than wasps. Until this is called,
        /// the Colossus form shows the river.
        /// </summary>
        /// <param name="boneIndices">The bone each sample follows, 0 to 63.</param>
        /// <param name="boneOffsets">Each sample's point in its bone's space.</param>
        /// <param name="heights01">Each sample's height on the body: 0 at the feet, 1 at the head.</param>
        public void SetColossusSamples(int[] boneIndices, Vector3[] boneOffsets, float[] heights01)
        {
            if (_staticData == null)
            {
                throw new InvalidOperationException("Call Initialize before SetColossusSamples.");
            }

            if (boneIndices == null)
            {
                throw new ArgumentNullException(nameof(boneIndices));
            }

            if (boneOffsets == null)
            {
                throw new ArgumentNullException(nameof(boneOffsets));
            }

            if (heights01 == null)
            {
                throw new ArgumentNullException(nameof(heights01));
            }

            int sampleCount = Mathf.Min(boneIndices.Length, Mathf.Min(boneOffsets.Length, heights01.Length));
            if (sampleCount == 0)
            {
                return;
            }

            for (int wasp = 0; wasp < _staticData.Length; wasp++)
            {
                int sample = wasp % sampleCount;
                _staticData[wasp].BoneIndex = Mathf.Clamp(boneIndices[sample], 0, MostBones - 1);
                _staticData[wasp].BoneOffset = boneOffsets[sample];
                _staticData[wasp].Height01 = Mathf.Clamp01(heights01[sample]);
            }

            _hasColossusSamples = true;
            _staticBuffer?.SetData(_staticData);
        }

        /// <summary>The dome's radius and the tornado's outer radius from the song assets, so the wasps match the rules. 0 keeps the shader's default.</summary>
        public void SetFormRadii(float domeRadius, float tornadoRadius)
        {
            _domeRadius = Mathf.Max(0f, domeRadius);
            _tornadoRadius = Mathf.Max(0f, tornadoRadius);
        }

        /// <summary>The meteor's reach from MeteorSong.Radius, so the ring the wasps burst out to after the impact matches the rule. 0 keeps the shader's default.</summary>
        public void SetMeteorRadius(float radius) => _meteorRadius = Mathf.Max(0f, radius);

        private static WaspStatic[] BuildLayout(int count)
        {
            var random = new System.Random(LayoutSeed);
            var layout = new WaspStatic[count];

            for (int index = 0; index < count; index++)
            {
                layout[index] = new WaspStatic
                {
                    Seed = NextFloat(random),

                    // Golden-ratio steps spread any first N wasps evenly, so the river stays even as the count changes.
                    Lane = (float)(index * GoldenRatioFraction % 1.0),
                    RingAngle = NextFloat(random) * FullTurn,

                    // The square root spreads wasps evenly over the cross-section's area, not bunched at its centre.
                    RingRadius = Mathf.Sqrt(NextFloat(random)),
                    BoneOffset = Vector3.zero,
                    BoneIndex = 0f,
                    Height01 = NextFloat(random),
                    Level01 = NextFloat(random),
                    Phase01 = NextFloat(random),
                    Unused = 0f,
                };
            }

            return layout;
        }

        private static float NextFloat(System.Random random) => (float)random.NextDouble();

        private static Vector4 ShaderColor(Color color, bool isLinear) => isLinear ? color.linear : color;

        private static void Release(ref GraphicsBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

        private bool CanRun()
        {
            if (_swarmCompute == null || _waspShader == null)
            {
                Debug.LogError($"{name}: SwarmGpuView needs WaspSwarm.compute and WaspInstanced.shader. Rebuild the scene or fill them in.", this);
                return false;
            }

            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError($"{name}: this device has no compute shaders, so the wasp swarm cannot run.", this);
                return false;
            }

            if (!_swarmCompute.HasKernel(SimulateKernelName) || !_swarmCompute.HasKernel(ResetKernelName))
            {
                Debug.LogError($"{name}: WaspSwarm.compute is missing a kernel. Check it for compile errors.", this);
                return false;
            }

            if (!_waspShader.isSupported)
            {
                Debug.LogError($"{name}: WaspInstanced.shader does not run on this device. Check it for compile errors.", this);
                return false;
            }

            return true;
        }

        // The glow only dresses the wasps, so without it the swarm still runs, but the missing glow is reported.
        private bool CanGlow()
        {
            if (_glowShader == null)
            {
                Debug.LogError($"{name}: SwarmGpuView needs WaspGlow.shader to draw the wasps' glow. Rebuild the scene or fill it in.", this);
                return false;
            }

            if (!_glowShader.isSupported)
            {
                Debug.LogError($"{name}: WaspGlow.shader does not run on this device, so the wasps draw without their glow. Check it for compile errors.", this);
                return false;
            }

            return true;
        }

        private void CreateBuffers()
        {
            if (IsRunning)
            {
                return;
            }

            _staticBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _maxWasps, StaticStride);
            _staticBuffer.SetData(_staticData);
            _dynamicBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _maxWasps, DynamicStride);
            _trailBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _trailPoints.Length, TrailStride);
            _boneBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MostBones, BoneStride);
            _argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

            // The draw always covers every wasp. Dead ones are drawn at scale 0, so they cost nothing visible.
            _drawArgs[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = _mesh.GetIndexCount(0),
                instanceCount = (uint)_maxWasps,
                startIndex = _mesh.GetIndexStart(0),
                baseVertexIndex = _mesh.GetBaseVertex(0),
                startInstance = 0,
            };
            _argsBuffer.SetData(_drawArgs);

            _material.SetBuffer(StaticId, _staticBuffer);
            _material.SetBuffer(DynamicId, _dynamicBuffer);
            _renderParams = new RenderParams(_material)
            {
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                layer = gameObject.layer,
            };

            if (_glowMaterial != null)
            {
                CreateGlowBuffers();
            }

            // New buffers hold nothing yet, so the next frame fills the dynamic one before it moves anything.
            _boneCount = 0;
            _needsReset = true;
        }

        // One glow quad per wasp, from the same wasp buffers. Its material's queue (Transparent) draws it after the
        // opaque geometry, wasps included.
        private void CreateGlowBuffers()
        {
            _glowArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            _drawArgs[0] = new GraphicsBuffer.IndirectDrawIndexedArgs
            {
                indexCountPerInstance = _glowMesh.GetIndexCount(0),
                instanceCount = (uint)_maxWasps,
                startIndex = _glowMesh.GetIndexStart(0),
                baseVertexIndex = _glowMesh.GetBaseVertex(0),
                startInstance = 0,
            };
            _glowArgsBuffer.SetData(_drawArgs);

            _glowMaterial.SetBuffer(StaticId, _staticBuffer);
            _glowMaterial.SetBuffer(DynamicId, _dynamicBuffer);
            _glowRenderParams = new RenderParams(_glowMaterial)
            {
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                layer = gameObject.layer,
            };
        }

        private void StartOver()
        {
            ReleaseBuffers();
            DestroyOwnedAssets();
            _config = null;
            _simTime = 0f;
            _form = SwarmForm.River;
            _previousForm = SwarmForm.River;
            _formStartTime = 0f;
            _previousFormStartTime = 0f;
            _standingSeconds = 0f;
            _standingShare = 0f;
            _hasFormChanged = false;
        }

        // Spec 9.3: while the Piper stands, the river curls into a torus around them. The PathTrail never shrinks while
        // the Piper stands (it only stores points as they walk), so its length cannot tell standing from walking. This
        // measures standing directly instead: the Piper stands while they stay within StandStillMeters (on the ground)
        // of where they stopped, so settling never counts as a step. After StandDelaySeconds the river eases into the
        // torus over StandBlendSeconds, and eases back as soon as they walk.
        private void TrackStanding(float deltaTime)
        {
            float stillMeters = _config.StandStillMeters;
            Vector3 drift = _piperPosition - _standingAnchor;
            drift.y = 0f;
            if (drift.sqrMagnitude > stillMeters * stillMeters)
            {
                _standingAnchor = _piperPosition;
                _standingSeconds = 0f;
            }
            else
            {
                _standingSeconds += deltaTime;
            }

            float goal = _standingSeconds >= _config.StandDelaySeconds ? 1f : 0f;
            float blendSeconds = _config.StandBlendSeconds;
            float blendStep = blendSeconds > 0f ? deltaTime / blendSeconds : 1f;
            _standingShare = Mathf.MoveTowards(_standingShare, goal, blendStep);
        }

        private void DestroyOwnedAssets()
        {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
                _mesh = null;
            }

            if (_glowMaterial != null)
            {
                Destroy(_glowMaterial);
                _glowMaterial = null;
            }

            if (_glowMesh != null)
            {
                Destroy(_glowMesh);
                _glowMesh = null;
            }
        }

        private void ReleaseBuffers()
        {
            Release(ref _staticBuffer);
            Release(ref _dynamicBuffer);
            Release(ref _trailBuffer);
            Release(ref _boneBuffer);
            Release(ref _argsBuffer);
            Release(ref _glowArgsBuffer);
        }

        // Bound every frame, because the bindings live on the shared ComputeShader asset and are lost if it reloads.
        private void BindComputeBuffers()
        {
            _swarmCompute.SetBuffer(_simulateKernel, StaticId, _staticBuffer);
            _swarmCompute.SetBuffer(_simulateKernel, DynamicId, _dynamicBuffer);
            _swarmCompute.SetBuffer(_simulateKernel, TrailId, _trailBuffer);
            _swarmCompute.SetBuffer(_simulateKernel, BonesId, _boneBuffer);
            _swarmCompute.SetBuffer(_resetKernel, StaticId, _staticBuffer);
            _swarmCompute.SetBuffer(_resetKernel, DynamicId, _dynamicBuffer);
        }

        private void PushFrame(float deltaTime, int trailCount)
        {
            ComputeShader compute = _swarmCompute;
            compute.SetInt(MaxWaspsId, _maxWasps);
            compute.SetInt(ActiveCountId, Mathf.Min(_activeCount, _maxWasps));
            compute.SetInt(TrailCountId, trailCount);
            compute.SetInt(BoneCountId, _hasColossusSamples ? _boneCount : 0);
            compute.SetInt(FormId, (int)_form);
            compute.SetInt(PreviousFormId, (int)_previousForm);
            compute.SetFloat(SimTimeId, _simTime);
            compute.SetFloat(DeltaTimeId, deltaTime);
            compute.SetFloat(FormStartTimeId, _formStartTime);
            compute.SetFloat(PreviousFormStartTimeId, _previousFormStartTime);
            compute.SetVector(PiperPositionId, _piperPosition);
            compute.SetVector(PiperForwardId, _piperForward);
            compute.SetFloat(StandingShareId, _standingShare);
            compute.SetVector(StingId, new Vector4(_stingTarget.x, _stingTarget.y, _stingTarget.z, _stingFraction));
            compute.SetVector(HiveAId, _firstHive);
            compute.SetVector(HiveBId, _secondHive);
            compute.SetVector(LastHitId, _lastHit);
            compute.SetVector(MeteorId, new Vector4(_meteorTarget.x, _meteorTarget.y, _meteorTarget.z, _meteorFraction));
            compute.SetFloat(MeteorRiseSecondsId, _meteorRiseSeconds);
            compute.SetFloat(MeteorDiveSecondsId, _meteorDiveSeconds);
            compute.SetFloat(MeteorHeightId, _meteorHeight);
            compute.SetFloat(DomeRadiusId, _domeRadius);
            compute.SetFloat(TornadoRadiusId, _tornadoRadius);
            compute.SetFloat(MeteorRadiusId, _meteorRadius);
        }

        // Pushed every frame, so values tuned on the config asset in Play Mode show at once.
        private void PushConfig()
        {
            ComputeShader compute = _swarmCompute;
            compute.SetFloat(RiverSpringId, _config.RiverSpring);
            compute.SetFloat(RiverDampingId, _config.RiverDamping);
            compute.SetFloat(ShapeSpringId, _config.ShapeSpring);
            compute.SetFloat(ShapeDampingId, _config.ShapeDamping);
            compute.SetFloat(WobbleDampingId, _config.WobbleDamping);
            compute.SetFloat(WobbleSecondsId, _config.WobbleSeconds);
            compute.SetFloat(FormWaveSecondsId, _config.FormWaveSeconds);
            compute.SetFloat(FlowSpeedId, _config.FlowSpeed);
            compute.SetFloat(RiverLengthId, _config.RiverLengthMeters);
            compute.SetFloat(RiverRadiusId, _config.RiverRadius);
            compute.SetFloat(MeanderAmplitudeId, _config.MeanderAmplitude);
            compute.SetFloat(StandingTrailMetersId, _config.StandingTrailMeters);
            compute.SetFloat(TorusRadiusId, _config.TorusRadius);
            compute.SetFloat(DyingSecondsId, _config.DyingSeconds);

            _material.SetFloat(WaspLengthId, _config.WaspLength);
            _material.SetFloat(DyingSecondsId, _config.DyingSeconds);
            _material.SetFloat(BuzzAmplitudeId, _config.BuzzAmplitude);
            _material.SetFloat(BuzzFrequencyId, _config.BuzzFrequency);

            if (_glowMaterial != null)
            {
                _glowMaterial.SetFloat(GlowSizeId, _config.GlowSize);
                _glowMaterial.SetFloat(GlowStrengthId, _config.GlowStrength);
                _glowMaterial.SetFloat(DyingSecondsId, _config.DyingSeconds);
                _glowMaterial.SetFloat(BuzzAmplitudeId, _config.BuzzAmplitude);
                _glowMaterial.SetFloat(BuzzFrequencyId, _config.BuzzFrequency);
            }
        }

        // Once a frame, after everything that feeds this view has run: on a change of form, the colour the old form
        // showed becomes the colour of the wasps its wave has not reached yet. A SetCastColor in the same frame, before
        // or after the SetForm, colours the new form.
        private void LatchFormColors()
        {
            if (_hasFormChanged)
            {
                _previousFormColor = _formColor;
                _hasFormChanged = false;
            }

            _formColor = _castColor;
        }

        // The compute shader writes these colours straight into the draw, so they skip Unity's colour-space conversion.
        private void PushColors()
        {
            bool isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            ComputeShader compute = _swarmCompute;
            compute.SetVector(RiverColorId, ShaderColor(_config.RiverColor, isLinear));
            compute.SetVector(CastColorId, ShaderColor(_formColor, isLinear));
            compute.SetVector(PreviousCastColorId, ShaderColor(_previousFormColor, isLinear));
        }

        // 48 bytes, the same layout as WaspStatic in WaspSwarm.compute, WaspInstanced.shader and WaspGlow.shader.
        [StructLayout(LayoutKind.Sequential)]
        private struct WaspStatic
        {
            public float Seed;
            public float Lane;
            public float RingAngle;
            public float RingRadius;
            public Vector3 BoneOffset;
            public float BoneIndex;
            public float Height01;
            public float Level01;
            public float Phase01;
            public float Unused;
        }
    }
}
