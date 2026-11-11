using System;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// Which shape the swarm is in and the state of that shape: the timers, the sting's target and
    /// the damage it deals. Songs start forms, and the body's collapse and respawn move it too.
    /// </summary>
    /// <remarks>
    /// The session ticks <see cref="SwarmBody"/> itself. While Hive Call lasts, this machine also
    /// calls <see cref="SwarmBody.Gain"/> and ends the call when the swarm is at its maximum.
    /// A Start method returns false, and changes nothing, while the swarm is collapsed.
    /// </summary>
    public sealed class SwarmFormMachine
    {
        private readonly SwarmBody _body;
        private readonly FuryMeter _fury;

        private SwarmForm _current = SwarmForm.River;
        private float _secondsInForm;
        private float _duration;
        private ISwarmTarget _stingTarget;
        private float _stingFraction;
        private float _stingDamagePerSecond;

        public SwarmFormMachine(SwarmBody body, FuryMeter fury)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _fury = fury ?? throw new ArgumentNullException(nameof(fury));
            _body.Collapsed += OnCollapsed;
            _body.Respawned += OnRespawned;
        }

        /// <summary>The form the swarm is in now.</summary>
        public SwarmForm Current => _current;

        /// <summary>Seconds since the current form began.</summary>
        public float SecondsInForm => _secondsInForm;

        /// <summary>True in the dome form, which cuts wasp losses.</summary>
        public bool IsDomed => _current == SwarmForm.Dome;

        /// <summary>True in Hive Call and the Colossus: the Piper stands still and plays.</summary>
        public bool IsPiperRooted => _current == SwarmForm.HiveCall || _current == SwarmForm.Colossus;

        /// <summary>The beekeeper the sting wraps around, or null outside the sting.</summary>
        public ISwarmTarget StingTarget => _stingTarget;

        /// <summary>The share of the swarm that streams to the sting target, or 0 outside the sting.</summary>
        public float StingFraction => _current == SwarmForm.Sting ? _stingFraction : 0f;

        /// <summary>Raised with the new form every time the form changes.</summary>
        public event Action<SwarmForm> FormChanged;

        /// <summary>Starts the dome for the given seconds.</summary>
        public bool StartDome(float seconds)
        {
            RequireSeconds(seconds);
            return Begin(SwarmForm.Dome, seconds);
        }

        /// <summary>
        /// Starts the sting. False when the target is null or dead. The target takes damagePerSecond
        /// while the sting lasts, and that damage fills Fury.
        /// </summary>
        public bool StartSting(ISwarmTarget target, float seconds, float fraction, float damagePerSecond)
        {
            RequireSeconds(seconds);

            if (fraction < 0f || fraction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "The fraction must be from 0 to 1.");
            }

            if (damagePerSecond < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damagePerSecond), damagePerSecond, "Damage per second cannot be negative.");
            }

            if (target == null || !target.IsAlive || !Begin(SwarmForm.Sting, seconds))
            {
                return false;
            }

            _stingTarget = target;
            _stingFraction = fraction;
            _stingDamagePerSecond = damagePerSecond;
            return true;
        }

        /// <summary>Starts the tornado. The damage it already dealt (the caster's total) fills Fury.</summary>
        public bool StartTornado(float seconds, float damageDealt)
        {
            RequireSeconds(seconds);

            if (damageDealt < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damageDealt), damageDealt, "Damage dealt cannot be negative.");
            }

            if (!Begin(SwarmForm.Tornado, seconds))
            {
                return false;
            }

            if (damageDealt > 0f)
            {
                _fury.AddDamage(damageDealt);
            }

            return true;
        }

        /// <summary>Starts the gust for the given seconds.</summary>
        public bool StartGust(float seconds)
        {
            RequireSeconds(seconds);
            return Begin(SwarmForm.Gust, seconds);
        }

        /// <summary>Starts Hive Call, which lasts until the Piper moves, is hit or the swarm is full.</summary>
        public bool StartHiveCall()
        {
            return Begin(SwarmForm.HiveCall, 0f);
        }

        /// <summary>Starts the Colossus for the given seconds.</summary>
        public bool StartColossus(float seconds)
        {
            RequireSeconds(seconds);
            return Begin(SwarmForm.Colossus, seconds);
        }

        /// <summary>Damage the Colossus dealt. It fills Fury only while the Colossus stands.</summary>
        public void AddColossusDamage(float damage)
        {
            if (_current == SwarmForm.Colossus && damage > 0f)
            {
                _fury.AddDamage(damage);
            }
        }

        /// <summary>The Piper was hit. Cancels Hive Call.</summary>
        public void NotifyHit()
        {
            CancelHiveCall();
        }

        /// <summary>The Piper moved. Cancels Hive Call.</summary>
        public void NotifyPiperMoved()
        {
            CancelHiveCall();
        }

        /// <summary>Advances the form's timer and its damage or gain over time.</summary>
        public void Tick(float deltaTime, int standingHives)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Time cannot run backwards.");
            }

            float before = _secondsInForm;
            _secondsInForm += deltaTime;

            switch (_current)
            {
                case SwarmForm.Sting:
                    TickSting(deltaTime, before);
                    break;
                case SwarmForm.HiveCall:
                    _body.Gain(deltaTime, standingHives);

                    if (_body.Count >= _body.Max)
                    {
                        SetForm(SwarmForm.River);
                    }

                    break;
                case SwarmForm.Dome:
                case SwarmForm.Tornado:
                case SwarmForm.Gust:
                case SwarmForm.Colossus:
                    if (_secondsInForm >= _duration)
                    {
                        SetForm(SwarmForm.River);
                    }

                    break;
            }
        }

        private static void RequireSeconds(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A form cannot last a negative time.");
            }
        }

        private void TickSting(float deltaTime, float secondsBefore)
        {
            if (_stingTarget == null || !_stingTarget.IsAlive)
            {
                SetForm(SwarmForm.River);
                return;
            }

            float timeLeft = Math.Max(0f, _duration - secondsBefore);
            float damage = _stingDamagePerSecond * Math.Min(deltaTime, timeLeft);

            if (damage > 0f)
            {
                _stingTarget.TakeDamage(damage);
                _fury.AddDamage(damage);
            }

            if (!_stingTarget.IsAlive || _secondsInForm >= _duration)
            {
                SetForm(SwarmForm.River);
            }
        }

        private void CancelHiveCall()
        {
            if (_current == SwarmForm.HiveCall)
            {
                SetForm(SwarmForm.River);
            }
        }

        private bool Begin(SwarmForm form, float seconds)
        {
            if (_current == SwarmForm.Collapsed || (_current == SwarmForm.Colossus && form != SwarmForm.Colossus))
            {
                return false;
            }

            _duration = seconds;
            SetForm(form);
            return true;
        }

        private void SetForm(SwarmForm form)
        {
            bool changed = form != _current;
            _current = form;
            _secondsInForm = 0f;

            if (form != SwarmForm.Sting)
            {
                _stingTarget = null;
                _stingFraction = 0f;
                _stingDamagePerSecond = 0f;
            }

            if (changed)
            {
                FormChanged?.Invoke(form);
            }
        }

        private void OnCollapsed()
        {
            SetForm(SwarmForm.Collapsed);
        }

        private void OnRespawned()
        {
            if (_current == SwarmForm.Collapsed)
            {
                SetForm(SwarmForm.River);
            }
        }
    }
}
