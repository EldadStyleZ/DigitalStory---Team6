using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>What a creature or minion can do in a fight.</summary>
    [Serializable]
    public struct FighterStats
    {
        [SerializeField, Min(1f), Tooltip("Health when it arrives.")]
        private float _maxHealth;

        [SerializeField, Min(0f), Tooltip("Walking speed, in tiles per second.")]
        private float _moveSpeed;

        [SerializeField, Min(0f), Tooltip("Damage of one hit.")]
        private float _damage;

        [SerializeField, Min(0.05f), Tooltip("Seconds between hits.")]
        private float _secondsBetweenHits;

        [SerializeField, Min(0f), Tooltip("How far a hit reaches, in tiles.")]
        private float _reach;

        [SerializeField, Min(0f), Tooltip("Tiles around the target that a hit also covers. Zero hits only the target.")]
        private float _areaRadius;

        public FighterStats(float maxHealth, float moveSpeed, float damage, float secondsBetweenHits, float reach, float areaRadius = 0f)
        {
            _maxHealth = maxHealth;
            _moveSpeed = moveSpeed;
            _damage = damage;
            _secondsBetweenHits = secondsBetweenHits;
            _reach = reach;
            _areaRadius = areaRadius;
        }

        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float Damage => _damage;
        public float SecondsBetweenHits => _secondsBetweenHits;
        public float Reach => _reach;
        public float AreaRadius => _areaRadius;
        public bool HitsAnArea => _areaRadius > 0f;
    }
}
