using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How much experience each level needs.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Level Config", fileName = "LevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Total experience needed to reach each level, starting with level 1.")]
        private int[] _xpToReach = { 0, 150, 500, 1400, 3000 };

        public IReadOnlyList<int> XpToReach => _xpToReach;
    }
}
