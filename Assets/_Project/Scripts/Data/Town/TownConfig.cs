using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The town's size and river, and how its blocks, houses and lamps are generated.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Town Config", fileName = "TownConfig")]
    public sealed class TownConfig : ScriptableObject
    {
        [field: Header("Size")]
        [field: SerializeField, Min(0f), Tooltip("Town width in town units.")]
        public float Width { get; private set; } = 2000f;

        [field: SerializeField, Min(0f), Tooltip("Town height in town units.")]
        public float Height { get; private set; } = 1300f;

        [field: Header("River")]
        [field: SerializeField, Min(0f), Tooltip("Average x position of the river bank.")]
        public float RiverX { get; private set; } = 1750f;

        [field: SerializeField, Min(0f), Tooltip("Width of the wadeable water past the bank.")]
        public float ShallowsWidth { get; private set; } = 70f;

        [field: SerializeField, Min(0f), Tooltip("Size of the first bank wave.")]
        public float FirstWaveSize { get; private set; } = 18f;

        [field: SerializeField, Min(1f), Tooltip("Length of the first bank wave.")]
        public float FirstWaveLength { get; private set; } = 90f;

        [field: SerializeField, Min(0f), Tooltip("Size of the second bank wave.")]
        public float SecondWaveSize { get; private set; } = 9f;

        [field: SerializeField, Min(1f), Tooltip("Length of the second bank wave.")]
        public float SecondWaveLength { get; private set; } = 31f;

        [field: SerializeField, Min(0f), Tooltip("Offset of the second bank wave, in radians.")]
        public float SecondWaveOffset { get; private set; } = 2f;

        [field: Header("Blocks")]
        [field: SerializeField, Min(1f), Tooltip("Block width, including its street.")]
        public float BlockWidth { get; private set; } = 230f;

        [field: SerializeField, Min(1f), Tooltip("Block height, including its street.")]
        public float BlockHeight { get; private set; } = 190f;

        [field: SerializeField, Min(0f), Tooltip("Street width between blocks.")]
        public float StreetWidth { get; private set; } = 64f;

        [field: SerializeField, Min(0f), Tooltip("Space kept free of blocks beside the river.")]
        public float RiversideMargin { get; private set; } = 170f;

        [field: SerializeField, Min(0f), Tooltip("Blocks shorter than this are left empty.")]
        public float MinimumBlockHeight { get; private set; } = 40f;

        [field: SerializeField, Min(0f), Tooltip("Distance of the lowest street from the town edge.")]
        public float BottomStreetInset { get; private set; } = 24f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Chance that a block stays empty.")]
        public float EmptyBlockChance { get; private set; } = .15f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Chance that a block holds two houses side by side.")]
        public float SideBySideChance { get; private set; } = .4f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Chance that a block holds two stacked houses.")]
        public float StackedChance { get; private set; } = .25f;

        [field: Header("Houses")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Smallest width share of a side-by-side house.")]
        public float SideBySideMinimumShare { get; private set; } = .35f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Extra random width share of a side-by-side house.")]
        public float SideBySideExtraShare { get; private set; } = .3f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Smallest height share of a stacked house.")]
        public float StackedMinimumShare { get; private set; } = .4f;

        [field: SerializeField, Range(0f, 1f), Tooltip("Extra random height share of a stacked house.")]
        public float StackedExtraShare { get; private set; } = .2f;

        [field: SerializeField, Min(1), Tooltip("How many roof styles houses choose from.")]
        public int RoofStyleCount { get; private set; } = 6;

        [field: SerializeField, Range(0f, 1f), Tooltip("Chance that a house has a chimney.")]
        public float ChimneyChance { get; private set; } = .6f;

        [field: Header("Lamps")]
        [field: SerializeField, Range(0f, 1f), Tooltip("Chance that a junction has a lamp.")]
        public float JunctionLampChance { get; private set; } = .3f;

        [field: SerializeField, Min(0f), Tooltip("Offset of a junction lamp from the junction.")]
        public float JunctionLampOffset { get; private set; } = 14f;

        [field: SerializeField, Min(0f), Tooltip("Distance of riverside lamps from the river.")]
        public float RiversideLampSetback { get; private set; } = 110f;

        [field: SerializeField, Min(0f), Tooltip("Height of the first riverside lamp.")]
        public float RiversideLampStart { get; private set; } = 80f;

        [field: SerializeField, Min(1f), Tooltip("Spacing between riverside lamps.")]
        public float RiversideLampSpacing { get; private set; } = 210f;

        [field: SerializeField, Min(0f), Tooltip("Random extra height of each riverside lamp.")]
        public float RiversideLampJitter { get; private set; } = 50f;
    }
}
