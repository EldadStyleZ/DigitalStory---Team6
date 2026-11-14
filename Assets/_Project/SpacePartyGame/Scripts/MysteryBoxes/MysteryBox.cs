using System;
using UnityEngine;

namespace SpacePartyGame
{
    /// <summary>
    /// One mystery box. While out it spins around the world's up axis and bobs; after its idle time it flashes, then
    /// says it has expired. A player walking onto it collects it. The pool despawns it either way. Its clock stops
    /// while the game is paused.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class MysteryBox : MonoBehaviour
    {
        [SerializeField, Tooltip("The child that spins and bobs.")]
        private Transform _visual;

        [SerializeField, Tooltip("The box's picture, on the visual child.")]
        private SpriteRenderer _sprite;

        private MysteryBoxSettings _settings;
        private GameFlowManager _flow;
        private MysteryBoxLife _life;
        private Vector3 _visualHome;
        private float _spin;
        private float _time;

        /// <summary>Raised when a player walks onto it, with that player.</summary>
        public event Action<MysteryBox, PlayerIdentity> Collected;

        /// <summary>Raised when its time is up.</summary>
        public event Action<MysteryBox> Expired;

        /// <summary>Gives the box its settings and the game's stages. The pool calls it once for every box it makes.</summary>
        public void Initialize(MysteryBoxSettings settings, GameFlowManager flow)
        {
            _settings = settings;
            _flow = flow;

            if (settings != null)
            {
                _life = new MysteryBoxLife(settings.IdleSeconds, settings.FlashSeconds, settings.FlashesPerSecond);
            }
        }

        private void Awake()
        {
            if (_visual == null || _sprite == null)
            {
                Debug.LogError($"{name}: MysteryBox is missing its visual child or sprite.", this);
                return;
            }

            _visualHome = _visual.localPosition;
        }

        private void OnEnable()
        {
            _life?.Restart();
            _spin = 0f;
            _time = 0f;

            if (_sprite != null)
            {
                _sprite.enabled = true;
            }
        }

        private void Update()
        {
            if (_life == null || _visual == null || (_flow != null && _flow.State == GameState.Paused))
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            _life.Tick(deltaTime);

            if (_life.IsOver)
            {
                Expired?.Invoke(this);
                return;
            }

            _time += deltaTime;
            _spin = Mathf.Repeat(_spin + (_settings.SpinDegreesPerSecond * deltaTime), 360f);
            _visual.rotation = Quaternion.Euler(0f, _spin, 0f);
            _visual.localPosition = _visualHome + (Vector3.up * BoxSpin.Bob(_time, _settings.BobHeight, _settings.BobSeconds));
            _sprite.flipX = BoxSpin.ShowsBack(_spin);
            _sprite.enabled = _life.IsVisible;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            PlayerIdentity player = body != null ? body.GetComponent<PlayerIdentity>() : other.GetComponent<PlayerIdentity>();

            if (player != null && isActiveAndEnabled)
            {
                Collected?.Invoke(this, player);
            }
        }
    }
}
