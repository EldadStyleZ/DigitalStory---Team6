using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The rounds of a match (plan section L): each is a farm phase and then a wave of beekeepers.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Wave Config", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        [SerializeField, Tooltip("The waves in order, one a round. Each says how many sticks and smokers it sends.")]
        private WaveEntry[] _waves =
        {
            new WaveEntry(3, 0),
            new WaveEntry(4, 1),
            new WaveEntry(4, 3),
            new WaveEntry(6, 4),
            new WaveEntry(8, 5)
        };

        /// <summary>The waves in order, one a round.</summary>
        public IReadOnlyList<WaveEntry> Waves => _waves;

        [field: SerializeField, Min(0f), Tooltip("Seconds of the first round's farm phase, before the first wave. Longer than the rest, for the lesson.")]
        public float FirstFarmSeconds { get; private set; } = 45f;

        [field: SerializeField, Min(0f), Tooltip("Seconds of every later farm phase, which starts when a wave is cleared.")]
        public float FarmSeconds { get; private set; } = 35f;

        [field: SerializeField, Min(0f), Tooltip("Wasps the swarm gets for each second of a farm phase skipped by calling the wave early, up to its capacity.")]
        public float EarlyBonusPerSecond { get; private set; } = 5f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds between two beekeepers of the same wave appearing.")]
        public float SpawnIntervalSeconds { get; private set; } = 1.2f;
    }
}
