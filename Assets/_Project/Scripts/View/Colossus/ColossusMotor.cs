using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The Colossus's puppet: a rigged model whose renderer is off, because the wasps are its body. While the giant
    /// stands, it goes where the rules put it, walks and attacks, and hands its bones to the swarm every frame.
    /// The rules (position, facing, attacks and their timing) all live in HiveDefenseSession.
    /// </summary>
    /// <remarks>
    /// LateUpdate runs after the Animator has posed the bones and before SwarmGpuView (execution order 100), so the
    /// wasps follow this frame's pose.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Colossus Motor")]
    public sealed class ColossusMotor : MonoBehaviour
    {
        // Not game tuning: smooths the walk blend over the odd frame where the rules did not move the giant.
        private const float SpeedDampSeconds = 0.1f;

        // Punch, HeavyPunch and Kick: the Animator plays the attacks only when it has all of them.
        private const int AttackTriggerCount = 3;

        // The third punch of a chain swells more, so it reads as the heavy one.
        private const float HeavyPulseFactor = 1.5f;
        private const float MinimumPulseSeconds = 0.01f;
        private const float MinimumSquaredLength = 0.000001f;
        private const int GizmoSampleCount = 200;
        private const float GizmoRadius = 0.06f;

        private const int RingSegments = 48;
        private const float MinimumRingRadius = 0.1f;
        private const float RingHeight = 0.06f;
        private const float RingInnerRatio = 0.88f;
        private const string RingShaderName = "Sprites/Default";
        private const float FullTurnDegrees = 360f;
        private const float ArcHeightFactor = 4f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int PunchHash = Animator.StringToHash("Punch");
        private static readonly int HeavyPunchHash = Animator.StringToHash("HeavyPunch");
        private static readonly int KickHash = Animator.StringToHash("Kick");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int FistRightHash = Animator.StringToHash("FistRight");
        private static readonly int FistLeftHash = Animator.StringToHash("FistLeft");
        private static readonly int ColorHash = Shader.PropertyToID("_Color");
        private static readonly Color FeetGizmoColor = Color.cyan;
        private static readonly Color HeadGizmoColor = Color.magenta;

        [SerializeField, Tooltip("The puppet's SkinnedMeshRenderer. Only its bones are read, and it is switched off, because the wasps are the body.")]
        private SkinnedMeshRenderer _body;

        [SerializeField, Tooltip("The puppet's Animator. It needs a float named Speed (1 at the Colossus's full walking speed). When it also has triggers named Punch, HeavyPunch and Kick, the attacks play them.")]
        private Animator _animator;

        [SerializeField, Tooltip("What swells for a moment on each attack when the Animator has no attack triggers: an arm bone, or the body. Needed only then.")]
        private Transform _attackPivot;

        [SerializeField, Min(0f), Tooltip("How much the attack pivot grows at the moment an attack lands, as a share of its size. Used only without attack triggers.")]
        private float _pulseGrowth = 0.35f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the attack pivot takes to shrink back after an attack lands. Used only without attack triggers.")]
        private float _pulseRecoverSeconds = 0.2f;

        [Header("Moves")]
        [SerializeField, Min(0.05f), Tooltip("Seconds the slam's dip and rebound take.")]
        private float _slamSeconds = 0.5f;

        [SerializeField, Range(0f, 0.5f), Tooltip("How far the puppet's root squashes down at the bottom of the slam, as a share of its height.")]
        private float _slamDip = 0.15f;

        [SerializeField, Min(0.05f), Tooltip("Seconds the roar's rearing up takes.")]
        private float _roarSeconds = 0.8f;

        [SerializeField, Range(0f, 0.5f), Tooltip("How far the puppet rears up in the roar, as a share of its height.")]
        private float _roarRear = 0.08f;

        [SerializeField, Min(0.05f), Tooltip("Seconds the spin kick's one turn takes. A little longer than the rules' spin kick hit delay, so the turn is under way when it hits.")]
        private float _spinSeconds = 0.5f;

        [SerializeField, Min(0f), Tooltip("Metres the puppet rises at the top of the leap's arc. The leap's time is the rules' (ColossusConfig.LeapSeconds).")]
        private float _leapHeight = 3f;

        [SerializeField, Min(0.05f), Tooltip("Seconds a ring takes to expand and fade. The slam's, the roar's and the leap's rings reach as far as the rules' radii at the giant's level.")]
        private float _ringSeconds = 0.45f;

        [SerializeField, Tooltip("The slam's and the leap's ring colour. Its alpha is the brightest the ring gets.")]
        private Color _slamRingColor = new Color(1f, 0.6f, 0.15f, 0.85f);

        [SerializeField, Tooltip("The roar's ring colour. Softer than the slam's.")]
        private Color _roarRingColor = new Color(1f, 0.85f, 0.4f, 0.45f);

        [SerializeField, Tooltip("The colour of the marker where a leap (L L) will land, shown from the take-off until the landing. Its alpha is the brightest it gets.")]
        private Color _leapMarkerColor = new Color(1f, 0.45f, 0.1f, 0.9f);

        [SerializeField, Range(0f, 1f), Tooltip("How much wider than the landing's reach the leap marker starts; it closes to the reach as the giant comes down.")]
        private float _leapMarkerSpread = 0.25f;

        private HiveDefenseSession _session;
        private SwarmGpuView _swarm;
        private ColossusConfig _config;
        private Transform[] _bones;
        private Matrix4x4[] _boneMatrices;
        private int[] _sampleBones;
        private Vector3[] _sampleOffsets;
        private float[] _sampleHeights;
        private bool _hasAttackTriggers;
        private bool _hasHitTrigger;
        private Vector3 _restScale = Vector3.one;
        private ColossusAttack _move;
        private float _moveSeconds;
        private Material _ringMaterial;
        private Mesh _ringMesh;
        private MaterialPropertyBlock _ringBlock;
        private Transform _slamRing;
        private Renderer _slamRingRenderer;
        private Transform _roarRing;
        private Renderer _roarRingRenderer;
        private float _slamRingAge = -1f;
        private float _roarRingAge = -1f;
        private Vector3 _slamRingCenter;
        private Vector3 _roarRingCenter;
        private float _slamRingRadius = MinimumRingRadius;
        private float _roarRingRadius = MinimumRingRadius;
        private bool _isShowing;
        private Vector3 _lastPosition;
        private Vector3 _pivotRestScale = Vector3.one;
        private Transform _leapMarker;
        private Renderer _leapMarkerRenderer;
        private bool _hasFistTriggers;
        private readonly int[] _fistHandBones = { -1, -1 };
        private readonly bool[] _isFistOut = new bool[2];
        private readonly Vector3[] _fistOrigins = new Vector3[2];
        private bool _isPulsing;
        private float _pulseSeconds;
        private float _pulseRiseSeconds;
        private float _pulseStrength;

        /// <summary>True while the giant stands and its bones go to the swarm each frame, for tests and debugging.</summary>
        public bool IsShowing => _isShowing;

        /// <summary>True when the Animator plays the attacks; false when the fallback swell of the attack pivot does.</summary>
        public bool HasAttackTriggers => _hasAttackTriggers;

        /// <summary>True while the puppet plays a move of its own (slam, roar, spin kick or leap), for tests and debugging.</summary>
        public bool IsMoving => _move != ColossusAttack.None;

        /// <summary>True when the controller has a trigger named Hit, so the roar plays the hit-reaction clip.</summary>
        public bool HasHitTrigger => _hasHitTrigger;

        /// <summary>The move the puppet is playing on its own (slam, roar, spin kick or leap), or None, for tests and pictures.</summary>
        public ColossusAttack CurrentMove => _move;

        /// <summary>True when the controller has the FistRight and FistLeft triggers, so a hand launch plays its arm's thrust.</summary>
        public bool HasFistTriggers => _hasFistTriggers;

        /// <summary>True while the marker shows where the leap will land, from its take-off until it lands, for tests.</summary>
        public bool IsLeapMarkerShowing => _leapMarkerRenderer != null && _leapMarkerRenderer.enabled;

        /// <summary>Where the leap marker stands, for tests.</summary>
        public Vector3 LeapMarkerPosition => _leapMarker != null ? _leapMarker.position : Vector3.zero;

        /// <summary>Where the hand bone is now, the place a fist leaves from, for tests and pictures. Zero without the bone.</summary>
        public Vector3 HandPosition(ColossusHand hand)
        {
            int bone = _fistHandBones[(int)hand];
            return _bones != null && bone >= 0 && bone < _bones.Length ? _bones[bone].position : Vector3.zero;
        }

        private void LateUpdate()
        {
            if (_session == null)
            {
                return;
            }

            if (_session.IsColossusActive != _isShowing)
            {
                if (_isShowing)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }

            if (!_isShowing)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            FollowRules(deltaTime);
            AdvanceMove(deltaTime);
            AdvanceRings(deltaTime);
            ShowLeapMarker();
            AdvancePulse(deltaTime);
            SendBones();
            SendFists();
        }

        // Shows the samples in Play Mode, so a person can check they sit on the body (spec 9.5). Colour runs from the
        // feet (cyan) to the head (magenta), which is the order the Colossus builds in.
        private void OnDrawGizmosSelected()
        {
            if (_bones == null || _sampleBones == null)
            {
                return;
            }

            int stride = Mathf.Max(1, _sampleBones.Length / GizmoSampleCount);
            int drawn = 0;
            for (int sample = 0; sample < _sampleBones.Length && drawn < GizmoSampleCount; sample += stride)
            {
                Transform bone = _bones[_sampleBones[sample]];
                if (bone == null)
                {
                    continue;
                }

                Gizmos.color = Color.Lerp(FeetGizmoColor, HeadGizmoColor, _sampleHeights[sample]);
                Gizmos.DrawSphere(bone.localToWorldMatrix.MultiplyPoint3x4(_sampleOffsets[sample]), GizmoRadius);
                drawn++;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_ringMesh != null)
            {
                Destroy(_ringMesh);
            }

            if (_ringMaterial != null)
            {
                Destroy(_ringMaterial);
            }
        }

        /// <summary>
        /// Supplies the rules and the swarm, checks the puppet matches the baked samples, and gives the swarm its places
        /// on the body. Call after <see cref="SwarmGpuView.Initialize"/>. Calling again (a restart) starts over.
        /// Throws when a reference is missing or the puppet does not match the samples.
        /// </summary>
        public void Initialize(HiveDefenseSession session, SwarmGpuView swarm, ColossusSampleSet samples, ColossusConfig config)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (swarm == null)
            {
                throw new ArgumentNullException(nameof(swarm));
            }

            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            RequireSceneReferences();
            StartOver();
            ReadSamples(samples);
            ReadAnimator();
            PreparePuppet();
            BuildRings();

            _session = session;
            _swarm = swarm;
            _config = config;
            _session.ColossusAttackStarted += HandleColossusAttackStarted;
            _session.ColossusFistThrown += HandleColossusFistThrown;

            // A swarm view that switched itself off (no compute shaders) has already said so, and holds no wasps.
            if (swarm.enabled)
            {
                swarm.SetColossusSamples(_sampleBones, _sampleOffsets, _sampleHeights);
                FindFistBones(samples);
            }
        }

        // Songs-v2 R: the wasps on each forearm, hand and fingers are that hand's fist. A rig without them launches nothing
        // visible, but the rules still fly and hit.
        private void FindFistBones(ColossusSampleSet samples)
        {
            bool hasRight = ColossusArmBones.Find(samples.BoneNames, ColossusHand.Right, out int rightFirst, out int rightLast, out int rightHand);
            bool hasLeft = ColossusArmBones.Find(samples.BoneNames, ColossusHand.Left, out int leftFirst, out int leftLast, out int leftHand);
            if (!hasRight || !hasLeft)
            {
                Debug.LogWarning($"{name}: {samples.name} has no forearm and hand bones on both sides, so a hand launch shows no fist.", this);
            }

            _fistHandBones[(int)ColossusHand.Right] = rightHand;
            _fistHandBones[(int)ColossusHand.Left] = leftHand;
            _swarm.SetFistBones(rightFirst, rightLast, leftFirst, leftLast);
        }

        private void RequireSceneReferences()
        {
            if (_body == null)
            {
                throw new InvalidOperationException($"{name}: ColossusMotor has no body SkinnedMeshRenderer. Rebuild the scene or fill it in.");
            }

            if (_animator == null)
            {
                throw new InvalidOperationException($"{name}: ColossusMotor has no Animator. Rebuild the scene or fill it in.");
            }

            if (!_animator.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException($"{name}: the Colossus puppet must stay active; only its renderer is switched off.");
            }
        }

        private void StartOver()
        {
            Unsubscribe();
            if (_isShowing)
            {
                Hide();
            }

            _session = null;
            _swarm = null;
            _config = null;
        }

        private void ReadSamples(ColossusSampleSet samples)
        {
            if (!samples.IsBaked)
            {
                throw new InvalidOperationException($"{samples.name} holds no samples. Bake it from the puppet with ColossusSampleBaker.");
            }

            int boneCount = samples.BoneCount;
            if (boneCount > ColossusSampleSet.MaxBones)
            {
                throw new InvalidOperationException($"{samples.name} follows {boneCount} bones, more than the swarm's {ColossusSampleSet.MaxBones}. Bake it again.");
            }

            Transform[] rigBones = _body.bones;
            if (rigBones.Length != samples.RendererBoneCount)
            {
                throw new InvalidOperationException(
                    $"{name}: the puppet has {rigBones.Length} bones, but {samples.name} was baked from a rig with {samples.RendererBoneCount}. Bake it from this puppet.");
            }

            _bones = new Transform[boneCount];
            for (int slot = 0; slot < boneCount; slot++)
            {
                _bones[slot] = MatchBone(rigBones, samples, slot);
            }

            int sampleCount = samples.SampleCount;
            _boneMatrices = new Matrix4x4[boneCount];
            _sampleBones = new int[sampleCount];
            _sampleOffsets = new Vector3[sampleCount];
            _sampleHeights = new float[sampleCount];
            for (int sample = 0; sample < sampleCount; sample++)
            {
                int bone = samples.BoneIndices[sample];
                if (bone < 0 || bone >= boneCount)
                {
                    throw new InvalidOperationException($"{samples.name}: sample {sample} follows bone {bone}, but there are {boneCount}. Bake it again.");
                }

                _sampleBones[sample] = bone;
                _sampleOffsets[sample] = samples.BoneOffsets[sample];
                _sampleHeights[sample] = samples.Heights01[sample];
            }
        }

        private Transform MatchBone(Transform[] rigBones, ColossusSampleSet samples, int slot)
        {
            int rigIndex = samples.RendererBones[slot];
            Transform bone = rigIndex >= 0 && rigIndex < rigBones.Length ? rigBones[rigIndex] : null;
            string expected = samples.BoneNames[slot];
            if (bone == null || bone.name != expected)
            {
                string found = bone == null ? "nothing" : bone.name;
                throw new InvalidOperationException(
                    $"{name}: {samples.name} expects bone '{expected}' at index {rigIndex}, but the puppet has {found} there. Bake it from this puppet.");
            }

            return bone;
        }

        // Reads the controller's parameters once, so no frame ever sets one that does not exist.
        private void ReadAnimator()
        {
            if (_animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException($"{name}: the Colossus's Animator has no controller. Rebuild the scene or fill it in.");
            }

            // An Animator that has not run yet reports no parameters until it is bound.
            _animator.enabled = true;
            if (!_animator.isInitialized)
            {
                _animator.Rebind();
            }

            bool hasSpeed = false;
            _hasHitTrigger = false;
            int fistTriggers = 0;
            int triggerCount = 0;
            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int index = 0; index < parameters.Length; index++)
            {
                AnimatorControllerParameter parameter = parameters[index];
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == SpeedHash)
                {
                    hasSpeed = true;
                }
                else if (parameter.type == AnimatorControllerParameterType.Trigger && IsAttackTrigger(parameter.nameHash))
                {
                    triggerCount++;
                }
                else if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == HitHash)
                {
                    _hasHitTrigger = true;
                }
                else if (parameter.type == AnimatorControllerParameterType.Trigger
                    && (parameter.nameHash == FistRightHash || parameter.nameHash == FistLeftHash))
                {
                    fistTriggers++;
                }
            }

            _hasFistTriggers = fistTriggers == 2;

            if (!hasSpeed)
            {
                throw new InvalidOperationException($"{name}: the Colossus's Animator controller has no float parameter named Speed.");
            }

            _hasAttackTriggers = triggerCount == AttackTriggerCount;
            if (triggerCount > 0 && !_hasAttackTriggers)
            {
                Debug.LogWarning($"{name}: the Colossus's Animator has only {triggerCount} of the triggers Punch, HeavyPunch and Kick, so the attacks use the swell of the attack pivot instead.", this);
            }

            if (!_hasAttackTriggers)
            {
                if (_attackPivot == null)
                {
                    throw new InvalidOperationException($"{name}: the Colossus's Animator has no attack triggers, so ColossusMotor needs an attack pivot. Rebuild the scene or fill it in.");
                }

                _pivotRestScale = _attackPivot.localScale;
            }
        }

        // The rules own where the giant stands, and its renderer is off, so the Animator must neither move the root
        // nor stop posing the bones because nothing is visible. It stays off until the giant stands, so it costs nothing.
        private void PreparePuppet()
        {
            _restScale = transform.localScale;
            _body.enabled = false;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.enabled = false;
        }

        private static bool IsAttackTrigger(int nameHash)
        {
            return nameHash == PunchHash || nameHash == HeavyPunchHash || nameHash == KickHash;
        }

        private void Show()
        {
            _isShowing = true;
            _lastPosition = _session.ColossusPosition;
            _animator.enabled = true;
        }

        private void Hide()
        {
            _isShowing = false;
            EndMove();
            HideRings();
            EndPulse();
            _animator.enabled = false;
            _isFistOut[0] = false;
            _isFistOut[1] = false;
            if (_swarm != null)
            {
                _swarm.DropFists();
            }
        }

        private void FollowRules(float deltaTime)
        {
            Vector3 position = _session.ColossusPosition;
            Vector3 step = position - _lastPosition;
            step.y = 0f;
            _lastPosition = position;

            Vector3 facing = _session.ColossusFacing;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > MinimumSquaredLength
                ? Quaternion.LookRotation(facing, Vector3.up)
                : transform.rotation;
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = _restScale;

            if (deltaTime <= 0f)
            {
                return;
            }

            float speed = _config.Speed > 0f ? step.magnitude / deltaTime / _config.Speed : 0f;
            _animator.SetFloat(SpeedHash, speed, SpeedDampSeconds, deltaTime);
        }

        private void SendBones()
        {
            for (int slot = 0; slot < _bones.Length; slot++)
            {
                _boneMatrices[slot] = _bones[slot].localToWorldMatrix;
            }

            _swarm.SetColossusBones(_boneMatrices, _bones.Length);
        }

        // Each frame the giant stands, after its bones: where each hand's fist is. The fist leaves from where the hand is on
        // the frame it launches, and that place is kept for its whole flight. Once the match is over the rules' clock stands
        // still, so a fist that is out is no longer fed: the swarm's own clock flies it on, bursts it and brings its wasps home,
        // instead of pinning it in the air at the moment the match ended.
        private void SendFists()
        {
            if (!_swarm.enabled || _session.State != HiveDefenseState.Playing)
            {
                return;
            }

            float radius = _config.FistRadius * _session.ColossusSizeScale;
            for (int hand = 0; hand < _isFistOut.Length; hand++)
            {
                var side = (ColossusHand)hand;
                float since = _session.FistSecondsSinceLaunch(side);
                if (since < 0f)
                {
                    _isFistOut[hand] = false;
                }
                else if (!_isFistOut[hand])
                {
                    _isFistOut[hand] = true;
                    _fistOrigins[hand] = HandPosition(side);
                }

                _swarm.SetFist(
                    hand, _fistOrigins[hand], _session.FistTarget(side), since, _session.FistFlightSeconds(side),
                    _config.FistRegrowSeconds, radius);
            }
        }

        private void HandleColossusFistThrown(ColossusHand hand)
        {
            if (!_session.IsColossusActive)
            {
                return;
            }

            if (!_isShowing)
            {
                Show();
            }

            if (_hasFistTriggers)
            {
                _animator.SetTrigger(hand == ColossusHand.Right ? FistRightHash : FistLeftHash);
            }
        }

        private void HandleColossusAttackStarted(ColossusAttack attack)
        {
            // An attack can start in the same tick the giant appears, before LateUpdate has shown it.
            if (!_session.IsColossusActive)
            {
                return;
            }

            if (!_isShowing)
            {
                Show();
            }

            StartMove(attack);

            if (_hasAttackTriggers)
            {
                int trigger = TriggerOf(attack);
                if (trigger != 0)
                {
                    _animator.SetTrigger(trigger);
                }

                return;
            }

            StartPulse(attack);
        }

        private int TriggerOf(ColossusAttack attack)
        {
            switch (attack)
            {
                case ColossusAttack.Punch:
                    return PunchHash;
                case ColossusAttack.HeavyPunch:
                    return HeavyPunchHash;
                case ColossusAttack.Kick:
                case ColossusAttack.SpinKick:
                    return KickHash;
                case ColossusAttack.Slam:
                case ColossusAttack.LeapSlam:
                    return HeavyPunchHash;
                case ColossusAttack.Roar:
                    return _hasHitTrigger ? HitHash : 0;
                default:
                    return 0;
            }
        }

        // The swell peaks when the rules land the hit, so the fallback lines up with the damage.
        private float HitDelayOf(ColossusAttack attack)
        {
            switch (attack)
            {
                case ColossusAttack.Punch:
                    return _config.PunchHitDelay;
                case ColossusAttack.HeavyPunch:
                    return _config.HeavyPunchHitDelay;
                case ColossusAttack.Kick:
                    return _config.KickHitDelay;
                case ColossusAttack.Slam:
                    return _config.SlamHitDelay;
                case ColossusAttack.Roar:
                    return _config.RoarHitDelay;
                case ColossusAttack.SpinKick:
                    return _config.SpinKickHitDelay;
                case ColossusAttack.LeapSlam:
                    return _config.LeapSeconds;
                default:
                    return 0f;
            }
        }

        private void StartPulse(ColossusAttack attack)
        {
            if (attack == ColossusAttack.None)
            {
                return;
            }

            _isPulsing = true;
            _pulseSeconds = 0f;
            _pulseRiseSeconds = Mathf.Max(HitDelayOf(attack), MinimumPulseSeconds);
            _pulseStrength = attack == ColossusAttack.HeavyPunch ? _pulseGrowth * HeavyPulseFactor : _pulseGrowth;
        }

        // Runs after the Animator, so the swell wins over any scale the clip writes, and before the bones are sent.
        private void AdvancePulse(float deltaTime)
        {
            if (!_isPulsing)
            {
                return;
            }

            _pulseSeconds += deltaTime;
            float recoverSeconds = Mathf.Max(_pulseRecoverSeconds, MinimumPulseSeconds);
            float afterHit = _pulseSeconds - _pulseRiseSeconds;
            if (afterHit >= recoverSeconds)
            {
                EndPulse();
                return;
            }

            // Speeds up into the hit, then drops back quickly and settles.
            float swell;
            if (afterHit < 0f)
            {
                float rise = _pulseSeconds / _pulseRiseSeconds;
                swell = rise * rise;
            }
            else
            {
                float left = 1f - (afterHit / recoverSeconds);
                swell = left * left;
            }

            _attackPivot.localScale = _pivotRestScale * (1f + (_pulseStrength * swell));
        }

        private void EndPulse()
        {
            if (!_isPulsing)
            {
                return;
            }

            _isPulsing = false;
            if (_attackPivot != null)
            {
                _attackPivot.localScale = _pivotRestScale;
            }
        }

        // The slam's and the roar's rings show their reach at the giant's level, from the rules, so what the player sees is
        // what is hit. The leap's ring waits for the landing.
        private void StartMove(ColossusAttack attack)
        {
            float scale = _session.ColossusSizeScale;
            switch (attack)
            {
                case ColossusAttack.Slam:
                    StartRing(ref _slamRingAge, ref _slamRingCenter, ref _slamRingRadius, _session.ColossusPosition, _config.SlamRadius * scale);
                    break;
                case ColossusAttack.Roar:
                    StartRing(ref _roarRingAge, ref _roarRingCenter, ref _roarRingRadius, _session.ColossusPosition, _config.RoarRadius * scale);
                    break;
                case ColossusAttack.SpinKick:
                case ColossusAttack.LeapSlam:
                    break;
                default:
                    return;
            }

            _move = attack;
            _moveSeconds = 0f;
        }

        private static void StartRing(ref float age, ref Vector3 center, ref float radius, Vector3 position, float reach)
        {
            age = 0f;
            center = position;
            radius = Mathf.Max(MinimumRingRadius, reach);
        }

        // The leap's time is the rules' own, so the puppet lands on the tick the landing hits.
        private float MoveDuration()
        {
            switch (_move)
            {
                case ColossusAttack.Slam:
                    return _slamSeconds;
                case ColossusAttack.Roar:
                    return _roarSeconds;
                case ColossusAttack.SpinKick:
                    return _spinSeconds;
                default:
                    return Mathf.Max(_config.LeapSeconds, MinimumPulseSeconds);
            }
        }

        // Runs after FollowRules put the puppet where the rules say, so the move only adds to the pose: a squash of the
        // root, a turn about the vertical axis, or height along the leap's arc. The session owns the spot on the ground.
        private void AdvanceMove(float deltaTime)
        {
            if (_move == ColossusAttack.None)
            {
                return;
            }

            _moveSeconds += deltaTime;
            float duration = MoveDuration();
            float t = Mathf.Clamp01(_moveSeconds / duration);
            float wave = Mathf.Sin(t * Mathf.PI);
            Vector3 scale = _restScale;
            switch (_move)
            {
                case ColossusAttack.Slam:
                    scale.y *= 1f - (_slamDip * wave);
                    scale.x *= 1f + (_slamDip * 0.5f * wave);
                    scale.z *= 1f + (_slamDip * 0.5f * wave);
                    break;
                case ColossusAttack.Roar:
                    scale.y *= 1f + (_roarRear * wave);
                    break;
                case ColossusAttack.SpinKick:
                    transform.rotation = Quaternion.AngleAxis(FullTurnDegrees * t * t * (3f - (2f * t)), Vector3.up) * transform.rotation;
                    break;
                case ColossusAttack.LeapSlam:
                    Vector3 position = transform.position;
                    position.y += _leapHeight * ArcHeightFactor * t * (1f - t);
                    transform.position = position;
                    break;
            }

            transform.localScale = scale;

            // The rules end the leap on its landing, or drop it when the giant ends, whichever comes first.
            bool isLanded = _move == ColossusAttack.LeapSlam && !_session.IsColossusLeaping;
            if (t >= 1f || isLanded)
            {
                if (_move == ColossusAttack.LeapSlam)
                {
                    StartRing(
                        ref _slamRingAge, ref _slamRingCenter, ref _slamRingRadius, _session.ColossusLeapTarget,
                        _config.LeapRadius * _session.ColossusSizeScale);
                }

                EndMove();
            }
        }

        private void EndMove()
        {
            _move = ColossusAttack.None;
            _moveSeconds = 0f;
            transform.localScale = _restScale;
        }

        // The two rings are made once and reused. They are the hive ring's kind: a flat band of triangles on the ground,
        // drawn unlit and tinted with a property block.
        private void BuildRings()
        {
            if (_ringMesh != null)
            {
                return;
            }

            Shader shader = Shader.Find(RingShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException($"{name}: the shader {RingShaderName} was not found.");
            }

            _ringMaterial = new Material(shader) { name = "ColossusMotor Ring", hideFlags = HideFlags.HideAndDontSave };
            _ringMesh = BuildRingMesh(RingInnerRatio);
            _ringBlock = new MaterialPropertyBlock();
            _slamRing = BuildRing("SlamRing", out _slamRingRenderer);
            _roarRing = BuildRing("RoarRing", out _roarRingRenderer);
            _leapMarker = BuildRing("LeapMarker", out _leapMarkerRenderer);
        }

        private Transform BuildRing(string ringName, out Renderer ringRenderer)
        {
            var ring = new GameObject(ringName);
            // Not a child of the puppet: the puppet's scale and spin must not move a ring that sits on the ground.
            ring.transform.SetParent(transform.parent, false);
            ring.AddComponent<MeshFilter>().sharedMesh = _ringMesh;
            var meshRenderer = ring.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _ringMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;
            ringRenderer = meshRenderer;
            return ring.transform;
        }

        private void AdvanceRings(float deltaTime)
        {
            AdvanceRing(ref _slamRingAge, _slamRing, _slamRingRenderer, _slamRingCenter, _slamRingRadius, _slamRingColor, deltaTime);
            AdvanceRing(ref _roarRingAge, _roarRing, _roarRingRenderer, _roarRingCenter, _roarRingRadius, _roarRingColor, deltaTime);
        }

        private void AdvanceRing(ref float age, Transform ring, Renderer ringRenderer, Vector3 center, float radius, Color color, float deltaTime)
        {
            if (age < 0f)
            {
                return;
            }

            age += deltaTime;
            float t = age / _ringSeconds;
            if (t >= 1f)
            {
                age = -1f;
                ringRenderer.enabled = false;
                return;
            }

            // Fast out, then slower, while it fades away.
            float grow = 1f - ((1f - t) * (1f - t));
            ring.position = new Vector3(center.x, RingHeight, center.z);
            ring.localScale = new Vector3(radius * grow, 1f, radius * grow);
            color.a *= 1f - t;
            _ringBlock.SetColor(ColorHash, color);
            ringRenderer.SetPropertyBlock(_ringBlock);
            ringRenderer.enabled = true;
        }

        // Songs-v2 R, small fix 1: from the take-off, when the rules have chosen where the leap lands, until the landing, a ring
        // as wide as the landing's reach marks the spot. It starts a little wider and brightens as it closes, as the giant
        // comes down.
        private void ShowLeapMarker()
        {
            if (_leapMarkerRenderer == null)
            {
                return;
            }

            bool isLeaping = _session.IsColossusLeaping;
            _leapMarkerRenderer.enabled = isLeaping;
            if (!isLeaping)
            {
                return;
            }

            float flown = _move == ColossusAttack.LeapSlam ? Mathf.Clamp01(_moveSeconds / MoveDuration()) : 0f;
            float radius = Mathf.Max(MinimumRingRadius, _config.LeapRadius * _session.ColossusSizeScale) * (1f + (_leapMarkerSpread * (1f - flown)));
            Vector3 target = _session.ColossusLeapTarget;
            _leapMarker.position = new Vector3(target.x, RingHeight, target.z);
            _leapMarker.localScale = new Vector3(radius, 1f, radius);
            Color color = _leapMarkerColor;
            color.a *= Mathf.Lerp(0.55f, 1f, flown);
            _ringBlock.SetColor(ColorHash, color);
            _leapMarkerRenderer.SetPropertyBlock(_ringBlock);
        }

        private void HideRings()
        {
            _slamRingAge = -1f;
            _roarRingAge = -1f;
            if (_leapMarkerRenderer != null)
            {
                _leapMarkerRenderer.enabled = false;
            }

            if (_slamRingRenderer != null)
            {
                _slamRingRenderer.enabled = false;
            }

            if (_roarRingRenderer != null)
            {
                _roarRingRenderer.enabled = false;
            }
        }

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

            var mesh = new Mesh { name = "ColossusMotor Ring", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Unsubscribe()
        {
            if (_session != null)
            {
                _session.ColossusAttackStarted -= HandleColossusAttackStarted;
                _session.ColossusFistThrown -= HandleColossusFistThrown;
            }
        }
    }
}
