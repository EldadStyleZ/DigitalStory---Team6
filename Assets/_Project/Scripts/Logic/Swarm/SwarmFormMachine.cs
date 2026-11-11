using System;
using System.Collections.Generic;
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
        private Vector3 _meteorTarget;
        private float _meteorFraction;
        private float _meteorRiseSeconds;
        private float _meteorDiveSeconds;
        private float _meteorHeight;
        private float _meteorRadius;
        private float _meteorDamage;
        private float _meteorStunSeconds;
        private IReadOnlyList<ISwarmTarget> _meteorEnemies;
        private bool _meteorHasLanded;
        private ISwarmTarget _meteorFollow;
        private Vector3 _meteorPiper;
        private float _meteorMaxRange;

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

        /// <summary>Where the meteors fall, or zero outside the meteor form.</summary>
        public Vector3 MeteorTarget => _meteorTarget;

        /// <summary>The share of the swarm that climbs and falls, or 0 outside the meteor form.</summary>
        public float MeteorFraction => _current == SwarmForm.Meteor ? _meteorFraction : 0f;

        /// <summary>Seconds the meteors climb, or 0 outside the meteor form.</summary>
        public float MeteorRiseSeconds => _meteorRiseSeconds;

        /// <summary>Seconds the meteors dive, or 0 outside the meteor form.</summary>
        public float MeteorDiveSeconds => _meteorDiveSeconds;

        /// <summary>Metres above the Piper the meteors climb to, or 0 outside the meteor form.</summary>
        public float MeteorHeight => _meteorHeight;

        /// <summary>Raised once, with the impact point, when the meteors land.</summary>
        public event Action<Vector3> MeteorLanded;

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

        /// <summary>
        /// Starts the meteor: it climbs, dives, lands, then flows back (riseSeconds + diveSeconds + afterSeconds in all).
        /// At the landing every living enemy within radius metres (on the ground) of the target takes damage and is
        /// stunned, and that damage fills Fury. Nothing happens before the landing, and a collapse cancels it.
        /// </summary>
        public bool StartMeteor(
            Vector3 target,
            float fraction,
            float riseSeconds,
            float diveSeconds,
            float afterSeconds,
            float height,
            IReadOnlyList<ISwarmTarget> enemies,
            float radius,
            float damage,
            float stunSeconds)
        {
            return StartMeteor(
                target, null, Vector3.zero, 0f, fraction, riseSeconds, diveSeconds, afterSeconds, height, enemies, radius, damage, stunSeconds);
        }

        /// <summary>
        /// Starts the meteor like the overload without a follow target, but the impact point keeps following follow
        /// (pulled in to maxRange metres from piperPosition along the ground) while the wasps climb. It locks where
        /// follow stands when the dive starts, or where it was last seen alive if it dies during the climb.
        /// A null follow fixes the point at target, as the other overload does.
        /// </summary>
        public bool StartMeteor(
            Vector3 target,
            ISwarmTarget follow,
            Vector3 piperPosition,
            float maxRange,
            float fraction,
            float riseSeconds,
            float diveSeconds,
            float afterSeconds,
            float height,
            IReadOnlyList<ISwarmTarget> enemies,
            float radius,
            float damage,
            float stunSeconds)
        {
            RequireSeconds(riseSeconds);
            RequireSeconds(diveSeconds);
            RequireSeconds(afterSeconds);

            if (fraction < 0f || fraction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "The fraction must be from 0 to 1.");
            }

            if (maxRange < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRange), maxRange, "The range cannot be negative.");
            }

            if (height < 0f || radius < 0f || damage < 0f || stunSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "The height, radius, damage and stun cannot be negative.");
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (!Begin(SwarmForm.Meteor, riseSeconds + diveSeconds + afterSeconds))
            {
                return false;
            }

            _meteorTarget = target;
            _meteorFraction = fraction;
            _meteorRiseSeconds = riseSeconds;
            _meteorDiveSeconds = diveSeconds;
            _meteorHeight = height;
            _meteorRadius = radius;
            _meteorDamage = damage;
            _meteorStunSeconds = stunSeconds;
            _meteorEnemies = enemies;
            _meteorHasLanded = false;
            _meteorFollow = follow;
            _meteorPiper = piperPosition;
            _meteorMaxRange = maxRange;
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
                case SwarmForm.Meteor:
                    TickMeteor();
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

        private void TickMeteor()
        {
            FollowMeteorTarget();

            if (!_meteorHasLanded && _secondsInForm >= _meteorRiseSeconds + _meteorDiveSeconds)
            {
                Land();
            }

            if (_secondsInForm >= _duration)
            {
                SetForm(SwarmForm.River);
            }
        }

        // The point trails the target while the wasps climb, and locks when the dive starts or the target dies.
        private void FollowMeteorTarget()
        {
            if (_meteorFollow == null)
            {
                return;
            }

            if (_meteorFollow.IsAlive)
            {
                _meteorTarget = PullIn(_meteorFollow.Position, _meteorPiper, _meteorMaxRange);
            }

            if (!_meteorFollow.IsAlive || _secondsInForm >= _meteorRiseSeconds)
            {
                _meteorFollow = null;
            }
        }

        /// <summary>The point, or its ground-plane pull-in to maxRange metres from the piper when it is further.</summary>
        public static Vector3 PullIn(Vector3 point, Vector3 piper, float maxRange)
        {
            float dx = point.x - piper.x;
            float dz = point.z - piper.z;
            float distanceSquared = dx * dx + dz * dz;

            if (distanceSquared <= maxRange * maxRange)
            {
                return point;
            }

            float scale = maxRange / Mathf.Sqrt(distanceSquared);
            return new Vector3(piper.x + dx * scale, point.y, piper.z + dz * scale);
        }

        private void Land()
        {
            _meteorHasLanded = true;
            float radiusSquared = _meteorRadius * _meteorRadius;

            for (int i = 0; i < _meteorEnemies.Count; i++)
            {
                ISwarmTarget enemy = _meteorEnemies[i];

                if (!enemy.IsAlive)
                {
                    continue;
                }

                float dx = enemy.Position.x - _meteorTarget.x;
                float dz = enemy.Position.z - _meteorTarget.z;

                if (dx * dx + dz * dz > radiusSquared)
                {
                    continue;
                }

                enemy.TakeDamage(_meteorDamage);

                if (enemy.IsAlive)
                {
                    enemy.Stun(_meteorStunSeconds);
                }

                _fury.AddDamage(_meteorDamage);
            }

            MeteorLanded?.Invoke(_meteorTarget);
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

            if (form != SwarmForm.Meteor)
            {
                _meteorTarget = Vector3.zero;
                _meteorFraction = 0f;
                _meteorRiseSeconds = 0f;
                _meteorDiveSeconds = 0f;
                _meteorHeight = 0f;
                _meteorEnemies = null;
                _meteorFollow = null;
                _meteorPiper = Vector3.zero;
                _meteorMaxRange = 0f;
                _meteorHasLanded = false;
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
