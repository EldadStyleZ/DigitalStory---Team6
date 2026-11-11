using System;
using DigitalStory.Data;

namespace DigitalStory.Logic
{
    /// <summary>The Colossus's attack timing: the three-punch chain, the heavy third punch and the kick cooldown.</summary>
    public sealed class ColossusAttackChain
    {
        private const int PunchesBeforeHeavy = 2;

        private readonly ColossusConfig _config;

        private ColossusAttack _current;
        private float _hitTimer;
        private int _punchesInChain;
        private float _window;
        private float _kickCooldown;

        public ColossusAttackChain(ColossusConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        /// <summary>True from the moment an attack starts until it lands.</summary>
        public bool IsBusy => _current != ColossusAttack.None;

        /// <summary>Raised when an attack starts, before it lands.</summary>
        public event Action<ColossusAttack> Started;

        /// <summary>Raised when an attack's hit delay has passed.</summary>
        public event Action<ColossusAttack> Landed;

        /// <summary>Starts a punch. The third one inside the chain window is a heavy punch, then the chain restarts. False while busy.</summary>
        public bool TryPunch()
        {
            if (IsBusy)
            {
                return false;
            }

            if (_punchesInChain >= PunchesBeforeHeavy)
            {
                _punchesInChain = 0;
                Begin(ColossusAttack.HeavyPunch, _config.HeavyPunchHitDelay);
            }
            else
            {
                _punchesInChain++;
                Begin(ColossusAttack.Punch, _config.PunchHitDelay);
            }

            return true;
        }

        /// <summary>Starts a kick. False while busy or cooling down.</summary>
        public bool TryKick()
        {
            if (IsBusy || _kickCooldown > 0f)
            {
                return false;
            }

            _punchesInChain = 0;
            _window = 0f;
            _kickCooldown = _config.KickCooldownSeconds;
            Begin(ColossusAttack.Kick, _config.KickHitDelay);
            return true;
        }

        /// <summary>Advances the hit delay, the chain window and the kick cooldown.</summary>
        public void Tick(float deltaTime)
        {
            if (!(deltaTime > 0f))
            {
                return;
            }

            if (_kickCooldown > 0f)
            {
                _kickCooldown -= deltaTime;
            }

            if (IsBusy)
            {
                _hitTimer -= deltaTime;
                if (_hitTimer > 0f)
                {
                    return;
                }

                deltaTime = -_hitTimer;
                Land();
            }

            if (_punchesInChain > 0 && !IsBusy)
            {
                _window -= deltaTime;
                if (_window <= 0f)
                {
                    _punchesInChain = 0;
                    _window = 0f;
                }
            }
        }

        private void Begin(ColossusAttack attack, float hitDelay)
        {
            _current = attack;
            _hitTimer = hitDelay;
            Started?.Invoke(attack);
        }

        private void Land()
        {
            ColossusAttack landed = _current;
            _current = ColossusAttack.None;
            _hitTimer = 0f;
            _window = landed == ColossusAttack.Punch ? _config.ChainWindowSeconds : 0f;
            Landed?.Invoke(landed);
        }
    }
}
