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
    /// impact, and the giant's slam and leap, push the camera in for a moment.
    /// Several hits in one frame merge into one shake and one freeze.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Hit Feedback View")]
    public sealed class HitFeedbackView : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // The ground ring is one flat band round a circle of radius 1, scaled to the Tornado's reach.
        private const int RingSegments = 96;

        [Header("Camera")]
        [SerializeField, Min(1f), Tooltip("The damage that shakes the camera at full strength.")]
        private float _fullShakeDamage = 80f;

        [SerializeField, Range(0f, 1f), Tooltip("The least a hit shakes the camera, 0 to 1, so even a small hit is felt.")]
        private float _leastShake = 0.06f;

        [SerializeField, Range(0f, 1f), Tooltip("How hard the camera kicks the frame a song is cast, 0 to 1.")]
        private float _castKick = 0.35f;

        [SerializeField, Range(0f, 1f), Tooltip("How much of a shake a blocked hit gives, as a share of the same damage unblocked.")]
        private float _blockedShakeShare = 0.4f;

        [SerializeField, Range(0f, 1f), Tooltip("How hard the camera pushes in on a heavy song's impact (the Meteor, the Lance, the Hydra, a Nest going off, the Tornado's throw), 0 to 1. The camera's Liveliness scales it.")]
        private float _heavyPushIn = 0.8f;

        [SerializeField, Range(0f, 1f), Tooltip("How hard the camera pushes in when the giant's ground slam or leap slam lands, 0 to 1. The camera's Liveliness scales it.")]
        private float _giantPushIn = 0.5f;

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

        /// <summary>The shake strength the hits of this frame asked for, 0 to 1, until the end of the frame. For tests.</summary>
        public float PendingShake => _pendingShake;

        /// <summary>True while a freeze is waiting for the end of the frame. For tests.</summary>
        public bool HasPendingFreeze => _hasPendingFreeze;

        /// <summary>Starts listening to the match. Call once, after the swarm view and the camera exist; calling again starts over.</summary>
        /// <exception cref="ArgumentNullException">An argument is missing.</exception>
        /// <exception cref="InvalidOperationException">The shield flash material is not set.</exception>
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

            _pendingShake = 0f;
            _hasPendingFreeze = false;
            BuildFlashes();
            _session.SongLanded += OnSongLanded;
            _session.SongCastNow += OnSongCastNow;
            _session.Forms.Landed += OnFormLanded;
            _session.ColossusAttackLanded += OnColossusLanded;
            _isReady = true;
        }

        private void LateUpdate()
        {
            if (!_isReady)
            {
                return;
            }

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
            }
        }

        private void OnSongLanded(SongHit hit)
        {
            float damage01 = Mathf.Clamp01(hit.Damage / _fullShakeDamage);
            float shake = Mathf.Max(_leastShake, damage01);
            _pendingShake = Mathf.Max(_pendingShake, hit.IsBlocked ? shake * _blockedShakeShare : shake);

            if (hit.IsBlocked)
            {
                ShowShieldFlash(hit.Position);
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
            }

            if (form != SwarmForm.Tornado)
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

        private static bool IsHeavyImpact(SwarmForm form)
        {
            return form == SwarmForm.Meteor || form == SwarmForm.Lance || form == SwarmForm.Hydra || form == SwarmForm.Nest
                || form == SwarmForm.Tornado;
        }

        // The giant's ground slam and leap slam land with a push-in, whether or not they hit anyone; their hits are felt
        // through SongLanded like any other.
        private void OnColossusLanded(ColossusAttack attack)
        {
            if (attack == ColossusAttack.Slam || attack == ColossusAttack.LeapSlam)
            {
                _camera.PushIn(_giantPushIn);
            }
        }

        private void OnSongCastNow(int song)
        {
            _camera.Kick(_castKick);
            if (_session.Caster[song] is SlotSongEffect slotSong)
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
