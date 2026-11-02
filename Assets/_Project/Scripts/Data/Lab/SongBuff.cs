using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// What a battle song does. Zero or false leaves an effect out, so a new song is a new row of data.
    /// The first group lasts for <see cref="Seconds"/>, and the second happens once, as it is played.
    /// </summary>
    [Serializable]
    public struct SongBuff
    {
        [SerializeField, Min(0f), Tooltip("How long the lasting effects last, in seconds.")]
        private float _seconds;

        [SerializeField, Min(0f), Tooltip("Multiplies the damage of every creature. Zero leaves it alone.")]
        private float _damageMultiplier;

        [SerializeField, Range(0f, 1f), Tooltip("The share of each hit every creature ignores.")]
        private float _shieldShare;

        [SerializeField, Min(0f), Tooltip("Multiplies how fast every creature moves and hits. Zero leaves it alone.")]
        private float _paceMultiplier;

        [SerializeField, Min(0f), Tooltip("Multiplies creature damage to a target that is nearly finished. Zero leaves it alone.")]
        private float _finisherMultiplier;

        [SerializeField, Tooltip("Enemy healers stop healing.")]
        private bool _stopsEnemyHealing;

        [SerializeField, Min(0f), Tooltip("Health given at once to every creature and the Piper.")]
        private float _healing;

        [SerializeField, Range(0f, 1f), Tooltip("How much of its speed every awake enemy loses at once.")]
        private float _enemySlowShare;

        [SerializeField, Min(0f), Tooltip("How long that slow lasts, in seconds.")]
        private float _enemySlowSeconds;

        [SerializeField, Min(0f), Tooltip("Damage dealt at once to every awake enemy.")]
        private float _burstDamage;

        public SongBuff(
            float seconds = 0f,
            float damageMultiplier = 0f,
            float shieldShare = 0f,
            float paceMultiplier = 0f,
            float finisherMultiplier = 0f,
            bool stopsEnemyHealing = false,
            float healing = 0f,
            float enemySlowShare = 0f,
            float enemySlowSeconds = 0f,
            float burstDamage = 0f)
        {
            _seconds = seconds;
            _damageMultiplier = damageMultiplier;
            _shieldShare = shieldShare;
            _paceMultiplier = paceMultiplier;
            _finisherMultiplier = finisherMultiplier;
            _stopsEnemyHealing = stopsEnemyHealing;
            _healing = healing;
            _enemySlowShare = enemySlowShare;
            _enemySlowSeconds = enemySlowSeconds;
            _burstDamage = burstDamage;
        }

        public float Seconds => _seconds;
        public bool Lasts => _seconds > 0f;
        public float DamageMultiplier => _damageMultiplier;
        public float ShieldShare => _shieldShare;
        public float PaceMultiplier => _paceMultiplier;
        public float FinisherMultiplier => _finisherMultiplier;
        public bool StopsEnemyHealing => _stopsEnemyHealing;
        public float Healing => _healing;
        public float EnemySlowShare => _enemySlowShare;
        public float EnemySlowSeconds => _enemySlowSeconds;
        public float BurstDamage => _burstDamage;
    }
}
