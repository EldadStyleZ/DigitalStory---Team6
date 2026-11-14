using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// The felt hit. A keyboard has no haptics, so a hit is felt on the screen: the beekeeper it lands on flashes white and recoils,
    /// the camera shakes in proportion to the damage, a heavy hit holds the swarm still for a moment, and a hit a shield blocks flashes
    /// blue on the shield. The frame a song is cast, the squad flashes and the camera kicks, before any wasp has moved. The
    /// Tornado's hit also flashes a red ring on the ground at its reach, so where it throws is impossible to miss. A heavy song's
    /// impact, and the giant's heavy blows (its slam, its leap's landing and its third punch), push the camera in for a moment.
    /// Several hits in one frame merge into one shake and one freeze.
    /// It is also the feel director of plan section Y: every moment of play adds trauma by its weight (a hit least, a kill a
    /// little, a milestone of a kill chain, a heavy song, the giant's blows, a hive hit or lost, a wave cleared more), a cast
    /// punches the view's zoom and roll, the giant's blows kick the view along its facing, and kills feed a kill chain whose
    /// steps the HUD's sound and stamp read. Its numbers live in the FeelConfig.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Hit Feedback View")]
    public sealed class HitFeedbackView : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // The ground ring is one flat band round a circle of radius 1, scaled to the Tornado's reach.
        private const int RingSegments = 96;

        [Header("Camera")]
        [SerializeField, Tooltip("The feel numbers: trauma per moment, punches, the kill chain and the master scale.")]
        private FeelConfig _feel;

        [SerializeField, Range(0f, 1f), Tooltip("How hard the camera kicks the frame a song is cast, 0 to 1.")]
        private float _castKick = 0.35f;

        [SerializeField, Range(0f, 1f), Tooltip("How much of a shake a blocked hit gives, as a share of the same damage unblocked.")]
        private float _blockedShakeShare = 0.4f;

        [SerializeField, Range(0f, 1f), Tooltip("How hard the camera pushes in on a heavy song's impact (the Meteor, the Lance, the Hydra, a Nest going off, the Tornado's throw), 0 to 1. The camera's Liveliness scales it.")]
        private float _heavyPushIn = 0.8f;

        [SerializeField, Range(0f, 1f), Tooltip("How hard the camera pushes in when one of the giant's heavy blows lands (the ground slam, the leap slam or the third punch), 0 to 1. The camera's Liveliness scales it.")]
        private float _giantPushIn = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("How hard the camera pushes in as a giant's fist leaves its arm, 0 to 1: a small push. The camera's Liveliness scales it.")]
        private float _fistPushIn = 0.25f;

        [Header("Freeze")]
        [SerializeField, Min(0f), Tooltip("Seconds the swarm's motion holds still on a heavy hit.")]
        private float _heavyFreezeSeconds = 0.05f;

        [Header("Beekeeper")]
        [SerializeField, Min(0.1f), Tooltip("Metres from a hit within which the nearest beekeeper takes the flash and the recoil.")]
        private float _beekeeperReach = 3f;

        [SerializeField, Min(1f), Tooltip("The damage that gives a beekeeper its full recoil.")]
        private float _fullRecoilDamage = 60f;

        [Header("Shield flash")]
        [SerializeField, Tooltip("An unlit, transparent, additive material for the blue flash on a shield that blocks a hit.")]
        private Material _shieldFlashMaterial;

        [SerializeField, Tooltip("The colour of the shield flash.")]
        private Color _shieldFlashColor = new Color(0.35f, 0.65f, 1f, 0.8f);

        [SerializeField, Min(0.05f), Tooltip("Seconds the shield flash lasts.")]
        private float _shieldFlashSeconds = 0.25f;

        [SerializeField, Min(0.1f), Tooltip("The flash's diameter at its start, in metres. Larger than a hive, so it shows round it.")]
        private float _shieldFlashStartSize = 2.4f;

        [SerializeField, Min(0.1f), Tooltip("The flash's diameter at its end, in metres.")]
        private float _shieldFlashEndSize = 4.2f;

        [SerializeField, Min(0f), Tooltip("Metres above the guarded point (the core or a hive's foot) the flash is centred, so it wraps the hive instead of sinking into the ground.")]
        private float _shieldFlashHeight = 1f;

        [SerializeField, Min(1), Tooltip("How many shield flashes can show at once.")]
        private int _shieldFlashPool = 8;

        [Header("Tornado ring")]
        [SerializeField, Tooltip("The colour of the ring the Tornado's hit flashes on the ground at its reach.")]
        private Color _throwRingColor = new Color(1f, 0.35f, 0.2f, 0.9f);

        [SerializeField, Range(0.5f, 0.99f), Tooltip("The ring's inner edge, as a share of the reach. Its outer edge is the reach.")]
        private float _throwRingInnerShare = 0.86f;

        [SerializeField, Range(0.1f, 1f), Tooltip("The share of the reach the ring starts at, before it snaps out to the reach.")]
        private float _throwRingStartShare = 0.55f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the ring takes to snap out to the reach.")]
        private float _throwRingGrowSeconds = 0.12f;

        [SerializeField, Min(0.05f), Tooltip("Seconds the ring shows, from the hit, fading over the last half.")]
        private float _throwRingSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("Metres above the ground the ring lies, so the ground never hides it.")]
        private float _throwRingHeight = 0.06f;

        private HiveDefenseSession _session;
        private FollowCamera _camera;
        private SwarmGpuView _swarm;
        private MaterialPropertyBlock _block;
        private Transform[] _flashTransforms;
        private Renderer[] _flashRenderers;
        private float[] _flashAges;
        private Transform _ringTransform;
        private Renderer _ringRenderer;
        private Mesh _ringMesh;
        private float _ringAge = -1f;
        private float _ringReach;
        private float _pendingShake;
        private bool _hasPendingFreeze;
        private bool _isReady;
        private KillChain _killChain;
        private KillChainStep _lastChainStep;
        private float[] _hiveHitAt;
        private bool _wasColossus;
        private bool _wasWaveRunning;
        private float _castRollSign = 1f;

        /// <summary>Raised for each kill, with where the kill chain stands; the HUD's sound climbs and stamps from it.</summary>
        public event Action<KillChainStep> KillChained;

        /// <summary>The kill chain's step for the last kill. For tests.</summary>
        public KillChainStep LastChainStep => _lastChainStep;

        /// <summary>The feel numbers, for the views that read the kill chain.</summary>
        public FeelConfig Feel => _feel;

        /// <summary>The shake strength the hits of this frame asked for, 0 to 1, until the end of the frame. For tests.</summary>
        public float PendingShake => _pendingShake;

        /// <summary>True while a freeze is waiting for the end of the frame. For tests.</summary>
        public bool HasPendingFreeze => _hasPendingFreeze;

        /// <summary>Starts listening to the match. Call once, after the swarm view and the camera exist; calling again starts over.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="InvalidOperationException">The shield flash material or the feel config is not set.</exception>
        public void Initialize(HiveDefenseSession session, FollowCamera camera, SwarmGpuView swarm)
        {
            Release();
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            _swarm = swarm != null ? swarm : throw new ArgumentNullException(nameof(swarm));
            if (_shieldFlashMaterial == null)
            {
                throw new InvalidOperationException("HitFeedbackView needs the shield flash material. Set it on the HitFeedbackView.");
            }

            if (_feel == null)
            {
                throw new InvalidOperationException("HitFeedbackView needs a FeelConfig. The scene builder sets it.");
            }

            _camera.Configure(_feel);
            _pendingShake = 0f;
            _hasPendingFreeze = false;
            _killChain = new KillChain(_feel.ChainWindowSeconds, _feel.ChainTopSemitones, _feel.ChainMilestones);
            _lastChainStep = default;
            _wasColossus = _session.IsColossusActive;
            _wasWaveRunning = IsWaveRunning();
            BuildFlashes();
            _session.SongLanded += OnSongLanded;
            _session.SongCastNow += OnSongCastNow;
            _session.Forms.Landed += OnFormLanded;
            _session.ColossusAttackLanded += OnColossusLanded;
            _session.ColossusFistLaunched += OnColossusFistLaunched;
            _session.ColossusFistLanded += OnColossusFistLanded;
            _session.Hives.HiveDestroyed += OnHiveDestroyed;
            _hiveHitAt = new float[_session.Hives.Count];
            for (int index = 0; index < _session.Hives.Count; index++)
            {
                _hiveHitAt[index] = float.NegativeInfinity;
                _session.Hives[index].Damaged += OnHiveDamaged;
            }

            _isReady = true;
        }

        private void LateUpdate()
        {
            if (!_isReady)
            {
                return;
            }

            StepMoments();
            if (_pendingShake > 0f)
            {
                _camera.Shake(_pendingShake);
                _pendingShake = 0f;
            }

            if (_hasPendingFreeze)
            {
                _swarm.Freeze(_heavyFreezeSeconds);
                _hasPendingFreeze = false;
            }

            StepFlashes(Time.deltaTime);
            StepRing(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Release();
            if (_ringMesh != null)
            {
                Destroy(_ringMesh);
            }
        }

        private void Release()
        {
            _isReady = false;
            if (_session != null)
            {
                _session.SongLanded -= OnSongLanded;
                _session.SongCastNow -= OnSongCastNow;
                _session.Forms.Landed -= OnFormLanded;
                _session.ColossusAttackLanded -= OnColossusLanded;
                _session.ColossusFistLaunched -= OnColossusFistLaunched;
                _session.ColossusFistLanded -= OnColossusFistLanded;
                _session.Hives.HiveDestroyed -= OnHiveDestroyed;
                for (int index = 0; index < _session.Hives.Count; index++)
                {
                    _session.Hives[index].Damaged -= OnHiveDamaged;
                }
            }
        }

        // Adds trauma for this frame. The moments of one frame merge to the strongest, never their sum, so a Tornado that hits
        // twenty beekeepers shakes like one hit.
        private void AddTrauma(float amount)
        {
            _pendingShake = Mathf.Max(_pendingShake, amount);
        }

        private bool IsWaveRunning()
        {
            return _session.State == HiveDefenseState.Playing && _session.Phase == MatchPhase.Survive;
        }

        private bool AreFlashesOn => _camera.ShakeScale > 0f;

        // The moments read from the session's state: the swarm becoming the giant, and a wave cleared (its last kill).
        private void StepMoments()
        {
            bool isColossus = _session.IsColossusActive && _session.State == HiveDefenseState.Playing;
            if (isColossus && !_wasColossus)
            {
                AddTrauma(_feel.GiantFormedTrauma);
                _camera.Punch(_feel.GiantFovPunch, 0f);
            }

            _wasColossus = isColossus;
            bool isWaveRunning = IsWaveRunning();
            if (_wasWaveRunning && !isWaveRunning && _session.State != HiveDefenseState.Lost)
            {
                AddTrauma(_feel.WaveClearedTrauma);
            }

            _wasWaveRunning = isWaveRunning;
        }

        private void OnSongLanded(SongHit hit)
        {
            float damage01 = Mathf.Clamp01(hit.Damage / _feel.FullHitDamage);
            float trauma = _feel.HitTrauma * damage01;
            if (hit.IsKill && !hit.IsBlocked)
            {
                trauma = Mathf.Max(trauma, _feel.KillTrauma);
                _lastChainStep = _killChain.Kill(Time.time);
                if (_lastChainStep.Milestone > 0)
                {
                    trauma = Mathf.Max(trauma, _feel.MultiKillTrauma);
                }

                KillChained?.Invoke(_lastChainStep);
            }

            AddTrauma(hit.IsBlocked ? trauma * _blockedShakeShare : trauma);

            if (hit.IsBlocked)
            {
                if (AreFlashesOn)
                {
                    ShowShieldFlash(hit.Position);
                }

                return;
            }

            if (hit.IsHeavy && _heavyFreezeSeconds > 0f)
            {
                _hasPendingFreeze = true;
            }

            BeekeeperView struck = StruckBeekeeper(in hit);
            if (struck != null)
            {
                struck.Hit(hit.Position, Mathf.Clamp01(hit.Damage / _fullRecoilDamage));
            }
        }

        // A heavy song's impact pushes the camera in, whether or not it hits anyone. The Tornado's hit also flashes its ring:
        // its slot's ParamA.x is the reach it hit everyone within, grown by the level.
        private void OnFormLanded(SwarmForm form, Vector3 point)
        {
            if (IsHeavyImpact(form))
            {
                _camera.PushIn(_heavyPushIn);
                AddTrauma(ImpactTrauma(form));
            }

            if (form != SwarmForm.Tornado || !AreFlashesOn)
            {
                return;
            }

            SlotState strike = _session.Forms.Slot(SongSlot.Strike);
            _ringReach = strike.Form == SwarmForm.Tornado ? strike.ParamA.x : 0f;
            if (!(_ringReach > 0f))
            {
                return;
            }

            _ringAge = 0f;
            _ringTransform.position = point + (Vector3.up * _throwRingHeight);
            _ringRenderer.enabled = true;
            PaintRing();
        }

        private float ImpactTrauma(SwarmForm form)
        {
            switch (form)
            {
                case SwarmForm.Meteor:
                    return _feel.MeteorTrauma;
                case SwarmForm.Lance:
                    return _feel.LanceTrauma;
                case SwarmForm.Tornado:
                    return _feel.TornadoTrauma;
                default:
                    return _feel.HydraTrauma;
            }
        }

        private static bool IsHeavyImpact(SwarmForm form)
        {
            return form == SwarmForm.Meteor || form == SwarmForm.Lance || form == SwarmForm.Hydra || form == SwarmForm.Nest
                || form == SwarmForm.Tornado;
        }

        // The giant's heavy blows (plan section I: the ground slam, the leap's landing and the third punch) land with a
        // push-in, whether or not they hit anyone; their hits are felt through SongLanded like any other.
        private void OnColossusLanded(ColossusAttack attack)
        {
            if (attack == ColossusAttack.Slam || attack == ColossusAttack.LeapSlam || attack == ColossusAttack.HeavyPunch)
            {
                _camera.PushIn(_giantPushIn);
                AddTrauma(_feel.GiantHeavyTrauma);
            }
            else if (attack == ColossusAttack.Punch || attack == ColossusAttack.Kick || attack == ColossusAttack.SpinKick)
            {
                AddTrauma(_feel.GiantBlowTrauma);
            }

            if (attack == ColossusAttack.Punch || attack == ColossusAttack.HeavyPunch || attack == ColossusAttack.Kick)
            {
                _camera.Nudge(_session.ColossusFacing, _feel.GiantKickMetres);
            }
        }

        // A thrown fist that lands kicks the view along the giant's facing, as a punch does.
        private void OnColossusFistLanded(ColossusHand hand)
        {
            AddTrauma(_feel.GiantBlowTrauma);
            _camera.Nudge(_session.ColossusFacing, _feel.GiantKickMetres);
        }

        // A hive hit shakes a little, at most once per gap for each hive, so a hive under a crowd never rattles the screen.
        private void OnHiveDamaged(Hive hive)
        {
            for (int index = 0; index < _hiveHitAt.Length; index++)
            {
                if (!ReferenceEquals(_session.Hives[index], hive))
                {
                    continue;
                }

                if (Time.time - _hiveHitAt[index] >= _feel.HiveHitGapSeconds)
                {
                    _hiveHitAt[index] = Time.time;
                    AddTrauma(_feel.HiveHitTrauma);
                }

                return;
            }
        }

        private void OnHiveDestroyed(int hive)
        {
            AddTrauma(_feel.HiveLostTrauma);
        }

        // Songs-v2 R: a small push as the fist leaves; its hit is heavy, felt through SongLanded like the giant's other blows.
        private void OnColossusFistLaunched(ColossusHand hand)
        {
            _camera.PushIn(_fistPushIn);
        }

        // The cast's punch rolls the view to alternate sides, so a run of casts rocks the view instead of leaning it. Its
        // strength follows the song's weight (plan section Z): the Sting, the quick jab, punches lightly.
        private void OnSongCastNow(int song)
        {
            var slotSong = _session.Caster[song] as SlotSongEffect;
            float share = slotSong != null ? CastWeight.PunchShare(slotSong.Form, _feel.LightCastShare) : 1f;
            _camera.Kick(_castKick * share);
            _camera.Punch(_feel.CastFovPunch * share, _feel.CastRollDegrees * share * _castRollSign);
            _castRollSign = -_castRollSign;
            if (AreFlashesOn && slotSong != null)
            {
                _swarm.FlashSlot((int)slotSong.Slot);
            }
        }

        // The beekeeper the hit landed on. The rules report a hit where its brain stands, so the brain nearest the hit's place is
        // that one, never a neighbour that a recoil or a throw draws nearer. A kill is reported after its beekeeper has begun to
        // die, so a dying one counts only for a kill.
        private BeekeeperView StruckBeekeeper(in SongHit hit)
        {
            var views = BeekeeperView.Live;
            BeekeeperView best = null;
            float bestSqr = _beekeeperReach * _beekeeperReach;
            for (int index = 0; index < views.Count; index++)
            {
                BeekeeperView view = views[index];
                if (view.Brain == null || (view.IsDying && !hit.IsKill))
                {
                    continue;
                }

                Vector3 offset = view.Brain.Position - hit.Position;
                offset.y = 0f;
                float sqr = offset.sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = view;
                }
            }

            return best;
        }

        private void BuildFlashes()
        {
            if (_flashTransforms != null)
            {
                return; // Made once; a restart keeps them.
            }

            _block = new MaterialPropertyBlock();
            _flashTransforms = new Transform[_shieldFlashPool];
            _flashRenderers = new Renderer[_shieldFlashPool];
            _flashAges = new float[_shieldFlashPool];
            for (int index = 0; index < _shieldFlashPool; index++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "ShieldFlash";
                Destroy(sphere.GetComponent<Collider>());
                sphere.transform.SetParent(transform, false);
                Renderer meshRenderer = sphere.GetComponent<Renderer>();
                meshRenderer.sharedMaterial = _shieldFlashMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.enabled = false;
                _flashTransforms[index] = sphere.transform;
                _flashRenderers[index] = meshRenderer;
                _flashAges[index] = -1f;
            }

            BuildRing();
        }

        private void BuildRing()
        {
            _ringMesh = BuildRingMesh(_throwRingInnerShare);
            var ring = new GameObject("TornadoRing", typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(transform, false);
            ring.GetComponent<MeshFilter>().sharedMesh = _ringMesh;
            _ringRenderer = ring.GetComponent<MeshRenderer>();
            _ringRenderer.sharedMaterial = _shieldFlashMaterial;
            _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ringRenderer.receiveShadows = false;
            _ringRenderer.enabled = false;
            _ringTransform = ring.transform;
            _ringAge = -1f;
        }

        // A flat band on the ground from innerShare to 1, facing up.
        private static Mesh BuildRingMesh(float innerShare)
        {
            var vertices = new Vector3[(RingSegments + 1) * 2];
            var triangles = new int[RingSegments * 6];
            for (int segment = 0; segment <= RingSegments; segment++)
            {
                float angle = segment * (Mathf.PI * 2f / RingSegments);
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[segment * 2] = direction * innerShare;
                vertices[(segment * 2) + 1] = direction;
                if (segment == RingSegments)
                {
                    continue;
                }

                int first = segment * 2;
                int triangle = segment * 6;
                triangles[triangle] = first;
                triangles[triangle + 1] = first + 2;
                triangles[triangle + 2] = first + 1;
                triangles[triangle + 3] = first + 1;
                triangles[triangle + 4] = first + 2;
                triangles[triangle + 5] = first + 3;
            }

            var mesh = new Mesh { name = "TornadoRing", vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void StepRing(float deltaTime)
        {
            if (_ringAge < 0f)
            {
                return;
            }

            _ringAge += deltaTime;
            if (_ringAge >= _throwRingSeconds)
            {
                _ringAge = -1f;
                _ringRenderer.enabled = false;
                return;
            }

            PaintRing();
        }

        // Snaps out from the start share to the reach, then holds there, fading over the second half of its time.
        private void PaintRing()
        {
            float grow = Mathf.Clamp01(_ringAge / _throwRingGrowSeconds);
            float eased = 1f - ((1f - grow) * (1f - grow));
            float scale = _ringReach * Mathf.Lerp(_throwRingStartShare, 1f, eased);
            _ringTransform.localScale = new Vector3(scale, 1f, scale);
            float fade = Mathf.Clamp01(2f - (2f * _ringAge / _throwRingSeconds));
            Color color = _throwRingColor;
            color.a *= fade;
            _block.SetColor(ColorId, color);
            _ringRenderer.SetPropertyBlock(_block);
        }

        private void ShowShieldFlash(Vector3 position)
        {
            int slot = 0;
            float oldest = -1f;
            for (int index = 0; index < _flashAges.Length; index++)
            {
                if (_flashAges[index] < 0f)
                {
                    slot = index;
                    break;
                }

                if (_flashAges[index] > oldest)
                {
                    oldest = _flashAges[index];
                    slot = index;
                }
            }

            _flashAges[slot] = 0f;
            _flashTransforms[slot].position = position + (Vector3.up * _shieldFlashHeight);
            _flashRenderers[slot].enabled = true;
            PaintFlash(slot, 0f);
        }

        private void StepFlashes(float deltaTime)
        {
            for (int index = 0; index < _flashAges.Length; index++)
            {
                if (_flashAges[index] < 0f)
                {
                    continue;
                }

                _flashAges[index] += deltaTime;
                float t = _flashAges[index] / _shieldFlashSeconds;
                if (t >= 1f)
                {
                    _flashAges[index] = -1f;
                    _flashRenderers[index].enabled = false;
                    continue;
                }

                PaintFlash(index, t);
            }
        }

        private void PaintFlash(int index, float t)
        {
            float size = Mathf.Lerp(_shieldFlashStartSize, _shieldFlashEndSize, 1f - ((1f - t) * (1f - t)));
            _flashTransforms[index].localScale = new Vector3(size, size, size);
            Color color = _shieldFlashColor;
            color.a *= 1f - t;
            _block.SetColor(ColorId, color);
            _flashRenderers[index].SetPropertyBlock(_block);
        }
    }
}
