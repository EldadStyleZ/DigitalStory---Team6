using System.Collections.Generic;
using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>
    /// The wild nests in the trees (plan section H): when they open, how the core collects one (by playing its combo, plan
    /// section U), what one gives, and where they may stand. A collected nest raises the swarm's level by one; what a level does is in <see cref="SwarmLevelConfig"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalStory/F010/Nest Config", fileName = "NestConfig")]
    public sealed class NestConfig : ScriptableObject
    {
        [field: Header("Collecting")]
        [field: SerializeField, Min(0f), Tooltip("Metres on the ground from an open nest within which the core collects it.")]
        public float CollectRadius { get; private set; } = 2.5f;

        [field: SerializeField, Min(1), Tooltip("Fewest notes in a nest's combo (plan section U): standing in the ring shows them, and playing them in order collects the nest.")]
        public int ComboShortest { get; private set; } = 4;

        [field: SerializeField, Min(1), Tooltip("Most notes in a nest's combo. Each nest draws its length from ComboShortest to this, and its notes, from the match's seed.")]
        public int ComboLongest { get; private set; } = 5;

        [field: SerializeField, Min(0), Tooltip("Wasps a collected nest adds to the swarm's capacity, up to SwarmConfig.MaxWasps.")]
        public int CapacityPerNest { get; private set; } = 400;

        [field: SerializeField, Min(0), Tooltip("Wasps a collected nest adds to the swarm at once, up to its capacity.")]
        public int WaspsPerNest { get; private set; } = 400;

        [field: SerializeField, Min(0), Tooltip("Notes of an unfound song's beginning that a collected nest fills in on the song table. Never the whole song.")]
        public int HummedNotes { get; private set; } = 2;

        [field: Header("Combo prompt: look and sound")]
        [field: SerializeField, Min(0.01f), Tooltip("Seconds a slot's pop lasts when its note is played right.")]
        public float PromptPopSeconds { get; private set; } = 0.18f;

        [field: SerializeField, Min(0f), Tooltip("How much bigger a slot grows at the top of its pop: 0.35 is 35% bigger.")]
        public float PromptPopScale { get; private set; } = 0.35f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds the prompt shakes after a wrong note.")]
        public float PromptShakeSeconds { get; private set; } = 0.3f;

        [field: SerializeField, Min(0f), Tooltip("Pixels the prompt swings to each side at the start of its shake.")]
        public float PromptShakePixels { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("Swings per second of the shake.")]
        public float PromptShakeHertz { get; private set; } = 22f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds the prompt takes to burst and fade when the last note collects the nest.")]
        public float PromptBurstSeconds { get; private set; } = 0.45f;

        [field: SerializeField, Min(0f), Tooltip("How much bigger the prompt grows as it bursts: 0.6 is 60% bigger at the end.")]
        public float PromptBurstScale { get; private set; } = 0.6f;

        [field: SerializeField, Min(0f), Tooltip("Semitones the flute's note rises for each right note of a combo, so the combo climbs as it fills.")]
        public float LiftSemitonesPerNote { get; private set; } = 2f;

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
        public float LaterNestsFarthest { get; private set; } = 34f;

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
