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
    }
}
