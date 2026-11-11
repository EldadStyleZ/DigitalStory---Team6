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
    /// whole-swarm form and its colour, the three song slots, the Gust and a few positions.
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

        // Each wasp's squad value is frac(SquadStart + index x SquadStep) (songs-v2, section C). The step is not the
        // golden ratio the lanes use, so every band of squad values is spread evenly along the whole river.
        private const double SquadStart = 0.5;
        private const double SquadStep = 0.7548777;

        // One slot per DigitalStory.Logic.SongSlot, and three float4s per slot. Match SLOT_COUNT and SLOT_VECTORS in
        // WaspSwarm.compute.
        private const int SlotCount = (int)SongSlot.Strike + 1;
        private const int VectorsPerSlot = 3;
        private const int ParamAOffset = 1;
        private const int ParamBOffset = 2;

        // The compute shader reads these as "no song in the slot" and "the song has not been released".
        private const float EmptySlotForm = -1f;
        private const float NotReleased = -1f;

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
        private static readonly int CorePositionId = Shader.PropertyToID("_CorePosition");
        private static readonly int CoreForwardId = Shader.PropertyToID("_CoreForward");
        private static readonly int HiveAId = Shader.PropertyToID("_HiveA");
        private static readonly int HiveBId = Shader.PropertyToID("_HiveB");
        private static readonly int LastHitId = Shader.PropertyToID("_LastHit");
        private static readonly int SlotsId = Shader.PropertyToID("_Slots");
        private static readonly int SlotColorsId = Shader.PropertyToID("_SlotColors");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
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
        private static readonly int CoreRingShareId = Shader.PropertyToID("_CoreRingShare");
        private static readonly int CoreRingRadiusId = Shader.PropertyToID("_CoreRingRadius");
        private static readonly int CoreRingGlowId = Shader.PropertyToID("_CoreRingGlow");
        private static readonly int CoreClearRadiusId = Shader.PropertyToID("_CoreClearRadius");
        private static readonly int CoreClearSpeedId = Shader.PropertyToID("_CoreClearSpeed");
        private static readonly int DyingSecondsId = Shader.PropertyToID("_DyingSeconds");
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
        private Vector3 _corePosition;
        private Vector3 _coreForward = Vector3.forward;
        private Vector3 _standingAnchor;
        private float _standingSeconds;
        private float _standingShare;
        private Vector4 _firstHive;
        private Vector4 _secondHive;
        private Vector3 _lastHit;
        private int _boneCount;
        private bool _hasColossusSamples;

        // The song slots as WaspSwarm.compute reads them (_Slots and _SlotColors), made once and refilled in place.
        // SetSlot keeps the rules' clocks (seconds since the cast and the release) until LateUpdate has stepped this
        // view's clock, and only then turns them into its times, so a song's beats land on the frame the rules say.
        private readonly Vector4[] _slotVectors = new Vector4[SlotCount * VectorsPerSlot];
        private readonly Vector4[] _slotColorVectors = new Vector4[SlotCount];
        private readonly Color[] _slotColors = new Color[SlotCount];
        private readonly float[] _slotSeconds = new float[SlotCount];
        private readonly float[] _slotReleasedSeconds = new float[SlotCount];
        private readonly bool[] _isSlotFresh = new bool[SlotCount];

        // The Gust pulse, kept the same way: the rules' seconds since the Gust until LateUpdate turns them into a start.
        private Vector3 _gustCenter;
        private float _gustRadius;
        private float _gustSeconds;
        private float _gustStartTime;
        private bool _isGustFresh;

        /// <summary>True while the buffers exist and the swarm is moved and drawn each frame, for tests and debugging.</summary>
        public bool IsRunning => _staticBuffer != null;

        /// <summary>The colour last given to <see cref="SetCastColor"/>, which Hive Call and the Colossus show, for tests and debugging.</summary>
        public Color CastColor => _castColor;

        /// <summary>How far the river has curled into the ring around the standing core: 0 while it flows behind it, 1 once it is a ring.</summary>
        public float StandingShare => _standingShare;

        /// <summary>True while a song holds the slot (SongSlot as an int) as of the last <see cref="SetSlot"/>, for tests and debugging.</summary>
        public bool IsSlotLive(int slot) => _slotVectors[SlotHeaderIndex(slot)].x >= 0f;

        /// <summary>The colour last given to <see cref="SetSlot"/> for the slot (SongSlot as an int), for tests and debugging.</summary>
        public Color SlotColor(int slot)
        {
            SlotHeaderIndex(slot);
            return _slotColors[slot];
        }

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
            TimeSlots();
            TimeGust();

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

            var bounds = new Bounds(_corePosition, Vector3.one * (_config.BoundsMeters * 2f));
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
        /// Supplies the swarm's settings and the core's trail, and starts the count at SwarmConfig.StartingWasps.
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
            ClearSongs();
            _config = config;

            if (isActiveAndEnabled)
            {
                CreateBuffers();
            }
        }

        /// <summary>How many wasps are alive. Wasps above it die where they are, and dead ones below it come back at a hive.</summary>
        public void SetActiveCount(int count) => _activeCount = Mathf.Max(0, count);

        /// <summary>
        /// Changes the whole-swarm form: River, HiveCall, Colossus or Collapsed, the form every wasp outside a live song
        /// slot flies. The change sweeps through the swarm as a wave. The slot songs come in through
        /// <see cref="SetSlot"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The form is a slot song's form, such as Dome or Sting.</exception>
        public void SetForm(SwarmForm form)
        {
            if (!IsWholeSwarmForm(form))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(form), form, "Only River, HiveCall, Colossus and Collapsed are whole-swarm forms. Send slot songs through SetSlot.");
            }

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
        /// The colour the whole-swarm forms Hive Call and Colossus show until the next call: the colour of the category of
        /// the song that started the form, from SwarmConfig.ColorOf. The River and the collapsed swarm always show
        /// SwarmConfig.RiverColor. Each wasp takes the new colour as the form's wave reaches it, so the colour flows through
        /// the swarm with the motion. A slot's squad shows the colour given to <see cref="SetSlot"/> instead.
        /// </summary>
        public void SetCastColor(Color color) => _castColor = color;

        /// <summary>
        /// What one song slot holds this frame. Call it every frame for each <see cref="SongSlot"/>, after the rules have
        /// ticked. While <c>state.IsActive</c>, the wasps whose squad value lies in [Low, High) fly the song
        /// <c>state.Form</c>, using ParamA and ParamB as its numbers, and glow in <paramref name="color"/> (the song's
        /// category colour, from SwarmConfig.ColorOf); every other wasp flies the whole-swarm form. Seconds and
        /// ReleasedSeconds are read against this view's clock in this frame's LateUpdate, so the song's beats land on the
        /// frames the rules say. ParamB.w is left to this view, which sends the release there.
        /// </summary>
        /// <param name="slot">The slot: (int)SongSlot.Post, Guard or Strike.</param>
        /// <param name="state">The slot as the rules see it.</param>
        /// <param name="color">The colour the slot's squad glows in.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The slot is not a SongSlot, or an active state's squad range is not 0 &lt;= Low &lt;= High &lt;= 1.
        /// </exception>
        public void SetSlot(int slot, in SlotState state, Color color)
        {
            int header = SlotHeaderIndex(slot);
            if (!state.IsActive)
            {
                _slotVectors[header] = new Vector4(EmptySlotForm, 0f, 0f, 0f);
                _isSlotFresh[slot] = false;
                return;
            }

            if (state.Low < 0f || state.High > 1f || state.Low > state.High)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(state), $"Slot {slot}'s squad range [{state.Low}, {state.High}) must lie within [0, 1].");
            }

            _slotVectors[header] = new Vector4((int)state.Form, 0f, state.Low, state.High);
            _slotVectors[header + ParamAOffset] = state.ParamA;
            _slotVectors[header + ParamBOffset] = state.ParamB;
            _slotSeconds[slot] = state.Seconds;
            _slotReleasedSeconds[slot] = state.ReleasedSeconds;
            _slotColors[slot] = color;
            _isSlotFresh[slot] = true;

            // Against this view's clock as it stands now; TimeSlots sets them again once LateUpdate has stepped it.
            WriteSlotTimes(slot);
        }

        /// <summary>
        /// The Gust: a pulse that runs through every wasp within <paramref name="radius"/> metres of
        /// <paramref name="center"/> (on the ground), whatever it is doing. Call it every frame with the seconds since the
        /// last Gust, or -1 when there is none or it is over; a pulse already under way then plays out its last moments.
        /// </summary>
        public void SetGust(Vector3 center, float radius, float seconds)
        {
            if (seconds < 0f)
            {
                return;
            }

            _gustCenter = center;
            _gustRadius = Mathf.Max(0f, radius);
            _gustSeconds = seconds;
            _isGustFresh = true;
        }

        /// <summary>Where the core is and the way it faces. Only the facing on the ground is used.</summary>
        public void SetCore(Vector3 position, Vector3 forward)
        {
            _corePosition = position;
            forward.y = 0f;
            if (forward.sqrMagnitude > MinimumSquaredLength)
            {
                _coreForward = forward.normalized;
            }
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
                    Squad = (float)((SquadStart + (index * SquadStep)) % 1.0),
                };
            }

            return layout;
        }

        private static float NextFloat(System.Random random) => (float)random.NextDouble();

        // Where the slot's header sits in _slotVectors. Fails loudly on a slot that is not a SongSlot.
        private static int SlotHeaderIndex(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, $"A song slot is 0 to {SlotCount - 1} (SongSlot).");
            }

            return slot * VectorsPerSlot;
        }

        private static bool IsWholeSwarmForm(SwarmForm form) =>
            form == SwarmForm.River || form == SwarmForm.HiveCall || form == SwarmForm.Colossus || form == SwarmForm.Collapsed;

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
            ClearSongs();
        }

        // Empties every slot and forgets the Gust, as at the start of a run.
        private void ClearSongs()
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                int header = slot * VectorsPerSlot;
                _slotVectors[header] = new Vector4(EmptySlotForm, 0f, 0f, 0f);
                _slotVectors[header + ParamAOffset] = Vector4.zero;
                _slotVectors[header + ParamBOffset] = new Vector4(0f, 0f, 0f, NotReleased);
                _slotColors[slot] = Color.clear;
                _isSlotFresh[slot] = false;
            }

            _gustRadius = 0f;
            _gustStartTime = 0f;
            _isGustFresh = false;
        }

        // Turns the rules' clocks from this frame's SetSlot calls into this view's times, now that _simTime holds this
        // frame. A slot not fed this frame keeps its times, so its song plays on with this view's clock.
        private void TimeSlots()
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (_isSlotFresh[slot])
                {
                    WriteSlotTimes(slot);
                    _isSlotFresh[slot] = false;
                }
            }
        }

        // The slot's cast time into its header's y and its release into ParamB's w, both in _simTime.
        private void WriteSlotTimes(int slot)
        {
            int header = slot * VectorsPerSlot;
            float released = _slotReleasedSeconds[slot];
            _slotVectors[header].y = _simTime - _slotSeconds[slot];
            _slotVectors[header + ParamBOffset].w = released < 0f ? NotReleased : Mathf.Max(_simTime - released, 0f);
        }

        // The same for the Gust. A Gust that is over keeps its start, so the shader's pulse finishes on its own.
        private void TimeGust()
        {
            if (_isGustFresh)
            {
                _gustStartTime = _simTime - _gustSeconds;
                _isGustFresh = false;
            }
        }

        // Spec 9.3: while the core stands, the river curls into a torus around it. The PathTrail never shrinks while
        // the core stands (it only stores points as it moves), so its length cannot tell standing from walking. This
        // measures standing directly instead: the core stands while it stays within StandStillMeters (on the ground)
        // of where it stopped, so settling never counts as a step. After StandDelaySeconds the river eases into the
        // torus over StandBlendSeconds, and eases back as soon as it moves.
        private void TrackStanding(float deltaTime)
        {
            float stillMeters = _config.StandStillMeters;
            Vector3 drift = _corePosition - _standingAnchor;
            drift.y = 0f;
            if (drift.sqrMagnitude > stillMeters * stillMeters)
            {
                _standingAnchor = _corePosition;
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
            compute.SetVector(CorePositionId, _corePosition);
            compute.SetVector(CoreForwardId, _coreForward);
            compute.SetFloat(StandingShareId, _standingShare);
            compute.SetVector(HiveAId, _firstHive);
            compute.SetVector(HiveBId, _secondHive);
            compute.SetVector(LastHitId, _lastHit);
            compute.SetVectorArray(SlotsId, _slotVectors);
            compute.SetVector(PulseId, new Vector4(_gustCenter.x, _gustCenter.z, _gustStartTime, _gustRadius));
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
            compute.SetFloat(CoreRingShareId, _config.CoreRingShareFor(_activeCount));
            compute.SetFloat(CoreRingRadiusId, _config.CoreRingRadius);
            compute.SetFloat(CoreRingGlowId, _config.CoreRingGlow);
            compute.SetFloat(CoreClearRadiusId, _config.CoreClearRadius);
            compute.SetFloat(CoreClearSpeedId, _config.CoreClearSpeed);
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

            for (int slot = 0; slot < SlotCount; slot++)
            {
                _slotColorVectors[slot] = ShaderColor(_slotColors[slot], isLinear);
            }

            compute.SetVectorArray(SlotColorsId, _slotColorVectors);
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
            public float Squad;
        }
    }
}
