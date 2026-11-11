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

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int PunchHash = Animator.StringToHash("Punch");
        private static readonly int HeavyPunchHash = Animator.StringToHash("HeavyPunch");
        private static readonly int KickHash = Animator.StringToHash("Kick");
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

        private HiveDefenseSession _session;
        private SwarmGpuView _swarm;
        private ColossusConfig _config;
        private Transform[] _bones;
        private Matrix4x4[] _boneMatrices;
        private int[] _sampleBones;
        private Vector3[] _sampleOffsets;
        private float[] _sampleHeights;
        private bool _hasAttackTriggers;
        private bool _isShowing;
        private Vector3 _lastPosition;
        private Vector3 _pivotRestScale = Vector3.one;
        private bool _isPulsing;
        private float _pulseSeconds;
        private float _pulseRiseSeconds;
        private float _pulseStrength;

        /// <summary>True while the giant stands and its bones go to the swarm each frame, for tests and debugging.</summary>
        public bool IsShowing => _isShowing;

        /// <summary>True when the Animator plays the attacks; false when the fallback swell of the attack pivot does.</summary>
        public bool HasAttackTriggers => _hasAttackTriggers;

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
            AdvancePulse(deltaTime);
            SendBones();
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

        private void OnDestroy() => Unsubscribe();

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

            _session = session;
            _swarm = swarm;
            _config = config;
            _session.ColossusAttackStarted += HandleColossusAttackStarted;

            // A swarm view that switched itself off (no compute shaders) has already said so, and holds no wasps.
            if (swarm.enabled)
            {
                swarm.SetColossusSamples(_sampleBones, _sampleOffsets, _sampleHeights);
            }
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
            }

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
            EndPulse();
            _animator.enabled = false;
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

        private static int TriggerOf(ColossusAttack attack)
        {
            switch (attack)
            {
                case ColossusAttack.Punch:
                    return PunchHash;
                case ColossusAttack.HeavyPunch:
                    return HeavyPunchHash;
                case ColossusAttack.Kick:
                    return KickHash;
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

        private void Unsubscribe()
        {
            if (_session != null)
            {
                _session.ColossusAttackStarted -= HandleColossusAttackStarted;
            }
        }
    }
}
