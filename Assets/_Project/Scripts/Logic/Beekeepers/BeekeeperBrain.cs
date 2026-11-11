using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One beekeeper's decisions, for both the stick and the smoker kind. It moves itself and raises events; the session applies them.</summary>
    public sealed class BeekeeperBrain : ISwarmTarget
    {
        // How fast a shove fades, per second. It is a feel value of the physics, not a game rule.
        private const float KnockbackDecayPerSecond = 6f;
        private const float KnockbackStopSqrSpeed = 0.0001f;
        private const float DirectionEpsilonSqr = 1e-8f;
        private const int NoHive = -1;

        private readonly BeekeeperConfig _config;

        // A pooled beekeeper is off the field, and so out of every attack's reach, until it spawns.
        private BeekeeperState _state = BeekeeperState.Dead;
        private Vector3 _position;
        private Vector3 _facing = Vector3.forward;
        private Vector3 _knockback;
        private float _health;
        private float _stunLeft;
        private float _cooldownLeft;
        private float _windUpElapsed;
        private int _windUpHive = NoHive;
        private bool _isTargetingHive;
        private bool _hasAggro;
        private bool _isMoving;

        public BeekeeperBrain(BeekeeperConfig config)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _health = config.Health;
        }

        /// <summary>The kind and numbers this beekeeper follows.</summary>
        public BeekeeperConfig Config => _config;

        /// <summary>What it is doing right now.</summary>
        public BeekeeperState State => _state;

        /// <summary>Where it stands, in metres.</summary>
        public Vector3 Position => _position;

        /// <summary>A unit vector on the ground.</summary>
        public Vector3 Facing => _facing;

        /// <summary>Health left, from 0 to the configured health.</summary>
        public float Health => _health;

        /// <summary>False until it spawns, and once it is dead.</summary>
        public bool IsAlive => _state != BeekeeperState.Dead;

        /// <summary>
        /// True while a swing, or the recovery after it, is aimed at a hive; false when it is aimed at the core.
        /// A hive attack and a core attack share the WindUp and Recover states.
        /// </summary>
        public bool IsTargetingHive => _isTargetingHive;

        /// <summary>True when it walked during the last tick.</summary>
        public bool IsMoving => _isMoving;

        /// <summary>0 to 1 while winding up, otherwise 0.</summary>
        public float WindUpProgress
        {
            get
            {
                if (_state != BeekeeperState.WindUp)
                {
                    return 0f;
                }

                return _config.WindUpSeconds > 0f ? Mathf.Clamp01(_windUpElapsed / _config.WindUpSeconds) : 1f;
            }
        }

        /// <summary>Stick: the hive index it hit.</summary>
        public event Action<BeekeeperBrain, int> HiveHit;

        /// <summary>Stick: it hit the core.</summary>
        public event Action<BeekeeperBrain> CoreHit;

        /// <summary>Smoker: where the grenade will land.</summary>
        public event Action<BeekeeperBrain, Vector3> GrenadeThrown;

        /// <summary>Raised once, when health reaches 0.</summary>
        public event Action<BeekeeperBrain> Died;

        /// <summary>Resets everything, for pooling.</summary>
        public void Spawn(Vector3 position)
        {
            _position = position;
            _facing = Vector3.forward;
            _knockback = Vector3.zero;
            _health = _config.Health;
            _state = BeekeeperState.GoToHive;
            _stunLeft = 0f;
            _cooldownLeft = 0f;
            _windUpElapsed = 0f;
            _windUpHive = NoHive;
            _hasAggro = false;
            _isMoving = false;
        }

        /// <summary>Thinks and moves for one step.</summary>
        public void Tick(in BeekeeperSenses senses, float deltaTime)
        {
            if (senses.Hives == null)
            {
                throw new ArgumentException("The senses need a hive set.", nameof(senses));
            }

            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "The step cannot be negative.");
            }

            _isMoving = false;
            if (_state == BeekeeperState.Dead)
            {
                return;
            }

            if (_cooldownLeft > 0f)
            {
                _cooldownLeft -= deltaTime;
            }

            ApplyKnockback(deltaTime);

            if (_state == BeekeeperState.Stunned)
            {
                _stunLeft -= deltaTime;
                if (_stunLeft > 0f)
                {
                    return;
                }

                _stunLeft = 0f;
                _state = _cooldownLeft > 0f ? BeekeeperState.Recover : BeekeeperState.GoToHive;
            }

            TickAlive(in senses, deltaTime);
            ApplySeparation(senses.Separation, deltaTime);
        }

        /// <summary>Takes away health. Any damage makes it chase the core.</summary>
        public void TakeDamage(float amount)
        {
            if (_state == BeekeeperState.Dead || amount <= 0f)
            {
                return;
            }

            _hasAggro = true;
            _health = Mathf.Max(0f, _health - amount);
            if (_health > 0f)
            {
                return;
            }

            _state = BeekeeperState.Dead;
            _isMoving = false;
            _windUpElapsed = 0f;
            _windUpHive = NoHive;
            _knockback = Vector3.zero;
            Died?.Invoke(this);
        }

        /// <summary>Shoves it, in metres per second. The push decays quickly.</summary>
        public void Knockback(Vector3 velocity)
        {
            if (_state == BeekeeperState.Dead)
            {
                return;
            }

            _knockback += new Vector3(velocity.x, 0f, velocity.z);
        }

        /// <summary>Freezes it for the given seconds and cancels a wind-up. A shorter stun never cuts a longer one.</summary>
        public void Stun(float seconds)
        {
            if (_state == BeekeeperState.Dead || seconds <= 0f)
            {
                return;
            }

            _stunLeft = Mathf.Max(_stunLeft, seconds);
            _state = BeekeeperState.Stunned;
            _isMoving = false;
            _windUpElapsed = 0f;
            _windUpHive = NoHive;
        }

        private void TickAlive(in BeekeeperSenses senses, float deltaTime)
        {
            if (_state == BeekeeperState.Recover)
            {
                if (_cooldownLeft > 0f)
                {
                    return;
                }

                _state = BeekeeperState.GoToHive;
            }

            if (_state == BeekeeperState.WindUp && TickWindUp(in senses, deltaTime))
            {
                return;
            }

            Decide(in senses, deltaTime);
        }

        // Returns true when the wind-up is still the whole story of this tick (running or just landed).
        // Returns false when it was cancelled and the beekeeper must choose again.
        private bool TickWindUp(in BeekeeperSenses senses, float deltaTime)
        {
            if (_windUpHive != NoHive && senses.Hives[_windUpHive].IsDestroyed)
            {
                _state = BeekeeperState.GoToHive;
                _windUpElapsed = 0f;
                _windUpHive = NoHive;
                return false;
            }

            _windUpElapsed += deltaTime;
            if (_windUpElapsed >= _config.WindUpSeconds)
            {
                Land(in senses);
            }

            return true;
        }

        private void Land(in BeekeeperSenses senses)
        {
            int hive = _windUpHive;
            _windUpElapsed = 0f;
            _windUpHive = NoHive;
            _state = BeekeeperState.Recover;
            _cooldownLeft = _config.CooldownSeconds;
            bool isSmoker = _config.Kind == BeekeeperKind.Smoker;

            if (hive != NoHive)
            {
                if (isSmoker)
                {
                    GrenadeThrown?.Invoke(this, senses.Hives[hive].Position);
                }
                else
                {
                    HiveHit?.Invoke(this, hive);
                }

                return;
            }

            if (isSmoker)
            {
                GrenadeThrown?.Invoke(this, senses.CorePosition);
                return;
            }

            // The stick only lands if the core is still within reach, so stepping away from a swing dodges it.
            if (FlatSqrDistance(_position, senses.CorePosition) <= _config.AttackRange * _config.AttackRange)
            {
                CoreHit?.Invoke(this);
            }
        }

        private void Decide(in BeekeeperSenses senses, float deltaTime)
        {
            float coreSqr = FlatSqrDistance(_position, senses.CorePosition);
            if (coreSqr > _config.GiveUpRange * _config.GiveUpRange)
            {
                _hasAggro = false;
            }
            else if (coreSqr <= _config.NoticeRange * _config.NoticeRange)
            {
                _hasAggro = true;
            }

            float attackSqr = _config.AttackRange * _config.AttackRange;
            if (_hasAggro)
            {
                _state = BeekeeperState.ChaseCore;
                if (coreSqr <= attackSqr)
                {
                    StartWindUp(senses.CorePosition, NoHive);
                }
                else
                {
                    WalkToward(senses.CorePosition, deltaTime);
                }

                return;
            }

            _state = BeekeeperState.GoToHive;
            if (!senses.Hives.TryGetNearestStanding(_position, out int hive))
            {
                return;
            }

            Vector3 hivePosition = senses.Hives[hive].Position;
            if (FlatSqrDistance(_position, hivePosition) <= attackSqr)
            {
                StartWindUp(hivePosition, hive);
            }
            else
            {
                WalkToward(hivePosition, deltaTime);
            }
        }

        private void StartWindUp(Vector3 target, int hive)
        {
            _state = BeekeeperState.WindUp;
            _windUpElapsed = 0f;
            _windUpHive = hive;
            _isTargetingHive = hive != NoHive;
            Face(target);
        }

        private void WalkToward(Vector3 target, float deltaTime)
        {
            float dx = target.x - _position.x;
            float dz = target.z - _position.z;
            float sqr = dx * dx + dz * dz;
            if (sqr <= DirectionEpsilonSqr)
            {
                return;
            }

            float distance = Mathf.Sqrt(sqr);
            float step = Mathf.Min(_config.Speed * deltaTime, distance);
            float scale = step / distance;
            _position.x += dx * scale;
            _position.z += dz * scale;
            _facing = new Vector3(dx / distance, 0f, dz / distance);
            _isMoving = true;
        }

        private void Face(Vector3 target)
        {
            float dx = target.x - _position.x;
            float dz = target.z - _position.z;
            float sqr = dx * dx + dz * dz;
            if (sqr <= DirectionEpsilonSqr)
            {
                return;
            }

            float length = Mathf.Sqrt(sqr);
            _facing = new Vector3(dx / length, 0f, dz / length);
        }

        private void ApplyKnockback(float deltaTime)
        {
            if (_knockback.sqrMagnitude <= 0f)
            {
                return;
            }

            _position += _knockback * deltaTime;
            _knockback *= Mathf.Exp(-KnockbackDecayPerSecond * deltaTime);
            if (_knockback.sqrMagnitude < KnockbackStopSqrSpeed)
            {
                _knockback = Vector3.zero;
            }
        }

        private void ApplySeparation(Vector3 separation, float deltaTime)
        {
            float x = separation.x;
            float z = separation.z;
            float sqr = x * x + z * z;
            if (sqr <= 0f)
            {
                return;
            }

            if (sqr > 1f)
            {
                float length = Mathf.Sqrt(sqr);
                x /= length;
                z /= length;
            }

            float distance = _config.Speed * deltaTime;
            _position.x += x * distance;
            _position.z += z * distance;
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
