using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// The Colossus's move timing: the three-punch chain with its heavy third punch, the kick, the ground slam and the roar
    /// with their cooldowns, and the two phrases of plan section I. A second kick within
    /// <see cref="ColossusConfig.PhraseWindowSeconds"/> of a kick is a spin kick, and a second slam within it of a slam is a
    /// leap slam, even while the first cools down. A phrase pressed while its first move is still in the air waits, and
    /// starts the moment that move lands, so a quick K K is not lost. Any other move in between breaks the phrase.
    /// </summary>
    public sealed class ColossusAttackChain
    {
        private const int PunchesBeforeHeavy = 2;

        private readonly ColossusConfig _config;

        private ColossusAttack _current;
        private ColossusAttack _waiting;
        private float _hitTimer;
        private int _punchesInChain;
        private float _window;
        private float _kickCooldown;
        private float _slamCooldown;
        private float _roarCooldown;
        private ColossusAttack _phrase;
        private float _phraseLeft;

        public ColossusAttackChain(ColossusConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        /// <summary>True from the moment a move starts until it lands, and while a phrase waits for the move before it.</summary>
        public bool IsBusy => _current != ColossusAttack.None;

        /// <summary>How much of the slam's cooldown is left, for the HUD: 1 just used, 0 ready.</summary>
        public float SlamCooldown01 => Share(_slamCooldown, _config.SlamCooldownSeconds);

        /// <summary>How much of the roar's cooldown is left, for the HUD: 1 just used, 0 ready.</summary>
        public float RoarCooldown01 => Share(_roarCooldown, _config.RoarCooldownSeconds);

        /// <summary>Raised when a move starts, before it lands.</summary>
        public event Action<ColossusAttack> Started;

        /// <summary>Raised when a move's hit delay has passed: for the leap slam, when the giant lands.</summary>
        public event Action<ColossusAttack> Landed;

        /// <summary>Starts a punch. The third one inside the chain window is a heavy punch, then the chain restarts. False while busy.</summary>
        public bool TryPunch()
        {
            if (IsBusy)
            {
                return false;
            }

            EndPhrase();
            if (_punchesInChain >= PunchesBeforeHeavy)
            {
                _punchesInChain = 0;
                Begin(ColossusAttack.HeavyPunch);
            }
            else
            {
                _punchesInChain++;
                Begin(ColossusAttack.Punch);
            }

            return true;
        }

        /// <summary>
        /// Starts a kick, or, within the phrase window of a kick, a spin kick. False while busy or cooling down, unless it is
        /// the spin kick, which waits for the kick to land when it is still in the air.
        /// </summary>
        public bool TryKick()
        {
            if (TryPhrase(ColossusAttack.Kick, ColossusAttack.SpinKick))
            {
                return true;
            }

            if (IsBusy || _kickCooldown > 0f)
            {
                return false;
            }

            _kickCooldown = _config.KickCooldownSeconds;
            StartPhrase(ColossusAttack.Kick);
            Begin(ColossusAttack.Kick);
            return true;
        }

        /// <summary>
        /// Starts a ground slam, or, within the phrase window of a slam, a leap slam. False while busy or cooling down, unless
        /// it is the leap slam, which waits for the slam to land when it is still winding up.
        /// </summary>
        public bool TrySlam()
        {
            if (TryPhrase(ColossusAttack.Slam, ColossusAttack.LeapSlam))
            {
                return true;
            }

            if (IsBusy || _slamCooldown > 0f)
            {
                return false;
            }

            _slamCooldown = _config.SlamCooldownSeconds;
            StartPhrase(ColossusAttack.Slam);
            Begin(ColossusAttack.Slam);
            return true;
        }

        /// <summary>Starts a roar. False while busy or cooling down.</summary>
        public bool TryRoar()
        {
            if (IsBusy || _roarCooldown > 0f)
            {
                return false;
            }

            _roarCooldown = _config.RoarCooldownSeconds;
            EndPhrase();
            Begin(ColossusAttack.Roar);
            return true;
        }

        /// <summary>
        /// Forgets everything: the move under way, which never lands, a waiting phrase, the chain and every cooldown. The
        /// session calls it when a giant forms and when it ends, so nothing of one giant reaches the next or outlives it.
        /// </summary>
        public void Reset()
        {
            _current = ColossusAttack.None;
            _waiting = ColossusAttack.None;
            _hitTimer = 0f;
            _punchesInChain = 0;
            _window = 0f;
            _kickCooldown = 0f;
            _slamCooldown = 0f;
            _roarCooldown = 0f;
            EndPhrase();
        }

        /// <summary>Advances the hit delay, the chain window, the phrase window and the cooldowns.</summary>
        public void Tick(float deltaTime)
        {
            if (!(deltaTime > 0f))
            {
                return;
            }

            _kickCooldown = CoolDown(_kickCooldown, deltaTime);
            _slamCooldown = CoolDown(_slamCooldown, deltaTime);
            _roarCooldown = CoolDown(_roarCooldown, deltaTime);
            if (_phraseLeft > 0f)
            {
                _phraseLeft -= deltaTime;
                if (_phraseLeft <= 0f)
                {
                    EndPhrase();
                }
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
                if (_waiting != ColossusAttack.None)
                {
                    ColossusAttack next = _waiting;
                    _waiting = ColossusAttack.None;
                    Begin(next);
                    return;
                }
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

        private static float CoolDown(float seconds, float deltaTime)
        {
            return seconds > 0f ? Mathf.Max(0f, seconds - deltaTime) : 0f;
        }

        private static float Share(float left, float whole)
        {
            return whole > 0f ? Mathf.Clamp01(left / whole) : 0f;
        }

        // The second press of a phrase: its move starts now, or as the first move lands when that is still in the air. The
        // phrase is used up either way, so a third press is no second phrase.
        private bool TryPhrase(ColossusAttack first, ColossusAttack phrase)
        {
            if (_phrase != first || !(_phraseLeft > 0f) || _waiting != ColossusAttack.None)
            {
                return false;
            }

            EndPhrase();
            if (IsBusy)
            {
                _waiting = phrase;
            }
            else
            {
                Begin(phrase);
            }

            return true;
        }

        private void StartPhrase(ColossusAttack first)
        {
            _phrase = first;
            _phraseLeft = _config.PhraseWindowSeconds;
        }

        private void EndPhrase()
        {
            _phrase = ColossusAttack.None;
            _phraseLeft = 0f;
        }

        private float HitDelay(ColossusAttack attack)
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
                    throw new ArgumentOutOfRangeException(nameof(attack), attack, "Not a move the Colossus can start.");
            }
        }

        // Every move but a punch breaks the punch chain.
        private void Begin(ColossusAttack attack)
        {
            if (attack != ColossusAttack.Punch && attack != ColossusAttack.HeavyPunch)
            {
                _punchesInChain = 0;
                _window = 0f;
            }

            _current = attack;
            _hitTimer = HitDelay(attack);
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
