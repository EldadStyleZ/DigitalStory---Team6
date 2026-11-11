using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The wild nests in the trees (plan section H): when they open, how the core collects one, what one gives, and where
    /// they may stand. A collected nest raises the swarm's level by one; what a level does is in <see cref="SwarmLevelConfig"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Nest Config", fileName = "NestConfig")]
    public sealed class NestConfig : ScriptableObject
    {
        [field: Header("Collecting")]
        [field: SerializeField, Min(0f), Tooltip("Metres on the ground from an open nest within which the core collects it.")]
        public float CollectRadius { get; private set; } = 2.5f;

        [field: SerializeField, Min(0f), Tooltip("Seconds the core must stay within CollectRadius, without a break, to collect the nest. Leaving the circle or a hit on the core starts them again.")]
        public float CollectSeconds { get; private set; } = 2f;

        [field: SerializeField, Min(0), Tooltip("Wasps a collected nest adds to the swarm's capacity, up to SwarmConfig.MaxWasps.")]
        public int CapacityPerNest { get; private set; } = 400;

        [field: SerializeField, Min(0), Tooltip("Wasps a collected nest adds to the swarm at once, up to its capacity.")]
        public int WaspsPerNest { get; private set; } = 400;

        [field: SerializeField, Min(0), Tooltip("Notes of an unfound song's beginning that a collected nest fills in on the song table. Never the whole song.")]
        public int HummedNotes { get; private set; } = 2;

        [field: Header("Opening")]
        [field: SerializeField, Min(0), Tooltip("Nests open when the match starts: the first ones in the scene's order.")]
        public int OpenAtStart { get; private set; } = 2;

        [SerializeField, Tooltip("How many more nests open when each wave is cleared, in wave order: the first number after wave 1. Nests open in the scene's order. The nests on the map are OpenAtStart plus all of these.")]
        private int[] _openAfterWave = { 2, 2, 1, 1 };

        [field: Header("Where they stand")]
        [field: SerializeField, Min(0f), Tooltip("Fewest metres on the ground from a nest to any hive, so collecting a nest is a trip away from home.")]
        public float HiveClearance { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from the middle of the map to a nest that is open at the start.")]
        public float FirstNestsNearest { get; private set; } = 10f;

        [field: SerializeField, Min(0f), Tooltip("Most metres from the middle of the map to a nest that is open at the start, so the first lesson's nest is close.")]
        public float FirstNestsFarthest { get; private set; } = 14f;

        [field: SerializeField, Min(0f), Tooltip("Fewest metres from the middle of the map to a nest that opens after a wave.")]
        public float LaterNestsNearest { get; private set; } = 16f;

        [field: SerializeField, Min(0f), Tooltip("Most metres from the middle of the map to a nest that opens after a wave.")]
        public float LaterNestsFarthest { get; private set; } = 30f;

        [field: SerializeField, Range(0f, 360f), Tooltip("The nests that open after a wave are spread around the middle: seen from the middle, no gap between two neighbouring ones is wider than this many degrees.")]
        public float LaterNestsWidestGapDegrees { get; private set; } = 90f;

        /// <summary>How many more nests open when each wave is cleared, in wave order: the first number is for wave 1.</summary>
        public IReadOnlyList<int> OpenAfterWave => _openAfterWave;

        /// <summary>How many nests the map holds: those open at the start plus every one that opens after a wave.</summary>
        public int NestCount
        {
            get
            {
                int count = OpenAtStart;
                for (int wave = 0; wave < _openAfterWave.Length; wave++)
                {
                    count += _openAfterWave[wave];
                }

                return count;
            }
        }
    }
}
