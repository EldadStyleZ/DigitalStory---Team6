using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>The sprites, colors and framing that dress the town. Swap these to change the look without code.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Town Art Config", fileName = "TownArtConfig")]
    public sealed class TownArtConfig : ScriptableObject
    {
        [field: Header("Sprites")]
        [field: SerializeField, Tooltip("A square of 1 scene unit, stretched and tinted for ground, water and houses.")]
        public Sprite BlockSprite { get; private set; }

        [field: SerializeField, Tooltip("A rat facing right.")]
        public Sprite RatSprite { get; private set; }

        [field: SerializeField, Tooltip("The Piper facing right.")]
        public Sprite PiperSprite { get; private set; }

        [field: SerializeField, Tooltip("A cat facing right.")]
        public Sprite CatSprite { get; private set; }

        [field: SerializeField, Tooltip("A cat's eyes, lined up with its sprite and white so they can be tinted.")]
        public Sprite CatEyesSprite { get; private set; }

        [field: SerializeField, Tooltip("A street lamp seen from above.")]
        public Sprite LampSprite { get; private set; }

        [field: SerializeField, Tooltip("The material that draws the night over the town.")]
        public Material DarknessMaterial { get; private set; }

        [field: Header("Town colors")]
        [field: SerializeField, Tooltip("Streets.")]
        public Color GroundColor { get; private set; } = new Color32(0x25, 0x29, 0x37, 0xFF);

        [field: SerializeField, Tooltip("The muddy strip beside the water.")]
        public Color BankColor { get; private set; } = new Color32(0x2E, 0x3A, 0x2C, 0xFF);

        [field: SerializeField, Tooltip("Water the Piper can wade through.")]
        public Color ShallowsColor { get; private set; } = new Color32(0x2F, 0x71, 0x84, 0xFF);

        [field: SerializeField, Tooltip("Deep water.")]
        public Color RiverColor { get; private set; } = new Color32(0x1F, 0x4F, 0x63, 0xFF);

        [SerializeField, Tooltip("Roof colors that houses choose from.")]
        private Color[] _roofColors =
        {
            new Color32(0x4B, 0x55, 0x68, 0xFF),
            new Color32(0x56, 0x60, 0x4A, 0xFF),
            new Color32(0x6A, 0x45, 0x38, 0xFF),
            new Color32(0x5B, 0x52, 0x66, 0xFF),
            new Color32(0x46, 0x5A, 0x5E, 0xFF),
            new Color32(0x5E, 0x54, 0x46, 0xFF),
        };

        [field: Header("Rat colors")]
        [field: SerializeField, Tooltip("A rat going about its business.")]
        public Color CalmRatColor { get; private set; } = new Color32(0xA5, 0x8F, 0x80, 0xFF);

        [field: SerializeField, Tooltip("A rat under the Piper's spell.")]
        public Color CharmedRatColor { get; private set; } = new Color32(0xFF, 0xC5, 0x52, 0xFF);

        [field: Header("Cat colors")]
        [field: SerializeField, Tooltip("A cat on the prowl.")]
        public Color CatColor { get; private set; } = new Color32(0x14, 0x13, 0x1A, 0xFF);

        [field: SerializeField, Tooltip("A cat under the lullaby.")]
        public Color SleepingCatColor { get; private set; } = new Color32(0x6B, 0x6F, 0x9A, 0xFF);

        [field: SerializeField, Tooltip("A cat's eyes on the prowl.")]
        public Color CatEyeColor { get; private set; } = new Color32(0xA6, 0xF0, 0x5A, 0xFF);

        [field: SerializeField, Tooltip("A cat's eyes while it runs away.")]
        public Color FleeingCatEyeColor { get; private set; } = Color.white;

        [field: Header("Song looks")]
        [SerializeField, Tooltip("How each rat look appears, in the order of RatLook.")]
        private RatLookStyle[] _ratLooks =
        {
            new RatLookStyle(Color.white, 1f),
            new RatLookStyle(new Color32(0x6E, 0x6A, 0x78, 0xFF), 1f),
            new RatLookStyle(new Color32(0xFF, 0x96, 0xC8, 0xFF), 1.6f),
        };

        [SerializeField, Tooltip("The color of each flute hole, from hole 1 upward.")]
        private Color[] _holeColors =
        {
            new Color32(0xF2, 0xB5, 0x44, 0xFF),
            new Color32(0xF2, 0x9A, 0x44, 0xFF),
            new Color32(0x8F, 0xD3, 0x6A, 0xFF),
            new Color32(0x5F, 0xC9, 0xA8, 0xFF),
            new Color32(0x8F, 0xB8, 0xFF, 0xFF),
            new Color32(0xD9, 0xD2, 0xFF, 0xFF),
        };

        [field: SerializeField, Min(0f), Tooltip("How high a singing rat hops, in town units.")]
        public float ChoirHopHeight { get; private set; } = 7f;

        [field: SerializeField, Min(0f), Tooltip("How quickly a singing rat hops.")]
        public float ChoirHopRate { get; private set; } = 11f;

        [field: SerializeField, Min(0f), Tooltip("How high the swarm lifts the Piper, in town units.")]
        public float SurfLift { get; private set; } = 12f;

        [field: SerializeField, Min(0f), Tooltip("How far the Piper bobs while lifted, in town units.")]
        public float SurfBob { get; private set; } = 5f;

        [field: SerializeField, Min(0f), Tooltip("How quickly the Piper bobs.")]
        public float SurfBobRate { get; private set; } = 9f;

        [field: Header("Framing")]
        [field: SerializeField, Min(1f), Tooltip("How much of the town's height is on screen, in town units.")]
        public float VisibleTownHeight { get; private set; } = 700f;

        [field: SerializeField, Min(1f), Tooltip("Height of each strip of the curved river, in town units.")]
        public float RiverStripHeight { get; private set; } = 8f;

        [field: SerializeField, Min(0f), Tooltip("Width of the muddy strip before the water, in town units.")]
        public float BankWidth { get; private set; } = 30f;

        public Color RoofColor(int roofStyle) => _roofColors[roofStyle % _roofColors.Length];

        public Color HoleColor(int hole) => _holeColors[hole];

        public RatLookStyle StyleOf(RatLook look) => _ratLooks[(int)look];
    }
}
