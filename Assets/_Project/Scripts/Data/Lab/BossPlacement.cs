using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>A boss and how deep in the maze it waits.</summary>
    [Serializable]
    public sealed class BossPlacement
    {
        [SerializeField, Tooltip("The boss.")]
        private MinionType _boss;

        [SerializeField, Range(0f, 1f), Tooltip("How deep it waits: 0 is the entrance and 1 the furthest cell.")]
        private float _depth;

        public BossPlacement(MinionType boss, float depth)
        {
            _boss = boss;
            _depth = depth;
        }

        public MinionType Boss => _boss;
        public float Depth => _depth;
    }
}
