using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>One beehive: it has health, can be damaged, and shows "under attack" for a short time after a hit.</summary>
    public sealed class Hive
    {
        private readonly HiveConfig _config;
        private float _health;
        private float _underAttackLeft;

        public Hive(HiveConfig config, Vector3 position)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            if (!(config.Health > 0f))
            {
                throw new ArgumentException("A hive needs a health above 0.", nameof(config));
            }

            Position = position;
            _health = config.Health;
        }

        /// <summary>Where the hive stands.</summary>
        public Vector3 Position { get; }

        /// <summary>Health left, from 0 to <see cref="MaxHealth"/>.</summary>
        public float Health => _health;

        /// <summary>Health at full strength.</summary>
        public float MaxHealth => _config.Health;

        /// <summary>True once health reached 0.</summary>
        public bool IsDestroyed => _health <= 0f;

        /// <summary>True for UnderAttackSeconds after the last damage.</summary>
        public bool IsUnderAttack => _underAttackLeft > 0f;

        /// <summary>Raised after every accepted hit. Raised before <see cref="Destroyed"/> on the fatal hit.</summary>
        public event Action<Hive> Damaged;

        /// <summary>Raised once, when health reaches 0.</summary>
        public event Action<Hive> Destroyed;

        /// <summary>Removes health. Ignored once destroyed, and for amounts of 0 or less.</summary>
        public void Damage(float amount)
        {
            if (IsDestroyed || !(amount > 0f))
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            _underAttackLeft = _config.UnderAttackSeconds;
            Damaged?.Invoke(this);
            if (IsDestroyed)
            {
                Destroyed?.Invoke(this);
            }
        }

        /// <summary>Counts the under-attack timer down.</summary>
        public void Tick(float deltaTime)
        {
            if (_underAttackLeft > 0f)
            {
                _underAttackLeft = Mathf.Max(0f, _underAttackLeft - deltaTime);
            }
        }
    }
}
