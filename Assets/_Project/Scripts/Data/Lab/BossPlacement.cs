using System;
using System.Collections.Generic;
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

        [SerializeField, Tooltip("The name of each guard beside it. Each must match a minion in the population.")]
        private string[] _guards;

        public BossPlacement(MinionType boss, float depth, params string[] guards)
        {
            _boss = boss;
            _depth = depth;
            _guards = guards;
        }

        public MinionType Boss => _boss;
        public float Depth => _depth;
        public IReadOnlyList<string> Guards => _guards ?? Array.Empty<string>();
    }
}
