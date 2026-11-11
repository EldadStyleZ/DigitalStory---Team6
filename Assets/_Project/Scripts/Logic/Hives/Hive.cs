using System;
using DigitalStory.Data;
using UnityEngine;

namespace DigitalStory.Logic
{
    /// <summary>
    /// One beehive: it has health, can be damaged and healed, and shows "under attack" for a short time after a hit. While
    /// its health is at or above <see cref="HiveConfig.ProductionHealthFraction"/> it makes wasps into its own store, which
    /// it hands out to the swarm (plan section M). Its store is gone when it is destroyed.
    /// </summary>
    public sealed class Hive
    {
        private readonly HiveConfig _config;
        private float _health;
        private float _underAttackLeft;
        private float _store;

        /// <summary>Makes a hive at full health with <see cref="HiveConfig.StartStore"/> wasps in its store.</summary>
        public Hive(HiveConfig config, Vector3 position)
            : this(config, position, false)
        {
        }

        /// <summary>
        /// Makes a hive with <see cref="HiveConfig.StartStore"/> wasps in its store, at full health, or at
        /// <see cref="HiveConfig.DamagedStartFraction"/> of it when <paramref name="startsDamaged"/> is true.
        /// </summary>
        /// <exception cref="ArgumentException">The config has no health, no price per health point, or a start store above the capacity.</exception>
        public Hive(HiveConfig config, Vector3 position, bool startsDamaged)
        {
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            if (!(config.Health > 0f))
            {
                throw new ArgumentException("A hive needs a health above 0.", nameof(config));
            }

            if (!(config.WaspsPerHealthPoint > 0f))
            {
                throw new ArgumentException("HiveConfig.WaspsPerHealthPoint must be above 0.", nameof(config));
            }

            if (config.StartStore > config.StoreCapacity)
            {
                throw new ArgumentException("HiveConfig.StartStore must be at most StoreCapacity.", nameof(config));
            }

            Position = position;
            _health = startsDamaged ? config.Health * config.DamagedStartFraction : config.Health;
            _store = config.StartStore;
        }

        /// <summary>Where the hive stands.</summary>
        public Vector3 Position { get; }

        /// <summary>Health left, from 0 to <see cref="MaxHealth"/>.</summary>
        public float Health => _health;

        /// <summary>Health at full strength.</summary>
        public float MaxHealth => _config.Health;

        /// <summary>True once health reached 0.</summary>
        public bool IsDestroyed => _health <= 0f;

        /// <summary>True while the hive stands below full health, so giving wasps can heal it.</summary>
        public bool IsDamaged => !IsDestroyed && _health < _config.Health;

        /// <summary>True for UnderAttackSeconds after the last damage.</summary>
        public bool IsUnderAttack => _underAttackLeft > 0f;

        /// <summary>Wasps in the hive's store, from 0 to <see cref="StoreCapacity"/>. 0 once destroyed.</summary>
        public float Store => _store;

        /// <summary>Most wasps the store holds.</summary>
        public float StoreCapacity => _config.StoreCapacity;

        /// <summary>
        /// True while the hive stands and is healthy enough to make wasps: at or above ProductionHealthFraction of its health.
        /// A full store takes no more, but the hive still counts as producing.
        /// </summary>
        public bool IsProducing => !IsDestroyed && _health >= _config.Health * _config.ProductionHealthFraction;

        /// <summary>Raised after every accepted hit. Raised before <see cref="Destroyed"/> on the fatal hit.</summary>
        public event Action<Hive> Damaged;

        /// <summary>Raised once, when health reaches 0, with the store already empty.</summary>
        public event Action<Hive> Destroyed;

        /// <summary>Removes health. Ignored once destroyed, and for amounts of 0 or less. The fatal hit empties the store.</summary>
        public void Damage(float amount)
        {
            if (IsDestroyed || !(amount > 0f))
            {
                return;
            }

            _health = Mathf.Max(0f, _health - amount);
            _underAttackLeft = _config.UnderAttackSeconds;
            if (IsDestroyed)
            {
                _store = 0f;
            }

            Damaged?.Invoke(this);
            if (IsDestroyed)
            {
                Destroyed?.Invoke(this);
            }
        }

        /// <summary>Adds health, up to full, and returns how much it added. Nothing once destroyed, and for 0 or less.</summary>
        public float Heal(float health)
        {
            if (IsDestroyed || !(health > 0f))
            {
                return 0f;
            }

            float before = _health;
            _health = Mathf.Min(_config.Health, _health + health);
            return _health - before;
        }

        /// <summary>
        /// The store flows out at <see cref="HiveConfig.CollectPerSecond"/> for <paramref name="deltaTime"/>, but never more
        /// than <paramref name="most"/> wasps nor more than it holds. Returns how many wasps left it, parts of a wasp included.
        /// </summary>
        public float HandOut(float deltaTime, float most)
        {
            return TakeFromStore(Mathf.Min(_config.CollectPerSecond * deltaTime, most));
        }

        /// <summary>Takes up to <paramref name="wasps"/> from the store and returns how many it took. Nothing for 0 or less.</summary>
        public float TakeFromStore(float wasps)
        {
            if (!(wasps > 0f))
            {
                return 0f;
            }

            float taken = Mathf.Min(wasps, _store);
            _store -= taken;
            return taken;
        }

        /// <summary>Counts the under-attack timer down, and makes wasps into the store while the hive is producing.</summary>
        public void Tick(float deltaTime)
        {
            if (_underAttackLeft > 0f)
            {
                _underAttackLeft = Mathf.Max(0f, _underAttackLeft - deltaTime);
            }

            if (IsProducing && deltaTime > 0f)
            {
                _store = Mathf.Min(_config.StoreCapacity, _store + (_config.ProductionPerSecond * deltaTime));
            }
        }
    }
}
