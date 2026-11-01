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

        [field: Header("Fighters")]
        [field: SerializeField, Tooltip("Multiplies a sleeping minion's color.")]
        public Color SleepingTint { get; private set; } = new Color(0.5f, 0.5f, 0.6f, 1f);

        [field: SerializeField, Tooltip("The empty part of a health bar over a fighter.")]
        public Color BarBackColor { get; private set; } = new Color(0f, 0f, 0f, 0.7f);

        [field: SerializeField, Tooltip("The health bar over a creature.")]
        public Color CreatureBarColor { get; private set; } = new Color32(0x6B, 0xE0, 0x7A, 0xFF);

        [field: SerializeField, Tooltip("The health bar over a minion.")]
        public Color MinionBarColor { get; private set; } = new Color32(0xFF, 0x5A, 0x5A, 0xFF);

        [field: SerializeField, Min(0f), Tooltip("How thick a health bar is, in tiles.")]
        public float BarThickness { get; private set; } = 0.14f;

        [field: Header("Effects")]
        [field: SerializeField, Tooltip("The flash of a hit on one target.")]
        public Color HitColor { get; private set; } = new Color32(0xFF, 0xF0, 0xC0, 0xFF);

        [field: SerializeField, Tooltip("The ring of a creature's area hit.")]
        public Color CreatureAreaColor { get; private set; } = new Color32(0x8E, 0xE0, 0x6B, 0xFF);

        [field: SerializeField, Tooltip("The ring of a minion's area hit.")]
        public Color MinionAreaColor { get; private set; } = new Color32(0xE0, 0x6B, 0xE0, 0xFF);

        [field: SerializeField, Tooltip("The ring when a creature arrives.")]
        public Color SummonColor { get; private set; } = new Color32(0xE8, 0xC5, 0x47, 0xFF);

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
    }
}
