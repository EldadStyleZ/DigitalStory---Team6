using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The rounds of a match (plan section L): each is a farm phase and then a wave of beekeepers.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Wave Config", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        // Set by the playtest bot (EconomyPlaytestTests) to the owner's target in plan section H: lost without nests,
        // winnable with them. Waves 4 and 5 grow most, to match the swarm that nests make stronger. They grew again (from
        // 12 + 8 and 17 + 11) when the towers came, because a Honeycomb let a player without nests win most matches, and
        // wave 3 lost a stick (from 6 + 3) so the player reaches them with a few more wasps.
        [SerializeField, Tooltip("The waves in order, one a round. Each says how many sticks and smokers it sends.")]
        private WaveEntry[] _waves =
        {
            new WaveEntry(4, 0),
            new WaveEntry(5, 1),
            new WaveEntry(5, 3),
            new WaveEntry(16, 11),
            new WaveEntry(18, 12)
        };

        /// <summary>The waves in order, one a round.</summary>
        public IReadOnlyList<WaveEntry> Waves => _waves;

        [field: SerializeField, Min(0f), Tooltip("Seconds of the first round's farm phase, before the first wave. Longer than the rest, for the lesson.")]
        public float FirstFarmSeconds { get; private set; } = 45f;

        [field: SerializeField, Min(0f), Tooltip("Seconds of every later farm phase, which starts when a wave is cleared.")]
        public float FarmSeconds { get; private set; } = 40f;

        [field: SerializeField, Min(0f), Tooltip("Wasps the swarm gets for each second of a farm phase skipped by calling the wave early, up to its capacity.")]
        public float EarlyBonusPerSecond { get; private set; } = 5f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds between two beekeepers of the same wave appearing.")]
        public float SpawnIntervalSeconds { get; private set; } = 3f;
    }
}
