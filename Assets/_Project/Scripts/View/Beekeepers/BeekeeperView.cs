using System;
using DigitalStory.Data;
using DigitalStory.Logic;
using UnityEngine;

namespace DigitalStory.View
{
    /// <summary>Shows one beekeeper: follows its brain, walks, leans forward as a warning while it winds up, swings, flashes when hit and falls over when it dies.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("DigitalStory/Beekeeper View")]
    public sealed class BeekeeperView : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

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

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private BeekeeperBrain _brain;
        private Vector3 _startScale;
        private float _lastHealth;
        private float _lastProgress;
        private float _swingLeft;
        private float _flashLeft;
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

        private void Awake()
        {
            _startScale = transform.localScale;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
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

            transform.localScale = _startScale;
            transform.SetPositionAndRotation(brain.Position, FacingRotation(brain.Facing));
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
            SetLean(0f);
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

            transform.position = _brain.Position;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, FacingRotation(_brain.Facing), _turnDegreesPerSecond * deltaTime);
            SetSpeed(_brain.IsMoving ? 1f : 0f);
            ShowLean(deltaTime);
        }

        private void OnDisable()
        {
            if (_block != null && _renderers != null)
            {
                ClearFlash();
            }
        }

        private void ShowLean(float deltaTime)
        {
            float progress = _brain.WindUpProgress;
            if (progress > 0f)
            {
                _swingLeft = 0f;
                SetLean(_maxLeanDegrees * progress);
            }
            else if (_lastProgress > 0f)
            {
                _swingLeft = _swingSeconds;
            }

            _lastProgress = progress;
            if (progress > 0f || _swingLeft <= 0f)
            {
                return;
            }

            // Snap forward past the lean, then settle upright.
            _swingLeft = Mathf.Max(0f, _swingLeft - deltaTime);
            float settle = _swingLeft / _swingSeconds;
            SetLean(_swingDegrees * settle * settle);
        }

        private void ShowDeath(float deltaTime)
        {
            _deathElapsed += deltaTime;
            float t = Mathf.Clamp01(_deathElapsed / _deathSeconds);
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
                    _block.SetColor(ColorId, _flashColor);
                    SetBlock(_block);
                }
            }

            _lastHealth = health;
            if (!_isFlashing)
            {
                return;
            }

            _flashLeft -= deltaTime;
            if (_flashLeft <= 0f)
            {
                ClearFlash();
            }
        }

        private void ClearFlash()
        {
            _flashLeft = 0f;
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
