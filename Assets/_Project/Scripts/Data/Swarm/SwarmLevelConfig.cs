using System;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// What the swarm's level does (plan section H). The level is 1 plus the nests collected. Strength comes from the level,
    /// never from the live wasp count, because the count is also the player's health: a hurt swarm must not hit softer.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Swarm Level Config", fileName = "SwarmLevelConfig")]
    public sealed class SwarmLevelConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Share added to every song's radius, reach and height, and the giant's size, for each level above 1.")]
        public float SizePerLevel { get; private set; } = 0.12f;

        [field: SerializeField, Min(0f), Tooltip("Share added to every song's damage, and the giant's, for each level above 1.")]
        public float DamagePerLevel { get; private set; } = 0.12f;

        /// <summary>What sizes are multiplied by at <paramref name="level"/>: 1 + SizePerLevel x (level - 1).</summary>
        /// <exception cref="ArgumentOutOfRangeException">The level is below 1.</exception>
        public float SizeScale(int level)
        {
            return Scale(level, SizePerLevel);
        }

        /// <summary>What damage is multiplied by at <paramref name="level"/>: 1 + DamagePerLevel x (level - 1).</summary>
        /// <exception cref="ArgumentOutOfRangeException">The level is below 1.</exception>
        public float DamageScale(int level)
        {
            return Scale(level, DamagePerLevel);
        }

        private static float Scale(int level, float perLevel)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "The swarm's level starts at 1.");
            }

            return 1f + (perLevel * (level - 1));
        }
    }
}
