using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The waves of beekeepers and the pauses between them.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Wave Config", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        [SerializeField, Tooltip("The waves in order. Each says how many sticks and smokers it sends.")]
        private WaveEntry[] _waves =
        {
            new WaveEntry(3, 0),
            new WaveEntry(4, 1),
            new WaveEntry(4, 3),
            new WaveEntry(6, 4),
            new WaveEntry(8, 5)
        };

        /// <summary>The waves in order.</summary>
        public IReadOnlyList<WaveEntry> Waves => _waves;

        [field: SerializeField, Min(0f), Tooltip("Seconds before the first wave.")]
        public float FirstWaveDelaySeconds { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Seconds of rest after a wave is cleared.")]
        public float BreakSeconds { get; private set; } = 15f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds between two beekeepers of the same wave appearing.")]
        public float SpawnIntervalSeconds { get; private set; } = 1.2f;
    }
}
