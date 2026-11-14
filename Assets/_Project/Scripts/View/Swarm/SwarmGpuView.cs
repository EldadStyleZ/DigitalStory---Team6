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
    /// whole-swarm form and its colour, the three song slots, the Sting's targets, the Gust, the level's size scale, a
    /// few positions, and the moments a hit freezes the swarm or a cast flashes a squad.
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

        // The compute shader reads these as "no song in the slot" and "the song has not been released" (or, for a sting's
        // target, "it still lives").
        private const float EmptySlotForm = -1f;
        private const float NotReleased = -1f;

        // The most stings one Sting cast sends. Matches STING_MOST in WaspSwarm.compute. A target whose w is above
        // LivingLeast lives.
        private const int MostStings = 10;
        private const float LivingLeast = 0.5f;

        // LateUpdates a cast flash lasts: one bright frame or two, then the squad cools back to its song's heat.
        private const int FlashFrames = 2;

        // The giant's two hands (DigitalStory.Logic.ColossusHand), three float4s each. Match FIST_COUNT and FIST_VECTORS in
        // WaspSwarm.compute. NoFist is "no fist out" in a launch's w and "not dropped" in the drop.
        private const int FistCount = 2;
        private const int VectorsPerFist = 3;
        private const int FistHitOffset = 1;
        private const int FistGrowOffset = 2;
        private const float NoFist = -1f;

        // The giant's thrown wasps (songs-v2 W): a ring of the latest throws, four float4s each, and each bone's limb bits,
        // four bones to a float4. Match THROW_COUNT, THROW_VECTORS and BONE_LIMB_VECTORS in WaspSwarm.compute.
        private const int ThrowCount = 4;
        private const int VectorsPerThrow = 4;
        private const int ThrowAimOffset = 1;
        private const int ThrowBandOffset = 2;
        private const int ThrowNumbersOffset = 3;
        private const int BonesPerVector = 4;
        private const float NoThrow = -1f;

        // Each throw's band of wasps starts this share of the seeds on from the last, so quick blows throw fresh wasps.
        private const float GoldenShare = 0.618034f;

        // The figure-eight round the standing core turns this fast about it, so it never looks frozen. Once the core walks
        // on, it swings back across the walk at up to EightAlignSpeed, so the river forming behind the core unwinds from
        // its sides instead of through the core.
        private const float EightPrecessionSpeed = 0.25f;
        private const float EightAlignSpeed = 3f;
        private const float HalfTurn = Mathf.PI;
        private const float QuarterTurn = Mathf.PI * 0.5f;
        private const float MinimumTrailMeters = 0.0001f;

        // The core's past that the free swarm reads with its delays (plan section Q): 128 samples at 60 a second, a little
        // over two seconds, more than SwarmConfig.FollowDelaySeconds may ask for. HistorySamples matches HISTORY_SIZE in
        // WaspSwarm.compute.
        private const int HistorySamples = 128;
        private const float HistoryStepSeconds = 1f / 60f;

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
        private static readonly int SlotFlashId = Shader.PropertyToID("_SlotFlash");
        private static readonly int StingTargetsId = Shader.PropertyToID("_StingTargets");
        private static readonly int StingTargetCountId = Shader.PropertyToID("_StingTargetCount");
        private static readonly int FistsId = Shader.PropertyToID("_Fists");
        private static readonly int FistBonesId = Shader.PropertyToID("_FistBones");
        private static readonly int ThrowsId = Shader.PropertyToID("_Throws");
        private static readonly int BoneLimbsId = Shader.PropertyToID("_BoneLimbs");
        private static readonly int ThrowColorId = Shader.PropertyToID("_ThrowColor");
        private static readonly int ThrowRedSecondsId = Shader.PropertyToID("_ThrowRedSeconds");
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
        private static readonly int EightShareId = Shader.PropertyToID("_EightShare");
        private static readonly int EightRadiusId = Shader.PropertyToID("_EightRadius");
        private static readonly int EightHeightId = Shader.PropertyToID("_EightHeight");
        private static readonly int EightTiltId = Shader.PropertyToID("_EightTilt");
        private static readonly int EightThicknessId = Shader.PropertyToID("_EightThickness");
        private static readonly int EightLapSecondsId = Shader.PropertyToID("_EightLapSeconds");
        private static readonly int EightYawId = Shader.PropertyToID("_EightYaw");
        private static readonly int EightSecondsId = Shader.PropertyToID("_EightSeconds");
        private static readonly int HistoryId = Shader.PropertyToID("_History");
        private static readonly int HistoryStepId = Shader.PropertyToID("_HistoryStep");
        private static readonly int TrailLeadId = Shader.PropertyToID("_TrailLead");
        private static readonly int RiverReachId = Shader.PropertyToID("_RiverReach");
        private static readonly int EarlierFormId = Shader.PropertyToID("_EarlierForm");
        private static readonly int FollowDelayId = Shader.PropertyToID("_FollowDelay");
        private static readonly int FollowDelayPowerId = Shader.PropertyToID("_FollowDelayPower");
        private static readonly int WaveJitterId = Shader.PropertyToID("_WaveJitter");
        private static readonly int TailWidthId = Shader.PropertyToID("_TailWidth");
        private static readonly int FormBlendSecondsId = Shader.PropertyToID("_FormBlendSeconds");
        private static readonly int SpringStepId = Shader.PropertyToID("_SpringStep");
        private static readonly int DyingSecondsId = Shader.PropertyToID("_DyingSeconds");
        private static readonly int WaspLengthId = Shader.PropertyToID("_WaspLength");
        private static readonly int BuzzAmplitudeId = Shader.PropertyToID("_BuzzAmplitude");
        private static readonly int BuzzFrequencyId = Shader.PropertyToID("_BuzzFrequency");
        private static readonly int GlowSizeId = Shader.PropertyToID("_GlowSize");
        private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        private static readonly int ObstaclesId = Shader.PropertyToID("_Obstacles");
        private static readonly int ObstacleCountId = Shader.PropertyToID("_ObstacleCount");
        private static readonly int ObstacleMarginId = Shader.PropertyToID("_ObstacleMargin");
        private static readonly int ObstacleLeadId = Shader.PropertyToID("_ObstacleLead");
        private static readonly int ObstacleCloseId = Shader.PropertyToID("_ObstacleClose");
        private static readonly int ObstacleStrengthId = Shader.PropertyToID("_ObstacleStrength");
        private static readonly int ObstacleSpeedUpId = Shader.PropertyToID("_ObstacleSpeedUp");

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

        // The form before _previousForm. A quick second change (Hive Call ending as the stores run dry) finds wasps the
        // first change's wave has not yet reached; they ease out of this one instead of jumping.
        private SwarmForm _earlierForm = SwarmForm.River;
        private Vector3 _corePosition;
        private Vector3 _coreForward = Vector3.forward;
        private Vector3 _standingAnchor;
        private float _standingSeconds;
        private float _standingShare;
        private float _sizeScale = 1f;
        private float _freezeSeconds;

        // The figure-eight: how far the swarm is wound into it, the way its first lobe points on the ground (radians, from
        // +x toward +z), and the seconds it has shown since the swarm was last all river.
        private float _eightShare = 1f;
        private float _eightYaw;
        private float _eightSeconds;

        // The core's past, sent each frame as _History. The swing is every turn of the eight but its slow precession, added
        // up, so a wasp that is still wound into the eight its delay ago turns as the eight turned then. The eight's clock
        // starts over only once even the most delayed wasp has unwound.
        private readonly CoreHistory _history = new CoreHistory(HistorySamples, HistoryStepSeconds);
        private readonly Vector4[] _historySamples = new Vector4[HistorySamples];
        private float _swing;
        private float _unwoundSeconds;
        private double _lastTravelled;

        // Metres the core has walked since the swarm was last wound into the eight, which the river is drawn out of. It
        // starts over only once even the most delayed wasp has wound in.
        private float _riverReach;
        private float _woundSeconds;
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
        private readonly int[] _slotFlashFrames = new int[SlotCount];

        // The running Sting's targets as WaspSwarm.compute reads them (_StingTargets): w is NotReleased while the target
        // lives, or the moment it fell on this view's clock.
        private readonly Vector4[] _stingTargetVectors = new Vector4[MostStings];
        private int _stingTargetCount;

        // The giant's fists as WaspSwarm.compute reads them (_Fists and _FistBones). SetFist keeps the rules' clocks until
        // LateUpdate has stepped this view's clock, as SetSlot does, and only then turns them into its times.
        private readonly Vector4[] _fistVectors = new Vector4[FistCount * VectorsPerFist];
        private readonly float[] _fistSinceLaunch = new float[FistCount];
        private readonly float[] _fistFlight = new float[FistCount];
        private readonly float[] _fistRegrow = new float[FistCount];
        private readonly bool[] _isFistFresh = new bool[FistCount];
        private Vector4 _fistBones = new Vector4(NoFist, NoFist, NoFist, NoFist);

        // The giant's thrown wasps as WaspSwarm.compute reads them (_Throws, _BoneLimbs, _ThrowColor, _ThrowRedSeconds).
        private readonly Vector4[] _throwVectors = new Vector4[ThrowCount * VectorsPerThrow];
        private readonly Vector4[] _boneLimbVectors = new Vector4[MostBones / BonesPerVector];
        private int _nextThrow;
        private int _throwsStarted;
        private Color _throwColor = Color.red;
        private float _throwRedSeconds;

        // The Gust pulse, kept the same way: the rules' seconds since the Gust until LateUpdate turns them into a start.
        private Vector3 _gustCenter;
        private float _gustRadius;
        private float _gustSeconds;
        private float _gustStartTime;
        private bool _isGustFresh;

        // Plan section X: the tall props, nest trees and hives the free swarm flows round, as SwarmObstacles makes them. Sent
        // to the compute shader when they change, once a match, never per frame.
        private readonly Vector4[] _obstacleVectors = new Vector4[SwarmObstacles.MostObstacles];
        private int _obstacleCount;

        /// <summary>True while the buffers exist and the swarm is moved and drawn each frame, for tests and debugging.</summary>
        public bool IsRunning => _staticBuffer != null;

        /// <summary>The colour last given to <see cref="SetCastColor"/>, which Hive Call and the Colossus show, for tests and debugging.</summary>
        public Color CastColor => _castColor;

        /// <summary>How far the core counts as standing: 0 while it walks, easing to 1 once it has stood still a moment.</summary>
        public float StandingShare => _standingShare;

        /// <summary>
        /// How far the free swarm is wound into the figure-eight round the core: 0 while it flows behind the walking core
        /// as a river, 1 once it is all eight. Also 1 at the start of a run, before the core has walked far enough to
        /// trail a river. For tests and debugging.
        /// </summary>
        public float EightShare => _eightShare;

        /// <summary>
        /// The wasps' state on the GPU (WaspDynamic in WaspSwarm.compute), or null while the view is not running, for tests
        /// that read the swarm back. The game never reads it on the CPU: a read stalls the GPU.
        /// </summary>
        public GraphicsBuffer WaspStates => _dynamicBuffer;

        /// <summary>How many obstacles the free swarm flows round (<see cref="SetObstacles"/>), for tests and debugging.</summary>
        public int ObstacleCount => _obstacleCount;

        /// <summary>
        /// One obstacle the free swarm flows round: x and z of its middle on the ground, its radius and the height of its top,
        /// for tests and debugging.
        /// </summary>
        public Vector4 Obstacle(int index)
        {
            if (index < 0 || index >= _obstacleCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"There are {_obstacleCount} obstacles.");
            }

            return _obstacleVectors[index];
        }

        /// <summary>True while <see cref="Freeze"/> holds the swarm still, for tests and debugging.</summary>
        public bool IsFrozen => _freezeSeconds > 0f;

        /// <summary>True while the hand's fist is out, from its launch until its wasps are all home, for tests and debugging.</summary>
        /// <summary>How many throws (songs-v2 W) have started since the swarm last started over. For tests.</summary>
        public int ThrowsStarted => _throwsStarted;

        public bool IsFistOut(int hand) => _fistVectors[FistIndex(hand) * VectorsPerFist].w >= 0f && _simTime < _fistVectors[(hand * VectorsPerFist) + FistGrowOffset].x;

        /// <summary>How many sting targets the last <see cref="SetStingTargets"/> gave, for tests and debugging.</summary>
        public int StingTargetCount => _stingTargetCount;

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

            // While a freeze holds the swarm, its clock and its step stand still; the core it follows does not.
            float deltaTime = Mathf.Min(Time.deltaTime, _config.MaxDeltaTime);
            float stepSeconds = StepSeconds(deltaTime);
            _simTime += stepSeconds;
            TrackStanding(deltaTime);
            TimeSlots();
            TimeFists();
            TimeGust();

            int trailCount = _trail.CopyTo(_trailPoints);
            if (trailCount > 0)
            {
                _trailBuffer.SetData(_trailPoints, 0, 0, trailCount);
            }

            TrackEight(stepSeconds, trailCount);
            RecordThePast(stepSeconds);
            LatchFormColors();
            BindComputeBuffers();
            PushFrame(stepSeconds, trailCount);
            PushConfig();
            PushColors();
            CountDownFlashes();

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
            ForgetThePast();
            ClearSongs();
            _config = config;

            // The shader keeps its obstacles between scenes, so a match that sends none never flows round the last one's.
            _obstacleCount = 0;
            PushObstacles();

            if (isActiveAndEnabled)
            {
                CreateBuffers();
            }
        }

        /// <summary>
        /// The tall props, nest trees and hives the free swarm flows round (plan section X), each as x and z of its middle on
        /// the ground, its radius and the height of its top (DigitalStory.Logic.SwarmObstacles). Call once a match, after the
        /// map stands and after <see cref="Initialize"/>; the shader keeps them until the next call.
        /// </summary>
        /// <exception cref="ArgumentNullException">The obstacles are missing.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The count is below 0, above the most obstacles, or more than the array holds.</exception>
        public void SetObstacles(Vector4[] obstacles, int count)
        {
            if (obstacles == null)
            {
                throw new ArgumentNullException(nameof(obstacles));
            }

            if (count < 0 || count > SwarmObstacles.MostObstacles || count > obstacles.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, $"The swarm flows round 0 to {SwarmObstacles.MostObstacles} obstacles, and the array must hold that many.");
            }

            Array.Clear(_obstacleVectors, 0, _obstacleVectors.Length);
            Array.Copy(obstacles, _obstacleVectors, count);
            _obstacleCount = count;
            PushObstacles();
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

            _earlierForm = _previousForm;
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

        /// <summary>
        /// The running Sting's targets, one per sting the cast sent (SwarmFormMachine.StingCount and StingTarget). Call it
        /// every frame, with a count of 0 while no Sting runs. The Strike squad is shared evenly between the stings, and
        /// each flies to, wraps and follows its own target. xyz is the target's feet; w is 1 while it lives and 0 once it
        /// has fallen, and a sting whose target falls pops and flies home on its own. With a count of 0 every sting flies
        /// to the slot's ParamA.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="targets"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is below 0, above 10, or more than <paramref name="targets"/> holds.
        /// </exception>
        public void SetStingTargets(Vector4[] targets, int count)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            if (count < 0 || count > MostStings || count > targets.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), count, $"A Sting sends 0 to {MostStings} stings, and the array must hold that many targets.");
            }

            for (int sting = 0; sting < MostStings; sting++)
            {
                Vector4 before = _stingTargetVectors[sting];
                bool hadFallen = sting < _stingTargetCount && before.w >= 0f;
                if (sting >= count)
                {
                    _stingTargetVectors[sting] = new Vector4(0f, 0f, 0f, NotReleased);
                    continue;
                }

                // A target that has just fallen keeps the moment it fell, on this view's clock, so its sting's pop plays
                // out from there.
                Vector4 target = targets[sting];
                float fell = NotReleased;
                if (target.w <= LivingLeast)
                {
                    fell = hadFallen ? before.w : _simTime;
                }

                _stingTargetVectors[sting] = new Vector4(target.x, target.y, target.z, fell);
            }

            _stingTargetCount = count;
        }

        /// <summary>
        /// What the swarm's sizes are multiplied by at its level (HiveDefenseSession.SizeScale): the figure-eight round the
        /// standing core grows with it, as the songs do. Call it every frame or whenever the level changes; it starts at 1.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The scale is not above 0, or is infinite.</exception>
        public void SetSizeScale(float sizeScale)
        {
            if (!(sizeScale > 0f) || float.IsInfinity(sizeScale))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeScale), sizeScale, "The swarm's size scale must be above 0.");
            }

            _sizeScale = sizeScale;
        }

        /// <summary>
        /// Holds the swarm's motion still for <paramref name="seconds"/> of game time, from this frame: the hit-stop of a
        /// big hit. The wasps are still drawn, and a song's flash still shows. A shorter freeze never cuts a longer one.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The seconds are below 0, not a number, or infinite.</exception>
        public void Freeze(float seconds)
        {
            if (!(seconds >= 0f) || float.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A freeze lasts 0 seconds or more.");
            }

            _freezeSeconds = Mathf.Max(_freezeSeconds, seconds);
        }

        /// <summary>
        /// The cast flash: the slot's squad burns white in its colour for a frame or two from this frame, before any of
        /// its wasps has moved. Call it on the frame the song is cast, with its slot fed to <see cref="SetSlot"/> that frame.
        /// </summary>
        /// <param name="slot">The slot: (int)SongSlot.Post, Guard or Strike.</param>
        /// <exception cref="ArgumentOutOfRangeException">The slot is not a SongSlot.</exception>
        public void FlashSlot(int slot)
        {
            SlotHeaderIndex(slot);
            _slotFlashFrames[slot] = FlashFrames;
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

        /// <summary>
        /// Which Colossus bones each hand's fist is made of, its forearm, hand and fingers (ColossusArmBones), as indices into the
        /// bones of <see cref="SetColossusBones"/>; -1 first and last for a hand that has none. Call once with the samples.
        /// </summary>
        public void SetFistBones(int rightFirst, int rightLast, int leftFirst, int leftLast)
        {
            _fistBones = new Vector4(rightFirst, rightLast, leftFirst, leftLast);
        }

        /// <summary>
        /// The hand's fist this frame (songs-v2 R), in the rules' clock: call it every frame the giant stands. While the fist is
        /// not out (<paramref name="secondsSinceLaunch"/> below 0) its wasps keep their places on the arm. Once it is, they leave
        /// the arm, fly from <paramref name="origin"/> to <paramref name="target"/> to hit after <paramref name="flightSeconds"/>,
        /// burst to <paramref name="hitRadius"/> and stream home over <paramref name="regrowSeconds"/>.
        /// </summary>
        /// <param name="hand">The hand: (int)ColossusHand.Right or Left.</param>
        /// <param name="origin">Where the fist left the arm: the hand as it launched.</param>
        /// <param name="target">Where it flies, on the ground; the shader aims at the chest above it.</param>
        /// <param name="secondsSinceLaunch">Seconds since the fist left, or below 0 while it is not out.</param>
        /// <param name="flightSeconds">Seconds from the launch to the hit.</param>
        /// <param name="regrowSeconds">Seconds from the hit until the arm is whole again.</param>
        /// <param name="hitRadius">The hit's radius in metres, which the fist bursts to.</param>
        /// <exception cref="ArgumentOutOfRangeException">The hand is not 0 or 1.</exception>
        public void SetFist(int hand, Vector3 origin, Vector3 target, float secondsSinceLaunch, float flightSeconds, float regrowSeconds, float hitRadius)
        {
            int at = FistIndex(hand) * VectorsPerFist;
            if (!(secondsSinceLaunch >= 0f))
            {
                _fistVectors[at].w = NoFist;
                _isFistFresh[hand] = false;
                return;
            }

            _fistVectors[at] = new Vector4(origin.x, origin.y, origin.z, _fistVectors[at].w);
            _fistVectors[at + FistHitOffset] = new Vector4(target.x, target.y, target.z, _fistVectors[at + FistHitOffset].w);
            _fistVectors[at + FistGrowOffset] = new Vector4(_fistVectors[at + FistGrowOffset].x, NoFist, Mathf.Max(hitRadius, 0f), 0f);
            _fistSinceLaunch[hand] = secondsSinceLaunch;
            _fistFlight[hand] = Mathf.Max(flightSeconds, 0f);
            _fistRegrow[hand] = Mathf.Max(regrowSeconds, 0f);
            _isFistFresh[hand] = true;
        }

        /// <summary>
        /// The giant has ended: a fist still flying never hits, and its wasps ease back into the swarm from where it was. A fist
        /// that has hit streams home as it would have. Nothing more need be fed for either.
        /// </summary>
        public void DropFists()
        {
            for (int hand = 0; hand < FistCount; hand++)
            {
                int at = hand * VectorsPerFist;
                _isFistFresh[hand] = false;
                if (_fistVectors[at].w >= 0f && _simTime < _fistVectors[at + FistHitOffset].w)
                {
                    _fistVectors[at + FistGrowOffset].y = _simTime;
                }
            }
        }

        /// <summary>
        /// Each Colossus bone's limb (songs-v2 W, DigitalStory.Logic.ColossusThrows.FindLimbs), in the order of
        /// <see cref="SetColossusBones"/>, so a throw knows which wasps stand on the limbs it throws. Call once with the samples.
        /// </summary>
        /// <param name="limbs">Each bone's limb.</param>
        /// <param name="count">How many of them to use; bones past it, and past the most bones, throw nothing.</param>
        /// <exception cref="ArgumentNullException">The limbs are missing.</exception>
        public void SetBoneLimbs(ColossusLimb[] limbs, int count)
        {
            if (limbs == null)
            {
                throw new ArgumentNullException(nameof(limbs));
            }

            Array.Clear(_boneLimbVectors, 0, _boneLimbVectors.Length);
            int used = Mathf.Clamp(count, 0, Mathf.Min(limbs.Length, MostBones));
            for (int bone = 0; bone < used; bone++)
            {
                Vector4 four = _boneLimbVectors[bone / BonesPerVector];
                four[bone % BonesPerVector] = (int)limbs[bone];
                _boneLimbVectors[bone / BonesPerVector] = four;
            }
        }

        /// <summary>The colour thrown wasps and flying fists burn, and the seconds they take to turn to it as they leave.</summary>
        public void SetThrowLook(Color color, float redSeconds)
        {
            _throwColor = color;
            _throwRedSeconds = Mathf.Max(redSeconds, 0f);
        }

        /// <summary>
        /// Throws a share of the wasps on <paramref name="limbs"/> off the giant (songs-v2 W), from this frame: they fly out in
        /// <paramref name="shape"/>, hold a moment and stream home, all back after <paramref name="backSeconds"/>. A look only.
        /// The latest four throws fly at once; a wasp two of them claim flies the later one, and a flying fist keeps its own.
        /// </summary>
        /// <param name="shape">How they fly.</param>
        /// <param name="limbs">The limbs they leave from.</param>
        /// <param name="center">The move's centre on the ground: the giant's feet, or where the leap landed.</param>
        /// <param name="facing">The giant's facing; flattened here.</param>
        /// <param name="share">The share of the limbs' wasps that leave, 0 to 1.</param>
        /// <param name="spreadDegrees">Degrees either side of the facing the burst spreads.</param>
        /// <param name="reachMetres">Metres the wasps fly out.</param>
        /// <param name="outSeconds">Seconds they fly out.</param>
        /// <param name="backSeconds">Seconds until every one is back.</param>
        /// <param name="shapeNumber">The spiral's spin in radians a second, or the crater's second ring share; 0 otherwise.</param>
        public void Throw(
            ColossusThrowShape shape, ColossusLimb limbs, Vector3 center, Vector3 facing, float share, float spreadDegrees,
            float reachMetres, float outSeconds, float backSeconds, float shapeNumber)
        {
            if (shape == ColossusThrowShape.None || limbs == ColossusLimb.None || !(share > 0f) || !(backSeconds > 0f))
            {
                return;
            }

            facing.y = 0f;
            facing = facing.sqrMagnitude > 0f ? facing.normalized : Vector3.forward;
            float bandStart = Mathf.Repeat(_throwsStarted * GoldenShare, 1f);
            int at = _nextThrow * VectorsPerThrow;
            _throwVectors[at] = new Vector4(center.x, center.y, center.z, _simTime);
            _throwVectors[at + ThrowAimOffset] = new Vector4(facing.x, facing.y, facing.z, (int)shape);
            _throwVectors[at + ThrowBandOffset] = new Vector4((int)limbs, Mathf.Clamp01(share), bandStart, backSeconds);
            _throwVectors[at + ThrowNumbersOffset] = new Vector4(
                spreadDegrees * Mathf.Deg2Rad, Mathf.Max(reachMetres, 0f), Mathf.Clamp(outSeconds, 0.01f, backSeconds), shapeNumber);
            _nextThrow = (_nextThrow + 1) % ThrowCount;
            _throwsStarted++;
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
            _earlierForm = SwarmForm.River;
            _standingSeconds = 0f;
            _standingShare = 0f;
            _sizeScale = 1f;
            _freezeSeconds = 0f;
            _eightShare = 1f;
            _eightYaw = 0f;
            _eightSeconds = 0f;
            _hasFormChanged = false;
            ForgetThePast();
            ClearSongs();
        }

        // A new run's swarm starts wound into the eight round a core that has always stood where it is.
        private void ForgetThePast()
        {
            _swing = 0f;
            _unwoundSeconds = 0f;
            _lastTravelled = 0.0;
            _riverReach = 0f;
            _woundSeconds = 0f;
            _history.Reset(_eightShare, _swing);
        }

        // Empties every slot and forgets the Sting's targets, the cast flashes and the Gust, as at the start of a run.
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
                _slotFlashFrames[slot] = 0;
            }

            for (int sting = 0; sting < MostStings; sting++)
            {
                _stingTargetVectors[sting] = new Vector4(0f, 0f, 0f, NotReleased);
            }

            _stingTargetCount = 0;
            for (int hand = 0; hand < FistCount; hand++)
            {
                int at = hand * VectorsPerFist;
                _fistVectors[at] = new Vector4(0f, 0f, 0f, NoFist);
                _fistVectors[at + FistHitOffset] = Vector4.zero;
                _fistVectors[at + FistGrowOffset] = new Vector4(0f, NoFist, 0f, 0f);
                _isFistFresh[hand] = false;
            }

            for (int index = 0; index < ThrowCount; index++)
            {
                int at = index * VectorsPerThrow;
                _throwVectors[at] = new Vector4(0f, 0f, 0f, NoThrow);
                _throwVectors[at + ThrowAimOffset] = Vector4.zero;
                _throwVectors[at + ThrowBandOffset] = Vector4.zero;
                _throwVectors[at + ThrowNumbersOffset] = Vector4.zero;
            }

            _nextThrow = 0;
            _throwsStarted = 0;
            _gustRadius = 0f;
            _gustStartTime = 0f;
            _isGustFresh = false;
        }

        // The seconds the swarm moves this frame: none while a freeze holds it. The freeze counts down in game time.
        private float StepSeconds(float deltaTime)
        {
            if (_freezeSeconds <= 0f)
            {
                return deltaTime;
            }

            _freezeSeconds = Mathf.Max(0f, _freezeSeconds - Time.deltaTime);
            return 0f;
        }

        // A flash lasts FlashFrames LateUpdates, this one included.
        private void CountDownFlashes()
        {
            for (int slot = 0; slot < SlotCount; slot++)
            {
                if (_slotFlashFrames[slot] > 0)
                {
                    _slotFlashFrames[slot]--;
                }
            }
        }

        private float FlashOf(int slot) => _slotFlashFrames[slot] > 0 ? 1f : 0f;

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

        // The fists' launches, hits and regrows into this view's times, as for the slots.
        private void TimeFists()
        {
            for (int hand = 0; hand < FistCount; hand++)
            {
                if (!_isFistFresh[hand])
                {
                    continue;
                }

                int at = hand * VectorsPerFist;
                float launch = _simTime - _fistSinceLaunch[hand];
                float hit = launch + _fistFlight[hand];
                _fistVectors[at].w = launch;
                _fistVectors[at + FistHitOffset].w = hit;
                _fistVectors[at + FistGrowOffset].x = hit + _fistRegrow[hand];
                _isFistFresh[hand] = false;
            }
        }

        private static int FistIndex(int hand)
        {
            if (hand < 0 || hand >= FistCount)
            {
                throw new ArgumentOutOfRangeException(nameof(hand), hand, "A hand is 0 (right) or 1 (left).");
            }

            return hand;
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

        // While the core stands, the river winds into the figure-eight around it (the owner's change to spec 9.3's torus).
        // The PathTrail never shrinks while the core stands (it only stores points as it moves), so its length cannot
        // tell standing from walking. This measures standing directly instead: the core stands while it stays within
        // StandStillMeters (on the ground) of where it stopped, so settling never counts as a step. After
        // StandDelaySeconds the river eases into the eight over StandBlendSeconds, and eases back as soon as it moves.
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

        // The eight shows while the core stands, and while the trail is still too short to hold a river (the start of a
        // run). It lies across the way the core last walked, so the river behind it winds out to the lobes at its sides
        // rather than across the core; it turns slowly while the core stands, and swings back across the walk the shorter
        // way once the core moves on. Its clock and its turn start over each time the swarm is all river again, where
        // neither shows. The trail's length is read as WaspSwarm.compute reads it, from the points just copied.
        private void TrackEight(float stepSeconds, int trailCount)
        {
            float trailLength = trailCount > 0 ? _trailPoints[trailCount - 1].w : 0f;
            float trailShare = Mathf.Clamp01(trailLength / Mathf.Max(_config.StandingTrailMeters, MinimumTrailMeters));
            _eightShare = 1f - (trailShare * (1f - _standingShare));
            float mostDelay = _config.FollowDelaySeconds + HistoryStepSeconds;
            _woundSeconds = _eightShare >= 1f ? _woundSeconds + stepSeconds : 0f;
            if (_woundSeconds > mostDelay)
            {
                _riverReach = 0f;
            }

            // Across the walk: the core's facing turned a quarter turn, cross(up, forward), as an angle on the ground.
            float acrossYaw = Mathf.Atan2(-_coreForward.x, _coreForward.z);
            if (_eightShare <= 0f)
            {
                // The river's tail may still be wound into the eight its delay ago, so the eight's clock runs on until even
                // it has unwound.
                _unwoundSeconds += stepSeconds;
                _eightSeconds = _unwoundSeconds > mostDelay ? 0f : _eightSeconds + stepSeconds;
                Swing(acrossYaw);
                return;
            }

            _unwoundSeconds = 0f;
            _eightSeconds += stepSeconds;
            if (_standingSeconds >= _config.StandDelaySeconds)
            {
                _eightYaw = Mathf.Repeat(_eightYaw + (EightPrecessionSpeed * stepSeconds), FullTurn);
                return;
            }

            // Either way across the walk will do, so it swings at most a quarter turn.
            float toAcross = Mathf.DeltaAngle(_eightYaw * Mathf.Rad2Deg, acrossYaw * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (toAcross > QuarterTurn)
            {
                toAcross -= HalfTurn;
            }
            else if (toAcross < -QuarterTurn)
            {
                toAcross += HalfTurn;
            }

            float most = EightAlignSpeed * stepSeconds;
            Swing(_eightYaw + Mathf.Clamp(toAcross, -most, most));
        }

        // Turns the eight to a new yaw and adds the turn to the swing the free swarm reads with its delays.
        private void Swing(float yaw)
        {
            _swing += Mathf.DeltaAngle(_eightYaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            _eightYaw = Mathf.Repeat(yaw, FullTurn);
        }

        // Adds this frame to the core's past: how far it went (the trail's odometer), how far the swarm is wound into the
        // eight and how far the eight has swung. On the swarm's own clock, so a freeze holds the past still too. A trail
        // that started over (a new run) starts the past over with it.
        private void RecordThePast(float stepSeconds)
        {
            double travelled = _trail.Travelled;
            if (travelled < _lastTravelled)
            {
                _history.Reset(_eightShare, _swing);
                _riverReach = 0f;
            }
            else
            {
                _riverReach += (float)(travelled - _lastTravelled);
            }

            _lastTravelled = travelled;
            _history.Record(stepSeconds, travelled, _eightShare, _swing);
            _history.CopyTo(_historySamples);
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

        // Once a match, not per frame: the compute shader keeps the values it was given.
        private void PushObstacles()
        {
            if (_swarmCompute == null)
            {
                return;
            }

            _swarmCompute.SetVectorArray(ObstaclesId, _obstacleVectors);
            _swarmCompute.SetInt(ObstacleCountId, _obstacleCount);
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
            compute.SetInt(EarlierFormId, (int)_earlierForm);
            compute.SetFloat(SimTimeId, _simTime);
            compute.SetFloat(DeltaTimeId, deltaTime);
            compute.SetFloat(FormStartTimeId, _formStartTime);
            compute.SetFloat(PreviousFormStartTimeId, _previousFormStartTime);
            compute.SetVector(CorePositionId, _corePosition);
            compute.SetVector(CoreForwardId, _coreForward);
            compute.SetFloat(EightShareId, _eightShare);
            compute.SetFloat(EightYawId, _eightYaw);
            compute.SetFloat(EightSecondsId, _eightSeconds);
            compute.SetVectorArray(HistoryId, _historySamples);
            compute.SetFloat(HistoryStepId, HistoryStepSeconds);
            compute.SetFloat(TrailLeadId, _trail.Lead);
            compute.SetFloat(RiverReachId, _riverReach);
            compute.SetVector(HiveAId, _firstHive);
            compute.SetVector(HiveBId, _secondHive);
            compute.SetVector(LastHitId, _lastHit);
            compute.SetVectorArray(SlotsId, _slotVectors);
            compute.SetVector(SlotFlashId, new Vector4(FlashOf((int)SongSlot.Post), FlashOf((int)SongSlot.Guard), FlashOf((int)SongSlot.Strike), 0f));
            compute.SetVectorArray(StingTargetsId, _stingTargetVectors);
            compute.SetInt(StingTargetCountId, _stingTargetCount);
            compute.SetVectorArray(FistsId, _fistVectors);
            compute.SetVector(FistBonesId, _fistBones);
            compute.SetVectorArray(ThrowsId, _throwVectors);
            compute.SetVectorArray(BoneLimbsId, _boneLimbVectors);
            compute.SetFloat(ThrowRedSecondsId, _throwRedSeconds);
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
            compute.SetFloat(EightRadiusId, _config.EightRadius * _sizeScale);
            compute.SetFloat(EightHeightId, _config.EightHeight * _sizeScale);
            compute.SetFloat(EightTiltId, _config.EightTilt);
            compute.SetFloat(EightThicknessId, _config.EightThickness * _sizeScale);
            compute.SetFloat(EightLapSecondsId, _config.EightLapSeconds);
            compute.SetFloat(FollowDelayId, _config.FollowDelaySeconds);
            compute.SetFloat(FollowDelayPowerId, _config.FollowDelayPower);
            compute.SetFloat(WaveJitterId, _config.WaveJitter);
            compute.SetFloat(TailWidthId, _config.TailWidthShare);
            compute.SetFloat(FormBlendSecondsId, _config.FormBlendSeconds);
            compute.SetFloat(SpringStepId, _config.SpringStepSeconds);
            compute.SetFloat(DyingSecondsId, _config.DyingSeconds);
            compute.SetFloat(ObstacleMarginId, _config.ObstacleMargin);
            compute.SetFloat(ObstacleLeadId, _config.ObstacleLeadMeters);
            compute.SetFloat(ObstacleCloseId, _config.ObstacleCloseMeters);
            compute.SetFloat(ObstacleStrengthId, _config.ObstacleStrength);
            compute.SetFloat(ObstacleSpeedUpId, _config.ObstacleSpeedUp);

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
            compute.SetVector(ThrowColorId, ShaderColor(_throwColor, isLinear));
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
