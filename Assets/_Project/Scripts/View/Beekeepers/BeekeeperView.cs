using System;
using System.Collections.Generic;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>
    /// Shows one beekeeper: follows its brain, walks, leans forward as a warning while it winds up, swings, flashes when hit,
    /// flies up in an arc when a song throws it, and falls over when it dies.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Beekeeper View")]
    public sealed class BeekeeperView : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Metres per second squared that pull a thrown beekeeper back down: the picture's gravity, not a game rule.
        private const float Gravity = 9.81f;

        [SerializeField, Tooltip("The model's Animator. It needs a float parameter named Speed. Optional.")]
        private Animator _animator;

        [SerializeField, Tooltip("The part that leans forward during the wind-up. Optional.")]
        private Transform _pivot;

        [Header("Look")]
        [SerializeField, Range(0f, 90f), Tooltip("Degrees the pivot leans forward at the end of the wind-up.")]
        private float _maxLeanDegrees = 35f;

        [SerializeField, Range(0f, 90f), Tooltip("Degrees the pivot snaps forward to when the swing lands, before it settles upright.")]
        private float _swingDegrees = 55f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the swing takes to settle upright.")]
        private float _swingSeconds = 0.18f;

        [SerializeField, Min(1f), Tooltip("Degrees per second the beekeeper turns toward where it faces.")]
        private float _turnDegreesPerSecond = 720f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the death fall and shrink take.")]
        private float _deathSeconds = 0.8f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the hit flash lasts.")]
        private float _flashSeconds = 0.12f;

        [SerializeField, Tooltip("The colour the renderers flash when health drops.")]
        private Color _flashColor = new Color(1f, 0.25f, 0.2f, 1f);

        [Header("Felt hit")]
        [SerializeField, Tooltip("The colour the renderers flash when a song lands on this beekeeper.")]
        private Color _hitFlashColor = Color.white;

        [SerializeField, Min(0.01f), Tooltip("Seconds the white hit flash lasts.")]
        private float _hitFlashSeconds = 0.09f;

        [SerializeField, Min(0f), Tooltip("Metres a hit pushes the model back, before it settles.")]
        private float _recoilMetres = 0.35f;

        [SerializeField, Min(1f), Tooltip("How fast the recoil settles, per second.")]
        private float _recoilSettlePerSecond = 14f;

        [SerializeField, ColorUsage(false, true), Tooltip("The glow a felt hit adds, so the white suit flashes white even at night. The suit's materials need emission on, which the scene builder sets.")]
        private Color _hitGlowColor = new Color(1.4f, 1.4f, 1.4f, 1f);

        [Header("Thrown")]
        [SerializeField, Min(0.1f), Tooltip("Metres per second on the ground above which the beekeeper counts as thrown: faster than any beekeeper walks.")]
        private float _thrownSpeed = 5f;

        [SerializeField, Min(1f), Tooltip("Metres per second on the ground above which a move is a jump to a new place (a spawn), never a throw.")]
        private float _teleportSpeed = 40f;

        [SerializeField, Min(0f), Tooltip("Metres per second upward the model is thrown for each metre per second over the thrown speed. The Tornado's 12 m/s throw rises about a metre, the Gust's 9 m/s shove a hop.")]
        private float _liftPerSpeed = 0.5f;

        [SerializeField, Range(0f, 90f), Tooltip("Degrees the model tips back for each metre it is off the ground, as it flies away from the blow.")]
        private float _tumbleDegreesPerMetre = 35f;

        [SerializeField, Range(0f, 90f), Tooltip("The most the model tips back in flight, in degrees, so it reads as flying rather than lying.")]
        private float _mostTumbleDegrees = 60f;

        private static readonly List<BeekeeperView> LiveViews = new List<BeekeeperView>(64);

        private Vector3 _recoil;
        private Vector3 _lastBrainPosition;
        private Vector3 _flightDirection;
        private Quaternion _facing = Quaternion.identity;
        private float _liftHeight;
        private float _liftSpeed;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private BeekeeperBrain _brain;
        private Vector3 _startScale;
        private float _lastHealth;
        private float _lastProgress;
        private float _swingLeft;
        private float _flashLeft;
        private float _hitFlashLeft;
        private float _deathElapsed;
        private float _lastSpeed = -1f;
        private bool _isDying;
        private bool _isFlashing;

        /// <summary>Raised once, after the death animation ends. The pool takes the view back.</summary>
        public event Action<BeekeeperView> DeathFinished;

        /// <summary>The brain this view shows, or null while it is not bound.</summary>
        public BeekeeperBrain Brain => _brain;

        /// <summary>The kind of the bound brain.</summary>
        public BeekeeperKind Kind { get; private set; }

        /// <summary>True while the death animation runs.</summary>
        public bool IsDying => _isDying;

        /// <summary>Every enabled beekeeper view. Read only, so a hit can find the one nearest to it without a search or an allocation.</summary>
        public static IReadOnlyList<BeekeeperView> Live => LiveViews;

        /// <summary>The model's position as drawn, with any recoil and lift. For tests.</summary>
        public Vector3 ShownPosition => transform.position;

        /// <summary>True while the white flash of a felt hit shows. For tests.</summary>
        public bool IsHitFlashing => _hitFlashLeft > 0f;

        /// <summary>
        /// The felt hit: flashes the model white and pushes it back, away from where the hit came from. The recoil is scaled by
        /// <paramref name="strength01"/>, 0 to 1. The killing blow lands as the death begins, so a dying model flashes too, but
        /// is not pushed: its fall holds it where it died. Allocates nothing.
        /// </summary>
        public void Hit(Vector3 hitPoint, float strength01)
        {
            if (_brain == null)
            {
                return;
            }

            _flashLeft = Mathf.Max(_flashLeft, _hitFlashSeconds);
            _hitFlashLeft = _hitFlashSeconds;
            _block.SetColor(ColorId, _hitFlashColor);
            _block.SetColor(EmissionColorId, _hitGlowColor);
            SetBlock(_block);
            _isFlashing = true;

            if (_isDying)
            {
                return;
            }

            Vector3 away = _brain.Position - hitPoint;
            away.y = 0f;
            if (away.sqrMagnitude > 1e-6f)
            {
                _recoil = away.normalized * (_recoilMetres * Mathf.Clamp01(strength01));
            }
        }

        private void OnEnable()
        {
            LiveViews.Add(this);
        }

        private void Awake()
        {
            _startScale = transform.localScale;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            _spine = FindBone(transform, "Spine");
            _upperArm = FindBone(transform, "RightUpperArm");
            _lowerArm = FindBone(transform, "RightLowerArm");
            for (int index = 0; index < 3; index++)
            {
                _poseSet[index] = new Quaternion(0f, 0f, 0f, 0f);
            }
        }

        /// <summary>Starts showing the brain, from its spawn point. Call after the brain spawned.</summary>
        public void Bind(BeekeeperBrain brain)
        {
            if (brain == null)
            {
                throw new ArgumentNullException(nameof(brain), "BeekeeperView needs a brain to show.");
            }

            _brain = brain;
            Kind = brain.Config.Kind;
            _lastHealth = brain.Health;
            _lastProgress = 0f;
            _swingLeft = 0f;
            _deathElapsed = 0f;
            _isDying = false;
            _lastSpeed = -1f;
            ClearFlash();
            _recoil = Vector3.zero;
            _lastBrainPosition = brain.Position;
            _liftHeight = 0f;
            _liftSpeed = 0f;
            _facing = FacingRotation(brain.Facing);

            transform.localScale = _startScale;
            transform.SetPositionAndRotation(brain.Position, _facing);
            SetLean(0f);
            SetSpeed(0f);
        }

        /// <summary>Stops showing the brain and puts the model back upright.</summary>
        public void Unbind()
        {
            _brain = null;
            _isDying = false;
            ClearFlash();
            transform.localScale = _startScale;
            transform.rotation = Quaternion.identity;
            SetLean(0f);
        }

        /// <summary>Starts the death animation. Does nothing when it already runs or the view is not bound.</summary>
        public void BeginDeath()
        {
            if (_brain == null || _isDying)
            {
                return;
            }

            _isDying = true;
            _deathElapsed = 0f;
            _swingLeft = 0f;
            Pose(0f, 0f);
            SetSpeed(0f);
        }

        private void LateUpdate()
        {
            if (_brain == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            ShowFlash(deltaTime);

            if (_isDying)
            {
                ShowDeath(deltaTime);
                return;
            }

            if (!_brain.IsAlive)
            {
                BeginDeath();
                return;
            }

            _recoil = Vector3.Lerp(_recoil, Vector3.zero, 1f - Mathf.Exp(-_recoilSettlePerSecond * deltaTime));
            ShowFlight(deltaTime);
            transform.position = OnGround(_brain.Position) + _recoil + (Vector3.up * _liftHeight);
            _facing = Quaternion.RotateTowards(_facing, FacingRotation(_brain.Facing), _turnDegreesPerSecond * deltaTime);
            transform.rotation = Tumble() * _facing;
            SetSpeed(_brain.IsMoving ? 1f : 0f);
            ShowLean(deltaTime);
        }

        // The brain only slides a thrown beekeeper along the ground, so the picture reads its speed and, when it is faster
        // than any walk, throws the model up in an arc that falls back under gravity: the Tornado's throw and the Gust's shove
        // become impossible to miss.
        private void ShowFlight(float deltaTime)
        {
            Vector3 step = _brain.Position - _lastBrainPosition;
            step.y = 0f;
            _lastBrainPosition = _brain.Position;
            float speed = deltaTime > 0f ? step.magnitude / deltaTime : 0f;
            if (_liftHeight <= 0f && speed > _thrownSpeed && speed < _teleportSpeed)
            {
                _liftSpeed = (speed - _thrownSpeed) * _liftPerSpeed;
                _flightDirection = step / (speed * deltaTime);
            }

            if (_liftHeight <= 0f && _liftSpeed <= 0f)
            {
                return;
            }

            _liftHeight += _liftSpeed * deltaTime;
            _liftSpeed -= Gravity * deltaTime;
            if (_liftHeight <= 0f)
            {
                _liftHeight = 0f;
                _liftSpeed = 0f;
            }
        }

        // Tips the flying model back, away from the way it flies.
        private Quaternion Tumble()
        {
            if (_liftHeight <= 0f)
            {
                return Quaternion.identity;
            }

            Vector3 axis = Vector3.Cross(_flightDirection, Vector3.up);
            return Quaternion.AngleAxis(Mathf.Min(_mostTumbleDegrees, _tumbleDegreesPerMetre * _liftHeight), axis);
        }

        private void OnDisable()
        {
            LiveViews.Remove(this);
            _recoil = Vector3.zero;
            if (_block != null && _renderers != null)
            {
                ClearFlash();
            }
        }

        // A real blow instead of a fall: the wind-up raises the stick arm over the head and leans the body back, the strike
        // whips the arm down and the chest forward into the hive, then the beekeeper straightens up again.
        private void ShowLean(float deltaTime)
        {
            float progress = _brain.WindUpProgress;
            if (progress > 0f)
            {
                _swingLeft = 0f;
                float raise = Mathf.SmoothStep(0f, 1f, progress);
                Pose(raise, 0f);
            }
            else if (_lastProgress > 0f)
            {
                _swingLeft = StrikeSeconds;
            }

            _lastProgress = progress;
            if (progress > 0f)
            {
                return;
            }

            if (_swingLeft <= 0f)
            {
                Pose(0f, 0f);
                return;
            }

            _swingLeft = Mathf.Max(0f, _swingLeft - deltaTime);
            float t = 1f - (_swingLeft / StrikeSeconds);
            float down = Mathf.Clamp01(t / StrikeDownShare);
            float back = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(StrikeHoldUntil, 1f, t));
            Pose(1f - (down * down), down * (1f - back));
        }

        private const float StrikeSeconds = 0.5f;
        private const float StrikeDownShare = 0.22f;    // share of the strike the arm takes to come down
        private const float StrikeHoldUntil = 0.5f;     // the blow holds on the hive until here, then recovers
        private const float RaiseArmDegrees = 165f;
        private const float RaiseElbowDegrees = 55f;
        private const float RaiseLeanBackDegrees = 14f;
        private const float StrikeArmDegrees = 55f;
        private const float StrikeChestDegrees = 24f;
        private const float StrikeBodyDegrees = 8f;

        private Transform _spine;
        private Transform _upperArm;
        private Transform _lowerArm;
        private readonly Quaternion[] _poseBase = new Quaternion[3];
        private readonly Quaternion[] _poseSet = new Quaternion[3];

        // raise: 0 rest, 1 arm over the head. strike: 0 rest, 1 arm forward and down on the hive, chest bent in.
        private void Pose(float raise, float strike)
        {
            SetLean((StrikeBodyDegrees * strike) - (3f * raise));
            Vector3 axis = transform.right;
            Bend(0, _spine, (StrikeChestDegrees * strike) - (RaiseLeanBackDegrees * raise), axis);
            Bend(1, _upperArm, -(RaiseArmDegrees * raise) - (StrikeArmDegrees * strike), axis);
            Bend(2, _lowerArm, -RaiseElbowDegrees * raise, axis);
        }

        // Turns a bone about the beekeeper's right axis on top of what the Animator posed this frame. When the Animator
        // left the bone alone, the turn of the last frame is taken off first, so the turns never pile up.
        private void Bend(int slot, Transform bone, float degrees, Vector3 axis)
        {
            if (bone == null)
            {
                return;
            }

            Quaternion local = bone.localRotation;
            Quaternion baseLocal = Quaternion.Angle(local, _poseSet[slot]) < 0.01f ? _poseBase[slot] : local;
            _poseBase[slot] = baseLocal;
            if (Mathf.Abs(degrees) < 0.01f)
            {
                bone.localRotation = baseLocal;
            }
            else
            {
                Quaternion parent = bone.parent != null ? bone.parent.rotation : Quaternion.identity;
                bone.rotation = Quaternion.AngleAxis(degrees, axis) * (parent * baseLocal);
            }

            _poseSet[slot] = bone.localRotation;
        }

        private static Transform FindBone(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindBone(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        // Walks on the hills and climbs over the bridges, instead of the flat height the brain plans on.
        private static Vector3 OnGround(Vector3 position)
        {
            position.y = RiverBridge.GroundAt(position);
            return position;
        }

        private void ShowDeath(float deltaTime)
        {
            _deathElapsed += deltaTime;
            float t = Mathf.Clamp01(_deathElapsed / _deathSeconds);
            if (_liftHeight > 0f)
            {
                // One killed in the air falls back to the ground as it dies.
                ShowFlight(deltaTime);
                transform.position = OnGround(_brain.Position) + (Vector3.up * _liftHeight);
            }

            Quaternion fall = Quaternion.Euler(0f, 0f, 90f * t);
            transform.rotation = FacingRotation(_brain.Facing) * fall;
            transform.localScale = _startScale * (1f - t * t);

            if (t >= 1f)
            {
                DeathFinished?.Invoke(this);
            }
        }

        private void ShowFlash(float deltaTime)
        {
            float health = _brain.Health;
            if (health < _lastHealth)
            {
                _flashLeft = _flashSeconds;
                if (!_isFlashing)
                {
                    _isFlashing = true;
                    PaintDamageFlash();
                }
            }

            _lastHealth = health;
            if (!_isFlashing)
            {
                return;
            }

            // The white of a felt hit is a blink: while the health still drops, as under a sting, the red flash comes back.
            if (_hitFlashLeft > 0f)
            {
                _hitFlashLeft -= deltaTime;
                if (_hitFlashLeft <= 0f)
                {
                    PaintDamageFlash();
                }
            }

            _flashLeft -= deltaTime;
            if (_flashLeft <= 0f)
            {
                ClearFlash();
            }
        }

        private void PaintDamageFlash()
        {
            _block.SetColor(ColorId, _flashColor);
            _block.SetColor(EmissionColorId, Color.black);
            SetBlock(_block);
        }

        private void ClearFlash()
        {
            _flashLeft = 0f;
            _hitFlashLeft = 0f;
            if (!_isFlashing)
            {
                return;
            }

            _isFlashing = false;
            SetBlock(null);
        }

        private void SetBlock(MaterialPropertyBlock block)
        {
            for (int index = 0; index < _renderers.Length; index++)
            {
                _renderers[index].SetPropertyBlock(block);
            }
        }

        private void SetLean(float degrees)
        {
            if (_pivot != null)
            {
                _pivot.localRotation = Quaternion.Euler(degrees, 0f, 0f);
            }
        }

        private void SetSpeed(float speed)
        {
            if (_animator == null || Mathf.Approximately(speed, _lastSpeed))
            {
                return;
            }

            _lastSpeed = speed;
            _animator.SetFloat(SpeedId, speed);
        }

        private static Quaternion FacingRotation(Vector3 facing)
        {
            facing.y = 0f;
            return facing.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(facing, Vector3.up) : Quaternion.identity;
        }
    }
}
