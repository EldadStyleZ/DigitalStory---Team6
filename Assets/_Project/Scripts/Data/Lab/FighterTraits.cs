using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// What makes a fighter more than its numbers. Both sides share these, so a new role is a new row of
    /// data. Zero or false leaves a trait out.
    /// </summary>
    [Serializable]
    public struct FighterTraits
    {
        [SerializeField, Min(0f), Tooltip("Health given back instead of hitting. A healer never hits.")]
        private float _healing;

        [SerializeField, Min(0f), Tooltip("Extra share of damage for each nearby fighter that also has this trait.")]
        private float _packBonus;

        [SerializeField, Tooltip("Runs past the front line to reach enemies that stand back.")]
        private bool _huntsBackline;

        [SerializeField, Tooltip("Stands behind its side, so hunters go for it.")]
        private bool _standsBack;

        [SerializeField, Range(0f, 1f), Tooltip("How much of its speed a hit takes from what it lands on.")]
        private float _slowShare;

        [SerializeField, Min(0f), Tooltip("How long that slow lasts, in seconds.")]
        private float _slowSeconds;

        [SerializeField, Min(0f), Tooltip("Enemies this many tiles away must fight it. They still need a passage to it.")]
        private float _guardTiles;

        [SerializeField, Range(0f, 1f), Tooltip("The share of each hit it ignores.")]
        private float _armour;

        public FighterTraits(
            float healing = 0f,
            float packBonus = 0f,
            bool huntsBackline = false,
            bool standsBack = false,
            float slowShare = 0f,
            float slowSeconds = 0f,
            float guardTiles = 0f,
            float armour = 0f)
        {
            _healing = healing;
            _packBonus = packBonus;
            _huntsBackline = huntsBackline;
            _standsBack = standsBack;
            _slowShare = slowShare;
            _slowSeconds = slowSeconds;
            _guardTiles = guardTiles;
            _armour = armour;
        }

        public float Healing => _healing;
        public bool Heals => _healing > 0f;
        public float PackBonus => _packBonus;
        public bool HuntsBackline => _huntsBackline;
        public bool StandsBack => _standsBack;
        public float SlowShare => _slowShare;
        public float SlowSeconds => _slowSeconds;
        public float GuardTiles => _guardTiles;
        public bool Guards => _guardTiles > 0f;
        public float Armour => _armour;
    }
}
