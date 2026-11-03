using UnityEngine;

namespace DigitalStory.Data
{
    /// <summary>How the lab looks. Sprites come from the town's art.</summary>
    [CreateAssetMenu(menuName = "DigitalStory/Lab/Art Config", fileName = "LabArtConfig")]
    public sealed class LabArtConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("The color of the maze's walls.")]
        public Color WallColor { get; private set; } = new Color32(0x4A, 0x4F, 0x6A, 0xFF);

        [field: SerializeField, Tooltip("The color of the maze's floor.")]
        public Color FloorColor { get; private set; } = new Color32(0x17, 0x19, 0x24, 0xFF);

        [field: SerializeField, Min(1f), Tooltip("How many tiles fit from the top of the screen to the bottom.")]
        public float VisibleTiles { get; private set; } = 20f;

        [field: SerializeField, Min(0f), Tooltip("How long the Piper is drawn, in tiles.")]
        public float PiperSize { get; private set; } = 1.3f;

        [field: Header("Arrows")]
        [field: SerializeField, Tooltip("The arrow on the floor, pointing east.")]
        public Sprite ArrowSprite { get; private set; }

        [field: SerializeField, Tooltip("The color of the arrows on the floor.")]
        public Color ArrowColor { get; private set; } = new Color(0.91f, 0.77f, 0.28f, 0.55f);

        [field: SerializeField, Min(0.1f), Tooltip("How long an arrow is drawn, in tiles.")]
        public float ArrowTiles { get; private set; } = 2f;

        [field: Header("Fighters")]
        [field: SerializeField, Tooltip("Multiplies a sleeping minion's color.")]
        public Color SleepingTint { get; private set; } = new Color(0.5f, 0.5f, 0.6f, 1f);

        [field: SerializeField, Tooltip("Multiplies a creature's color while it hits harder.")]
        public Color BoostTint { get; private set; } = new Color(1f, 0.55f, 0.35f, 1f);

        [field: SerializeField, Tooltip("The empty part of a health bar over a fighter.")]
        public Color BarBackColor { get; private set; } = new Color(0f, 0f, 0f, 0.7f);

        [field: SerializeField, Tooltip("The health bar over a creature.")]
        public Color CreatureBarColor { get; private set; } = new Color32(0x6B, 0xE0, 0x7A, 0xFF);

        [field: SerializeField, Tooltip("The health bar over a minion.")]
        public Color MinionBarColor { get; private set; } = new Color32(0xFF, 0x5A, 0x5A, 0xFF);

        [field: SerializeField, Min(0f), Tooltip("How thick a health bar is, in tiles.")]
        public float BarThickness { get; private set; } = 0.14f;

        [field: Header("Moving")]
        [field: SerializeField, Min(0.05f), Tooltip("How far one whole walk cycle carries a fighter, in its own body lengths.")]
        public float StrideBodyLengths { get; private set; } = 0.9f;

        [field: SerializeField, Min(0f), Tooltip("Seconds without a step before a fighter shows its standing picture.")]
        public float StillSeconds { get; private set; } = 0.15f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds a lunge takes, out and back.")]
        public float LungeSeconds { get; private set; } = 0.2f;

        [field: SerializeField, Min(0f), Tooltip("How far a lunge reaches, in the fighter's body lengths.")]
        public float LungeBodyLengths { get; private set; } = 0.22f;

        [field: SerializeField, Min(0f), Tooltip("How much bigger a healer swells as it heals, as a share of its size.")]
        public float HealPulseShare { get; private set; } = 0.12f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds a hurt fighter flashes.")]
        public float HurtSeconds { get; private set; } = 0.18f;

        [field: SerializeField, Tooltip("The color a hurt fighter flashes.")]
        public Color HurtColor { get; private set; } = new Color(1f, 0.35f, 0.35f, 1f);

        [field: SerializeField, Min(0.1f), Tooltip("Seconds of one breath of a fighter standing still.")]
        public float BreathSeconds { get; private set; } = 1.8f;

        [field: SerializeField, Min(0f), Tooltip("How much a breath changes a fighter's size, as a share of it.")]
        public float BreathShare { get; private set; } = 0.025f;

        [field: Header("Shadows")]
        [field: SerializeField, Tooltip("The soft round shadow under every fighter.")]
        public Sprite ShadowSprite { get; private set; }

        [field: SerializeField, Tooltip("The color of the shadows.")]
        public Color ShadowColor { get; private set; } = new Color(0f, 0f, 0f, 0.4f);

        [field: SerializeField, Tooltip("A shadow's length and width, in the fighter's body lengths.")]
        public Vector2 ShadowBodyLengths { get; private set; } = new Vector2(0.85f, 0.6f);

        [field: SerializeField, Tooltip("Where a shadow falls from its fighter, in body lengths. It stays put as the fighter turns.")]
        public Vector2 ShadowOffset { get; private set; } = new Vector2(0.05f, -0.07f);

        [field: Header("Effects")]
        [field: SerializeField, Tooltip("The flash of a hit on one target.")]
        public Color HitColor { get; private set; } = new Color32(0xFF, 0xF0, 0xC0, 0xFF);

        [field: SerializeField, Tooltip("The ring of a creature's area hit.")]
        public Color CreatureAreaColor { get; private set; } = new Color32(0x8E, 0xE0, 0x6B, 0xFF);

        [field: SerializeField, Tooltip("The ring of a minion's area hit.")]
        public Color MinionAreaColor { get; private set; } = new Color32(0xE0, 0x6B, 0xE0, 0xFF);

        [field: SerializeField, Tooltip("The ring when a creature arrives.")]
        public Color SummonColor { get; private set; } = new Color32(0xE8, 0xC5, 0x47, 0xFF);

        [field: SerializeField, Tooltip("The flash when health comes back.")]
        public Color HealColor { get; private set; } = new Color32(0x7C, 0xF2, 0xA0, 0xFF);

        [field: SerializeField, Tooltip("The ring warning that hidden enemies are coming, and their arrival.")]
        public Color AmbushColor { get; private set; } = new Color32(0xFF, 0x4D, 0x4D, 0xFF);

        [field: SerializeField, Tooltip("The ring when a creature goes home.")]
        public Color LeftColor { get; private set; } = new Color32(0x6B, 0x6F, 0x85, 0xFF);

        [field: SerializeField, Min(0f), Tooltip("How wide a hit's flash is, in tiles.")]
        public float HitSize { get; private set; } = 0.7f;

        [field: SerializeField, Min(0.01f), Tooltip("Seconds a hit or ring lasts.")]
        public float EffectSeconds { get; private set; } = 0.35f;

        [field: Header("Screen")]
        [field: SerializeField, Tooltip("The Piper's health bar.")]
        public Color HealthColor { get; private set; } = new Color32(0xE2, 0x4B, 0x4B, 0xFF);

        [field: SerializeField, Tooltip("The Piper's stamina bar.")]
        public Color StaminaColor { get; private set; } = new Color32(0xF2, 0xB5, 0x44, 0xFF);

        [field: SerializeField, Tooltip("The experience bar.")]
        public Color XpColor { get; private set; } = new Color32(0x5A, 0xA8, 0xFF, 0xFF);

        [field: SerializeField, Tooltip("A combo waiting for its cooldown.")]
        public Color CoolingColor { get; private set; } = new Color32(0x6B, 0x6F, 0x85, 0xFF);

        [field: Header("Balloon")]
        [field: SerializeField, Tooltip("The balloon behind the combo words.")]
        public Color BalloonColor { get; private set; } = new Color(0.97f, 0.95f, 0.88f, 0.96f);

        [field: SerializeField, Tooltip("A letter not played yet.")]
        public Color BalloonWaitingColor { get; private set; } = new Color32(0x8A, 0x8D, 0x9C, 0xFF);

        [field: SerializeField, Tooltip("The name in the balloon.")]
        public Color BalloonTextColor { get; private set; } = new Color32(0x24, 0x26, 0x33, 0xFF);

        [field: SerializeField, Tooltip("The letters played, when the combo has only begun.")]
        public Color BalloonStartColor { get; private set; } = new Color32(0xE0, 0x96, 0x10, 0xFF);

        [field: SerializeField, Tooltip("The letters played, when the combo is nearly done or finished.")]
        public Color BalloonDoneColor { get; private set; } = new Color32(0x12, 0x9E, 0x4A, 0xFF);

        [field: SerializeField, Tooltip("The balloon when a combo fails.")]
        public Color BalloonFailedColor { get; private set; } = new Color32(0xE2, 0x4B, 0x4B, 0xFF);

        [field: SerializeField, Min(0.05f), Tooltip("Seconds a finished or failed combo stays on the balloon.")]
        public float BalloonEndSeconds { get; private set; } = 0.7f;

        [field: SerializeField, Min(0f), Tooltip("How far above the Piper the balloon floats, in tiles.")]
        public float BalloonLift { get; private set; } = 1.2f;
    }
}
